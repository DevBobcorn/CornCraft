using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

using CraftSharp.Control;
using CraftSharp.Event;
using CraftSharp.Protocol;
using CraftSharp.Protocol.Handlers;
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Message;
using CraftSharp.Proxy;
using CraftSharp.UI;
using CraftSharp.Inventory;
using CraftSharp.Rendering;

namespace CraftSharp
{
    [RequireComponent(typeof (InteractionUpdater))]
    public class CornClientOnline : BaseCornClient, IMinecraftComHandler
    {
        #nullable enable

        #region Login Information
        private string? host;
        private int port;
        private int protocolVersion;
        private string? username;
        private Guid uuid;
        private string? sessionId;
        private PlayerKeyPair? playerKeyPair;
        #endregion

        #region Thread and Chat Control
        private readonly Queue<Action> threadTasks = new();
        private readonly object threadTasksLock = new();

        private readonly Queue<string> chatQueue = new();
        private DateTime nextMessageSendTime = DateTime.MinValue;
        private bool canSendMessage = false;

        #endregion

        #region Time and Networking
        private DateTime lastKeepAlive;
        private readonly object lastKeepAliveLock = new();
        private long lastAge = 0L;
        private DateTime lastTime;
        private double serverTPS = 0;
        private double averageTPS = 20;
        private const int maxSamples = 5;
        private readonly List<double> tpsSamples = new(maxSamples);
        private double sampleSum = 0;
        private int packetCount = 0;
        
        TcpClient? tcpClient;
        IMinecraftCom? handler;
        Tuple<Thread, CancellationTokenSource>? timeoutdetector = null;
        #endregion

        #nullable disable

        #region Players and Entities
        private bool locationReceived = false;
        private readonly Entity clientEntity = new(0, EntityType.DUMMY_ENTITY_TYPE, Location.Zero);
        private int sequenceId; // User for player block synchronization (Aka. digging, placing blocks, etc..)
        private int foodSaturation, level, totalExperience;
        private readonly Dictionary<int, Container> inventories = new();
        private readonly object movementLock = new();
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();
        #endregion

        void Awake() // In case where the client wasn't properly assigned before
        {
            if (CornApp.CurrentClient == null)
            {
                CornApp.SetCurrentClient(this);
            }
        }

        void Start()
        {
            // Set up screen control
            ScreenControl.SetClient(this);
            ScreenControl.SetLoadingScreen(true);
            
            // Set up chunk render manager
            ChunkRenderManager.SetClient(this);

            // Set up environment manager
            EnvironmentManager.SetCamera(m_MainCamera);

            // Freeze player controller until terrain is ready
            PlayerController.DisablePhysics();
        }

        public override bool StartClient(StartLoginInfo info)
        {
            var session = info.Session;

            this.sessionId = session.Id;
            if (!Guid.TryParse(session.PlayerId, out this.uuid))
                this.uuid = Guid.Empty;
            this.username = session.PlayerName;
            this.host = info.ServerIp;
            this.port = info.ServerPort;
            this.protocolVersion = info.ProtocolVersion;
            this.playerKeyPair = info.Player;

            // Start up client
            try
            {
                // Setup tcp client
                tcpClient = ProxyHandler.newTcpClient(host, port);
                tcpClient.ReceiveBufferSize = 1024 * 1024;
                tcpClient.ReceiveTimeout = 30000; // 30 seconds

                // Create handler
                handler = ProtocolHandler.GetProtocolHandler(tcpClient, info.ProtocolVersion, info.ForgeInfo, this);

                // Start update loop
                timeoutdetector = Tuple.Create(new Thread(new ParameterizedThreadStart(TimeoutDetector)), new CancellationTokenSource());
                timeoutdetector.Item1.Name = "Connection Timeout Detector";
                timeoutdetector.Item1.Start(timeoutdetector.Item2.Token);

                if (handler.Login(this.playerKeyPair, session, info.AccountLower)) // Login
                {
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
                else
                {
                    Debug.LogError(Translations.Get("error.login_failed"));
                    Disconnect();
                }
            }
            catch (Exception e)
            {
                tcpClient = ProxyHandler.newTcpClient(host, port);
                tcpClient.ReceiveBufferSize = 1024 * 1024;
                tcpClient.ReceiveTimeout = 30000; // 30 seconds

                Debug.LogError(Translations.Get("error.connect", e.Message));
                Debug.LogError(e.StackTrace);
                Disconnect();
            }

            return false; // Failed to start client
        }

        void Update()
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

            var playerPos = PlayerController.transform.position;

            if (Mathf.Abs(playerPos.x) > 512F || Mathf.Abs(playerPos.z) > 512F)
            {
                // World origin shifting logic
                var updatedOffset = WorldOriginOffset;

                while (playerPos.x > 512F)
                {
                    playerPos.x -= 512F;
                    updatedOffset.x += 1;
                }

                while (playerPos.x < -512F)
                {
                    playerPos.x += 512F;
                    updatedOffset.x -= 1;
                }

                while (playerPos.z > 512F)
                {
                    playerPos.z -= 512F;
                    updatedOffset.z += 1;
                }

                while (playerPos.z < -512F)
                {
                    playerPos.z += 512F;
                    updatedOffset.z -= 1;
                }

                SetWorldOriginOffset(updatedOffset);
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

        /// <summary>
        /// Retrieve messages from the queue and send.
        /// Note: requires external locking.
        /// </summary>
        private void TrySendMessageToServer()
        {
            if (!canSendMessage)
            {
                //Debug.LogWarning("Not allowed to send message now!");
                return;
            }

            while (chatQueue.Count > 0 && nextMessageSendTime < DateTime.Now)
            {
                string text = chatQueue.Dequeue();
                handler!.SendChatMessage(text, playerKeyPair);
                nextMessageSendTime = DateTime.Now + TimeSpan.FromSeconds(ProtocolSettings.MessageCooldown);
            }
        }
        
        /// <summary>
        /// Called ~20 times per second by the protocol handler (on net read thread)
        /// </summary>
        public void OnHandlerUpdate(int pc)
        {
            lock (chatQueue)
            {
                TrySendMessageToServer();
            }

            if (locationReceived)
            {
                lock (movementLock)
                {
                    handler?.SendLocationUpdate(
                            PlayerController.Location2Send,
                            PlayerController.IsGrounded2Send,
                            PlayerController.MCYaw2Send,
                            PlayerController.Pitch2Send);

                    // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
                    // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
                    //yawToSend = pitchToSend = null;
                }
            }

            lock (threadTasksLock)
            {
                while (threadTasks.Count > 0)
                {
                    Action taskToRun = threadTasks.Dequeue();
                    taskToRun();
                }
            }

            packetCount = pc;
        }

        #region Disconnect logic

        /// <summary>
        /// Periodically checks for server keepalives and consider that connection has been lost if the last received keepalive is too old.
        /// </summary>
        private void TimeoutDetector(object o)
        {
            var token = (CancellationToken) o;
            UpdateKeepAlive();

            do
            {
                for (int i = 0;i < 30;i++) // 15 seconds in total
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));

                    if (token.IsCancellationRequested) break;
                }

                if (token.IsCancellationRequested)
                    return;
                
                lock (lastKeepAliveLock)
                {
                    if (lastKeepAlive.AddSeconds(30) < DateTime.Now)
                    {
                        if (token.IsCancellationRequested)
                            return;
                        
                        OnConnectionLost(DisconnectReason.ConnectionLost, Translations.Get("error.timeout"));
                        return;
                    }
                }
            }
            while (!token.IsCancellationRequested);
        }

        /// <summary>
        /// Update last keep alive to current time
        /// </summary>
        private void UpdateKeepAlive()
        {
            lock (lastKeepAliveLock)
            {
                lastKeepAlive = DateTime.Now;
            }
        }

        /// <summary>
        /// Disconnect the client from the server
        /// </summary>
        public override void Disconnect()
        {
            handler?.Disconnect();
            handler?.Dispose();
            handler = null;

            timeoutdetector?.Item2.Cancel();
            timeoutdetector = null;

            tcpClient?.Close();
            tcpClient = null;
            
            Loom.QueueOnMainThread(() => {
                // Clear item mesh cache
                ItemMeshBuilder.ClearMeshCache();
                
                // Return to login scene
                CornApp.BackToLogin();
            });
        }

