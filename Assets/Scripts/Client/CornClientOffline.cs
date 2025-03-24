using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using CraftSharp.Control;
using CraftSharp.Event;
using CraftSharp.Protocol;
using CraftSharp.UI;
using CraftSharp.Inventory;
using CraftSharp.Rendering;
using CraftSharp.Protocol.Message;

namespace CraftSharp
{
    [RequireComponent(typeof (InteractionUpdater))]
    public class CornClientOffline : BaseCornClient
    {
#nullable enable

        #region Login Information
        public static readonly int DUMMY_PROTOCOL_VERSION = ProtocolHandler.GetMinSupported();
        public static readonly string DUMMY_USERNAME = "dummy_user";
        private Guid uuid = new("069a79f4-44e9-4726-a5be-fca90e38aaf5");
        #endregion

        #region Dummy Client Control

        #endregion

        #nullable disable

        #region Players and Entities
        private bool locationReceived = false;
        private readonly EntityData clientEntity = new(0, EntityType.DUMMY_ENTITY_TYPE, Location.Zero);
        private readonly Dictionary<int, Container> inventories = new();
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();
        #endregion

        private void Start()
        {
            if (CornApp.CurrentClient == null) // In case where the client wasn't properly assigned before
            {
                CornApp.SetCurrentClient(this);

                // Start up by self since it's not started from login screen
                StartClient(new(false, new(), null, "dummy", 0, DUMMY_PROTOCOL_VERSION, null, "dummy_player"));
            }

            // Set up screen control
            ScreenControl.SetClient(this);
            
            // Setup chunk render manager
            ChunkRenderManager.SetClient(this);

            // Set up environment manager
            EnvironmentManager.SetCamera(m_MainCamera);

            // Freeze player controller until terrain is ready
            PlayerController.DisablePhysics();
        }

        public override bool StartClient(StartLoginInfo info)
        {
            var session = info.Session;

            if (!EntityTypePalette.INSTANCE.CheckId(EntityType.PLAYER_ID))
            {
                // Entity type not present, create a dummy one for player
                EntityTypePalette.INSTANCE.InjectEntityType(2077, EntityType.PLAYER_ID);
            }

            // Update entity type for dummy client entity
            clientEntity.Type = EntityTypePalette.INSTANCE.GetById(EntityType.PLAYER_ID);
            // Update client entity name
            clientEntity.Name = session.PlayerName;
            clientEntity.UUID = uuid;
            clientEntity.MaxHealth = 20F;

            // Create player render
            SwitchFirstPlayerRender(clientEntity);
            // Create camera controller
            SwitchFirstCameraController();

            return true; // Client successfully started
        }

        private void Update()
        {
            if (Keyboard.current.f5Key.isPressed)
            {
                if (Keyboard.current.lKey.wasPressedThisFrame) // Debug function, update environment lighting
                {
                    CornApp.Notify(Translations.Get("rendering.debug.update_env_lighting"));
                    // Recalculate dynamic GI
                    DynamicGI.UpdateEnvironment();
                }
                
                if (Keyboard.current.cKey.wasPressedThisFrame) // Debug function, rebuild chunk renders
                {
                    CornApp.Notify(Translations.Get("rendering.debug.reload_chunk_render"));
                    // Don't destroy block entity renders
                    ChunkRenderManager.ReloadChunksRender(false);
                }
            }

            if (PlayerController != null)
            {
                if (Keyboard.current.f6Key.wasPressedThisFrame) // Select previous
                {
                    SwitchPlayerRenderBy(clientEntity, -1);
                }
                else if (Keyboard.current.f7Key.wasPressedThisFrame) // Regenerate current prefab
                {
                    SwitchPlayerRenderBy(clientEntity,  0);
                }
                else if (Keyboard.current.f8Key.wasPressedThisFrame) // Select next
                {
                    SwitchPlayerRenderBy(clientEntity,  1);
                }
            }

            if (Keyboard.current.shiftKey.isPressed)
            {
                if (Keyboard.current.tabKey.wasPressedThisFrame) // Select next camera controller
                {
                    SwitchCameraControllerBy(1);
                }
            }
        }

