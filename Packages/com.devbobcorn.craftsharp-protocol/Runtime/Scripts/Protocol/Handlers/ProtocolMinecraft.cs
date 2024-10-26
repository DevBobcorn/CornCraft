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
        public const int MC_1_15_Version   = 573;
        public const int MC_1_15_2_Version = 578;
        public const int MC_1_16_Version   = 735;
        public const int MC_1_16_1_Version = 736;
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

        private int compression_threshold = -1;
        private int autocomplete_transaction_id = 0;
        private readonly Dictionary<int, short> window_actions = new();
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
        private Guid chatUuid = Guid.NewGuid();
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

                            // Ignore other packets at this stage
                            default:
                                return true;
                        }

                        break;

                    // https://wiki.vg/Protocol#Configuration
                    case CurrentState.Configuration:
                        switch (packetPalette.GetIncomingConfigurationTypeById(packetId))
                        {
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

                                if (protocolVersion <= MC_1_20_4_Version) // Different registries are wrapped in one nbt structure
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
                        handler.OnReceivePlayerEntityID(playerEntityId);

                        if (protocolVersion >= MC_1_16_2_Version)
                            DataTypes.ReadNextBool(packetData);                       // Is hardcore - 1.16.2 and above

                        if (protocolVersion < MC_1_20_2_Version)
                            handler.OnGamemodeUpdate(Guid.Empty, DataTypes.ReadNextByte(packetData));

                        if (protocolVersion >= MC_1_16_Version)
                        {
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
                        }

                        if (protocolVersion < MC_1_20_2_Version)
                        {
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
                                    dimensionTypeName = DataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                else if (protocolVersion >= MC_1_16_2_Version)
                                    dimensionType = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT); // Dimension Type: NBT Tag Compound
                                else
                                    DataTypes.ReadNextString(packetData);
                                this.currentDimension = 0;
                            }
                            else
                                this.currentDimension = DataTypes.ReadNextInt(packetData);

                            if (protocolVersion >= MC_1_16_Version)
                            {
                                string dimensionName = DataTypes.ReadNextString(packetData); // Dimension Id (World Id) - 1.16 and above
                                var dimensionId = ResourceLocation.FromString(dimensionName);

                                if (protocolVersion >= MC_1_16_2_Version && protocolVersion <= MC_1_18_2_Version)
                                {
                                    // Store the received dimension type with received dimension id
                                    World.StoreOneDimensionType(dimensionId, dimensionType!);

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
                        }

                        if (protocolVersion is >= MC_1_15_Version and < MC_1_20_2_Version)
                            DataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above

                        if (protocolVersion >= MC_1_16_2_Version)
                            DataTypes.ReadNextVarInt(packetData);         // Max Players - 1.16.2 and above
                        else
                            DataTypes.ReadNextByte(packetData);           // Max Players - 1.16.1 and below

                        if (protocolVersion < MC_1_16_Version)
                            DataTypes.SkipNextString(packetData);         // Level Type - 1.15 and below
                            
                        DataTypes.ReadNextVarInt(packetData);             // View distance - 1.14 and above
                            
                        if (protocolVersion >= MC_1_18_1_Version)
                            DataTypes.ReadNextVarInt(packetData);         // Simulation Distance - 1.18 and above
                            
                        DataTypes.ReadNextBool(packetData);               // Reduced debug info - 1.8 and above

                        if (protocolVersion >= MC_1_15_Version)
                            DataTypes.ReadNextBool(packetData);           // Enable respawn screen - 1.15 and above

                        if (protocolVersion < MC_1_20_2_Version)
                        {
                            if (protocolVersion >= MC_1_16_Version)
                            {
                                DataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                                DataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                            }
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
                            var dimensionTypeName = DataTypes.ReadNextString(packetData); // Dimension Type: Identifier
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
                            string message = DataTypes.ReadNextString(packetData);

                            Guid senderUuid;
                            //Hide system messages or xp bar messages?
                            messageType = DataTypes.ReadNextByte(packetData);
                            if ((messageType == 1 && !ProtocolSettings.DisplaySystemMessages)
                                || (messageType == 2 && !ProtocolSettings.DisplayXpBarMessages))
                                break;

                            if (protocolVersion >= MC_1_16_5_Version)
                                senderUuid = DataTypes.ReadNextUUID(packetData);
                            else senderUuid = Guid.Empty;

                            handler.OnTextReceived(new(message, null, true, messageType, senderUuid));
                        }
                        else if (protocolVersion == MC_1_19_Version) // 1.19
                        {
                            string signedChat = DataTypes.ReadNextString(packetData);

                            bool hasUnsignedChatContent = DataTypes.ReadNextBool(packetData);
                            string? unsignedChatContent =
                                hasUnsignedChatContent ? DataTypes.ReadNextString(packetData) : null;

                            messageType = DataTypes.ReadNextVarInt(packetData);
                            if ((messageType == 1 && !ProtocolSettings.DisplaySystemMessages)
                                || (messageType == 2 && !ProtocolSettings.DisplayXpBarMessages))
                                break;

                            Guid senderUuid = DataTypes.ReadNextUUID(packetData);
                            string senderDisplayName = ChatParser.ParseText(DataTypes.ReadNextString(packetData));

                            bool hasSenderTeamName = DataTypes.ReadNextBool(packetData);
                            string? senderTeamName = hasSenderTeamName
                                ? ChatParser.ParseText(DataTypes.ReadNextString(packetData))
                                : null;

                            long timestamp = DataTypes.ReadNextLong(packetData);

                            long salt = DataTypes.ReadNextLong(packetData);

                            byte[] messageSignature = DataTypes.ReadNextByteArray(packetData);

                            bool verifyResult;
                            if (!isOnlineMode)
                                verifyResult = false;
                            else if (senderUuid == handler.GetUserUuid())
                                verifyResult = true;
                            else
                            {
                                PlayerInfo? player = handler.GetPlayerInfo(senderUuid);
                                verifyResult = player != null && player.VerifyMessage(signedChat, timestamp, salt,
                                    ref messageSignature);
                            }

                            ChatMessage chat = new(signedChat, true, messageType, senderUuid, unsignedChatContent,
                                senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult);
                            handler.OnTextReceived(chat);
                        }
                        else if (protocolVersion == MC_1_19_2_Version)
                        {
                            // 1.19.1 - 1.19.2
                            byte[]? precedingSignature = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextByteArray(packetData)
                                : null;
                            Guid senderUuid = DataTypes.ReadNextUUID(packetData);
                            byte[] headerSignature = DataTypes.ReadNextByteArray(packetData);

                            string signedChat = DataTypes.ReadNextString(packetData);
                            string? decorated = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextString(packetData)
                                : null;

                            long timestamp = DataTypes.ReadNextLong(packetData);
                            long salt = DataTypes.ReadNextLong(packetData);

                            int lastSeenMessageListLen = DataTypes.ReadNextVarInt(packetData);
                            LastSeenMessageList.AcknowledgedMessage[] lastSeenMessageList =
                                new LastSeenMessageList.AcknowledgedMessage[lastSeenMessageListLen];
                            for (int i = 0; i < lastSeenMessageListLen; ++i)
                            {
                                Guid user = DataTypes.ReadNextUUID(packetData);
                                byte[] lastSignature = DataTypes.ReadNextByteArray(packetData);
                                lastSeenMessageList[i] = new(user, lastSignature, true);
                            }

                            LastSeenMessageList lastSeenMessages = new(lastSeenMessageList);

                            string? unsignedChatContent = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextString(packetData)
                                : null;

                            MessageFilterType filterEnum = (MessageFilterType)DataTypes.ReadNextVarInt(packetData);
                            if (filterEnum == MessageFilterType.PartiallyFiltered)
                                DataTypes.ReadNextULongArray(packetData);

                            int chatTypeId = DataTypes.ReadNextVarInt(packetData);
                            string chatName = DataTypes.ReadNextString(packetData);
                            string? targetName = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextString(packetData)
                                : null;

                            Dictionary<string, Json.JSONData> chatInfo = Json.ParseJson(chatName).Properties;
                            string senderDisplayName =
                                (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"])
                                .StringValue;
                            string? senderTeamName = null;
                            if (!ChatParser.MessageTypeRegistry.TryGetByNumId(chatTypeId, out ChatParser.MessageType messageTypeEnum))
                            {
                                messageTypeEnum = ChatParser.MessageType.CHAT;
                            }
                            if (targetName != null &&
                                messageTypeEnum is ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING or ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING)
                                senderTeamName = Json.ParseJson(targetName).Properties["with"].DataArray[0]
                                    .Properties["text"].StringValue;

                            if (string.IsNullOrWhiteSpace(senderDisplayName))
                            {
                                PlayerInfo? player = handler.GetPlayerInfo(senderUuid);
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
                            else if (senderUuid == handler.GetUserUuid())
                                verifyResult = true;
                            else
                            {
                                PlayerInfo? player = handler.GetPlayerInfo(senderUuid);
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

                            ChatMessage chat = new(signedChat, false, chatTypeId, senderUuid, unsignedChatContent,
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
                            Guid senderUuid = DataTypes.ReadNextUUID(packetData);
                            int index = DataTypes.ReadNextVarInt(packetData);
                            // Signature is fixed size of 256 bytes
                            byte[]? messageSignature = DataTypes.ReadNextBool(packetData)
                                ? DataTypes.ReadNextByteArray(packetData, 256)
                                : null;

                            // Body
                            // net.minecraft.network.message.MessageBody.Serialized#write
                            string message = DataTypes.ReadNextString(packetData);
                            long timestamp = DataTypes.ReadNextLong(packetData);
                            long salt = DataTypes.ReadNextLong(packetData);

                            // Previous Messages
                            // net.minecraft.network.message.LastSeenMessageList.Indexed#write
                            // net.minecraft.network.message.MessageSignatureData.Indexed#write
                            int totalPreviousMessages = DataTypes.ReadNextVarInt(packetData);
                            Tuple<int, byte[]?>[] previousMessageSignatures =
                                new Tuple<int, byte[]?>[totalPreviousMessages];

                            for (int i = 0; i < totalPreviousMessages; i++)
                            {
                                // net.minecraft.network.message.MessageSignatureData.Indexed#fromBuf
                                int messageId = DataTypes.ReadNextVarInt(packetData) - 1;
                                if (messageId == -1)
                                    previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId,
                                        DataTypes.ReadNextByteArray(packetData, 256));
                                else
                                    previousMessageSignatures[i] = new Tuple<int, byte[]?>(messageId, null);
                            }

                            // Other
                            string? unsignedChatContent = DataTypes.ReadNextBool(packetData)
                                ? dataTypes.ReadNextChat(packetData)
                                : null;

                            MessageFilterType filterType = (MessageFilterType)DataTypes.ReadNextVarInt(packetData);

                            if (filterType == MessageFilterType.PartiallyFiltered)
                                DataTypes.ReadNextULongArray(packetData);

                            // Network Target
                            // net.minecraft.network.message.MessageType.Serialized#write
                            int chatTypeId = DataTypes.ReadNextVarInt(packetData);
                            string chatName = dataTypes.ReadNextChat(packetData);
                            string? targetName = DataTypes.ReadNextBool(packetData)
                                ? dataTypes.ReadNextChat(packetData)
                                : null;

                            if (!ChatParser.MessageTypeRegistry.TryGetByNumId(chatTypeId, out ChatParser.MessageType messageTypeEnum))
                            {
                                messageTypeEnum = ChatParser.MessageType.CHAT;
                            }

                            //var chatInfo = Json.ParseJson(targetName ?? chatName).Properties;
                            var senderDisplayName = chatName;
                            string? senderTeamName = targetName;

                            if (string.IsNullOrWhiteSpace(senderDisplayName))
                            {
                                var player = handler.GetPlayerInfo(senderUuid);
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
                                if (senderUuid == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    var player = handler.GetPlayerInfo(senderUuid);
                                    if (player == null || !player.IsMessageChainLegal())
                                        verifyResult = false;
                                    else
                                    {
                                        verifyResult = player.VerifyMessage(message, senderUuid, player.ChatUuid,
                                            index, timestamp, salt, ref messageSignature,
                                            previousMessageSignatures);
                                    }
                                }
                            }

                            ChatMessage chat = new(message, false, chatTypeId, senderUuid, unsignedChatContent,
                                senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult);
                            lock (MessageSigningLock)
                                Acknowledge(chat);
                            handler.OnTextReceived(chat);
                        }

                        break;
                    }
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
                        break;
                    }
                case PacketTypesIn.ChunkBatchStarted:
                    {
                        chunkBatchStartTime = GetNanos();
                        break;
                    }
                    
                case PacketTypesIn.StartConfiguration:
                    {
                        currentState = CurrentState.Configuration;
                        SendAcknowledgeConfiguration();
                        break;
                    }
                case PacketTypesIn.HideMessage:
                    {
                        var hideMessageSignature = DataTypes.ReadNextByteArray(packetData);
                        Debug.Log($"HideMessage was not processed! (SigLen={hideMessageSignature.Length})");
                        break;
                    }
                case PacketTypesIn.SystemChat:
                    string systemMessage = DataTypes.ReadNextString(packetData);
                    if (protocolVersion >= MC_1_19_3_Version)
                    {
                        bool isOverlay = DataTypes.ReadNextBool(packetData);
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

                        handler.OnTextReceived(new(systemMessage, null, true, -1, Guid.Empty, true));
                    }
                    else
                    {
                        int msgType = DataTypes.ReadNextVarInt(packetData);
                        if ((msgType == 1 && !ProtocolSettings.DisplaySystemMessages))
                            break;
                        handler.OnTextReceived(new(systemMessage, null, true, msgType, Guid.Empty, true));
                    }

                    break;
                case PacketTypesIn.ProfilelessChatMessage:
                    string message_ = DataTypes.ReadNextString(packetData);
                    int messageType_ = DataTypes.ReadNextVarInt(packetData);
                    string messageName = DataTypes.ReadNextString(packetData);
                    string? targetName_ = DataTypes.ReadNextBool(packetData)
                        ? DataTypes.ReadNextString(packetData)
                        : null;
                    ChatMessage profilelessChat = new(message_, targetName_ ?? messageName, true, messageType_,
                        Guid.Empty, true);
                    profilelessChat.isSenderJson = true;
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
                        byte[]? precedingSignature = DataTypes.ReadNextBool(packetData)
                            ? DataTypes.ReadNextByteArray(packetData)
                            : null;
                        Guid senderUuid = DataTypes.ReadNextUUID(packetData);
                        byte[] headerSignature = DataTypes.ReadNextByteArray(packetData);
                        byte[] bodyDigest = DataTypes.ReadNextByteArray(packetData);

                        bool verifyResult;

                        if (!isOnlineMode)
                            verifyResult = false;
                        else if (senderUuid == handler.GetUserUuid())
                            verifyResult = true;
                        else
                        {
                            PlayerInfo? player = handler.GetPlayerInfo(senderUuid);

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
                            dimensionTypeNameRespawn = DataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                        else if (protocolVersion >= MC_1_16_2_Version)
                            dimensionTypeRespawn = DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT); // Dimension Type: NBT Tag Compound
                        else
                            DataTypes.ReadNextString(packetData);
                            
                        this.currentDimension = 0;

                        string dimensionName = DataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above
                        var dimensionId = ResourceLocation.FromString(dimensionName);

                        if (protocolVersion >= MC_1_16_2_Version && protocolVersion <= MC_1_18_2_Version)
                        {
                            // Store the received dimension type with received dimension id
                            World.StoreOneDimensionType(dimensionId, dimensionTypeRespawn!);

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
                        break;
                    }
                case PacketTypesIn.PlayerPositionAndLook:
                    {
                        // These always need to be read, since we need the field after them for teleport confirm
                        var location = new Location(
                            DataTypes.ReadNextDouble(packetData), // X
                            DataTypes.ReadNextDouble(packetData), // Y
                            DataTypes.ReadNextDouble(packetData) // Z
                        );

                        float yaw = DataTypes.ReadNextFloat(packetData);
                        float pitch = DataTypes.ReadNextFloat(packetData);
                        byte locMask = DataTypes.ReadNextByte(packetData);

                        // entity handling require player pos for distance calculating
                        var currentLocation = handler.GetCurrentLocation();
                        location.X = (locMask & 1 << 0) != 0 ? currentLocation.X + location.X : location.X;
                        location.Y = (locMask & 1 << 1) != 0 ? currentLocation.Y + location.Y : location.Y;
                        location.Z = (locMask & 1 << 2) != 0 ? currentLocation.Z + location.Z : location.Z;
                        
                        int teleportId = DataTypes.ReadNextVarInt(packetData);

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

                        int chunkX = DataTypes.ReadNextInt(packetData);
                        int chunkZ = DataTypes.ReadNextInt(packetData);

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
                            bool chunksContinuous = DataTypes.ReadNextBool(packetData);
                            if (protocolVersion >= MC_1_16_Version && protocolVersion <= MC_1_16_1_Version)
                                DataTypes.ReadNextBool(packetData); // Ignore old data - 1.16 to 1.16.1 only
                                
                            ushort chunkMask = (ushort)DataTypes.ReadNextVarInt(packetData);

                            DataTypes.ReadNextNbt(packetData, dataTypes.UseAnonymousNBT);  // Heightmaps - 1.14 and above

                            pTerrain.ProcessChunkColumnData16(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, currentDimension, packetData);
                        }
                        break;
                    }
                case PacketTypesIn.ChunksBiomes: // 1.19.4
                    // TODO
                    break;
                case PacketTypesIn.UpdateLight:
                    {
                        int chunkX = DataTypes.ReadNextVarInt(packetData);
                        int chunkZ = DataTypes.ReadNextVarInt(packetData);

                        pTerrain.ProcessChunkLightData(chunkX, chunkZ, packetData);
                        break;
                    }
                case PacketTypesIn.MapData:
                    {
                        int mapId = DataTypes.ReadNextVarInt(packetData);
                        byte scale = DataTypes.ReadNextByte(packetData);

                        bool trackingPosition = true; //  1.9+
                        bool locked = false;          // 1.14+

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
                                    mapIcon.DisplayName = ChatParser.ParseText(DataTypes.ReadNextString(packetData));

                                icons.Add(mapIcon);
                            }
                        }

                        byte colsUpdated = DataTypes.ReadNextByte(packetData); // width
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
                        break;
                    }
                case PacketTypesIn.TradeList: // MC 1.14 or greater
                    {
                        int windowId = DataTypes.ReadNextVarInt(packetData);
                        int size = DataTypes.ReadNextByte(packetData);
                        List<VillagerTrade> trades = new();
                        for (int tradeId = 0; tradeId < size; tradeId++)
                        {
                            VillagerTrade trade = dataTypes.ReadNextTrade(packetData, ItemPalette.INSTANCE);
                                trades.Add(trade);
                        }
                        VillagerInfo villagerInfo = new VillagerInfo()
                        {
                            Level = DataTypes.ReadNextVarInt(packetData),
                            Experience = DataTypes.ReadNextVarInt(packetData),
                            IsRegularVillager = DataTypes.ReadNextBool(packetData),
                            CanRestock = DataTypes.ReadNextBool(packetData)
                        };
                        handler.OnTradeList(windowId, trades, villagerInfo);
                    }
                    break;
                case PacketTypesIn.Title:
                    {
                        int action = DataTypes.ReadNextVarInt(packetData);
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

                        break;
                    }
                case PacketTypesIn.MultiBlockChange:
                    {
                        // MC 1.16.2+
                        long chunkSection = DataTypes.ReadNextLong(packetData);
                        int sectionX = (int)(chunkSection >> 42);
                        int sectionY = (int)((chunkSection << 44) >> 44);
                        int sectionZ = (int)((chunkSection << 22) >> 42);
                            
                        if(protocolVersion < MC_1_20_Version)
                            DataTypes.ReadNextBool(packetData); // Useless boolean (Related to light update)

                        int blocksSize = DataTypes.ReadNextVarInt(packetData);

                        for (int i = 0; i < blocksSize; i++)
                        {
                            ulong block = (ulong)DataTypes.ReadNextVarLong(packetData);
                            int blockId = (int)(block >> 12);
                            int localX = (int)((block >> 8) & 0x0F);
                            int localZ = (int)((block >> 4) & 0x0F);
                            int localY = (int)(block & 0x0F);

                            var bloc = new Block((ushort)blockId);
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
                        var blockLoc = DataTypes.ReadNextBlockLoc(packetData);
                        var bloc = new Block((ushort) DataTypes.ReadNextVarInt(packetData));
                            
                        handler.GetChunkRenderManager().SetBlock(blockLoc, bloc);
                        break;
                    }
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
                        break;
                    }
                case PacketTypesIn.UnloadChunk:
                    {
                        int chunkX = DataTypes.ReadNextInt(packetData);
                        int chunkZ = DataTypes.ReadNextInt(packetData);

                        // Warning: It is legal to include unloaded chunks in the UnloadChunk packet. Since chunks that have not been loaded are not recorded, this may result in loading chunks that should be unloaded and inaccurate statistics.
                        handler.GetChunkRenderManager().UnloadChunkColumn(chunkX, chunkZ);
                        break;
                    }
                case PacketTypesIn.ChangeGameState:
                    {
                        byte changeReason = DataTypes.ReadNextByte(packetData);
                        float changeValue = DataTypes.ReadNextFloat(packetData);
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
                    bool previewsChatSetting = DataTypes.ReadNextBool(packetData);
                    // TODO handler.OnChatPreviewSettingUpdate(previewsChatSetting);
                    break;
                case PacketTypesIn.ChatPreview:
                    // TODO Currently not implemented
                    break;
                case PacketTypesIn.PlayerInfo:
                    if (protocolVersion >= MC_1_19_3_Version)
                    {
                        byte actionBitset = DataTypes.ReadNextByte(packetData);
                        int numberOfActions = DataTypes.ReadNextVarInt(packetData);
                        for (int i = 0; i < numberOfActions; i++)
                        {
                            Guid playerUuid = DataTypes.ReadNextUUID(packetData);

                            PlayerInfo player;
                            if ((actionBitset & (1 << 0)) > 0) // Actions bit 0: add player
                            {
                                string name = DataTypes.ReadNextString(packetData);
                                int numberOfProperties = DataTypes.ReadNextVarInt(packetData);
                                for (int j = 0; j < numberOfProperties; ++j)
                                {
                                    DataTypes.SkipNextString(packetData);
                                    DataTypes.SkipNextString(packetData);
                                    if (DataTypes.ReadNextBool(packetData))
                                        DataTypes.SkipNextString(packetData);
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
                                bool hasSignatureData = DataTypes.ReadNextBool(packetData);
                                if (hasSignatureData)
                                {
                                    Guid chatUuid = DataTypes.ReadNextUUID(packetData);
                                    long publicKeyExpiryTime = DataTypes.ReadNextLong(packetData);
                                    byte[] encodedPublicKey = DataTypes.ReadNextByteArray(packetData);
                                    byte[] publicKeySignature = DataTypes.ReadNextByteArray(packetData);
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
                                handler.OnGamemodeUpdate(playerUuid, DataTypes.ReadNextVarInt(packetData));
                            }

                            if ((actionBitset & (1 << 3)) > 0) // Actions bit 3: update listed
                            {
                                player.Listed = DataTypes.ReadNextBool(packetData);
                            }

                            if ((actionBitset & (1 << 4)) > 0) // Actions bit 4: update latency
                            {
                                int latency = DataTypes.ReadNextVarInt(packetData);
                                handler.OnLatencyUpdate(playerUuid, latency); //Update latency;
                            }

                            if ((actionBitset & (1 << 5)) > 0) // Actions bit 5: update display name
                            {
                                if (DataTypes.ReadNextBool(packetData))
                                    player.DisplayName = DataTypes.ReadNextString(packetData);
                                else
                                    player.DisplayName = null;
                            }
                        }
                    }
                    else // 1.8 - 1.19.2
                    {
                        int action = DataTypes.ReadNextVarInt(packetData); // Action Name
                        int numberOfPlayers = DataTypes.ReadNextVarInt(packetData); // Number Of Players 

                        for (int i = 0; i < numberOfPlayers; i++)
                        {
                            Guid uuid = DataTypes.ReadNextUUID(packetData); // Player UUID

                            switch (action)
                            {
                                case 0x00: //Player Join (Add player since 1.19)
                                    string name = DataTypes.ReadNextString(packetData); // Player name
                                    int propNum =
                                        DataTypes.ReadNextVarInt(
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
                                            DataTypes.ReadNextString(packetData); // Name: String (32767)
                                        string val =
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

                                    int gameMode = DataTypes.ReadNextVarInt(packetData); // Gamemode
                                    handler.OnGamemodeUpdate(uuid, gameMode);

                                    int ping = DataTypes.ReadNextVarInt(packetData); // Ping

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

                                            int publicKeyLength =
                                                DataTypes.ReadNextVarInt(packetData); // Public Key Length 
                                            if (publicKeyLength > 0)
                                                publicKey = DataTypes.ReadData(publicKeyLength,
                                                    packetData); // Public key

                                            int signatureLength =
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
                                    int latency = DataTypes.ReadNextVarInt(packetData);
                                    handler.OnLatencyUpdate(uuid, latency); //Update latency;
                                    break;
                                case 0x03: //Update display name
                                    if (DataTypes.ReadNextBool(packetData))
                                    {
                                        PlayerInfo? player = handler.GetPlayerInfo(uuid);
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
                    int numberOfLeavePlayers = DataTypes.ReadNextVarInt(packetData);
                    for (int i = 0; i < numberOfLeavePlayers; ++i)
                    {
                        Guid playerUuid = DataTypes.ReadNextUUID(packetData);
                        handler.OnPlayerLeave(playerUuid);
                    }

                    break;
                case PacketTypesIn.TabComplete:
                    {   // MC 1.13 or greater
                        autocomplete_transaction_id = DataTypes.ReadNextVarInt(packetData);
                        int completionStart  = DataTypes.ReadNextVarInt(packetData); // Start of text to replace
                        int completionLength = DataTypes.ReadNextVarInt(packetData); // Length of text to replace

                        int resultCount = DataTypes.ReadNextVarInt(packetData);
                        var completeResults = new List<string>();

                        for (int i = 0; i < resultCount; i++)
                        {
                            completeResults.Add(DataTypes.ReadNextString(packetData));
                            // MC 1.13+ Skip optional tooltip for each tab-complete result
                            if (DataTypes.ReadNextBool(packetData))
                                DataTypes.SkipNextString(packetData);
                        }
                        handler.OnTabComplete(completionStart, completionLength, completeResults);
                        break;
                    }
                case PacketTypesIn.PluginMessage:
                    {
                        string channel = DataTypes.ReadNextString(packetData);
                        // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                        //handler.OnPluginChannelMessage(channel, packetData.ToArray());
                        return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                    }
                case PacketTypesIn.Disconnect:
                    handler.OnConnectionLost(DisconnectReason.InGameKick, ChatParser.ParseText(DataTypes.ReadNextString(packetData)));
                    return false;
                case PacketTypesIn.OpenWindow:
                    {   // MC 1.14 or greater
                        int windowId = DataTypes.ReadNextVarInt(packetData);
                        int windowType = DataTypes.ReadNextVarInt(packetData);
                        string title = DataTypes.ReadNextString(packetData);
                        var inventory = new Container(windowId, windowType, ChatParser.ParseText(title));
                        handler.OnInventoryOpen(windowId, inventory);
                    }
                    break;
                case PacketTypesIn.CloseWindow:
                    {
                        byte windowId = DataTypes.ReadNextByte(packetData);
                        lock (window_actions) { window_actions[windowId] = 0; }
                        handler.OnInventoryClose(windowId);
                        break;
                    }
                case PacketTypesIn.WindowItems:
                    {
                        byte windowId = DataTypes.ReadNextByte(packetData);
                        int stateId = -1;
                        int elements = 0;

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

                        handler.OnWindowItems(windowId, inventorySlots, stateId);
                        break;
                    }
                case PacketTypesIn.SetSlot:
                    {
                        byte windowId = DataTypes.ReadNextByte(packetData);
                        int stateId = -1;
                        if (protocolVersion >= MC_1_17_1_Version)
                            stateId = DataTypes.ReadNextVarInt(packetData); // State ID - 1.17.1 and above
                        short slotId2 = DataTypes.ReadNextShort(packetData);
                        ItemStack? item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                        handler.OnSetSlot(windowId, slotId2, item!, stateId);
                        break;
                    }
                case PacketTypesIn.WindowConfirmation:
                    {
                        byte windowId = DataTypes.ReadNextByte(packetData);
                        short actionId = DataTypes.ReadNextShort(packetData);
                        bool accepted = DataTypes.ReadNextBool(packetData);
                        if (!accepted)
                        {
                            SendWindowConfirmation(windowId, actionId, accepted);
                        }
                        break;
                    }
                case PacketTypesIn.ResourcePackSend:
                    {
                        string url = DataTypes.ReadNextString(packetData);
                        string hash = DataTypes.ReadNextString(packetData);
                        bool forced = true; // Assume forced for MC 1.16 and below
                        if (protocolVersion >= MC_1_17_Version)
                        {
                            forced = DataTypes.ReadNextBool(packetData);
                            bool hasPromptMessage = DataTypes.ReadNextBool(packetData);   // Has Prompt Message (Boolean) - 1.17 and above
                            if (hasPromptMessage)
                                DataTypes.SkipNextString(packetData); // Prompt Message (Optional Chat) - 1.17 and above
                        }
                        // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
                        if (!url.StartsWith("http") && hash.Length != 40) // Some server may have null hash value
                            break;
                        //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
                        byte[] responseHeader = new byte[0];
                        SendPacket(PacketTypesOut.ResourcePackStatus, DataTypes.ConcatBytes(responseHeader, DataTypes.GetVarInt(3))); //Accepted pack
                        SendPacket(PacketTypesOut.ResourcePackStatus, DataTypes.ConcatBytes(responseHeader, DataTypes.GetVarInt(0))); //Successfully loaded
                        break;
                    }
                case PacketTypesIn.SpawnEntity:
                    {
                        Entity entity = dataTypes.ReadNextEntity(packetData, EntityTypePalette.INSTANCE, false);

                        if (protocolVersion >= MC_1_20_2_Version)
                        {
                            if (entity.Type.TypeId == EntityType.PLAYER_ID)
                                handler.OnSpawnPlayer(entity.ID, entity.UUID, entity.Location, (byte)entity.Yaw, (byte)entity.Pitch);
                            break;
                        }

                        handler.OnSpawnEntity(entity);
                        
                        break;
                    }
                case PacketTypesIn.EntityEquipment:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        if (protocolVersion >= MC_1_16_Version)
                        {
                            bool hasNext;
                            do
                            {
                                byte bitsData = DataTypes.ReadNextByte(packetData);
                                //  Top bit set if another entry follows, and otherwise unset if this is the last item in the array
                                hasNext = (bitsData >> 7) == 1 ? true : false;
                                int slot2 = bitsData >> 1;
                                ItemStack? item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                                handler.OnEntityEquipment(entityId, slot2, item!);
                            } while (hasNext);
                        }
                        else
                        {
                            int slot2 = DataTypes.ReadNextVarInt(packetData);
                            ItemStack? item = dataTypes.ReadNextItemSlot(packetData, ItemPalette.INSTANCE);
                            handler.OnEntityEquipment(entityId, slot2, item!);
                        }
                        break;
                    }
                case PacketTypesIn.SpawnLivingEntity:
                    {
                        Entity entity = dataTypes.ReadNextEntity(packetData, EntityTypePalette.INSTANCE, true);
                        // packet before 1.15 has metadata at the end
                        // this is not handled in DataTypes.ReadNextEntity()
                        // we are simply ignoring leftover data in packet
                        handler.OnSpawnEntity(entity);
                        break;
                    }
                case PacketTypesIn.SpawnPlayer:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        Guid UUID = DataTypes.ReadNextUUID(packetData);
                        double x = DataTypes.ReadNextDouble(packetData);
                        double y = DataTypes.ReadNextDouble(packetData);
                        double z = DataTypes.ReadNextDouble(packetData);
                        byte yaw = DataTypes.ReadNextByte(packetData);
                        byte pitch = DataTypes.ReadNextByte(packetData);
                        Location location = new Location(x, y, z);
                        handler.OnSpawnPlayer(entityId, UUID, location, yaw, pitch);
                        break;
                    }
                case PacketTypesIn.SpawnExperienceOrb:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        double x = DataTypes.ReadNextDouble(packetData);
                        double y = DataTypes.ReadNextDouble(packetData);
                        double z = DataTypes.ReadNextDouble(packetData);
                        DataTypes.ReadNextShort(packetData); // TODO Use this value
                        handler.OnSpawnEntity(new(entityId, EntityTypePalette.INSTANCE.GetById(EntityType.EXPERIENCE_ORB_ID),
                                new(x, y, z), 0, 0, 0, 0));
                        break;
                    }
                case PacketTypesIn.EntityEffect:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        Inventory.Effects effect = Effects.Speed;
                        if (Enum.TryParse(DataTypes.ReadNextByte(packetData).ToString(), out effect))
                        {
                            int amplifier = DataTypes.ReadNextByte(packetData);
                            int duration = DataTypes.ReadNextVarInt(packetData);
                            byte flags = DataTypes.ReadNextByte(packetData);
                                
                            bool hasFactorData = false;
                            Dictionary<string, object>? factorCodec = null;

                            if (protocolVersion >= MC_1_19_Version)
                            {
                                hasFactorData = DataTypes.ReadNextBool(packetData);
                                // Temp disabled to avoid crashing TODO Check how it works
                                //factorCodec = DataTypes.ReadNextNbt(packetData);
                            }

                            handler.OnEntityEffect(entityId, effect, amplifier, duration, flags, hasFactorData, factorCodec);
                        }
                        break;
                    }
                case PacketTypesIn.DestroyEntities:
                    {
                        int entityCount = 1; // 1.17.0 has only one entity per packet
                        if (protocolVersion != MC_1_17_Version)
                            entityCount = DataTypes.ReadNextVarInt(packetData); // All other versions have a "count" field
                        int[] entityList = new int[entityCount];
                        for (int i = 0; i < entityCount; i++)
                        {
                            entityList[i] = DataTypes.ReadNextVarInt(packetData);
                        }
                        handler.OnDestroyEntities(entityList);
                        break;
                    }
                case PacketTypesIn.EntityPosition:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        double deltaX = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        double deltaY = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        double deltaZ = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        bool onGround = DataTypes.ReadNextBool(packetData);
                        deltaX = deltaX / (128 * 32);
                        deltaY = deltaY / (128 * 32);
                        deltaZ = deltaZ / (128 * 32);
                        handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, onGround);
                        break;
                    }
                case PacketTypesIn.EntityPositionAndRotation:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        double deltaX = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        double deltaY = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        double deltaZ = Convert.ToDouble(DataTypes.ReadNextShort(packetData));
                        byte yaw = DataTypes.ReadNextByte(packetData);
                        byte pitch = DataTypes.ReadNextByte(packetData);
                        bool onGround = DataTypes.ReadNextBool(packetData);
                        deltaX = deltaX / (128 * 32);
                        deltaY = deltaY / (128 * 32);
                        deltaZ = deltaZ / (128 * 32);
                        handler.OnEntityPosition(entityId, deltaX, deltaY, deltaZ, onGround);
                        handler.OnEntityRotation(entityId, yaw, pitch, onGround);
                        break;
                    }
                case PacketTypesIn.EntityRotation:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        byte yaw = DataTypes.ReadNextByte(packetData);
                        byte pitch = DataTypes.ReadNextByte(packetData);
                        bool onGround = DataTypes.ReadNextBool(packetData);
                        handler.OnEntityRotation(entityId, yaw, pitch, onGround);
                        break;
                    }
                case PacketTypesIn.EntityHeadLook:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        byte headYaw = DataTypes.ReadNextByte(packetData);
                        handler.OnEntityHeadLook(entityId, headYaw);
                        break;
                    }
                case PacketTypesIn.EntityProperties:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        int NumberOfProperties = protocolVersion >= MC_1_17_Version ? DataTypes.ReadNextVarInt(packetData) : DataTypes.ReadNextInt(packetData);
                        Dictionary<string, double> keys = new Dictionary<string, double>();
                        for (int i = 0; i < NumberOfProperties; i++)
                        {
                            string _key = DataTypes.ReadNextString(packetData);
                            double _value = DataTypes.ReadNextDouble(packetData);

                            List<double> op0 = new();
                            List<double> op1 = new();
                            List<double> op2 = new();
                            int NumberOfModifiers = DataTypes.ReadNextVarInt(packetData);
                            for (int j = 0; j < NumberOfModifiers; j++)
                            {
                                DataTypes.ReadNextUUID(packetData);
                                double amount = DataTypes.ReadNextDouble(packetData);
                                byte operation = DataTypes.ReadNextByte(packetData);
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
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        Dictionary<int, object?> metadata = dataTypes.ReadNextMetadata(packetData,
                                ItemPalette.INSTANCE, entityMetadataPalette);

                        int healthField; // See https://wiki.vg/Entity_metadata#Living_Entity
                        if (protocolVersion > MC_1_20_4_Version)
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
                        int entityId = DataTypes.ReadNextInt(packetData);
                        byte status = DataTypes.ReadNextByte(packetData);
                        handler.OnEntityStatus(entityId, status);
                        break;
                    }
                case PacketTypesIn.TimeUpdate:
                    {
                        long WorldAge = DataTypes.ReadNextLong(packetData);
                        long TimeOfday = DataTypes.ReadNextLong(packetData);
                        handler.OnTimeUpdate(WorldAge, TimeOfday);
                        break;
                    }
                case PacketTypesIn.EntityTeleport:
                    {
                        int entityId = DataTypes.ReadNextVarInt(packetData);
                        double tX = DataTypes.ReadNextDouble(packetData);
                        double tY = DataTypes.ReadNextDouble(packetData);
                        double tZ = DataTypes.ReadNextDouble(packetData);
                        byte yaw = DataTypes.ReadNextByte(packetData);
                        byte pitch = DataTypes.ReadNextByte(packetData);
                        bool onGround = DataTypes.ReadNextBool(packetData);
                        handler.OnEntityTeleport(entityId, tX, tY, tZ, onGround);
                        break;
                    }
                case PacketTypesIn.UpdateHealth:
                    {
                        float health = DataTypes.ReadNextFloat(packetData);
                        int food;
                        food = DataTypes.ReadNextVarInt(packetData);
                        DataTypes.ReadNextFloat(packetData); // Food Saturation
                        handler.OnUpdateHealth(health, food);
                        break;
                    }
                case PacketTypesIn.SetExperience:
                    {
                        float experiencebar = DataTypes.ReadNextFloat(packetData);
                        int level = DataTypes.ReadNextVarInt(packetData);
                        int totalexperience = DataTypes.ReadNextVarInt(packetData);
                        handler.OnSetExperience(experiencebar, level, totalexperience);
                        break;
                    }
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
                        break;   
                    }
                case PacketTypesIn.HeldItemChange:
                    {
                        byte slot = DataTypes.ReadNextByte(packetData);
                        handler.OnHeldItemChange(slot);
                        break;
                    }
                case PacketTypesIn.ScoreboardObjective:
                    {
                        string objectiveName = DataTypes.ReadNextString(packetData);
                        byte mode = DataTypes.ReadNextByte(packetData);

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
                        break;
                    }
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

                        break;
                    }
                case PacketTypesIn.BlockBreakAnimation:
                    {
                        int playerId = DataTypes.ReadNextVarInt(packetData);
                        Location blockLocation = DataTypes.ReadNextLocation(packetData);
                        byte stage = DataTypes.ReadNextByte(packetData);
                        handler.OnBlockBreakAnimation(playerId, blockLocation, stage);
                        break;
                    }
                case PacketTypesIn.EntityAnimation:
                    {
                        int playerId = DataTypes.ReadNextVarInt(packetData);
                        byte animation = DataTypes.ReadNextByte(packetData);
                        handler.OnEntityAnimation(playerId, animation);
                        break;
                    }
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

            var uuid = handler.GetUserUuid();
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
                    uuid = handler.GetUserUuid();

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
                            return StartEncryption(accountLower, handler.GetUserUuidStr(), handler.GetSessionID(), token, serverId,
                                serverPublicKey, playerKeyPair, session);
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
                            var uuidReceived = protocolVersion >= MC_1_16_Version
                                ? DataTypes.ReadNextUUID(packetData)
                                : Guid.Parse(DataTypes.ReadNextString(packetData));
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
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveTimeout = 30000; // 30 seconds
            tcp.ReceiveBufferSize = 1024 * 1024;
            SocketWrapper socketWrapper = new SocketWrapper(tcp);
            DataTypes dataTypes = new DataTypes(MC_1_15_Version);

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
                Queue<byte> packetData = new Queue<byte>(socketWrapper.ReadDataRAW(packetLength));
                if (DataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet Id
                {
                    string result = DataTypes.ReadNextString(packetData); //Get the Json data

                    if (ProtocolSettings.DebugMode)
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
            if (String.IsNullOrEmpty(command))
                return true;

            command = Regex.Replace(command, @"\s+", " ");
            command = Regex.Replace(command, @"\s$", string.Empty);

            //Debug.Log("chat command = " + command);

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
                        Guid uuid = handler.GetUserUuid();
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
                                sign = playerKeyPair!.PrivateKey.SignMessage(message, uuid, chatUuid, messageIndex++,
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
                if (protocolVersion >= MC_1_16_Version)
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
                if (protocolVersion >= MC_1_16_Version)
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
                if (protocolVersion >= MC_1_16_Version)
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

        public bool SendPlayerBlockPlacement(int hand, BlockLoc location, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new();
                packet.AddRange(DataTypes.GetVarInt(hand));
                packet.AddRange(DataTypes.GetBlockLoc(location));
                packet.AddRange(DataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                packet.AddRange(DataTypes.GetFloat(0.5f)); // cursorX
                packet.AddRange(DataTypes.GetFloat(0.5f)); // cursorY
                packet.AddRange(DataTypes.GetFloat(0.5f)); // cursorZ
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

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, ItemStack? item, List<Tuple<short, ItemStack?>> changedSlots, int stateId)
        {
            try
            {
                var itemPalette = ItemPalette.INSTANCE;

                short actionNumber;
                lock (window_actions)
                {
                    if (!window_actions.ContainsKey(windowId))
                        window_actions[windowId] = 0;
                    actionNumber = (short)(window_actions[windowId] + 1);
                    window_actions[windowId] = actionNumber;
                }

                byte button = 0;
                byte mode = 0;

                switch (action)
                {
                    case WindowActionType.LeftClick:
                        button = 0;
                        break;
                    case WindowActionType.RightClick:
                        button = 1;
                        break;
                    case WindowActionType.MiddleClick:
                        button = 2;
                        mode = 3;
                        break;
                    case WindowActionType.ShiftClick:
                        button = 0;
                        mode = 1;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case WindowActionType.ShiftRightClick: // Right-shift click uses button 1
                        button = 1;
                        mode = 1;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case WindowActionType.DropItem:
                        button = 0;
                        mode = 4;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case WindowActionType.DropItemStack:
                        button = 1;
                        mode = 4;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case WindowActionType.StartDragLeft:
                        button = 0;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case WindowActionType.StartDragRight:
                        button = 4;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case WindowActionType.StartDragMiddle:
                        button = 8;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case WindowActionType.EndDragLeft:
                        button = 2;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case WindowActionType.EndDragRight:
                        button = 6;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case WindowActionType.EndDragMiddle:
                        button = 10;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        slotId = -999;
                        break;
                    case WindowActionType.AddDragLeft:
                        button = 1;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case WindowActionType.AddDragRight:
                        button = 5;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                    case WindowActionType.AddDragMiddle:
                        button = 9;
                        mode = 5;
                        item = new ItemStack(Item.NULL, 0);
                        break;
                }

                List<byte> packet = new()
                {
                    (byte)windowId // Window ID
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

                SendPacket(PacketTypesOut.ClickWindow, packet);
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

        public bool ClickContainerButton(int windowId, int buttonId)
        {
            try
            {
                var packet = new List<byte>
                {
                    (byte)windowId,
                    (byte)buttonId
                };
                SendPacket(PacketTypesOut.ClickWindowButton, packet);
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

        public bool SendCloseWindow(int windowId)
        {
            try
            {
                lock (window_actions)
                {
                    if (window_actions.ContainsKey(windowId))
                        window_actions[windowId] = 0;
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

        public bool SendWindowConfirmation(byte windowId, short actionId, bool accepted)
        {
            try
            {
                List<byte> packet = new();
                packet.Add(windowId);
                packet.AddRange(DataTypes.GetShort(actionId));
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

                    packet.AddRange(DataTypes.GetUUID(chatUuid));
                    packet.AddRange(DataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds()));
                    packet.AddRange(DataTypes.GetVarInt(playerKeyPair.PublicKey.Key.Length));
                    packet.AddRange(playerKeyPair.PublicKey.Key);
                    packet.AddRange(DataTypes.GetVarInt(playerKeyPair.PublicKey.SignatureV2!.Length));
                    packet.AddRange(playerKeyPair.PublicKey.SignatureV2);

                    Debug.Log($"SendPlayerSession MessageUUID = {chatUuid},  len(PublicKey) = {playerKeyPair.PublicKey.Key.Length}, len(SignatureV2) = {playerKeyPair.PublicKey.SignatureV2!.Length}");

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