        /// <summary>
        /// When connection has been lost, login was denied or player was kicked from the server
        /// </summary>
        public void OnConnectionLost(DisconnectReason reason, string message)
        {
            Loom.QueueOnMainThread(() => {
                // Clear world data
                ChunkRenderManager.ClearChunksData();
            });

            switch (reason)
            {
                case DisconnectReason.ConnectionLost:
                    Debug.Log(Translations.Get("mcc.disconnect.lost"));
                    break;

                case DisconnectReason.InGameKick:
                    Debug.Log(Translations.Get("mcc.disconnect.server") + message);
                    break;

                case DisconnectReason.LoginRejected:
                    Debug.Log(Translations.Get("mcc.disconnect.login") + message);
                    break;

                case DisconnectReason.UserLogout:
                    throw new InvalidOperationException(Translations.Get("exception.user_logout"));
            }

            Disconnect();
        }

        #endregion

        #region Thread-Invoke: Cross-thread method calls

        /// <summary>
        /// Invoke a task on the main thread, wait for completion and retrieve return value.
        /// </summary>
        /// <param name="task">Task to run with any type or return value</param>
        /// <returns>Any result returned from task, result type is inferred from the task</returns>
        /// <example>bool result = InvokeOnNetMainThread(methodThatReturnsAbool);</example>
        /// <example>bool result = InvokeOnNetMainThread(() => methodThatReturnsAbool(argument));</example>
        /// <example>int result = InvokeOnNetMainThread(() => { yourCode(); return 42; });</example>
        /// <typeparam name="T">Type of the return value</typeparam>
        public T InvokeOnNetMainThread<T>(Func<T> task)
        {
            if (!InvokeRequired)
            {
                return task();
            }
            else
            {
                TaskWithResult<T> taskWithResult = new(task);
                lock (threadTasksLock)
                {
                    threadTasks.Enqueue(taskWithResult.ExecuteSynchronously);
                }
                return taskWithResult.WaitGetResult();
            }
        }

        /// <summary>
        /// Invoke a task on the main thread and wait for completion
        /// </summary>
        /// <param name="task">Task to run without return value</param>
        /// <example>InvokeOnNetMainThread(methodThatReturnsNothing);</example>
        /// <example>InvokeOnNetMainThread(() => methodThatReturnsNothing(argument));</example>
        /// <example>InvokeOnNetMainThread(() => { yourCode(); });</example>
        public void InvokeOnNetMainThread(Action task)
        {
            InvokeOnNetMainThread(() => { task(); return true; });
        }

        /// <summary>
        /// Clear all tasks
        /// </summary>
        public void ClearTasks()
        {
            lock (threadTasksLock)
            {
                threadTasks.Clear();
            }
        }

        /// <summary>
        /// Check if running on a different thread and InvokeOnNetMainThread is required
        /// </summary>
        /// <returns>True if calling thread is not the main thread</returns>
        public bool InvokeRequired
        {
            get
            {
                int callingThreadId = Thread.CurrentThread.ManagedThreadId;
                if (handler != null)
                {
                    return handler.GetNetMainThreadId() != callingThreadId;
                }
                else
                {
                    // net main thread not yet ready
                    return false;
                }
            }
        }

        #endregion

        #region Getters: Retrieve data for use in other methods
        #nullable enable

        // Retrieve client connection info
        public override string GetServerHost() => host!;
        public override int GetServerPort() => port;
        public override int GetProtocolVersion() => protocolVersion;
        public override string GetUsername() => username!;
        public override Guid GetUserUuid() => uuid;
        public override string GetUserUuidStr() => uuid.ToString().Replace("-", string.Empty);
        public override string GetSessionId() => sessionId!;
        public override double GetServerTps() => averageTPS;
        public override int GetPacketCount() => packetCount;
        public override float GetTickMilSec() => (float)(1D / averageTPS);

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
            return inventories.GetValueOrDefault(inventoryId);
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
            //return CoordConvert.MC2Unity(PlayerController.Location2Send);
            return PlayerController.transform.position;
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
                //var block = ChunkRenderManager.GetBlock(playerBlockLoc);
                //var lightEmission = block.State.LightEmissionLevel;
                //var lightBlockage = block.State.LightBlockageLevel;
                //var lightValue = ChunkRenderManager.GetBlockLight(playerBlockLoc);
                //var lightInfo = $"Emission: {lightEmission}\tBlockage: {lightBlockage}\nLight Value: {lightValue}";

                var dimensionId = ChunkRenderManager.GetDimensionId();
                var biomeId = ChunkRenderManager.GetBiome(playerBlockLoc).BiomeId;

                // Ray casting debugging
                string targetInfo;
                if (interactionUpdater.TargetBlockLoc is not null)
                {
                    var targetBlockLoc = interactionUpdater.TargetBlockLoc.Value;
                    var targetBlock = ChunkRenderManager.GetBlock(targetBlockLoc);
                    targetInfo = $"Target BlockLoc:\t{targetBlockLoc}\nTarget Block:\t{targetBlock.State}";
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
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return handler!.GetMaxChatMessageLength();
        }

        /// <summary>
        /// Get a list of disallowed characters in chat
        /// </summary>
        /// <returns>An array of disallowed characters</returns>
        public static char[] GetDisallowedChatCharacters()
        {
            return new char[] { (char)167, (char)127 }; // Minecraft color code and ASCII code DEL
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

        #region Action methods: Perform an action on the Server

        public void SetCanSendMessage(bool canSendMessage)
        {
            this.canSendMessage = canSendMessage;
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// </summary>
        public override void TrySendChat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                InvokeOnNetMainThread(() => SendChat(text));
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
        }

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        private void SendChat(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            int maxLength = handler!.GetMaxChatMessageLength();

            lock (chatQueue)
            {
                if (text.Length > maxLength) //Message is too long?
                {
                    if (text[0] == '/')
                    {
                        //Send the first 100/256 chars of the command
                        text = text[..maxLength];
                        chatQueue.Enqueue(text);
                    }
                    else
                    {
                        //Split the message into several messages
                        while (text.Length > maxLength)
                        {
                            chatQueue.Enqueue(text[..maxLength]);
                            text = text[maxLength..];
                        }
                        chatQueue.Enqueue(text);
                    }
                }
                else
                    chatQueue.Enqueue(text);
                
                TrySendMessageToServer();
            }
        }

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        public override bool SendRespawnPacket()
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread<bool>(SendRespawnPacket);

            return handler!.SendRespawnPacket();
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified Id
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool SendEntityAction(EntityActionType entityAction)
        {
            return InvokeOnNetMainThread(() => handler!.SendEntityAction(clientEntity.Id, (int)entityAction));
        }

        /// <summary>
        /// Allows the user to send requests to complete current command
        /// </summary>
        public override void SendAutoCompleteRequest(string text)
        {
            // We shouldn't trim the string here because spaces here really matter
            // For example, auto completing '/gamemode ' returns the game modes as
            // the result options while passing in '/gamemode' yields nothing...
            try
            {
                InvokeOnNetMainThread(() => handler!.SendAutoCompleteText(text));
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
        }

        /// <summary>
        /// Use the item currently in the player's main hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool UseItemOnMainHand()
        {
            return InvokeOnNetMainThread(() => handler!.SendUseItem(0, sequenceId++));
        }

        /// <summary>
        /// Use the item currently in the player's off hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool UseItemOnOffHand()
        {
            return InvokeOnNetMainThread(() => handler!.SendUseItem(1, sequenceId++));
        }

        /// <summary>
        /// Try to merge a slot
        /// </summary>
        /// <param name="inventory">The container where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slotId">The Id of the slot of the item to be processed</param>
        /// <param name="curItem">The slot that was put down</param>
        /// <param name="curId">The Id of the slot being put down</param>
        /// <param name="changedSlots">Record changes</param>
        /// <returns>Whether to fully merge</returns>
        private static bool TryMergeSlot(Container inventory, ItemStack item, int slotId, ItemStack curItem, int curId, List<Tuple<short, ItemStack?>> changedSlots)
        {
            int spaceLeft = curItem.ItemType.StackLimit - curItem.Count;
            if (curItem.ItemType == item!.ItemType && spaceLeft > 0)
            {
                // Put item on that stack
                if (item.Count <= spaceLeft)
                {
                    // Can fit into the stack
                    item.Count = 0;
                    curItem.Count += item.Count;

                    changedSlots.Add(new Tuple<short, ItemStack?>((short)curId, curItem));
                    changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, null));

                    inventory.Items.Remove(slotId);
                    return true;
                }
                else
                {
                    item.Count -= spaceLeft;
                    curItem.Count += spaceLeft;

                    changedSlots.Add(new Tuple<short, ItemStack?>((short)curId, curItem));
                }
            }
            return false;
        }