        public override void Disconnect()
        {
            // Clear item mesh cache
            ItemMeshBuilder.ClearMeshCache();
            
            // Return to login scene
            CornApp.BackToLogin();
        }

        #region Getters: Retrieve data for use in other methods
        #nullable enable

        // Retrieve client connection info
        public override string GetServerHost() => string.Empty;
        public override int GetServerPort() => 0;
        public override int GetProtocolVersion() => DUMMY_PROTOCOL_VERSION;
        public override string GetUsername() => DUMMY_USERNAME!;
        public override Guid GetUserUuid() => uuid;
        public override string GetUserUuidStr() => uuid.ToString().Replace("-", string.Empty);
        public override string GetSessionId() => string.Empty;
        public override double GetServerTps() => 20;
        public override int GetPacketCount() => 0;
        public override int GetClientEntityId() => clientEntity.Id;
        public override float GetTickMilSec() => 50F;

        /// <summary>
        /// Get current chunk render manager
        /// </summary>
        public override IChunkRenderManager GetChunkRenderManager()
        {
            return ChunkRenderManager;
        }

        /// <summary>
        /// Get player inventory with a given id
        /// </summary>
        public override Container? GetInventory(int inventoryId)
        {
            if (inventories.ContainsKey(inventoryId))
                return inventories[inventoryId];
            return null;
        }

        /// <summary>
        /// Get item stack held by client player
        /// </summary>
        public override ItemStack? GetActiveItem()
        {
            return GetInventory(0)?.GetHotbarItem(CurrentSlot);
        }

        /// <summary>
        /// Get current player location (in Minecraft world)
        /// </summary>
        public override Location GetCurrentLocation()
        {
            return PlayerController.Location2Send;
        }

        /// <summary>
        /// Get current player position (in Unity scene)
        /// </summary>
        public override Vector3 GetPosition()
        {
            return CoordConvert.MC2Unity(WorldOriginOffset, PlayerController.Location2Send);
        }

        /// <summary>
        /// Get current status about the client
        /// </summary>
        /// <returns>Status info string</returns>
        public override string GetInfoString(bool withDebugInfo)
        {
            string baseString = $"FPS: {Mathf.Round(1F / Time.deltaTime), 4}\n{GameMode}\nTime: {EnvironmentManager.GetTimeString()}";

            if (withDebugInfo)
            {
                // Light debugging
                var playerBlockLoc = GetCurrentLocation().GetBlockLoc();

                var dimensionId = ChunkRenderManager.GetDimensionId();
                var biomeId = ChunkRenderManager.GetBiome(playerBlockLoc).BiomeId;

                // Ray casting debugging
                string targetInfo;
                if (interactionUpdater.TargetBlockLoc is not null)
                {
                    var targetBlockLoc = interactionUpdater.TargetBlockLoc.Value;
                    var targetDirection = interactionUpdater.TargetDirection!.Value;
                    var targetBlock = ChunkRenderManager.GetBlock(targetBlockLoc);
                    targetInfo = $"Target: {targetBlockLoc} ({targetDirection}) {targetBlock.State}";
                }
                else
                {
                    targetInfo = string.Empty;
                }

                return baseString + $"\nLoc: {GetCurrentLocation()}\n{PlayerController.GetDebugInfo()}\nDimension: {dimensionId}\nBiome: {biomeId}\n{targetInfo}\nWorld Origin Offset: {WorldOriginOffset}" +
                        $"\n{ChunkRenderManager.GetDebugInfo()}\n{EntityRenderManager.GetDebugInfo()}\nServer TPS: {GetServerTps():0.0}";
            }
            
            return baseString;
        }

        /// <summary>
        /// Get all players latency
        /// </summary>
        public override Dictionary<string, int> GetPlayersLatency()
        {
            Dictionary<string, int> playersLatency = new();
            foreach (var player in onlinePlayers)
                playersLatency.Add(player.Value.Name, player.Value.Ping);
            return playersLatency;
        }

