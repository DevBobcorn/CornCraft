using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Inventory;
using CraftSharp.Protocol.Message;


namespace CraftSharp
{
    public class DummyServer : MonoBehaviour
    {
        public CornClientOffline client;

        public GameObject placeHolderGround;

        // Dummy server-side handlers
        private Action<string> dummyChatHandler;

        private void DummyHandleCommand(string text)
        {
            if (text.StartsWith("/gamemode "))
            {
                switch (text[10..])
                {
                    case "creative":
                        client.DummyOnGamemodeUpdate(client.GetUserUuid(), (int) GameMode.Creative);
                        break;
                    case "survival":
                        client.DummyOnGamemodeUpdate(client.GetUserUuid(), (int) GameMode.Survival);
                        break;
                    case "adventure":
                        client.DummyOnGamemodeUpdate(client.GetUserUuid(), (int) GameMode.Adventure);
                        break;
                    case "spectator":
                        client.DummyOnGamemodeUpdate(client.GetUserUuid(), (int) GameMode.Spectator);
                        break;
                    
                    default:
                        client.DummyOnTextReceived(new ChatMessage($"§cInvalid gamemode: {text[10..]}", "DUMMY SERVER", false, 0, Guid.Empty, true));
                        break;
                }

                return;
            }

            client.DummyOnTextReceived(new ChatMessage($"§cInvalid command: {text}", "DUMMY SERVER", false, 0, Guid.Empty, true));
        }

        void Start()
        {
            // TODO: Load a world save

            
            if (client != null)
            {
                client.OnDummySendChat += text =>
                {
                    if (text.StartsWith('/'))
                    {
                        DummyHandleCommand(text);
                    }
                    else
                    {
                        client.DummyOnTextReceived(new ChatMessage($"{client.GetUsername()}: {text}", "DUMMY CLIENT", false, 0, client.GetUserUuid(), false));
                    }
                };

                var dataLoaded = BlockStatePalette.INSTANCE.CheckNumId(1);

                if (dataLoaded)
                {
                    placeHolderGround.SetActive(false);

                    // Fill in chunk data
                    var chunksManager = client.ChunkRenderManager;
                    var chunkHeight = World.GetDimensionType().height;
                    var minChunkYOffset = -Mathf.FloorToInt(World.GetDimensionType().minY / 16F);
                    var chunkColumnSize = Mathf.CeilToInt(chunkHeight / (float) Chunk.SIZE);

                    var blockA = new Block(1); // stone
                    var blockB = new Block(2); // granite
                    var chunkA = new Chunk();
                    var chunkB = new Chunk();

                    var emptyLight = new byte[4096 * (chunkColumnSize + 2)];

                    for (int x = 0; x < Chunk.SIZE; x++)
                        for (int y = 0; y < Chunk.SIZE; y++)
                            for (int z = 0; z < Chunk.SIZE; z++)
                            {
                                chunkA.SetWithoutCheck(x, y, z, blockA);
                            }
                    
                    for (int x = 0; x < Chunk.SIZE; x++)
                        for (int y = 0; y < Chunk.SIZE - 1; y++)
                            for (int z = 0; z < Chunk.SIZE; z++)
                            {
                                chunkB.SetWithoutCheck(x, y, z, blockB);
                            }
                    
                    for (int chunkX = -5; chunkX < 5; chunkX++)
                        for (int chunkZ = -5; chunkZ < 5; chunkZ++)
                        {
                            if (chunkX == -5 || chunkZ == -5 || chunkX == 4 || chunkZ == 4)
                            {
                                // Pad with empty chunks
                                chunksManager.StoreChunk(chunkX, minChunkYOffset, chunkZ, chunkColumnSize, null);
                            }
                            else
                            {
                                // Fill chunk at height 0~15 in the column
                                chunksManager.StoreChunk(chunkX, minChunkYOffset, chunkZ, chunkColumnSize, (chunkX + chunkZ) % 2 == 0 ? chunkA : chunkB);
                            }
                            

                            // Set light data and mark as loaded
                            var c = chunksManager.GetChunkColumn(chunkX, chunkZ);
                            c.SetLights(emptyLight, emptyLight);
                            c.FullyLoaded = true;
                        }
                }
                else
                {
                    // No data loaded, use playerholder instead
                    placeHolderGround.SetActive(true);
                }

                StartCoroutine(DeferredInitialization(dataLoaded));
            }
        }

        private IEnumerator DeferredInitialization(bool dataLoaded)
        {
            yield return new WaitForEndOfFrame();

            // Initialize dummy biome
            client.DummyInitializeBiomes(new (ResourceLocation id, int numId, object obj)[]
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
            client.DummyUpdateLocation(new Location(0, dataLoaded ? 16 : 0, 0), 0, 0);

            // Send initial gamemode (initialization, use empty UUID)
            client.DummyOnGamemodeUpdate(Guid.Empty, (int) GameMode.Creative);

            // Send initial inventory
            client.DummyOnInventoryOpen(0, new Container(ContainerType.PlayerInventory));

            if (dataLoaded)
            {
                client.DummyOnSetSlot(0, 36, new ItemStack(ItemPalette.INSTANCE.GetById(new("diamond_sword")), 1), 0);
                client.DummyOnSetSlot(0, 37, new ItemStack(ItemPalette.INSTANCE.GetById(new("bow")), 1), 0);
            }
        }

        void Update()
        {
            if (client != null)
            {
                if (client.GetPosition().y < -100)
                {
                    // Reset player position
                    client.DummyUpdateLocation(new Location(0, 16, 0), 0, 0);
                }
            }
        }
    }
}