        /// <summary>
        /// Store items in a new slot
        /// </summary>
        /// <param name="inventory">The container where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slotId">The Id of the slot of the item to be processed</param>
        /// <param name="newSlotId">Id of the new slot</param>
        /// <param name="changedSlots">Record changes</param>
        private static void StoreInNewSlot(Container inventory, ItemStack item, int slotId, int newSlotId, List<Tuple<short, ItemStack?>> changedSlots)
        {
            ItemStack newItem = new(item.ItemType, item.Count, item.NBT);
            inventory.Items[newSlotId] = newItem;
            inventory.Items.Remove(slotId);

            changedSlots.Add(new Tuple<short, ItemStack?>((short)newSlotId, newItem));
            changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, null));
        }

        private static readonly HashSet<ResourceLocation> BEACON_FUEL_ITEM_IDS = new()
        {
            new ResourceLocation("iron_ingot"), new ResourceLocation("gold_ingot"),
            new ResourceLocation("emarald"),    new ResourceLocation("diamond"),
            new ResourceLocation("netherite_ingot")
        };

        private static readonly ResourceLocation BREWING_STAND_FUEL_ITEM_ID = new("blaze_powder");

        private static readonly HashSet<ResourceLocation> BREWING_STAND_BOTTLE_ITEM_IDS = new()
        {
            // Water Bottle's id is also minecraft:potion
            new ResourceLocation("potion"), new ResourceLocation("glass_bottle")
        };

        private static readonly ResourceLocation ENCHANTING_TABLE_FUEL_ITEM_ID = new("lapis_lazuli");

        private static readonly HashSet<ResourceLocation> CARTOGRAPHY_TABLE_EMPTY_ITEM_IDS = new()
        {
            new ResourceLocation("map"), new ResourceLocation("paper")
        };

        private static readonly ResourceLocation CARTOGRAPHY_TABLE_FILLED_ITEM_ID = new("filled_map");

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public override bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DoWindowAction(windowId, slotId, action));

            ItemStack? item = null;
            if (inventories.ContainsKey(windowId) && inventories[windowId].Items.ContainsKey(slotId))
                item = inventories[windowId].Items[slotId];

            List<Tuple<short, ItemStack?>> changedSlots = new(); // List<Slot Id, Changed Items>

            // Update our inventory base on action type
            Container inventory = GetInventory(windowId)!;
            Container playerInventory = GetInventory(0)!;
            switch (action)
            {
                case WindowActionType.LeftClick:
                    // Check if cursor have item (slot -1)
                    if (playerInventory.Items.ContainsKey(-1))
                    {
                        // When item on cursor and clicking slot 0, nothing will happen
                        if (slotId == 0) break;

                        // Check target slot also have item?
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            // Check if both item are the same?
                            if (inventory.Items[slotId].ItemType == playerInventory.Items[-1].ItemType)
                            {
                                int maxCount = inventory.Items[slotId].ItemType.StackLimit;
                                // Check item stacking
                                if ((inventory.Items[slotId].Count + playerInventory.Items[-1].Count) <= maxCount)
                                {
                                    // Put cursor item to target
                                    inventory.Items[slotId].Count += playerInventory.Items[-1].Count;
                                    playerInventory.Items.Remove(-1);
                                }
                                else
                                {
                                    // Leave some item on cursor
                                    playerInventory.Items[-1].Count -= (maxCount - inventory.Items[slotId].Count);
                                    inventory.Items[slotId].Count = maxCount;
                                }
                            }
                            else
                            {
                                // Swap two items
                                (inventory.Items[slotId], playerInventory.Items[-1]) = (playerInventory.Items[-1], inventory.Items[slotId]);
                            }
                        }
                        else
                        {
                            // Put cursor item to target
                            inventory.Items[slotId] = playerInventory.Items[-1];
                            playerInventory.Items.Remove(-1);
                        }

                        changedSlots.Add(inventory.Items.TryGetValue(slotId, out var inventoryItem)
                            ? new Tuple<short, ItemStack?>((short)slotId, inventoryItem)
                            : new Tuple<short, ItemStack?>((short)slotId, null));
                    }
                    else
                    {
                        // Check target slot have item?
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            // When taking item from slot 0, server will update us
                            if (slotId == 0) break;

                            // Put target slot item to cursor
                            playerInventory.Items[-1] = inventory.Items[slotId];
                            inventory.Items.Remove(slotId);

                            changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, null));
                        }
                    }
                    break;
                case WindowActionType.RightClick:
                    // Check if cursor have item (slot -1)
                    if (playerInventory.Items.ContainsKey(-1))
                    {
                        // When item on cursor and clicking slot 0, nothing will happen
                        if (slotId == 0) break;

                        // Check target slot have item?
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            // Check if both item are the same?
                            if (inventory.Items[slotId].ItemType == playerInventory.Items[-1].ItemType)
                            {
                                // Check item stacking
                                if (inventory.Items[slotId].Count < inventory.Items[slotId].ItemType.StackLimit)
                                {
                                    // Drop 1 item count from cursor
                                    playerInventory.Items[-1].Count--;
                                    inventory.Items[slotId].Count++;
                                }
                            }
                            else
                            {
                                // Swap two items
                                (inventory.Items[slotId], playerInventory.Items[-1]) = (playerInventory.Items[-1], inventory.Items[slotId]);
                            }
                        }
                        else
                        {
                            // Drop 1 item count from cursor
                            var itemTmp = playerInventory.Items[-1];
                            ItemStack itemClone = new(itemTmp.ItemType, 1, itemTmp.NBT);
                            inventory.Items[slotId] = itemClone;
                            playerInventory.Items[-1].Count--;
                        }
                    }
                    else
                    {
                        // Check target slot have item?
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            if (slotId == 0)
                            {
                                // no matter how many item in slot 0, only 1 will be taken out
                                // Also server will update us
                                break;
                            }
                            if (inventory.Items[slotId].Count == 1)
                            {
                                // Only 1 item count. Put it to cursor
                                playerInventory.Items[-1] = inventory.Items[slotId];
                                inventory.Items.Remove(slotId);
                            }
                            else
                            {
                                // Take half of the item stack to cursor
                                if (inventory.Items[slotId].Count % 2 == 0)
                                {
                                    // Can be evenly divided
                                    var itemTmp = inventory.Items[slotId];
                                    playerInventory.Items[-1] = new ItemStack(itemTmp.ItemType, itemTmp.Count / 2, itemTmp.NBT);
                                    inventory.Items[slotId].Count = itemTmp.Count / 2;
                                }
                                else
                                {
                                    // Cannot be evenly divided. item count on cursor is always larger than item on inventory
                                    var itemTmp = inventory.Items[slotId];
                                    playerInventory.Items[-1] = new ItemStack(itemTmp.ItemType, (itemTmp.Count + 1) / 2, itemTmp.NBT);
                                    inventory.Items[slotId].Count = (itemTmp.Count - 1) / 2;
                                }
                            }
                        }
                    }
                    if (inventory.Items.ContainsKey(slotId))
                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, inventory.Items[slotId]));
                    else
                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, null));
                    break;
                case WindowActionType.ShiftClick:
                case WindowActionType.ShiftRightClick:
                    if (slotId == 0) break;
                    if (item != null)
                    {
                        /* Target slot have item */

                        bool lower2upper = false, upper2backpack = false, backpack2hotbar = false; // mutual exclusion
                        bool hotbarFirst = true; // Used when upper2backpack = true
                        int upperStartSlot = 9;
                        int upperEndSlot = 35;
                        int lowerStartSlot = 36;

                        switch (inventory.Type)
                        {
                            case ContainerType.PlayerInventory:
                                if (slotId >= 0 && slotId <= 8 || slotId == 45)
                                {
                                    if (slotId != 0)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 9;
                                }
                                else if (item != null && false /* Check if wearable */)
                                {
                                    lower2upper = true;
                                    // upperStartSlot = ?;
                                    // upperEndSlot = ?;
                                    // Todo: Distinguish the type of equipment
                                }
                                else
                                {
                                    if (slotId >= 9 && slotId <= 35)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 36;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 9;
                                        upperEndSlot = 35;
                                    }
                                }
                                break;
                            case ContainerType.Generic_9x1:
                                if (slotId >= 0 && slotId <= 8)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 9;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 8;
                                }
                                break;
                            case ContainerType.Generic_9x2:
                                if (slotId >= 0 && slotId <= 17)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 18;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 17;
                                }
                                break;
                            case ContainerType.Generic_9x3:
                            case ContainerType.ShulkerBox:
                                if (slotId >= 0 && slotId <= 26)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 27;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 26;
                                }
                                break;
                            case ContainerType.Generic_9x4:
                                if (slotId >= 0 && slotId <= 35)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 36;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 35;
                                }
                                break;
                            case ContainerType.Generic_9x5:
                                if (slotId >= 0 && slotId <= 44)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 45;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 44;
                                }
                                break;
                            case ContainerType.Generic_9x6:
                                if (slotId >= 0 && slotId <= 53)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 54;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 53;
                                }
                                break;
                            case ContainerType.Generic_3x3:
                                if (slotId >= 0 && slotId <= 8)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 9;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 8;
                                }
                                break;
                            case ContainerType.Anvil:
                                if (slotId >= 0 && slotId <= 2)
                                {
                                    if (slotId >= 0 && slotId <= 1)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 3;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 1;
                                }
                                break;
                            case ContainerType.Beacon:
                                if (slotId == 0)
                                {
                                    hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 1;
                                }
                                else if (item != null && item.Count == 1 && BEACON_FUEL_ITEM_IDS
                                             .Contains(item.ItemType.ItemId) && !inventory.Items.ContainsKey(0))
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 0;
                                }
                                else
                                {
                                    if (slotId >= 1 && slotId <= 27)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 28;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 1;
                                        upperEndSlot = 27;
                                    }
                                }
                                break;
                            case ContainerType.BlastFurnace:
                            case ContainerType.Furnace:
                            case ContainerType.Smoker:
                                if (slotId >= 0 && slotId <= 2)
                                {
                                    if (slotId >= 0 && slotId <= 1)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 3;
                                }
                                else if (item != null && false /* Check if it can be burned */)
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 0;
                                }
                                else
                                {
                                    if (slotId >= 3 && slotId <= 29)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 30;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 3;
                                        upperEndSlot = 29;
                                    }
                                }
                                break;
                            case ContainerType.BrewingStand:
                                if (slotId >= 0 && slotId <= 3)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 5;
                                }
                                else if (item != null && item.ItemType.ItemId == BREWING_STAND_FUEL_ITEM_ID)
                                {
                                    lower2upper = true;
                                    if (!inventory.Items.ContainsKey(4) || inventory.Items[4].Count < 64)
                                        upperStartSlot = upperEndSlot = 4;
                                    else
                                        upperStartSlot = upperEndSlot = 3;
                                }
                                else if (item != null && false /* Check if it can be used for alchemy */)
                                {
                                    lower2upper = true;
                                    upperStartSlot = upperEndSlot = 3;
                                }
                                else if (item != null && BREWING_STAND_BOTTLE_ITEM_IDS.Contains(item.ItemType.ItemId))
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 2;
                                }
                                else
                                {
                                    if (slotId >= 5 && slotId <= 31)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 32;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 5;
                                        upperEndSlot = 31;
                                    }
                                }
                                break;
                            case ContainerType.Crafting:
                                if (slotId >= 0 && slotId <= 9)
                                {
                                    if (slotId >= 1 && slotId <= 9)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 10;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 1;
                                    upperEndSlot = 9;
                                }
                                break;
                            case ContainerType.Enchantment:
                                if (slotId >= 0 && slotId <= 1)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 5;
                                }
                                else if (item != null && item.ItemType.ItemId == ENCHANTING_TABLE_FUEL_ITEM_ID)
                                {
                                    lower2upper = true;
                                    upperStartSlot = upperEndSlot = 1;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 0;
                                }
                                break;
                            case ContainerType.Grindstone:
                                if (slotId >= 0 && slotId <= 2)
                                {
                                    if (slotId >= 0 && slotId <= 1)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 3;
                                }
                                else if (item != null && false /* Check */)
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 1;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 1;
                                }
                                break;
                            case ContainerType.Hopper:
                                if (slotId >= 0 && slotId <= 4)
                                {
                                    upper2backpack = true;
                                    lowerStartSlot = 5;
                                }
                                else
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 4;
                                }
                                break;
                            case ContainerType.Lectern:
                                return false;
                            // break;
                            case ContainerType.Loom:
                                if (slotId >= 0 && slotId <= 3)
                                {
                                    if (slotId >= 0 && slotId <= 5)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 4;
                                }
                                else if (item != null && false /* Check for availability for staining */)
                                {
                                    lower2upper = true;
                                    // upperStartSlot = ?;
                                    // upperEndSlot = ?;
                                }
                                else
                                {
                                    if (slotId >= 4 && slotId <= 30)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 31;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 4;
                                        upperEndSlot = 30;
                                    }
                                }
                                break;
                            case ContainerType.Merchant:
                                if (slotId >= 0 && slotId <= 2)
                                {
                                    if (slotId >= 0 && slotId <= 1)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 3;
                                }
                                else if (item != null && false /* Check if it is available for trading */)
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 1;
                                }
                                else
                                {
                                    if (slotId >= 3 && slotId <= 29)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 30;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 3;
                                        upperEndSlot = 29;
                                    }
                                }
                                break;
                            case ContainerType.Cartography:
                                if (slotId >= 0 && slotId <= 2)
                                {
                                    if (slotId >= 0 && slotId <= 1)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 3;
                                }
                                else if (item != null && item.ItemType.ItemId == CARTOGRAPHY_TABLE_FILLED_ITEM_ID)
                                {
                                    lower2upper = true;
                                    upperStartSlot = upperEndSlot = 0;
                                }
                                else if (item != null && CARTOGRAPHY_TABLE_EMPTY_ITEM_IDS.Contains(item.ItemType.ItemId))
                                {
                                    lower2upper = true;
                                    upperStartSlot = upperEndSlot = 1;
                                }
                                else
                                {
                                    if (slotId >= 3 && slotId <= 29)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 30;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 3;
                                        upperEndSlot = 29;
                                    }
                                }
                                break;
                            case ContainerType.Stonecutter:
                                if (slotId >= 0 && slotId <= 1)
                                {
                                    if (slotId == 0)
                                        hotbarFirst = false;
                                    upper2backpack = true;
                                    lowerStartSlot = 2;
                                }
                                else if (item != null && false /* Check if it is available for stone cutteing */)
                                {
                                    lower2upper = true;
                                    upperStartSlot = 0;
                                    upperEndSlot = 0;
                                }
                                else
                                {
                                    if (slotId >= 2 && slotId <= 28)
                                    {
                                        backpack2hotbar = true;
                                        lowerStartSlot = 29;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 2;
                                        upperEndSlot = 28;
                                    }
                                }
                                break;
                            // TODO: Define more container type here
                            default:
                                return false;
                        }

                        // Cursor have item or not doesn't matter
                        // If hotbar already have same item, will put on it first until every stack are full
                        // If no more same item , will put on the first empty slot (smaller slot id)
                        // If inventory full, item will not move
                        int itemCount = inventory.Items[slotId].Count;
                        if (lower2upper)
                        {
                            int firstEmptySlot = -1;
                            for (int i = upperStartSlot; i <= upperEndSlot; ++i)
                            {
                                if (inventory.Items.TryGetValue(i, out ItemStack? curItem))
                                {
                                    if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                        break;
                                }
                                else if (firstEmptySlot == -1)
                                    firstEmptySlot = i;
                            }
                            if (item!.Count > 0)
                            {
                                if (firstEmptySlot != -1)
                                    StoreInNewSlot(inventory, item, slotId, firstEmptySlot, changedSlots);
                                else if (item.Count != itemCount)
                                    changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, inventory.Items[slotId]));
                            }
                        }
                        else if (upper2backpack)
                        {
                            int hotbarEnd = lowerStartSlot + 4 * 9 - 1;
                            if (hotbarFirst)
                            {
                                int lastEmptySlot = -1;
                                for (int i = hotbarEnd; i >= lowerStartSlot; --i)
                                {
                                    if (inventory.Items.TryGetValue(i, out ItemStack? curItem))
                                    {
                                        if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                            break;
                                    }
                                    else if (lastEmptySlot == -1)
                                        lastEmptySlot = i;
                                }
                                if (item!.Count > 0)
                                {
                                    if (lastEmptySlot != -1)
                                        StoreInNewSlot(inventory, item, slotId, lastEmptySlot, changedSlots);
                                    else if (item.Count != itemCount)
                                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, inventory.Items[slotId]));
                                }
                            }
                            else
                            {
                                int firstEmptySlot = -1;
                                for (int i = lowerStartSlot; i <= hotbarEnd; ++i)
                                {
                                    if (inventory.Items.TryGetValue(i, out ItemStack? curItem))
                                    {
                                        if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                            break;
                                    }
                                    else if (firstEmptySlot == -1)
                                        firstEmptySlot = i;
                                }
                                if (item!.Count > 0)
                                {
                                    if (firstEmptySlot != -1)
                                        StoreInNewSlot(inventory, item, slotId, firstEmptySlot, changedSlots);
                                    else if (item.Count != itemCount)
                                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, inventory.Items[slotId]));
                                }
                            }
                        }
                        else if (backpack2hotbar)
                        {
                            int hotbarEnd = lowerStartSlot + 1 * 9 - 1;

                            int firstEmptySlot = -1;
                            for (int i = lowerStartSlot; i <= hotbarEnd; ++i)
                            {
                                if (inventory.Items.TryGetValue(i, out ItemStack? curItem))
                                {
                                    if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                        break;
                                }
                                else if (firstEmptySlot == -1)
                                    firstEmptySlot = i;
                            }
                            if (item!.Count > 0)
                            {
                                if (firstEmptySlot != -1)
                                    StoreInNewSlot(inventory, item, slotId, firstEmptySlot, changedSlots);
                                else if (item.Count != itemCount)
                                    changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, inventory.Items[slotId]));
                            }
                        }
                    }
                    break;
                case WindowActionType.DropItem:
                    if (inventory.Items.ContainsKey(slotId))
                    {
                        inventory.Items[slotId].Count--;
                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, inventory.Items[slotId]));
                    }

                    if (inventory.Items[slotId].Count <= 0)
                    {
                        inventory.Items.Remove(slotId);
                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, null));
                    }

                    break;
                case WindowActionType.DropItemStack:
                    inventory.Items.Remove(slotId);
                    changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, null));
                    break;
            }

            return handler!.SendWindowAction(windowId, slotId, action, item, changedSlots, inventories[windowId].StateId);
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
        public bool DoCreativeGive(int slot, Item itemType, int count, Dictionary<string, object>? nbt = null)
        {
            return InvokeOnNetMainThread(() => handler!.SendCreativeInventoryAction(slot, itemType, count, nbt));
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public bool DoAnimation(int animation)
        {
            return InvokeOnNetMainThread(() => handler!.SendAnimation(animation, clientEntity.Id));
        }

        /// <summary>
        /// Close the specified inventory window
        /// </summary>
        /// <param name="windowId">Window Id</param>
        /// <returns>TRUE if the window was successfully closed</returns>
        /// <remarks>Sending close window for inventory 0 can cause server to update our inventory if there are any item in the crafting area</remarks>
        public bool CloseInventory(int windowId)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => CloseInventory(windowId));

            if (inventories.ContainsKey(windowId))
            {
                if (windowId != 0)
                    inventories.Remove(windowId);
                return handler!.SendCloseWindow(windowId);
            }
            return false;
        }

        /// <summary>
        /// Clean all inventory
        /// </summary>
        /// <returns>TRUE if the successfully cleared</returns>
        public bool ClearInventories()
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread<bool>(ClearInventories);

            inventories.Clear();
            inventories[0] = new Container(0, ContainerType.PlayerInventory, "Player Inventory");
            return true;
        }

        /// <summary>
        /// Interact with an entity
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="type">0: interact, 1: attack, 2: interact at</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if interaction succeeded</returns>
        public bool InteractEntity(int entityId, int type, Hand hand = Hand.MainHand)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => InteractEntity(entityId, type, hand));

            if (EntityRenderManager.HasEntityRender(entityId))
            {
                if (type == 0)
                    return handler!.SendInteractEntity(entityId, type, (int)hand);
                else
                    return handler!.SendInteractEntity(entityId, type);
            }
            else return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="blockLoc">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <returns>TRUE if successfully placed</returns>
        public override bool PlaceBlock(BlockLoc blockLoc, Direction blockFace, Hand hand = Hand.MainHand)
        {
            return InvokeOnNetMainThread(() => handler!.SendPlayerBlockPlacement((int)hand, blockLoc, blockFace, sequenceId++));
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="blockLoc">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        public override bool DigBlock(BlockLoc blockLoc, bool swingArms = true, bool lookAtBlock = true)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DigBlock(blockLoc, swingArms, lookAtBlock));

            // TODO select best face from current player location
            Direction blockFace = Direction.Down;

            // Look at block before attempting to break it
            if (lookAtBlock)
            {
                UpdateLocation(GetCurrentLocation(), new Location(blockLoc.X, blockLoc.Y, blockLoc.Z));
            }

            // Send dig start and dig end, will need to wait for server response to know dig result
            // See https://wiki.vg/How_to_Write_a_Client#Digging for more details
            return handler!.SendPlayerDigging(0, blockLoc, blockFace, sequenceId++)
                && (!swingArms || DoAnimation((int)Hand.MainHand))
                && handler.SendPlayerDigging(2, blockLoc, blockFace, sequenceId++);
        }

        /// <summary>
        /// Drop item in active hotbar slot
        /// </summary>
        /// <param name="dropEntireStack">Whether or not to drop the entire item stack</param>
        public override bool DropItem(bool dropEntireStack)
        {
            return handler!.SendPlayerAction(dropEntireStack ? 3 : 4);
        }

        /// <summary>
        /// Swap item stacks in main hand and off hand
        /// </summary>
        public override bool SwapItemOnHands()
        {
            return handler!.SendPlayerAction(6);
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

            if (InvokeRequired)
                return InvokeOnNetMainThread(() => ChangeSlot(slot));
            
            // There won't be confirmation from the server
            // Simply set it on the client side
            CurrentSlot = Convert.ToByte(slot);
            var curItem = inventories[0].GetHotbarItem(slot);
            // Broad cast hotbar selection change
            EventManager.Instance.BroadcastOnUnityThread(
                    new HeldItemChangeEvent(CurrentSlot, curItem, PlayerActionHelper.GetItemActionType(curItem)));

            return handler!.SendHeldItemChange(slot);
        }

        /// <summary>
        /// Update sign text
        /// </summary>
        /// <param name="location">sign location</param>
        /// <param name="line1">text one</param>
        /// <param name="line2">text two</param>
        /// <param name="line3">text three</param>
        /// <param name="line4">text1 four</param>
        public bool UpdateSign(Location location, string line1, string line2, string line3, string line4)
        {
            // TODO Open sign editor first https://wiki.vg/Protocol#Open_Sign_Editor
            return InvokeOnNetMainThread(() => handler!.SendUpdateSign(location, line1, line2, line3, line4));
        }

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0.</param>
        public bool SelectTrade(int selectedSlot)
        {
            return InvokeOnNetMainThread(() => handler!.SelectTrade(selectedSlot));
        }

        /// <summary>
        /// Teleport to player in spectator mode
        /// </summary>
        /// <param name="entity">Player to teleport to</param>
        /// Teleporting to other entityies is NOT implemented yet
        public bool Spectate(Entity entity)
        {
            if (entity.Type.TypeId == EntityType.PLAYER_ID)
                return SpectateByUUID(entity.UUID);
            return false;
        }

        /// <summary>
        /// Teleport to player/entity in spectator mode
        /// </summary>
        /// <param name="UUID">UUID of player/entity to teleport to</param>
        public bool SpectateByUUID(Guid UUID)
        {
            if(GameMode == GameMode.Spectator)
            {
                if(InvokeRequired)
                    return InvokeOnNetMainThread(() => SpectateByUUID(UUID));
                return handler!.SendSpectate(UUID);
            }
            return false;
        }
        #endregion

        #region Event handlers: An event occurs on the Server
        /// <summary>
        /// Called when a network packet received or sent
        /// </summary>
        /// <remarks>
        /// Only called if <see cref="ProtocolSettings.CapturePackets"/> is set to True
        /// </remarks>
        /// <param name="packetId">Packet Id</param>
        /// <param name="packetData">A copy of Packet Data</param>
        /// <param name="currentState">The current game state in which the packet is handled</param>
        /// <param name="isInbound">The packet is received from server or sent by client</param>
        public void OnNetworkPacket(int packetId, byte[] packetData, CurrentState currentState, bool isInbound)
        {
            if (currentState == CurrentState.Play)
            {
                // Regular in-game packet
                EventManager.Instance.BroadcastOnUnityThread(new InGamePacketEvent(isInbound, packetId, packetData));
            }
        }

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
        public void OnGameJoined(bool isOnlineMode)
        {
            if (protocolVersion < ProtocolMinecraft.MC_1_19_3_Version || playerKeyPair == null || !isOnlineMode)
                SetCanSendMessage(true);
            else
                SetCanSendMessage(false);

            handler!.SendBrandInfo(ProtocolSettings.BrandInfo.Trim());

            if (ProtocolSettings.MCSettings.Enabled)
                handler.SendClientSettings(
                    ProtocolSettings.MCSettings.Locale,
                    ProtocolSettings.MCSettings.RenderDistance,
                    ProtocolSettings.MCSettings.Difficulty,
                    ProtocolSettings.MCSettings.ChatMode,
                    ProtocolSettings.MCSettings.ChatColors,
                    ProtocolSettings.MCSettings.Skin_All,
                    ProtocolSettings.MCSettings.MainHand);

            if (protocolVersion >= ProtocolMinecraft.MC_1_19_3_Version
                && playerKeyPair != null && isOnlineMode)
                handler.SendPlayerSession(playerKeyPair);

            ClearInventories();
        }

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        public void OnRespawn()
        {
            // Reset location received flag
            locationReceived = false;

            ClearTasks();

            ChunkRenderManager.ClearChunksData();

            //if (!keepAttr)
            {
                ClearInventories();
            }

            Loom.QueueOnMainThread(() => {
                ScreenControl.SetLoadingScreen(true);
                PlayerController.DisablePhysics();

                ChunkRenderManager.ReloadChunksRender();
                EntityRenderManager.ReloadEntityRenders();
            });
        }

        /// <summary>
        /// Called when the server sends a new player location.
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="lookAtLocation">Block coordinates to look at</param>
        public void UpdateLocation(Location location, Location lookAtLocation)
        {
            double dx = lookAtLocation.X - (location.X - 0.5);
            double dy = lookAtLocation.Y - (location.Y + 1);
            double dz = lookAtLocation.Z - (location.Z - 0.5);

            double r = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            float yaw = Convert.ToSingle(-Math.Atan2(dx, dz) / Math.PI * 180);
            float pitch = Convert.ToSingle(-Math.Asin(dy / r) / Math.PI * 180);
            if (yaw < 0) yaw += 360;

            UpdateLocation(location, yaw, pitch);
        }

        /// <summary>
        /// Called when the server sends a new player location.
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="yaw">Yaw to look at</param>
        /// <param name="pitch">Pitch to look at</param>
        public void UpdateLocation(Location location, float yaw, float pitch)
        {
            lock (movementLock)
            {
                if (!locationReceived) // On entering world or respawning
                {
                    locationReceived = true;

                    Loom.QueueOnMainThread(() => {
                        // Update player location
                        PlayerController.SetLocationFromServer(location, mcYaw: yaw);
                        // Force refresh environment collider
                        ChunkRenderManager.InitializeTerrainCollider(location.GetBlockLoc(), () =>
                                {
                                    // Pop loading screen
                                    ScreenControl.SetLoadingScreen(false);
                                    PlayerController.EnablePhysics();
                                });
                        // Update camera yaw (convert to Unity yaw)
                        CameraController.SetYaw(yaw + 90F);
                    });
                }
                else // Position correction from server
                {                    
                    Loom.QueueOnMainThread(() => {
                        var delta = PlayerController.transform.position - CoordConvert.MC2Unity(WorldOriginOffset, location);
                        if (delta.magnitude < 8F)
                        {
                            return; // I don't like this packet.
                        }

                        // Force refresh environment collider
                        ChunkRenderManager.RebuildTerrainCollider(location.GetBlockLoc());
                        // Then update player location
                        PlayerController.SetLocationFromServer(location, reset: true, mcYaw: yaw);
                        //Debug.Log($"Updated to {location} offset: {offset.magnitude}");
                    });
                }
            }
        }

        /// <summary>
        /// Received chat/system message from the server
        /// </summary>
        /// <param name="message">Message received</param>
        public void OnTextReceived(ChatMessage message)
        {
            UpdateKeepAlive();

            List<string> links = new();
            string messageText;

            if (message.isSignedChat)
            {
                if (!ProtocolSettings.ShowIllegalSignedChat && !message.isSystemChat && !(bool)message.isSignatureLegal!)
                    return;
                messageText = ChatParser.ParseSignedChat(message, links);
            }
            else
            {
                if (message.isJson)
                    messageText = ChatParser.ParseText(message.content, links);
                else
                    messageText = message.content;
            }

            EventManager.Instance.BroadcastOnUnityThread<ChatMessageEvent>(new(messageText));
        }

        /// <summary>
        /// Received a connection keep-alive from the server
        /// </summary>
        public void OnServerKeepAlive()
        {
            UpdateKeepAlive();
        }

        /// <summary>
        /// Called tab complete suggestion is received
        /// </summary>
        public void OnTabCompleteDone(int completionStart, int completionLength, string[] completeResults)
        {
            if (completeResults.Length > 0)
            {
                EventManager.Instance.BroadcastOnUnityThread<AutoCompletionEvent>(
                        new(completionStart, completionLength, completeResults));
            }
            else
            {
                EventManager.Instance.BroadcastOnUnityThread(AutoCompletionEvent.EMPTY);
            }
        }

        /// <summary>
        /// When an inventory is opened
        /// </summary>
        /// <param name="inventory">The inventory</param>
        /// <param name="inventoryId">Inventory Id</param>
        public void OnInventoryOpen(int inventoryId, Container inventory)
        {
            inventories[inventoryId] = inventory;

            if (inventoryId != 0)
            {
                Debug.Log(Translations.Get("extra.inventory_open", inventoryId, inventory.Title));
                Debug.Log(Translations.Get("extra.inventory_interact"));
            }
        }

        /// <summary>
        /// When an inventory is close
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        public void OnInventoryClose(int inventoryId)
        {
            if (inventories.ContainsKey(inventoryId))
            {
                if (inventoryId == 0)
                    inventories[0].Items.Clear(); // Don't delete player inventory
                else
                    inventories.Remove(inventoryId);
            }

            if (inventoryId != 0)
            {
                Debug.Log(Translations.Get("extra.inventory_close", inventoryId));
            }
        }

        /// <summary>
        /// When received window items from server.
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="itemList">Item list, key = slot Id, value = Item information</param>
        public void OnWindowItems(byte inventoryId, Dictionary<int, ItemStack> itemList, int stateId)
        {
            if (inventories.ContainsKey(inventoryId))
            {
                var container = inventories[inventoryId];

                container.Items = itemList;
                container.StateId = stateId;
                
                Loom.QueueOnMainThread(() => {
                    foreach (var pair in itemList)
                    {
                        EventManager.Instance.Broadcast(new SlotUpdateEvent(inventoryId, pair.Key, pair.Value));

                        if (container.IsHotbar(pair.Key, out int hotbarSlot))
                        {
                            EventManager.Instance.Broadcast(new HotbarUpdateEvent(hotbarSlot, pair.Value));
                        }
                    }
                });
            }
        }

        /// <summary>
        /// When a slot is set inside window items
        /// </summary>
        /// <param name="inventoryId">Window Id</param>
        /// <param name="slotId">Slot Id</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        public void OnSetSlot(byte inventoryId, short slotId, ItemStack item, int stateId)
        {
            if (inventories.ContainsKey(inventoryId))
                inventories[inventoryId].StateId = stateId;

            // Handle inventoryId -2 - Add item to player inventory without animation
            if (inventoryId == 254)
                inventoryId = 0;
            // Handle cursor item
            if (inventoryId == 255 && slotId == -1)
            {
                //inventoryId = 0; // Prevent key not found for some bots relied to this event
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
                if (inventories.ContainsKey(inventoryId))
                {
                    var container = inventories[inventoryId];
                    
                    if (item == null || item.IsEmpty)
                    {
                        if (container.Items.ContainsKey(slotId))
                            container.Items.Remove(slotId);
                    }
                    else container.Items[slotId] = item;

                    Loom.QueueOnMainThread(() => {
                        EventManager.Instance.Broadcast(new SlotUpdateEvent(inventoryId, slotId, item));

                        if (container.IsHotbar(slotId, out int hotbarSlot))
                        {
                            EventManager.Instance.Broadcast(new HotbarUpdateEvent(hotbarSlot, item));
                            if (hotbarSlot == CurrentSlot) // Updating held item
                            {
                                EventManager.Instance.Broadcast(new HeldItemChangeEvent(
                                        CurrentSlot, item, PlayerActionHelper.GetItemActionType(item)));
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Set client player's Id for later receiving player's own properties
        /// </summary>
        /// <param name="entityId">Player Entity Id</param>
        public void OnReceivePlayerEntityId(int entityId)
        {
            clientEntity.Id = entityId;
            //Debug.Log($"Client entity id received: {entityId}");
        }

        /// <summary>
        /// Triggered when a new player joins the game
        /// </summary>
        /// <param name="player">player info</param>
        public void OnPlayerJoin(PlayerInfo player)
        {
            //Ignore placeholders eg 0000tab# from TabListPlus
            if (!PlayerInfo.IsValidName(player.Name))
                return;

            if (player.Name == username)
            {
                // 1.19+ offline server is possible to return different uuid
                this.uuid = player.Uuid;
                // Also update client entity uuid
                clientEntity.UUID = uuid;
            }

            lock (onlinePlayers)
            {
                onlinePlayers[player.Uuid] = player;
            }
        }

        /// <summary>
        /// Triggered when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        public void OnPlayerLeave(Guid uuid)
        {
            string? username = null;

            lock (onlinePlayers)
            {
                if (onlinePlayers.ContainsKey(uuid))
                {
                    username = onlinePlayers[uuid].Name;
                    onlinePlayers.Remove(uuid);
                }
            }
        }

        /// <summary>
        /// Called when a player has been killed by another entity
        /// </summary>
        /// <param name="killerEntityId">Killer's entity id</param>
        /// <param name="chatMessage">message sent in chat when player is killed</param>
        public void OnPlayerKilled(int killerEntityId, string chatMessage)
        {

        }

        /// <summary>
        /// Called when an entity spawned
        /// </summary>
        public void OnSpawnEntity(Entity entity)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.AddEntityRender(entity);
            });
        }

        /// <summary>
        /// Called when the Entity use effects
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="effect">Effect Id</param>
        /// <param name="amplifier">Effect amplifier</param>
        /// <param name="duration">Effect duration</param>
        /// <param name="flags">Effect flags</param>
        /// <param name="hasFactorData">Has factor data</param>
        /// <param name="factorCodec">FactorCodec</param>
        public void OnEntityEffect(int entityId, Effects effect, int amplifier, int duration, byte flags, bool hasFactorData, Dictionary<string, object>? factorCodec) { }

        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        public void OnSpawnPlayer(int entityId, Guid uuid, Location location, byte Yaw, byte Pitch)
        {
            string? playerName = null;
            if (onlinePlayers.ContainsKey(uuid))
                playerName = onlinePlayers[uuid].Name;
            Entity playerEntity = new(entityId, EntityTypePalette.INSTANCE.GetById(EntityType.PLAYER_ID), location, uuid, playerName);
            OnSpawnEntity(playerEntity);
        }

        /// <summary>
        /// Called on Entity Equipment
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="slot">Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item">Item</param>
        public void OnEntityEquipment(int entityId, int slot, ItemStack item)
        {
            // TODO
        }

        /// <summary>
        /// Called when the Game Mode has been updated for a player
        /// </summary>
        /// <param name="uuid">Player UUID (Empty for initial gamemode on login)</param>
        /// <param name="gamemode">New Game Mode (0: Survival, 1: Creative, 2: Adventure, 3: Spectator).</param>
        public void OnGamemodeUpdate(Guid uuid, int gamemode)
        {
            
            if (uuid == Guid.Empty) // Initial gamemode on login
            {
                Loom.QueueOnMainThread(() =>
                {
                    GameMode = (GameMode) gamemode;
                    EventManager.Instance.Broadcast<GameModeUpdateEvent>(new(GameMode));
                });
            }
            else if (onlinePlayers.ContainsKey(uuid)) // Further regular gamemode change events
            {
                string playerName = onlinePlayers[uuid].Name;
                if (playerName == this.username)
                {
                    Loom.QueueOnMainThread(() =>
                    {
                        GameMode = (GameMode) gamemode;
                        EventManager.Instance.Broadcast<GameModeUpdateEvent>(new(GameMode));

                        CornApp.Notify(Translations.Get("gameplay.control.update_gamemode",
                                ChatParser.TranslateString($"gameMode.{GameMode.GetIdentifier()}")), Notification.Type.Success);
                    });
                }
            }
        }

        /// <summary>
        /// Called when entities dead/despawn.
        /// </summary>
        public void OnDestroyEntities(int[] entities)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.RemoveEntityRenders(entities);
            });
        }

        /// <summary>
        /// Called when an entity's position changed within 8 block of its previous position.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="dx">Delta X</param>
        /// <param name="dy">Delta Y</param>
        /// <param name="dz">Delta Z</param>
        /// <param name="onGround">Whether the entity is grounded</param>
        public void OnEntityPosition(int entityId, double dx, double dy, double dz, bool onGround)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.MoveEntityRender(entityId, new(dx, dy, dz));
            });
        }

        /// <summary>
        /// Called when an entity's yaw/pitch changed.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="yaw">New yaw</param>
        /// <param name="pitch">New pitch</param>
        /// <param name="onGround">Whether the entity is grounded</param>
        public void OnEntityRotation(int entityId, byte yaw, byte pitch, bool onGround)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.RotateEntityRender(entityId, yaw, pitch);
            });
        }

        /// <summary>
        /// Called when an entity's head yaw changed.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="headYaw">New head yaw</param>
        public void OnEntityHeadLook(int entityId, byte headYaw)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.RotateEntityRenderHead(entityId, headYaw);
            });
        }

        public void OnEntityTeleport(int entityId, double X, double Y, double Z, bool onGround)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.TeleportEntityRender(entityId, new(X, Y, Z));
            });
        }

        /// <summary>
        /// Called when received entity properties from server.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="prop"></param>
        public void OnEntityProperties(int entityId, Dictionary<string, double> prop) { }

        /// <summary>
        /// Called when the status of an entity have been changed
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="status">Status Id</param>
        public void OnEntityStatus(int entityId, byte status) { }

        /// <summary>
        /// Called when server sent a Time Update packet.
        /// </summary>
        /// <param name="worldAge"></param>
        /// <param name="timeOfDay"></param>
        public void OnTimeUpdate(long worldAge, long timeOfDay)
        {
            // TimeUpdate sent every server tick hence used as timeout detect
            UpdateKeepAlive();

            Loom.QueueOnMainThread(() => {
                EnvironmentManager.SetTime(timeOfDay);
            });

            // calculate server tps
            if (lastAge != 0)
            {
                DateTime currentTime = DateTime.Now;
                long tickDiff = worldAge - lastAge;
                double tps = tickDiff / (currentTime - lastTime).TotalSeconds;
                lastAge = worldAge;
                lastTime = currentTime;
                if (tps <= 20 && tps > 0)
                {
                    // calculate average tps
                    if (tpsSamples.Count >= maxSamples)
                    {
                        // full
                        sampleSum -= tpsSamples[0];
                        tpsSamples.RemoveAt(0);
                    }
                    tpsSamples.Add(tps);
                    sampleSum += tps;
                    averageTPS = sampleSum / tpsSamples.Count;
                    serverTPS = tps;
                }
            }
            else
            {
                lastAge = worldAge;
                lastTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Called when client player's health changed, e.g. getting attack
        /// </summary>
        /// <param name="health">Player current health</param>
        public void OnUpdateHealth(float health, int food)
        {
            bool updateMaxHealth = clientEntity.MaxHealth < health;

            if (updateMaxHealth)
                clientEntity.MaxHealth = health;
            
            clientEntity.Health = health;
            foodSaturation = food;

            EventManager.Instance.BroadcastOnUnityThread
                    <HealthUpdateEvent>(new(health, updateMaxHealth));
        }

        /// <summary>
        /// Called when experience updates
        /// </summary>
        /// <param name="expBar">Between 0 and 1</param>
        /// <param name="level">Level</param>
        /// <param name="totalExp">Total Experience</param>
        public void OnSetExperience(float expBar, int level, int totalExp)
        {
            this.level = level;
            totalExperience = totalExp;
        }

        /// <summary>
        /// Called when an explosion occurs on the server
        /// </summary>
        /// <param name="location">Explosion location</param>
        /// <param name="strength">Explosion strength</param>
        /// <param name="affectedBlocks">Amount of affected blocks</param>
        public void OnExplosion(Location location, float strength, int affectedBlocks) { }

        /// <summary>
        /// Called when Latency is updated
        /// </summary>
        /// <param name="uuid">player uuid</param>
        /// <param name="latency">Latency</param>
        public void OnLatencyUpdate(Guid uuid, int latency)
        {
            if (onlinePlayers.ContainsKey(uuid))
            {
                var player = onlinePlayers[uuid];
                player.Ping = latency;
            }
        }

        /// <summary>
        /// Called when held item change
        /// </summary>
        /// <param name="slot"> item slot</param>
        public void OnHeldItemChange(byte slot) //
        {
            CurrentSlot = slot;
            var newItem = inventories[0].GetHotbarItem(slot);
            // Broad cast hotbar selection change
            EventManager.Instance.BroadcastOnUnityThread(
                    new HeldItemChangeEvent(CurrentSlot, newItem,
                    PlayerActionHelper.GetItemActionType(newItem)));
        }

        /// <summary>
        /// Called when an update of the map is sent by the server, take a look at https://wiki.vg/Protocol#Map_Data for more info on the fields
        /// Map format and colors: https://minecraft.fandom.com/wiki/Map_item_format
        /// </summary>
        /// <param name="mapId">Map Id of the map being modified</param>
        /// <param name="scale">A scale of the Map, from 0 for a fully zoomed-in map (1 block per pixel) to 4 for a fully zoomed-out map (16 blocks per pixel)</param>
        /// <param name="trackingPosition">Specifies whether player and item frame icons are shown </param>
        /// <param name="locked">True if the map has been locked in a cartography table </param>
        /// <param name="icons">A list of MapIcon objects of map icons, send only if trackingPosition is true</param>
        /// <param name="colsUpdated">Numbs of columns that were updated (map width) (NOTE: If it is 0, the next fields are not used/are set to default values of 0 and null respectively)</param>
        /// <param name="rowsUpdated">Map height</param>
        /// <param name="mapColX">x offset of the westernmost column</param>
        /// <param name="mapRowZ">z offset of the northernmost row</param>
        /// <param name="colors">a byte array of colors on the map</param>
        public void OnMapData(int mapId, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons, byte colsUpdated, byte rowsUpdated, byte mapColX, byte mapRowZ, byte[]? colors) { }

        /// <summary>
        /// Received some Title from the server
        /// <param name="action"> 0 = set title, 1 = set subtitle, 3 = set action bar, 4 = set times and display, 4 = hide, 5 = reset</param>
        /// <param name="titleText"> title text</param>
        /// <param name="subtitleText"> suntitle text</param>
        /// <param name="actionbarText"> action bar text</param>
        /// <param name="fadeIn"> Fade In</param>
        /// <param name="stay"> Stay</param>
        /// <param name="fadeOut"> Fade Out</param>
        /// <param name="json"> json text</param>
        public void OnTitle(int action, string titleText, string subtitleText, string actionbarText, int fadeIn, int stay, int fadeOut, string json) { }

        /// <summary>
        /// Called when coreboardObjective
        /// </summary>
        /// <param name="objectivename">objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectivevalue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        public void OnScoreboardObjective(string objectivename, byte mode, string objectivevalue, int type)
        {
            string json = objectivevalue;
            objectivevalue = ChatParser.ParseText(objectivevalue);
        }

        /// <summary>
        /// Called when DisplayScoreboard
        /// </summary>
        /// <param name="entityName">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectiveName">The name of the objective the score belongs to</param>
        /// <param name="objectiveDisplayName">The name of the objective the score belongs to, but with chat formatting</param>
        /// <param name="objectiveValue">The score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        /// <param name="numberFormat">Number format: 0 - blank, 1 - styled, 2 - fixed</param>
        public void OnUpdateScore(string entityName, int action, string objectiveName, string objectiveDisplayName, int objectiveValue, int numberFormat)
        {

        }

        /// <summary>
        /// Called when the health of an entity changed
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="health">The health of the entity</param>
        public void OnEntityHealth(int entityId, float health)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.UpdateEntityHealth(entityId, health);
            });
        }

        /// <summary>
        /// Called when the metadata of an entity changed
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="metadata">The metadata of the entity</param>
        public void OnEntityMetadata(int entityId, Dictionary<int, object?> metadata)
        {
            Loom.QueueOnMainThread(() => {
                var entity = EntityRenderManager.GetEntityRender(entityId);

                if (entity != null)
                {
                    if (entity.Type!.ContainsItem && metadata.TryGetValue(7, out object? itemObj) && itemObj != null && itemObj.GetType() == typeof(ItemStack))
                    {
                        var item = (ItemStack?) itemObj;
                        entity.Item.Value = item;
                    }
                    if (metadata.TryGetValue(6, out object? poseObj) && poseObj != null && poseObj.GetType() == typeof(int))
                    {
                        entity.Pose = (EntityPose)poseObj;
                    }
                    if (metadata.TryGetValue(2, out object? nameObj) && nameObj != null && nameObj.GetType() == typeof(string))
                    {
                        string name = nameObj.ToString()!;
                        entity.CustomNameJson = name;
                        entity.CustomName = ChatParser.ParseText(name);
                    }
                    if (metadata.TryGetValue(3, out object? nameVisableObj) && nameVisableObj != null && nameVisableObj.GetType() == typeof(bool))
                    {
                        entity.IsCustomNameVisible = bool.Parse(nameVisableObj.ToString()!);
                    }

                    entity.UpdateMetadata(metadata);
                }
            });
        }

        /// <summary>
        /// Called when tradeList is recieved after interacting with villager
        /// </summary>
        /// <param name="windowId">Window Id</param>
        /// <param name="trades">List of trades.</param>
        /// <param name="villagerInfo">Contains Level, Experience, IsRegularVillager and CanRestock .</param>
        public void OnTradeList(int windowId, List<VillagerTrade> trades, VillagerInfo villagerInfo) { }

        /// <summary>
        /// Called every player break block in gamemode 0
        /// </summary>
        /// <param name="entityId">Player Id</param>
        /// <param name="blockLoc">Block location</param>
        /// <param name="stage">Destroy stage, maximum 255</param>
        public void OnBlockBreakAnimation(int entityId, BlockLoc blockLoc, byte stage)
        {
            // TODO
        }

        /// <summary>
        /// Called every animations of the hit and place block
        /// </summary>
        /// <param name="entityId">Player Id</param>
        /// <param name="animation">0 = LMB, 1 = RMB (RMB Corrent not work)</param>
        public void OnEntityAnimation(int entityId, byte animation)
        {
            // TODO
        }

        /// <summary>
        /// Called every time rain(snow) starts or stops
        /// </summary>
        /// <param name="begin">true if the rain is starting</param>
        public void OnRainChange(bool begin)
        {
            Loom.QueueOnMainThread(() => {
                EnvironmentManager.SetRain(begin);
            });
        }

        /// <summary>
        /// Called when a Synchronization sequence is recevied, this sequence need to be sent when breaking or placing blocks
        /// </summary>
        /// <param name="sequenceId">Sequence Id</param>
        public void OnBlockChangeAck(int sequenceId)
        {
            this.sequenceId = sequenceId;
        }

        /// <summary>
        /// Called when the protocol handler receives server data
        /// </summary>
        /// <param name="hasMotd">Indicates if the server has a motd message</param>
        /// <param name="motd">Server MOTD message</param>
        /// <param name="hasIcon">Indicates if the server has a an icon</param>
        /// <param name="iconBase64">Server icon in Base 64 format</param>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        public void OnServerDataReceived(bool hasMotd, string motd, bool hasIcon, string iconBase64, bool previewsChat) { }

        /// <summary>
        /// Called when the protocol handler receives "Set Display Chat Preview" packet
        /// </summary>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        public void OnChatPreviewSettingUpdate(bool previewsChat) { }

        /// <summary>
        /// Called when the protocol handler receives "Login Success" packet
        /// </summary>
        /// <param name="uuid">The player's UUID received from the server</param>
        /// <param name="userName">The player's DUMMY_USERNAME received from the server</param>
        /// <param name="playerProperty">Tuple<Name, Value, Signature(empty if there is no signature)></param>
        public void OnLoginSuccess(Guid uuid, string userName, Tuple<string, string, string>[]? playerProperty) { }

        #endregion
    }
}
