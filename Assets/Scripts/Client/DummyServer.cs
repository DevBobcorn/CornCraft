using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Inventory;
using CraftSharp.Protocol;
using CraftSharp.Protocol.Message;


namespace CraftSharp
{
    public class DummyServer : MonoBehaviour
    {
        public CornClientOffline client;

        public GameObject placeHolderGround;

        private int lastPlayerChunkX;
        private int lastPlayerChunkZ;

        /// <summary>
        /// Records coordinates of chunks which has been sent to client
        /// </summary>
        private readonly HashSet<Vector2Int> sentChunks = new();

        private bool dataLoaded = false;

        private void DummyHandleCommand(string text)
        {
            if (text.StartsWith("/gamemode "))
            {
                switch (text[10..])
                {
                    case "creative":
                        client.DummyOnGamemodeUpdate(client.GetUserUUID(), (int) GameMode.Creative);
                        break;
                    case "survival":
                        client.DummyOnGamemodeUpdate(client.GetUserUUID(), (int) GameMode.Survival);
                        break;
                    case "adventure":
                        client.DummyOnGamemodeUpdate(client.GetUserUUID(), (int) GameMode.Adventure);
                        break;
                    case "spectator":
                        client.DummyOnGamemodeUpdate(client.GetUserUUID(), (int) GameMode.Spectator);
                        break;
                    
                    default:
                        client.DummyOnTextReceived(new ChatMessage($"§cInvalid gamemode: {text[10..]}", "DUMMY SERVER", false, 0, Guid.Empty, true));
                        break;
                }

                return;
            }

            client.DummyOnTextReceived(new ChatMessage($"§cInvalid command: {text}", "DUMMY SERVER", false, 0, Guid.Empty, true));
        }

        private void DummySendInitialTerrainData()
        {
            if (!client || !dataLoaded) return;

            var chunkHeight = World.GetDimensionType().height;
            var chunkColumnSize = Mathf.CeilToInt(chunkHeight / (float) Chunk.SIZE);

            // Only one chunk in the column is not empty
            var minChunkYIndex = -Mathf.FloorToInt(World.GetDimensionType().minY / 16F);
            var chunkMask = 1 << minChunkYIndex;
            var emptyLight = new byte[4096 * (chunkColumnSize + 2)];
            var columnA = new[] { new ushort[4096] }; // 4096 * 1
            var columnB = new[] { new ushort[4096] }; // 4096 * 1

            Array.Fill(columnA[0], (ushort) 1); // Fill with stone
            Array.Fill(columnB[0], (ushort) 2, 0, 256 * 15); // Fill with granite, except top layer

            for (int chunkX = -5; chunkX < 5; chunkX++)
                for (int chunkZ = -5; chunkZ < 5; chunkZ++)
                {
                    client.DummyOnChunkData(chunkX, chunkZ, chunkMask, chunkColumnSize, (chunkX + chunkZ) % 2 == 0 ? columnA : columnB, emptyLight, emptyLight);

                    // Record sent chunks
                    sentChunks.Add(new Vector2Int(chunkX, chunkZ));
                }
        }

        private void DummySendTerrainDataUpdate(int clientChunkX, int clientChunkZ)
        {
            if (!client || !dataLoaded) return;

            var chunkHeight = World.GetDimensionType().height;
            var chunkColumnSize = Mathf.CeilToInt(chunkHeight / (float) Chunk.SIZE);

            // Unload chunks that are too far from client player
            sentChunks.RemoveWhere(coord =>
            {
                bool remove = Mathf.Abs(coord.x - clientChunkX) > 6 || Mathf.Abs(coord.y - clientChunkZ) > 6;

                if (remove) client.DummyOnChunkUnload(coord.x, coord.y);

                return remove;
            });

            // Only one chunk in the column is not empty
            var minChunkYIndex = -Mathf.FloorToInt(World.GetDimensionType().minY / 16F);
            var chunkMask = 1 << minChunkYIndex;
            var emptyLight = new byte[4096 * (chunkColumnSize + 2)];
            var columnA = new[] { new ushort[4096] }; // 4096 * 1
            var columnB = new[] { new ushort[4096] }; // 4096 * 1

            Array.Fill(columnA[0], (ushort) 1); // Fill with stone
            Array.Fill(columnB[0], (ushort) 2, 0, 256 * 15); // Fill with granite, except top layer

            // Sent chunk data near the client player
            for (int chunkX = clientChunkX - 5; chunkX <= clientChunkX + 5; chunkX++)
                for (int chunkZ = clientChunkZ - 5; chunkZ <= clientChunkZ + 5; chunkZ++)
                {
                    var coord = new Vector2Int(chunkX, chunkZ);

                    if (sentChunks.Contains(coord)) continue;

                    client.DummyOnChunkData(chunkX, chunkZ, chunkMask, chunkColumnSize, (chunkX + chunkZ) % 2 == 0 ? columnA : columnB, emptyLight, emptyLight);

                    // Record sent chunks
                    sentChunks.Add(coord);
                }
        }

