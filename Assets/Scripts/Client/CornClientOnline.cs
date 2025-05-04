using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Linq;
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
using CraftSharp.Protocol.Session;

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
        private string accountLower = string.Empty;
        private PlayerKeyPair? playerKeyPair;

        // Cookies
        private Dictionary<string, byte[]> Cookies { get; } = new();
        public void GetCookie(string key, out byte[]? data) => Cookies.TryGetValue(key, out data);
        public void SetCookie(string key, byte[] data) => Cookies[key] = data;
        public void DeleteCookie(string key) => Cookies.Remove(key, out _);
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

        private TcpClient? client;
        private IMinecraftCom? handler;
        private SessionToken? _sessionToken;
        private Tuple<Thread, CancellationTokenSource>? timeoutDetector = null;
        #endregion

        #nullable disable

        #region Players and Entities
        private bool locationReceived = false;
        private readonly EntityData clientEntity = new(0, EntityType.DUMMY_ENTITY_TYPE, Location.Zero);
        private int sequenceId; // User for player block synchronization (Aka. digging, placing blocks, etc..)
        private int foodSaturation, experienceLevel, totalExperience;
        private int activeInventoryId = -1;
        private readonly Dictionary<int, InventoryData> inventories = new();
        
        private ItemStack dragStartCursorItemClone; // Keep a copy of cursor item before drag start
        private readonly Dictionary<int, int> draggedSlots = new(); // And keep track of the initial count of each dragged slot
        private bool dragging = false;
        
        public override bool CheckAddDragged(ItemStack slotItem, Func<ItemStack, bool> slotPredicate)
        {
            if (!dragging || dragStartCursorItemClone is null) return false;

            // If the count of original cursor stack is greater than the count of dragged slots,
            // then we can still add more dragged slots, making sure each can get at least one item.
            if (dragStartCursorItemClone.Count <= draggedSlots.Count) return false;
            
            // If this slot doesn't accept the dragging item, don't add this slot
            if (!slotPredicate(dragStartCursorItemClone)) return false;
            
            if (slotItem is null) return true;
            
            return InventoryData.CheckStackable(slotItem, dragStartCursorItemClone);
        }

        private readonly object movementLock = new();
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();
        #endregion

        private void Awake() // In case where the client wasn't properly assigned before
        {
            if (CornApp.CurrentClient == null)
            {
                CornApp.SetCurrentClient(this);
            }
        }

        private void Start()
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

            sessionId = session.Id;
            if (!Guid.TryParse(session.PlayerId, out uuid))
            {
                uuid = Guid.Empty;
            }
            username = session.PlayerName;
            host = info.ServerIp;
            port = info.ServerPort;
            protocolVersion = info.ProtocolVersion;
            playerKeyPair = info.Player;
            accountLower = info.AccountLower;

            _sessionToken = session;

            // Start up client
            try
            {
                // Setup tcp client
                client = ProxyHandler.NewTcpClient(host, port);
                client!.ReceiveBufferSize = 1024 * 1024;
                client.ReceiveTimeout = 30000; // 30 seconds

                // Create handler
                handler = ProtocolHandler.GetProtocolHandler(client, info.ProtocolVersion, info.ForgeInfo, this);

                // Start update loop
                timeoutDetector = Tuple.Create(new Thread(TimeoutDetector), new CancellationTokenSource());
                timeoutDetector.Item1.Name = "Connection Timeout Detector";
                timeoutDetector.Item1.Start(timeoutDetector.Item2.Token);

                if (handler!.Login(playerKeyPair, session, accountLower)) // Login
                {
                    // Update entity type for dummy client entity
                    clientEntity.Type = EntityTypePalette.INSTANCE.GetById(EntityType.PLAYER_ID);
                    // Update client entity name
                    clientEntity.Name = session.PlayerName;
                    clientEntity.UUID = uuid;
                    Debug.Log($"Client uuid: {uuid}");
                    clientEntity.MaxHealth = 20F;

                    // Create player render
                    SwitchToFirstPlayerRender(clientEntity);
                    // Create camera controller
                    SwitchToFirstCameraController();

                    return true; // Client successfully started
                }
                Debug.LogError(Translations.Get("error.login_failed"));
                Disconnect();
            }
            catch (Exception e)
            {
                client = ProxyHandler.NewTcpClient(host, port);
                client!.ReceiveBufferSize = 1024 * 1024;
                client.ReceiveTimeout = 30000; // 30 seconds

                Debug.LogError(Translations.Get("error.connect", e.Message));
                Debug.LogError(e.StackTrace);
                Disconnect();
            }

            return false; // Failed to start client
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

            if (PlayerController)
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

        public void Transfer(string newHost, int newPort)
        {
            try
            {
                Debug.Log($"Initiating a transfer to: {host}:{port}");

                // Clear world data
                ChunkRenderManager.ClearChunksData();
                
                Loom.QueueOnMainThread(() =>
                {
                    EntityRenderManager.ReloadEntityRenders();
                });

                // Close existing connection
                client!.Close();

                // Establish new connection
                client = ProxyHandler.NewTcpClient(newHost, newPort);
                client!.ReceiveBufferSize = 1024 * 1024;
                client.ReceiveTimeout = 30000; // 30 seconds

                // Reinitialize the protocol handler
                handler = ProtocolHandler.GetProtocolHandler(client, protocolVersion, null, this);
                Debug.Log($"Connected to {host}:{port}");

                // Retry login process
                if (handler!.Login(playerKeyPair, _sessionToken!, accountLower))
                {
                    // TODO: Prepare client scene
                    Debug.Log("Successfully transferred connection and logged in.");
                }
                else
                {
                    Debug.LogError("Failed to login to the new host.");
                    throw new Exception("Login failed after transfer.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Transfer to {newHost}:{newPort} failed: {ex.Message}");

                // Handle reconnection attempts
                if (timeoutDetector != null)
                {
                    timeoutDetector.Item2.Cancel();
                    timeoutDetector = null;
                }

                throw new Exception("Transfer failed.");
            }
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
                for (int i = 0; i < 30; i++) // 15 seconds in total
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

            timeoutDetector?.Item2.Cancel();
            timeoutDetector = null;

            client?.Close();
            client = null;
            
            Loom.QueueOnMainThread(() =>
            {
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
            Loom.QueueOnMainThread(() =>
            {
                // Clear world data
                ChunkRenderManager.ClearChunksData();
                EntityRenderManager.ReloadEntityRenders();
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
        /// <example>bool result = InvokeOnNetMainThread(methodThatReturnsABool);</example>
        /// <example>bool result = InvokeOnNetMainThread(() => methodThatReturnsABool(argument));</example>
        /// <example>int result = InvokeOnNetMainThread(() => { yourCode(); return 42; });</example>
        /// <typeparam name="T">Type of the return value</typeparam>
        public T InvokeOnNetMainThread<T>(Func<T> task)
        {
            if (!InvokeRequired)
            {
                return task();
            }

            TaskWithResult<T> taskWithResult = new(task);
            lock (threadTasksLock)
            {
                threadTasks.Enqueue(taskWithResult.ExecuteSynchronously);
            }
            return taskWithResult.WaitGetResult();
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
        private void ClearTasks()
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
        private bool InvokeRequired
        {
            get
            {
                int callingThreadId = Thread.CurrentThread.ManagedThreadId;
                if (handler != null)
                {
                    return handler.GetNetMainThreadId() != callingThreadId;
                }

                // net main thread not yet ready
                return false;
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
        public override Guid GetUserUUID() => uuid;
        public override string GetUserUUIDStr() => uuid.ToString().Replace("-", string.Empty);
        public override string GetSessionId() => sessionId!;
        public override double GetServerAverageTps() => averageTPS;
        public override double GetLatestServerTps() => serverTPS;
        public override int GetPacketCount() => packetCount;
        public override int GetClientEntityId() => clientEntity.Id;
        public override double GetClientFoodSaturation() => foodSaturation;
        public override double GetClientExperienceLevel() => experienceLevel;
        public override double GetClientTotalExperience() => totalExperience;

        public override float GetTickMilSec() => (float)(1000D / averageTPS);

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
        public override InventoryData? GetInventory(int inventoryId)
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
                        $"\n{ChunkRenderManager.GetDebugInfo()}\n{EntityRenderManager.GetDebugInfo()}\nServer TPS: {GetLatestServerTps():0.0} (Avg: {GetServerAverageTps():0.0})";
            }
            
            return baseString;
        }

        /// <summary>
        /// Get all players latency
        /// </summary>
        public override Dictionary<string, int> GetPlayersLatency()
        {
            lock (onlinePlayers)
            {
                return onlinePlayers.ToDictionary(player => player.Value.Name, player => player.Value.Ping);
            }
        }

        /// <summary>
        /// Get latency for current player
        /// </summary>
        public override int GetOwnLatency()
        {
            lock (onlinePlayers)
            {
                return onlinePlayers.TryGetValue(uuid, out var selfPlayer) ? selfPlayer.Ping : 0;
            }
        }

        /// <summary>
        /// Get player info from uuid
        /// </summary>
        /// <param name="targetUUID">Player's UUID</param>
        public override PlayerInfo? GetPlayerInfo(Guid targetUUID)
        {
            lock (onlinePlayers)
            {
                return onlinePlayers.GetValueOrDefault(targetUUID);
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
        /// <returns>Dictionary of online players, key is UUID, value is Player name</returns>
        public override Dictionary<string, string> GetOnlinePlayersWithUUID()
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

        #region Action methods: Perform an action on the server

        public void SetCanSendMessage(bool canSend)
        {
            lock (chatQueue)
            {
                canSendMessage = canSend;
            }
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
                if (text.Length > maxLength) // Message is too long?
                {
                    if (text[0] == '/')
                    {
                        // Send the first 100/256 chars of the command
                        text = text[..maxLength];
                    }
                    else
                    {
                        // Split the message into several messages
                        while (text.Length > maxLength)
                        {
                            chatQueue.Enqueue(text[..maxLength]);
                            text = text[maxLength..];
                        }
                    }
                }

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
            return InvokeRequired ? InvokeOnNetMainThread(SendRespawnPacket) : handler!.SendRespawnPacket();
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified Id
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool SendEntityAction(EntityActionType entityAction)
        {
            return InvokeRequired ? InvokeOnNetMainThread(() => SendEntityAction(entityAction)) :
                handler!.SendEntityAction(clientEntity.Id, (int) entityAction);
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
            return InvokeRequired ? InvokeOnNetMainThread(UseItemOnMainHand) : handler!.SendUseItem(0, sequenceId++);
        }

        /// <summary>
        /// Use the item currently in the player's off hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool UseItemOnOffHand()
        {
            return InvokeRequired ? InvokeOnNetMainThread(UseItemOnOffHand) : handler!.SendUseItem(1, sequenceId++);
        }

        /// <summary>
        /// Open player inventory, doesn't actually send anything to the server
        /// </summary>
        public override void OpenPlayerInventory()
        {
            if (InvokeRequired)
            {
                InvokeOnNetMainThread(OpenPlayerInventory);
                return;
            }
            
            var playerInventory = inventories.GetValueOrDefault(0);
            if (playerInventory is null)
            {
                Debug.LogWarning("Player inventory data is not available!");
            }
            else
            {
                OnInventoryOpen(0, playerInventory);
            }
        }

        /// <summary>
        /// Click a slot in the specified inventory
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public override bool DoInventoryAction(int inventoryId, int slot, InventoryActionType actionType)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DoInventoryAction(inventoryId, slot, actionType));

            ItemStack? sendItem = null;
            //if (inventories.ContainsKey(inventoryId) && inventories[inventoryId].Items.TryGetValue(slot, out var iii))
            //{
            //    sendItem = new(iii.ItemType, iii.Count, iii.NBT);
            //}
            //if (inventories[0].Items.TryGetValue(slot, out var iii))
            //{
            //    sendItem = new(iii.ItemType, iii.Count, iii.NBT);
            //}
            
            List<Tuple<short, ItemStack?>> changedSlots = new(); // List<Slot Id, Changed Items>

            // Update our inventory base on action type
            InventoryData? inventory = GetInventory(inventoryId);
            InventoryData playerInventory = GetInventory(0)!;
            
            if (inventory != null)
            {
                var inventoryType = inventory.Type;
                var slotType = inventoryType.GetInventorySlotType(slot);

                if (slotType == InventorySlotType.Preview)
                {
                    return false; // Not interactable at all
                }

                var placePredicate = slotType.GetPlacePredicate();
                
                switch (actionType)
                {
                    case InventoryActionType.LeftClick:
                        // Check if cursor have item (slot -1)
                        if (playerInventory.Items.TryGetValue(-1, out var cursor1))
                        {
                            // Check target slot also have item
                            if (inventory.Items.TryGetValue(slot, out var target1))
                            {
                                // Check if items are stackable?
                                if (InventoryData.CheckStackable(target1, cursor1))
                                {
                                    int maxCount = target1.ItemType.StackLimit;
                                    
                                    // Check item stacking
                                    if (target1.Count + cursor1.Count <= maxCount)
                                    {
                                        if (slotType == InventorySlotType.Output)
                                        {
                                            // Stack target to cursor
                                            cursor1.Count += target1.Count;
                                            inventory.Items.Remove(slot);
                                            Debug.Log($"[LeftClick] [{slot}] Stack target (output) to cursor [{cursor1}]");
                                        }
                                        else
                                        {
                                            // Stack cursor to target
                                            target1.Count += cursor1.Count;
                                            playerInventory.Items.Remove(-1);
                                            Debug.Log($"[LeftClick] [{slot}] Stack cursor to target [{target1}]");
                                        }
                                    }
                                    else
                                    {
                                        if (slotType == InventorySlotType.Output)
                                        {
                                            Debug.Log($"[LeftClick] [{slot}] Cannot stack target (output) to cursor");
                                        }
                                        else
                                        {
                                            // Leave some item on cursor
                                            cursor1.Count -= maxCount - target1.Count;
                                            target1.Count = maxCount;
                                            Debug.Log($"[LeftClick] [{slot}] Stack cursor to target [{target1}], and leave some on cursor [{cursor1}]");
                                        }
                                    }
                                }
                                else // No stackable, swap
                                {
                                    if (!placePredicate(cursor1))
                                    {
                                        Debug.Log($"[LeftClick] [{slot}] Cannot swap target with cursor");
                                    }
                                    else
                                    {
                                        // Swap two items
                                        (inventory.Items[slot], playerInventory.Items[-1]) = (cursor1, target1);
                                        Debug.Log($"[LeftClick] [{slot}] Swap target [{target1}] with cursor [{cursor1}]");
                                    }
                                }
                            }
                            else // Target has no item
                            {
                                if (!placePredicate(cursor1))
                                {
                                    Debug.Log($"[LeftClick] [{slot}] Cannot put cursor to target");
                                }
                                else
                                {
                                    // Put cursor item to target
                                    inventory.Items[slot] = cursor1;
                                    playerInventory.Items.Remove(-1);
                                    Debug.Log($"[LeftClick] [{slot}] Put cursor to target [{inventory.Items[slot]}]");
                                }
                            }

                            changedSlots.Add(inventory.Items.TryGetValue(slot, out var inventoryItem)
                                ? new Tuple<short, ItemStack?>((short)slot, inventoryItem)
                                : new Tuple<short, ItemStack?>((short)slot, null));
                        }
                        else // Cursor has no item
                        {
                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slot))
                            {
                                // Put target slot item to cursor
                                playerInventory.Items[-1] = inventory.Items[slot];
                                inventory.Items.Remove(slot);
                                changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, null));
                                Debug.Log($"[LeftClick] [{slot}] Put target to cursor [{playerInventory.Items[-1]}]");
                            }
                            else // Target has no item
                            {
                                Debug.Log($"[LeftClick] [{slot}] Do nothing");
                            }
                        }
                        break;
                    case InventoryActionType.RightClick:
                        // Check if cursor have item (slot -1)
                        if (playerInventory.Items.TryGetValue(-1, out var cursor2))
                        {
                            // Check target slot also have item
                            if (inventory.Items.TryGetValue(slot, out var target2))
                            {
                                int maxCount = target2.ItemType.StackLimit;

                                // Check if these 2 items are stackable
                                if (InventoryData.CheckStackable(target2, cursor2))
                                {
                                    if (slotType == InventorySlotType.Output)
                                    {
                                        if (target2.Count + cursor2.Count <= maxCount)
                                        {
                                            // Stack target to cursor
                                            cursor2.Count += target2.Count;
                                            inventory.Items.Remove(slot);
                                            Debug.Log($"[RightClick] [{slot}] Stack target (output) to cursor [{cursor2}]");
                                        }
                                        else
                                        {
                                            Debug.Log($"[RightClick] [{slot}] Target (output) cannot be stacked anymore");
                                        }
                                    }
                                    else if (target2.Count + 1 <= maxCount)
                                    {
                                        // Drop 1 item count from cursor
                                        cursor2.Count--;
                                        target2.Count++;
                                        if (cursor2.Count <= 0) playerInventory.Items.Remove(-1);
                                        Debug.Log($"[RightClick] [{slot}] Drop cursor [{cursor2}] to target[{target2}]");
                                    }
                                }
                                else // No stackable, swap
                                {
                                    if (!placePredicate(cursor2))
                                    {
                                        Debug.Log($"[RightClick] [{slot}] Cannot swap target (output) with cursor");
                                    }
                                    else
                                    {
                                        // Swap two items
                                        (inventory.Items[slot], playerInventory.Items[-1]) = (cursor2, target2);
                                        Debug.Log($"[RightClick] [{slot}] Swap target [{target2}] with cursor [{cursor2}]");
                                    }
                                }
                            }
                            else // Target has no item
                            {
                                if (!placePredicate(cursor2))
                                {
                                    Debug.Log($"[RightClick] [{slot}] Cannot drop 1 cursor to target");
                                }
                                else
                                {
                                    // Drop 1 item count from cursor
                                    ItemStack itemClone = new(cursor2.ItemType, 1, cursor2.NBT);
                                    inventory.Items[slot] = itemClone;
                                    cursor2.Count--;
                                    if (cursor2.Count <= 0) playerInventory.Items.Remove(-1);
                                    Debug.Log($"[RightClick] [{slot}] Drop 1 cursor [{cursor2}] to target [{inventory.Items[slot]}]");
                                }
                            }
                        }
                        else // Cursor has no item
                        {
                            // Check target slot have item?
                            if (inventory.Items.TryGetValue(slot, out var target2))
                            {
                                if (inventoryType.GetInventorySlotType(slot) == InventorySlotType.Output)
                                {
                                    // You can't take half from an output slot, put entire item stack from target to cursor
                                    playerInventory.Items[-1] = inventory.Items[slot];
                                    inventory.Items.Remove(slot);
                                    changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, null));
                                    Debug.Log($"[RightClick] [{slot}] Put target (output) to cursor [{playerInventory.Items[-1]}]");
                                }
                                else // Take half of the item stack
                                {
                                    if (target2.Count == 1)
                                    {
                                        // Only 1 item. Put it to cursor
                                        playerInventory.Items[-1] = inventory.Items[slot];
                                        inventory.Items.Remove(slot);
                                        Debug.Log($"[RightClick] [{slot}] Take 1 target [{target2}] to cursor [{cursor2}]");
                                    }
                                    else
                                    {
                                        // Take half of the item stack to cursor
                                        if (inventory.Items[slot].Count % 2 == 0)
                                        {
                                            // Can be evenly divided
                                            var itemTmp = inventory.Items[slot];
                                            playerInventory.Items[-1] = new ItemStack(itemTmp.ItemType, itemTmp.Count / 2, itemTmp.NBT);
                                            inventory.Items[slot].Count = itemTmp.Count / 2;
                                            Debug.Log($"[RightClick] [{slot}] Take half(even) target [{target2}] to cursor [{cursor2}]");
                                        }
                                        else
                                        {
                                            // Cannot be evenly divided. item count on cursor is always larger than item on inventory
                                            var itemTmp = inventory.Items[slot];
                                            playerInventory.Items[-1] = new ItemStack(itemTmp.ItemType, (itemTmp.Count + 1) / 2, itemTmp.NBT);
                                            inventory.Items[slot].Count = (itemTmp.Count - 1) / 2;
                                            Debug.Log($"[RightClick] [{slot}] Take half(odd) target [{target2}] to cursor [{cursor2}]");
                                        }
                                    }
                                }
                            }
                            else // Target has no item
                            {
                                Debug.Log($"[RightClick] [{slot}] Do nothing");
                            }
                        }

                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slot,
                            inventory.Items.GetValueOrDefault(slot)));
                        break;
                    case InventoryActionType.ShiftClick:
                    case InventoryActionType.ShiftRightClick:
                        // TODO: Implement
                        break;
                    case InventoryActionType.DropItem:
                        if (inventory.Items.ContainsKey(slot))
                        {
                            inventory.Items[slot].Count--;
                            changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, inventory.Items[slot]));
                        }

                        if (inventory.Items[slot].Count <= 0)
                        {
                            inventory.Items.Remove(slot);
                            changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, null));
                        }

                        break;
                    case InventoryActionType.DropItemStack:
                        inventory.Items.Remove(slot);
                        changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, null));
                        break;

                    case InventoryActionType.StartDragLeft: // Distribute evenly
                    case InventoryActionType.StartDragRight: // Drop 1 in each
                        draggedSlots.Clear();
                        dragStartCursorItemClone = null;

                        // Check if cursor have item (slot -1) and update dragging status
                        if (playerInventory.Items.TryGetValue(-1, out var cursor7))
                        {
                            Debug.Log($"[{actionType}] Start from [{slot}]. Cursor item {cursor7}");
                            dragStartCursorItemClone = new(cursor7.ItemType, cursor7.Count, cursor7.NBT);
                            dragging = true;
                        }
                        else
                        {
                            Debug.LogWarning($"[{actionType}] Cursor slot shouldn't be empty!");
                            dragging = false;
                            return false;
                        }
                        break;
                    case InventoryActionType.AddDragLeft:
                    case InventoryActionType.AddDragRight:
                        if (!dragging || draggedSlots.ContainsKey(slot)) return false;
                        Debug.Log($"[{actionType}] Add dragged slot [{slot}]");
                        
                        var target8 = inventory.Items.GetValueOrDefault(slot);
                        draggedSlots[slot] = target8?.Count ?? 0; // Initial count is 0 if the slot is empty
                        
                        var stackMax = dragStartCursorItemClone.ItemType.StackLimit;
                        var initCursorCount = dragStartCursorItemClone.Count;
                        
                        int maxAllocForEachSlot = actionType switch
                        {
                            InventoryActionType.AddDragLeft => initCursorCount / draggedSlots.Count,
                            InventoryActionType.AddDragRight => 1,
                            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null)
                        };
                        int actualAllocationTotal = 0;
                        
                        // Increase item count in each of the dragged slots
                        foreach (var (draggedSlot, initCount) in draggedSlots)
                        {
                            if (stackMax <= initCount) continue; // Full stack already
                            
                            int actualAllocation = Mathf.Min(stackMax - initCount, maxAllocForEachSlot);
                            actualAllocationTotal += actualAllocation;
                            Debug.Log($"Dragged slot [{draggedSlot}] {initCount} +{actualAllocation} => {initCount + actualAllocation}");
                            if (inventory.Items.TryGetValue(draggedSlot, out target8))
                            {
                                target8.Count = initCount + actualAllocation;
                            }
                            else
                            {
                                inventory.Items[draggedSlot] = new ItemStack(dragStartCursorItemClone.ItemType, actualAllocation, dragStartCursorItemClone.NBT);
                            }
                            changedSlots.Add(new Tuple<short, ItemStack?>((short) draggedSlot, inventory.Items[draggedSlot]));
                        }
                        
                        // And reduce the item count in cursor slot
                        if (actualAllocationTotal == initCursorCount) // All items put into dragged slots, nothing left on cursor
                        {
                            playerInventory.Items.Remove(-1);
                        }
                        else if (actualAllocationTotal > initCursorCount) // Emm... We've got a problem here
                        {
                            Debug.LogWarning($"[{actionType}] WTF? Allocating {actualAllocationTotal} items out of a total of {dragStartCursorItemClone.Count}!");
                        }
                        else if (playerInventory.Items.TryGetValue(-1, out var cursor8)) // Reduce cursor count
                        {
                            cursor8.Count = dragStartCursorItemClone.Count - actualAllocationTotal;
                        }
                        else // Previous allocation used up all the item, recreate an item stack on cursor
                        {
                            playerInventory.Items[-1] = new ItemStack(dragStartCursorItemClone.ItemType, initCursorCount - actualAllocationTotal, dragStartCursorItemClone.NBT);
                        }
                        Debug.Log($"Cursor slot {initCursorCount} -{actualAllocationTotal} => {initCursorCount - actualAllocationTotal}");
                        break;
                    case InventoryActionType.EndDragLeft:
                    case InventoryActionType.EndDragRight:
                        if (!dragging) return false;
                        dragging = false;
                        Debug.Log($"[{actionType}] End");
                        
                        break;
                    case InventoryActionType.MiddleClick:
                    case InventoryActionType.StartDragMiddle:
                    case InventoryActionType.AddDragMiddle:
                    case InventoryActionType.EndDragMiddle:
                    default:
                        Debug.Log($"Inventory action not handled: {actionType}");
                        break;
                }
            }
            else
            {
                return false;
            }

            foreach (var (slotId, slotItem) in changedSlots) // Update local data for each changed slot
            {
                EventManager.Instance.BroadcastOnUnityThread(new InventorySlotUpdateEvent(inventoryId, slotId, slotItem, true));

                if (inventory.IsBackpack(slotId, out var backpackSlot)) // Update backpack slots in player inventory
                {
                    var slotInPlayerInventory = playerInventory.GetFirstBackpackSlot() + backpackSlot;
                    if (slotItem is null)
                        playerInventory.Items.Remove(slotInPlayerInventory);
                    else
                        playerInventory.Items[slotInPlayerInventory] = slotItem;
                }
                
                if (inventory.IsHotbar(slotId, out var hotbarSlot)) // Update hotbar slots in player inventory
                {
                    var slotInPlayerInventory = playerInventory.GetFirstHotbarSlot() + hotbarSlot;
                    if (slotItem is null)
                        playerInventory.Items.Remove(slotInPlayerInventory);
                    else
                        playerInventory.Items[slotInPlayerInventory] = slotItem;
                    
                    EventManager.Instance.BroadcastOnUnityThread(new HotbarSlotUpdateEvent(hotbarSlot, slotItem));

                    if (hotbarSlot == CurrentSlot) // The currently held item is updated
                    {
                        EventManager.Instance.BroadcastOnUnityThread(new HeldItemUpdateEvent(
                                hotbarSlot, slotItem, PlayerActionHelper.GetItemActionType(slotItem)));
                    }
                }
            }
            var cursorItem = playerInventory.Items.GetValueOrDefault(-1);
            var stateId = inventory.StateId;

            sendItem = cursorItem;
            
            //Debug.Log($"Changed slots: {string.Join(", ", changedSlots.Select(x => $"[{x.Item1}] {x.Item2}"))}, Cursor item: {cursorItem}, Send item: {sendItem}, State id: {stateId}");
            EventManager.Instance.BroadcastOnUnityThread(new InventorySlotUpdateEvent(inventoryId, -1, cursorItem, true));

            return handler!.SendInventoryAction(inventoryId, slot, actionType, sendItem, changedSlots, stateId);
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
            return InvokeRequired ? InvokeOnNetMainThread(() => DoCreativeGive(slot, itemType, count, nbt)) : handler!.SendCreativeInventoryAction(slot, itemType, count, nbt);
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="playerAnimation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public override bool DoAnimation(int playerAnimation)
        {
            return InvokeRequired ? InvokeOnNetMainThread(() => DoAnimation(playerAnimation)) : handler!.SendAnimation(playerAnimation, clientEntity.Id);
        }

        /// <summary>
        /// Close the specified inventory
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <returns>TRUE if the inventory was successfully closed</returns>
        /// <remarks>Sending close inventory for inventory 0 can cause server to update our inventory if there are any item in the crafting area</remarks>
        public override bool CloseInventory(int inventoryId)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => CloseInventory(inventoryId));

            if (inventories.ContainsKey(inventoryId))
            {
                if (inventoryId != 0)
                    inventories.Remove(inventoryId);
                return handler!.SendCloseInventory(inventoryId);
            }
            return false;
        }

        /// <summary>
        /// Clean all inventory
        /// </summary>
        /// <returns>TRUE if the successfully cleared</returns>
        public override bool ClearInventories()
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(ClearInventories);

            inventories.Clear();
            inventories[0] = new InventoryData(0, InventoryType.PLAYER, "Player Inventory");
            return true;
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
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => InteractEntity(entityId, type, hand));

            if (EntityRenderManager.HasEntityRender(entityId))
            {
                return type == 0 ?
                    handler!.SendInteractEntity(entityId, type, (int) hand) :
                    handler!.SendInteractEntity(entityId, type);
            }

            return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="blockLoc">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <param name="x">Block x</param>
        /// <param name="y">Block y</param>
        /// <param name="z">Block z</param>
        /// <param name="hand">Hand to use</param>
        /// <returns>TRUE if successfully placed</returns>
        public override bool PlaceBlock(BlockLoc blockLoc, Direction blockFace, float x, float y, float z, Hand hand = Hand.MainHand)
        {
            return InvokeRequired ? InvokeOnNetMainThread(() => PlaceBlock(blockLoc, blockFace, x, y, z)) :
                handler!.SendPlayerBlockPlacement((int)hand, blockLoc, x, y, z, blockFace, sequenceId++);
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="blockLoc">Location of block to dig</param>
        /// <param name="blockFace">Block face</param>
        /// <param name="status">Digging status</param>
        public override bool DigBlock(BlockLoc blockLoc, Direction blockFace, DiggingStatus status)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DigBlock(blockLoc, blockFace, status));

            return status switch
            {
                DiggingStatus.Started => handler!.SendPlayerDigging(0, blockLoc, blockFace, sequenceId++),
                DiggingStatus.Cancelled => handler!.SendPlayerDigging(1, blockLoc, blockFace, sequenceId++),
                DiggingStatus.Finished => handler!.SendPlayerDigging(2, blockLoc, blockFace, sequenceId++),
                _ => false
            };
        }

        /// <summary>
        /// Drop item in active hotbar slot
        /// </summary>
        /// <param name="dropEntireStack">Whether or not to drop the entire item stack</param>
        public override bool DropItem(bool dropEntireStack)
        {
            if (GameMode == GameMode.Spectator) return false;

            var curItem = inventories[0].GetHotbarItem(CurrentSlot);
            if (curItem == null || curItem.IsEmpty)
            {
                return false;
            }

            bool sent = handler!.SendPlayerAction(dropEntireStack ? 3 : 4);
            if (sent) // Do update on client side
            {
                ItemStack? updatedItem;

                if (dropEntireStack || curItem.Count <= 1)
                {
                    updatedItem = null; // Nothing left in slot
                    //Debug.Log($"Dropped every {curItem.ItemType.ItemId} in hotbar slot {CurrentSlot}.");
                }
                else
                {
                    updatedItem = new ItemStack(curItem.ItemType, curItem.Count - 1, curItem.NBT);
                    //Debug.Log($"Dropped a single {curItem.ItemType.ItemId} in hotbar slot {CurrentSlot}, {updatedItem.Count} left.");
                }

                int invSlot = inventories[0].GetFirstHotbarSlot() + CurrentSlot;
                if (updatedItem is null)
                {
                    inventories[0].Items.Remove(invSlot);
                }
                else
                {
                    // Add or update slot item stack
                    inventories[0].Items[invSlot] = updatedItem;
                }

                // Starting from 1.17, the server no longer send a packet to
                // update this dropped slot, so we do it manually here.
                if (protocolVersion >= ProtocolMinecraft.MC_1_17_1_Version)
                {
                    EventManager.Instance.Broadcast(new InventorySlotUpdateEvent(0, invSlot, updatedItem, true));
                    EventManager.Instance.Broadcast(new HotbarSlotUpdateEvent(CurrentSlot, updatedItem));
                    EventManager.Instance.Broadcast(new HeldItemUpdateEvent(
                                        CurrentSlot, updatedItem, PlayerActionHelper.GetItemActionType(updatedItem)));
                }
            }

            return sent;
        }

        /// <summary>
        /// Swap item stacks in main hand and off hand
        /// </summary>
        public override bool SwapItemOnHands()
        {
            return GameMode != GameMode.Spectator && handler!.SendPlayerAction(6);
        }

        /// <summary>
        /// Change active slot in the player inventory
        /// </summary>
        /// <param name="slot">Slot to activate (0 to 8)</param>
        /// <returns>TRUE if the slot was changed</returns>
        public override bool ChangeHotbarSlot(short slot)
        {
            if (slot is < 0 or > 8)
                return false;

            if (InvokeRequired)
                return InvokeOnNetMainThread(() => ChangeHotbarSlot(slot));
            
            // There won't be confirmation from the server
            // Simply set it on the client side
            CurrentSlot = Convert.ToByte(slot);
            var curItem = inventories[0].GetHotbarItem(slot);
            // Broad cast hotbar selection change
            EventManager.Instance.BroadcastOnUnityThread(
                    new HeldItemUpdateEvent(CurrentSlot, curItem, PlayerActionHelper.GetItemActionType(curItem)));

            return handler!.SendHeldItemChange(slot);
        }

        /// <summary>
        /// Update sign text
        /// </summary>
        /// <param name="location">sign location</param>
        /// <param name="line1">text one</param>
        /// <param name="line2">text two</param>
        /// <param name="line3">text three</param>
        /// <param name="line4">text four</param>
        public bool UpdateSign(Location location, string line1, string line2, string line3, string line4)
        {
            // TODO Open sign editor first https://wiki.vg/Protocol#Open_Sign_Editor
            return InvokeRequired ? InvokeOnNetMainThread(() => UpdateSign(location, line1, line2, line3, line4)) :
                handler!.SendUpdateSign(location, line1, line2, line3, line4);
        }

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0</param>
        public bool SelectTrade(int selectedSlot)
        {
            return InvokeRequired ? InvokeOnNetMainThread(() => SelectTrade(selectedSlot)) :
                handler!.SelectTrade(selectedSlot);
        }

        /// <summary>
        /// Teleport to player in spectator mode
        /// </summary>
        /// <param name="entity">Player to teleport to</param>
        /// Teleporting to other entities is NOT implemented yet
        public bool Spectate(EntityData entity)
        {
            return entity.Type.TypeId == EntityType.PLAYER_ID && SpectateByUUID(entity.UUID);
        }

        /// <summary>
        /// Teleport to player/entity in spectator mode
        /// </summary>
        /// <param name="UUID">UUID of player/entity to teleport to</param>
        private bool SpectateByUUID(Guid UUID)
        {
            if (GameMode == GameMode.Spectator)
            {
                return InvokeRequired ?
                    InvokeOnNetMainThread(() => SpectateByUUID(UUID)) :
                    handler!.SendSpectate(UUID);
            }
            return false;
        }
        #endregion

        #region Event handlers: Handle an event occurred on the server
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

            // Clear world data
            ChunkRenderManager.ClearChunksData();

            //if (!keepAttr)
            {
                ClearInventories();
            }

            Loom.QueueOnMainThread(() =>
            {
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

                    Loom.QueueOnMainThread(() =>
                    {
                        // Update player location
                        PlayerController.SetLocationFromServer(location, mcYaw: yaw);
                        // Force refresh environment collider
                        ChunkRenderManager.InitializeBoxTerrainCollider(location.GetBlockLoc(), () =>
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
                    Loom.QueueOnMainThread(() =>
                    {
                        var delta = PlayerController.transform.position - CoordConvert.MC2Unity(WorldOriginOffset, location);
                        if (delta.magnitude < 8F)
                        {
                            return; // I don't like this packet.
                        }

                        // Force refresh environment collider
                        ChunkRenderManager.RebuildTerrainBoxCollider(location.GetBlockLoc());
                        // Then update player location
                        PlayerController.SetLocationFromServer(location, reset: true, mcYaw: yaw);
                        //Debug.Log($"Updated to {location} offset: {offset.magnitude}");
                    });
                }
            }
        }

        /// <summary>
        /// Called when received chat/system message from the server
        /// </summary>
        /// <param name="message">Message received</param>
        public void OnTextReceived(ChatMessage message)
        {
            UpdateKeepAlive();

            List<string> links = new();
            string messageText;

            // Used for 1.19+ to mark: system message, legal / illegal signature
            string color = string.Empty;

            if (message.isSignedChat)
            {
                if (!ProtocolSettings.ShowIllegalSignedChat && !message.isSystemChat && !(bool)message.isSignatureLegal!)
                    return;
                messageText = ChatParser.ParseSignedChat(message, links);

                if (message.isSystemChat)
                {
                    if (ProtocolSettings.MarkSystemMessage)
                        color = "§7▌§r";     // Background Gray
                }
                else
                {
                    if ((bool) message.isSignatureLegal!)
                    {
                        if (ProtocolSettings.ShowModifiedChat && message.unsignedContent != null)
                        {
                            if (ProtocolSettings.MarkModifiedMsg)
                                color = "§6▌§r"; // Background Yellow
                        }
                        else
                        {
                            if (ProtocolSettings.MarkLegallySignedMsg)
                                color = "§2▌§r"; // Background Green
                        }
                    }
                    else
                    {
                        if (ProtocolSettings.MarkIllegallySignedMsg)
                            color = "§4▌§r"; // Background Red
                    }
                }
            }
            else
            {
                messageText = message.isJson ? ChatParser.ParseText(message.content, links) : message.content;
            }

            EventManager.Instance.BroadcastOnUnityThread<ChatMessageEvent>(new(color + messageText));
        }

        /// <summary>
        /// Called when received a connection keep-alive from the server
        /// </summary>
        public void OnServerKeepAlive()
        {
            UpdateKeepAlive();
        }

        /// <summary>
        /// Called when tab complete suggestion is received
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
        /// Called when an inventory is opened
        /// </summary>
        /// <param name="inventoryData">The inventory</param>
        /// <param name="inventoryId">Inventory Id</param>
        public void OnInventoryOpen(int inventoryId, InventoryData inventoryData)
        {
            activeInventoryId = inventoryId;
            inventories[inventoryId] = inventoryData;

            Debug.Log(Translations.Get("extra.inventory_open", inventoryId, inventoryData.Title));

            Loom.QueueOnMainThread(() =>
            {
                // Set inventory id before opening the screen
                ScreenControl.SetScreenData<InventoryScreen>(screen =>
                {
                    screen.SetActiveInventory(inventoryData);
                });
                ScreenControl.PushScreen<InventoryScreen>();
            });
        }

        /// <summary>
        /// Called when an inventory is closed
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        public void OnInventoryClose(int inventoryId)
        {
            if (activeInventoryId != inventoryId)
            {
                Debug.LogWarning($"Trying to close inventory [{inventoryId}] while active inventory is [{activeInventoryId}]!");
            }
            activeInventoryId = -1;
            
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
        /// When received inventory properties from server.
        /// Used for Furnaces, Enchanting Table, Beacon, Brewing stand, Stone cutter, Loom and Lectern
        /// More info about: https://wiki.vg/Protocol#Set_Inventory_Property
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="propertyId">Property Id</param>
        /// <param name="propertyValue">Property Value</param>
        public void OnInventoryProperties(byte inventoryId, short propertyId, short propertyValue)
        {
            if (!inventories.TryGetValue(inventoryId, out var inventory))
                return;

            inventory.Properties[propertyId] = propertyValue;

            if (inventory.Type.TypeId == InventoryType.ENCHANTMENT_ID)
            {
                // We got the last property for enchantment
                if (propertyId == 9 && propertyValue != -1)
                {
                    var topEnchantmentLevelRequirement = inventory.Properties[0];
                    var middleEnchantmentLevelRequirement = inventory.Properties[1];
                    var bottomEnchantmentLevelRequirement = inventory.Properties[2];

                    var topEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[4]);

                    var middleEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[5]);

                    var bottomEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[6]);

                    var topEnchantmentLevel = inventory.Properties[7];
                    var middleEnchantmentLevel = inventory.Properties[8];
                    var bottomEnchantmentLevel = inventory.Properties[9];

                    var lastEnchantment = new EnchantmentData
                    {
                        TopEnchantment = topEnchantment,
                        MiddleEnchantment = middleEnchantment,
                        BottomEnchantment = bottomEnchantment,

                        Seed = inventory.Properties[3],

                        TopEnchantmentLevel = topEnchantmentLevel,
                        MiddleEnchantmentLevel = middleEnchantmentLevel,
                        BottomEnchantmentLevel = bottomEnchantmentLevel,

                        TopEnchantmentLevelRequirement = topEnchantmentLevelRequirement,
                        MiddleEnchantmentLevelRequirement = middleEnchantmentLevelRequirement,
                        BottomEnchantmentLevelRequirement = bottomEnchantmentLevelRequirement
                    };

                    // TODO: Broadcast enchantment event
                }
            }

            Loom.QueueOnMainThread(() =>
            {
                EventManager.Instance.Broadcast(new InventoryPropertyUpdateEvent(inventoryId, propertyId, propertyValue));
            });
        }

        /// <summary>
        /// When received inventory items from server
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="itemList">Item list, key = slot Id, value = Item information</param>
        /// <param name="stateId">State Id of inventory</param>
        public void OnInventoryItems(byte inventoryId, Dictionary<int, ItemStack?> itemList, int stateId)
        {
            if (inventories.TryGetValue(inventoryId, out var inventory))
            {
                //Debug.Log($"Slots: [{inventoryId}] [{string.Join(", ", inventory.Items.Keys)}] -> [{string.Join(", ", itemList.Keys)}]");
                foreach (var (slot, itemStack) in itemList)
                {
                    if (itemStack is null)
                        inventory.Items.Remove(slot);
                    else
                        inventory.Items[slot] = itemStack;
                }

                inventory.StateId = Mathf.Max(inventory.StateId, stateId);
                var slotsText = string.Join(", ", itemList.Select(x => $"[{x.Key}] {x.Value}"));
                Debug.Log($"[OnInventoryItems] Set inventory [{inventoryId}] items. State id set to {stateId}. Slots: {slotsText}");
                
                Loom.QueueOnMainThread(() =>
                {
                    EventManager.Instance.Broadcast(new InventoryItemsUpdateEvent(inventoryId, itemList));

                    foreach (var (slot, itemStack) in itemList)
                    {
                        if (inventory.IsHotbar(slot, out int hotbarSlot))
                        {
                            EventManager.Instance.Broadcast(new HotbarSlotUpdateEvent(hotbarSlot, itemStack));
                            if (hotbarSlot == CurrentSlot) // Updating held item
                            {
                                EventManager.Instance.Broadcast(new HeldItemUpdateEvent(
                                    CurrentSlot, itemStack, PlayerActionHelper.GetItemActionType(itemStack)));
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Called when a single slot has been updated inside an inventory
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="slot">Item slot</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        /// <param name="stateId">State Id</param>
        /// <param name="fromClient">Whether this is sent from client</param>
        public void OnInventorySlot(byte inventoryId, short slot, ItemStack? item, int stateId, bool fromClient)
        {
            if (inventories.TryGetValue(inventoryId, out var inventory))
            {
                inventory.StateId = Mathf.Max(inventory.StateId, stateId);
            }

            if (dragging) return;
            
            // Handle inventoryId -2 - Add item to player inventory without animation. State id should be ignored
            if (inventoryId == 254)
                inventoryId = 0;

            if (inventoryId == 255 && slot == -1) // Handle cursor item
            {
                if (inventories.ContainsKey(0))
                {
                    if (item != null)
                        inventories[0].Items[-1] = item;
                    else
                        inventories[0].Items.Remove(-1);
                    
                    EventManager.Instance.BroadcastOnUnityThread(new InventorySlotUpdateEvent(0, -1, item, fromClient));
                }
            }
            else
            {
                if (inventories.TryGetValue(inventoryId, out var inventory2))
                {
                    if (item == null || item.IsEmpty)
                    {
                        inventory2.Items.Remove(slot);
                    }
                    else
                    {
                        inventory2.Items[slot] = item;
                    }

                    Loom.QueueOnMainThread(() =>
                    {
                        EventManager.Instance.Broadcast(new InventorySlotUpdateEvent(inventoryId, slot, item, fromClient));

                        if (inventory2.IsHotbar(slot, out int hotbarSlot))
                        {
                            EventManager.Instance.Broadcast(new HotbarSlotUpdateEvent(hotbarSlot, item));
                            if (hotbarSlot == CurrentSlot) // Updating held item
                            {
                                EventManager.Instance.Broadcast(new HeldItemUpdateEvent(
                                        CurrentSlot, item, PlayerActionHelper.GetItemActionType(item)));
                            }
                        }
                    });
                }
                else
                {
                    Debug.LogWarning($"Inventory [{inventoryId}] is not available!");
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
            //Debug.Log($"Player entity Id received: {entityId}");
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
                // This will disable custom skin for client player
                uuid = player.UUID;
                // Also update client entity uuid
                clientEntity.UUID = uuid;
                Debug.Log($"Updated client uuid: {this.uuid}");
            }

            lock (onlinePlayers)
            {
                onlinePlayers[player.UUID] = player;
            }
        }

        /// <summary>
        /// Triggered when a player has left the game
        /// </summary>
        /// <param name="playerUUID">UUID of the player</param>
        public void OnPlayerLeave(Guid playerUUID)
        {
            lock (onlinePlayers)
            {
                onlinePlayers.Remove(playerUUID);
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
        public void OnSpawnEntity(EntityData entity)
        {
            Loom.QueueOnMainThread(() =>
            {
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
        public void OnSpawnPlayer(int entityId, Guid playerUUID, Location location, byte Yaw, byte Pitch)
        {
            string? playerName = null;
            lock (onlinePlayers)
            {
                if (onlinePlayers.TryGetValue(playerUUID, out var player))
                    playerName = player.Name;
            }
            EntityData playerEntity = new(entityId, EntityTypePalette.INSTANCE.GetById(EntityType.PLAYER_ID), location, playerUUID, playerName);
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
        /// <param name="playerUUID">Player UUID (Empty for initial gamemode on login)</param>
        /// <param name="gamemode">New Game Mode (0: Survival, 1: Creative, 2: Adventure, 3: Spectator).</param>
        public void OnGamemodeUpdate(Guid playerUUID, int gamemode)
        {
            if (playerUUID == Guid.Empty) // Initial gamemode on login
            {
                Loom.QueueOnMainThread(() =>
                {
                    GameMode = (GameMode) gamemode;
                    EventManager.Instance.Broadcast<GameModeUpdateEvent>(new(GameMode));
                });
            }
            else
            {
                lock (onlinePlayers)
                {
                    if (onlinePlayers.TryGetValue(playerUUID, out var player)) // Further regular gamemode change events
                    {
                        string playerName = player.Name;
                        if (playerName == username)
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
            }
        }

        /// <summary>
        /// Called when entities dead/despawn.
        /// </summary>
        public void OnDestroyEntities(int[] entities)
        {
            Loom.QueueOnMainThread(() =>
            {
                EntityRenderManager.RemoveEntityRenders(entities);
            });
        }

        /// <summary>
        /// Called when an entity's position changed within 8 block of its previous position.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="dx">X offset</param>
        /// <param name="dy">Y offset</param>
        /// <param name="dz">Z offset</param>
        /// <param name="onGround">Whether the entity is grounded</param>
        public void OnEntityPosition(int entityId, double dx, double dy, double dz, bool onGround)
        {
            Loom.QueueOnMainThread(() =>
            {
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
            Loom.QueueOnMainThread(() =>
            {
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
            Loom.QueueOnMainThread(() =>
            {
                EntityRenderManager.RotateEntityRenderHead(entityId, headYaw);
            });
        }

        /// <summary>
        /// Called when an entity's head yaw changed.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="vX">New x velocity</param>
        /// <param name="vY">New y velocity</param>
        /// <param name="vZ">New z velocity</param>
        public void OnEntityVelocity(int entityId, short vX, short vY, short vZ)
        {
            Loom.QueueOnMainThread(() =>
            {
                EntityRenderManager.SetEntityRenderVelocity(entityId, new(vX / 8000F, vY / 8000F, vZ / 8000F));
            });
        }

        public void OnEntityTeleport(int entityId, double X, double Y, double Z, bool onGround)
        {
            Loom.QueueOnMainThread(() =>
            {
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

            Loom.QueueOnMainThread(() =>
            {
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
                if (tps is <= 20 and > 0)
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
        /// <param name="food">Player current hunger</param>
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
        /// Called when experience is updated
        /// </summary>
        /// <param name="expBar">Between 0 and 1</param>
        /// <param name="expLevel">Experience level</param>
        /// <param name="totalExp">Total experience</param>
        public void OnSetExperience(float expBar, int expLevel, int totalExp)
        {
            experienceLevel = expLevel;
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
        /// <param name="playerUUID">player uuid</param>
        /// <param name="latency">Latency</param>
        public void OnLatencyUpdate(Guid playerUUID, int latency)
        {
            lock (onlinePlayers)
            {
                if (onlinePlayers.TryGetValue(playerUUID, out var player))
                {
                    player.Ping = latency;
                }   
            }
        }

        /// <summary>
        /// Called when held item is changed
        /// </summary>
        /// <param name="slot"> item slot</param>
        public void OnHeldItemChange(byte slot) //
        {
            CurrentSlot = slot;
            var newItem = inventories[0].GetHotbarItem(slot);
            // Broad cast hotbar selection change
            EventManager.Instance.BroadcastOnUnityThread(
                    new HeldItemUpdateEvent(CurrentSlot, newItem,
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
        /// <param name="subtitleText"> subtitle text</param>
        /// <param name="actionbarText"> action bar text</param>
        /// <param name="fadeIn"> Fade In</param>
        /// <param name="stay"> Stay</param>
        /// <param name="fadeOut"> Fade Out</param>
        /// <param name="json"> json text</param>
        /// </summary>
        public void OnTitle(int action, string titleText, string subtitleText, string actionbarText, int fadeIn, int stay, int fadeOut, string json) { }

        /// <summary>
        /// Called when ScoreboardObjective is updated
        /// </summary>
        /// <param name="objectiveName">objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectiveValue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        public void OnScoreboardObjective(string objectiveName, byte mode, string objectiveValue, int type)
        {
            //string json = objectiveValue;
            //objectiveValue = ChatParser.ParseText(objectiveValue);
        }

        /// <summary>
        /// Called when DisplayScoreboard is updated
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
        /// Called when the metadata of an entity changed is received
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="metadata">The metadata of the entity</param>
        public void OnEntityMetadata(int entityId, Dictionary<int, object?> metadata)
        {
            Loom.QueueOnMainThread(() =>
            {
                var entity = EntityRenderManager.GetEntityRender(entityId);

                if (entity)
                {
                    entity.UpdateMetadata(metadata);
                }
            });
        }

        /// <summary>
        /// Called when tradeList is received after interacting with villager
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="trades">List of trades</param>
        /// <param name="villagerInfo">Contains Level, Experience, IsRegularVillager and CanRestock</param>
        public void OnTradeList(int inventoryId, List<VillagerTrade> trades, VillagerInfo villagerInfo) { }

        /// <summary>
        /// Called when another player break block in gamemode 0
        /// </summary>
        /// <param name="entityId">Player Id</param>
        /// <param name="blockLoc">Block location</param>
        /// <param name="stage">Destroy stage, maximum 255</param>
        public void OnBlockBreakAnimation(int entityId, BlockLoc blockLoc, byte stage)
        {
            /*
            var block = ChunkRenderManager.GetBlock(blockLoc);
            var status = stage < 9 ? DiggingStatus.Started : DiggingStatus.Finished;
            */
        }

        /// <summary>
        /// Called when animation of breaking and placing block is played
        /// </summary>
        /// <param name="entityId">Player Id</param>
        /// <param name="entityAnimation">0 = LMB, 1 = RMB (RMB Currently not working)</param>
        public void OnEntityAnimation(int entityId, byte entityAnimation)
        {
            // TODO
        }

        /// <summary>
        /// Called when rain(snow) starts or stops
        /// </summary>
        /// <param name="begin">true if the rain is starting</param>
        public void OnRainChange(bool begin)
        {
            Loom.QueueOnMainThread(() =>
            {
                EnvironmentManager.SetRain(begin);
            });
        }

        /// <summary>
        /// Called when a Synchronization sequence is received, this sequence need to be sent when breaking or placing blocks
        /// </summary>
        /// <param name="newSequenceId">Sequence Id</param>
        public void OnBlockChangeAck(int newSequenceId)
        {
            sequenceId = newSequenceId;
        }

        /// <summary>
        /// Called when the protocol handler receives server data
        /// </summary>
        /// <param name="hasMotd">Indicates if the server has a MOTD message</param>
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
        /// <param name="playerUUID">The player's UUID received from the server</param>
        /// <param name="userName">The player's DUMMY_USERNAME received from the server</param>
        /// <param name="playerProperty">Tuple(Name, Value, Signature(empty if there is no signature))</param>
        public void OnLoginSuccess(Guid playerUUID, string userName, Tuple<string, string, string>[]? playerProperty) { }

        #endregion
    }
}