        /// <summary>
        /// Get latency for current player
        /// </summary>
        public override int GetOwnLatency() => onlinePlayers.ContainsKey(uuid) ? onlinePlayers[uuid].Ping : 0;

        /// <summary>
        /// Get player info from uuid
        /// </summary>
        /// <param name="uuid">Player's UUID</param>
        public override PlayerInfo? GetPlayerInfo(Guid uuid)
        {
            lock (onlinePlayers)
            {
                if (onlinePlayers.ContainsKey(uuid))
                    return onlinePlayers[uuid];
                else
                    return null;
            }
        }
        
        /// <summary>
        /// Get a set of online player names
        /// </summary>
        /// <returns>Online player names</returns>
        public override string[] GetOnlinePlayers()
        {
            lock (onlinePlayers)
            {
                string[] playerNames = new string[onlinePlayers.Count];
                int idx = 0;
                foreach (var player in onlinePlayers)
                    playerNames[idx++] = player.Value.Name;
                return playerNames;
            }
        }

        /// <summary>
        /// Get a dictionary of online player names and their corresponding UUID
        /// </summary>
        /// <returns>Dictionay of online players, key is UUID, value is Player name</returns>
        public override Dictionary<string, string> GetOnlinePlayersWithUuid()
        {
            var uuid2Player = new Dictionary<string, string>();
            lock (onlinePlayers)
            {
                foreach (Guid key in onlinePlayers.Keys)
                {
                    uuid2Player.Add(key.ToString(), onlinePlayers[key].Name);
                }
            }
            return uuid2Player;
        }

        #endregion

        #region Action methods (Dummy): Perform an action on the server

        public event Action<string>? OnDummySendChat;

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// </summary>
        public override void TrySendChat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            OnDummySendChat?.Invoke(text);
        }

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        public override bool SendRespawnPacket()
        {
            return true;
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified ID
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool SendEntityAction(EntityActionType entityAction)
        {
            return false;
        }

        /// <summary>
        /// Allows the user to send requests to complete current command
        /// </summary>
        public override void SendAutoCompleteRequest(string text) { }

        /// <summary>
        /// Use the item currently in the player's main hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool UseItemOnMainHand()
        {
            return false;
        }

        /// <summary>
        /// Use the item currently in the player's off hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool UseItemOnOffHand()
        {
            return false;
        }

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public override bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            return false;
        }

