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
        private readonly Dictionary<int, InventoryData> inventories = new();

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
                
                Loom.QueueOnMainThread(() => {
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
        /// <param name="inventoryData">The inventory where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slot">The item slot to be processed</param>
        /// <param name="curItem">The slot that was put down</param>
        /// <param name="curSlot">The item slot being put down</param>
        /// <param name="changedSlots">Record changes</param>
        /// <returns>Whether to fully merge</returns>
        private static bool TryMergeSlot(InventoryData inventoryData, ItemStack item, int slot, ItemStack curItem, int curSlot, List<Tuple<short, ItemStack?>> changedSlots)
        {
            int spaceLeft = curItem.ItemType.StackLimit - curItem.Count;
            if (curItem.ItemType == item.ItemType && spaceLeft > 0)
            {
                // Put item on that stack
                if (item.Count <= spaceLeft)
                {
                    // Can fit into the stack
                    item.Count = 0;
                    curItem.Count += item.Count;

                    changedSlots.Add(new Tuple<short, ItemStack?>((short)curSlot, curItem));
                    changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, null));

                    inventoryData.Items.Remove(slot);
                    return true;
                }

                item.Count -= spaceLeft;
                curItem.Count += spaceLeft;

                changedSlots.Add(new Tuple<short, ItemStack?>((short)curSlot, curItem));
            }
            return false;
        }

        /// <summary>
        /// Store items in a new slot
        /// </summary>
        /// <param name="inventoryData">The inventory where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slot">The item slot to be processed</param>
        /// <param name="newSlot">New item slot</param>
        /// <param name="changedSlots">Record changes</param>
        private static void StoreInNewSlot(InventoryData inventoryData, ItemStack item, int slot, int newSlot, List<Tuple<short, ItemStack?>> changedSlots)
        {
            ItemStack newItem = new(item.ItemType, item.Count, item.NBT);
            inventoryData.Items[newSlot] = newItem;
            inventoryData.Items.Remove(slot);

            changedSlots.Add(new Tuple<short, ItemStack?>((short)newSlot, newItem));
            changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, null));
        }

        private static readonly HashSet<ResourceLocation> BEACON_FUEL_ITEM_IDS = new()
        {
            new ResourceLocation("iron_ingot"), new ResourceLocation("gold_ingot"),
            new ResourceLocation("emerald"),    new ResourceLocation("diamond"),
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
        /// Click a slot in the specified inventory
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public override bool DoInventoryAction(int inventoryId, int slot, InventoryActionType action)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DoInventoryAction(inventoryId, slot, action));

            ItemStack? item = null;
            if (inventories.ContainsKey(inventoryId) && inventories[inventoryId].Items.ContainsKey(slot))
                item = inventories[inventoryId].Items[slot];

            List<Tuple<short, ItemStack?>> changedSlots = new(); // List<Slot Id, Changed Items>

            // Update our inventory base on action type
            InventoryData? inventory = GetInventory(inventoryId);
            InventoryData playerInventoryData = GetInventory(0)!;
            
            if (inventory != null)
            {
                InventoryType inventoryType = inventory.Type;
                
                switch (action)
                {
                    case InventoryActionType.LeftClick:
                        // Check if cursor have item (slot -1)
                        if (playerInventoryData.Items.ContainsKey(-1))
                        {
                            // When item on cursor and clicking crafting output slot, nothing will happen
                            if (inventoryType.GetInventorySlotType(slot) == InventorySlotType.Output)
                            {
                                break; // TODO: Check output stacking
                            }

                            // Check target slot also have item?
                            if (inventory.Items.ContainsKey(slot))
                            {
                                // Check if both item are the same?
                                if (inventory.Items[slot].ItemType == playerInventoryData.Items[-1].ItemType)
                                {
                                    int maxCount = inventory.Items[slot].ItemType.StackLimit;
                                    // Check item stacking
                                    if (inventory.Items[slot].Count + playerInventoryData.Items[-1].Count <= maxCount)
                                    {
                                        // Put cursor item to target
                                        inventory.Items[slot].Count += playerInventoryData.Items[-1].Count;
                                        playerInventoryData.Items.Remove(-1);
                                    }
                                    else
                                    {
                                        // Leave some item on cursor
                                        playerInventoryData.Items[-1].Count -= maxCount - inventory.Items[slot].Count;
                                        inventory.Items[slot].Count = maxCount;
                                    }
                                }
                                else
                                {
                                    // Swap two items TODO: Check if this slot accepts cursor item
                                    (inventory.Items[slot], playerInventoryData.Items[-1]) = (playerInventoryData.Items[-1], inventory.Items[slot]);
                                }
                            }
                            else
                            {
                                // Put cursor item to target TODO: Check if this slot accepts cursor item
                                inventory.Items[slot] = playerInventoryData.Items[-1];
                                playerInventoryData.Items.Remove(-1);
                            }

                            changedSlots.Add(inventory.Items.TryGetValue(slot, out var inventoryItem)
                                ? new Tuple<short, ItemStack?>((short)slot, inventoryItem)
                                : new Tuple<short, ItemStack?>((short)slot, null));
                        }
                        else
                        {
                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slot))
                            {
                                // When taking item from crafting output slot, server will update us
                                if (inventoryType.GetInventorySlotType(slot) == InventorySlotType.Output)
                                {
                                    break;
                                }

                                // Put target slot item to cursor
                                playerInventoryData.Items[-1] = inventory.Items[slot];
                                inventory.Items.Remove(slot);

                                changedSlots.Add(new Tuple<short, ItemStack?>((short)slot, null));
                            }
                        }
                        break;
                    case InventoryActionType.RightClick:
                        // Check if cursor have item (slot -1)
                        if (playerInventoryData.Items.ContainsKey(-1))
                        {
                            // When item on cursor and clicking crafting output slot, nothing will happen
                            if (inventoryType.GetInventorySlotType(slot) == InventorySlotType.Output)
                            {
                                break;
                            }

                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slot))
                            {
                                // Check if these 2 items are stackable
                                if (inventory.Items[slot].ItemType == playerInventoryData.Items[-1].ItemType &&
                                    inventory.Items[slot].Count < inventory.Items[slot].ItemType.StackLimit)
                                {
                                    // Drop 1 item count from cursor
                                    playerInventoryData.Items[-1].Count--;
                                    inventory.Items[slot].Count++;
                                }
                                else
                                {
                                    // Swap two items TODO: Check if this slot accepts cursor item
                                    (inventory.Items[slot], playerInventoryData.Items[-1]) = (playerInventoryData.Items[-1], inventory.Items[slot]);
                                }
                            }
                            else
                            {
                                // Drop 1 item count from cursor TODO: Check if this slot accepts cursor item
                                var itemTmp = playerInventoryData.Items[-1];
                                ItemStack itemClone = new(itemTmp.ItemType, 1, itemTmp.NBT);
                                inventory.Items[slot] = itemClone;
                                playerInventoryData.Items[-1].Count--;
                            }
                        }
                        else
                        {
                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slot))
                            {
                                if (inventoryType.GetInventorySlotType(slot) == InventorySlotType.Output)
                                {
                                    // no matter how many item in crafting output slot, only 1 will be taken out
                                    // Also server will update us
                                    break;
                                }
                                if (inventory.Items[slot].Count == 1)
                                {
                                    // Only 1 item count. Put it to cursor
                                    playerInventoryData.Items[-1] = inventory.Items[slot];
                                    inventory.Items.Remove(slot);
                                }
                                else
                                {
                                    // Take half of the item stack to cursor
                                    if (inventory.Items[slot].Count % 2 == 0)
                                    {
                                        // Can be evenly divided
                                        var itemTmp = inventory.Items[slot];
                                        playerInventoryData.Items[-1] = new ItemStack(itemTmp.ItemType, itemTmp.Count / 2, itemTmp.NBT);
                                        inventory.Items[slot].Count = itemTmp.Count / 2;
                                    }
                                    else
                                    {
                                        // Cannot be evenly divided. item count on cursor is always larger than item on inventory
                                        var itemTmp = inventory.Items[slot];
                                        playerInventoryData.Items[-1] = new ItemStack(itemTmp.ItemType, (itemTmp.Count + 1) / 2, itemTmp.NBT);
                                        inventory.Items[slot].Count = (itemTmp.Count - 1) / 2;
                                    }
                                }
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
                    default:
                        Debug.Log($"Inventory action not handled: {action}");
                        break;
                }
            }

            return handler!.SendInventoryAction(inventoryId, slot, action, item, changedSlots, inventories[inventoryId].StateId);
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
            return InvokeOnNetMainThread(() => handler!.SendCreativeInventoryAction(slot, itemType, count, nbt));
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="playerAnimation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public override bool DoAnimation(int playerAnimation)
        {
            return InvokeOnNetMainThread(() => handler!.SendAnimation(playerAnimation, clientEntity.Id));
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
            return InvokeOnNetMainThread(() => handler!.SendPlayerBlockPlacement((int)hand, blockLoc, x, y, z, blockFace, sequenceId++));
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
                    EventManager.Instance.Broadcast(new SlotUpdateEvent(0, invSlot, updatedItem));
                    EventManager.Instance.Broadcast(new HotbarUpdateEvent(CurrentSlot, updatedItem));
                    EventManager.Instance.Broadcast(new HeldItemChangeEvent(
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
        /// <param name="line4">text four</param>
        public bool UpdateSign(Location location, string line1, string line2, string line3, string line4)
        {
            // TODO Open sign editor first https://wiki.vg/Protocol#Open_Sign_Editor
            return InvokeOnNetMainThread(() => handler!.SendUpdateSign(location, line1, line2, line3, line4));
        }

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0</param>
        public bool SelectTrade(int selectedSlot)
        {
            return InvokeOnNetMainThread(() => handler!.SelectTrade(selectedSlot));
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
                    Loom.QueueOnMainThread(() => {
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
            inventories[inventoryId] = inventoryData;

            if (inventoryId != 0)
            {
                Debug.Log(Translations.Get("extra.inventory_open", inventoryId, inventoryData.Title));
                //Debug.Log(Translations.Get("extra.inventory_interact"));

                Loom.QueueOnMainThread(() => {
                    // Set inventory id before opening the screen
                    ScreenControl.SetScreenData<InventoryScreen>(screen =>
                    {
                        screen.SetActiveInventory(inventoryData);
                    });
                    ScreenControl.PushScreen<InventoryScreen>();
                });
            }
        }

        /// <summary>
        /// Called when an inventory is closed
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

            if (inventory.Properties.ContainsKey(propertyId))
                inventory.Properties.Remove(propertyId);

            inventory.Properties.Add(propertyId, propertyValue);

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
        }

        /// <summary>
        /// When received inventory items from server
        /// </summary>
        /// <param name="inventoryId">Inventory Id</param>
        /// <param name="itemList">Item list, key = slot Id, value = Item information</param>
        /// <param name="stateId">State Id of inventory</param>
        public void OnInventoryItems(byte inventoryId, Dictionary<int, ItemStack> itemList, int stateId)
        {
            if (inventories.TryGetValue(inventoryId, out var inventory))
            {
                inventory.Items = itemList;
                inventory.StateId = stateId;
                
                Loom.QueueOnMainThread(() => {
                    foreach (var (slot, itemStack) in itemList)
                    {
                        EventManager.Instance.Broadcast(new SlotUpdateEvent(inventoryId, slot, itemStack));

                        Debug.Log($"Set inventory item: [{inventoryId}]/[{slot}] to {itemStack?.ItemType.ItemId.ToString() ?? "AIR"}");

                        if (inventory.IsHotbar(slot, out int hotbarSlot))
                        {
                            EventManager.Instance.Broadcast(new HotbarUpdateEvent(hotbarSlot, itemStack));
                            if (hotbarSlot == CurrentSlot) // Updating held item
                            {
                                EventManager.Instance.Broadcast(new HeldItemChangeEvent(
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
        public void OnInventorySlot(byte inventoryId, short slot, ItemStack? item, int stateId)
        {
            if (inventories.TryGetValue(inventoryId, out var inventory))
                inventory.StateId = stateId;

            // Handle inventoryId -2 - Add item to player inventory without animation
            if (inventoryId == 254)
                inventoryId = 0;
            // Handle cursor item
            if (inventoryId == 255 && slot == -1)
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
                if (inventories.TryGetValue(inventoryId, out var inventory2))
                {
                    if (item == null || item.IsEmpty)
                    {
                        if (inventory2.Items.ContainsKey(slot))
                            inventory2.Items.Remove(slot);
                    }
                    else inventory2.Items[slot] = item;

                    Loom.QueueOnMainThread(() => {
                        EventManager.Instance.Broadcast(new SlotUpdateEvent(inventoryId, slot, item));

                        //Debug.Log($"Set inventory item: [{inventoryId}]/[{slot}] to {item?.ItemType.ItemId.ToString() ?? "AIR"}");

                        if (inventory2.IsHotbar(slot, out int hotbarSlot))
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
            //string? username = null;

            lock (onlinePlayers)
            {
                if (onlinePlayers.ContainsKey(playerUUID))
                {
                    //username = onlinePlayers[playerUUID].Name;
                    onlinePlayers.Remove(playerUUID);
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
        public void OnSpawnEntity(EntityData entity)
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
            Loom.QueueOnMainThread(() => {
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

        /// <summary>
        /// Called when an entity's head yaw changed.
        /// </summary>
        /// <param name="entityId">Entity Id</param>
        /// <param name="vX">New x velocity</param>
        /// <param name="vY">New y velocity</param>
        /// <param name="vZ">New z velocity</param>
        public void OnEntityVelocity(int entityId, short vX, short vY, short vZ)
        {
            Loom.QueueOnMainThread(() => {
                EntityRenderManager.SetEntityRenderVelocity(entityId, new(vX / 8000F, vY / 8000F, vZ / 8000F));
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
            Loom.QueueOnMainThread(() => {
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
            Loom.QueueOnMainThread(() => {
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
