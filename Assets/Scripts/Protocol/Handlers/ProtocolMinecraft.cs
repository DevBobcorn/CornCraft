using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Keys;
using System.Text.RegularExpressions;
using UnityEngine;

using MinecraftClient.Crypto;
using MinecraftClient.Event;
using MinecraftClient.Proxy;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockStatePalettes;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.PacketPalettes;
using MinecraftClient.Protocol.Handlers.Forge;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.13+ Protocols
    /// </summary>
    /// <remarks>
    /// Typical update steps for implementing protocol changes for a new Minecraft version:
    ///  - Perform a diff between latest supported version in MCC and new stable version to support on https://wiki.vg/Protocol
    ///  - If there are any changes in packets implemented by MCC, add MCXXXVersion field below and implement new packet layouts
    ///  - Add the packet type palette for that Minecraft version. Please see PacketTypePalette.cs for more information
    ///  - Also see Material.cs and ItemType.cs for updating block and item data inside MCC
    /// </remarks>
    class ProtocolMinecraft : IMinecraftCom
    {
        internal const int MC_1_13_Version = 393;
        internal const int MC_1_14_Version = 477;
        internal const int MC_1_15_Version = 573;
        internal const int MC_1_15_2_Version = 578;
        internal const int MC_1_16_Version = 735;
        internal const int MC_1_16_1_Version = 736;
        internal const int MC_1_16_2_Version = 751;
        internal const int MC_1_16_3_Version = 753;
        internal const int MC_1_16_5_Version = 754;
        internal const int MC_1_17_Version = 755;
        internal const int MC_1_17_1_Version = 756;
        internal const int MC_1_18_1_Version = 757;
        internal const int MC_1_18_2_Version = 758;
        internal const int MC_1_19_Version = 759;

        private int compression_treshold = 0;
        private int autocompleteTransactionId = 0;
        private readonly Dictionary<int, short> windowActions = new Dictionary<int, short>();
        private bool login_phase = true;
        private int protocolversion;
        private int currentDimension;

        ProtocolForge pForge;
        ProtocolTerrain pTerrain;
        IMinecraftComHandler handler;
        EntityPalette entityPalette;
        ItemPalette itemPalette;
        PacketTypePalette packetPalette;
        SocketWrapper socketWrapper;
        DataTypes dataTypes;
        #nullable enable
        Tuple<Thread, CancellationTokenSource>? netRead = null; // main thread
        #nullable disable
        RandomNumberGenerator randomGen;

        public ProtocolMinecraft(TcpClient Client, int protocolVersion, IMinecraftComHandler handler, ForgeInfo forgeInfo)
        {
            ChatParser.InitTranslations();
            this.socketWrapper = new SocketWrapper(Client);
            this.dataTypes = new DataTypes(protocolVersion);
            this.protocolversion = protocolVersion;
            this.handler = handler;
            this.pForge = new ProtocolForge(forgeInfo, protocolVersion, dataTypes, this, handler);
            this.pTerrain = new ProtocolTerrain(protocolVersion, dataTypes, handler);
            this.packetPalette = new PacketTypeHandler(protocolVersion, forgeInfo != null).GetTypeHandler();

            // Entity palette
            if (protocolversion > MC_1_18_2_Version)
                throw new NotImplementedException(Translations.Get("exception.palette.entity"));
            if (protocolversion >= MC_1_17_Version)
                entityPalette = new EntityPalette117();
            else if (protocolversion >= MC_1_16_2_Version)
                entityPalette = new EntityPalette1162();
            else if (protocolversion >= MC_1_16_Version)
                entityPalette = new EntityPalette1161();
            else if (protocolversion >= MC_1_15_Version)
                entityPalette = new EntityPalette115();
            else if (protocolVersion >= MC_1_14_Version)
                entityPalette = new EntityPalette114();
            else entityPalette = new EntityPalette113();

            // Item palette
            if (protocolversion >= MC_1_16_2_Version)
            {
                if (protocolversion > MC_1_18_2_Version)
                    throw new NotImplementedException(Translations.Get("exception.palette.item"));
                if (protocolversion >= MC_1_18_1_Version)
                    itemPalette = new ItemPalette118();
                else if (protocolversion >= MC_1_17_Version)
                    itemPalette = new ItemPalette117();
                else if (protocolversion >= MC_1_16_2_Version)
                    itemPalette = new ItemPalette1162();
                else itemPalette = new ItemPalette1161();
            }
            else itemPalette = new ItemPalette115();
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        #nullable enable
        private void Updater(object? o)
        {
            Debug.Log("Netread updater start");
            if (((CancellationToken) o!).IsCancellationRequested)
                return;

            try
            {
                bool keepUpdating = true;
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                while (keepUpdating)
                {
                    ((CancellationToken)o!).ThrowIfCancellationRequested();
                    stopWatch.Start();
                    keepUpdating = Update();
                    stopWatch.Stop();
                    int elapsed = stopWatch.Elapsed.Milliseconds;
                    stopWatch.Reset();
                    // MODIFIED Changed to 50ms, making it run at 20tps
                    if (elapsed < 50)
                        Thread.Sleep(50 - elapsed);
                }
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }

            if (((CancellationToken) o!).IsCancellationRequested)
            {   // Normally disconnected
                Loom.QueueOnMainThread(
                    () => handler.OnConnectionLost(DisconnectReason.ConnectionLost, "")
                );
                return;
            }

        }
        #nullable disable

        /// <summary>
        /// Read data from the network. Should be called on a separate thread.
        /// </summary>
        /// <returns>FALSE if an error occured, TRUE otherwise.</returns>
        private bool Update()
        {
            handler.OnUpdate();
            if (!socketWrapper.IsConnected())
                return false;
            try
            {
                while (socketWrapper.HasDataAvailable())
                {
                    int packetId = 0;
                    Queue<byte> packetData = new Queue<byte>();
                    ReadNextPacket(ref packetId, packetData);
                    HandlePacket(packetId, new Queue<byte>(packetData));
                }
            }
            catch (System.IO.IOException) { return false; }
            catch (SocketException) { return false; }
            catch (NullReferenceException) { return false; }
            catch (Ionic.Zlib.ZlibException) { return false; }
            return true;
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetId">will contain packet Id</param>
        /// <param name="packetData">will contain raw packet Data</param>
        internal void ReadNextPacket(ref int packetId, Queue<byte> packetData)
        {
            packetData.Clear();
            int size = dataTypes.ReadNextVarIntRAW(socketWrapper); //Packet size
            byte[] rawpacket = socketWrapper.ReadDataRAW(size); //Packet contents
            for (int i = 0; i < rawpacket.Length; i++)
                packetData.Enqueue(rawpacket[i]);

            //Handle packet decompression
            if (compression_treshold > 0)
            {
                int sizeUncompressed = dataTypes.ReadNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData.Clear();
                    for (int i = 0; i < uncompressed.Length; i++)
                        packetData.Enqueue(uncompressed[i]);
                }
            }

            packetId = dataTypes.ReadNextVarInt(packetData); // Packet Id
        }

        #nullable enable
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
                        string motd = "-";
                        bool hasMotd = dataTypes.ReadNextBool(packetData);
                        if (hasMotd)
                            motd = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                        string iconBase64 = "-";
                        bool hasIcon = dataTypes.ReadNextBool(packetData);
                        if (hasIcon)
                            iconBase64 = dataTypes.ReadNextString(packetData);

                        bool previewsChat = dataTypes.ReadNextBool(packetData);

                        handler.OnServerDataReceived(hasMotd, motd, hasIcon, iconBase64, previewsChat);
                        break;
                    case PacketTypesIn.KeepAlive:
                        SendPacket(PacketTypesOut.KeepAlive, packetData);
                        handler.OnServerKeepAlive();
                        break;
                    case PacketTypesIn.JoinGame:
                        handler.OnGameJoined();
                        int playerEntityId = dataTypes.ReadNextInt(packetData);
                        handler.OnReceivePlayerEntityID(playerEntityId);

                        if (protocolversion >= MC_1_16_2_Version)
                            dataTypes.ReadNextBool(packetData);                       // Is hardcore - 1.16.2 and above

                        handler.OnGamemodeUpdate(Guid.Empty, dataTypes.ReadNextByte(packetData));

                        if (protocolversion >= MC_1_16_Version)
                        {
                            dataTypes.ReadNextByte(packetData);                       // Previous Gamemode - 1.16 and above
                            int worldCount = dataTypes.ReadNextVarInt(packetData);    // Dimension Count (World Count) - 1.16 and above
                            for (int i = 0; i < worldCount; i++)
                                dataTypes.ReadNextString(packetData);                 // Dimension Names (World Names) - 1.16 and above
                            dataTypes.ReadNextNbt(packetData);                        // Registry Codec (Dimension Codec) - 1.16 and above
                        }

                        string? currentDimensionName = null;
                        Dictionary<string, object>? currentDimensionType = null;

                        // Current dimension
                        //   NBT Tag Compound: 1.16.2 and above
                        //   String identifier: 1.16 and 1.16.1
                        //   varInt: [1.9.1 to 1.15.2]
                        //   byte: below 1.9.1
                        if (protocolversion >= MC_1_16_Version)
                        {
                            if (protocolversion >= MC_1_19_Version)
                            {
                                dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                currentDimensionType = new Dictionary<string, object>();
                            }
                            else if (protocolversion >= MC_1_16_2_Version)
                                currentDimensionType = dataTypes.ReadNextNbt(packetData); // Dimension Type: NBT Tag Compound
                            else
                                dataTypes.ReadNextString(packetData);
                            this.currentDimension = 0;
                        }
                        else
                            this.currentDimension = dataTypes.ReadNextInt(packetData);

                        if (protocolversion < MC_1_14_Version)
                            dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                        
                        if (protocolversion >= MC_1_16_Version)
                            currentDimensionName = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above
                        
                        if (protocolversion >= MC_1_16_2_Version)
                            World.SetDimension(currentDimensionName, currentDimensionType);
                        
                        if (protocolversion >= MC_1_15_Version)
                            dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above

                        if (protocolversion >= MC_1_16_2_Version)
                            dataTypes.ReadNextVarInt(packetData);         // Max Players - 1.16.2 and above
                        else
                            dataTypes.ReadNextByte(packetData);           // Max Players - 1.16.1 and below

                        if (protocolversion < MC_1_16_Version)
                            dataTypes.ReadNextString(packetData);         // Level Type - 1.15 and below
                        if (protocolversion >= MC_1_14_Version)
                            dataTypes.ReadNextVarInt(packetData);         // View distance - 1.14 and above
                        if (protocolversion >= MC_1_18_1_Version)
                            dataTypes.ReadNextVarInt(packetData);         // Simulation Distance - 1.18 and above
                        
                        dataTypes.ReadNextBool(packetData);           // Reduced debug info - 1.8 and above

                        if (protocolversion >= MC_1_15_Version)
                            dataTypes.ReadNextBool(packetData);           // Enable respawn screen - 1.15 and above

                        if (protocolversion >= MC_1_16_Version)
                        {
                            dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                            dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                        }
                        if (protocolversion >= MC_1_19_Version)
                        {
                            bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                            if (hasDeathLocation)
                            {
                                dataTypes.ReadNextString(packetData);     // Death dimension name: Identifier
                                dataTypes.ReadNextLocation(packetData);   // Death location
                            }
                        }
                        break;
                    case PacketTypesIn.ChatMessage:
                        int messageType = 0;

                        if (protocolversion <= MC_1_18_2_Version) // 1.18 and below
                        {
                            string message = dataTypes.ReadNextString(packetData);

                            Guid senderUUID;
                            // Hide system messages or xp bar messages?
                            messageType = dataTypes.ReadNextByte(packetData);
                            if ((messageType == 1 && !CornCraft.DisplaySystemMessages)
                                || (messageType == 2 && !CornCraft.DisplayXPBarMessages))
                                break;
                            
                            if (protocolversion >= MC_1_16_5_Version)
                                senderUUID = dataTypes.ReadNextUUID(packetData);
                            else senderUUID = Guid.Empty;

                            handler.OnTextReceived(new(message, true, messageType, senderUUID));
                        }
                        else // 1.19+
                        {
                            string signedChat = dataTypes.ReadNextString(packetData);

                            bool hasUnsignedChatContent = dataTypes.ReadNextBool(packetData);
                            string? unsignedChatContent = hasUnsignedChatContent ? dataTypes.ReadNextString(packetData) : null;

                            messageType = dataTypes.ReadNextVarInt(packetData);
                            if ((messageType == 1 && !CornCraft.DisplaySystemMessages)
                                    || (messageType == 2 && !CornCraft.DisplayXPBarMessages))
                                break;

                            Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                            string senderDisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                            bool hasSenderTeamName = dataTypes.ReadNextBool(packetData);
                            string? senderTeamName = hasSenderTeamName ? ChatParser.ParseText(dataTypes.ReadNextString(packetData)) : null;

                            long timestamp = dataTypes.ReadNextLong(packetData);

                            long salt = dataTypes.ReadNextLong(packetData);

                            byte[] messageSignature = dataTypes.ReadNextByteArray(packetData);

                            PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                            bool verifyResult = player == null ? false : player.VerifyMessage(signedChat, senderUUID, timestamp, salt, ref messageSignature);

                            handler.OnTextReceived(new(signedChat, true, messageType, senderUUID, unsignedChatContent, senderDisplayName, senderTeamName, timestamp, verifyResult));
                        }
                        break;
                    case PacketTypesIn.Respawn:
                        string? dimensionNameInRespawn = null;
                        Dictionary<string, object>? dimensionTypeInRespawn = null;
                        if (protocolversion >= MC_1_16_Version)
                        {
                            if (protocolversion >= MC_1_19_Version)
                            {
                                dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                dimensionTypeInRespawn = new Dictionary<string, object>();
                            }
                            else if (protocolversion >= MC_1_16_2_Version)
                                dimensionTypeInRespawn = dataTypes.ReadNextNbt(packetData); // Dimension Type: NBT Tag Compound
                            else
                                dataTypes.ReadNextString(packetData);
                            this.currentDimension = 0;
                        }
                        else
                        {
                            // 1.15 and below
                            this.currentDimension = dataTypes.ReadNextInt(packetData);
                        }
                        if (protocolversion >= MC_1_16_Version)
                            dimensionNameInRespawn = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above

                        if (protocolversion >= MC_1_16_2_Version)
                            World.SetDimension(dimensionNameInRespawn, dimensionTypeInRespawn);

                        if (protocolversion < MC_1_14_Version)
                            dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                        if (protocolversion >= MC_1_15_Version)
                            dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above
                        dataTypes.ReadNextByte(packetData);               // Gamemode
                        if (protocolversion >= MC_1_16_Version)
                            dataTypes.ReadNextByte(packetData);           // Previous Game mode - 1.16 and above
                        if (protocolversion < MC_1_16_Version)
                            dataTypes.ReadNextString(packetData);         // Level Type - 1.15 and below
                        if (protocolversion >= MC_1_16_Version)
                        {
                            dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                            dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                            dataTypes.ReadNextBool(packetData);           // Copy metadata - 1.16 and above
                        }
                        if (protocolversion >= MC_1_19_Version)
                        {
                            bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                            if (hasDeathLocation)
                            {
                                dataTypes.ReadNextString(packetData); // Death dimension name: Identifier
                                dataTypes.ReadNextLocation(packetData); // Death location
                            }
                        }
                        handler.OnRespawn();
                        break;
                    case PacketTypesIn.PlayerPositionAndLook:
                        // These always need to be read, since we need the field after them for teleport confirm
                        double x = dataTypes.ReadNextDouble(packetData);
                        double y = dataTypes.ReadNextDouble(packetData);
                        double z = dataTypes.ReadNextDouble(packetData);
                        float yaw = dataTypes.ReadNextFloat(packetData);
                        float pitch = dataTypes.ReadNextFloat(packetData);
                        byte locMask = dataTypes.ReadNextByte(packetData);

                        // entity handling require player pos for distance calculating
                        Location location = handler.GetCurrentLocation();
                        location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                        location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                        location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                        handler.UpdateLocation(location, yaw, pitch);

                        int teleportId = dataTypes.ReadNextVarInt(packetData);
                        // Teleport confirm packet
                        SendPacket(PacketTypesOut.TeleportConfirm, dataTypes.GetVarInt(teleportId));
                        
                        if (protocolversion >= MC_1_17_Version)
                            dataTypes.ReadNextBool(packetData); // Dismount Vehicle - 1.17 and above
                        break;
                    case PacketTypesIn.ChunkData:
                        int chunkX = dataTypes.ReadNextInt(packetData);
                        int chunkZ = dataTypes.ReadNextInt(packetData);
                        if (protocolversion >= MC_1_17_Version)
                        {
                            ulong[]? verticalStripBitmask = null;

                            if (protocolversion == MC_1_17_Version || protocolversion == MC_1_17_1_Version)
                                verticalStripBitmask = dataTypes.ReadNextULongArray(packetData); // Bit Mask Length  and  Primary Bit Mask

                            dataTypes.ReadNextNbt(packetData); // Heightmaps

                            if (protocolversion == MC_1_17_Version || protocolversion == MC_1_17_1_Version)
                            {
                                int biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length
                                for (int i = 0; i < biomesLength; i++)
                                {
                                    dataTypes.SkipNextVarInt(packetData); // Biomes
                                }
                            }

                            int dataSize = dataTypes.ReadNextVarInt(packetData); // Size

                            Interlocked.Increment(ref handler.GetWorld().chunkCnt);
                            Interlocked.Increment(ref handler.GetWorld().chunkLoadNotCompleted);
                            new Task(() =>
                            {
                                pTerrain.ProcessChunkColumnData(chunkX, chunkZ, verticalStripBitmask, packetData);
                                Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                            }).Start();
                        }
                        else
                        {
                            bool chunksContinuous = dataTypes.ReadNextBool(packetData);
                            if (protocolversion >= MC_1_16_Version && protocolversion <= MC_1_16_1_Version)
                                dataTypes.ReadNextBool(packetData); // Ignore old data - 1.16 to 1.16.1 only
                            ushort chunkMask = (ushort)dataTypes.ReadNextVarInt(packetData);

                            if (protocolversion >= MC_1_14_Version)
                                dataTypes.ReadNextNbt(packetData);  // Heightmaps - 1.14 and above
                            int biomesLength = 0;
                            if (protocolversion >= MC_1_16_2_Version)
                                if (chunksContinuous)
                                    biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length - 1.16.2 and above
                            if (protocolversion >= MC_1_15_Version && chunksContinuous)
                            {
                                if (protocolversion >= MC_1_16_2_Version)
                                {
                                    for (int i = 0; i < biomesLength; i++)
                                    {
                                        // Biomes - 1.16.2 and above
                                        // Don't use ReadNextVarInt because it cost too much time
                                        dataTypes.SkipNextVarInt(packetData);
                                    }
                                }
                                else dataTypes.ReadData(1024 * 4, packetData); // Biomes - 1.15 and above
                            }
                            int dataSize = dataTypes.ReadNextVarInt(packetData);
                            new Task(() =>
                            {
                                pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, currentDimension, packetData);
                            }).Start();
                        }
                        break;
                    case PacketTypesIn.MapData:
                        int mapid = dataTypes.ReadNextVarInt(packetData);
                        byte scale = dataTypes.ReadNextByte(packetData);
                        bool trackingposition = protocolversion >= MC_1_17_Version ? false : dataTypes.ReadNextBool(packetData);
                        bool locked = false;
                        if (protocolversion >= MC_1_14_Version)
                        {
                            locked = dataTypes.ReadNextBool(packetData);
                        }
                        if (protocolversion >= MC_1_17_Version)
                        {
                            trackingposition = dataTypes.ReadNextBool(packetData);
                        }
                        int iconcount = dataTypes.ReadNextVarInt(packetData);
                        handler.OnMapData(mapid, scale, trackingposition, locked, iconcount);
                        break;
                    case PacketTypesIn.TradeList:
                        if ((protocolversion >= MC_1_14_Version)) // MC 1.14 or greater
                        {
                            int windowId = dataTypes.ReadNextVarInt(packetData);
                            int size = dataTypes.ReadNextByte(packetData);
                            List<VillagerTrade> trades = new List<VillagerTrade>();
                            for (int tradeId = 0; tradeId < size; tradeId++)
                            {
                                VillagerTrade trade = dataTypes.ReadNextTrade(packetData, itemPalette);
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
                        int action2 = dataTypes.ReadNextVarInt(packetData);
                        string titletext = String.Empty;
                        string subtitletext = String.Empty;
                        string actionbartext = String.Empty;
                        string json = String.Empty;
                        int fadein = -1;
                        int stay = -1;
                        int fadeout = -1;
                        // MC 1.10 or greater
                        if (action2 == 0)
                        {
                            json = titletext;
                            titletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                        }
                        else if (action2 == 1)
                        {
                            json = subtitletext;
                            subtitletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                        }
                        else if (action2 == 2)
                        {
                            json = actionbartext;
                            actionbartext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                        }
                        else if (action2 == 3)
                        {
                            fadein = dataTypes.ReadNextInt(packetData);
                            stay = dataTypes.ReadNextInt(packetData);
                            fadeout = dataTypes.ReadNextInt(packetData);
                        }
                        handler.OnTitle(action2, titletext, subtitletext, actionbartext, fadein, stay, fadeout, json);

                        break;
                    case PacketTypesIn.MultiBlockChange:
                        {
                            var locs = new List<Location>();

                            if (protocolversion >= MC_1_16_2_Version)
                            {
                                long chunkSection = dataTypes.ReadNextLong(packetData);
                                int sectionX = (int)(chunkSection >> 42);
                                int sectionY = (int)((chunkSection << 44) >> 44);
                                int sectionZ = (int)((chunkSection << 22) >> 42);
                                dataTypes.ReadNextBool(packetData); // Useless boolean
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
                                    var loc1 = new Location(blockX, blockY, blockZ);
                                    handler.GetWorld().SetBlock(loc1, bloc);
                                    locs.Add((loc1));
                                }
                            }
                            else
                            {
                                int chunkX2 = dataTypes.ReadNextInt(packetData);
                                int chunkZ2 = dataTypes.ReadNextInt(packetData);
                                int recordCount = dataTypes.ReadNextVarInt(packetData);

                                for (int i = 0; i < recordCount; i++)
                                {
                                    byte locationXZ = dataTypes.ReadNextByte(packetData);
                                    int blockY = (ushort)dataTypes.ReadNextByte(packetData);
                                    ushort blockIdMeta = (ushort)dataTypes.ReadNextVarInt(packetData);

                                    int blockX = locationXZ >> 4;
                                    int blockZ = locationXZ & 0x0F;
                                    Block bloc = new Block(blockIdMeta);
                                    var loc1 = new Location(chunkX2, chunkZ2, blockX, blockY, blockZ);
                                    handler.GetWorld().SetBlock(loc1, bloc);
                                    locs.Add(loc1);
                                }
                            }
                            //Debug.Log("Blocks change: " + locs[0] + ", etc");
                            Loom.QueueOnMainThread(() => {
                                EventManager.Instance.Broadcast<BlocksUpdateEvent>(new BlocksUpdateEvent(locs));
                            });
                        }
                        break;
                    case PacketTypesIn.BlockChange:
                        var loc2 = dataTypes.ReadNextLocation(packetData);
                        handler.GetWorld().SetBlock(loc2, new Block((ushort)dataTypes.ReadNextVarInt(packetData)));
                        //Debug.Log("Block change: " + loc2);
                        Loom.QueueOnMainThread(() => {
                            EventManager.Instance.Broadcast<BlockUpdateEvent>(new BlockUpdateEvent(loc2));
                        });
                        break;
                    case PacketTypesIn.UnloadChunk:
                        int chunkX3 = dataTypes.ReadNextInt(packetData);
                        int chunkZ3 = dataTypes.ReadNextInt(packetData);

                        if (handler.GetWorld()[chunkX3, chunkZ3] != null)
                            Interlocked.Decrement(ref handler.GetWorld().chunkCnt);
                        // Warning: It is legal to include unloaded chunks in the UnloadChunk packet. Since chunks that have not been loaded are not recorded, this may result in loading chunks that should be unloaded and inaccurate statistics.
                        
                        handler.GetWorld()[chunkX3, chunkZ3] = null;

                        Loom.QueueOnMainThread(() => {
                            EventManager.Instance.Broadcast<UnloadChunkColumnEvent>(new UnloadChunkColumnEvent(chunkX3, chunkZ3));
                        });
                        break;
                    case PacketTypesIn.SetDisplayChatPreview:
                        bool previewsChatSetting = dataTypes.ReadNextBool(packetData);
                        // TODO handler.OnChatPreviewSettingUpdate(previewsChatSetting);
                        break;
                    case PacketTypesIn.ChatPreview:
                        // TODO Currently noy implemented
                        break;
                    case PacketTypesIn.PlayerInfo:
                        int action = dataTypes.ReadNextVarInt(packetData);                                      // Action Name
                        int numberOfPlayers = dataTypes.ReadNextVarInt(packetData);                             // Number Of Players 
                        for (int i = 0; i < numberOfPlayers; i++)
                        {
                            Guid uuid = dataTypes.ReadNextUUID(packetData);                                     // Player UUID

                            switch (action)
                            {
                                case 0x00: //Player Join (Add player since 1.19)
                                    string name = dataTypes.ReadNextString(packetData);                         // Player name
                                    int propNum = dataTypes.ReadNextVarInt(packetData);                         // Number of properties in the following array

                                    Tuple<string, string, string>[]? property = null; // Property: Tuple<Name, Value, Signature(empty if there is no signature)
                                    for (int p = 0; p < propNum; p++)
                                    {
                                        string key = dataTypes.ReadNextString(packetData);                      // Name
                                        string val = dataTypes.ReadNextString(packetData);                      // Value

                                        if (dataTypes.ReadNextBool(packetData))                                 // Is Signed
                                            dataTypes.ReadNextString(packetData);                               // Signature
                                    }

                                    int gameMode = dataTypes.ReadNextVarInt(packetData);                        // Gamemode
                                    handler.OnGamemodeUpdate(uuid, gameMode);

                                    int ping = dataTypes.ReadNextVarInt(packetData);                            // Ping
                                    string? displayName = null;
                                    
                                    if (dataTypes.ReadNextBool(packetData))                                     // Has display name
                                        displayName = dataTypes.ReadNextString(packetData);                     // Display name

                                    // 1.19 Additions
                                    long? keyExpiration = null;
                                    byte[]? publicKey = null, signature = null;
                                    if (protocolversion >= MC_1_19_Version)
                                    {
                                        if (dataTypes.ReadNextBool(packetData))                                 // Has Sig Data (if true, red the following fields)
                                        {
                                            keyExpiration = dataTypes.ReadNextLong(packetData);                 // Timestamp

                                            int publicKeyLength = dataTypes.ReadNextVarInt(packetData);         // Public Key Length 
                                            if (publicKeyLength > 0)
                                                publicKey = dataTypes.ReadData(publicKeyLength, packetData);    // Public key

                                            int signatureLength = dataTypes.ReadNextVarInt(packetData);         // Signature Length 
                                            if (signatureLength > 0)
                                                signature = dataTypes.ReadData(signatureLength, packetData);    // Public key
                                        }
                                    }

                                    handler.OnPlayerJoin(new PlayerInfo(uuid, name, property, gameMode, ping, displayName, keyExpiration, publicKey, signature));
                                    break;
                                case 0x01: // Update gamemode
                                    handler.OnGamemodeUpdate(uuid, dataTypes.ReadNextVarInt(packetData));
                                    break;
                                case 0x02: // Update latency
                                    int latency = dataTypes.ReadNextVarInt(packetData);
                                    handler.OnLatencyUpdate(uuid, latency); //Update latency;
                                    break;
                                case 0x03: // Update display name
                                    if (dataTypes.ReadNextBool(packetData))
                                        dataTypes.ReadNextString(packetData);
                                    break;
                                case 0x04: // Player Leave
                                    handler.OnPlayerLeave(uuid);
                                    break;
                                default:
                                    //Unknown player list item type
                                    break;
                            }
                        }
                        break;
                    case PacketTypesIn.TabComplete:
                        // MC 1.13 or greater
                        autocompleteTransactionId = dataTypes.ReadNextVarInt(packetData);
                        dataTypes.ReadNextVarInt(packetData); // Start of text to replace
                        dataTypes.ReadNextVarInt(packetData); // Length of text to replace

                        int resultCount = dataTypes.ReadNextVarInt(packetData);
                        var completeResults = new List<string>();

                        for (int i = 0; i < resultCount; i++)
                        {
                            completeResults.Add(dataTypes.ReadNextString(packetData));
                            if (protocolversion >= MC_1_13_Version)
                            {
                                // Skip optional tooltip for each tab-complete result
                                if (dataTypes.ReadNextBool(packetData))
                                    dataTypes.ReadNextString(packetData);
                            }
                        }
                        // TODO Trigger Corn events...
                        foreach (var result in completeResults)
                            Debug.Log(result);
                        break;
                    case PacketTypesIn.PluginMessage:
                        String channel = dataTypes.ReadNextString(packetData);
                        // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                        //handler.OnPluginChannelMessage(channel, packetData.ToArray());
                        return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                    case PacketTypesIn.Disconnect:
                        handler.OnConnectionLost(DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                        return false;
                    case PacketTypesIn.OpenWindow:
                        if (protocolversion < MC_1_14_Version)
                        {   // MC 1.13 or lower
                            byte windowId1 = dataTypes.ReadNextByte(packetData);
                            string type = dataTypes.ReadNextString(packetData).Replace("minecraft:", "").ToUpper();
                            ContainerTypeOld inventoryType = (ContainerTypeOld)Enum.Parse(typeof(ContainerTypeOld), type);
                            string title = dataTypes.ReadNextString(packetData);
                            byte slots = dataTypes.ReadNextByte(packetData);
                            Container inventory = new Container(windowId1, inventoryType, ChatParser.ParseText(title));
                            handler.OnInventoryOpen(windowId1, inventory);
                        }
                        else
                        {   // MC 1.14 or greater
                            int windowId1 = dataTypes.ReadNextVarInt(packetData);
                            int windowType = dataTypes.ReadNextVarInt(packetData);
                            string title = dataTypes.ReadNextString(packetData);
                            Container inventory = new Container(windowId1, windowType, ChatParser.ParseText(title));
                            handler.OnInventoryOpen(windowId1, inventory);
                        }
                        break;
                    case PacketTypesIn.CloseWindow:
                        byte windowId2 = dataTypes.ReadNextByte(packetData);
                        lock (windowActions) { windowActions[windowId2] = 0; }
                        handler.OnInventoryClose(windowId2);
                        break;
                    case PacketTypesIn.WindowItems:
                        byte windowId3 = dataTypes.ReadNextByte(packetData);
                        int stateId1 = -1;
                        int elements = 0;

                        if (protocolversion >= MC_1_17_1_Version)
                        {
                            // State ID and Elements as VarInt - 1.17.1 and above
                            stateId1 = dataTypes.ReadNextVarInt(packetData);
                            elements = dataTypes.ReadNextVarInt(packetData);
                        }
                        else
                        {
                            // Elements as Short - 1.17 and below
                            dataTypes.ReadNextShort(packetData);
                        }

                        Dictionary<int, Item> inventorySlots = new Dictionary<int, Item>();
                        for (int slotId = 0; slotId < elements; slotId++)
                        {
                            Item item1 = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                            if (item1 != null)
                                inventorySlots[slotId] = item1;
                        }

                        if (protocolversion >= MC_1_17_1_Version) // Carried Item - 1.17.1 and above
                            dataTypes.ReadNextItemSlot(packetData, itemPalette);

                        handler.OnWindowItems(windowId3, inventorySlots, stateId1);
                        break;
                    case PacketTypesIn.SetSlot:
                        byte windowId4 = dataTypes.ReadNextByte(packetData);
                        int stateId2 = -1;
                        if (protocolversion >= MC_1_17_1_Version)
                            stateId2 = dataTypes.ReadNextVarInt(packetData); // State ID - 1.17.1 and above
                        short slotId2 = dataTypes.ReadNextShort(packetData);
                        Item item2 = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                        handler.OnSetSlot(windowId4, slotId2, item2, stateId2);
                        break;
                    case PacketTypesIn.WindowConfirmation:
                        byte windowId5 = dataTypes.ReadNextByte(packetData);
                        short actionId = dataTypes.ReadNextShort(packetData);
                        bool accepted = dataTypes.ReadNextBool(packetData);
                        if (!accepted)
                        {
                            SendWindowConfirmation(windowId5, actionId, accepted);
                        }
                        break;
                    case PacketTypesIn.ResourcePackSend:
                        string url = dataTypes.ReadNextString(packetData);
                        string hash = dataTypes.ReadNextString(packetData);
                        bool forced = true; // Assume forced for MC 1.16 and below
                        if (protocolversion >= MC_1_17_Version)
                        {
                            forced = dataTypes.ReadNextBool(packetData);
                            String forcedMessage = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                        }
                        // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
                        if (!url.StartsWith("http") && hash.Length != 40) // Some server may have null hash value
                            break;
                        //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
                        byte[] responseHeader = new byte[0];
                        SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(3))); //Accepted pack
                        SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(0))); //Successfully loaded
                        break;
                    case PacketTypesIn.SpawnEntity:
                        Entity entity1 = dataTypes.ReadNextEntity(packetData, entityPalette, false);
                        handler.OnSpawnEntity(entity1);
                        break;
                    case PacketTypesIn.EntityEquipment:
                        int entityId = dataTypes.ReadNextVarInt(packetData);
                        if (protocolversion >= MC_1_16_Version)
                        {
                            bool hasNext;
                            do
                            {
                                byte bitsData = dataTypes.ReadNextByte(packetData);
                                //  Top bit set if another entry follows, and otherwise unset if this is the last item in the array
                                hasNext = (bitsData >> 7) == 1 ? true : false;
                                int slot2 = bitsData >> 1;
                                Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                handler.OnEntityEquipment(entityId, slot2, item);
                            } while (hasNext);
                        }
                        else
                        {
                            int slot2 = dataTypes.ReadNextVarInt(packetData);
                            Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                            handler.OnEntityEquipment(entityId, slot2, item);
                        }
                        break;
                   case PacketTypesIn.SpawnLivingEntity:
                        Entity entity = dataTypes.ReadNextEntity(packetData, entityPalette, true);
                        // packet before 1.15 has metadata at the end
                        // this is not handled in dataTypes.ReadNextEntity()
                        // we are simply ignoring leftover data in packet
                        handler.OnSpawnEntity(entity);
                        break;
                    case PacketTypesIn.SpawnPlayer:
                        int entityId1 = dataTypes.ReadNextVarInt(packetData);
                        Guid UUID = dataTypes.ReadNextUUID(packetData);
                        double X = dataTypes.ReadNextDouble(packetData);
                        double Y = dataTypes.ReadNextDouble(packetData);
                        double Z = dataTypes.ReadNextDouble(packetData);
                        byte Yaw = dataTypes.ReadNextByte(packetData);
                        byte Pitch = dataTypes.ReadNextByte(packetData);
                        Location EntityLocation = new Location(X, Y, Z);
                        handler.OnSpawnPlayer(entityId1, UUID, EntityLocation, Yaw, Pitch);
                        break;
                    case PacketTypesIn.EntityEffect:
                        int entityId2 = dataTypes.ReadNextVarInt(packetData);
                        Inventory.Effects effect = Effects.Speed;
                        if (Enum.TryParse(dataTypes.ReadNextByte(packetData).ToString(), out effect))
                        {
                            int amplifier = dataTypes.ReadNextByte(packetData);
                            int duration = dataTypes.ReadNextVarInt(packetData);
                            byte flags = dataTypes.ReadNextByte(packetData);
                            
                            bool hasFactorData = false;
                            Dictionary<string, object>? factorCodec = null;

                            if (protocolversion >= MC_1_19_Version)
                            {
                                hasFactorData = dataTypes.ReadNextBool(packetData);
                                factorCodec = dataTypes.ReadNextNbt(packetData);
                            }

                            handler.OnEntityEffect(entityId2, effect, amplifier, duration, flags, hasFactorData, factorCodec);
                        }
                        break;
                    case PacketTypesIn.DestroyEntities:
                        int entityCount = 1; // 1.17.0 has only one entity per packet
                        if (protocolversion != MC_1_17_Version)
                            entityCount = dataTypes.ReadNextVarInt(packetData); // All other versions have a "count" field
                        int[] entityList = new int[entityCount];
                        for (int i = 0; i < entityCount; i++)
                        {
                            entityList[i] = dataTypes.ReadNextVarInt(packetData);
                        }
                        handler.OnDestroyEntities(entityList);
                        break;
                    case PacketTypesIn.EntityPosition:
                        int entityId3 = dataTypes.ReadNextVarInt(packetData);
                        Double DeltaX1 = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        Double DeltaY1 = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        Double DeltaZ1 = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        bool OnGround1 = dataTypes.ReadNextBool(packetData);
                        DeltaX1 = DeltaX1 / (128 * 32);
                        DeltaY1 = DeltaY1 / (128 * 32);
                        DeltaZ1 = DeltaZ1 / (128 * 32);
                        handler.OnEntityPosition(entityId3, DeltaX1, DeltaY1, DeltaZ1, OnGround1);
                        break;
                    case PacketTypesIn.EntityPositionAndRotation:
                        int entityId4 = dataTypes.ReadNextVarInt(packetData);
                        Double DeltaX2 = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        Double DeltaY2 = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        Double DeltaZ2 = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                        byte _yaw1 = dataTypes.ReadNextByte(packetData);
                        byte _pitch1 = dataTypes.ReadNextByte(packetData);
                        bool OnGround2 = dataTypes.ReadNextBool(packetData);
                        DeltaX2 = DeltaX2 / (128 * 32);
                        DeltaY2 = DeltaY2 / (128 * 32);
                        DeltaZ2 = DeltaZ2 / (128 * 32);
                        handler.OnEntityPosition(entityId4, DeltaX2, DeltaY2, DeltaZ2, OnGround2);
                        handler.OnEntityRotation(entityId4, _yaw1, _pitch1, OnGround2);
                        break;
                    case PacketTypesIn.EntityRotation:
                        int entityId5 = dataTypes.ReadNextVarInt(packetData);
                        byte _yaw2 = dataTypes.ReadNextByte(packetData);
                        byte _pitch2 = dataTypes.ReadNextByte(packetData);
                        bool OnGround3 = dataTypes.ReadNextBool(packetData);
                        handler.OnEntityRotation(entityId5, _yaw2, _pitch2, OnGround3);
                        break;
                    case PacketTypesIn.EntityProperties:
                        int entityId6 = dataTypes.ReadNextVarInt(packetData);
                        int NumberOfProperties = protocolversion >= MC_1_17_Version ? dataTypes.ReadNextVarInt(packetData) : dataTypes.ReadNextInt(packetData);
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
                        handler.OnEntityProperties(entityId6, keys);
                        break;
                    case PacketTypesIn.EntityMetadata:
                        int entityId7 = dataTypes.ReadNextVarInt(packetData);
                        Dictionary<int, object> metadata = dataTypes.ReadNextMetadata(packetData, itemPalette);

                        // See https://wiki.vg/Entity_metadata#Living_Entity
                        int healthField = 7; // From 1.10 to 1.13.2
                        if (protocolversion >= MC_1_14_Version)
                            healthField = 8; // 1.14 and above
                        if (protocolversion >= MC_1_17_Version)
                            healthField = 9; // 1.17 and above
                        if (protocolversion > MC_1_18_2_Version)
                            throw new NotImplementedException(Translations.Get("exception.palette.healthfield"));

                        if (metadata.ContainsKey(healthField) && metadata[healthField] != null && metadata[healthField].GetType() == typeof(float))
                            handler.OnEntityHealth(entityId7, (float)metadata[healthField]);
                        handler.OnEntityMetadata(entityId7, metadata);
                        break;
                    case PacketTypesIn.EntityStatus:
                        int entityId8 = dataTypes.ReadNextInt(packetData);
                        byte status = dataTypes.ReadNextByte(packetData);
                        handler.OnEntityStatus(entityId8, status);
                        break;
                    case PacketTypesIn.TimeUpdate:
                        long WorldAge = dataTypes.ReadNextLong(packetData);
                        long TimeOfday = dataTypes.ReadNextLong(packetData);
                        handler.OnTimeUpdate(WorldAge, TimeOfday);
                        break;
                    case PacketTypesIn.EntityTeleport:
                        int EntityId = dataTypes.ReadNextVarInt(packetData);
                        Double tX = dataTypes.ReadNextDouble(packetData);
                        Double tY = dataTypes.ReadNextDouble(packetData);
                        Double tZ = dataTypes.ReadNextDouble(packetData);
                        byte EntityYaw = dataTypes.ReadNextByte(packetData);
                        byte EntityPitch = dataTypes.ReadNextByte(packetData);
                        bool OnGround = dataTypes.ReadNextBool(packetData);
                        handler.OnEntityTeleport(EntityId, tX, tY, tZ, OnGround);
                        break;
                    case PacketTypesIn.UpdateHealth:
                        float health = dataTypes.ReadNextFloat(packetData);
                        int food;
                        food = dataTypes.ReadNextVarInt(packetData);
                        dataTypes.ReadNextFloat(packetData); // Food Saturation
                        handler.OnUpdateHealth(health, food);
                        break;
                    case PacketTypesIn.SetExperience:
                        float experiencebar = dataTypes.ReadNextFloat(packetData);
                        int level = dataTypes.ReadNextVarInt(packetData);
                        int totalexperience = dataTypes.ReadNextVarInt(packetData);
                        handler.OnSetExperience(experiencebar, level, totalexperience);
                        break;
                    case PacketTypesIn.Explosion:
                        Location explosionLocation = new Location(dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData));
                        float explosionStrength = dataTypes.ReadNextFloat(packetData);
                        int explosionBlockCount = protocolversion >= MC_1_17_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextInt(packetData);
                        // Ignoring additional fields (records, pushback)
                        handler.OnExplosion(explosionLocation, explosionStrength, explosionBlockCount);
                        break;
                    case PacketTypesIn.HeldItemChange:
                        byte slot = dataTypes.ReadNextByte(packetData);
                        handler.OnHeldItemChange(slot);
                        break;
                    case PacketTypesIn.ScoreboardObjective:
                        string objectivename = dataTypes.ReadNextString(packetData);
                        byte mode = dataTypes.ReadNextByte(packetData);
                        string objectivevalue = String.Empty;
                        int type2 = -1;
                        if (mode == 0 || mode == 2)
                        {
                            objectivevalue = dataTypes.ReadNextString(packetData);
                            type2 = dataTypes.ReadNextVarInt(packetData);
                        }
                        handler.OnScoreboardObjective(objectivename, mode, objectivevalue, type2);
                        break;
                    case PacketTypesIn.UpdateScore:
                        string entityname = dataTypes.ReadNextString(packetData);
                        int action3 = protocolversion >= MC_1_18_2_Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextByte(packetData);
                        string objectivename2 = String.Empty;
                        int value = -1;
                        objectivename2 = dataTypes.ReadNextString(packetData);
                        if (action3 != 1)
                            value = dataTypes.ReadNextVarInt(packetData);
                        handler.OnUpdateScore(entityname, action3, objectivename2, value);
                        break;
                    case PacketTypesIn.BlockBreakAnimation:
                        int playerId1 = dataTypes.ReadNextVarInt(packetData);
                        Location blockLocation = dataTypes.ReadNextLocation(packetData);
                        byte stage = dataTypes.ReadNextByte(packetData);
                        handler.OnBlockBreakAnimation(playerId1, blockLocation, stage);
                        break;
                    case PacketTypesIn.EntityAnimation:
                        int playerId2 = dataTypes.ReadNextVarInt(packetData);
                        byte animation = dataTypes.ReadNextByte(packetData);
                        handler.OnEntityAnimation(playerId2, animation);
                        break;
                    case PacketTypesIn.ChangeGameState:
                        int changeReason = dataTypes.ReadNextByte(packetData);
                        float changeValue = dataTypes.ReadNextFloat(packetData);
                        switch (changeReason)
                        {
                            case 1: // End raining
                                handler.OnRainChange(false);
                                break;
                            case 2: // Begin raining
                                handler.OnRainChange(true);
                                break;
                        }
                        break;
                    default:
                        return false; //Ignored packet
                }
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
                        protocolversion,
                        login_phase,
                        innerException.GetType()),
                    innerException);
            }
        }
        #nullable disable

        /// <summary>
        /// Start the updating thread. Should be called after login success.
        /// </summary>
        private void StartUpdating()
        {
            netRead = new Tuple<Thread, CancellationTokenSource>(new Thread(new ParameterizedThreadStart(Updater)), new CancellationTokenSource());
            netRead.Item1.Name = "ProtocolPacketHandler";
            netRead.Item1.Start(netRead.Item2.Token);
        }

        /// <summary>
        /// Get net read thread (main thread) Id
        /// </summary>
        /// <returns>Net read thread Id</returns>
        public int GetNetReadThreadId()
        {
            return netRead != null ? netRead.Item1.ManagedThreadId : -1;
        }

        /// <summary>
        /// Disconnect from the server, cancel network reading.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (netRead != null)
                {
                    netRead.Item2.Cancel();
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
            byte[] thePacket = dataTypes.ConcatBytes(dataTypes.GetVarInt(packetId), packetData.ToArray());

            if (compression_treshold > 0) //Compression enabled?
            {
                if (thePacket.Length >= compression_treshold) //Packet long enough for compressing?
                {
                    byte[] compressedPacket = ZlibUtils.Compress(thePacket);
                    thePacket = dataTypes.ConcatBytes(dataTypes.GetVarInt(thePacket.Length), compressedPacket);
                }
                else
                {
                    byte[] uncompressed_length = dataTypes.GetVarInt(0); //Not compressed (short packet)
                    thePacket = dataTypes.ConcatBytes(uncompressed_length, thePacket);
                }
            }

            //Debug.Log("[C -> S] Sending packet " + packetId + " > " + dataTypes.ByteArrayToString(dataTypes.ConcatBytes(dataTypes.GetVarInt(thePacket.Length), thePacket)));

            socketWrapper.SendDataRAW(dataTypes.ConcatBytes(dataTypes.GetVarInt(thePacket.Length), thePacket));
        }

        private static void PrintArr(byte[] arr)
        {   // TODO Remove
            string s = "";
            foreach (byte b in arr)
                s += b.ToString() + "\t";
            Debug.Log(s);
        }

        #nullable enable
        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login(PlayerKeyPair? playerKeyPair)
        {
            byte[] protocol_version = dataTypes.GetVarInt(protocolversion);
            string server_address = pForge.GetServerAddress(handler.GetServerHost());
            byte[] server_port = dataTypes.GetUShort((ushort)handler.GetServerPort());
            byte[] next_state = dataTypes.GetVarInt(2);
            byte[] handshakePacket = dataTypes.ConcatBytes(protocol_version, dataTypes.GetString(server_address), server_port, next_state);

            SendPacket(0x00, handshakePacket);

            List<byte> fullLoginPacket = new List<byte>();
            fullLoginPacket.AddRange(dataTypes.GetString(handler.GetUsername()));                             // Username
            if (protocolversion >= MC_1_19_Version)
            {
                if (playerKeyPair is null)
                    fullLoginPacket.AddRange(dataTypes.GetBool(false));                                       // Has Sig Data
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true));                                        // Has Sig Data
                    fullLoginPacket.AddRange(dataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds()));   // Expiration time
                    fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.Key));                // Public key received from Microsoft API
                    fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.Signature));          // Public key signature received from Microsoft API
                }
            }
            SendPacket(0x00, fullLoginPacket);

            int packetId = -1;
            Queue<byte> packetData = new Queue<byte>();
            while (true)
            {
                ReadNextPacket(ref packetId, packetData);
                if (packetId == 0x00) // Login rejected
                {
                    handler.OnConnectionLost(DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetId == 0x01) // Encryption request
                {
                    string serverId = dataTypes.ReadNextString(packetData);
                    byte[] serverPublicKey = dataTypes.ReadNextByteArray(packetData);
                    byte[] token = dataTypes.ReadNextByteArray(packetData);
                    return StartEncryption(handler.GetUserUUID(), handler.GetSessionID(), token, serverId, serverPublicKey, playerKeyPair);
                }
                else if (packetId == 0x02) // Login successful
                {
                    Translations.Log("mcc.server_offline");
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        Translations.LogError("error.forge");
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
        private bool StartEncryption(string uuid, string sessionId, byte[] token, string serverIdhash, byte[] serverPublicKey, PlayerKeyPair? playerKeyPair)
        {
            System.Security.Cryptography.RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey);
            byte[] secretKey = CryptoHandler.GenerateAESPrivateKey();

            Translations.Log("debug.crypto");

            if (serverIdhash != "-")
            {
                Translations.Log("mcc.session");
                if (!ProtocolHandler.SessionCheck(uuid, sessionId, CryptoHandler.getServerHash(serverIdhash, serverPublicKey, secretKey)))
                {
                    handler.OnConnectionLost(DisconnectReason.LoginRejected, Translations.Get("mcc.session_fail"));
                    return false;
                }
            }

            // Encrypt the data
            byte[] key_enc = dataTypes.GetArray(RSAService.Encrypt(secretKey, false));
            byte[] token_enc = dataTypes.GetArray(RSAService.Encrypt(token, false));

            // Encryption Response packet
            List<byte> encryptionResponse = new();
            encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(secretKey, false)));     // Shared Secret
            if (protocolversion >= MC_1_19_Version)
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
            socketWrapper.SwitchToEncrypted(secretKey);

            // Process the next packet
            int loopPrevention = UInt16.MaxValue;
            while (true)
            {
                int packetId = -1;
                Queue<byte> packetData = new Queue<byte>();
                ReadNextPacket(ref packetId, packetData);
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
                    Guid uuidReceived = dataTypes.ReadNextUUID(packetData);
                    string userName = dataTypes.ReadNextString(packetData);
                    Tuple<string, string, string>[]? playerProperty = null;
                    if (protocolversion >= MC_1_19_Version)
                    {
                        int count = dataTypes.ReadNextVarInt(packetData); // Number Of Properties
                        playerProperty = new Tuple<string, string, string>[count];
                        for (int i = 0; i < count; ++i)
                        {
                            string name = dataTypes.ReadNextString(packetData);
                            string value = dataTypes.ReadNextString(packetData);
                            bool isSigned = dataTypes.ReadNextBool(packetData);
                            string signature = isSigned ? dataTypes.ReadNextString(packetData) : String.Empty;
                            playerProperty[i] = new Tuple<string, string, string>(name, value, signature);
                        }
                    }
                    handler.OnLoginSuccess(uuidReceived, userName, playerProperty);

                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        Translations.LogError("error.forge_encrypt");
                        return false;
                    }

                    StartUpdating();
                    return true;
                }
                else HandlePacket(packetId, packetData);
            }
        }
        #nullable disable

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
        public static bool doPing(string host, int port, ref int protocol, ref ForgeInfo forgeInfo)
        {
            string version = "";
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveTimeout = 30000; // 30 seconds
            tcp.ReceiveBufferSize = 1024 * 1024;
            SocketWrapper socketWrapper = new SocketWrapper(tcp);
            DataTypes dataTypes = new DataTypes(MC_1_13_Version);

            byte[] packetId = dataTypes.GetVarInt(0);
            byte[] protocolVersion = dataTypes.GetVarInt(-1);
            byte[] serverPort = BitConverter.GetBytes((ushort)port); Array.Reverse(serverPort);
            byte[] nextState  = dataTypes.GetVarInt(1);
            byte[] packet = dataTypes.ConcatBytes(packetId, protocolVersion, dataTypes.GetString(host), serverPort, nextState);
            byte[] tosend = dataTypes.ConcatBytes(dataTypes.GetVarInt(packet.Length), packet);

            socketWrapper.SendDataRAW(tosend);

            byte[] statusRequest = dataTypes.GetVarInt(0);
            byte[] requestPacket = dataTypes.ConcatBytes(dataTypes.GetVarInt(statusRequest.Length), statusRequest);

            socketWrapper.SendDataRAW(requestPacket);

            int packetLength = dataTypes.ReadNextVarIntRAW(socketWrapper);
            if (packetLength > 0) // Read Response length
            {
                Queue<byte> packetData = new Queue<byte>(socketWrapper.ReadDataRAW(packetLength));
                if (dataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet Id
                {
                    string result = dataTypes.ReadNextString(packetData); //Get the Json data

                    if (CornCraft.DebugMode)
                    {
                        Debug.Log(result);
                    }

                    if (!String.IsNullOrEmpty(result) && result.StartsWith("{") && result.EndsWith("}"))
                    {
                        Json.JSONData jsonData = Json.ParseJson(result);
                        if (jsonData.Type == Json.JSONData.DataType.Object && jsonData.Properties.ContainsKey("version"))
                        {
                            Json.JSONData versionData = jsonData.Properties["version"];

                            // Retrieve display name of the Minecraft version
                            if (versionData.Properties.ContainsKey("name"))
                                version = versionData.Properties["name"].StringValue;

                            // Retrieve protocol version number for handling this server
                            if (versionData.Properties.ContainsKey("protocol"))
                                protocol = int.Parse(versionData.Properties["protocol"].StringValue);

                            // Check for forge on the server.
                            ProtocolForge.ServerInfoCheckForge(jsonData, ref forgeInfo);

                            Translations.Log("mcc.server_protocol", version, protocol + (forgeInfo != null ? Translations.Get("mcc.with_forge") : ""));

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
            return protocolversion;
        }

        /// <summary>
        /// The signable argument names and their values from command
        /// Signature will used in Vanilla's say, me, msg, teammsg, ban, banip, and kick commands.
        /// https://gist.github.com/kennytv/ed783dd244ca0321bbd882c347892874#signed-command-arguments
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns> List< Argument Name, Argument Value > </returns>
        private List<Tuple<string, string>> collectCommandArguments(string command)
        {
            List<Tuple<string, string>> needSigned = new();

            if (!CornCraft.SignMessageInCommand)
                return needSigned;

            string[] argStage1 = command.Split(' ', 2, StringSplitOptions.None);
            if (argStage1.Length == 2)
            {
                /* /me      <action>
                   /say     <message>
                   /teammsg <message> */
                if (argStage1[0] == "me")
                    needSigned.Add(new("action", argStage1[1]));
                else if (argStage1[0] == "say" || argStage1[0] == "teammsg")
                    needSigned.Add(new("message", argStage1[1]));
                else if (argStage1[0] == "msg" || argStage1[0] == "ban" || argStage1[0] == "ban-ip" || argStage1[0] == "kick")
                {
                    /* /msg    <targets> <message>
                       /ban    <target>  [<reason>]
                       /ban-ip <target>  [<reason>]
                       /kick   <target>  [<reason>] */
                    string[] argStage2 = argStage1[1].Split(' ', 2, StringSplitOptions.None);
                    if (argStage2.Length == 2)
                    {
                        if (argStage1[0] == "msg")
                            needSigned.Add(new("message", argStage2[1]));
                        else if (argStage1[0] == "ban" || argStage1[0] == "ban-ip" || argStage1[0] == "kick")
                            needSigned.Add(new("reason", argStage2[1]));
                    }
                }
            }

            return needSigned;
        }

        #nullable enable
        /// <summary>
        /// Send a chat command to the server
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

            //Debug.Log("chat command = " + command);

            try
            {
                List<byte> fields = new();

                // Command: String
                fields.AddRange(dataTypes.GetString(command));

                // Timestamp: Instant(Long)
                DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                fields.AddRange(dataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                List<Tuple<string, string>> needSigned = collectCommandArguments(command); // List< Argument Name, Argument Value >
                // foreach (var msg in needSigned)
                //     log.Info("<" + msg.Item1 + ">: " + msg.Item2);
                if (needSigned.Count == 0 || playerKeyPair == null || !CornCraft.SignMessageInCommand)
                {
                    fields.AddRange(dataTypes.GetLong(0));                    // Salt: Long
                    fields.AddRange(dataTypes.GetVarInt(0));                  // Signature Length: VarInt
                }
                else
                {
                    string uuid = handler.GetUserUUID()!;
                    byte[] salt = GenerateSalt();
                    fields.AddRange(salt);                                    // Salt: Long
                    fields.AddRange(dataTypes.GetVarInt(needSigned.Count));   // Signature Length: VarInt
                    foreach (var argument in needSigned)
                    {
                        fields.AddRange(dataTypes.GetString(argument.Item1)); // Argument name: String
                        byte[] sign = playerKeyPair.PrivateKey.SignMessage(argument.Item2, uuid, timeNow, ref salt);
                        fields.AddRange(dataTypes.GetVarInt(sign.Length));    // Signature length: VarInt
                        fields.AddRange(sign);                                // Signature: Byte Array
                    }
                }

                // Signed Preview: Boolean
                fields.AddRange(dataTypes.GetBool(false));

                SendPacket(PacketTypesOut.ChatCommand, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair)
        {
            if (String.IsNullOrEmpty(message))
                return true;
            
            // Process Chat Command - 1.19 and above
            if (protocolversion >= MC_1_19_Version && message.StartsWith('/'))
                return SendChatCommand(message[1..], playerKeyPair);
            
            try
            {
                List<byte> fields = new();

                // 	Message: String (up to 256 chars)
                fields.AddRange(dataTypes.GetString(message));

                if (protocolversion >= MC_1_19_Version)
                {
                    // Timestamp: Instant(Long)
                    DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                    fields.AddRange(dataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                    if (playerKeyPair == null || !CornCraft.SignChat)
                    {
                        fields.AddRange(dataTypes.GetLong(0));   // Salt: Long
                        fields.AddRange(dataTypes.GetVarInt(0)); // Signature Length: VarInt
                    }
                    else
                    {
                        // Salt: Long
                        byte[] salt = GenerateSalt();
                        fields.AddRange(salt);

                        // Signature Length & Signature: (VarInt) and Byte Array
                        string uuid = handler.GetUserUUID()!;
                        byte[] sign = playerKeyPair.PrivateKey.SignMessage(message, uuid, timeNow, ref salt);
                        fields.AddRange(dataTypes.GetVarInt(sign.Length));
                        fields.AddRange(sign);
                    }

                    // Signed Preview: Boolean
                    fields.AddRange(dataTypes.GetBool(false));
                }
                SendPacket(PacketTypesOut.ChatMessage, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        #nullable disable

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
                byte[] transactionId = dataTypes.GetVarInt(autocompleteTransactionId);
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
                fields.AddRange(dataTypes.GetVarInt(PlayerEntityId));
                fields.AddRange(dataTypes.GetVarInt(ActionId));
                fields.AddRange(dataTypes.GetVarInt(0));
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
            if (protocolversion >= MC_1_13_Version)
            {
                return SendPluginChannelPacket("minecraft:brand", dataTypes.GetString(brandInfo));
            }
            else
            {
                return SendPluginChannelPacket("MC|Brand", dataTypes.GetString(brandInfo));
            }
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
                fields.AddRange(dataTypes.GetVarInt(chatMode));
                fields.Add(chatColors ? (byte)1 : (byte)0);
                fields.Add(skinParts);
                fields.AddRange(dataTypes.GetVarInt(mainHand));
                if (protocolversion >= MC_1_17_Version)
                {
                    if (protocolversion >= MC_1_18_1_Version)
                        fields.Add(0); // 1.18 and above - Enable text filtering. (Always false)
                    else
                        fields.Add(1); // 1.17 and 1.17.1 - Disable text filtering. (Always true)
                }
                if (protocolversion >= MC_1_18_1_Version)
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
                SendPacket(0x02, dataTypes.ConcatBytes(dataTypes.GetVarInt(messageId), dataTypes.GetBool(understood), data));
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send an Interact Entity Packet to server
        /// </summary>
        /// <param name="EntityId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool SendInteractEntity(int EntityId, int type)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetVarInt(EntityId));
                fields.AddRange(dataTypes.GetVarInt(type));

                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolversion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));

                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        // TODO: Interact at block location (e.g. chest minecart)
        public bool SendInteractEntity(int EntityId, int type, float X, float Y, float Z, int hand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetVarInt(EntityId));
                fields.AddRange(dataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetFloat(X));
                fields.AddRange(dataTypes.GetFloat(Y));
                fields.AddRange(dataTypes.GetFloat(Z));
                fields.AddRange(dataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolversion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        public bool SendInteractEntity(int EntityId, int type, int hand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetVarInt(EntityId));
                fields.AddRange(dataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolversion >= MC_1_16_Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        public bool SendInteractEntity(int EntityId, int type, float X, float Y, float Z)
        {
            return false;
        }

        public bool SendUseItem(int hand, int sequenceId)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(hand));
                if (protocolversion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.UseItem, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerDigging(int status, Location location, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(status));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                if (protocolversion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.PlayerDigging, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerBlockPlacement(int hand, Location location, Direction face, int sequenceId)
        {
            if (protocolversion < MC_1_14_Version)
                return false; // NOT IMPLEMENTED for older MC versions
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(hand));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorX
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorY
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorZ
                packet.Add(0); // insideBlock = false;
                if (protocolversion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
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

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, Item item, List<Tuple<short, Item>> changedSlots, int stateId)
        {
            try
            {
                short actionNumber;
                lock (windowActions)
                {
                    if (!windowActions.ContainsKey(windowId))
                        windowActions[windowId] = 0;
                    actionNumber = (short)(windowActions[windowId] + 1);
                    windowActions[windowId] = actionNumber;
                }

                byte button = 0;
                byte mode = 0;

                switch (action)
                {
                    case WindowActionType.LeftClick:       button = 0;  break;
                    case WindowActionType.RightClick:      button = 1;  break;
                    case WindowActionType.MiddleClick:     button = 2;  mode = 3; break;
                    case WindowActionType.ShiftClick:      button = 0;  mode = 1; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItem:        button = 0;  mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItemStack:   button = 1;  mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.StartDragLeft:   button = 0;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragRight:  button = 4;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragMiddle: button = 8;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragLeft:     button = 2;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragRight:    button = 6;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragMiddle:   button = 10; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.AddDragLeft:     button = 1;  mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragRight:    button = 5;  mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragMiddle:   button = 9;  mode = 5; item = new Item(ItemType.Null, 0, null); break;
                }

                List<byte> packet = new List<byte>();
                packet.Add((byte)windowId); // Window ID

                // 1.18+
                if (protocolversion >= MC_1_18_1_Version)
                {
                    packet.AddRange(dataTypes.GetVarInt(stateId)); // State ID
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                }
                // 1.17.1
                else if (protocolversion == MC_1_17_1_Version)
                {
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                    packet.AddRange(dataTypes.GetVarInt(stateId)); // State ID
                }
                // Older
                else
                {
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                }

                packet.Add(button); // Button

                if (protocolversion < MC_1_17_Version)
                    packet.AddRange(dataTypes.GetShort(actionNumber));

                packet.AddRange(dataTypes.GetVarInt(mode)); // MC 1.9+, Mode

                // 1.17+  Array of changed slots
                if (protocolversion >= MC_1_17_Version)
                {
                    packet.AddRange(dataTypes.GetVarInt(changedSlots.Count)); // Length of the array
                    foreach (var slot in changedSlots)
                    {
                        packet.AddRange(dataTypes.GetShort(slot.Item1)); // slot ID
                        packet.AddRange(dataTypes.GetItemSlot(slot.Item2, itemPalette)); // slot Data
                    }
                }

                packet.AddRange(dataTypes.GetItemSlot(item, itemPalette)); // Carried item (Clicked item)

                packet.AddRange(dataTypes.GetVarInt(mode));
                packet.AddRange(dataTypes.GetItemSlot(item, itemPalette));
                SendPacket(PacketTypesOut.ClickWindow, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendCreativeInventoryAction(int slot, ItemType itemType, int count, Dictionary<string, object> nbt)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetShort((short)slot));
                packet.AddRange(dataTypes.GetItemSlot(new Item(itemType, count, nbt), itemPalette));
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
                    packet.AddRange(dataTypes.GetVarInt(animation));
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
        
        public bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            if (protocolversion <= MC_1_13_Version)
            {
                try
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(dataTypes.GetLocation(location));
                    packet.AddRange(dataTypes.GetString(command));
                    packet.AddRange(dataTypes.GetVarInt((int)mode));
                    packet.Add((byte)flags);
                    SendPacket(PacketTypesOut.UpdateSign, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false;  }
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
            // MC 1.13 or greater
            if (protocolversion >= MC_1_13_Version)
            {
                try
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(dataTypes.GetVarInt(selectedSlot));
                    SendPacket(PacketTypesOut.SelectTrade, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false; }
        }

        public bool SendSpectate(Guid UUID)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetUUID(UUID));
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
