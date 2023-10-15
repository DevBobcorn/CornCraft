#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Text.RegularExpressions;
using UnityEngine;

using CraftSharp.Crypto;
using CraftSharp.Event;
using CraftSharp.Proxy;
using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers.PacketPalettes;
using CraftSharp.Protocol.Handlers.Forge;
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Message;
using CraftSharp.Protocol.Session;

namespace CraftSharp.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.16+ Protocols
    /// </summary>
    /// <remarks>
    /// Typical update steps for implementing protocol changes for a new Minecraft version:
    ///  - Perform a diff between latest supported version in MCC and new stable version to support on https://wiki.vg/Protocol
    ///  - If there are any changes in packets implemented by MCC, add MCXXXVersion field below and implement new packet layouts
    /// </remarks>
    class ProtocolMinecraft : IMinecraftCom
    {
        internal const int MC_1_15_Version   = 573;
        internal const int MC_1_15_2_Version = 578;
        internal const int MC_1_16_Version   = 735;
        internal const int MC_1_16_1_Version = 736;
        internal const int MC_1_16_2_Version = 751;
        internal const int MC_1_16_3_Version = 753;
        internal const int MC_1_16_5_Version = 754;
        internal const int MC_1_17_Version   = 755;
        internal const int MC_1_17_1_Version = 756;
        internal const int MC_1_18_1_Version = 757;
        internal const int MC_1_18_2_Version = 758;
        internal const int MC_1_19_Version   = 759;
        internal const int MC_1_19_2_Version = 760;
        internal const int MC_1_19_3_Version = 761;
        internal const int MC_1_19_4_Version = 762;
        internal const int MC_1_20_Version   = 763;
        internal const int MC_1_20_2_Version = 764;

        private int compression_treshold = 0;
        private int autoCompleteTransactionId = 0;
        private readonly Dictionary<int, short> windowActions = new Dictionary<int, short>();
        private bool login_phase = true;
        private int protocolVersion;
        private int currentDimension;
        private bool isOnlineMode = false;
        private readonly BlockingCollection<Tuple<int, Queue<byte>>> packetQueue = new();

        private bool receiveDeclareCommands = false, receivePlayerInfo = false;
        private object MessageSigningLock = new();
        private Guid chatUuid = Guid.NewGuid();
        private int pendingAcknowledgments = 0, messageIndex = 0;
        private LastSeenMessagesCollector lastSeenMessagesCollector;
        private LastSeenMessageList.AcknowledgedMessage? lastReceivedMessage = null;

        readonly ProtocolForge pForge;
        readonly ProtocolTerrain pTerrain;
        readonly IMinecraftComHandler handler;
        readonly EntityMetadataPalette entityMetadataPalette;
        readonly PacketTypePalette packetPalette;
        readonly SocketWrapper socketWrapper;
        readonly DataTypes dataTypes;
        Tuple<Thread, CancellationTokenSource>? netMain = null; // Net main thread
        Tuple<Thread, CancellationTokenSource>? netReader = null; // Net reader thread
        readonly RandomNumberGenerator randomGen;

        public ProtocolMinecraft(TcpClient Client, int protocolVersion, IMinecraftComHandler handler, ForgeInfo forgeInfo)
        {
            this.socketWrapper = new SocketWrapper(Client);
            this.dataTypes = new DataTypes(protocolVersion);
            this.protocolVersion = protocolVersion;
            this.handler = handler;
            this.pForge = new ProtocolForge(forgeInfo, protocolVersion, dataTypes, this, handler);
            this.pTerrain = new ProtocolTerrain(protocolVersion, dataTypes, handler);
            this.packetPalette = new PacketTypeHandler(protocolVersion, forgeInfo != null).GetTypeHandler();
            this.randomGen = RandomNumberGenerator.Create();
            lastSeenMessagesCollector = protocolVersion >= MC_1_19_3_Version ? new(20) : new(5);

            entityMetadataPalette = EntityMetadataPalette.GetPalette(protocolVersion);

            // MessageType 
            // You can find it in https://wiki.vg/Protocol#Player_Chat_Message or /net/minecraft/network/message/MessageType.java
            if (protocolVersion >= MC_1_19_2_Version)
            {
                ChatParser.ChatId2Type = new()
                {
                    { 0,  ChatParser.MessageType.CHAT },
                    { 1,  ChatParser.MessageType.SAY_COMMAND },
                    { 2,  ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 3,  ChatParser.MessageType.MSG_COMMAND_OUTGOING },
                    { 4,  ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 5,  ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING },
                    { 6,  ChatParser.MessageType.EMOTE_COMMAND },
                };
            }
            else if (protocolVersion >= MC_1_19_Version)
            {
                ChatParser.ChatId2Type = new()
                {
                    { 0,  ChatParser.MessageType.CHAT },
                    { 1,  ChatParser.MessageType.RAW_MSG },
                    { 2,  ChatParser.MessageType.RAW_MSG },
                    { 3,  ChatParser.MessageType.SAY_COMMAND },
                    { 4,  ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 5,  ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 6,  ChatParser.MessageType.EMOTE_COMMAND },
                    { 7,  ChatParser.MessageType.RAW_MSG },
                };
            }
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        private void Updater(object? o)
        {
            CancellationToken cancelToken = (CancellationToken)o!;

            if (cancelToken.IsCancellationRequested)
                return;

            try
            {
                System.Diagnostics.Stopwatch stopWatch = new();
                while (!packetQueue.IsAddingCompleted)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    handler.OnHandlerUpdate();
                    stopWatch.Restart();

                    while (packetQueue.TryTake(out Tuple<int, Queue<byte>>? packetInfo))
                    {
                        (int packetID, Queue<byte> packetData) = packetInfo;
                        HandlePacket(packetID, packetData);

                        if (stopWatch.Elapsed.Milliseconds >= 50)
                        {
                            handler.OnHandlerUpdate();
                            stopWatch.Restart();
                        }
                    }

                    int sleepLength = 50 - stopWatch.Elapsed.Milliseconds;
                    if (sleepLength > 0)
                        Thread.Sleep(sleepLength);
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }

            if (cancelToken.IsCancellationRequested)
            {   // Normally disconnected
                handler.OnConnectionLost(DisconnectReason.ConnectionLost, "");
                return;
            }
        }

        /// <summary>
        /// Read and decompress packets.
        /// </summary>
        internal void PacketReader(object? o)
        {
            CancellationToken cancelToken = (CancellationToken)o!;
            while (socketWrapper.IsConnected() && !cancelToken.IsCancellationRequested)
            {
                try
                {
                    while (socketWrapper.HasDataAvailable())
                    {
                        packetQueue.Add(ReadNextPacket());

                        if (cancelToken.IsCancellationRequested)
                            break;
                    }
                }
                catch (System.IO.IOException) { break; }
                catch (SocketException) { break; }
                catch (NullReferenceException) { break; }
                catch (Ionic.Zlib.ZlibException) { break; }

                if (cancelToken.IsCancellationRequested)
                    break;

                Thread.Sleep(10);
            }
            packetQueue.CompleteAdding();
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetID">will contain packet ID</param>
        /// <param name="packetData">will contain raw packet Data</param>
        internal Tuple<int, Queue<byte>> ReadNextPacket()
        {
            int size = dataTypes.ReadNextVarIntRAW(socketWrapper); //Packet size
            Queue<byte> packetData = new(socketWrapper.ReadDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (compression_treshold > 0)
            {
                int sizeUncompressed = dataTypes.ReadNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData = new(uncompressed);
                }
            }

            int packetID = dataTypes.ReadNextVarInt(packetData); //Packet ID

            return new(packetID, packetData);
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetId">Packet Id</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        internal bool HandlePacket(int packetId, Queue<byte> packetData)
        {
            try
            {
                if (login_phase)
                {
                    switch (packetId) //Packet Ids are different while logging in
                    {
                        case 0x03:
                            compression_treshold = dataTypes.ReadNextVarInt(packetData);
                            break;
                        case 0x04:
                            int messageId = dataTypes.ReadNextVarInt(packetData);
                            string channel = dataTypes.ReadNextString(packetData);
                            List<byte> responseData = new List<byte>();
                            bool understood = pForge.HandleLoginPluginRequest(channel, packetData, ref responseData);
                            SendLoginPluginResponse(messageId, understood, responseData.ToArray());
                            return understood;
                        default:
                            return false; //Ignored packet
                    }
                }
                // Regular in-game packets
                else switch (packetPalette.GetIncommingTypeById(packetId))
                {
                    case PacketTypesIn.ServerData:
                        {
                            string motd = "-";

                            bool hasMotd = false;
                            if (protocolVersion < MC_1_19_4_Version)
                            {
                                hasMotd = dataTypes.ReadNextBool(packetData);

                                if (hasMotd)
                                    motd = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                            }
                            else
                            {
                                hasMotd = true;
                                motd = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                            }

                            string iconBase64 = "-";
                            bool hasIcon = dataTypes.ReadNextBool(packetData);
                            if (hasIcon)
                                iconBase64 = dataTypes.ReadNextString(packetData);

                            bool previewsChat = dataTypes.ReadNextBool(packetData);

                            handler.OnServerDataReceived(hasMotd, motd, hasIcon, iconBase64, previewsChat);
                            break;
                        }
                    case PacketTypesIn.KeepAlive:
                        {
                            SendPacket(PacketTypesOut.KeepAlive, packetData);
                            handler.OnServerKeepAlive();
                            break;
                        }
                    case PacketTypesIn.Ping:
                        {
                            int ID = dataTypes.ReadNextInt(packetData);
                            SendPacket(PacketTypesOut.Pong, DataTypes.GetInt(ID));
                            break;
                        }
                    case PacketTypesIn.JoinGame:
                        {
                            {
                                // Temporary fix
                                receiveDeclareCommands = receivePlayerInfo = false;

                                messageIndex = 0;
                                pendingAcknowledgments = 0;

                                lastReceivedMessage = null;
                                lastSeenMessagesCollector = protocolVersion >= MC_1_19_3_Version ? new(20) : new(5);
                            }

                            handler.OnGameJoined();
                            int playerEntityId = dataTypes.ReadNextInt(packetData);
                            handler.OnReceivePlayerEntityID(playerEntityId);

                            if (protocolVersion >= MC_1_16_2_Version)
                                dataTypes.ReadNextBool(packetData);                       // Is hardcore - 1.16.2 and above

                            handler.OnGamemodeUpdate(Guid.Empty, dataTypes.ReadNextByte(packetData));

                            if (protocolVersion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextByte(packetData);                       // Previous Gamemode - 1.16 and above
                                int worldCount = dataTypes.ReadNextVarInt(packetData);    // Dimension Count (World Count) - 1.16 and above
                                for (int i = 0; i < worldCount; i++)
                                    dataTypes.ReadNextString(packetData);                 // Dimension Names (World Names) - 1.16 and above
                                var registryCodec = dataTypes.ReadNextNbt(packetData);    // Registry Codec (Dimension Codec) - 1.16 and above

                                // Read and store defined dimensions 1.16.2 and above
                                World.StoreDimensionList(registryCodec);

                                // Read and store defined biomes 1.16.2 and above
                                World.StoreBiomeList(registryCodec);
                            }

                            // Current dimension
                            //   String: 1.19 and above
                            //   NBT Tag Compound: [1.16.2 to 1.18.2]
                            //   String identifier: 1.16 and 1.16.1
                            //   varInt: [1.9.1 to 1.15.2]
                            //   byte: below 1.9.1
                            string? dimensionTypeName = null;
                            Dictionary<string, object>? dimensionType = null;
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                if (protocolVersion >= MC_1_19_Version)
                                    dimensionTypeName = dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                else if (protocolVersion >= MC_1_16_2_Version)
                                    dimensionType = dataTypes.ReadNextNbt(packetData);        // Dimension Type: NBT Tag Compound
                                else
                                    dataTypes.ReadNextString(packetData);
                                this.currentDimension = 0;
                            }
                            else
                                this.currentDimension = dataTypes.ReadNextInt(packetData);
                            
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                string dimensionName = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above
                                
                                if (protocolVersion >= MC_1_16_2_Version && protocolVersion <= MC_1_18_2_Version)
                                {
                                    World.StoreOneDimension(dimensionName, dimensionType!);
                                    World.SetDimension(dimensionName);
                                }
                                else if (protocolVersion >= MC_1_19_Version)
                                {
                                    World.SetDimension(dimensionTypeName!);
                                }
                            }

                            if (protocolVersion >= MC_1_15_Version)
                                dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above

                            if (protocolVersion >= MC_1_16_2_Version)
                                dataTypes.ReadNextVarInt(packetData);         // Max Players - 1.16.2 and above
                            else
                                dataTypes.ReadNextByte(packetData);           // Max Players - 1.16.1 and below

                            if (protocolVersion < MC_1_16_Version)
                                dataTypes.SkipNextString(packetData);         // Level Type - 1.15 and below
                            
                            dataTypes.ReadNextVarInt(packetData);             // View distance - 1.14 and above
                            
                            if (protocolVersion >= MC_1_18_1_Version)
                                dataTypes.ReadNextVarInt(packetData);         // Simulation Distance - 1.18 and above
                            
                            dataTypes.ReadNextBool(packetData);               // Reduced debug info - 1.8 and above

                            if (protocolVersion >= MC_1_15_Version)
                                dataTypes.ReadNextBool(packetData);           // Enable respawn screen - 1.15 and above

                            if (protocolVersion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                                dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                            }
                            if (protocolVersion >= MC_1_19_Version)
                            {
                                bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                                if (hasDeathLocation)
                                {
                                    dataTypes.SkipNextString(packetData);     // Death dimension name: Identifier
                                    dataTypes.ReadNextLocation(packetData);   // Death location
                                }
                            }
                            if (protocolVersion >= MC_1_20_Version)
                                dataTypes.ReadNextVarInt(packetData); // Portal Cooldown - 1.20 and above
                            
                            // Enable chat
                            if (protocolVersion < MC_1_19_Version)
                            {
                                handler.SetCanSendMessage(true);
                            }
                            break;
                        }
                    case PacketTypesIn.SpawnPainting: // Just skip, no need for this
                        return true;
                    case PacketTypesIn.DeclareCommands:
                        if (protocolVersion >= MC_1_19_Version)
                        {
                            DeclareCommands.Read(dataTypes, packetData, protocolVersion);
                            receiveDeclareCommands = true;
                            if (receivePlayerInfo)
                                handler.SetCanSendMessage(true);
                        }

                        break;
                    case PacketTypesIn.ChatMessage:
                        {
                            int messageType = 0;

                            if (protocolVersion <= MC_1_18_2_Version) // 1.18 and below
                            {
                                string message = dataTypes.ReadNextString(packetData);

                                Guid senderUUID;
                                //Hide system messages or xp bar messages?
                                messageType = dataTypes.ReadNextByte(packetData);
                                if ((messageType == 1 && !CornGlobal.DisplaySystemMessages)
                                    || (messageType == 2 && !CornGlobal.DisplayXPBarMessages))
                                    break;

                                if (protocolVersion >= MC_1_16_5_Version)
                                    senderUUID = dataTypes.ReadNextUUID(packetData);
                                else senderUUID = Guid.Empty;

                                handler.OnTextReceived(new(message, null, true, messageType, senderUUID));
                            }
                            else if (protocolVersion == MC_1_19_Version) // 1.19
                            {
                                string signedChat = dataTypes.ReadNextString(packetData);

                                bool hasUnsignedChatContent = dataTypes.ReadNextBool(packetData);
                                string? unsignedChatContent =
                                    hasUnsignedChatContent ? dataTypes.ReadNextString(packetData) : null;

                                messageType = dataTypes.ReadNextVarInt(packetData);
                                if ((messageType == 1 && !CornGlobal.DisplaySystemMessages)
                                    || (messageType == 2 && !CornGlobal.DisplayXPBarMessages))
                                    break;

                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                string senderDisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                                bool hasSenderTeamName = dataTypes.ReadNextBool(packetData);
                                string? senderTeamName = hasSenderTeamName
                                    ? ChatParser.ParseText(dataTypes.ReadNextString(packetData))
                                    : null;

                                long timestamp = dataTypes.ReadNextLong(packetData);

                                long salt = dataTypes.ReadNextLong(packetData);

                                byte[] messageSignature = dataTypes.ReadNextByteArray(packetData);

                                bool verifyResult;
                                if (!isOnlineMode)
                                    verifyResult = false;
                                else if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    verifyResult = player != null && player.VerifyMessage(signedChat, timestamp, salt,
                                        ref messageSignature);
                                }

                                ChatMessage chat = new(signedChat, true, messageType, senderUUID, unsignedChatContent,
                                    senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult);
                                handler.OnTextReceived(chat);
                            }
                            else if (protocolVersion == MC_1_19_2_Version)
                            {
                                // 1.19.1 - 1.19.2
                                byte[]? precedingSignature = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextByteArray(packetData)
                                    : null;
                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                byte[] headerSignature = dataTypes.ReadNextByteArray(packetData);

                                string signedChat = dataTypes.ReadNextString(packetData);
                                string? decorated = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextString(packetData)
                                    : null;

                                long timestamp = dataTypes.ReadNextLong(packetData);
                                long salt = dataTypes.ReadNextLong(packetData);

                                int lastSeenMessageListLen = dataTypes.ReadNextVarInt(packetData);
                                LastSeenMessageList.AcknowledgedMessage[] lastSeenMessageList =
                                    new LastSeenMessageList.AcknowledgedMessage[lastSeenMessageListLen];
                                for (int i = 0; i < lastSeenMessageListLen; ++i)
                                {
                                    Guid user = dataTypes.ReadNextUUID(packetData);
                                    byte[] lastSignature = dataTypes.ReadNextByteArray(packetData);
                                    lastSeenMessageList[i] = new(user, lastSignature, true);
                                }

                                LastSeenMessageList lastSeenMessages = new(lastSeenMessageList);

                                string? unsignedChatContent = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextString(packetData)
                                    : null;

                                MessageFilterType filterEnum = (MessageFilterType)dataTypes.ReadNextVarInt(packetData);
                                if (filterEnum == MessageFilterType.PartiallyFiltered)
                                    dataTypes.ReadNextULongArray(packetData);

                                int chatTypeId = dataTypes.ReadNextVarInt(packetData);
                                string chatName = dataTypes.ReadNextString(packetData);
                                string? targetName = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextString(packetData)
                                    : null;

                                Dictionary<string, Json.JSONData> chatInfo = Json.ParseJson(chatName).Properties;
                                string senderDisplayName =
                                    (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"])
                                    .StringValue;
                                string? senderTeamName = null;
                                ChatParser.MessageType messageTypeEnum =
                                    ChatParser.ChatId2Type!.GetValueOrDefault(chatTypeId, ChatParser.MessageType.CHAT);
                                if (targetName != null &&
                                    (messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING ||
                                     messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING))
                                    senderTeamName = Json.ParseJson(targetName).Properties["with"].DataArray[0]
                                        .Properties["text"].StringValue;

                                if (string.IsNullOrWhiteSpace(senderDisplayName))
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    if (player != null && (player.DisplayName != null || player.Name != null) &&
                                        string.IsNullOrWhiteSpace(senderDisplayName))
                                    {
                                        senderDisplayName = ChatParser.ParseText(player.DisplayName ?? player.Name);
                                        if (string.IsNullOrWhiteSpace(senderDisplayName))
                                            senderDisplayName = player.DisplayName ?? player.Name;
                                        else
                                            senderDisplayName += "§r";
                                    }
                                }

                                bool verifyResult;
                                if (!isOnlineMode)
                                    verifyResult = false;
                                else if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    if (player == null || !player.IsMessageChainLegal())
                                        verifyResult = false;
                                    else
                                    {
                                        bool lastVerifyResult = player.IsMessageChainLegal();
                                        verifyResult = player.VerifyMessage(signedChat, timestamp, salt,
                                            ref headerSignature, ref precedingSignature, lastSeenMessages);
                                        if (lastVerifyResult && !verifyResult)
                                            Debug.LogWarning(Translations.Get("chat_message_chain_broken", senderDisplayName));
                                    }
                                }

                                ChatMessage chat = new(signedChat, false, chatTypeId, senderUUID, unsignedChatContent,
                                    senderDisplayName, senderTeamName, timestamp, headerSignature, verifyResult);
                                if (isOnlineMode && !chat.LacksSender())
                                    Acknowledge(chat);
                                handler.OnTextReceived(chat);
                            }
                            else if (protocolVersion >= MC_1_19_3_Version)
                            {
                                // 1.19.3+
                                // Header section
                                // net.minecraft.network.packet.s2c.play.ChatMessageS2CPacket#write
                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                int index = dataTypes.ReadNextVarInt(packetData);
                                // Signature is fixed size of 256 bytes
                                byte[]? messageSignature = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextByteArray(packetData, 256)
                                    : null;

                                // Body
                                // net.minecraft.network.message.MessageBody.Serialized#write
                                string message = dataTypes.ReadNextString(packetData);
                                long timestamp = dataTypes.ReadNextLong(packetData);
                                long salt = dataTypes.ReadNextLong(packetData);

                                // Previous Messages
                                // net.minecraft.network.message.LastSeenMessageList.Indexed#write
                                // net.minecraft.network.message.MessageSignatureData.Indexed#write
                                int totalPreviousMessages = dataTypes.ReadNextVarInt(packetData);
                                Tuple<int, byte[]?>[] previousMessageSignatures =
                                    new Tuple<int, byte[]?>[totalPreviousMessages];
                                for (int i = 0; i < totalPreviousMessages; i++)
                                {
                                    // net.minecraft.network.message.MessageSignatureData.Indexed#fromBuf
                                    int messageId = dataTypes.ReadNextVarInt(packetData) - 1;
                                    if (messageId == -1)
                                        previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId,
                                            dataTypes.ReadNextByteArray(packetData, 256));
                                    else
                                        previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId, null);
                                }

                                // Other
                                string? unsignedChatContent = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextString(packetData)
                                    : null;

                                MessageFilterType filterType = (MessageFilterType)dataTypes.ReadNextVarInt(packetData);

                                if (filterType == MessageFilterType.PartiallyFiltered)
                                    dataTypes.ReadNextULongArray(packetData);

                                // Network Target
                                // net.minecraft.network.message.MessageType.Serialized#write
                                int chatTypeId = dataTypes.ReadNextVarInt(packetData);
                                string chatName = dataTypes.ReadNextString(packetData);
                                string? targetName = dataTypes.ReadNextBool(packetData)
                                    ? dataTypes.ReadNextString(packetData)
                                    : null;

                                ChatParser.MessageType messageTypeEnum =
                                    ChatParser.ChatId2Type!.GetValueOrDefault(chatTypeId, ChatParser.MessageType.CHAT);

                                Dictionary<string, Json.JSONData> chatInfo =
                                    Json.ParseJson(targetName ?? chatName).Properties;
                                string senderDisplayName =
                                    (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"])
                                    .StringValue;
                                string? senderTeamName = null;
                                if (targetName != null &&
                                    (messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING ||
                                     messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING))
                                    senderTeamName = Json.ParseJson(targetName).Properties["with"].DataArray[0]
                                        .Properties["text"].StringValue;

                                if (string.IsNullOrWhiteSpace(senderDisplayName))
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    if (player != null && (player.DisplayName != null || player.Name != null) &&
                                        string.IsNullOrWhiteSpace(senderDisplayName))
                                    {
                                        senderDisplayName = ChatParser.ParseText(player.DisplayName ?? player.Name);
                                        if (string.IsNullOrWhiteSpace(senderDisplayName))
                                            senderDisplayName = player.DisplayName ?? player.Name;
                                        else
                                            senderDisplayName += "§r";
                                    }
                                }

                                bool verifyResult;
                                if (!isOnlineMode || messageSignature == null)
                                    verifyResult = false;
                                else
                                {
                                    if (senderUUID == handler.GetUserUuid())
                                        verifyResult = true;
                                    else
                                    {
                                        PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                        if (player == null || !player.IsMessageChainLegal())
                                            verifyResult = false;
                                        else
                                        {
                                            verifyResult = false;
                                            verifyResult = player.VerifyMessage(message, senderUUID, player.ChatUuid,
                                                index, timestamp, salt, ref messageSignature,
                                                previousMessageSignatures);
                                        }
                                    }
                                }

                                ChatMessage chat = new(message, false, chatTypeId, senderUUID, unsignedChatContent,
                                    senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult);
                                lock (MessageSigningLock)
                                    Acknowledge(chat);
                                handler.OnTextReceived(chat);
                            }

                            break;
                        }
                    case PacketTypesIn.HideMessage:
                        /* Not handled */
                        break;
                    case PacketTypesIn.SystemChat:
                        string systemMessage = dataTypes.ReadNextString(packetData);
                        if (protocolVersion >= MC_1_19_3_Version)
                        {
                            bool isOverlay = dataTypes.ReadNextBool(packetData);
                            if (isOverlay)
                            {
                                if (!CornGlobal.DisplayXPBarMessages)
                                    break;
                            }
                            else
                            {
                                if (!CornGlobal.DisplaySystemMessages)
                                    break;
                            }

                            handler.OnTextReceived(new(systemMessage, null, true, -1, Guid.Empty, true));
                        }
                        else
                        {
                            int msgType = dataTypes.ReadNextVarInt(packetData);
                            if ((msgType == 1 && !CornGlobal.DisplaySystemMessages))
                                break;
                            handler.OnTextReceived(new(systemMessage, null, true, msgType, Guid.Empty, true));
                        }

                        break;
                    case PacketTypesIn.ProfilelessChatMessage:
                        string message_ = dataTypes.ReadNextString(packetData);
                        int messageType_ = dataTypes.ReadNextVarInt(packetData);
                        string messageName = dataTypes.ReadNextString(packetData);
                        string? targetName_ = dataTypes.ReadNextBool(packetData)
                            ? dataTypes.ReadNextString(packetData)
                            : null;
                        ChatMessage profilelessChat = new(message_, targetName_ ?? messageName, true, messageType_,
                            Guid.Empty, true);
                        profilelessChat.isSenderJson = true;
                        handler.OnTextReceived(profilelessChat);
                        break;
                    case PacketTypesIn.CombatEvent:
                        /* Not handled */
                        break;
                    case PacketTypesIn.DeathCombatEvent:
                        /* Not handled */
                        break;
                    case PacketTypesIn.DamageEvent: // 1.19.4
                        if (protocolVersion >= MC_1_19_4_Version)
                        {
                            var entityId = dataTypes.ReadNextVarInt(packetData);
                            var sourceTypeId = dataTypes.ReadNextVarInt(packetData);
                            var sourceCauseId = dataTypes.ReadNextVarInt(packetData);
                            var sourceDirectId = dataTypes.ReadNextVarInt(packetData);

                            Location? sourcePos;
                            if (dataTypes.ReadNextBool(packetData))
                            {
                                sourcePos = new Location()
                                {
                                    X = dataTypes.ReadNextDouble(packetData),
                                    Y = dataTypes.ReadNextDouble(packetData),
                                    Z = dataTypes.ReadNextDouble(packetData)
                                };
                            }

                            // TODO: Write a function to use this data ? But seems not too useful
                        }
                        break;
                    case PacketTypesIn.MessageHeader: // 1.19.2 only
                        if (protocolVersion == MC_1_19_2_Version)
                        {
                            byte[]? precedingSignature = dataTypes.ReadNextBool(packetData)
                                ? dataTypes.ReadNextByteArray(packetData)
                                : null;
                            Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                            byte[] headerSignature = dataTypes.ReadNextByteArray(packetData);
                            byte[] bodyDigest = dataTypes.ReadNextByteArray(packetData);

                            bool verifyResult;

                            if (!isOnlineMode)
                                verifyResult = false;
                            else if (senderUUID == handler.GetUserUuid())
                                verifyResult = true;
                            else
                            {
                                PlayerInfo? player = handler.GetPlayerInfo(senderUUID);

                                if (player == null || !player.IsMessageChainLegal())
                                    verifyResult = false;
                                else
                                {
                                    bool lastVerifyResult = player.IsMessageChainLegal();
                                    verifyResult = player.VerifyMessageHead(ref precedingSignature,
                                        ref headerSignature, ref bodyDigest);
                                    
                                    if (lastVerifyResult && !verifyResult)
                                        Debug.LogWarning(Translations.Get("chat_message_chain_broken", player.Name));
                                }
                            }
                        }

                        break;
                    case PacketTypesIn.Respawn:
                        {
                            string? dimensionTypeNameRespawn = null;
                            Dictionary<string, object>? dimensionTypeRespawn = null;
                            // MC 1.16+
                            if (protocolVersion >= MC_1_19_Version)
                                dimensionTypeNameRespawn = dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                            else if (protocolVersion >= MC_1_16_2_Version)
                                dimensionTypeRespawn = dataTypes.ReadNextNbt(packetData);        // Dimension Type: NBT Tag Compound
                            else
                                dataTypes.ReadNextString(packetData);
                            
                            this.currentDimension = 0;

                            string dimensionName = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above
                            
                            if (protocolVersion >= MC_1_16_2_Version && protocolVersion <= MC_1_18_2_Version)
                            {
                                World.SetDimension(dimensionName);
                            }
                            else if (protocolVersion >= MC_1_19_Version)
                            {
                                World.SetDimension(dimensionTypeNameRespawn!);
                            }

                            dataTypes.ReadNextLong(packetData);               // Hashed world seed - 1.15 and above
                            dataTypes.ReadNextByte(packetData);               // Gamemode
                            dataTypes.ReadNextByte(packetData);               // Previous Game mode - 1.16 and above

                            dataTypes.ReadNextBool(packetData);               // Is Debug - 1.16 and above
                            dataTypes.ReadNextBool(packetData);               // Is Flat - 1.16 and above

                            bool keepAttributes = false, keepMetadata = false;

                            if (protocolVersion < MC_1_20_Version)
                            {
                                keepMetadata = dataTypes.ReadNextBool(packetData);   // Copy metadata - 1.16 to 1.19.4
                                keepAttributes = keepMetadata;
                            }
                            else if (protocolVersion < MC_1_20_2_Version)
                            {
                                // Data kept flags
                                byte dataKept = dataTypes.ReadNextByte(packetData);  // Data Kept - 1.20 and 1.20.1
                                keepAttributes = (dataKept & 0x01) != 0;
                                keepMetadata = (dataKept & 0x02) != 0;
                            }
                            
                            if (protocolVersion >= MC_1_19_Version)
                            {
                                bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                                if (hasDeathLocation)
                                {
                                    dataTypes.ReadNextString(packetData); // Death dimension name: Identifier
                                    dataTypes.ReadNextLocation(packetData); // Death location
                                }
                            }

                            if (protocolVersion >= MC_1_20_Version)
                                dataTypes.ReadNextVarInt(packetData); // Portal Cooldown - 1.20 and above
                            
                            if (protocolVersion >= MC_1_20_2_Version)
                            {
                                // Data kept flags
                                byte dataKept = dataTypes.ReadNextByte(packetData);  // Data Kept - 1.20 and 1.20.1
                                keepAttributes = (dataKept & 0x01) != 0;
                                keepMetadata = (dataKept & 0x02) != 0;
                            }

                            handler.OnRespawn(keepAttributes, keepMetadata);
                            break;
                        }
                    case PacketTypesIn.PlayerPositionAndLook:
                        {
                            // These always need to be read, since we need the field after them for teleport confirm
                            double x = dataTypes.ReadNextDouble(packetData);
                            double y = dataTypes.ReadNextDouble(packetData);
                            double z = dataTypes.ReadNextDouble(packetData);
                            float yaw = dataTypes.ReadNextFloat(packetData);
                            float pitch = dataTypes.ReadNextFloat(packetData);
                            byte locMask = dataTypes.ReadNextByte(packetData);

                            // entity handling require player pos for distance calculating
                            Location location = handler.GetLocation();
                            location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                            location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                            location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                            bool yawIsOffset = (locMask & 1 << 3) != 0;
                            bool pitchIsOffset = (locMask & 1 << 4) != 0;
                            handler.UpdateLocation(location, yawIsOffset, yaw, pitchIsOffset, pitch);

                            int teleportId = dataTypes.ReadNextVarInt(packetData);
                            // Teleport confirm packet
                            SendPacket(PacketTypesOut.TeleportConfirm, DataTypes.GetVarInt(teleportId));
                            
                            if (protocolVersion >= MC_1_17_Version && protocolVersion < MC_1_19_4_Version)
                                dataTypes.ReadNextBool(packetData); // Dismount Vehicle    - 1.17 to 1.19.3
                        }
                        break;
                    case PacketTypesIn.ChunkData:
                        {
                            var chunkRenderManager = handler.GetChunkRenderManager();

                            Interlocked.Increment(ref chunkRenderManager.chunkCnt);
                            Interlocked.Increment(ref chunkRenderManager.chunkLoadNotCompleted);
                            
                            int chunkX = dataTypes.ReadNextInt(packetData);
                            int chunkZ = dataTypes.ReadNextInt(packetData);

                            if (protocolVersion >= MC_1_17_Version)
                            {
                                ulong[]? verticalStripBitmask = null;

                                if (protocolVersion == MC_1_17_Version || protocolVersion == MC_1_17_1_Version)
                                    verticalStripBitmask = dataTypes.ReadNextULongArray(packetData); // Bit Mask Length  and  Primary Bit Mask

                                dataTypes.ReadNextNbt(packetData); // Heightmaps

                                if (pTerrain.ProcessChunkColumnData17(chunkX, chunkZ, verticalStripBitmask, packetData))
                                    Interlocked.Decrement(ref chunkRenderManager.chunkLoadNotCompleted);
                            }
                            else
                            {
                                bool chunksContinuous = dataTypes.ReadNextBool(packetData);
                                if (protocolVersion >= MC_1_16_Version && protocolVersion <= MC_1_16_1_Version)
                                    dataTypes.ReadNextBool(packetData); // Ignore old data - 1.16 to 1.16.1 only
                                
                                ushort chunkMask = (ushort)dataTypes.ReadNextVarInt(packetData);

                                dataTypes.ReadNextNbt(packetData);  // Heightmaps - 1.14 and above

                                if (pTerrain.ProcessChunkColumnData16(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, currentDimension, packetData))
                                    Interlocked.Decrement(ref chunkRenderManager.chunkLoadNotCompleted);
                            }
                            break;
                        }
                    case PacketTypesIn.UpdateLight:
                        {
                            int chunkX = dataTypes.ReadNextVarInt(packetData);
                            int chunkZ = dataTypes.ReadNextVarInt(packetData);

                            pTerrain.ProcessChunkLightData(chunkX, chunkZ, packetData);
                            break;
                        }
                    case PacketTypesIn.MapData:
                        {
                            int mapId = dataTypes.ReadNextVarInt(packetData);
                            byte scale = dataTypes.ReadNextByte(packetData);

                            bool trackingPosition = true; //  1.9+
                            bool locked = false;          // 1.14+

                            // 1.17+ (locked and trackingPosition switched places)
                            if (protocolVersion >= MC_1_17_Version)
                            {
                                locked = dataTypes.ReadNextBool(packetData);

                                trackingPosition = dataTypes.ReadNextBool(packetData);
                            }
                            else
                            {
                                trackingPosition = dataTypes.ReadNextBool(packetData);

                                locked = dataTypes.ReadNextBool(packetData);
                            }

                            int iconCount = 0;
                            List<MapIcon> icons = new();

                            // 1.9 or later needs tracking position to be true to get the icons
                            if (trackingPosition)
                            {
                                iconCount = dataTypes.ReadNextVarInt(packetData);

                                for (int i = 0; i < iconCount; i++)
                                {
                                    MapIcon mapIcon = new();

                                    // 1.13.2+
                                    mapIcon.Type = (MapIconType)dataTypes.ReadNextVarInt(packetData);

                                    mapIcon.X = dataTypes.ReadNextByte(packetData);
                                    mapIcon.Z = dataTypes.ReadNextByte(packetData);

                                    // 1.13.2+
                                    mapIcon.Direction = dataTypes.ReadNextByte(packetData);

                                    if (dataTypes.ReadNextBool(packetData)) // Has Display Name?
                                        mapIcon.DisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                                    icons.Add(mapIcon);
                                }
                            }

                            byte colsUpdated = dataTypes.ReadNextByte(packetData); // width
                            byte rowsUpdated = 0; // height
                            byte mapColX = 0;
                            byte mapRowZ = 0;
                            byte[]? colors = null;

                            if (colsUpdated > 0)
                            {
                                rowsUpdated = dataTypes.ReadNextByte(packetData); // height
                                mapColX = dataTypes.ReadNextByte(packetData);
                                mapRowZ = dataTypes.ReadNextByte(packetData);
                                colors = dataTypes.ReadNextByteArray(packetData);
                            }

                            handler.OnMapData(mapId, scale, trackingPosition, locked, icons, colsUpdated, rowsUpdated, mapColX, mapRowZ, colors);
                            break;
                        }
                    case PacketTypesIn.TradeList: // MC 1.14 or greater
                        {
                            int windowId = dataTypes.ReadNextVarInt(packetData);
                            int size = dataTypes.ReadNextByte(packetData);
                            List<VillagerTrade> trades = new List<VillagerTrade>();
                            for (int tradeId = 0; tradeId < size; tradeId++)
                            {
                                VillagerTrade trade = dataTypes.ReadNextTrade(packetData, ItemPalette.INSTANCE);
                                    trades.Add(trade);
                            }
                            VillagerInfo villagerInfo = new VillagerInfo()
                            {
                                Level = dataTypes.ReadNextVarInt(packetData),
                                Experience = dataTypes.ReadNextVarInt(packetData),
                                IsRegularVillager = dataTypes.ReadNextBool(packetData),
                                CanRestock = dataTypes.ReadNextBool(packetData)
                            };
                            handler.OnTradeList(windowId, trades, villagerInfo);
                        }
                        break;
                    case PacketTypesIn.Title:
                        {
                            int action = dataTypes.ReadNextVarInt(packetData);
                            string titleText = string.Empty;
                            string subtitleText = string.Empty;
                            string actionbarText = string.Empty;
                            string json = string.Empty;
                            int fadein = -1;
                            int stay = -1;
                            int fadeout = -1;
                            // MC 1.10 or greater
                            if (action == 0)
                            {
                                json = titleText;
                                titleText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                            }
                            else if (action == 1)
                            {
                                json = subtitleText;
                                subtitleText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                            }
                            else if (action == 2)
                            {
                                json = actionbarText;
                                actionbarText = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                            }
                            else if (action == 3)
                            {
                                fadein = dataTypes.ReadNextInt(packetData);
                                stay = dataTypes.ReadNextInt(packetData);
                                fadeout = dataTypes.ReadNextInt(packetData);
                            }
                            handler.OnTitle(action, titleText, subtitleText, actionbarText, fadein, stay, fadeout, json);

                            break;
                        }
                    case PacketTypesIn.MultiBlockChange:
                        {
                            // MC 1.16.2+
                            long chunkSection = dataTypes.ReadNextLong(packetData);
                            int sectionX = (int)(chunkSection >> 42);
                            int sectionY = (int)((chunkSection << 44) >> 44);
                            int sectionZ = (int)((chunkSection << 22) >> 42);
                            
                            if(protocolVersion < MC_1_20_Version)
                                dataTypes.ReadNextBool(packetData); // Useless boolean (Related to light update)

                            int blocksSize = dataTypes.ReadNextVarInt(packetData);

                            for (int i = 0; i < blocksSize; i++)
                            {
                                ulong block = (ulong)dataTypes.ReadNextVarLong(packetData);
                                int blockId = (int)(block >> 12);
                                int localX = (int)((block >> 8) & 0x0F);
                                int localZ = (int)((block >> 4) & 0x0F);
                                int localY = (int)(block & 0x0F);

                                Block bloc = new Block((ushort)blockId);
                                int blockX = (sectionX * 16) + localX;
                                int blockY = (sectionY * 16) + localY;
                                int blockZ = (sectionZ * 16) + localZ;
                                var blockLoc = new BlockLoc(blockX, blockY, blockZ);
                                handler.GetChunkRenderManager().SetBlock(blockLoc, bloc);
                            }
                            break;
                        }
                    case PacketTypesIn.BlockChange:
                        {
                            var blockLoc = dataTypes.ReadNextBlockLoc(packetData);
                            handler.GetChunkRenderManager().SetBlock(blockLoc,
                                    new Block((ushort) dataTypes.ReadNextVarInt(packetData)));
                            break;
                        }
                    case PacketTypesIn.BlockEntityData:
                        {
                            var blockLoc = dataTypes.ReadNextBlockLoc(packetData);
                            var ttt = dataTypes.ReadNextVarInt(packetData);
                            var tag = dataTypes.ReadNextNbt(packetData);
                            // Output block entity data
                            var typeId = ResourceLocation.FromString((string) tag["id"]);
                            var type = BlockEntityPalette.INSTANCE.FromId(typeId);
                            UnityEngine.Debug.Log($"Single [{blockLoc}] {Json.Object2Json(tag)}");
                            Loom.QueueOnMainThread(() => {
                                handler.GetChunkRenderManager().AddBlockEntityRender(blockLoc, type, tag);
                            });
                            break;
                        }
                    case PacketTypesIn.UnloadChunk:
                        {
                            int chunkX = dataTypes.ReadNextInt(packetData);
                            int chunkZ = dataTypes.ReadNextInt(packetData);

                            if (handler.GetChunkRenderManager().GetChunkColumn(chunkX, chunkZ) != null)
                                Interlocked.Decrement(ref handler.GetChunkRenderManager().chunkCnt);
                            // Warning: It is legal to include unloaded chunks in the UnloadChunk packet. Since chunks that have not been loaded are not recorded, this may result in loading chunks that should be unloaded and inaccurate statistics.
                            handler.GetChunkRenderManager().UnloadChunkColumn(chunkX, chunkZ);
                            break;
                        }
                    case PacketTypesIn.ChangeGameState:
                        {
                            byte changeReason = dataTypes.ReadNextByte(packetData);
                            float changeValue = dataTypes.ReadNextFloat(packetData);
                            switch (changeReason)
                            {
                                case 1: // End raining
                                    handler.OnRainChange(false);
                                    break;
                                case 2: // Begin raining
                                    handler.OnRainChange(true);
                                    break;
                                case 7: // Rain level change
                                    
                                    break;
                                case 8: // Thunder level change
                                    
                                    break;
                            }
                        }
                        break;
                    case PacketTypesIn.SetDisplayChatPreview:
                        bool previewsChatSetting = dataTypes.ReadNextBool(packetData);
                        // TODO handler.OnChatPreviewSettingUpdate(previewsChatSetting);
                        break;
                    case PacketTypesIn.ChatPreview:
                        // TODO Currently not implemented
                        break;
                    case PacketTypesIn.PlayerInfo:
                        if (protocolVersion >= MC_1_19_3_Version)
                        {
                            byte actionBitset = dataTypes.ReadNextByte(packetData);
                            int numberOfActions = dataTypes.ReadNextVarInt(packetData);
                            for (int i = 0; i < numberOfActions; i++)
                            {
                                Guid playerUuid = dataTypes.ReadNextUUID(packetData);

                                PlayerInfo player;
                                if ((actionBitset & (1 << 0)) > 0) // Actions bit 0: add player
                                {
                                    string name = dataTypes.ReadNextString(packetData);
                                    int numberOfProperties = dataTypes.ReadNextVarInt(packetData);
                                    for (int j = 0; j < numberOfProperties; ++j)
                                    {
                                        dataTypes.SkipNextString(packetData);
                                        dataTypes.SkipNextString(packetData);
                                        if (dataTypes.ReadNextBool(packetData))
                                            dataTypes.SkipNextString(packetData);
                                    }

                                    player = new(name, playerUuid);
                                    handler.OnPlayerJoin(player);
                                }
                                else
                                {
                                    PlayerInfo? playerGet = handler.GetPlayerInfo(playerUuid);
                                    if (playerGet == null)
                                    {
                                        player = new(string.Empty, playerUuid);
                                        handler.OnPlayerJoin(player);
                                    }
                                    else
                                    {
                                        player = playerGet;
                                    }
                                }

                                if ((actionBitset & (1 << 1)) > 0) // Actions bit 1: initialize chat
                                {
                                    bool hasSignatureData = dataTypes.ReadNextBool(packetData);
                                    if (hasSignatureData)
                                    {
                                        Guid chatUuid = dataTypes.ReadNextUUID(packetData);
                                        long publicKeyExpiryTime = dataTypes.ReadNextLong(packetData);
                                        byte[] encodedPublicKey = dataTypes.ReadNextByteArray(packetData);
                                        byte[] publicKeySignature = dataTypes.ReadNextByteArray(packetData);
                                        player.SetPublicKey(chatUuid, publicKeyExpiryTime, encodedPublicKey,
                                            publicKeySignature);

                                        if (playerUuid == handler.GetUserUuid())
                                        {
                                            this.chatUuid = chatUuid;
                                        }
                                    }
                                    else
                                    {
                                        player.ClearPublicKey();
                                    }

                                    if (playerUuid == handler.GetUserUuid())
                                    {
                                        receivePlayerInfo = true;
                                        if (receiveDeclareCommands)
                                            handler.SetCanSendMessage(true);
                                    }
                                }

                                if ((actionBitset & 1 << 2) > 0) // Actions bit 2: update gamemode
                                {
                                    handler.OnGamemodeUpdate(playerUuid, dataTypes.ReadNextVarInt(packetData));
                                }

                                if ((actionBitset & (1 << 3)) > 0) // Actions bit 3: update listed
                                {
                                    player.Listed = dataTypes.ReadNextBool(packetData);
                                }

                                if ((actionBitset & (1 << 4)) > 0) // Actions bit 4: update latency
                                {
                                    int latency = dataTypes.ReadNextVarInt(packetData);
                                    handler.OnLatencyUpdate(playerUuid, latency); //Update latency;
                                }

                                if ((actionBitset & (1 << 5)) > 0) // Actions bit 5: update display name
                                {
                                    if (dataTypes.ReadNextBool(packetData))
                                        player.DisplayName = dataTypes.ReadNextString(packetData);
                                    else
                                        player.DisplayName = null;
                                }
                            }
                        }
                        else // 1.8 - 1.19.2
                        {
                            int action = dataTypes.ReadNextVarInt(packetData); // Action Name
                            int numberOfPlayers = dataTypes.ReadNextVarInt(packetData); // Number Of Players 

                            for (int i = 0; i < numberOfPlayers; i++)
                            {
                                Guid uuid = dataTypes.ReadNextUUID(packetData); // Player UUID

                                switch (action)
                                {
                                    case 0x00: //Player Join (Add player since 1.19)
                                        string name = dataTypes.ReadNextString(packetData); // Player name
                                        int propNum =
                                            dataTypes.ReadNextVarInt(
                                                packetData); // Number of properties in the following array

                                        // Property: Tuple<Name, Value, Signature(empty if there is no signature)
                                        // The Property field looks as in the response of https://wiki.vg/Mojang_API#UUID_to_Profile_and_Skin.2FCape
                                        const bool useProperty = false;
#pragma warning disable CS0162 // Unreachable code detected
                                        Tuple<string, string, string?>[]? properties =
                                            useProperty ? new Tuple<string, string, string?>[propNum] : null;
                                        for (int p = 0; p < propNum; p++)
                                        {
                                            string propertyName =
                                                dataTypes.ReadNextString(packetData); // Name: String (32767)
                                            string val =
                                                dataTypes.ReadNextString(packetData); // Value: String (32767)
                                            string? propertySignature = null;
                                            if (dataTypes.ReadNextBool(packetData)) // Is Signed
                                                propertySignature =
                                                    dataTypes.ReadNextString(
                                                        packetData); // Signature: String (32767)
                                            if (useProperty)
                                                properties![p] = new(propertyName, val, propertySignature);
                                        }
#pragma warning restore CS0162 // Unreachable code detected

                                        int gameMode = dataTypes.ReadNextVarInt(packetData); // Gamemode
                                        handler.OnGamemodeUpdate(uuid, gameMode);

                                        int ping = dataTypes.ReadNextVarInt(packetData); // Ping

                                        string? displayName = null;
                                        if (dataTypes.ReadNextBool(packetData)) // Has display name
                                            displayName = dataTypes.ReadNextString(packetData); // Display name

                                        // 1.19 Additions
                                        long? keyExpiration = null;
                                        byte[]? publicKey = null, signature = null;
                                        if (protocolVersion >= MC_1_19_Version)
                                        {
                                            if (dataTypes.ReadNextBool(
                                                    packetData)) // Has Sig Data (if true, red the following fields)
                                            {
                                                keyExpiration = dataTypes.ReadNextLong(packetData); // Timestamp

                                                int publicKeyLength =
                                                    dataTypes.ReadNextVarInt(packetData); // Public Key Length 
                                                if (publicKeyLength > 0)
                                                    publicKey = dataTypes.ReadData(publicKeyLength,
                                                        packetData); // Public key

                                                int signatureLength =
                                                    dataTypes.ReadNextVarInt(packetData); // Signature Length 
                                                if (signatureLength > 0)
                                                    signature = dataTypes.ReadData(signatureLength,
                                                        packetData); // Public key
                                            }
                                        }

                                        handler.OnPlayerJoin(new PlayerInfo(uuid, name, properties, gameMode, ping,
                                            displayName, keyExpiration, publicKey, signature));
                                        break;
                                    case 0x01: //Update gamemode
                                        handler.OnGamemodeUpdate(uuid, dataTypes.ReadNextVarInt(packetData));
                                        break;
                                    case 0x02: //Update latency
                                        int latency = dataTypes.ReadNextVarInt(packetData);
                                        handler.OnLatencyUpdate(uuid, latency); //Update latency;
                                        break;
                                    case 0x03: //Update display name
                                        if (dataTypes.ReadNextBool(packetData))
                                        {
                                            PlayerInfo? player = handler.GetPlayerInfo(uuid);
                                            if (player != null)
                                                player.DisplayName = dataTypes.ReadNextString(packetData);
                                            else
                                                dataTypes.SkipNextString(packetData);
                                        }

                                        break;
                                    case 0x04: //Player Leave
                                        handler.OnPlayerLeave(uuid);
                                        break;
                                    default:
                                        //Unknown player list item type
                                        break;
                                }
                            }
                        }
                        break;
                    case PacketTypesIn.PlayerRemove:
                        int numberOfLeavePlayers = dataTypes.ReadNextVarInt(packetData);
                        for (int i = 0; i < numberOfLeavePlayers; ++i)
                        {
                            Guid playerUuid = dataTypes.ReadNextUUID(packetData);
                            handler.OnPlayerLeave(playerUuid);
                        }

                        break;
                    case PacketTypesIn.TabComplete:
                        {   // MC 1.13 or greater
                            autoCompleteTransactionId = dataTypes.ReadNextVarInt(packetData);
                            int completionStart  = dataTypes.ReadNextVarInt(packetData); // Start of text to replace
                            int completionLength = dataTypes.ReadNextVarInt(packetData); // Length of text to replace

                            int resultCount = dataTypes.ReadNextVarInt(packetData);
                            var completeResults = new List<string>();

                            for (int i = 0; i < resultCount; i++)
                            {
                                completeResults.Add(dataTypes.ReadNextString(packetData));
                                // MC 1.13+ Skip optional tooltip for each tab-complete result
                                if (dataTypes.ReadNextBool(packetData))
                                    dataTypes.SkipNextString(packetData);
                            }
                            if (completeResults.Count > 0)
                            {
                                EventManager.Instance.BroadcastOnUnityThread<AutoCompletionEvent>(
                                        new(completionStart, completionLength, completeResults.ToArray()));
                            }
                            else
                            {
                                EventManager.Instance.BroadcastOnUnityThread(AutoCompletionEvent.EMPTY);
                            }
                            break;
                        }
                    case PacketTypesIn.PluginMessage:
                        {
                            string channel = dataTypes.ReadNextString(packetData);
                            // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                            //handler.OnPluginChannelMessage(channel, packetData.ToArray());
                            return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                        }
                    case PacketTypesIn.Disconnect:
                        handler.OnConnectionLost(DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                        return false;
                    case PacketTypesIn.OpenWindow:
                        {   // MC 1.14 or greater
                            int windowId = dataTypes.ReadNextVarInt(packetData);
                            int windowType = dataTypes.ReadNextVarInt(packetData);
                            string title = dataTypes.ReadNextString(packetData);
                            var inventory = new Container(windowId, windowType, ChatParser.ParseText(title));
                            handler.OnInventoryOpen(windowId, inventory);
                        }
                        break;
                    case PacketTypesIn.CloseWindow:
                        {
                            byte windowId = dataTypes.ReadNextByte(packetData);
                            lock (windowActions) { windowActions[windowId] = 0; }
                            handler.OnInventoryClose(windowId);
                            break;
                        }
                    case PacketTypesIn.WindowItems:
                        {
                            byte windowId = dataTypes.ReadNextByte(packetData);
                            int stateId = -1;
                            int elements = 0;

                            if (protocolVersion >= MC_1_17_1_Version)
                            {
                                // State ID and Elements as VarInt - 1.17.1 and above
                                stateId = dataTypes.ReadNextVarInt(packetData);
                                elements = dataTypes.ReadNextVarInt(packetData);
                            }
                            else
                            {
                                // Elements as Short - 1.17 and below
                                dataTypes.ReadNextShort(packetData);
                            }

                            var inventorySlots = new Dictionary<int, ItemStack>();
                            for (int slotId = 0; slotId < elements; slotId++)
                            {
                                ItemStack? item1 = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                                if (item1 is not null)
                                    inventorySlots[slotId] = item1;
                            }

                            if (protocolVersion >= MC_1_17_1_Version) // Carried Item - 1.17.1 and above
                                dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);

                            handler.OnWindowItems(windowId, inventorySlots, stateId);
                            break;
                        }
                    case PacketTypesIn.SetSlot:
                        {
                            byte windowId = dataTypes.ReadNextByte(packetData);
                            int stateId = -1;
                            if (protocolVersion >= MC_1_17_1_Version)
                                stateId = dataTypes.ReadNextVarInt(packetData); // State ID - 1.17.1 and above
                            short slotId2 = dataTypes.ReadNextShort(packetData);
                            ItemStack? item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                            handler.OnSetSlot(windowId, slotId2, item!, stateId);
                            break;
                        }
                    case PacketTypesIn.WindowConfirmation:
                        {
                            byte windowId = dataTypes.ReadNextByte(packetData);
                            short actionId = dataTypes.ReadNextShort(packetData);
                            bool accepted = dataTypes.ReadNextBool(packetData);
                            if (!accepted)
                            {
                                SendWindowConfirmation(windowId, actionId, accepted);
                            }
                            break;
                        }
                    case PacketTypesIn.ResourcePackSend:
                        {
                            string url = dataTypes.ReadNextString(packetData);
                            string hash = dataTypes.ReadNextString(packetData);
                            bool forced = true; // Assume forced for MC 1.16 and below
                            if (protocolVersion >= MC_1_17_Version)
                            {
                                forced = dataTypes.ReadNextBool(packetData);
                                bool hasPromptMessage = dataTypes.ReadNextBool(packetData);   // Has Prompt Message (Boolean) - 1.17 and above
                                if (hasPromptMessage)
                                    dataTypes.SkipNextString(packetData); // Prompt Message (Optional Chat) - 1.17 and above
                            }
                            // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
                            if (!url.StartsWith("http") && hash.Length != 40) // Some server may have null hash value
                                break;
                            //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
                            byte[] responseHeader = new byte[0];
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, DataTypes.GetVarInt(3))); //Accepted pack
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, DataTypes.GetVarInt(0))); //Successfully loaded
                            break;
                        }
                    case PacketTypesIn.SpawnEntity:
                        {
                            Entity entity = dataTypes.ReadNextEntity(packetData, EntityPalette.INSTANCE, false);
                            handler.OnSpawnEntity(entity);
                            break;
                        }
                    case PacketTypesIn.EntityEquipment:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                bool hasNext;
                                do
                                {
                                    byte bitsData = dataTypes.ReadNextByte(packetData);
                                    //  Top bit set if another entry follows, and otherwise unset if this is the last item in the array
                                    hasNext = (bitsData >> 7) == 1 ? true : false;
                                    int slot2 = bitsData >> 1;
                                    ItemStack? item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                                    handler.OnEntityEquipment(entityId, slot2, item!);
                                } while (hasNext);
                            }
                            else
                            {
                                int slot2 = dataTypes.ReadNextVarInt(packetData);
                                ItemStack? item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                                handler.OnEntityEquipment(entityId, slot2, item!);
                            }
                            break;
                        }
                   case PacketTypesIn.SpawnLivingEntity:
                        {
                            Entity entity = dataTypes.ReadNextEntity(packetData, EntityPalette.INSTANCE, true);
                            // packet before 1.15 has metadata at the end
                            // this is not handled in dataTypes.ReadNextEntity()
                            // we are simply ignoring leftover data in packet
                            handler.OnSpawnEntity(entity);
                            break;
                        }
                    case PacketTypesIn.SpawnPlayer:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            Guid UUID = dataTypes.ReadNextUUID(packetData);
                            double x = dataTypes.ReadNextDouble(packetData);
                            double y = dataTypes.ReadNextDouble(packetData);
                            double z = dataTypes.ReadNextDouble(packetData);
                            byte yaw = dataTypes.ReadNextByte(packetData);
                            byte pitch = dataTypes.ReadNextByte(packetData);
                            Location location = new Location(x, y, z);
                            handler.OnSpawnPlayer(entityId, UUID, location, yaw, pitch);
                            break;
                        }
                    case PacketTypesIn.SpawnExperienceOrb:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            double x = dataTypes.ReadNextDouble(packetData);
                            double y = dataTypes.ReadNextDouble(packetData);
                            double z = dataTypes.ReadNextDouble(packetData);
                            dataTypes.ReadNextShort(packetData); // TODO Use this value
                            handler.OnSpawnEntity(new(entityId, EntityPalette.INSTANCE.FromId(EntityType.EXPERIENCE_ORB_ID),
                                    new(x, y, z), 0, 0, 0, 0));
                            break;
                        }
                    case PacketTypesIn.EntityEffect:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            Inventory.Effects effect = Effects.Speed;
                            if (Enum.TryParse(dataTypes.ReadNextByte(packetData).ToString(), out effect))
                            {
                                int amplifier = dataTypes.ReadNextByte(packetData);
                                int duration = dataTypes.ReadNextVarInt(packetData);
                                byte flags = dataTypes.ReadNextByte(packetData);
                                
                                bool hasFactorData = false;
                                Dictionary<string, object>? factorCodec = null;

                                if (protocolVersion >= MC_1_19_Version)
                                {
                                    hasFactorData = dataTypes.ReadNextBool(packetData);
                                    // Temp disabled to avoid crashing TODO Check how it works
                                    //factorCodec = dataTypes.ReadNextNbt(packetData);
                                }

                                handler.OnEntityEffect(entityId, effect, amplifier, duration, flags, hasFactorData, factorCodec);
                            }
                            break;
                        }
                    case PacketTypesIn.DestroyEntities:
                        {
                            int entityCount = 1; // 1.17.0 has only one entity per packet
                            if (protocolVersion != MC_1_17_Version)
                                entityCount = dataTypes.ReadNextVarInt(packetData); // All other versions have a "count" field
                            int[] entityList = new int[entityCount];
                            for (int i = 0; i < entityCount; i++)
                            {
                                entityList[i] = dataTypes.ReadNextVarInt(packetData);
                            }
                            handler.OnDestroyEntities(entityList);
                            break;
                        }
                    case PacketTypesIn.EntityPosition:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            Double deltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double deltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double deltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            bool onGround = dataTypes.ReadNextBool(packetData);
                            deltaX = deltaX / (128 * 32);
                            deltaY = deltaY / (128 * 32);
                            deltaZ = deltaZ / (128 * 32);
                            handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, onGround);
                            break;
                        }
                    case PacketTypesIn.EntityPositionAndRotation:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            Double deltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double deltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double deltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            byte yaw = dataTypes.ReadNextByte(packetData);
                            byte pitch = dataTypes.ReadNextByte(packetData);
                            bool onGround = dataTypes.ReadNextBool(packetData);
                            deltaX = deltaX / (128 * 32);
                            deltaY = deltaY / (128 * 32);
                            deltaZ = deltaZ / (128 * 32);
                            handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, onGround);
                            handler.OnEntityRotation(entityId, yaw, pitch, onGround);
                            break;
                        }
                    case PacketTypesIn.EntityRotation:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            byte yaw = dataTypes.ReadNextByte(packetData);
                            byte pitch = dataTypes.ReadNextByte(packetData);
                            bool onGround = dataTypes.ReadNextBool(packetData);
                            handler.OnEntityRotation(entityId, yaw, pitch, onGround);
                            break;
                        }
                    case PacketTypesIn.EntityHeadLook:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            byte headYaw = dataTypes.ReadNextByte(packetData);
                            handler.OnEntityHeadLook(entityId, headYaw);
                            break;
                        }
                    case PacketTypesIn.EntityProperties:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            int NumberOfProperties = protocolVersion >= MC_1_17_Version ? dataTypes.ReadNextVarInt(packetData) : dataTypes.ReadNextInt(packetData);
                            Dictionary<string, Double> keys = new Dictionary<string, Double>();
                            for (int i = 0; i < NumberOfProperties; i++)
                            {
                                string _key = dataTypes.ReadNextString(packetData);
                                Double _value = dataTypes.ReadNextDouble(packetData);

                                List<double> op0 = new List<double>();
                                List<double> op1 = new List<double>();
                                List<double> op2 = new List<double>();
                                int NumberOfModifiers = dataTypes.ReadNextVarInt(packetData);
                                for (int j = 0; j < NumberOfModifiers; j++)
                                {
                                    dataTypes.ReadNextUUID(packetData);
                                    Double amount = dataTypes.ReadNextDouble(packetData);
                                    byte operation = dataTypes.ReadNextByte(packetData);
                                    switch (operation)
                                    {
                                        case 0: op0.Add(amount); break;
                                        case 1: op1.Add(amount); break;
                                        case 2: op2.Add(amount + 1); break;
                                    }
                                }
                                if (op0.Count > 0) _value += op0.Sum();
                                if (op1.Count > 0) _value *= 1 + op1.Sum();
                                if (op2.Count > 0) _value *= op2.Aggregate((a, _x) => a * _x);
                                keys.Add(_key, _value);
                            }
                            handler.OnEntityProperties(entityId, keys);
                            break;
                        }
                    case PacketTypesIn.EntityMetadata:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            Dictionary<int, object?> metadata = dataTypes.ReadNextMetadata(packetData,
                                    ItemPalette.INSTANCE, entityMetadataPalette);

                            int healthField; // See https://wiki.vg/Entity_metadata#Living_Entity
                            if (protocolVersion > MC_1_20_Version)
                                throw new NotImplementedException(Translations.Get("exception.palette.healthfield"));
                            else if (protocolVersion >= MC_1_17_Version) // 1.17 and above
                                healthField = 9;
                            else // 1.14 and above
                                healthField = 8;

                            if (metadata.TryGetValue(healthField, out object? healthObj) && healthObj != null && healthObj.GetType() == typeof(float))
                                handler.OnEntityHealth(entityId, (float)healthObj);
                            
                            handler.OnEntityMetadata(entityId, metadata);
                        }
                        break;
                    case PacketTypesIn.EntityStatus:
                        {
                            int entityId = dataTypes.ReadNextInt(packetData);
                            byte status = dataTypes.ReadNextByte(packetData);
                            handler.OnEntityStatus(entityId, status);
                            break;
                        }
                    case PacketTypesIn.TimeUpdate:
                        {
                            long WorldAge = dataTypes.ReadNextLong(packetData);
                            long TimeOfday = dataTypes.ReadNextLong(packetData);
                            handler.OnTimeUpdate(WorldAge, TimeOfday);
                            break;
                        }
                    case PacketTypesIn.EntityTeleport:
                        {
                            int entityId = dataTypes.ReadNextVarInt(packetData);
                            Double tX = dataTypes.ReadNextDouble(packetData);
                            Double tY = dataTypes.ReadNextDouble(packetData);
                            Double tZ = dataTypes.ReadNextDouble(packetData);
                            byte yaw = dataTypes.ReadNextByte(packetData);
                            byte pitch = dataTypes.ReadNextByte(packetData);
                            bool onGround = dataTypes.ReadNextBool(packetData);
                            handler.OnEntityTeleport(entityId, tX, tY, tZ, onGround);
                            break;
                        }
                    case PacketTypesIn.UpdateHealth:
                        {
                            float health = dataTypes.ReadNextFloat(packetData);
                            int food;
                            food = dataTypes.ReadNextVarInt(packetData);
                            dataTypes.ReadNextFloat(packetData); // Food Saturation
                            handler.OnUpdateHealth(health, food);
                            break;
                        }
                    case PacketTypesIn.SetExperience:
                        {
                            float experiencebar = dataTypes.ReadNextFloat(packetData);
                            int level = dataTypes.ReadNextVarInt(packetData);
                            int totalexperience = dataTypes.ReadNextVarInt(packetData);
                            handler.OnSetExperience(experiencebar, level, totalexperience);
                            break;
                        }
                    case PacketTypesIn.Explosion:
                        {
                            Location explosionLocation = new(dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData));

                            float explosionStrength = dataTypes.ReadNextFloat(packetData);
                            int explosionBlockCount = protocolVersion >= MC_1_17_Version
                                ? dataTypes.ReadNextVarInt(packetData)
                                : dataTypes.ReadNextInt(packetData);

                            for (int i = 0; i < explosionBlockCount; i++)
                                dataTypes.ReadData(3, packetData);

                            float playerVelocityX = dataTypes.ReadNextFloat(packetData);
                            float playerVelocityY = dataTypes.ReadNextFloat(packetData);
                            float playerVelocityZ = dataTypes.ReadNextFloat(packetData);

                            handler.OnExplosion(explosionLocation, explosionStrength, explosionBlockCount);
                            break;   
                        }
                    case PacketTypesIn.HeldItemChange:
                        {
                            byte slot = dataTypes.ReadNextByte(packetData);
                            handler.OnHeldItemChange(slot);
                            break;
                        }
                    case PacketTypesIn.ScoreboardObjective:
                        {
                            string objectiveName = dataTypes.ReadNextString(packetData);
                            byte mode = dataTypes.ReadNextByte(packetData);
                            string objectiveValue = string.Empty;
                            int type2 = -1;
                            if (mode == 0 || mode == 2)
                            {
                                objectiveValue = dataTypes.ReadNextString(packetData);
                                type2 = dataTypes.ReadNextVarInt(packetData);
                            }
                            handler.OnScoreboardObjective(objectiveName, mode, objectiveValue, type2);
                            break;
                        }
                    case PacketTypesIn.UpdateScore:
                        {
                            string entityName = dataTypes.ReadNextString(packetData);
                            int action3 = protocolVersion >= MC_1_18_2_Version
                                ? dataTypes.ReadNextVarInt(packetData)
                                : dataTypes.ReadNextByte(packetData);
                            string objectivename2 = string.Empty;
                            int value = -1;
                            objectivename2 = dataTypes.ReadNextString(packetData);
                            if (action3 != 1)
                                value = dataTypes.ReadNextVarInt(packetData);
                            handler.OnUpdateScore(entityName, action3, objectivename2, value);
                            break;
                        }
                    case PacketTypesIn.BlockBreakAnimation:
                        {
                            int playerId = dataTypes.ReadNextVarInt(packetData);
                            Location blockLocation = dataTypes.ReadNextLocation(packetData);
                            byte stage = dataTypes.ReadNextByte(packetData);
                            handler.OnBlockBreakAnimation(playerId, blockLocation, stage);
                            break;
                        }
                    case PacketTypesIn.EntityAnimation:
                        {
                            int playerId = dataTypes.ReadNextVarInt(packetData);
                            byte animation = dataTypes.ReadNextByte(packetData);
                            handler.OnEntityAnimation(playerId, animation);
                            break;
                        }
                    default:
                        return false; //Ignored packet
                }
                
                //Debug.Log("[S -> C] Receiving packet " + packetId);

                return true; //Packet processed
            }
            catch (Exception innerException)
            {
                if (innerException is ThreadAbortException || innerException is SocketException || innerException.InnerException is SocketException)
                    throw; //Thread abort or Connection lost rather than invalid data
                throw new System.IO.InvalidDataException(
                    Translations.Get("exception.packet_process",
                        packetPalette.GetIncommingTypeById(packetId),
                        packetId,
                        protocolVersion,
                        login_phase,
                        innerException.GetType()),
                    innerException);
            }
        }

        /// <summary>
        /// Start the updating thread. Should be called after login success.
        /// </summary>
        private void StartUpdating()
        {
            Thread threadUpdater = new Thread(new ParameterizedThreadStart(Updater));
            threadUpdater.Name = "ProtocolPacketHandler";
            netMain = new Tuple<Thread, CancellationTokenSource>(threadUpdater, new());
            threadUpdater.Start(netMain.Item2.Token);

            Thread threadReader = new Thread(new ParameterizedThreadStart(PacketReader));
            threadReader.Name = "ProtocolPacketReader";
            netReader = new Tuple<Thread, CancellationTokenSource>(threadReader, new());
            threadReader.Start(netReader.Item2.Token);
        }

        /// <summary>
        /// Get net main thread ID
        /// </summary>
        /// <returns>Net main thread ID</returns>
        public int GetNetMainThreadId()
        {
            return netMain != null ? netMain.Item1.ManagedThreadId : -1;
        }

        /// <summary>
        /// Disconnect from the server, cancel network reading.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (netMain != null)
                {
                    netMain.Item2.Cancel();
                }

                if (netReader != null)
                {
                    netReader.Item2.Cancel();
                    socketWrapper.Disconnect();
                }
            }
            catch { }
        }

        /// <summary>
        /// Send a packet to the server. Packet Id, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(PacketTypesOut packet, IEnumerable<byte> packetData)
        {
            SendPacket(packetPalette.GetOutgoingIdByType(packet), packetData);
        }

        /// <summary>
        /// Send a packet to the server. Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetId">packet Id</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetId, IEnumerable<byte> packetData)
        {
            //Debug.Log("[C -> S] Sending packet " + packetId + " > " + dataTypes.ByteArrayToString(packetData.ToArray()));

            // The inner packet
            byte[] thePacket = dataTypes.ConcatBytes(DataTypes.GetVarInt(packetId), packetData.ToArray());

            if (compression_treshold > 0) //Compression enabled?
            {
                if (thePacket.Length >= compression_treshold) //Packet long enough for compressing?
                {
                    byte[] compressedPacket = ZlibUtils.Compress(thePacket);
                    thePacket = dataTypes.ConcatBytes(DataTypes.GetVarInt(thePacket.Length), compressedPacket);
                }
                else
                {
                    byte[] uncompressed_length = DataTypes.GetVarInt(0); //Not compressed (short packet)
                    thePacket = dataTypes.ConcatBytes(uncompressed_length, thePacket);
                }
            }

            //Debug.Log("[C -> S] Sending packet " + packetId + " > " + dataTypes.ByteArrayToString(dataTypes.ConcatBytes(dataTypes.GetVarInt(thePacket.Length), thePacket)));

            socketWrapper.SendDataRAW(dataTypes.ConcatBytes(DataTypes.GetVarInt(thePacket.Length), thePacket));
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login(PlayerKeyPair? playerKeyPair, SessionToken session, string accountLower)
        {
            byte[] protocol_version = DataTypes.GetVarInt(protocolVersion);
            string server_address = pForge.GetServerAddress(handler.GetServerHost());
            byte[] server_port = dataTypes.GetUShort((ushort)handler.GetServerPort());
            byte[] next_state = DataTypes.GetVarInt(2);
            byte[] handshake_packet = dataTypes.ConcatBytes(protocol_version, dataTypes.GetString(server_address), server_port, next_state);

            SendPacket(0x00, handshake_packet);

            List<byte> fullLoginPacket = new();
            fullLoginPacket.AddRange(dataTypes.GetString(handler.GetUsername())); // Username

            // 1.19 - 1.19.2
            if (protocolVersion >= MC_1_19_Version && protocolVersion < MC_1_19_3_Version)
            {
                if (playerKeyPair == null)
                    fullLoginPacket.AddRange(dataTypes.GetBool(false)); // Has Sig Data
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true)); // Has Sig Data
                    fullLoginPacket.AddRange(
                        DataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds())); // Expiration time
                    fullLoginPacket.AddRange(
                        dataTypes.GetArray(playerKeyPair.PublicKey.Key)); // Public key received from Microsoft API
                    if (protocolVersion >= MC_1_19_2_Version)
                        fullLoginPacket.AddRange(
                            dataTypes.GetArray(playerKeyPair.PublicKey.SignatureV2!)); // Public key signature received from Microsoft API
                    else
                        fullLoginPacket.AddRange(
                            dataTypes.GetArray(playerKeyPair.PublicKey.Signature!)); // Public key signature received from Microsoft API
                }
            }

            if (protocolVersion >= MC_1_19_2_Version)
            {
                Guid uuid = handler.GetUserUuid();

                if (uuid == Guid.Empty)
                    fullLoginPacket.AddRange(dataTypes.GetBool(false)); // Has UUID
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true)); // Has UUID
                    fullLoginPacket.AddRange(DataTypes.GetUUID(uuid)); // UUID
                }
            }

            SendPacket(0x00, fullLoginPacket);

            while (true)
            {
                (int packetId, Queue<byte> packetData) = ReadNextPacket();
                if (packetId == 0x00) // Login rejected
                {
                    handler.OnConnectionLost(DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetId == 0x01) // Encryption request
                {
                    isOnlineMode = true;
                    string serverId = dataTypes.ReadNextString(packetData);
                    byte[] serverPublicKey = dataTypes.ReadNextByteArray(packetData);
                    byte[] token = dataTypes.ReadNextByteArray(packetData);
                    return StartEncryption(accountLower, handler.GetUserUuidStr(), handler.GetSessionID(), token, serverId,
                        serverPublicKey, playerKeyPair, session);
                }
                else if (packetId == 0x02) // Login successful
                {
                    Debug.Log(Translations.Get("mcc.server_offline"));
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        Debug.LogError(Translations.Get("error.forge"));
                        return false;
                    }

                    StartUpdating();
                    return true; // No need to check session or start encryption
                }
                else HandlePacket(packetId, packetData);
            }
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>
        private bool StartEncryption(string accountLower, string uuid, string sessionId, byte[] token, string serverIdhash, byte[] serverPublicKey, PlayerKeyPair? playerKeyPair, SessionToken session)
        {
            RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey);
            byte[] secretKey = CryptoHandler.ClientAESPrivateKey ?? CryptoHandler.GenerateAESPrivateKey();

            Debug.Log(Translations.Get("debug.crypto"));

            if (serverIdhash != "-")
            {
                Debug.Log(Translations.Get("mcc.session"));

                bool needCheckSession = true;
                if (session.ServerPublicKey != null && session.SessionPreCheckTask != null
                        && serverIdhash == session.ServerIDhash &&
                        Enumerable.SequenceEqual(serverPublicKey, session.ServerPublicKey))
                {
                    session.SessionPreCheckTask.Wait();
                    if (session.SessionPreCheckTask.Result) // PreCheck Successed
                        needCheckSession = false;
                }

                if (needCheckSession)
                {
                    string serverHash = CryptoHandler.GetServerHash(serverIdhash, serverPublicKey, secretKey);

                    if (ProtocolHandler.SessionCheck(uuid, sessionId, serverHash))
                    {
                        session.ServerIDhash = serverIdhash;
                        session.ServerPublicKey = serverPublicKey;
                        SessionCache.Store(accountLower, session);
                    }
                    else
                    {
                        handler.OnConnectionLost(DisconnectReason.LoginRejected, Translations.Get("mcc.session_fail"));
                        return false;
                    }
                }
            }

            // Encryption Response packet
            List<byte> encryptionResponse = new();
            encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(secretKey, false)));     // Shared Secret
            
            // 1.19 - 1.19.2
            if (protocolVersion >= MC_1_19_Version && protocolVersion < MC_1_19_3_Version)
            {
                if (playerKeyPair is null)
                {
                    encryptionResponse.AddRange(dataTypes.GetBool(true));                              // Has Verify Token
                    encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false))); // Verify Token
                }
                else
                {
                    byte[] salt = GenerateSalt();
                    byte[] messageSignature = playerKeyPair.PrivateKey.SignData(dataTypes.ConcatBytes(token, salt));

                    encryptionResponse.AddRange(dataTypes.GetBool(false));                            // Has Verify Token
                    encryptionResponse.AddRange(salt);                                                // Salt
                    encryptionResponse.AddRange(dataTypes.GetArray(messageSignature));                // Message Signature
                }
            }
            else
            {
                encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false)));    // Verify Token
            }
            SendPacket(0x01, encryptionResponse);

            // Start client-side encryption
            socketWrapper.SwitchToEncrypted(secretKey); // pre switch

            // Process the next packet
            int loopPrevention = UInt16.MaxValue;
            while (true)
            {
                (int packetId, Queue<byte> packetData) = ReadNextPacket();
                if (packetId < 0 || loopPrevention-- < 0) // Failed to read packet or too many iterations (issue #1150)
                {
                    handler.OnConnectionLost(DisconnectReason.ConnectionLost, Translations.Get("error.invalid_encrypt"));
                    return false;
                }
                else if (packetId == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetId == 0x02) //Login successful
                {
                    Guid uuidReceived;
                    if (protocolVersion >= MC_1_16_Version)
                        uuidReceived = dataTypes.ReadNextUUID(packetData);
                    else
                        uuidReceived = Guid.Parse(dataTypes.ReadNextString(packetData));
                    string userName = dataTypes.ReadNextString(packetData);
                    Tuple<string, string, string>[]? playerProperty = null;
                    if (protocolVersion >= MC_1_19_Version)
                    {
                        int count = dataTypes.ReadNextVarInt(packetData); // Number Of Properties
                        playerProperty = new Tuple<string, string, string>[count];
                        for (int i = 0; i < count; ++i)
                        {
                            string name = dataTypes.ReadNextString(packetData);
                            string value = dataTypes.ReadNextString(packetData);
                            bool isSigned = dataTypes.ReadNextBool(packetData);
                            string signature = isSigned ? dataTypes.ReadNextString(packetData) : string.Empty;
                            playerProperty[i] = new Tuple<string, string, string>(name, value, signature);
                        }
                    }
                    handler.OnLoginSuccess(uuidReceived, userName, playerProperty);

                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        Debug.Log(Translations.Get("error.forge_encrypt"));
                        return false;
                    }

                    StartUpdating();
                    return true;
                }
                else HandlePacket(packetId, packetData);
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            socketWrapper.Disconnect();
        }

        /// <summary>
        /// Ping a Minecraft server to get information about the server
        /// </summary>
        /// <returns>True if ping was successful</returns>
        public static bool doPing(string host, int port, ref string versionName, ref int protocol, ref ForgeInfo? forgeInfo)
        {
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveTimeout = 30000; // 30 seconds
            tcp.ReceiveBufferSize = 1024 * 1024;
            SocketWrapper socketWrapper = new SocketWrapper(tcp);
            DataTypes dataTypes = new DataTypes(MC_1_15_Version);

            byte[] packetId = DataTypes.GetVarInt(0);
            byte[] protocolVersion = DataTypes.GetVarInt(-1);
            byte[] serverPort = BitConverter.GetBytes((ushort)port); Array.Reverse(serverPort);
            byte[] nextState  = DataTypes.GetVarInt(1);
            byte[] packet = dataTypes.ConcatBytes(packetId, protocolVersion, dataTypes.GetString(host), serverPort, nextState);
            byte[] tosend = dataTypes.ConcatBytes(DataTypes.GetVarInt(packet.Length), packet);

            socketWrapper.SendDataRAW(tosend);

            byte[] statusRequest = DataTypes.GetVarInt(0);
            byte[] requestPacket = dataTypes.ConcatBytes(DataTypes.GetVarInt(statusRequest.Length), statusRequest);

            socketWrapper.SendDataRAW(requestPacket);

            int packetLength = dataTypes.ReadNextVarIntRAW(socketWrapper);
            if (packetLength > 0) // Read Response length
            {
                Queue<byte> packetData = new Queue<byte>(socketWrapper.ReadDataRAW(packetLength));
                if (dataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet Id
                {
                    string result = dataTypes.ReadNextString(packetData); //Get the Json data

                    if (CornGlobal.DebugMode)
                        Debug.Log(result);

                    if (!String.IsNullOrEmpty(result) && result.StartsWith("{") && result.EndsWith("}"))
                    {
                        Json.JSONData jsonData = Json.ParseJson(result);
                        if (jsonData.Type == Json.JSONData.DataType.Object && jsonData.Properties.ContainsKey("version"))
                        {
                            Json.JSONData versionData = jsonData.Properties["version"];

                            // Retrieve display name of the Minecraft version
                            if (versionData.Properties.ContainsKey("name"))
                                versionName = versionData.Properties["name"].StringValue;

                            // Retrieve protocol version number for handling this server
                            if (versionData.Properties.ContainsKey("protocol"))
                                protocol = int.Parse(versionData.Properties["protocol"].StringValue);

                            // Check for forge on the server.
                            ProtocolForge.ServerInfoCheckForge(jsonData, ref forgeInfo);

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return 256;
        }

        /// <summary>
        /// Get the current protocol version.
        /// </summary>
        /// <remarks>
        /// Version-specific operations should be handled inside the Protocol handled whenever possible.
        /// </remarks>
        /// <returns>Minecraft Protocol version number</returns>
        public int GetProtocolVersion()
        {
            return protocolVersion;
        }

        /// <summary>
        /// Send MessageAcknowledgment packet
        /// </summary>
        /// <param name="acknowledgment">Message acknowledgment</param>
        /// <returns>True if properly sent</returns>
        public bool SendMessageAcknowledgment(LastSeenMessageList.Acknowledgment acknowledgment)
        {
            try
            {
                byte[] fields = dataTypes.GetAcknowledgment(acknowledgment,
                    isOnlineMode && CornGlobal.LoginWithSecureProfile);

                SendPacket(PacketTypesOut.MessageAcknowledgment, fields);

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Send MessageAcknowledgment packet
        /// </summary>
        /// <param name="acknowledgment">Message acknowledgment</param>
        /// <returns>True if properly sent</returns>
        public bool SendMessageAcknowledgment(int messageCount)
        {
            try
            {
                byte[] fields = DataTypes.GetVarInt(messageCount);

                SendPacket(PacketTypesOut.MessageAcknowledgment, fields);

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public LastSeenMessageList.Acknowledgment ConsumeAcknowledgment()
        {
            pendingAcknowledgments = 0;
            return new LastSeenMessageList.Acknowledgment(lastSeenMessagesCollector.GetLastSeenMessages(),
                lastReceivedMessage);
        }

        public void Acknowledge(ChatMessage message)
        {
            LastSeenMessageList.AcknowledgedMessage? entry = message.ToLastSeenMessageEntry();

            if (entry != null)
            {
                if (protocolVersion >= MC_1_19_3_Version)
                {
                    if (lastSeenMessagesCollector.Add_1_19_3(entry, true))
                    {
                        if (lastSeenMessagesCollector.messageCount > 64)
                        {
                            int messageCount = lastSeenMessagesCollector.ResetMessageCount();
                            if (messageCount > 0)
                                SendMessageAcknowledgment(messageCount);
                        }
                    }
                }
                else
                {
                    lastSeenMessagesCollector.Add_1_19_2(entry);
                    lastReceivedMessage = null;
                    if (pendingAcknowledgments++ > 64)
                        SendMessageAcknowledgment(ConsumeAcknowledgment());
                }
            }
        }

        /// <summary>
        /// Send a chat command to the server - 1.19 and above
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="playerKeyPair">PlayerKeyPair</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatCommand(string command, PlayerKeyPair? playerKeyPair)
        {
            if (String.IsNullOrEmpty(command))
                return true;

            command = Regex.Replace(command, @"\s+", " ");
            command = Regex.Replace(command, @"\s$", string.Empty);

            Debug.Log("chat command = " + command);

            try
            {
                List<Tuple<string, string>>? needSigned = null; // List< Argument Name, Argument Value >
                if (playerKeyPair != null && isOnlineMode && protocolVersion >= MC_1_19_Version
                    && CornGlobal.LoginWithSecureProfile && CornGlobal.SignMessageInCommand)
                    needSigned = DeclareCommands.CollectSignArguments(command);

                lock (MessageSigningLock)
                {
                    LastSeenMessageList.Acknowledgment? acknowledgment_1_19_2 =
                        (protocolVersion == MC_1_19_2_Version) ? ConsumeAcknowledgment() : null;

                    (LastSeenMessageList.AcknowledgedMessage[] acknowledgment_1_19_3, byte[] bitset_1_19_3,
                            int messageCount_1_19_3) =
                        (protocolVersion >= MC_1_19_3_Version)
                            ? lastSeenMessagesCollector.Collect_1_19_3()
                            : new(Array.Empty<LastSeenMessageList.AcknowledgedMessage>(), Array.Empty<byte>(), 0);

                    List<byte> fields = new();

                    // Command: String
                    fields.AddRange(dataTypes.GetString(command));

                    // Timestamp: Instant(Long)
                    DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                    fields.AddRange(DataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                    if (needSigned == null || needSigned!.Count == 0)
                    {
                        fields.AddRange(DataTypes.GetLong(0)); // Salt: Long
                        fields.AddRange(DataTypes.GetVarInt(0)); // Signature Length: VarInt
                    }
                    else
                    {
                        Guid uuid = handler.GetUserUuid();
                        byte[] salt = GenerateSalt();
                        fields.AddRange(salt); // Salt: Long
                        fields.AddRange(DataTypes.GetVarInt(needSigned.Count)); // Signature Length: VarInt
                        foreach ((string argName, string message) in needSigned)
                        {
                            fields.AddRange(dataTypes.GetString(argName)); // Argument name: String

                            byte[] sign;
                            if (protocolVersion == MC_1_19_Version)
                                sign = playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt);
                            else if (protocolVersion == MC_1_19_2_Version)
                                sign = playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt,
                                    acknowledgment_1_19_2!.lastSeen);
                            else // protocolVersion >= MC_1_19_3_Version
                                sign = playerKeyPair!.PrivateKey.SignMessage(message, uuid, chatUuid, messageIndex++,
                                    timeNow, ref salt, acknowledgment_1_19_3);

                            if (protocolVersion <= MC_1_19_2_Version)
                                fields.AddRange(DataTypes.GetVarInt(sign.Length)); // Signature length: VarInt

                            fields.AddRange(sign); // Signature: Byte Array
                        }
                    }

                    if (protocolVersion <= MC_1_19_2_Version)
                        fields.AddRange(dataTypes.GetBool(false)); // Signed Preview: Boolean

                    if (protocolVersion == MC_1_19_2_Version)
                    {
                        // Message Acknowledgment (1.19.2)
                        fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment_1_19_2!,
                            isOnlineMode && CornGlobal.LoginWithSecureProfile));
                    }
                    else if (protocolVersion >= MC_1_19_3_Version)
                    {
                        // message count
                        fields.AddRange(DataTypes.GetVarInt(messageCount_1_19_3));

                        // Acknowledged: BitSet
                        fields.AddRange(bitset_1_19_3);
                    }

                    SendPacket(PacketTypesOut.ChatCommand, fields);
                }

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="playerKeyPair">PlayerKeyPair</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair)
        {
            if (string.IsNullOrEmpty(message))
                return true;

            // Process Chat Command - 1.19 and above
            if (protocolVersion >= MC_1_19_Version && message.StartsWith('/'))
                return SendChatCommand(message[1..], playerKeyPair);

            try
            {
                List<byte> fields = new();

                // 	Message: String (up to 256 chars)
                fields.AddRange(dataTypes.GetString(message));

                if (protocolVersion >= MC_1_19_Version)
                {
                    lock (MessageSigningLock)
                    {
                        LastSeenMessageList.Acknowledgment? acknowledgment_1_19_2 =
                            (protocolVersion == MC_1_19_2_Version) ? ConsumeAcknowledgment() : null;

                        (LastSeenMessageList.AcknowledgedMessage[] acknowledgment_1_19_3, byte[] bitset_1_19_3,
                                int messageCount_1_19_3) =
                            (protocolVersion >= MC_1_19_3_Version)
                                ? lastSeenMessagesCollector.Collect_1_19_3()
                                : new(Array.Empty<LastSeenMessageList.AcknowledgedMessage>(), Array.Empty<byte>(), 0);

                        // Timestamp: Instant(Long)
                        DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                        fields.AddRange(DataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                        if (!isOnlineMode || playerKeyPair == null || !CornGlobal.LoginWithSecureProfile ||
                            !CornGlobal.SignChat)
                        {
                            fields.AddRange(DataTypes.GetLong(0)); // Salt: Long
                            if (protocolVersion < MC_1_19_3_Version)
                                fields.AddRange(DataTypes.GetVarInt(0)); // Signature Length: VarInt (1.19 - 1.19.2)
                            else
                                fields.AddRange(dataTypes.GetBool(false)); // Has signature: bool (1.19.3)
                        }
                        else
                        {
                            // Salt: Long
                            byte[] salt = GenerateSalt();
                            fields.AddRange(salt);

                            // Signature Length & Signature: (VarInt) and Byte Array
                            Guid playerUuid = handler.GetUserUuid();
                            byte[] sign;
                            if (protocolVersion == MC_1_19_Version) // 1.19.1 or lower
                                sign = playerKeyPair.PrivateKey.SignMessage(message, playerUuid, timeNow, ref salt);
                            else if (protocolVersion == MC_1_19_2_Version) // 1.19.2
                                sign = playerKeyPair.PrivateKey.SignMessage(message, playerUuid, timeNow, ref salt,
                                    acknowledgment_1_19_2!.lastSeen);
                            else // protocolVersion >= MC_1_19_3_Version
                                sign = playerKeyPair.PrivateKey.SignMessage(message, playerUuid, chatUuid,
                                    messageIndex++, timeNow, ref salt, acknowledgment_1_19_3);

                            if (protocolVersion >= MC_1_19_3_Version)
                                fields.AddRange(dataTypes.GetBool(true));
                            else
                                fields.AddRange(DataTypes.GetVarInt(sign.Length));
                            fields.AddRange(sign);
                        }

                        if (protocolVersion <= MC_1_19_2_Version)
                            fields.AddRange(dataTypes.GetBool(false)); // Signed Preview: Boolean

                        if (protocolVersion >= MC_1_19_3_Version)
                        {
                            // message count
                            fields.AddRange(DataTypes.GetVarInt(messageCount_1_19_3));

                            // Acknowledged: BitSet
                            fields.AddRange(bitset_1_19_3);
                        }
                        else if (protocolVersion == MC_1_19_2_Version)
                        {
                            // Message Acknowledgment
                            fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment_1_19_2!,
                                isOnlineMode && CornGlobal.LoginWithSecureProfile));
                        }
                    }
                }

                SendPacket(PacketTypesOut.ChatMessage, fields);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Autocomplete text while typing commands
        /// </summary>
        /// <param name="text">Text to complete</param>
        /// <returns>True if properly sent</returns>
        public bool SendAutoCompleteText(string text)
        {
            if (String.IsNullOrEmpty(text))
                return false;
            try
            {
                byte[] transactionId = DataTypes.GetVarInt(autoCompleteTransactionId);
                byte[] requestPacket = new byte[] { };

                requestPacket = dataTypes.ConcatBytes(requestPacket, transactionId);
                requestPacket = dataTypes.ConcatBytes(requestPacket, dataTypes.GetString(text));

                SendPacket(PacketTypesOut.TabComplete, requestPacket);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendEntityAction(int PlayerEntityId, int ActionId)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(DataTypes.GetVarInt(PlayerEntityId));
                fields.AddRange(DataTypes.GetVarInt(ActionId));
                fields.AddRange(DataTypes.GetVarInt(0));
                SendPacket(PacketTypesOut.EntityAction, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <returns>True if properly sent</returns>
        public bool SendRespawnPacket()
        {
            try
            {
                SendPacket(PacketTypesOut.ClientStatus, new byte[] { 0 });
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Tell the server what client is being used to connect to the server
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>
        public bool SendBrandInfo(string brandInfo)
        {
            if (String.IsNullOrEmpty(brandInfo))
                return false;
            // Plugin channels were significantly changed between Minecraft 1.12 and 1.13
            // https://wiki.vg/index.php?title=Pre-release_protocol&oldid=14132#Plugin_Channels
            return SendPluginChannelPacket("minecraft:brand", dataTypes.GetString(brandInfo));
        }

        /// <summary>
        /// Inform the server of the client's Minecraft settings
        /// </summary>
        /// <param name="language">Client language eg en_US</param>
        /// <param name="viewDistance">View distance, in chunks</param>
        /// <param name="difficulty">Game difficulty (client-side...)</param>
        /// <param name="chatMode">Chat mode (allows muting yourself)</param>
        /// <param name="chatColors">Show chat colors</param>
        /// <param name="skinParts">Show skin layers</param>
        /// <param name="mainHand">1.9+ main hand</param>
        /// <returns>True if client settings were successfully sent</returns>
        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetString(language));
                fields.Add(viewDistance);
                fields.AddRange(DataTypes.GetVarInt(chatMode));
                fields.Add(chatColors ? (byte)1 : (byte)0);
                fields.Add(skinParts);
                fields.AddRange(DataTypes.GetVarInt(mainHand));
                if (protocolVersion >= MC_1_17_Version)
                {
                    if (protocolVersion >= MC_1_18_1_Version)
                        fields.Add(0); // 1.18 and above - Enable text filtering. (Always false)
                    else
                        fields.Add(1); // 1.17 and 1.17.1 - Disable text filtering. (Always true)
                }
                if (protocolVersion >= MC_1_18_1_Version)
                    fields.Add(1); // 1.18 and above - Allow server listings
                SendPacket(PacketTypesOut.ClientSettings, fields);
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
            return false;
        }

        /// <summary>
        /// Send a location update to the server
        /// </summary>
        /// <param name="location">The new location of the player</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <param name="yaw">Optional new yaw for updating player look</param>
        /// <param name="pitch">Optional new pitch for updating player look</param>
        /// <returns>True if the location update was successfully sent</returns>
        public bool SendLocationUpdate(Location location, bool onGround, float? yaw = null, float? pitch = null)
        {
            byte[] yawpitch = new byte[0];
            PacketTypesOut packetType = PacketTypesOut.PlayerPosition;

            if (yaw.HasValue && pitch.HasValue)
            {
                yawpitch = dataTypes.ConcatBytes(dataTypes.GetFloat(yaw.Value), dataTypes.GetFloat(pitch.Value));
                packetType = PacketTypesOut.PlayerPositionAndRotation;
            }

            try
            {
                SendPacket(packetType, dataTypes.ConcatBytes(
                    dataTypes.GetDouble(location.X),
                    dataTypes.GetDouble(location.Y),
                    new byte[0],
                    dataTypes.GetDouble(location.Z),
                    yawpitch,
                    new byte[] { onGround ? (byte)1 : (byte)0 }));
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a plugin channel packet (0x17) to the server, compression and encryption will be handled automatically
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            try
            {
                SendPacket(PacketTypesOut.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), data));
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a Login Plugin Response packet (0x02)
        /// </summary>
        /// <param name="messageId">Login Plugin Request message Id </param>
        /// <param name="understood">TRUE if the request was understood</param>
        /// <param name="data">Response to the request</param>
        /// <returns>TRUE if successfully sent</returns>
        public bool SendLoginPluginResponse(int messageId, bool understood, byte[] data)
        {
            try
            {
                SendPacket(0x02, dataTypes.ConcatBytes(DataTypes.GetVarInt(messageId), dataTypes.GetBool(understood), data));
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send an Interact Entity Packet to server
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool SendInteractEntity(int entityId, int type)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(DataTypes.GetVarInt(entityId));
                fields.AddRange(DataTypes.GetVarInt(type));

                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));

                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        // TODO: Interact at block location (e.g. chest minecart)
        public bool SendInteractEntity(int entityId, int type, float X, float Y, float Z, int hand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(DataTypes.GetVarInt(entityId));
                fields.AddRange(DataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetFloat(X));
                fields.AddRange(dataTypes.GetFloat(Y));
                fields.AddRange(dataTypes.GetFloat(Z));
                fields.AddRange(DataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        
        public bool SendInteractEntity(int entityId, int type, int hand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(DataTypes.GetVarInt(entityId));
                fields.AddRange(DataTypes.GetVarInt(type));
                fields.AddRange(DataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolVersion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendInteractEntity(int entityId, int type, float X, float Y, float Z)
        {
            return false;
        }

        public bool SendUseItem(int hand, int sequenceId)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(DataTypes.GetVarInt(hand));
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(DataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.UseItem, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerDigging(int status, BlockLoc blockLoc, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(DataTypes.GetVarInt(status));
                packet.AddRange(dataTypes.GetBlockLoc(blockLoc));
                packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(DataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.PlayerDigging, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerBlockPlacement(int hand, BlockLoc location, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(DataTypes.GetVarInt(hand));
                packet.AddRange(dataTypes.GetBlockLoc(location));
                packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorX
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorY
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorZ
                packet.Add(0); // insideBlock = false;
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(DataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.PlayerBlockPlacement, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendHeldItemChange(short slot)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetShort(slot));
                SendPacket(PacketTypesOut.HeldItemChange, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, ItemStack? item, List<Tuple<short, ItemStack?>> changedSlots, int stateId)
        {
            // TODO

            return false;
        }

        public bool SendCreativeInventoryAction(int slot, Item itemType, int count, Dictionary<string, object>? nbt)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetShort((short)slot));
                packet.AddRange(dataTypes.GetItemSlot(new ItemStack(itemType, count, nbt), ItemPalette.INSTANCE));
                SendPacket(PacketTypesOut.CreativeInventoryAction, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendAnimation(int animation, int playerid)
        {
            try
            {
                if (animation == 0 || animation == 1)
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(DataTypes.GetVarInt(animation));
                    SendPacket(PacketTypesOut.Animation, packet);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendCloseWindow(int windowId)
        {
            try
            {
                lock (windowActions)
                {
                    if (windowActions.ContainsKey(windowId))
                        windowActions[windowId] = 0;
                }
                SendPacket(PacketTypesOut.CloseWindow, new[] { (byte)windowId });
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendUpdateSign(Location sign, string line1, string line2, string line3, string line4)
        {
            try
            {
                if (line1.Length > 23)
                    line1 = line1.Substring(0, 23);
                if (line2.Length > 23)
                    line2 = line1.Substring(0, 23);
                if (line3.Length > 23)
                    line3 = line1.Substring(0, 23);
                if (line4.Length > 23)
                    line4 = line1.Substring(0, 23);

                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetLocation(sign));
                packet.AddRange(dataTypes.GetString(line1));
                packet.AddRange(dataTypes.GetString(line2));
                packet.AddRange(dataTypes.GetString(line3));
                packet.AddRange(dataTypes.GetString(line4));
                SendPacket(PacketTypesOut.UpdateSign, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendWindowConfirmation(byte windowId, short actionId, bool accepted)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.Add(windowId);
                packet.AddRange(dataTypes.GetShort(actionId));
                packet.Add(accepted ? (byte)1 : (byte)0);
                SendPacket(PacketTypesOut.WindowConfirmation, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SelectTrade(int selectedSlot)
        {
            try // MC 1.13+
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(DataTypes.GetVarInt(selectedSlot));
                SendPacket(PacketTypesOut.SelectTrade, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendSpectate(Guid UUID)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(DataTypes.GetUUID(UUID));
                SendPacket(PacketTypesOut.Spectate, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[8];
            randomGen.GetNonZeroBytes(salt);
            return salt;
        }
    }
}
