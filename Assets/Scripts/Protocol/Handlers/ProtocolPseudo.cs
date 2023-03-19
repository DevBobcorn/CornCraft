#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Event;
using System.IO;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Pseudo implementation for Minecraft Protocol
    /// </summary>
    /// <remarks>
    /// Runs without networking and simulates world manipulation,
    /// used only for testing chunk building and player controller
    /// </remarks>
    public class ProtocolPseudo : IMinecraftCom
    {
        public const int DEFAULT_PROTOCOL = ProtocolMinecraft.MC_1_16_5_Version;
        public const string SERVER_NAME = "world.execute(me);";

        private int protocolVersion;

        IMinecraftComHandler handler;
        DataTypes dataTypes;
        Tuple<Thread, CancellationTokenSource>? netMain = null; // Net main thread

        public ProtocolPseudo(int protocolVersion, IMinecraftComHandler handler)
        {
            ChatParser.InitTranslations();
            this.dataTypes = new(protocolVersion);
            this.handler = handler;
            this.protocolVersion = protocolVersion;

            var game = CornClient.Instance;
            game.StartCoroutine(SetupWorld());

            StartUpdating();

        }

        private readonly Dictionary<int, Entity> worldEntities = new();

        private IEnumerator SetupWorld()
        {
            // Generate initial chunks
            GenerateChunkData(true, CornCraft.MCSettings_RenderDistance);

            // Initialize player position
            handler.OnPlayerJoin(new(handler.GetUserUUID(), handler.GetUsername(), null, 0, 0, null, null, null, null));
            handler.UpdateLocation(new(8, 2, 8), 0F, 0F);

            yield return new WaitForSeconds(1F);

            // Generate a pig ring
            int entityId = 0;
            float radius = 7F;

            for (int deg = 0;deg < 360;deg += 15)
            {
                float rad = Mathf.Deg2Rad * deg;
                var loc = new Location(8 + Mathf.Sin(rad) * radius, 2, 8 + Mathf.Cos(rad) * radius);

                var entity = new Entity(entityId++, EntityPalette.INSTANCE.FromId(EntityType.HUSK_ID), loc);
                entity.Yaw = -deg;
                worldEntities.Add(entity.ID, entity);

                handler.OnSpawnEntity(entity);
                yield return new WaitForSeconds(0.2F);
            }
            
        }

        /// <summary>
        /// Separate thread. Timer loop.
        /// </summary>
        private void Updater(object? o)
        {
            CancellationToken cancelToken = (CancellationToken)o!;

            if (cancelToken.IsCancellationRequested)
                return;

            try
            {
                System.Diagnostics.Stopwatch stopWatch = new();
                List<int> entitiesToDestroy = new();

                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    // Generate environment chunks
                    GenerateChunkData(false, CornCraft.MCSettings_RenderDistance);
                    
                    // Update entities
                    entitiesToDestroy.Clear();
                    foreach (var pair in worldEntities)
                    {
                        if (pair.Value.Health <= 0F)
                        {
                            // Mark this entity as to be destroyed
                            entitiesToDestroy.Add(pair.Key);

                        }
                    }

                    if (entitiesToDestroy.Count > 0)
                    {
                        handler.OnDestroyEntities(entitiesToDestroy.ToArray());
                        
                        foreach (var entityId in entitiesToDestroy)
                            worldEntities.Remove(entityId);
                    }

                    handler.OnUpdate();

                    stopWatch.Restart();

                    int sleepLength = 50 - stopWatch.Elapsed.Milliseconds;
                    if (sleepLength > 0)
                        Thread.Sleep(sleepLength);
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }

            if (cancelToken.IsCancellationRequested)
            {   // Normally disconnected
                Loom.QueueOnMainThread(
                    () => handler.OnConnectionLost(DisconnectReason.ConnectionLost, "")
                );
                return;
            }
        }

        private void StartUpdating()
        {
            Thread threadUpdater = new Thread(new ParameterizedThreadStart(Updater));
            threadUpdater.Name = "ProtocolPseudoHandler";
            netMain = new Tuple<Thread, CancellationTokenSource>(threadUpdater, new());
            threadUpdater.Start(netMain.Item2.Token);
        }

        private int playerChunkX, playerChunkZ;

        private void GenerateChunkData(bool forceUpdate, int size)
        {
            var game = CornClient.Instance;
            
            lock (game.movementLock)
            {
                var playerLoc = CornClient.Instance.PlayerData.Location;

                if (!forceUpdate && playerLoc.ChunkX == playerChunkX && playerLoc.ChunkZ == playerChunkZ)
                    return;
                
                playerChunkX = playerLoc.ChunkX;
                playerChunkZ = playerLoc.ChunkZ;
            }

            var world = handler.GetWorld();
            const int chunkColumnSize = 16;
            int biomeLength = chunkColumnSize * 64;

            HashSet<int> stateANumIds = BlockStatePalette.INSTANCE.StateListTable[new("diamond_block")];
            HashSet<int> stateBNumIds = BlockStatePalette.INSTANCE.StateListTable[new("quartz_block")];

            ushort StateAId = 0, StateBId = 0;

            foreach (var id in stateANumIds)
            {
                StateAId = (ushort) id;
                break;
            }

            foreach (var id in stateBNumIds)
            {
                StateBId = (ushort) id;
                break;
            }

            int chunkY = 0;

            for (int chunkX = -size + playerChunkX;chunkX <= size + playerChunkX;chunkX++)
                for (int chunkZ = -size + playerChunkZ;chunkZ <= size + playerChunkZ;chunkZ++)
                {
                    if (world.GetChunkColumn(chunkX, chunkZ) is not null)
                        continue; // Chunk data already presents, skip

                    Chunk chunk = new(world);

                    for (int x = 0;x < Chunk.SizeX;x++)
                        for (int z = 0;z < Chunk.SizeZ;z++)
                            for (int y = 0;y < ((chunkX + chunkZ) % 2 == 0 ? 2 : 1);y++)
                                chunk[x, y, z] = (chunkX + chunkZ) % 2 == 0 ? new(StateAId) : new(StateBId);
                    
                    world.StoreChunk(chunkX, chunkY, chunkZ, chunkColumnSize, chunk);

                    // Lighting data
                    var skyLight   = new byte[4096 * (chunkColumnSize + 2)];
                    var blockLight = new byte[4096 * (chunkColumnSize + 2)];

                    // Biome data
                    var biomes = new short[biomeLength];
                    Array.Fill(biomes, (short) 1);

                    var c = world[chunkX, chunkZ];

                    c!.SetLights(skyLight, blockLight);
                    c!.SetBiomes(biomes);

                    c.FullyLoaded = true;

                    // Broadcast event to update world render (Event is not used yet)
                    Loom.QueueOnMainThread(() => {
                            EventManager.Instance.Broadcast<ReceiveChunkColumnEvent>(new(chunkX, chunkZ));
                        }
                    );
                }
            
            
        }

        public void Disconnect() { }

        public void Dispose()
        {
            try
            {
                if (netMain != null)
                {
                    netMain.Item2.Cancel();
                }
            }
            catch { }
        }

        public int GetMaxChatMessageLength()
        {
            return 256;
        }

        public int GetNetMainThreadId()
        {
            // Pseudo handler doesn't have a net read thread at all,
            // so just use caller's current thread id
            return Thread.CurrentThread.ManagedThreadId;
        }

        public int GetProtocolVersion()
        {
            return -1;
        }

        public bool Login(PlayerKeyPair? playerKeyPair, SessionToken session, string accountLower)
        {
            return true;
        }

        public bool SelectTrade(int selectedSlot)
        {
            return true;
        }

        public bool SendAnimation(int animation, int playerid)
        {
            return true;
        }

        public bool SendAutoCompleteText(string text)
        {
            if (String.IsNullOrEmpty(text))
                return false;
            return true;
        }

        public bool SendBrandInfo(string brandInfo)
        {
            return true;
        }

        public bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair = null)
        {
            if (String.IsNullOrEmpty(message))
                return false;

            if (message.StartsWith("/gamemode "))
            {
                var mode = message.Split(' ')[1].ToLower();

                switch (mode)
                {
                    case "survival":
                        handler.OnGamemodeUpdate(handler.GetUserUUID(), 0);
                        break;
                    case "creative":
                        handler.OnGamemodeUpdate(handler.GetUserUUID(), 1);
                        break;
                    case "adventure":
                        handler.OnGamemodeUpdate(handler.GetUserUUID(), 2);
                        break;
                    case "spectator":
                        handler.OnGamemodeUpdate(handler.GetUserUUID(), 3);
                        break;
                    default:
                        Debug.LogWarning($"Unknown gamemode: {mode}");
                        return false;
                }
            }

            if (message.StartsWith("/struct "))
            {
                var parameters = message.Split(' ');

                if (parameters.Length != 5)
                {
                    
                    CornClient.ShowNotification($"Structure parameters incorrect: {string.Join(", ", parameters[1..])}",
                            UI.Notification.Type.Warning);

                    return false;
                }

                int offsetX, offsetY, offsetZ;

                if (int.TryParse(parameters[1], out offsetX) &&
                    int.TryParse(parameters[2], out offsetY) &&
                    int.TryParse(parameters[3], out offsetZ))
                {
                    var name = parameters[4].ToLower();
                    var path = PathHelper.GetExtraDataFile($"structures/{name}.nbt");

                    return LoadStructure(path, offsetX, offsetY, offsetZ);
                }
                else
                {
                    CornClient.ShowNotification($"Structure coordinates invalid: ({parameters[1]}, {parameters[2]}, {parameters[3]})",
                            UI.Notification.Type.Warning);
                    
                    return false;
                }
            }

            return true;
        }

        private bool LoadStructure(string path, int offsetX, int offsetY, int offsetZ)
        {
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var nbt = dataTypes.ReadNbtFromBytes(bytes);

                var size = (object[]) nbt["size"];
                int sizeX = (int) size[0], sizeY = (int) size[1], sizeZ = (int) size[2];

                var offset = new Location(offsetX, offsetY, offsetZ);

                var palette = (object[]) nbt["palette"];
                var mapping = new Block[palette.Length]; // Palette index => Numeral blockstate id

                var stateListTable = BlockStatePalette.INSTANCE.StateListTable;
                var statesTable = BlockStatePalette.INSTANCE.StatesTable;

                for (var entryIdx = 0;entryIdx < palette.Length;entryIdx++)
                {
                    var entryObj = (Dictionary<string, object>) palette[entryIdx];
                    var blockId = ResourceLocation.fromString(entryObj["Name"].ToString());

                    int entryStateId = 0; // Air by default

                    if (!stateListTable.ContainsKey(blockId)) // Block not found in registry, ignore this entry
                    {
                        Debug.Log($"Block with numeral id {blockId} cannot be found, using empty block instead");

                    }
                    else // Get the exact blockstate id (in current palatte)
                    {
                        if (entryObj.ContainsKey("Properties")) // Find the specific variant of this block
                        {
                            var propDict = (Dictionary<string, object>) entryObj["Properties"];
                            var conditions = new Dictionary<string, string>();

                            foreach (var prop in propDict.Keys)
                                conditions.Add(prop, propDict[prop].ToString());

                            var predicate = new BlockStatePredicate(conditions);

                            foreach (var stateId in stateListTable[blockId])
                            {
                                if (!statesTable.ContainsKey(stateId))
                                    continue;
                                
                                if (predicate.check(statesTable[stateId]))
                                {
                                    entryStateId = stateId;
                                    break;
                                }
                            }
                        }
                        else // Use the default variant of this block
                        {
                            foreach (var stateId in stateListTable[blockId])
                            {
                                entryStateId = stateId;
                                break;
                            }
                        }
                    }

                    mapping[entryIdx] = new((ushort) entryStateId);
                }

                var world = handler.GetWorld();

                var blocks = (object[]) nbt["blocks"];

                for (int blockIdx = 0;blockIdx < blocks.Length;blockIdx++)
                {
                    var blockInfo = (Dictionary<string, object>) blocks[blockIdx];
                    var pos = (object[]) blockInfo["pos"];
                    int posX = (int) pos[0], posY = (int) pos[1], posZ = (int) pos[2];
                    var block = mapping[(int) blockInfo["state"]];

                    var loc = new Location(posX, posY, posZ) + offset;

                    world.SetBlock(loc, block);
                }

                List<Location> locs = new();

                for (int x = offsetX;x < offsetX + sizeX;x += Chunk.SizeX)
                    for (int z = offsetZ;z < offsetZ + sizeZ;z += Chunk.SizeZ)
                        for (int y = offsetY;y < offsetY + sizeY;y += Chunk.SizeY)
                        {
                            var loc = new Location(x, y, z);
                            locs.Add(loc);
                        }

                // Broadcast event to update world render
                Loom.QueueOnMainThread(() => {
                        EventManager.Instance.Broadcast<BlocksUpdateEvent>(new(locs));
                    }
                );

                CornClient.ShowNotification($"Structure generated at ({offsetX}, {offsetY}, {offsetZ})",
                        UI.Notification.Type.Success);
                
                return true;
            }
            else
            {
                CornClient.ShowNotification($"Structure file not found at {path}",
                        UI.Notification.Type.Error);

                return false;
            }
        }

        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            return true;
        }

        public bool SendCloseWindow(int windowId)
        {
            return true;
        }

        public bool SendCreativeInventoryAction(int slot, Item itemType, int count, Dictionary<string, object>? nbt)
        {
            return true;
        }

        public bool SendEntityAction(int entityId, int type)
        {
            return true;
        }

        public bool SendHeldItemChange(short slot)
        {
            return true;
        }

        public bool SendInteractEntity(int entityId, int type)
        {
            return SendInteractEntity(entityId, type, 0);
        }

        public bool SendInteractEntity(int entityId, int type, float X, float Y, float Z, int hand)
        {
            return true;
        }

        public bool SendInteractEntity(int entityId, int type, float X, float Y, float Z)
        {
            return true;
        }

        public bool SendInteractEntity(int entityId, int type, int hand)
        {
            if (type == 1)
            {
                if (worldEntities.ContainsKey(entityId))
                {
                    worldEntities[entityId].Health -= 1F;
                }
                else
                    return false;
            }

            return true;
        }

        public bool SendLocationUpdate(Location location, bool onGround, float? yaw, float? pitch)
        {
            return true;
        }

        public bool SendPlayerBlockPlacement(int hand, Location location, Direction face, int sequenceId)
        {
            return true;
        }

        public bool SendPlayerDigging(int status, Location location, Direction face, int sequenceId)
        {
            return true;
        }

        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            return true;
        }

        public bool SendRespawnPacket()
        {
            return true;
        }

        public bool SendSpectate(Guid uuid)
        {
            return true;
        }

        public bool SendUpdateSign(Location location, string line1, string line2, string line3, string line4)
        {
            return true;
        }

        public bool SendUseItem(int hand, int sequenceId)
        {
            return true;
        }

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, ItemStack? item, List<Tuple<short, ItemStack?>> changedSlots, int stateId)
        {
            return true;
        }
    }
}