        private void Start()
        {
            if (client)
            {
                client.OnDummySendChat += text =>
                {
                    if (text.StartsWith('/'))
                    {
                        DummyHandleCommand(text);
                    }
                    else
                    {
                        client.DummyOnTextReceived(new ChatMessage($"{client.GetUsername()}: {text}", client.GetUsername(), false, 0, client.GetUserUUID()));
                    }
                };

                dataLoaded = BlockStatePalette.INSTANCE.CheckNumId(1);

                if (dataLoaded)
                {
                    placeHolderGround.SetActive(false);

                    // Send initial terrain data
                    DummySendInitialTerrainData();
                }
                else
                {
                    // No data loaded, use placeholder instead
                    placeHolderGround.SetActive(true);
                }

                StartCoroutine(DeferredInitialization());
            }
        }

        private IEnumerator DeferredInitialization()
        {
            yield return new WaitForEndOfFrame();

            // Initialize dummy biome
            CornClientOffline.DummyInitializeBiomes(new (ResourceLocation id, int numId, object obj)[]
            {
                (
                    new ResourceLocation("plains"), 0,
                    new Dictionary<string, object>
                    {
                        ["downfall"]    = 0.4F,
                        ["temperature"] = 0.8F,
                        ["effects"]     = new Dictionary<string, object>()
                        {
                            ["fog_color"]       = 12638463,
                            ["sky_color"]       = 7907327,
                            ["water_color"]     = 4159204,
                            ["water_fog_color"] = 329011,
                        }
                    }
                )
            });

            // Send initial location and yaw
            var startLocation = new Location(0, dataLoaded ? 16 : 0, 0);
            client.DummyUpdateLocation(startLocation, 0, 0, dataLoaded);

            lastPlayerChunkX = 0;
            lastPlayerChunkZ = 0;

            // Send initial gamemode (initialization, use empty UUID)
            client.DummyOnGamemodeUpdate(Guid.Empty, (int) GameMode.Creative);

            // Send initial inventory
            client.DummyOnInventoryOpen(0, new InventoryData(0, InventorySlotTypePalette.INSTANCE.PLAYER,
                ChatParser.TranslateString("container.inventory")));

            if (dataLoaded)
            {
                client.DummyOnInventorySlot(0, 36, new ItemStack(ItemPalette.INSTANCE.GetById(new("diamond_sword")), 1), 0);
                client.DummyOnInventorySlot(0, 37, new ItemStack(ItemPalette.INSTANCE.GetById(new("bow")), 1), 0);
            }
        }

        /// <summary>
        /// TODO: Maybe use a thread to do the update
        /// </summary>
        private void FixedUpdate()
        {
            if (client)
            {
                if (client.GetPosition().y < -100)
                {
                    // Reset player position
                    client.DummyUpdateLocation(new Location(0, 16, 0), 0, 0, dataLoaded);

                    // Don't reset reset last position so as to trigger terrain data update
                }

                var currentLoc = client.GetCurrentLocation().GetBlockLoc();
                var currentChunkX = currentLoc.GetChunkX();
                var currentChunkZ = currentLoc.GetChunkZ();

                if (currentChunkX != lastPlayerChunkX || currentChunkZ != lastPlayerChunkZ)
                {
                    lastPlayerChunkX = currentChunkX;
                    lastPlayerChunkZ = currentChunkZ;

                    DummySendTerrainDataUpdate(currentChunkX, currentChunkZ);
                }
            }
        }
    }
}