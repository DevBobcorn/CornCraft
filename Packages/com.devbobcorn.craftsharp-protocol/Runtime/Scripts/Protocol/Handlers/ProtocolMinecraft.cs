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
    public class ProtocolMinecraft : IMinecraftCom
    {

        public const int MC_1_16_2_Version = 751;
        public const int MC_1_16_3_Version = 753;
        public const int MC_1_16_5_Version = 754;
        public const int MC_1_17_Version   = 755;
        public const int MC_1_17_1_Version = 756;
        public const int MC_1_18_1_Version = 757;
        public const int MC_1_18_2_Version = 758;
        public const int MC_1_19_Version   = 759;
        public const int MC_1_19_2_Version = 760;
        public const int MC_1_19_3_Version = 761;
        public const int MC_1_19_4_Version = 762;
        public const int MC_1_20_Version   = 763;
        public const int MC_1_20_2_Version = 764;
        public const int MC_1_20_4_Version = 765;
        public const int MC_1_20_6_Version = 766;
        public const int MC_1_21_Version   = 767;

        private int compression_threshold = -1;
        private int autocomplete_transaction_id = 0;
        private readonly Dictionary<int, short> inventoryActions = new();
        private CurrentState currentState = CurrentState.Login;
        private readonly int protocolVersion;
        private int currentDimension;
        private bool isOnlineMode;
        private readonly BlockingCollection<Tuple<int, Queue<byte>>> packetQueue = new();
        private float LastYaw, LastPitch;
        private long chunkBatchStartTime;
        private double aggregatedNanosPerChunk = 2000000.0;
        private int oldSamplesWeight = 1;

        private bool receiveDeclareCommands = false, receivePlayerInfo = false;
        private readonly object MessageSigningLock = new();
        private Guid chatUUID = Guid.NewGuid();
        private int pendingAcknowledgments = 0, messageIndex = 0;
        private LastSeenMessagesCollector lastSeenMessagesCollector;
        private LastSeenMessageList.AcknowledgedMessage? lastReceivedMessage = null;

        private readonly ProtocolForge pForge;
        private readonly ProtocolTerrain pTerrain;
        private readonly IMinecraftComHandler handler;
        private readonly EntityMetadataPalette entityMetadataPalette;
        private readonly PacketTypePalette packetPalette;
        private readonly SocketWrapper socketWrapper;
        private readonly DataTypes dataTypes;
        private Tuple<Thread, CancellationTokenSource>? netMain = null; // Net main thread
        private Tuple<Thread, CancellationTokenSource>? netReader = null; // Net reader thread
        private readonly RandomNumberGenerator randomGen;

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
                var charTypeRegistry = ChatParser.MessageTypeRegistry;
                charTypeRegistry.Clear();

                charTypeRegistry.Register(new ResourceLocation("chat"), 0, ChatParser.MessageType.CHAT);
                charTypeRegistry.Register(new ResourceLocation("say_command"), 1, ChatParser.MessageType.SAY_COMMAND);
                charTypeRegistry.Register(new ResourceLocation("msg_command_incoming"), 2, ChatParser.MessageType.MSG_COMMAND_INCOMING);
                charTypeRegistry.Register(new ResourceLocation("msg_command_outgoing"), 3, ChatParser.MessageType.MSG_COMMAND_OUTGOING);
                charTypeRegistry.Register(new ResourceLocation("team_msg_command_incoming"), 4, ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING);
                charTypeRegistry.Register(new ResourceLocation("team_msg_command_outgoing"), 5, ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING);
                charTypeRegistry.Register(new ResourceLocation("emote_command"), 7, ChatParser.MessageType.EMOTE_COMMAND);
            }
            else if (protocolVersion >= MC_1_19_Version)
            {
                var charTypeRegistry = ChatParser.MessageTypeRegistry;
                charTypeRegistry.Clear();

                charTypeRegistry.Register(new ResourceLocation("chat"), 0, ChatParser.MessageType.CHAT);
                charTypeRegistry.Register(new ResourceLocation("raw_msg"), 1, ChatParser.MessageType.RAW_MSG);
                charTypeRegistry.RegisterDummy(new ResourceLocation("raw_msg"), 2, ChatParser.MessageType.RAW_MSG);
                charTypeRegistry.Register(new ResourceLocation("say_command"), 3, ChatParser.MessageType.SAY_COMMAND);
                charTypeRegistry.Register(new ResourceLocation("msg_command_incoming"), 4, ChatParser.MessageType.MSG_COMMAND_INCOMING);
                charTypeRegistry.Register(new ResourceLocation("team_msg_command_outgoing"), 5, ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING);
                charTypeRegistry.Register(new ResourceLocation("emote_command"), 5, ChatParser.MessageType.EMOTE_COMMAND);
                charTypeRegistry.RegisterDummy(new ResourceLocation("raw_msg"), 7, ChatParser.MessageType.RAW_MSG);
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

                    handler.OnHandlerUpdate(packetQueue.Count);
                    stopWatch.Restart();

                    while (packetQueue.TryTake(out Tuple<int, Queue<byte>>? packetInfo))
                    {
                        (int packetId, Queue<byte> packetData) = packetInfo;
                        HandlePacket(packetId, packetData);

                        /*
                        // Use this to figure out if there're certain types of packets that's taking too long to handle
                        // And if that is the case, consider caching them somewhere to avoid flooding our packet queue
                        if (stopWatch.ElapsedMilliseconds >= 50)
                            Debug.Log(packetPalette.GetIncommingTypeById(packetId));
                        */

                        if (stopWatch.Elapsed.Milliseconds >= 50)
                        {
                            handler.OnHandlerUpdate(packetQueue.Count);
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
        private void PacketReader(object? o)
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
        internal Tuple<int, Queue<byte>> ReadNextPacket()
        {
            int size = DataTypes.ReadNextVarIntRAW(socketWrapper); //Packet size
            Queue<byte> packetData = new(socketWrapper.ReadDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (compression_threshold >= 0)
            {
                int sizeUncompressed = DataTypes.ReadNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData = new(uncompressed);
                }
            }

            int packetId = DataTypes.ReadNextVarInt(packetData); //Packet ID

            if (ProtocolSettings.CapturePackets)
                handler.OnNetworkPacket(packetId, packetData.ToArray(), currentState, true);

            return new(packetId, packetData);
        }

        /// <summary>
        /// Read RegistryCodec as a single nbt. Used in versions lower than 1.20.5
        /// </summary>
        private static (ResourceLocation id, int numId, object? obj)[] ReadRegistryCodecArray(object[] entries)
        {
            return entries.Select(x =>
            {
                var entry = (x as Dictionary<string, object>)!;

                var id = ResourceLocation.FromString((string)entry["name"]);
                var numId = (int)entry["id"];
                object? obj = entry.TryGetValue("element", out var value) ? value : null;

                return (id, numId, obj);
            }).ToArray();
        }
        
        /// <summary>
        /// Read one single registry from a tuple array. Used in 1.20.5+
        /// </summary>
        private static (ResourceLocation id, int numId, object? obj)[] ReadSingleRegistry(Queue<byte> packetData, bool useAnonymousNBT)
        {
            var entryCount = DataTypes.ReadNextVarInt(packetData);
            var entryList = new (ResourceLocation, int, object?)[entryCount];

            for (int i = 0; i < entryCount; i++)
            {
                var entryId = ResourceLocation.FromString(DataTypes.ReadNextString(packetData));
                var entryHasObj = DataTypes.ReadNextBool(packetData);
                var entryObj = entryHasObj ? DataTypes.ReadNextNbt(packetData, useAnonymousNBT) : null;

                entryList[i] = (entryId, i, entryObj);
            }

            return entryList;
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetId">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        internal bool HandlePacket(int packetId, Queue<byte> packetData)
        {
            // This copy is necessary because by the time we get to the catch block,
            // the packetData queue will have been processed and the data will be lost
            var _copy = packetData.ToArray();

            try
            {
                switch (currentState)
                {
                    // https://wiki.vg/Protocol#Login
                    case CurrentState.Login:
                        switch (packetId)
                        {
                            // Set Compression
                            case 0x03:
                                // MC 1.8+
                                compression_threshold = DataTypes.ReadNextVarInt(packetData);
                                break;

                            // Login Plugin Request
                            case 0x04:
                                var messageId = DataTypes.ReadNextVarInt(packetData);
                                var channel = DataTypes.ReadNextString(packetData);
                                List<byte> responseData = new();
                                var understood = pForge.HandleLoginPluginRequest(channel, packetData, ref responseData);
                                SendLoginPluginResponse(messageId, understood, responseData.ToArray());
                                return understood;
                            
                            // Cookie Request
                            case 0x05:
                                var cookieName = DataTypes.ReadNextString(packetData);
                                var cookieData = null as byte[];
                                handler.GetCookie(cookieName, out cookieData);
                                SendCookieResponse(cookieName, cookieData);
                                break;

                            // Ignore other packets at this stage
                            default:
                                return true;
                        }

                        break;

                    // https://wiki.vg/Protocol#Configuration
                    case CurrentState.Configuration:
                        switch (packetPalette.GetIncomingConfigurationTypeById(packetId))
                        {
                            case ConfigurationPacketTypesIn.CookieRequest:
                                var cookieName = DataTypes.ReadNextString(packetData);
                                var cookieData = null as byte[];
                                handler.GetCookie(cookieName, out cookieData);
                                SendCookieResponse(cookieName, cookieData);
                                break;
                            
                            case ConfigurationPacketTypesIn.Disconnect:
                                handler.OnConnectionLost(DisconnectReason.InGameKick,
                                    dataTypes.ReadNextChat(packetData));
                                return false;

                            case ConfigurationPacketTypesIn.FinishConfiguration:
                                currentState = CurrentState.Play;
                                SendPacket(ConfigurationPacketTypesOut.FinishConfiguration, new List<byte>());
                                break;

                            case ConfigurationPacketTypesIn.KeepAlive:
                                SendPacket(ConfigurationPacketTypesOut.KeepAlive, packetData);
                                break;

                            case ConfigurationPacketTypesIn.Ping:
                                SendPacket(ConfigurationPacketTypesOut.Pong, packetData);
                                break;

                            case ConfigurationPacketTypesIn.RegistryData:

                                if (protocolVersion < MC_1_20_6_Version) // Different registries are wrapped in one nbt structure
                                {
                                    var registryCodec = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT).ToDictionary(
                                            x => ResourceLocation.FromString(x.Key), x => x.Value);
                                    
                                    Debug.Log($"Registry codec data: {string.Join(", ", registryCodec.Keys)}");
                                    
                                    if (registryCodec.TryGetValue(ChatParser.CHAT_TYPE_ID, out object chatTypes))
                                    {
                                        var chatTypeListNbt = ((Dictionary<string, object>) chatTypes)["value"];
                                        var chatTypeList = ReadRegistryCodecArray((object[]) chatTypeListNbt);

                                        ChatParser.ReadChatType(chatTypeList);
                                    }

                                    if (registryCodec.TryGetValue(World.DIMENSION_TYPE_ID, out object dimensionTypes))
                                    {
                                        var dimensionListNbt = ((Dictionary<string, object>) dimensionTypes)["value"];
                                        var dimensionList = ReadRegistryCodecArray((object[]) dimensionListNbt);

                                        World.StoreDimensionTypeList(dimensionList);
                                    }
                                    
                                    if (registryCodec.TryGetValue(World.WORLDGEN_BIOME_ID, out object worldgenBiomes))
                                    {
                                        var biomeListNbt = ((Dictionary<string, object>) worldgenBiomes)["value"];
                                        var biomeList = ReadRegistryCodecArray((object[]) biomeListNbt);

                                        // Read and store defined biomes 1.16.2 and above
                                        World.StoreBiomeList(biomeList);
                                    }
                                }
                                else // Different registries are sent in separate packets respectively
                                {
                                    var registryId = ResourceLocation.FromString(DataTypes.ReadNextString(packetData));

                                    if (registryId == ChatParser.CHAT_TYPE_ID)
                                    {
                                        var chatTypeList = ReadSingleRegistry(packetData, dataTypes.UseAnonymousNBT);
                                        
                                        ChatParser.ReadChatType(chatTypeList);
                                    }

                                    if (registryId == World.DIMENSION_TYPE_ID)
                                    {
                                        var dimensionList = ReadSingleRegistry(packetData, dataTypes.UseAnonymousNBT);
                                        
                                        World.StoreDimensionTypeList(dimensionList);
                                    }
                                }

                                break;

                            case ConfigurationPacketTypesIn.RemoveResourcePack:
                                if (DataTypes.ReadNextBool(packetData)) // Has UUID
                                    DataTypes.ReadNextUUID(packetData); // UUID
                                break;

                            case ConfigurationPacketTypesIn.ResourcePack:
                                HandleResourcePackPacket(packetData);
                                break;
                            
                            case ConfigurationPacketTypesIn.StoreCookie:
                                var name = DataTypes.ReadNextString(packetData);
                                var data = DataTypes.ReadNextByteArray(packetData);
                                handler.SetCookie(name, data);
                                break;
                            
                            case ConfigurationPacketTypesIn.Transfer:
                                var host = DataTypes.ReadNextString(packetData);
                                var port = DataTypes.ReadNextVarInt(packetData);
                                    
                                handler.Transfer(host, port);
                                break;
                            
                            case ConfigurationPacketTypesIn.KnownDataPacks:
                                var knownPacksCount = DataTypes.ReadNextVarInt(packetData);
                                List<(string, string, string)> knownDataPacks = new();
                                
                                for (var i = 0; i < knownPacksCount; i++)
                                {
                                    var nameSpace = DataTypes.ReadNextString(packetData);
                                    var id = DataTypes.ReadNextString(packetData);
                                    var version = DataTypes.ReadNextString(packetData);
                                    knownDataPacks.Add((nameSpace, id, version));
                                }

                                SendKnownDataPacks(knownDataPacks);
                                break;

                            // Ignore other packets at this stage
                            default:
                                return true;
                        }

                        break;

                    // https://wiki.vg/Protocol#Play
                    case CurrentState.Play:
                        return HandlePlayPackets(packetId, packetData);

                    default:
                        return true;
                }
            }
            catch (Exception innerException)
            {
                if (innerException is ThreadAbortException || innerException is SocketException ||
                    innerException.InnerException is SocketException)
                    throw; //Thread abort or Connection lost rather than invalid data

                throw new System.IO.InvalidDataException(
                    Translations.Get("exception.packet_process",
                        packetPalette.GetIncomingTypeById(packetId),
                        packetId,
                        protocolVersion,
                        currentState == CurrentState.Login,
                        innerException.GetType()),
                    innerException);
            }

            return true;
        }

        private void HandleResourcePackPacket(Queue<byte> packetData)
        {
            var uuid = Guid.Empty;

            if (protocolVersion >= MC_1_20_4_Version)
                uuid = DataTypes.ReadNextUUID(packetData);

            var url = DataTypes.ReadNextString(packetData);
            var hash = DataTypes.ReadNextString(packetData);

            if (protocolVersion >= MC_1_17_Version)
            {
                DataTypes.ReadNextBool(packetData); // Forced
                if (DataTypes.ReadNextBool(packetData)) // Has Prompt Message
                    dataTypes.ReadNextChat(packetData); // Prompt Message
            }

            // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
            if (!url.StartsWith("http") &&
                hash.Length != 40) // Some server may have null hash value
                return;

            //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
            var responseHeader = Array.Empty<byte>(); // After 1.10, the MC does not include resource pack hash in responses

            var basePacketData = protocolVersion >= MC_1_20_4_Version && uuid != Guid.Empty
                ? DataTypes.ConcatBytes(responseHeader, DataTypes.GetUUID(uuid))
                : responseHeader;

            var acceptedResourcePackData = DataTypes.ConcatBytes(basePacketData, DataTypes.GetVarInt(3));
            var loadedResourcePackData = DataTypes.ConcatBytes(basePacketData, DataTypes.GetVarInt(0));

            if (currentState == CurrentState.Configuration)
            {
                SendPacket(ConfigurationPacketTypesOut.ResourcePackResponse, acceptedResourcePackData); // Accepted
                SendPacket(ConfigurationPacketTypesOut.ResourcePackResponse, loadedResourcePackData); // Successfully loaded
            }
            else
            {
                SendPacket(PacketTypesOut.ResourcePackStatus, acceptedResourcePackData); // Accepted
                SendPacket(PacketTypesOut.ResourcePackStatus, loadedResourcePackData); // Successfully loaded
            }
        }

        private bool HandlePlayPackets(int packetId, Queue<byte> packetData)
        {
            switch (packetPalette.GetIncomingTypeById(packetId))
            {
                case PacketTypesIn.KeepAlive: // Keep Alive (Play)
                    {
                        SendPacket(PacketTypesOut.KeepAlive, packetData);
                        handler.OnServerKeepAlive();
                        break;
                    }
                case PacketTypesIn.Ping:
                    {
                        SendPacket(PacketTypesOut.Pong, packetData);
                        break;
                    }
                case PacketTypesIn.JoinGame:
                    {
                        // Temporary fix
                        receiveDeclareCommands = receivePlayerInfo = false;

                        messageIndex = 0;
                        pendingAcknowledgments = 0;

                        lastReceivedMessage = null;
                        lastSeenMessagesCollector = protocolVersion >= MC_1_19_3_Version ? new(20) : new(5);

                        handler.OnGameJoined(isOnlineMode);

                        int playerEntityId = DataTypes.ReadNextInt(packetData);
                        handler.OnReceivePlayerEntityId(playerEntityId);

                        DataTypes.ReadNextBool(packetData);                       // Is hardcore - 1.16.2 and above

                        if (protocolVersion < MC_1_20_2_Version)
                            handler.OnGamemodeUpdate(Guid.Empty, DataTypes.ReadNextByte(packetData));

                        if (protocolVersion < MC_1_20_2_Version)
                            DataTypes.ReadNextByte(packetData);                   // Previous Gamemode - 1.16 and above
                        
                        int worldCount = DataTypes.ReadNextVarInt(packetData);    // Dimension Count (World Count) - 1.16 and above
                        for (int i = 0; i < worldCount; i++)
                            DataTypes.ReadNextString(packetData);                 // Dimension Names (World Names) - 1.16 and above

                        if (protocolVersion < MC_1_20_2_Version)
                        {
                            // Registry Codec (Dimension Codec) - 1.16 and above
                            var registryCodec = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT).ToDictionary(
                                        x => ResourceLocation.FromString(x.Key), x => x.Value);

                            if (registryCodec.TryGetValue(World.DIMENSION_TYPE_ID, out object dimensionTypes))
                            {
                                var dimensionListNbt = ((Dictionary<string, object>) dimensionTypes)["value"];
                                var dimensionList = ReadRegistryCodecArray((object[]) dimensionListNbt);

                                // Read and store defined dimensions 1.16.2 and above
                                World.StoreDimensionTypeList(dimensionList);
                            }

                            if (registryCodec.TryGetValue(World.WORLDGEN_BIOME_ID, out object worldgenBiomes))
                            {
                                var biomeListNbt = ((Dictionary<string, object>) worldgenBiomes)["value"];
                                var biomeList = ReadRegistryCodecArray((object[]) biomeListNbt);

                                // Read and store defined biomes 1.16.2 and above
                                World.StoreBiomeList(biomeList);
                            }
                        }

                        if (protocolVersion < MC_1_20_2_Version)
                        {
                            // Current dimension
                            //   String: 1.19 and above
                            //   NBT Tag Compound: [1.16.2 to 1.18.2]
                            string? dimensionTypeName = null;
                            Dictionary<string, object>? dimensionType = null;
                            
                            if (protocolVersion >= MC_1_19_Version)
                                dimensionTypeName = DataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                            else
                                dimensionType = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT); // Dimension Type: NBT Tag Compound

                            currentDimension = 0;

                            string dimensionName = DataTypes.ReadNextString(packetData); // Dimension Id (World Id) - 1.16 and above
                            var dimensionId = ResourceLocation.FromString(dimensionName);

                            if (protocolVersion <= MC_1_18_2_Version)
                            {
                                // Store the received dimension type with received dimension id
                                World.StoreOneDimensionType(dimensionId, World.GetNextDimensionTypeNumIdCandidate(), dimensionType!);

                                World.SetDimensionType(dimensionId);
                                World.SetDimensionId(dimensionId);
                            }
                            else if (protocolVersion >= MC_1_19_Version)
                            {
                                var dimensionTypeId = ResourceLocation.FromString(dimensionTypeName!);

                                World.SetDimensionType(dimensionTypeId);
                                World.SetDimensionId(dimensionId);
                            }
                        }

                        if (protocolVersion < MC_1_20_2_Version)
                            DataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above

                        DataTypes.ReadNextVarInt(packetData);             // Max Players - 1.16.2 and above
                            
                        DataTypes.ReadNextVarInt(packetData);             // View distance - 1.14 and above
                            
                        if (protocolVersion >= MC_1_18_1_Version)
                            DataTypes.ReadNextVarInt(packetData);         // Simulation Distance - 1.18 and above
                            
                        DataTypes.ReadNextBool(packetData);               // Reduced debug info - 1.8 and above

                        DataTypes.ReadNextBool(packetData);               // Enable respawn screen - 1.15 and above

                        if (protocolVersion < MC_1_20_2_Version)
                        {
                            DataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                            DataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above

                            if (protocolVersion >= MC_1_19_Version)
                            {
                                if (DataTypes.ReadNextBool(packetData))       // Has death location
                                {
                                    DataTypes.SkipNextString(packetData);     // Death dimension name: Identifier
                                    DataTypes.ReadNextLocation(packetData);   // Death location
                                }
                            }

                            if (protocolVersion >= MC_1_20_Version)
                                DataTypes.ReadNextVarInt(packetData); // Portal Cooldown - 1.20 and above
                        }
                        else
                        {
                            DataTypes.ReadNextBool(packetData);                           // Do limited crafting
                            
                            // Dimension Type (string bellow 1.20.6, VarInt for 1.20.6+)
                            var dimensionTypeName = protocolVersion < MC_1_20_6_Version
                                ? DataTypes.ReadNextString(packetData) // < 1.20.6
                                : World.GetDimensionTypeIdByNumId(DataTypes.ReadNextInt(packetData)).ToString();

                            var dimensionName = DataTypes.ReadNextString(packetData);     // Dimension Name (World Name) - 1.16 and above
                            
                            var dimensionTypeId = ResourceLocation.FromString(dimensionTypeName);
                            var dimensionId = ResourceLocation.FromString(dimensionName);

                            World.SetDimensionType(dimensionTypeId);
                            World.SetDimensionId(dimensionId);

                            DataTypes.ReadNextLong(packetData); // Hashed world seed
                            handler.OnGamemodeUpdate(Guid.Empty, DataTypes.ReadNextByte(packetData));
                            DataTypes.ReadNextByte(packetData); // Previous Gamemode
                            DataTypes.ReadNextBool(packetData); // Is Debug
                            DataTypes.ReadNextBool(packetData); // Is Flat
                                
                            if (DataTypes.ReadNextBool(packetData))     // Has death location
                            {
                                DataTypes.SkipNextString(packetData);   // Death dimension name: Identifier
                                DataTypes.ReadNextLocation(packetData); // Death location
                            }

                            DataTypes.ReadNextVarInt(packetData); // Portal Cooldown

                            if (protocolVersion >= MC_1_20_6_Version)
                                DataTypes.ReadNextBool(packetData); // Enforoces Secure Chat
                        }
                    }
                    break;
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
                        var messageType = 0;

                        if (protocolVersion <= MC_1_18_2_Version) // 1.18 and below
                        {
                            var message = DataTypes.ReadNextString(packetData);

                            Guid senderUUID;
                            //Hide system messages or xp bar messages?
                            messageType = DataTypes.ReadNextByte(packetData);
                            if ((messageType == 1 && !ProtocolSettings.DisplaySystemMessages)
                                || (messageType == 2 && !ProtocolSettings.DisplayXpBarMessages))
                                break;

                            if (protocolVersion >= MC_1_16_5_Version)
                                senderUUID = DataTypes.ReadNextUUID(packetData);
                            else senderUUID = Guid.Empty;

                            handler.OnTextReceived(new(message, null, true, messageType, senderUUID));
                        }
                        else if (protocolVersion == MC_1_19_Version) // 1.19
                        {
                            var signedChat = DataTypes.ReadNextString(packetData);

                            var hasUnsignedChatContent = DataTypes.ReadNextBool(packetData);
                            string? unsignedChatContent =
                                hasUnsignedChatContent ? DataTypes.ReadNextString(packetData) : null;

                            messageType = DataTypes.ReadNextVarInt(packetData);
                            if ((messageType == 1 && !ProtocolSettings.DisplaySystemMessages)
                                || (messageType == 2 && !ProtocolSettings.DisplayXpBarMessages))
                                break;

                            var senderUUID = DataTypes.ReadNextUUID(packetData);
                            var senderDisplayName = ChatParser.ParseText(DataTypes.ReadNextString(packetData));

                            bool hasSenderTeamName = DataTypes.ReadNextBool(packetData);
                            string? senderTeamName = hasSenderTeamName
                                ? ChatParser.ParseText(DataTypes.ReadNextString(packetData))
                                : null;

                            var timestamp = DataTypes.ReadNextLong(packetData);

                            var salt = DataTypes.ReadNextLong(packetData);

                            var messageSignature = DataTypes.ReadNextByteArray(packetData);

                            bool verifyResult;
                            if (!isOnlineMode)
                                verifyResult = false;
                            else if (senderUUID == handler.GetUserUUID())
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
                            var precedingSignature = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextByteArray(packetData)
                                : null;
                            var senderUUID = DataTypes.ReadNextUUID(packetData);
                            var headerSignature = DataTypes.ReadNextByteArray(packetData);

                            var signedChat = DataTypes.ReadNextString(packetData);
                            var decorated = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextString(packetData)
                                : null;

                            var timestamp = DataTypes.ReadNextLong(packetData);
                            var salt = DataTypes.ReadNextLong(packetData);

                            var lastSeenMessageListLen = DataTypes.ReadNextVarInt(packetData);
                            var lastSeenMessageList =
                                new LastSeenMessageList.AcknowledgedMessage[lastSeenMessageListLen];
                            for (int i = 0; i < lastSeenMessageListLen; ++i)
                            {
                                var user = DataTypes.ReadNextUUID(packetData);
                                var lastSignature = DataTypes.ReadNextByteArray(packetData);
                                lastSeenMessageList[i] = new(user, lastSignature, true);
                            }

                            LastSeenMessageList lastSeenMessages = new(lastSeenMessageList);

                            var unsignedChatContent = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextString(packetData)
                                : null;

                            var filterEnum = (MessageFilterType)DataTypes.ReadNextVarInt(packetData);
                            if (filterEnum == MessageFilterType.PartiallyFiltered)
                                DataTypes.ReadNextULongArray(packetData);

                            var chatTypeId = DataTypes.ReadNextVarInt(packetData);
                            var chatName = DataTypes.ReadNextString(packetData);
                            var targetName = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextString(packetData)
                                : null;

                            var chatInfo = Json.ParseJson(chatName).Properties;
                            var senderDisplayName =
                                (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"])
                                .StringValue;
                            string? senderTeamName = null;
                            if (!ChatParser.MessageTypeRegistry.TryGetByNumId(chatTypeId, out var messageTypeEnum))
                            {
                                messageTypeEnum = ChatParser.MessageType.CHAT;
                            }

                            if (targetName != null &&
                                messageTypeEnum is ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING or ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING)
                                senderTeamName = Json.ParseJson(targetName).Properties["with"].DataArray[0]
                                    .Properties["text"].StringValue;

                            if (string.IsNullOrWhiteSpace(senderDisplayName))
                            {
                                var player = handler.GetPlayerInfo(senderUUID);
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
                            else if (senderUUID == handler.GetUserUUID())
                                verifyResult = true;
                            else
                            {
                                var player = handler.GetPlayerInfo(senderUUID);
                                if (player == null || !player.IsMessageChainLegal())
                                    verifyResult = false;
                                else
                                {
                                    var lastVerifyResult = player.IsMessageChainLegal();
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
                            var senderUUID = DataTypes.ReadNextUUID(packetData);
                            var index = DataTypes.ReadNextVarInt(packetData);
                            // Signature is fixed size of 256 bytes
                            var messageSignature = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextByteArray(packetData, 256)
                                : null;

                            // Body
                            // net.minecraft.network.message.MessageBody.Serialized#write
                            var message = DataTypes.ReadNextString(packetData);
                            var timestamp = DataTypes.ReadNextLong(packetData);
                            var salt = DataTypes.ReadNextLong(packetData);

                            // Previous Messages
                            // net.minecraft.network.message.LastSeenMessageList.Indexed#write
                            // net.minecraft.network.message.MessageSignatureData.Indexed#write
                            var totalPreviousMessages = DataTypes.ReadNextVarInt(packetData);
                            var previousMessageSignatures = new Tuple<int, byte[]?>[totalPreviousMessages];

                            for (int i = 0; i < totalPreviousMessages; i++)
                            {
                                // net.minecraft.network.message.MessageSignatureData.Indexed#fromBuf
                                var messageId = DataTypes.ReadNextVarInt(packetData) - 1;
                                if (messageId == -1)
                                    previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId,
                                        DataTypes.ReadNextByteArray(packetData, 256));
                                else
                                    previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId, null);
                            }

                            // Other
                            var unsignedChatContent = DataTypes.ReadNextBool(packetData)
                                ? dataTypes.ReadNextChat(packetData)
                                : null;

                            var filterType = (MessageFilterType)DataTypes.ReadNextVarInt(packetData);

                            if (filterType == MessageFilterType.PartiallyFiltered)
                                DataTypes.ReadNextULongArray(packetData);

                            // Network Target
                            // net.minecraft.network.message.MessageType.Serialized#write
                            var chatTypeId = DataTypes.ReadNextVarInt(packetData);
                            var chatName = dataTypes.ReadNextChat(packetData);
                            var targetName = DataTypes.ReadNextBool(packetData)
                                ? dataTypes.ReadNextChat(packetData)
                                : null;

                            if (!ChatParser.MessageTypeRegistry.TryGetByNumId(chatTypeId, out var messageTypeEnum))
                            {
                                messageTypeEnum = ChatParser.MessageType.CHAT;
                            }

                            //var chatInfo = Json.ParseJson(targetName ?? chatName).Properties;
                            var senderDisplayName = chatName;
                            var senderTeamName = targetName;

                            if (string.IsNullOrWhiteSpace(senderDisplayName))
                            {
                                var player = handler.GetPlayerInfo(senderUUID);
                                if (player != null && (player.DisplayName != null || player.Name != null) &&
                                    string.IsNullOrWhiteSpace(senderDisplayName))
                                {
                                    senderDisplayName = player.DisplayName ?? player.Name;
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
                                if (senderUUID == handler.GetUserUUID())
                                    verifyResult = true;
                                else
                                {
                                    var player = handler.GetPlayerInfo(senderUUID);
                                    if (player == null || !player.IsMessageChainLegal())
                                        verifyResult = false;
                                    else
                                    {
                                        verifyResult = player.VerifyMessage(message, senderUUID, player.ChatUUID,
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
                    }
                    break;
                case PacketTypesIn.ChunkBatchFinished:
                    {
                        var batchSize = DataTypes.ReadNextVarInt(packetData); // Number of chunks received

                        if (batchSize > 0)
                        {
                            var d = GetNanos() - chunkBatchStartTime;
                            var d2 = d / (double)batchSize;
                            var d3 = Math.Clamp(d2, aggregatedNanosPerChunk / 3.0, aggregatedNanosPerChunk * 3.0);
                            aggregatedNanosPerChunk =
                                (aggregatedNanosPerChunk * oldSamplesWeight + d3) / (oldSamplesWeight + 1);
                            oldSamplesWeight = Math.Min(49, oldSamplesWeight + 1);
                        }

                        SendChunkBatchReceived((float)(7000000.0 / aggregatedNanosPerChunk));
                    }
                    break;
                case PacketTypesIn.ChunkBatchStarted:
                    {
                        chunkBatchStartTime = GetNanos();
                    }
                    break;
                case PacketTypesIn.StartConfiguration:
                    {
                        currentState = CurrentState.Configuration;
                        SendAcknowledgeConfiguration();
                    }
                    break;
                case PacketTypesIn.HideMessage:
                    {
                        var hideMessageSignature = DataTypes.ReadNextByteArray(packetData);
                        Debug.Log($"HideMessage was not processed! (SigLen={hideMessageSignature.Length})");
                    }
                    break;
                case PacketTypesIn.SystemChat:
                    var systemMessage = dataTypes.ReadNextChat(packetData);
                    
                    if (protocolVersion >= MC_1_19_3_Version)
                    {
                        var isOverlay = DataTypes.ReadNextBool(packetData);
                        if (isOverlay)
                        {
                            if (!ProtocolSettings.DisplayXpBarMessages)
                                break;
                        }
                        else
                        {
                            if (!ProtocolSettings.DisplaySystemMessages)
                                break;
                        }

                        handler.OnTextReceived(new(systemMessage, null, false, -1, Guid.Empty, true));
                    }
                    else
                    {
                        var msgType = DataTypes.ReadNextVarInt(packetData);
                        if (msgType == 1 && !ProtocolSettings.DisplaySystemMessages)
                            break;
                        handler.OnTextReceived(new(systemMessage, null, true, msgType, Guid.Empty, true));
                    }

                    break;
                case PacketTypesIn.ProfilelessChatMessage:
                    var message_ = dataTypes.ReadNextChat(packetData);
                    var messageType_ = DataTypes.ReadNextVarInt(packetData);
                    var messageName = dataTypes.ReadNextChat(packetData);
                    var targetName_ = DataTypes.ReadNextBool(packetData)
                        ? dataTypes.ReadNextChat(packetData)
                        : null;
                    ChatMessage profilelessChat = new(message_, targetName_ ?? messageName,
                        true, messageType_, Guid.Empty, true)
                    {
                        isSenderJson = true
                    };
                    handler.OnTextReceived(profilelessChat);
                    break;
                case PacketTypesIn.CombatEvent:
                    // 1.8 - 1.16.5
                    if (protocolVersion <= MC_1_16_5_Version)
                    {
                        var eventType = (CombatEventType)DataTypes.ReadNextVarInt(packetData);

                        if (eventType == CombatEventType.EntityDead)
                        {
                            DataTypes.SkipNextVarInt(packetData);

                            handler.OnPlayerKilled(
                                DataTypes.ReadNextInt(packetData),
                                ChatParser.ParseText(DataTypes.ReadNextString(packetData))
                            );
                        }
                    }
                    break;
                case PacketTypesIn.DeathCombatEvent:
                    DataTypes.SkipNextVarInt(packetData);

                    handler.OnPlayerKilled(
                        protocolVersion >= MC_1_20_Version ? -1 : DataTypes.ReadNextInt(packetData),
                        ChatParser.ParseText(dataTypes.ReadNextChat(packetData))
                    );

                    break;
                case PacketTypesIn.DamageEvent: // 1.19.4
                    if (protocolVersion >= MC_1_19_4_Version)
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);
                        var sourceTypeId = DataTypes.ReadNextVarInt(packetData);
                        var sourceCauseId = DataTypes.ReadNextVarInt(packetData);
                        var sourceDirectId = DataTypes.ReadNextVarInt(packetData);

                        Location? sourcePos;
                        if (DataTypes.ReadNextBool(packetData))
                        {
                            sourcePos = new Location()
                            {
                                X = DataTypes.ReadNextDouble(packetData),
                                Y = DataTypes.ReadNextDouble(packetData),
                                Z = DataTypes.ReadNextDouble(packetData)
                            };
                        }

                        // TODO: Write a function to use this data ? But seems not too useful
                    }
                    break;
                case PacketTypesIn.MessageHeader: // 1.19.2 only
                    if (protocolVersion == MC_1_19_2_Version)
                    {
                        var precedingSignature = DataTypes.ReadNextBool(packetData)
                            ? DataTypes.ReadNextByteArray(packetData)
                            : null;
                        var senderUUID = DataTypes.ReadNextUUID(packetData);
                        var headerSignature = DataTypes.ReadNextByteArray(packetData);
                        var bodyDigest = DataTypes.ReadNextByteArray(packetData);

                        bool verifyResult;

                        if (!isOnlineMode)
                            verifyResult = false;
                        else if (senderUUID == handler.GetUserUUID())
                            verifyResult = true;
                        else
                        {
                            var player = handler.GetPlayerInfo(senderUUID);

                            if (player == null || !player.IsMessageChainLegal())
                                verifyResult = false;
                            else
                            {
                                var lastVerifyResult = player.IsMessageChainLegal();
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

                        if (protocolVersion >= MC_1_20_6_Version)
                            dimensionTypeNameRespawn = World.GetDimensionTypeIdByNumId(DataTypes.ReadNextInt(packetData)).ToString();
                        if (protocolVersion >= MC_1_19_Version)
                            dimensionTypeNameRespawn = DataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                        else // 1.16.2+
                            dimensionTypeRespawn = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT); // Dimension Type: NBT Tag Compound
                            
                        currentDimension = 0;

                        var dimensionName = DataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above
                        var dimensionId = ResourceLocation.FromString(dimensionName);

                        if (protocolVersion <= MC_1_18_2_Version)
                        {
                            // Store the received dimension type with received dimension id
                            World.StoreOneDimensionType(dimensionId, World.GetNextDimensionTypeNumIdCandidate(), dimensionTypeRespawn!);

                            World.SetDimensionType(dimensionId);
                            World.SetDimensionId(dimensionId);
                        }
                        else if (protocolVersion >= MC_1_19_Version)
                        {
                            var dimensionTypeIdRespawn = ResourceLocation.FromString(dimensionTypeNameRespawn);

                            World.SetDimensionType(dimensionTypeIdRespawn);
                            World.SetDimensionId(dimensionId);
                        }

                        DataTypes.ReadNextLong(packetData);               // Hashed world seed - 1.15 and above
                        DataTypes.ReadNextByte(packetData);               // Gamemode
                        DataTypes.ReadNextByte(packetData);               // Previous Game mode - 1.16 and above

                        DataTypes.ReadNextBool(packetData);               // Is Debug - 1.16 and above
                        DataTypes.ReadNextBool(packetData);               // Is Flat - 1.16 and above

                        if (protocolVersion < MC_1_20_2_Version)
                            DataTypes.ReadNextBool(packetData); // Copy metadata (Data Kept) - 1.16 - 1.20.2

                        if (protocolVersion >= MC_1_19_Version)
                        {
                            if (DataTypes.ReadNextBool(packetData))     // Has death location
                            {
                                DataTypes.ReadNextString(packetData);   // Death dimension name: Identifier
                                DataTypes.ReadNextLocation(packetData); // Death location
                            }
                        }

                        if (protocolVersion >= MC_1_20_Version)
                            DataTypes.ReadNextVarInt(packetData); // Portal Cooldown - 1.20 and above

                        if (protocolVersion >= MC_1_20_2_Version)
                            DataTypes.ReadNextBool(packetData);   // Copy metadata (Data Kept) - 1.20.2 and ab

                        handler.OnRespawn();
                    }
                    break;
                case PacketTypesIn.PlayerPositionAndLook:
                    {
                        // These always need to be read, since we need the field after them for teleport confirm
                        var location = new Location(
                            DataTypes.ReadNextDouble(packetData), // X
                            DataTypes.ReadNextDouble(packetData), // Y
                            DataTypes.ReadNextDouble(packetData) // Z
                        );

                        var yaw = DataTypes.ReadNextFloat(packetData);
                        var pitch = DataTypes.ReadNextFloat(packetData);
                        var locMask = DataTypes.ReadNextByte(packetData);

                        // entity handling require player pos for distance calculating
                        var currentLocation = handler.GetCurrentLocation();
                        location.X = (locMask & 1 << 0) != 0 ? currentLocation.X + location.X : location.X;
                        location.Y = (locMask & 1 << 1) != 0 ? currentLocation.Y + location.Y : location.Y;
                        location.Z = (locMask & 1 << 2) != 0 ? currentLocation.Z + location.Z : location.Z;

                        var teleportId = DataTypes.ReadNextVarInt(packetData);

                        if (teleportId < 0)
                        {
                            yaw = LastYaw;
                            pitch = LastPitch;
                        }
                        else
                        {
                            LastYaw = yaw;
                            LastPitch = pitch;
                        }

                        handler.UpdateLocation(location, yaw, pitch);

                        // Teleport confirm packet
                        SendPacket(PacketTypesOut.TeleportConfirm, DataTypes.GetVarInt(teleportId));
                            
                        if (protocolVersion is >= MC_1_17_Version and < MC_1_19_4_Version)
                            DataTypes.ReadNextBool(packetData); // Dismount Vehicle    - 1.17 to 1.19.3
                    }
                    break;
                case PacketTypesIn.ChunkData:
                    {
                        var chunkRenderManager = handler.GetChunkRenderManager();

                        var chunkX = DataTypes.ReadNextInt(packetData);
                        var chunkZ = DataTypes.ReadNextInt(packetData);

                        if (protocolVersion >= MC_1_17_Version)
                        {
                            ulong[]? verticalStripBitmask = null;

                            if (protocolVersion is MC_1_17_Version or MC_1_17_1_Version)
                                verticalStripBitmask = DataTypes.ReadNextULongArray(packetData); // Bit Mask Length  and  Primary Bit Mask

                            DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT); // Heightmaps

                            pTerrain.ProcessChunkColumnData17(chunkX, chunkZ, verticalStripBitmask, packetData);
                        }
                        else
                        {
                            var chunksContinuous = DataTypes.ReadNextBool(packetData);

                            var chunkMask = (ushort)DataTypes.ReadNextVarInt(packetData);

                            DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT);  // Heightmaps - 1.14 and above

                            pTerrain.ProcessChunkColumnData16(chunkX, chunkZ, chunkMask, chunksContinuous, packetData);
                        }
                    }
                    break;
                case PacketTypesIn.ChunksBiomes: // 1.19.4
                    {
                        var count = DataTypes.ReadNextVarInt(packetData);

                        for (int i = 0; i < count; i++)
                        {
                            // z goes before x in packed chunk pos
                            var chunkZ = DataTypes.ReadNextInt(packetData);
                            var chunkX = DataTypes.ReadNextInt(packetData);

                            var size = DataTypes.ReadNextVarInt(packetData);

                            // Drop data for now, handle them if needed in the future
                            DataTypes.DropData(size, packetData);
                        }
                    }
                    break;
                case PacketTypesIn.UpdateLight:
                    {
                        var chunkX = DataTypes.ReadNextVarInt(packetData);
                        var chunkZ = DataTypes.ReadNextVarInt(packetData);

                        pTerrain.ProcessChunkLightData(chunkX, chunkZ, packetData);
                    }
                    break;
                case PacketTypesIn.MapData:
                    {
                        var mapId = DataTypes.ReadNextVarInt(packetData);
                        var scale = DataTypes.ReadNextByte(packetData);

                        var trackingPosition = true; //  1.9+
                        var locked = false;          // 1.14+

                        // 1.17+ (locked and trackingPosition switched places)
                        if (protocolVersion >= MC_1_17_Version)
                        {
                            locked = DataTypes.ReadNextBool(packetData);

                            trackingPosition = DataTypes.ReadNextBool(packetData);
                        }
                        else
                        {
                            trackingPosition = DataTypes.ReadNextBool(packetData);

                            locked = DataTypes.ReadNextBool(packetData);
                        }

                        List<MapIcon> icons = new();

                        // 1.9 or later needs tracking position to be true to get the icons
                        if (trackingPosition)
                        {
                            var iconCount = DataTypes.ReadNextVarInt(packetData);

                            for (int i = 0; i < iconCount; i++)
                            {
                                MapIcon mapIcon = new()
                                {
                                    // 1.13.2+
                                    Type = (MapIconType)DataTypes.ReadNextVarInt(packetData),
                                    X = DataTypes.ReadNextByte(packetData),
                                    Z = DataTypes.ReadNextByte(packetData),
                                    // 1.13.2+
                                    Direction = DataTypes.ReadNextByte(packetData)
                                };

                                if (DataTypes.ReadNextBool(packetData)) // Has Display Name?
                                    mapIcon.DisplayName = dataTypes.ReadNextChat(packetData);

                                icons.Add(mapIcon);
                            }
                        }

                        var colsUpdated = DataTypes.ReadNextByte(packetData); // width
                        byte rowsUpdated = 0; // height
                        byte mapColX = 0;
                        byte mapRowZ = 0;
                        byte[]? colors = null;

                        if (colsUpdated > 0)
                        {
                            rowsUpdated = DataTypes.ReadNextByte(packetData); // height
                            mapColX = DataTypes.ReadNextByte(packetData);
                            mapRowZ = DataTypes.ReadNextByte(packetData);
                            colors = DataTypes.ReadNextByteArray(packetData);
                        }

                        handler.OnMapData(mapId, scale, trackingPosition, locked, icons, colsUpdated, rowsUpdated, mapColX, mapRowZ, colors);
                    }
                    break;
                case PacketTypesIn.TradeList: // MC 1.14 or greater
                    {
                        var inventoryId = DataTypes.ReadNextVarInt(packetData);
                        int size = DataTypes.ReadNextByte(packetData);
                        List<VillagerTrade> trades = new();
                        for (int tradeId = 0; tradeId < size; tradeId++)
                        {
                            VillagerTrade trade = dataTypes.ReadNextTrade(packetData, ItemPalette.INSTANCE);
                                trades.Add(trade);
                        }
                        VillagerInfo villagerInfo = new()
                        {
                            Level = DataTypes.ReadNextVarInt(packetData),
                            Experience = DataTypes.ReadNextVarInt(packetData),
                            IsRegularVillager = DataTypes.ReadNextBool(packetData),
                            CanRestock = DataTypes.ReadNextBool(packetData)
                        };
                        handler.OnTradeList(inventoryId, trades, villagerInfo);
                    }
                    break;
                case PacketTypesIn.Title:
                    {
                        var action = DataTypes.ReadNextVarInt(packetData);
                        var titleText = string.Empty;
                        var subtitleText = string.Empty;
                        var actionbarText = string.Empty;
                        var json = string.Empty;
                        var fadein = -1;
                        var stay = -1;
                        var fadeout = -1;

                        // MC 1.10 or greater
                        if (action == 0)
                        {
                            json = titleText;
                            titleText = ChatParser.ParseText(DataTypes.ReadNextString(packetData));
                        }
                        else if (action == 1)
                        {
                            json = subtitleText;
                            subtitleText = ChatParser.ParseText(DataTypes.ReadNextString(packetData));
                        }
                        else if (action == 2)
                        {
                            json = actionbarText;
                            actionbarText = ChatParser.ParseText(DataTypes.ReadNextString(packetData));
                        }
                        else if (action == 3)
                        {
                            fadein = DataTypes.ReadNextInt(packetData);
                            stay = DataTypes.ReadNextInt(packetData);
                            fadeout = DataTypes.ReadNextInt(packetData);
                        }
                        handler.OnTitle(action, titleText, subtitleText, actionbarText, fadein, stay, fadeout, json);
                    }
                    break;
                case PacketTypesIn.ServerData:
                    var motd = "-";

                    var hasMotd = false;
                    if (protocolVersion < MC_1_19_4_Version)
                    {
                        hasMotd = DataTypes.ReadNextBool(packetData);

                        if (hasMotd)
                            motd = dataTypes.ReadNextChat(packetData);
                    }
                    else
                    {
                        hasMotd = true;
                        motd = dataTypes.ReadNextChat(packetData);
                    }

                    var iconBase64 = "-";
                    var hasIcon = DataTypes.ReadNextBool(packetData);
                    if (hasIcon)
                    {
                        if (protocolVersion < MC_1_20_2_Version)
                            iconBase64 = DataTypes.ReadNextString(packetData);
                        else
                        {
                            var pngData = DataTypes.ReadNextByteArray(packetData);
                            iconBase64 = Convert.ToBase64String(pngData);
                        }
                    }

                    var previewsChat = false;
                    if (protocolVersion < MC_1_19_3_Version)
                        previewsChat = DataTypes.ReadNextBool(packetData);

                    handler.OnServerDataReceived(hasMotd, motd, hasIcon, iconBase64, previewsChat);
                    break;
                case PacketTypesIn.MultiBlockChange:
                    {
                        // MC 1.16.2+
                        var chunkSection = DataTypes.ReadNextLong(packetData);
                        var sectionX = (int)(chunkSection >> 42);
                        var sectionY = (int)((chunkSection << 44) >> 44);
                        var sectionZ = (int)((chunkSection << 22) >> 42);
                            
                        if(protocolVersion < MC_1_20_Version)
                            DataTypes.ReadNextBool(packetData); // Useless boolean (Related to light update)

                        var blocksSize = DataTypes.ReadNextVarInt(packetData);
                        for (int i = 0; i < blocksSize; i++)
                        {
                            var block = (ulong)DataTypes.ReadNextVarLong(packetData);
                            var blockId = (int)(block >> 12);
                            var localX = (int)((block >> 8) & 0x0F);
                            var localZ = (int)((block >> 4) & 0x0F);
                            var localY = (int)(block & 0x0F);

                            var bloc = new Block((ushort)blockId);
                            var blockX = (sectionX * 16) + localX;
                            var blockY = (sectionY * 16) + localY;
                            var blockZ = (sectionZ * 16) + localZ;

                            var blockLoc = new BlockLoc(blockX, blockY, blockZ);

                            handler.GetChunkRenderManager().SetBlock(blockLoc, bloc);
                        }
                    }
                    break;
                case PacketTypesIn.BlockChange:
                    {
                        var blockLoc = DataTypes.ReadNextBlockLoc(packetData);
                        var bloc = new Block((ushort) DataTypes.ReadNextVarInt(packetData));
                            
                        handler.GetChunkRenderManager().SetBlock(blockLoc, bloc);
                    }
                    break;
                case PacketTypesIn.BlockEntityData:
                    {
                        var blockLoc = DataTypes.ReadNextBlockLoc(packetData);
                        var ttt = DataTypes.ReadNextVarInt(packetData);
                        var tag = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT);
                            
                        if (protocolVersion < MC_1_18_1_Version)
                        {
                            // Block entity id is sent with nbt data
                            var typeId = ResourceLocation.FromString((string) tag["id"]);
                            var type = BlockEntityTypePalette.INSTANCE.GetById(typeId);
                            //UnityEngine.Debug.Log($"Single [{blockLoc}] {Json.Object2Json(tag)}");
                            Loom.QueueOnMainThread(() => {
                                handler.GetChunkRenderManager().AddBlockEntityRender(blockLoc, type, tag);
                            });
                        }
                        else
                        {
                            // Block entity id is sent as varint
                            var type = BlockEntityTypePalette.INSTANCE.GetByNumId(ttt);
                            Loom.QueueOnMainThread(() => {
                                handler.GetChunkRenderManager().AddBlockEntityRender(blockLoc, type, tag);
                            });
                        }
                    }
                    break;
                case PacketTypesIn.UnloadChunk:
                    {
                        // Warning: It is legal to include unloaded chunks in the UnloadChunk packet.
                        // Since chunks that have not been loaded are not recorded, this may result
                        // in loading chunks that should be unloaded and inaccurate statistics.
                        if (protocolVersion >= MC_1_20_2_Version)
                        {
                            // z goes before x in packed chunk pos
                            var chunkZ = DataTypes.ReadNextInt(packetData);
                            var chunkX = DataTypes.ReadNextInt(packetData);

                            handler.GetChunkRenderManager().UnloadChunkColumn(chunkX, chunkZ);
                        }
                        else
                        {
                            // Before 1.20.2 (764) these are sent as two integers, x then z
                            var chunkX = DataTypes.ReadNextInt(packetData);
                            var chunkZ = DataTypes.ReadNextInt(packetData);

                            handler.GetChunkRenderManager().UnloadChunkColumn(chunkX, chunkZ);
                        }
                    }
                    break;
                case PacketTypesIn.ChangeGameState:
                    {
                        var changeReason = DataTypes.ReadNextByte(packetData);
                        var changeValue = DataTypes.ReadNextFloat(packetData);
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
                    var previewsChatSetting = DataTypes.ReadNextBool(packetData);
                    // TODO handler.OnChatPreviewSettingUpdate(previewsChatSetting);
                    break;
                case PacketTypesIn.ChatPreview:
                    // TODO Currently not implemented
                    break;
                case PacketTypesIn.PlayerInfo:
                    if (protocolVersion >= MC_1_19_3_Version)
                    {
                        var actionBitset = DataTypes.ReadNextByte(packetData);
                        var numberOfActions = DataTypes.ReadNextVarInt(packetData);
                        for (int i = 0; i < numberOfActions; i++)
                        {
                            var playerUUID = DataTypes.ReadNextUUID(packetData);

                            PlayerInfo player;
                            if ((actionBitset & (1 << 0)) > 0) // Actions bit 0: add player
                            {
                                var name = DataTypes.ReadNextString(packetData);
                                var numberOfProperties = DataTypes.ReadNextVarInt(packetData);
                                for (int j = 0; j < numberOfProperties; ++j)
                                {
                                    DataTypes.SkipNextString(packetData);
                                    DataTypes.SkipNextString(packetData);
                                    if (DataTypes.ReadNextBool(packetData))
                                        DataTypes.SkipNextString(packetData);
                                }

                                player = new(name, playerUUID);
                                handler.OnPlayerJoin(player);
                            }
                            else
                            {
                                var playerGet = handler.GetPlayerInfo(playerUUID);
                                if (playerGet == null)
                                {
                                    player = new(string.Empty, playerUUID);
                                    handler.OnPlayerJoin(player);
                                }
                                else
                                {
                                    player = playerGet;
                                }
                            }

                            if ((actionBitset & (1 << 1)) > 0) // Actions bit 1: initialize chat
                            {
                                var hasSignatureData = DataTypes.ReadNextBool(packetData);
                                
                                if (hasSignatureData)
                                {
                                    var chatUUID = DataTypes.ReadNextUUID(packetData);
                                    var publicKeyExpiryTime = DataTypes.ReadNextLong(packetData);
                                    var encodedPublicKey = DataTypes.ReadNextByteArray(packetData);
                                    var publicKeySignature = DataTypes.ReadNextByteArray(packetData);
                                    player.SetPublicKey(chatUUID, publicKeyExpiryTime, encodedPublicKey,
                                        publicKeySignature);

                                    if (playerUUID == handler.GetUserUUID())
                                    {
                                        this.chatUUID = chatUUID;
                                    }
                                }
                                else
                                {
                                    player.ClearPublicKey();
                                }

                                if (playerUUID == handler.GetUserUUID())
                                {
                                    receivePlayerInfo = true;
                                    if (receiveDeclareCommands)
                                        handler.SetCanSendMessage(true);
                                }
                            }

                            if ((actionBitset & 1 << 2) > 0) // Actions bit 2: update gamemode
                            {
                                handler.OnGamemodeUpdate(playerUUID, DataTypes.ReadNextVarInt(packetData));
                            }

                            if ((actionBitset & (1 << 3)) > 0) // Actions bit 3: update listed
                            {
                                player.Listed = DataTypes.ReadNextBool(packetData);
                            }

                            if ((actionBitset & (1 << 4)) > 0) // Actions bit 4: update latency
                            {
                                var latency = DataTypes.ReadNextVarInt(packetData);
                                handler.OnLatencyUpdate(playerUUID, latency); //Update latency;
                            }

                            // Actions bit 5: update display name
                            if ((actionBitset & 1 << 5) <= 0) continue;
                            player.DisplayName = DataTypes.ReadNextBool(packetData)
                                ? dataTypes.ReadNextChat(packetData)
                                : null;
                        }
                    }
                    else // 1.8 - 1.19.2
                    {
                        var action = DataTypes.ReadNextVarInt(packetData); // Action Name
                        var numberOfPlayers = DataTypes.ReadNextVarInt(packetData); // Number Of Players 

                        for (int i = 0; i < numberOfPlayers; i++)
                        {
                            var uuid = DataTypes.ReadNextUUID(packetData); // Player UUID

                            switch (action)
                            {
                                case 0x00: //Player Join (Add player since 1.19)
                                    var name = DataTypes.ReadNextString(packetData); // Player name
                                    var propNum =
                                        DataTypes.ReadNextVarInt(
                                            packetData); // Number of properties in the following array

                                    // Property: Tuple<Name, Value, Signature(empty if there is no signature)
                                    // The Property field looks as in the response of https://wiki.vg/Mojang_API#UUID_to_Profile_and_Skin.2FCape
                                    const bool useProperty = false;
#pragma warning disable CS0162 // Unreachable code detected
                                    var properties =
                                        useProperty ? new Tuple<string, string, string?>[propNum] : null;
                                    for (int p = 0; p < propNum; p++)
                                    {
                                        var propertyName =
                                            DataTypes.ReadNextString(packetData); // Name: String (32767)
                                        var val =
                                            DataTypes.ReadNextString(packetData); // Value: String (32767)
                                        string? propertySignature = null;
                                        if (DataTypes.ReadNextBool(packetData)) // Is Signed
                                            propertySignature =
                                                DataTypes.ReadNextString(
                                                    packetData); // Signature: String (32767)
                                        if (useProperty)
                                            properties![p] = new(propertyName, val, propertySignature);
                                    }
#pragma warning restore CS0162 // Unreachable code detected

                                    var gameMode = DataTypes.ReadNextVarInt(packetData); // Gamemode
                                    handler.OnGamemodeUpdate(uuid, gameMode);

                                    var ping = DataTypes.ReadNextVarInt(packetData); // Ping

                                    string? displayName = null;
                                    if (DataTypes.ReadNextBool(packetData)) // Has display name
                                        displayName = DataTypes.ReadNextString(packetData); // Display name

                                    // 1.19 Additions
                                    long? keyExpiration = null;
                                    byte[]? publicKey = null, signature = null;
                                    if (protocolVersion >= MC_1_19_Version)
                                    {
                                        if (DataTypes.ReadNextBool(
                                                packetData)) // Has Sig Data (if true, red the following fields)
                                        {
                                            keyExpiration = DataTypes.ReadNextLong(packetData); // Timestamp

                                            var publicKeyLength =
                                                DataTypes.ReadNextVarInt(packetData); // Public Key Length 
                                            if (publicKeyLength > 0)
                                                publicKey = DataTypes.ReadData(publicKeyLength,
                                                    packetData); // Public key

                                            var signatureLength =
                                                DataTypes.ReadNextVarInt(packetData); // Signature Length 
                                            if (signatureLength > 0)
                                                signature = DataTypes.ReadData(signatureLength,
                                                    packetData); // Public key
                                        }
                                    }

                                    handler.OnPlayerJoin(new PlayerInfo(uuid, name, properties, gameMode, ping,
                                        displayName, keyExpiration, publicKey, signature));
                                    break;
                                case 0x01: //Update gamemode
                                    handler.OnGamemodeUpdate(uuid, DataTypes.ReadNextVarInt(packetData));
                                    break;
                                case 0x02: //Update latency
                                    var latency = DataTypes.ReadNextVarInt(packetData);
                                    handler.OnLatencyUpdate(uuid, latency); //Update latency;
                                    break;
                                case 0x03: //Update display name
                                    if (DataTypes.ReadNextBool(packetData))
                                    {
                                        var player = handler.GetPlayerInfo(uuid);
                                        if (player != null)
                                            player.DisplayName = DataTypes.ReadNextString(packetData);
                                        else
                                            DataTypes.SkipNextString(packetData);
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
                    var numberOfLeavePlayers = DataTypes.ReadNextVarInt(packetData);
                    for (int i = 0; i < numberOfLeavePlayers; ++i)
                    {
                        var playerUUID = DataTypes.ReadNextUUID(packetData);
                        handler.OnPlayerLeave(playerUUID);
                    }

                    break;
                case PacketTypesIn.TabComplete:
                    {
                        var oldTransactionId = autocomplete_transaction_id;

                        // MC 1.13 or greater
                        autocomplete_transaction_id = DataTypes.ReadNextVarInt(packetData);
                        var completionStart  = DataTypes.ReadNextVarInt(packetData); // Start of text to replace
                        var completionLength = DataTypes.ReadNextVarInt(packetData); // Length of text to replace

                        var resultCount = DataTypes.ReadNextVarInt(packetData);
                        var completeResults = new string[resultCount];

                        for (int i = 0; i < resultCount; i++)
                        {
                            completeResults[i] = DataTypes.ReadNextString(packetData);

                            // MC 1.13+ Skip optional tooltip for each tab-complete result
                            if (DataTypes.ReadNextBool(packetData))
                                dataTypes.ReadNextChat(packetData);
                        }
                        handler.OnTabCompleteDone(completionStart, completionLength, completeResults);
                    }
                    break;
                case PacketTypesIn.PluginMessage:
                    {
                        var channel = DataTypes.ReadNextString(packetData);
                        // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                        //handler.OnPluginChannelMessage(channel, packetData.ToArray());
                        return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                    }
                case PacketTypesIn.Disconnect:
                    handler.OnConnectionLost(DisconnectReason.InGameKick,
                        dataTypes.ReadNextChat(packetData));
                    return false;
                case PacketTypesIn.SetCompression:
                    /* Legacy packet. Used in MC 1.8.X */
                    break;
                case PacketTypesIn.OpenInventory:
                    {
                        // MC 1.14 or greater
                        var inventoryId = DataTypes.ReadNextVarInt(packetData);
                        var inventoryType = DataTypes.ReadNextVarInt(packetData);
                        var title = dataTypes.ReadNextChat(packetData);
                        var inventory = new BaseInventory(inventoryId, inventoryType, title);
                        handler.OnInventoryOpen(inventoryId, inventory);
                    }
                    break;
                case PacketTypesIn.CloseInventory:
                    {
                        var inventoryId = DataTypes.ReadNextByte(packetData);
                        lock (inventoryActions) { inventoryActions[inventoryId] = 0; }
                        handler.OnInventoryClose(inventoryId);
                    }
                    break;
                case PacketTypesIn.InventoryItems:
                    {
                        var inventoryId = DataTypes.ReadNextByte(packetData);
                        var stateId = -1;
                        var elements = 0;

                        if (protocolVersion >= MC_1_17_1_Version)
                        {
                            // State ID and Elements as VarInt - 1.17.1 and above
                            stateId = DataTypes.ReadNextVarInt(packetData);
                            elements = DataTypes.ReadNextVarInt(packetData);
                        }
                        else
                        {
                            // Elements as Short - 1.17 and below
                            DataTypes.ReadNextShort(packetData);
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

                        handler.OnInventoryItems(inventoryId, inventorySlots, stateId);
                    }
                    break;
                case PacketTypesIn.InventoryProperty:
                    {
                        var inventoryId = DataTypes.ReadNextByte(packetData);
                        var propertyId = DataTypes.ReadNextShort(packetData);
                        var propertyValue = DataTypes.ReadNextShort(packetData);
                        handler.OnInventoryProperties(inventoryId, propertyId, propertyValue);
                    }
                    break;
                case PacketTypesIn.SetSlot:
                    {
                        var inventoryId = DataTypes.ReadNextByte(packetData);
                        var stateId = -1;
                        if (protocolVersion >= MC_1_17_1_Version)
                            stateId = DataTypes.ReadNextVarInt(packetData); // State ID - 1.17.1 and above
                        var slotId2 = DataTypes.ReadNextShort(packetData);
                        var item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                        handler.OnInventorySlot(inventoryId, slotId2, item!, stateId);
                    }
                    break;
                case PacketTypesIn.InventoryConfirmation:
                    {
                        var inventoryId = DataTypes.ReadNextByte(packetData);
                        var actionId = DataTypes.ReadNextShort(packetData);
                        var accepted = DataTypes.ReadNextBool(packetData);
                        if (!accepted)
                            SendInventoryConfirmation(inventoryId, actionId, accepted);
                    }
                    break;
                case PacketTypesIn.RemoveResourcePack:
                    if (DataTypes.ReadNextBool(packetData)) // Has UUID
                        DataTypes.ReadNextUUID(packetData); // UUID
                    break;
                case PacketTypesIn.ResourcePackSend:
                    HandleResourcePackPacket(packetData);
                    break;
                case PacketTypesIn.ResetScore:
                    DataTypes.ReadNextString(packetData); // Entity Name
                    if (DataTypes.ReadNextBool(packetData)) // Has Objective Name
                        DataTypes.ReadNextString(packetData); // Objective Name

                    break;
                case PacketTypesIn.SpawnEntity:
                    {
                        var entity = dataTypes.ReadNextEntity(packetData, EntityTypePalette.INSTANCE, false);

                        if (protocolVersion >= MC_1_20_2_Version)
                        {
                            if (entity.Type.TypeId == EntityType.PLAYER_ID)
                            {
                                handler.OnSpawnPlayer(entity.Id, entity.UUID, entity.Location, (byte)entity.Yaw, (byte)entity.Pitch);
                                break;
                            }
                        }

                        handler.OnSpawnEntity(entity);
                        
                        break;
                    }
                case PacketTypesIn.EntityEquipment:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);

                        bool hasNext;
                        do
                        {
                            var bitsData = DataTypes.ReadNextByte(packetData);
                            //  Top bit set if another entry follows, and otherwise unset if this is the last item in the array
                            hasNext = bitsData >> 7 == 1;
                            var slot2 = bitsData >> 1;
                            var item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                            handler.OnEntityEquipment(entityId, slot2, item!);
                        } while (hasNext);
                    }
                    break;
                case PacketTypesIn.SpawnLivingEntity:
                    {
                        EntityData entity = dataTypes.ReadNextEntity(packetData, EntityTypePalette.INSTANCE, true);
                        // packet before 1.15 has metadata at the end
                        // this is not handled in DataTypes.ReadNextEntity()
                        // we are simply ignoring leftover data in packet
                        handler.OnSpawnEntity(entity);
                    }
                    break;
                case PacketTypesIn.SpawnPlayer:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);
                        var UUID = DataTypes.ReadNextUUID(packetData);

                        var x = DataTypes.ReadNextDouble(packetData);
                        var y = DataTypes.ReadNextDouble(packetData);
                        var z = DataTypes.ReadNextDouble(packetData);

                        var yaw = DataTypes.ReadNextByte(packetData);
                        var pitch = DataTypes.ReadNextByte(packetData);
                        handler.OnSpawnPlayer(entityId, UUID, new(x, y, z), yaw, pitch);
                    }
                    break;
                case PacketTypesIn.SpawnExperienceOrb:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);
                        var x = DataTypes.ReadNextDouble(packetData);
                        var y = DataTypes.ReadNextDouble(packetData);
                        var z = DataTypes.ReadNextDouble(packetData);

                        DataTypes.ReadNextShort(packetData); // TODO Use this value
                        handler.OnSpawnEntity(new(entityId, EntityTypePalette.INSTANCE.GetById(EntityType.EXPERIENCE_ORB_ID),
                                new(x, y, z), 0, 0, 0, 0));
                    }
                    break;
                case PacketTypesIn.EntityEffect:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);
                        var effectId = protocolVersion >= MC_1_18_2_Version
                            ? DataTypes.ReadNextVarInt(packetData)
                            : DataTypes.ReadNextByte(packetData);

                        if (Enum.TryParse(effectId.ToString(), out Effects effect))
                        {
                            var amplifier = DataTypes.ReadNextByte(packetData);
                            var duration = DataTypes.ReadNextVarInt(packetData);
                            var flags = DataTypes.ReadNextByte(packetData);
                            var hasFactorData = false;
                            Dictionary<string, object>? factorCodec = null;

                            if (protocolVersion >= MC_1_19_Version && protocolVersion < MC_1_20_6_Version)
                            {
                                hasFactorData = DataTypes.ReadNextBool(packetData);
                                if (hasFactorData)
                                    factorCodec = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT);
                            }

                            handler.OnEntityEffect(entityId, effect, amplifier, duration, flags, hasFactorData,
                                factorCodec);
                        }
                    }
                    break;
                case PacketTypesIn.DestroyEntities:
                    {
                        var entityCount = 1; // 1.17.0 has only one entity per packet
                        if (protocolVersion != MC_1_17_Version)
                            entityCount =
                                DataTypes.ReadNextVarInt(packetData); // All other versions have a "count" field

                        var entityList = new int[entityCount];
                        for (int i = 0; i < entityCount; i++)
                            entityList[i] = DataTypes.ReadNextVarInt(packetData);
                        
                        handler.OnDestroyEntities(entityList);
                    }
                    break;
                case PacketTypesIn.EntityPosition:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);

                        var deltaX = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        var deltaY = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        var deltaZ = Convert.ToDouble(DataTypes.ReadNextShort(packetData));

                        var onGround = DataTypes.ReadNextBool(packetData);
                        deltaX /= (128 * 32);
                        deltaY /= (128 * 32);
                        deltaZ /= (128 * 32);

                        handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, onGround);
                    }
                    break;
                case PacketTypesIn.EntityPositionAndRotation:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);

                        var deltaX = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        var deltaY = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        var deltaZ = Convert.ToDouble(DataTypes.ReadNextShort(packetData));

                        var yaw = DataTypes.ReadNextByte(packetData);
                        var pitch = DataTypes.ReadNextByte(packetData);
                        var onGround = DataTypes.ReadNextBool(packetData);
                        deltaX /= (128 * 32);
                        deltaY /= (128 * 32);
                        deltaZ /= (128 * 32);

                        handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, onGround);
                        handler.OnEntityRotation(entityId, yaw, pitch, onGround);
                    }
                    break;
                case PacketTypesIn.EntityRotation:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);

                        var yaw = DataTypes.ReadNextByte(packetData);
                        var pitch = DataTypes.ReadNextByte(packetData);
                        var onGround = DataTypes.ReadNextBool(packetData);

                        handler.OnEntityRotation(entityId, yaw, pitch, onGround);
                    }
                    break;
                case PacketTypesIn.EntityHeadLook:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);
                        var headYaw = DataTypes.ReadNextByte(packetData);

                        handler.OnEntityHeadLook(entityId, headYaw);
                    }
                    break;
                case PacketTypesIn.EntityProperties:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);
                        var NumberOfProperties = protocolVersion >= MC_1_17_Version
                            ? DataTypes.ReadNextVarInt(packetData)
                            : DataTypes.ReadNextInt(packetData);

                        var attributeDictionary = new Dictionary<int, string>
                        {
                            { 0, "generic.armor" },
                            { 1, "generic.armor_toughness" },
                            { 2, "generic.attack_damage" },
                            { 3, "generic.attack_knockback" },
                            { 4, "generic.attack_speed" },
                            { 5, "generic.block_break_speed" },
                            { 6, "generic.block_interaction_range" },
                            { 7, "generic.entity_interaction_range" },
                            { 8, "generic.fall_damage_multiplier" },
                            { 9, "generic.flying_speed" },
                            { 10, "generic.follow_range" },
                            { 11, "generic.gravity" },
                            { 12, "generic.jump_strength" },
                            { 13, "generic.knockback_resistance" },
                            { 14, "generic.luck" },
                            { 15, "generic.max_absorption" },
                            { 16, "generic.max_health" },
                            { 17, "generic.movement_speed" },
                            { 18, "generic.safe_fall_distance" },
                            { 19, "generic.scale" },
                            { 20, "zombie.spawn_reinforcements" },
                            { 21, "generic.step_height" },
                            { 22, "generic.submerged_mining_speed" },
                            { 23, "generic.sweeping_damage_ratio" },
                            { 24, "generic.water_movement_efficiency" }
                        };

                        Dictionary<string, double> keys = new();
                        for (int i = 0; i < NumberOfProperties; i++)
                        {
                            var propertyKey = protocolVersion < MC_1_20_6_Version
                                ? DataTypes.ReadNextString(packetData) 
                                : attributeDictionary[DataTypes.ReadNextVarInt(packetData)];
                            var propertyValue = DataTypes.ReadNextDouble(packetData);

                            List<double> op0 = new();
                            List<double> op1 = new();
                            List<double> op2 = new();

                            var NumberOfModifiers = DataTypes.ReadNextVarInt(packetData);
                            for (var j = 0; j < NumberOfModifiers; j++)
                            {
                                DataTypes.ReadNextUUID(packetData);
                                var amount = DataTypes.ReadNextDouble(packetData);
                                var operation = DataTypes.ReadNextByte(packetData);
                                switch (operation)
                                {
                                    case 0:
                                        op0.Add(amount);
                                        break;
                                    case 1:
                                        op1.Add(amount);
                                        break;
                                    case 2:
                                        op2.Add(amount + 1);
                                        break;
                                }
                            }

                            if (op0.Count > 0) propertyValue += op0.Sum();
                            if (op1.Count > 0) propertyValue *= 1 + op1.Sum();
                            if (op2.Count > 0) propertyValue *= op2.Aggregate((a, _x) => a * _x);
                            keys.Add(propertyKey, propertyValue);
                        }

                        handler.OnEntityProperties(entityId, keys);
                    }
                    break;
                case PacketTypesIn.EntityMetadata:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);
                        var metadata = dataTypes.ReadNextMetadata(packetData,
                                ItemPalette.INSTANCE, entityMetadataPalette);
                            
                        handler.OnEntityMetadata(entityId, metadata);
                    }
                    break;
                case PacketTypesIn.EntityStatus:
                    {
                        var entityId = DataTypes.ReadNextInt(packetData);
                        var status = DataTypes.ReadNextByte(packetData);
                        handler.OnEntityStatus(entityId, status);
                    }
                    break;
                case PacketTypesIn.TimeUpdate:
                    {
                        var WorldAge = DataTypes.ReadNextLong(packetData);
                        var TimeOfday = DataTypes.ReadNextLong(packetData);
                        handler.OnTimeUpdate(WorldAge, TimeOfday);
                    }
                    break;
                case PacketTypesIn.EntityTeleport:
                    {
                        var entityId = DataTypes.ReadNextVarInt(packetData);

                        var x = DataTypes.ReadNextDouble(packetData);
                        var y = DataTypes.ReadNextDouble(packetData);
                        var z = DataTypes.ReadNextDouble(packetData);

                        var yaw = DataTypes.ReadNextByte(packetData);
                        var pitch = DataTypes.ReadNextByte(packetData);
                        var onGround = DataTypes.ReadNextBool(packetData);
                        handler.OnEntityTeleport(entityId, x, y, z, onGround);
                    }
                    break;
                case PacketTypesIn.UpdateHealth:
                    {
                        var health = DataTypes.ReadNextFloat(packetData);
                        var food = DataTypes.ReadNextVarInt(packetData);
                        DataTypes.ReadNextFloat(packetData); // Food Saturation
                        handler.OnUpdateHealth(health, food);
                    }
                    break;
                case PacketTypesIn.SetExperience:
                    {
                        var experiencebar = DataTypes.ReadNextFloat(packetData);
                        var level = DataTypes.ReadNextVarInt(packetData);
                        var totalexperience = DataTypes.ReadNextVarInt(packetData);
                        handler.OnSetExperience(experiencebar, level, totalexperience);
                    }
                    break;
                case PacketTypesIn.Explosion:
                    {
                        Location explosionLocation;
                        if (protocolVersion >= MC_1_19_3_Version)
                            explosionLocation = new(DataTypes.ReadNextDouble(packetData),
                                DataTypes.ReadNextDouble(packetData), DataTypes.ReadNextDouble(packetData));
                        else
                            explosionLocation = new(DataTypes.ReadNextFloat(packetData),
                                DataTypes.ReadNextFloat(packetData), DataTypes.ReadNextFloat(packetData));

                        var explosionStrength = DataTypes.ReadNextFloat(packetData);
                        var explosionBlockCount = protocolVersion >= MC_1_17_Version
                            ? DataTypes.ReadNextVarInt(packetData)
                            : DataTypes.ReadNextInt(packetData); // Record count

                        // Records
                        for (var i = 0; i < explosionBlockCount; i++)
                            DataTypes.ReadData(3, packetData);

                        // Maybe use in the future when the physics are implemented
                        DataTypes.ReadNextFloat(packetData); // Player Motion X
                        DataTypes.ReadNextFloat(packetData); // Player Motion Y
                        DataTypes.ReadNextFloat(packetData); // Player Motion Z

                        if (protocolVersion >= MC_1_20_4_Version)
                        {
                            var itemPalette = ItemPalette.INSTANCE; 

                            DataTypes.ReadNextVarInt(packetData); // Block Interaction
                            dataTypes.ReadParticleData(packetData, itemPalette); // Small Explosion Particles
                            dataTypes.ReadParticleData(packetData, itemPalette); // Large Explosion Particles

                            // Explosion Sound
                            DataTypes.ReadNextString(packetData); // Sound Id
                            var hasFixedRange = DataTypes.ReadNextBool(packetData);
                            if (hasFixedRange)
                                DataTypes.ReadNextFloat(packetData); // Range
                        }

                        handler.OnExplosion(explosionLocation, explosionStrength, explosionBlockCount);
                    }
                    break;
                case PacketTypesIn.HeldItemChange:
                    {
                        handler.OnHeldItemChange(DataTypes.ReadNextByte(packetData)); // Slot
                    }
                    break;
                case PacketTypesIn.ScoreboardObjective:
                    {
                        var objectiveName = DataTypes.ReadNextString(packetData);
                        var mode = DataTypes.ReadNextByte(packetData);

                        var objectiveValue = string.Empty;
                        var objectiveType = -1;
                        var numberFormat = 0;

                        if (mode is 0 or 2)
                        {
                            objectiveValue = dataTypes.ReadNextChat(packetData);
                            objectiveType = DataTypes.ReadNextVarInt(packetData);

                            if (protocolVersion >= MC_1_20_4_Version)
                            {
                                if (DataTypes.ReadNextBool(packetData)) // Has Number Format
                                    numberFormat = DataTypes.ReadNextVarInt(packetData); // Number Format
                            }
                        }

                        handler.OnScoreboardObjective(objectiveName, mode, objectiveValue, numberFormat);
                    }
                    break;
                case PacketTypesIn.UpdateScore:
                    {
                        var entityName = DataTypes.ReadNextString(packetData);

                        var action3 = 0;
                        var objectiveName3 = string.Empty;
                        var objectiveValue2 = -1;
                        var objectiveDisplayName3 = string.Empty;
                        var numberFormat2 = 0;

                        if (protocolVersion >= MC_1_20_4_Version)
                        {
                            objectiveName3 = DataTypes.ReadNextString(packetData); // Objective Name
                            objectiveValue2 = DataTypes.ReadNextVarInt(packetData); // Value

                            if (DataTypes.ReadNextBool(packetData)) // Has Display Name
                                objectiveDisplayName3 =
                                    ChatParser.ParseText(DataTypes.ReadNextString(packetData)); // Has Display Name

                            if (DataTypes.ReadNextBool(packetData)) // Has Number Format
                                numberFormat2 = DataTypes.ReadNextVarInt(packetData); // Number Format
                        }
                        else
                        {
                            action3 = protocolVersion >= MC_1_18_2_Version
                                ? DataTypes.ReadNextVarInt(packetData)
                                : DataTypes.ReadNextByte(packetData);

                            if (action3 != 1)
                                objectiveName3 = DataTypes.ReadNextString(packetData);

                            if (action3 != 1)
                                objectiveValue2 = DataTypes.ReadNextVarInt(packetData);
                        }

                        handler.OnUpdateScore(entityName, action3, objectiveName3, objectiveDisplayName3, objectiveValue2,
                            numberFormat2);
                    }
                    break;
                case PacketTypesIn.BlockChangedAck:
                    //handler.OnBlockChangeAck(DataTypes.ReadNextVarInt(packetData));
                    break;
                case PacketTypesIn.BlockBreakAnimation:
                    {
                        var playerId = DataTypes.ReadNextVarInt(packetData);
                        var blockLoc = DataTypes.ReadNextBlockLoc(packetData);
                        var stage = DataTypes.ReadNextByte(packetData);
                        handler.OnBlockBreakAnimation(playerId, blockLoc, stage);
                    }
                    break;
                case PacketTypesIn.EntityAnimation:
                    {
                        int playerId = DataTypes.ReadNextVarInt(packetData);
                        byte animation = DataTypes.ReadNextByte(packetData);
                        handler.OnEntityAnimation(playerId, animation);
                    }
                    break;
                case PacketTypesIn.OpenSignEditor: // TODO: Use
                    {
                        var signLocation = DataTypes.ReadNextBlockLoc(packetData);
                        var isFrontText = true;

                        if (protocolVersion >= MC_1_20_Version)
                            isFrontText = DataTypes.ReadNextBool(packetData);
                    }
                    break;
                case PacketTypesIn.SetTickingState:
                    DataTypes.ReadNextFloat(packetData);
                    DataTypes.ReadNextBool(packetData);
                    break;
                default:
                    return false; //Ignored packet
            }

            return true; //Packet processed
        }

        /// <summary>
        /// Start the updating thread. Should be called after login success.
        /// </summary>
        private void StartUpdating()
        {
            var threadUpdater = new Thread(new ParameterizedThreadStart(Updater))
            {
                Name = "ProtocolPacketHandler"
            };
            netMain = new Tuple<Thread, CancellationTokenSource>(threadUpdater, new());
            threadUpdater.Start(netMain.Item2.Token);

            var threadReader = new Thread(new ParameterizedThreadStart(PacketReader))
            {
                Name = "ProtocolPacketReader"
            };
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
                netMain?.Item2.Cancel();

                if (netReader != null)
                {
                    netReader.Item2.Cancel();
                    socketWrapper.Disconnect();
                }
            }
            catch { }
        }

        /// <summary>
        /// Send a packet to the server. Packet ID, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(PacketTypesOut packet, IEnumerable<byte> packetData)
        {
            SendPacket(packetPalette.GetOutgoingIdByType(packet), packetData);
        }

        /// <summary>
        /// Send a configuration packet to the server. Packet ID, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(ConfigurationPacketTypesOut packet, IEnumerable<byte> packetData)
        {
            SendPacket(packetPalette.GetOutgoingIdByTypeConfiguration(packet), packetData);
        }

        /// <summary>
        /// Send a packet to the server. Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetId">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetId, IEnumerable<byte> packetData)
        {
            if (ProtocolSettings.CapturePackets)
            {
                handler.OnNetworkPacket(packetId, packetData.ToArray(), currentState, false);
            }

            //log.Info($"[C -> S] Sending packet {packetId:X} > {DataTypes.ByteArrayToString(packetData.ToArray())}");

            //The inner packet
            var thePacket = DataTypes.ConcatBytes(DataTypes.GetVarInt(packetId), packetData.ToArray());

            if (compression_threshold >= 0) //Compression enabled?
            {
                thePacket = thePacket.Length >= compression_threshold
                    ? DataTypes.ConcatBytes(DataTypes.GetVarInt(thePacket.Length), ZlibUtils.Compress(thePacket))
                    : DataTypes.ConcatBytes(DataTypes.GetVarInt(0), thePacket);
            }

            //log.Debug("[C -> S] Sending packet " + packetId + " > " + DataTypes.ByteArrayToString(DataTypes.ConcatBytes(DataTypes.GetVarInt(thePacket.Length), thePacket)));
            socketWrapper.SendDataRAW(DataTypes.ConcatBytes(DataTypes.GetVarInt(thePacket.Length), thePacket));
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login(PlayerKeyPair? playerKeyPair, SessionToken session, string accountLower)
        {
            // 1. Send the handshake packet
            SendPacket(0x00, DataTypes.ConcatBytes(
                    // Protocol Version
                    DataTypes.GetVarInt(protocolVersion),

                    // Server Address
                    DataTypes.GetString(pForge.GetServerAddress(handler.GetServerHost())),

                    // Server Port
                    DataTypes.GetUShort((ushort)handler.GetServerPort()),

                    // Next State
                    DataTypes.GetVarInt(2)) // 2 is for the Login state
            );

            // 2. Send the Login Start packet
            List<byte> fullLoginPacket = new();
            fullLoginPacket.AddRange(DataTypes.GetString(handler.GetUsername())); // Username

            // 1.19 - 1.19.2
            if (protocolVersion is >= MC_1_19_Version and < MC_1_19_3_Version)
            {
                if (playerKeyPair == null)
                    fullLoginPacket.AddRange(DataTypes.GetBool(false)); // Has Sig Data
                else
                {
                    fullLoginPacket.AddRange(DataTypes.GetBool(true)); // Has Sig Data
                    fullLoginPacket.AddRange(
                        DataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds())); // Expiration time
                    fullLoginPacket.AddRange(
                        DataTypes.GetArray(playerKeyPair.PublicKey.Key)); // Public key received from Microsoft API
                    if (protocolVersion >= MC_1_19_2_Version)
                        fullLoginPacket.AddRange(
                            DataTypes.GetArray(playerKeyPair.PublicKey
                                .SignatureV2!)); // Public key signature received from Microsoft API
                    else
                        fullLoginPacket.AddRange(
                            DataTypes.GetArray(playerKeyPair.PublicKey
                                .Signature!)); // Public key signature received from Microsoft API
                }
            }

            var uuid = handler.GetUserUUID();
            switch (protocolVersion)
            {
                case >= MC_1_19_2_Version and < MC_1_20_2_Version:
                    {
                        if (uuid == Guid.Empty)
                            fullLoginPacket.AddRange(DataTypes.GetBool(false)); // Has UUID
                        else
                        {
                            fullLoginPacket.AddRange(DataTypes.GetBool(true)); // Has UUID
                            fullLoginPacket.AddRange(DataTypes.GetUUID(uuid)); // UUID
                        }

                        break;
                    }
                case >= MC_1_20_2_Version:
                    uuid = handler.GetUserUUID();

                    if (uuid == Guid.Empty)
                        uuid = Guid.NewGuid();

                    fullLoginPacket.AddRange(DataTypes.GetUUID(uuid)); // UUID
                    break;
            }

            SendPacket(0x00, fullLoginPacket);

            // 3. Encryption Request - 9. Login Acknowledged
            while (true)
            {
                var (packetId, packetData) = ReadNextPacket();

                switch (packetId)
                {
                    // Login rejected
                    case 0x00:
                        handler.OnConnectionLost(DisconnectReason.LoginRejected,
                            ChatParser.ParseText(DataTypes.ReadNextString(packetData)));
                        return false;

                    // Encryption request
                    case 0x01:
                        {
                            isOnlineMode = true;
                            var serverId = DataTypes.ReadNextString(packetData);
                            var serverPublicKey = DataTypes.ReadNextByteArray(packetData);
                            var token = DataTypes.ReadNextByteArray(packetData);

                            var shouldAuthenticate = false;

                            if (protocolVersion >= MC_1_20_6_Version)
                                shouldAuthenticate = DataTypes.ReadNextBool(packetData);

                            return StartEncryption(accountLower, handler.GetUserUUIDStr(),
                                handler.GetSessionId(), token, serverId, serverPublicKey,
                                playerKeyPair, session, shouldAuthenticate);
                        }

                    // Login successful
                    case 0x02:
                        {
                            Debug.Log(Translations.Get("mcc.server_offline"));
                            currentState = protocolVersion < MC_1_20_2_Version
                                ? CurrentState.Play
                                : CurrentState.Configuration;

                            if (protocolVersion >= MC_1_20_2_Version)
                                SendPacket(0x03, new List<byte>());

                            if (!pForge.CompleteForgeHandshake())
                            {
                        Debug.LogError(Translations.Get("error.forge"));
                                return false;
                            }

                            StartUpdating();
                            return true; //No need to check session or start encryption
                        }
                    default:
                        HandlePacket(packetId, packetData);
                        break;
                }
            }
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>
        private bool StartEncryption(string accountLower, string uuid, string sessionId, byte[] token, string serverIdhash,
                byte[] serverPublicKey, PlayerKeyPair? playerKeyPair, SessionToken session, bool shouldAuthenticate)
        {
            RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey);
            byte[] secretKey = CryptoHandler.ClientAESPrivateKey ?? CryptoHandler.GenerateAESPrivateKey();

            Debug.Log(Translations.Get("debug.crypto"));

            if (serverIdhash != "-")
            {
                Debug.Log(Translations.Get("mcc.session"));

                bool needCheckSession = true;
                if (session.ServerPublicKey != null && session.SessionPreCheckTask != null
                        && serverIdhash == session.ServerIdHash &&
                        Enumerable.SequenceEqual(serverPublicKey, session.ServerPublicKey))
                {
                    session.SessionPreCheckTask.Wait();
                    if (session.SessionPreCheckTask.Result) // PreCheck Successed
                        needCheckSession = false;
                }

                // 1.20.6+
                if (shouldAuthenticate)
                    needCheckSession = true;

                if (needCheckSession)
                {
                    string serverHash = CryptoHandler.GetServerHash(serverIdhash, serverPublicKey, secretKey);

                    if (ProtocolHandler.SessionCheck(uuid, sessionId, serverHash))
                    {
                        session.ServerIdHash = serverIdhash;
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
            encryptionResponse.AddRange(DataTypes.GetArray(RSAService.Encrypt(secretKey, false)));     // Shared Secret
            
            // 1.19 - 1.19.2
            if (protocolVersion >= MC_1_19_Version && protocolVersion < MC_1_19_3_Version)
            {
                if (playerKeyPair is null)
                {
                    encryptionResponse.AddRange(DataTypes.GetBool(true));                              // Has Verify Token
                    encryptionResponse.AddRange(DataTypes.GetArray(RSAService.Encrypt(token, false))); // Verify Token
                }
                else
                {
                    byte[] salt = GenerateSalt();
                    byte[] messageSignature = playerKeyPair.PrivateKey.SignData(DataTypes.ConcatBytes(token, salt));

                    encryptionResponse.AddRange(DataTypes.GetBool(false));                            // Has Verify Token
                    encryptionResponse.AddRange(salt);                                                // Salt
                    encryptionResponse.AddRange(DataTypes.GetArray(messageSignature));                // Message Signature
                }
            }
            else
            {
                encryptionResponse.AddRange(DataTypes.GetArray(RSAService.Encrypt(token, false)));    // Verify Token
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

                switch (packetId)
                {
                    //Login rejected
                    case 0x00:
                        handler.OnConnectionLost(DisconnectReason.LoginRejected,
                            ChatParser.ParseText(DataTypes.ReadNextString(packetData)));
                        return false;
                    //Login successful
                    case 0x02:
                        {
                            var uuidReceived = DataTypes.ReadNextUUID(packetData);
                            var userName = DataTypes.ReadNextString(packetData);
                            Tuple<string, string, string>[]? playerProperty = null;
                            if (protocolVersion >= MC_1_19_Version)
                            {
                                var count = DataTypes.ReadNextVarInt(packetData); // Number Of Properties
                                playerProperty = new Tuple<string, string, string>[count];
                                for (var i = 0; i < count; ++i)
                                {
                                    var name = DataTypes.ReadNextString(packetData);
                                    var value = DataTypes.ReadNextString(packetData);
                                    var isSigned = DataTypes.ReadNextBool(packetData);
                                    var signature = isSigned ? DataTypes.ReadNextString(packetData) : string.Empty;
                                    playerProperty[i] = new Tuple<string, string, string>(name, value, signature);
                                }
                            }

                            // Strict Error Handling (Ignored)
                            if (protocolVersion >= MC_1_20_6_Version)
                                DataTypes.ReadNextBool(packetData);

                            currentState = protocolVersion < MC_1_20_2_Version
                                ? CurrentState.Play
                                : CurrentState.Configuration;

                            if (protocolVersion >= MC_1_20_2_Version)
                                SendPacket(0x03, new List<byte>());

                            handler.OnLoginSuccess(uuidReceived, userName, playerProperty);

                            if (!pForge.CompleteForgeHandshake())
                            {
                                Debug.Log(Translations.Get("error.forge_encrypt"));
                                return false;
                            }

                            StartUpdating();
                            return true;
                        }
                    default:
                        HandlePacket(packetId, packetData);
                        break;
                }
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
        public static bool DoPing(string host, int port, ref string versionName, ref int protocol, ref ForgeInfo? forgeInfo)
        {
            var tcp = ProxyHandler.NewTcpClient(host, port);
            tcp.ReceiveTimeout = 30000; // 30 seconds
            tcp.ReceiveBufferSize = 1024 * 1024;
            var socketWrapper = new SocketWrapper(tcp);

            byte[] packetId = DataTypes.GetVarInt(0);
            byte[] protocolVersion = DataTypes.GetVarInt(-1);
            byte[] serverPort = BitConverter.GetBytes((ushort)port); Array.Reverse(serverPort);
            byte[] nextState  = DataTypes.GetVarInt(1);
            byte[] packet = DataTypes.ConcatBytes(packetId, protocolVersion, DataTypes.GetString(host), serverPort, nextState);
            byte[] tosend = DataTypes.ConcatBytes(DataTypes.GetVarInt(packet.Length), packet);

            socketWrapper.SendDataRAW(tosend);

            byte[] statusRequest = DataTypes.GetVarInt(0);
            byte[] requestPacket = DataTypes.ConcatBytes(DataTypes.GetVarInt(statusRequest.Length), statusRequest);

            socketWrapper.SendDataRAW(requestPacket);

            int packetLength = DataTypes.ReadNextVarIntRAW(socketWrapper);
            if (packetLength > 0) // Read Response length
            {
                Queue<byte> packetData = new(socketWrapper.ReadDataRAW(packetLength));
                if (DataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet Id
                {
                    string result = DataTypes.ReadNextString(packetData); //Get the Json data

                    if (ProtocolSettings.DebugMode)
                        Debug.Log(result);

                    if (!string.IsNullOrEmpty(result) && result.StartsWith("{") && result.EndsWith("}"))
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
                    isOnlineMode && ProtocolSettings.LoginWithSecureProfile);

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
            if (string.IsNullOrEmpty(command))
                return true;

            command = Regex.Replace(command, @"\s+", " ");
            command = Regex.Replace(command, @"\s$", string.Empty);

            //Debug.Log("chat command = " + command);

            if (protocolVersion >= MC_1_20_6_Version && !isOnlineMode)
            {
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetString(command));
                SendPacket(PacketTypesOut.ChatCommand, fields);
                return true;
            }

            try
            {
                List<Tuple<string, string>>? needSigned = null; // List< Argument Name, Argument Value >
                if (playerKeyPair != null && isOnlineMode && protocolVersion >= MC_1_19_Version
                    && ProtocolSettings.LoginWithSecureProfile && ProtocolSettings.SignMessageInCommand)
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
                    fields.AddRange(DataTypes.GetString(command));

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
                        Guid uuid = handler.GetUserUUID();
                        byte[] salt = GenerateSalt();
                        fields.AddRange(salt); // Salt: Long
                        fields.AddRange(DataTypes.GetVarInt(needSigned.Count)); // Signature Length: VarInt
                        foreach ((string argName, string message) in needSigned)
                        {
                            fields.AddRange(DataTypes.GetString(argName)); // Argument name: String

                            byte[] sign;
                            if (protocolVersion == MC_1_19_Version)
                                sign = playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt);
                            else if (protocolVersion == MC_1_19_2_Version)
                                sign = playerKeyPair!.PrivateKey.SignMessage(message, uuid, timeNow, ref salt,
                                    acknowledgment_1_19_2!.lastSeen);
                            else // protocolVersion >= MC_1_19_3_Version
                                sign = playerKeyPair!.PrivateKey.SignMessage(message, uuid, chatUUID, messageIndex++,
                                    timeNow, ref salt, acknowledgment_1_19_3);

                            if (protocolVersion <= MC_1_19_2_Version)
                                fields.AddRange(DataTypes.GetVarInt(sign.Length)); // Signature length: VarInt

                            fields.AddRange(sign); // Signature: Byte Array
                        }
                    }

                    if (protocolVersion <= MC_1_19_2_Version)
                        fields.AddRange(DataTypes.GetBool(false)); // Signed Preview: Boolean

                    if (protocolVersion == MC_1_19_2_Version)
                    {
                        // Message Acknowledgment (1.19.2)
                        fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment_1_19_2!,
                            isOnlineMode && ProtocolSettings.LoginWithSecureProfile));
                    }
                    else if (protocolVersion >= MC_1_19_3_Version)
                    {
                        // message count
                        fields.AddRange(DataTypes.GetVarInt(messageCount_1_19_3));

                        // Acknowledged: BitSet
                        fields.AddRange(bitset_1_19_3);
                    }

                    SendPacket(protocolVersion < MC_1_20_6_Version ? PacketTypesOut.ChatCommand : PacketTypesOut.SignedChatCommand, fields);
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
                fields.AddRange(DataTypes.GetString(message));

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

                        if (!isOnlineMode || playerKeyPair == null || !ProtocolSettings.LoginWithSecureProfile ||
                            !ProtocolSettings.SignChat)
                        {
                            fields.AddRange(DataTypes.GetLong(0)); // Salt: Long
                            if (protocolVersion < MC_1_19_3_Version)
                                fields.AddRange(DataTypes.GetVarInt(0)); // Signature Length: VarInt (1.19 - 1.19.2)
                            else
                                fields.AddRange(DataTypes.GetBool(false)); // Has signature: bool (1.19.3)
                        }
                        else
                        {
                            // Salt: Long
                            byte[] salt = GenerateSalt();
                            fields.AddRange(salt);

                            // Signature Length & Signature: (VarInt) and Byte Array
                            Guid playerUUID = handler.GetUserUUID();
                            byte[] sign;
                            if (protocolVersion == MC_1_19_Version) // 1.19.1 or lower
                                sign = playerKeyPair.PrivateKey.SignMessage(message, playerUUID, timeNow, ref salt);
                            else if (protocolVersion == MC_1_19_2_Version) // 1.19.2
                                sign = playerKeyPair.PrivateKey.SignMessage(message, playerUUID, timeNow, ref salt,
                                    acknowledgment_1_19_2!.lastSeen);
                            else // protocolVersion >= MC_1_19_3_Version
                                sign = playerKeyPair.PrivateKey.SignMessage(message, playerUUID, chatUUID,
                                    messageIndex++, timeNow, ref salt, acknowledgment_1_19_3);

                            if (protocolVersion >= MC_1_19_3_Version)
                                fields.AddRange(DataTypes.GetBool(true));
                            else
                                fields.AddRange(DataTypes.GetVarInt(sign.Length));
                            fields.AddRange(sign);
                        }

                        if (protocolVersion <= MC_1_19_2_Version)
                            fields.AddRange(DataTypes.GetBool(false)); // Signed Preview: Boolean

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
                                isOnlineMode && ProtocolSettings.LoginWithSecureProfile));
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
            if (string.IsNullOrEmpty(text))
                return false;
            try
            {
                byte[] transactionId = DataTypes.GetVarInt(autocomplete_transaction_id);
                byte[] requestPacket = new byte[] { };

                requestPacket = DataTypes.ConcatBytes(requestPacket, transactionId);
                requestPacket = DataTypes.ConcatBytes(requestPacket, DataTypes.GetString(text));

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
                List<byte> fields = new();
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
            return SendPluginChannelPacket("minecraft:brand", DataTypes.GetString(brandInfo));
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
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetString(language));
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
                yawpitch = DataTypes.ConcatBytes(DataTypes.GetFloat(yaw.Value), DataTypes.GetFloat(pitch.Value));
                packetType = PacketTypesOut.PlayerPositionAndRotation;
            }

            try
            {
                SendPacket(packetType, DataTypes.ConcatBytes(
                    DataTypes.GetDouble(location.X),
                    DataTypes.GetDouble(location.Y),
                    new byte[0],
                    DataTypes.GetDouble(location.Z),
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
                SendPacket(PacketTypesOut.PluginMessage, DataTypes.ConcatBytes(DataTypes.GetString(channel), data));
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
                SendPacket(0x02, DataTypes.ConcatBytes(DataTypes.GetVarInt(messageId), DataTypes.GetBool(understood), data));
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
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(entityId));
                fields.AddRange(DataTypes.GetVarInt(type));

                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                fields.AddRange(DataTypes.GetBool(false));

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
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(entityId));
                fields.AddRange(DataTypes.GetVarInt(type));
                fields.AddRange(DataTypes.GetFloat(X));
                fields.AddRange(DataTypes.GetFloat(Y));
                fields.AddRange(DataTypes.GetFloat(Z));
                fields.AddRange(DataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                fields.AddRange(DataTypes.GetBool(false));
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
                List<byte> fields = new();
                fields.AddRange(DataTypes.GetVarInt(entityId));
                fields.AddRange(DataTypes.GetVarInt(type));
                fields.AddRange(DataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                fields.AddRange(DataTypes.GetBool(false));
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
                List<byte> packet = new();
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
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetVarInt(status));
                packet.AddRange(DataTypes.GetBlockLoc(blockLoc));
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

        // Shares same packet id with player digging. See https://wiki.vg/Protocol#Player_Action
        public bool SendPlayerAction(int status)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetVarInt(status));
                packet.AddRange(DataTypes.GetBlockLoc(BlockLoc.Zero)); // Location is always set to 0/0/0
                packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(Direction.Down))); // Face is always set to -Y
                if (protocolVersion >= MC_1_19_Version)
                    packet.AddRange(DataTypes.GetVarInt(0)); // Sequence is always set to 0
                SendPacket(PacketTypesOut.PlayerDigging, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerBlockPlacement(int hand, BlockLoc location, float x, float y, float z, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetVarInt(hand));
                packet.AddRange(DataTypes.GetBlockLoc(location));
                packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                packet.AddRange(DataTypes.GetFloat(x)); // cursorX
                packet.AddRange(DataTypes.GetFloat(y)); // cursorY
                packet.AddRange(DataTypes.GetFloat(z)); // cursorZ
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
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetShort(slot));
                SendPacket(PacketTypesOut.HeldItemChange, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendInventoryAction(int inventoryId, int slotId, InventoryActionType action, ItemStack? item, List<Tuple<short, ItemStack?>> changedSlots, int stateId)
        {
            try
            {
                var itemPalette = ItemPalette.INSTANCE;

                short actionNumber;
                lock (inventoryActions)
                {
                    inventoryActions.TryAdd(inventoryId, 0);
                    actionNumber = (short)(inventoryActions[inventoryId] + 1);
                    inventoryActions[inventoryId] = actionNumber;
                }

                byte button = 0;
                byte mode = 0;

                switch (action)
                {
                    case InventoryActionType.LeftClick:
                        button = 0;
                        break;
                    case InventoryActionType.RightClick:
                        button = 1;
                        break;
                    case InventoryActionType.MiddleClick:
                        button = 2;
                        mode = 3;
                        break;
                    case InventoryActionType.ShiftClick:
                        button = 0;
                        mode = 1;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case InventoryActionType.ShiftRightClick: // Right-shift click uses button 1
                        button = 1;
                        mode = 1;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case InventoryActionType.DropItem:
                        button = 0;
                        mode = 4;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case InventoryActionType.DropItemStack:
                        button = 1;
                        mode = 4;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case InventoryActionType.StartDragLeft:
                        button = 0;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case InventoryActionType.StartDragRight:
                        button = 4;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case InventoryActionType.StartDragMiddle:
                        button = 8;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case InventoryActionType.EndDragLeft:
                        button = 2;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case InventoryActionType.EndDragRight:
                        button = 6;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case InventoryActionType.EndDragMiddle:
                        button = 10;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case InventoryActionType.AddDragLeft:
                        button = 1;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case InventoryActionType.AddDragRight:
                        button = 5;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case InventoryActionType.AddDragMiddle:
                        button = 9;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                }

                List<byte> packet = new()
                {
                    (byte)inventoryId // Inventory Id
                };

                switch (protocolVersion)
                {
                    // 1.18+
                    case >= MC_1_18_1_Version:
                        packet.AddRange(DataTypes.GetVarInt(stateId)); // State ID
                        packet.AddRange(DataTypes.GetShort((short)slotId)); // Slot ID
                        break;
                    // 1.17.1
                    case MC_1_17_1_Version:
                        packet.AddRange(DataTypes.GetShort((short)slotId)); // Slot ID
                        packet.AddRange(DataTypes.GetVarInt(stateId)); // State ID
                        break;
                    // Older
                    default:
                        packet.AddRange(DataTypes.GetShort((short)slotId)); // Slot ID
                        break;
                }

                packet.Add(button); // Button

                if (protocolVersion < MC_1_17_Version)
                    packet.AddRange(DataTypes.GetShort(actionNumber));

                packet.AddRange(DataTypes.GetVarInt(mode)); // 1.9+  Mode

                // 1.17+  Array of changed slots
                if (protocolVersion >= MC_1_17_Version)
                {
                    packet.AddRange(DataTypes.GetVarInt(changedSlots.Count)); // Length of the array
                    foreach (var slot in changedSlots)
                    {
                        packet.AddRange(DataTypes.GetShort(slot.Item1)); // slot ID
                        packet.AddRange(dataTypes.GetItemSlot(slot.Item2, itemPalette)); // slot Data
                    }
                }

                packet.AddRange(dataTypes.GetItemSlot(item, itemPalette)); // Carried item (Clicked item)

                SendPacket(PacketTypesOut.ClickInventory, packet);
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

        public bool SendCreativeInventoryAction(int slot, Item itemType, int count, Dictionary<string, object>? nbt)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetShort((short)slot));
                packet.AddRange(dataTypes.GetItemSlot(new ItemStack(itemType, count, nbt), ItemPalette.INSTANCE));
                SendPacket(PacketTypesOut.CreativeInventoryAction, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendChunkBatchReceived(float desiredNumberOfChunksPerBatch)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetFloat(desiredNumberOfChunksPerBatch));
                SendPacket(PacketTypesOut.ChunkBatchReceived, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendAcknowledgeConfiguration()
        {
            try
            {
                SendPacket(PacketTypesOut.AcknowledgeConfiguration, new List<byte>());
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool ClickInventoryButton(int inventoryId, int buttonId)
        {
            try
            {
                var packet = new List<byte>
                {
                    (byte)inventoryId,
                    (byte)buttonId
                };
                SendPacket(PacketTypesOut.ClickInventoryButton, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendAnimation(int animation, int playerId)
        {
            try
            {
                if (animation is 0 or 1)
                {
                    List<byte> packet = new();
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

        public bool SendCloseInventory(int inventoryId)
        {
            try
            {
                lock (inventoryActions)
                {
                    if (inventoryActions.ContainsKey(inventoryId))
                        inventoryActions[inventoryId] = 0;
                }
                SendPacket(PacketTypesOut.CloseInventory, new[] { (byte)inventoryId });
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
                    line1 = line1[..23];
                if (line2.Length > 23)
                    line2 = line1[..23];
                if (line3.Length > 23)
                    line3 = line1[..23];
                if (line4.Length > 23)
                    line4 = line1[..23];

                List<byte> packet = new();
                packet.AddRange(DataTypes.GetLocation(sign));
                packet.AddRange(DataTypes.GetString(line1));
                packet.AddRange(DataTypes.GetString(line2));
                packet.AddRange(DataTypes.GetString(line3));
                packet.AddRange(DataTypes.GetString(line4));
                SendPacket(PacketTypesOut.UpdateSign, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendInventoryConfirmation(byte inventoryId, short actionId, bool accepted)
        {
            try
            {
                List<byte> packet = new()
                {
                    inventoryId
                };
                packet.AddRange(DataTypes.GetShort(actionId));
                packet.Add(accepted ? (byte)1 : (byte)0);
                SendPacket(PacketTypesOut.InventoryConfirmation, packet);
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
                List<byte> packet = new();
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
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetUUID(UUID));
                SendPacket(PacketTypesOut.Spectate, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerSession(PlayerKeyPair? playerKeyPair)
        {
            if (playerKeyPair == null || !isOnlineMode)
                return false;

            if (protocolVersion >= MC_1_19_3_Version)
            {
                try
                {
                    List<byte> packet = new();

                    packet.AddRange(DataTypes.GetUUID(chatUUID));
                    packet.AddRange(DataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds()));
                    packet.AddRange(DataTypes.GetVarInt(playerKeyPair.PublicKey.Key.Length));
                    packet.AddRange(playerKeyPair.PublicKey.Key);
                    packet.AddRange(DataTypes.GetVarInt(playerKeyPair.PublicKey.SignatureV2!.Length));
                    packet.AddRange(playerKeyPair.PublicKey.SignatureV2);

                    Debug.Log($"SendPlayerSession MessageUUID = {chatUUID},  len(PublicKey) = {playerKeyPair.PublicKey.Key.Length}, len(SignatureV2) = {playerKeyPair.PublicKey.SignatureV2!.Length}");

                    SendPacket(PacketTypesOut.PlayerSession, packet);
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

            return false;
        }

        public bool SendRenameItem(string itemName)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetString(itemName.Length > 50 ? itemName[..50] : itemName));
                SendPacket(PacketTypesOut.NameItem, packet);
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

        public bool SendCookieResponse(string name, byte[]? data)
        {
            try
            {
                var packet = new List<byte>();
                var hasPayload = data is not null;
                packet.AddRange(DataTypes.GetString(name)); // Identifier
                packet.AddRange(DataTypes.GetBool(hasPayload)); // Has payload
                
                if (hasPayload)
                    packet.AddRange(DataTypes.GetArray(data!)); // Payload Data Array Size + Data Array

                switch (currentState)
                {
                    case CurrentState.Login:
                        SendPacket(0x04, packet);
                        break;

                    case CurrentState.Configuration:
                        SendPacket(ConfigurationPacketTypesOut.CookieResponse, packet);
                        break;

                    case CurrentState.Play:
                        SendPacket(PacketTypesOut.CookieResponse, packet);
                        break;
                }
                
                handler.DeleteCookie(name);
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

        public bool SendKnownDataPacks(List<(string, string, string)> knownDataPacks)
        {
            try
            {
                var packet = new List<byte>();
                packet.AddRange(DataTypes.GetVarInt(knownDataPacks.Count)); // Known Packs Count
                foreach (var dataPack in knownDataPacks)
                {
                    packet.AddRange(DataTypes.GetString(dataPack.Item1));
                    packet.AddRange(DataTypes.GetString(dataPack.Item2));
                    packet.AddRange(DataTypes.GetString(dataPack.Item3));
                }

                switch (currentState)
                {
                    case CurrentState.Configuration: 
                        SendPacket(ConfigurationPacketTypesOut.KnownDataPacks, packet);
                        break;
                    
                    case CurrentState.Play:
                        SendPacket(PacketTypesOut.KnownDataPacks, packet);
                        break;
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

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[8];
            randomGen.GetNonZeroBytes(salt);
            return salt;
        }

        private static long GetNanos()
        {
            var nano = 10000L * System.Diagnostics.Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }
    }

    public enum CurrentState
    {
        Login = 0,
        Configuration,
        Play
    }
}
