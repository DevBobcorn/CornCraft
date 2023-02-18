#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Event;
using System.Diagnostics;

namespace MinecraftClient.Protocol.Handlers
{
    public class ProtocolPseudo : IMinecraftCom
    {
        public const int DEFAULT_PROTOCOL = ProtocolMinecraft.MC_1_16_5_Version;
        public const string SERVER_NAME = "world.execute(me);";

        private int protocolVersion;

        IMinecraftComHandler handler;
        Tuple<Thread, CancellationTokenSource>? netMain = null; // Net main thread

        public ProtocolPseudo(int protocolVersion, IMinecraftComHandler handler)
        {
            ChatParser.InitTranslations();
            this.handler = handler;
            this.protocolVersion = protocolVersion;

            StartUpdating();

            SetupTestScene();
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
                Stopwatch stopWatch = new();
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

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

        private void SetupTestScene()
        {
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

            int chunkY = 0, testSize = 5;
            var chunkMask = 0b1;

            for (int chunkX = -testSize;chunkX <= testSize;chunkX++)
                for (int chunkZ = -testSize;chunkZ <= testSize;chunkZ++)
                {
                    Chunk chunk = new(world);

                    for (int x = 0;x < Chunk.SizeX;x++)
                        for (int z = 0;z < Chunk.SizeZ;z++)
                            for (int y = 0;y < ((chunkX + chunkZ) % 2 == 0 ? 1 : 2);y++)
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

                    c.ChunkMask = chunkMask;
                    c.FullyLoaded = true;

                    // Broadcast event to update world render
                    Loom.QueueOnMainThread(() => {
                            EventManager.Instance.Broadcast<ReceiveChunkColumnEvent>(new(chunkX, chunkZ));
                        }
                    );
                }
            
            // Initialize player position
            handler.OnPlayerJoin(new(handler.GetUserUUID(), handler.GetUsername(), null, 0, 0, null, null, null, null));

            handler.UpdateLocation(new(5, 10, 5), 0F, 0F);
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

            if (message.Contains("gamemode"))
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
                        UnityEngine.Debug.LogWarning($"Unknown gamemode: {mode}");
                        break;
                }
            }

            return true;
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
            return true;
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