        /// <summary>
        /// Give Creative Mode items into regular/survival Player Inventory
        /// </summary>
        /// <remarks>(obviously) requires to be in creative mode</remarks>
        /// <param name="slot">Destination inventory slot</param>
        /// <param name="itemType">Item type</param>
        /// <param name="count">Item count</param>
        /// <param name="nbt">Item NBT</param>
        /// <returns>TRUE if item given successfully</returns>
        public override bool DoCreativeGive(int slot, Item itemType, int count, Dictionary<string, object>? nbt = null)
        {
            return false;
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public override bool DoAnimation(int animation)
        {
            return false;
        }

        /// <summary>
        /// Close the specified inventory window
        /// </summary>
        /// <param name="windowId">Window Id</param>
        /// <returns>TRUE if the window was successfully closed</returns>
        /// <remarks>Sending close window for inventory 0 can cause server to update our inventory if there are any item in the crafting area</remarks>
        public override bool CloseInventory(int windowId)
        {
            return false;
        }

        /// <summary>
        /// Clean all inventory
        /// </summary>
        /// <returns>TRUE if the successfully cleared</returns>
        public override bool ClearInventories()
        {
            return false;
        }

        /// <summary>
        /// Interact with an entity
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="type">0: interact, 1: attack, 2: interact at</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if interaction succeeded</returns>
        public override bool InteractEntity(int entityId, int type, Hand hand = Hand.MainHand)
        {
            return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="blockLoc">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <returns>TRUE if successfully placed</returns>
        public override bool PlaceBlock(BlockLoc blockLoc, Direction blockFace, float x, float y, float z, Hand hand = Hand.MainHand)
        {
            return false;
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="blockLoc">Location of block to dig</param>
        /// <param name="blockFace">Block face</param>
        /// <param name="status">Digging status</param>
        public override bool DigBlock(BlockLoc blockLoc, Direction blockFace, DiggingStatus status = DiggingStatus.Started)
        {
            return false;
        }

        /// <summary>
        /// Drop item in active hotbar slot
        /// </summary>
        /// <param name="dropEntireStack">Whether or not to drop the entire item stack</param>
        public override bool DropItem(bool dropEntireStack)
        {
            return false;
        }

        /// <summary>
        /// Swap item stacks in main hand and off hand
        /// </summary>
        public override bool SwapItemOnHands()
        {
            return false;
        }

        /// <summary>
        /// Change active slot in the player inventory
        /// </summary>
        /// <param name="slot">Slot to activate (0 to 8)</param>
        /// <returns>TRUE if the slot was changed</returns>
        public override bool ChangeSlot(short slot)
        {
            if (slot < 0 || slot > 8)
                return false;

            DummyOnHeldItemChange(Convert.ToByte(slot));

            return true;
        }

        #endregion

        #region Event handlers (Dummy): Handle an event occurred on the server

        public void DummyInitializeBiomes((ResourceLocation id, int numId, object? obj)[] biomeList)
        {
            World.StoreBiomeList(biomeList);
        }

        public void DummyUpdateLocation(Location location, float yaw, float pitch, bool useTerrain)
        {
            if (!locationReceived) // On entering world or respawning
            {
                locationReceived = true;

                // Update player location
                PlayerController.SetLocationFromServer(location, mcYaw: yaw);

                if (useTerrain)
                {
                    // Force refresh environment collider
                    ChunkRenderManager.InitializeBoxTerrainCollider(location.GetBlockLoc(), () =>
                    {
                        PlayerController.EnablePhysics();
                    });
                }
                else
                {
                    PlayerController.EnablePhysics();
                }

                // Update camera yaw (convert to Unity yaw)
                CameraController.SetYaw(yaw + 90F);
            }
            else
            {
                // Force refresh environment collider
                ChunkRenderManager.RebuildTerrainBoxCollider(location.GetBlockLoc());
                // Then update player location
                PlayerController.SetLocationFromServer(location, reset: true, mcYaw: yaw);
                //Debug.Log($"Updated to {location} offset: {offset.magnitude}");
            }
        }
    
        /// <summary>
        /// Received chat/system message from the server
        /// </summary>
        /// <param name="message">Message received</param>
        public void DummyOnTextReceived(ChatMessage message)
        {
            string messageText;

            if (message.isJson)
                messageText = ChatParser.ParseText(message.content);
            else
                messageText = message.content;
            
            EventManager.Instance.BroadcastOnUnityThread<ChatMessageEvent>(new(messageText));
        }

        /// <summary>
        /// Dummy method for receiving chunk data from dummy server
        /// </summary>
        public void DummyOnChunkData(int chunkX, int chunkZ, int chunkMask, int chunkColumnSize, ushort[][] blockStateIds, byte[] skyLight, byte[] blockLight)
        {
            var chunksManager = ChunkRenderManager;

            for (int chunkYIndex = 0, curIndex = 0; chunkYIndex < chunkColumnSize; chunkYIndex++)
            {
                if ((chunkMask & (1 << chunkYIndex)) != 0) // Chunk is not empty
                {
                    var chunk = new Chunk();

                    for (int blockY = 0; blockY < 16; blockY++)
                        for (int blockZ = 0; blockZ < 16; blockZ++)
                            for (int blockX = 0; blockX < 16; blockX++)
                            {
                                var block = new Block(blockStateIds[curIndex][(blockY << 8) | (blockZ << 4) | blockX]);
                                chunk.SetWithoutCheck(blockX, blockY, blockZ, block);
                            }
                    

                    chunksManager.StoreChunk(chunkX, chunkYIndex, chunkZ, chunkColumnSize, chunk);

                    curIndex++;
                }
            }

            // Set light data and mark as loaded
            var c = chunksManager.GetChunkColumn(chunkX, chunkZ);
            c.SetLights(skyLight, blockLight);
            c.FullyLoaded = true;
        }

        /// <summary>
        /// Dummy method for receiving chunk unload action from dummy server
        /// </summary>
        public void DummyOnChunkUnload(int chunkX, int chunkZ)
        {
            ChunkRenderManager.UnloadChunkColumn(chunkX, chunkZ);
        }

        /// <summary>
        /// Called when held item change
        /// </summary>
        /// <param name="slot"> item slot</param>
        public void DummyOnHeldItemChange(byte slot)
        {
            CurrentSlot = slot;
            var newItem = inventories[0].GetHotbarItem(slot);
            // Broad cast hotbar selection change
            EventManager.Instance.Broadcast(
                    new HeldItemChangeEvent(CurrentSlot, newItem,
                    PlayerActionHelper.GetItemActionType(newItem)));
        }

        /// <summary>
        /// When an inventory is opened
        /// </summary>
        /// <param name="inventory">The inventory</param>
        /// <param name="inventoryID">Inventory ID</param>
        public void DummyOnInventoryOpen(int inventoryID, Container inventory)
        {
            inventories[inventoryID] = inventory;

            if (inventoryID != 0)
            {
                Debug.Log(Translations.Get("extra.inventory_open", inventoryID, inventory.Title));
                Debug.Log(Translations.Get("extra.inventory_interact"));
            }
        }

        /// <summary>
        /// When a slot is set inside window items
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        public void DummyOnSetSlot(byte inventoryID, short slotID, ItemStack item, int stateId)
        {
            if (inventories.ContainsKey(inventoryID))
                inventories[inventoryID].StateId = stateId;

            // Handle inventoryID -2 - Add item to player inventory without animation
            if (inventoryID == 254)
                inventoryID = 0;
            
            // Handle cursor item
            if (inventoryID == 255 && slotID == -1)
            {
                //inventoryID = 0; // Prevent key not found for some bots relied to this event
                if (inventories.ContainsKey(0))
                {
                    if (item != null)
                        inventories[0].Items[-1] = item;
                    else
                        inventories[0].Items.Remove(-1);
                }
            }
            else
            {
                if (inventories.ContainsKey(inventoryID))
                {
                    var container = inventories[inventoryID];
                    
                    if (item == null || item.IsEmpty)
                    {
                        if (container.Items.ContainsKey(slotID))
                            container.Items.Remove(slotID);
                    }
                    else container.Items[slotID] = item;

                    EventManager.Instance.Broadcast(new SlotUpdateEvent(inventoryID, slotID, item));

                    if (container.IsHotbar(slotID, out int hotbarSlot)) // The updated slot is in the hotbar
                    {
                        EventManager.Instance.Broadcast(new HotbarUpdateEvent(hotbarSlot, item));

                        if (hotbarSlot == CurrentSlot) // The currently held item is updated
                        {
                            EventManager.Instance.Broadcast(new HeldItemChangeEvent(
                                    hotbarSlot, item, PlayerActionHelper.GetItemActionType(item)));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when the Game Mode has been updated for a player
        /// </summary>
        /// <param name="uuid">Player UUID (Empty for initial gamemode on login)</param>
        /// <param name="gamemode">New Game Mode (0: Survival, 1: Creative, 2: Adventure, 3: Spectator).</param>
        public void DummyOnGamemodeUpdate(Guid uuid, int gamemode)
        {
            GameMode = (GameMode) gamemode;

            EventManager.Instance.Broadcast<GameModeUpdateEvent>(new(GameMode));

            if (uuid != Guid.Empty)
            {
                CornApp.Notify(Translations.Get("gameplay.control.update_gamemode",
                        ChatParser.TranslateString($"gameMode.{GameMode.GetIdentifier()}")), Notification.Type.Success);
            }
        }

        #endregion
    }
}
