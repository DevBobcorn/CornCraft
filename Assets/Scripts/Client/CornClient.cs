#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using UnityEngine;

using MinecraftClient.Control;
using MinecraftClient.Event;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using MinecraftClient.Rendering;
using MinecraftClient.UI;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;

namespace MinecraftClient
{
    public class CornClient : MonoBehaviour, IMinecraftComHandler
    {
        #region Inspector Fields
        [SerializeField] public ChunkRenderManager? ChunkRenderManager;
        [SerializeField] public EntityRenderManager? EntityRenderManager;
        [SerializeField] public BaseEnvironmentManager? EnvironmentManager;
        [SerializeField] public MaterialManager? MaterialManager;
        [SerializeField] public GameObject? PlayerPrefab;
        [SerializeField] public CameraController? CameraController;
        [SerializeField] public ScreenControl? ScreenControl;
        [SerializeField] public HUDScreen? HUDScreen;

        #endregion

        #region Login Information
        private string? host;
        private int port;
        private int protocolVersion;
        private string? username;
        private string? uuidStr;
        private Guid uuid;
        private string? sessionId;
        private PlayerKeyPair? playerKeyPair;
        public string GetServerHost() => host!;
        public int GetServerPort() => port;
        public string GetUsername() => username!;
        public Guid GetUserUUID() => uuid;
        public string GetUserUUIDStr() => uuidStr!;
        public string GetSessionID() => sessionId!;
        #endregion

        #region Client Control
        private Queue<Action> threadTasks = new();
        private object threadTasksLock = new();

        private Queue<string> chatQueue = new();
        private DateTime nextMessageSendTime = DateTime.MinValue;

        #endregion

        #region Time and Networking
        private DateTime lastKeepAlive;
        private object lastKeepAliveLock = new();
        private long lastAge = 0L;
        private DateTime lastTime;
        private double serverTPS = 0;
        private double averageTPS = 20;
        private const int maxSamples = 5;
        private List<double> tpsSamples = new(maxSamples);
        private double sampleSum = 0;
        public double GetServerTPS() => averageTPS;
        public float GetTickMilSec() => (float)(1D / averageTPS);
        
        TcpClient? tcpClient;
        IMinecraftCom? handler;

        Tuple<Thread, CancellationTokenSource>? timeoutdetector = null;
        #endregion

        #region Environment
        private bool worldAndMovementsRequested = false;
        private readonly World world = new();
        public World GetWorld() => world;
        public bool IsMovementReady()
        {
            if (!locationReceived || ChunkRenderManager is null)
                return false;
            
            lock (movementLock)
            {
                var loc = clientEntity.Location;
            
                return ChunkRenderManager.IsChunkRenderColumnReady(loc.GetChunkX(), loc.GetChunkZ());
            }
        }

        #endregion

        #region Players and Entities
        private bool locationReceived = false;
        public bool LocationReceived => locationReceived;
        public Perspective Perspective = 0;
        private GameMode gameMode = GameMode.Survival;
        public GameMode GameMode => gameMode;
        private readonly Entity clientEntity = new(0, EntityType.DUMMY_ENTITY_TYPE, Location.Zero);
        public float? YawToSend = null, PitchToSend = null;
        public bool Grounded = false;
        private int clientSequenceId;
        private int foodSaturation, level, totalExperience;
        private Dictionary<int, Container> inventories = new();
        public byte currentSlot = 0;

        public Container? GetInventory(int inventoryID)
        {
            if (inventories.ContainsKey(inventoryID))
                return inventories[inventoryID];
            return null;
        }

        private object movementLock = new();
        public Location GetLocation() => clientEntity.Location;
        public Vector3 GetPosition() => CoordConvert.MC2Unity(clientEntity.Location);
        
        public void UpdatePlayerStatus(Vector3 newPosition, float newYaw, float newPitch, bool newGrounded)
        {
            lock (movementLock)
            {
                // Update player location
                clientEntity.Location = CoordConvert.Unity2MC(newPosition);

                // Update player yaw and pitch
                
                if (clientEntity.Yaw != newYaw || clientEntity.Pitch != newPitch)
                {
                    YawToSend = newYaw;
                    clientEntity.Yaw = newYaw;
                    PitchToSend = newPitch;
                    clientEntity.Pitch = newPitch;
                }
                
                Grounded = newGrounded;
            }
        }

        public Vector3? GetAttackTarget()
        {
            var nearbyEntities = EntityRenderManager!.GetNearbyEntities();
            Vector3? targetPos = null;

            if (nearbyEntities is null || nearbyEntities.Count == 0) // Nothing to do
                return null;
            
            float minDist = float.MaxValue;

            foreach (var pair in nearbyEntities)
            {
                if (pair.Value < minDist)
                {
                    var render = EntityRenderManager.GetEntityRender(pair.Key);

                    if (render!.Entity.Type.ContainsItem) // Not a valid target
                        continue;

                    var pos = render.transform.position;
                    
                    if (pair.Value <= 16F && pos.y - transform.position.y < 2F)
                        targetPos = pos;
                }
            }

            return targetPos;
        }

        private PlayerController? playerController;
        private InteractionUpdater? interactionUpdater;
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();
        private Dictionary<int, Entity> entities = new();
        #endregion

        void Awake() // In case where the client wasn't properly assigned before
        {
            if (CornApp.CurrentClient is null)
                CornApp.SetCurrentClient(this);
        }

        void Start()
        {
            // Push HUD Screen on start
            ScreenControl!.PushScreen(HUDScreen!);

            // Setup chunk render manager
            ChunkRenderManager!.SetClient(this);

            // Create player entity
            var playerObj = GameObject.Instantiate(PlayerPrefab);
            playerController = playerObj!.GetComponent<PlayerController>();

            // Update entity type for dummy client entity
            clientEntity.Type = EntityPalette.INSTANCE.FromId(EntityType.PLAYER_ID);
            playerController.Initialize(this, clientEntity, CameraController!);

            // Set up camera controller
            CameraController!.SetClient(this);
            CameraController.SetTarget(playerController.cameraRef!);

            // Set up interaction updater
            interactionUpdater = GetComponent<InteractionUpdater>();
            interactionUpdater!.Initialize(this, CameraController);
        }

        public bool IsPaused() => ScreenControl!.IsPaused;

        public bool MouseScrollAbsorbed() => ScreenControl!.GetTopScreen().AbsorbMouseScroll();

        public void SwitchPerspective()
        {
            // Switch to next perspective
            var newPersp = Perspective switch
            {
                Perspective.FirstPerson    => Perspective.ThirdPerson,
                Perspective.ThirdPerson    => Perspective.FirstPerson,

                _                          => Perspective.ThirdPerson
            };
            
            CameraController?.SetPerspective(newPersp);
        }

        public string GetInfoString(bool withDebugInfo)
        {
            string baseString = $"FPS: {((int)(1F / Time.deltaTime)).ToString().PadLeft(4, ' ')}\n{GameMode}\nTime: {EnvironmentManager!.GetTimeString()}";

            if (withDebugInfo)
            {
                var targetLoc = interactionUpdater?.TargetLocation;
                var loc = GetLocation();

                string targetInfo;

                if (targetLoc is not null)
                {
                    var targetBlock = world?.GetBlock(targetLoc.Value);
                    if (targetBlock is not null)
                        targetInfo = $"Target: {targetLoc}\n{targetBlock}";
                    else
                        targetInfo = $"Target: {targetLoc}\n";
                }
                else
                {
                    targetInfo = "\n";
                }

                var worldInfo = $"\nLoc: {loc}\nLighting:\nsky {world?.GetSkyLight(loc)} block {world?.GetBlockLight(loc)}\nBiome:\n[{world?.GetBiomeId(loc)}] {world?.GetBiome(loc)}\n{targetInfo}";
                
                return baseString + $"{worldInfo}\n{playerController?.GetDebugInfo()}\n{ChunkRenderManager!.GetDebugInfo()}\n{EntityRenderManager!.GetDebugInfo()}\nSvr TPS: {GetServerTPS():00.00}";
            }
            
            return baseString;
        }

        public bool StartClient(SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp, ushort port,
                int protocol, ForgeInfo? forgeInfo, Action<string> updateStatus, string accountLower)
        {
            this.sessionId = session.ID;
            if (!Guid.TryParse(session.PlayerID, out this.uuid))
                this.uuid = Guid.Empty;
            this.uuidStr = session.PlayerID;
            this.username = session.PlayerName;
            this.host = serverIp;
            this.port = port;
            this.protocolVersion = protocol;
            this.playerKeyPair = playerKeyPair;

            clientEntity.Name = session.PlayerName;
            clientEntity.MaxHealth = 20F;

            // Start up client
            try
            {
                // Setup tcp client
                tcpClient = ProxyHandler.newTcpClient(host, port);
                tcpClient.ReceiveBufferSize = 1024 * 1024;
                tcpClient.ReceiveTimeout = 30000; // 30 seconds

                // Create handler
                handler = Protocol.ProtocolHandler.GetProtocolHandler(tcpClient, protocol, forgeInfo, this);

                // Start update loop
                timeoutdetector = Tuple.Create(new Thread(new ParameterizedThreadStart(TimeoutDetector)), new CancellationTokenSource());
                timeoutdetector.Item1.Name = "Connection Timeout Detector";
                timeoutdetector.Item1.Start(timeoutdetector.Item2.Token);

                if (handler.Login(this.playerKeyPair, session, accountLower)) // Login
                    return true; // Client successfully started
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
                Disconnect();
            }

            return false; // Failed to start client
        }

        /// <summary>
        /// Retrieve messages from the queue and send.
        /// Note: requires external locking.
        /// </summary>
        private void TrySendMessageToServer()
        {
            while (chatQueue.Count > 0 && nextMessageSendTime < DateTime.Now)
            {
                string text = chatQueue.Dequeue();
                handler!.SendChatMessage(text, playerKeyPair);
                nextMessageSendTime = DateTime.Now + TimeSpan.FromSeconds(CornGlobal.MessageCooldown);
            }
        }

        /// <summary>
        /// Called ~20 times per second by the protocol handler (on net read thread)
        /// </summary>
        public void OnUpdate()
        {
            lock (chatQueue)
            {
                TrySendMessageToServer();
            }

            if (locationReceived)
            {
                lock (movementLock)
                {
                    handler!.SendLocationUpdate(clientEntity.Location, Grounded, YawToSend, PitchToSend);

                    // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
                    // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
                    YawToSend = PitchToSend = null;
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

        }

        #region Disconnect logic

        /// <summary>
        /// Periodically checks for server keepalives and consider that connection has been lost if the last received keepalive is too old.
        /// </summary>
        private void TimeoutDetector(object? o)
        {
            Debug.Log("Timeout Detector start");
            UpdateKeepAlive();
            do
            {
                bool end = false;
                for (int i = 0;i < 30;i++) // 15 seconds in total
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    end = ((CancellationToken)o!).IsCancellationRequested;

                    if (end) break;
                }

                if (end) break;

                if (((CancellationToken)o!).IsCancellationRequested)
                    return;
                
                lock (lastKeepAliveLock)
                {
                    if (lastKeepAlive.AddSeconds(30) < DateTime.Now)
                    {
                        if (((CancellationToken)o!).IsCancellationRequested)
                            return;
                        
                        OnConnectionLost(DisconnectReason.ConnectionLost, Translations.Get("error.timeout"));
                        return;
                    }
                }
            }
            while (!((CancellationToken)o!).IsCancellationRequested);
            Debug.Log("Timeout Detector stop");
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
        public void Disconnect()
        {
            handler?.Disconnect();
            handler?.Dispose();
            handler = null;

            timeoutdetector?.Item2.Cancel();
            timeoutdetector = null;

            tcpClient?.Close();
            tcpClient = null;
            
            CornApp.Instance.BackToLogin();
        }

        /// <summary>
        /// When connection has been lost, login was denied or player was kicked from the server
        /// </summary>
        public void OnConnectionLost(DisconnectReason reason, string message)
        {
            // Clear world data
            world.Clear();

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

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// </summary>
        public void TrySendChat(string text)
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
        /// Allows the user to send requests to complete current command
        /// </summary>
        public void AutoComplete(string text)
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
                TaskWithResult<T> taskWithResult = new TaskWithResult<T>(task);
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
        /// <returns></returns>
        public static char[] GetDisallowedChatCharacters()
        {
            return new char[] { (char)167, (char)127 }; // Minecraft color code and ASCII code DEL
        }

        /// <summary>
        /// Get all Entities
        /// </summary>
        /// <returns>All Entities</returns>
        public Dictionary<int, Entity> GetEntities() => entities;

        /// <summary>
        /// Get all players latency
        /// </summary>
        /// <returns>All players latency</returns>
        public Dictionary<string, int> GetPlayersLatency()
        {
            Dictionary<string, int> playersLatency = new();
            foreach (var player in onlinePlayers)
                playersLatency.Add(player.Value.Name, player.Value.Ping);
            return playersLatency;
        }

        public int GetOwnLatency() => onlinePlayers.ContainsKey(uuid) ? onlinePlayers[uuid].Ping : 0;

        /// <summary>
        /// Get player info from uuid
        /// </summary>
        /// <param name="uuid">Player's UUID</param>
        /// <returns>Player info</returns>
        public PlayerInfo? GetPlayerInfo(Guid uuid)
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
        public string[] GetOnlinePlayers()
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
        public Dictionary<string, string> GetOnlinePlayersWithUUID()
        {
            Dictionary<string, string> uuid2Player = new Dictionary<string, string>();
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

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        private void SendChat(string text)
        {
            if (String.IsNullOrEmpty(text))
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
        public bool SendRespawnPacket()
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread<bool>(SendRespawnPacket);

            return handler!.SendRespawnPacket();
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified ID
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool SendEntityAction(EntityActionType entityAction)
        {
            return InvokeOnNetMainThread(() => handler!.SendEntityAction(clientEntity.ID, (int)entityAction));
        }

        /// <summary>
        /// Use the item currently in the player's hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool UseItemOnHand()
        {
            return InvokeOnNetMainThread(() => handler!.SendUseItem(0, clientSequenceId));
        }

        /// <summary>
        /// Store items in a new slot
        /// </summary>
        /// <param name="inventory">The container where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slotId">The ID of the slot of the item to be processed</param>
        /// <param name="newSlotId">ID of the new slot</param>
        /// <param name="changedSlots">Record changes</param>
        private static void StoreInNewSlot(Container inventory, ItemStack item, int slotId, int newSlotId, List<Tuple<short, ItemStack?>> changedSlots)
        {
            ItemStack newItem = new(item.Type, item.Count, item.NBT);
            inventory.Items[newSlotId] = newItem;
            inventory.Items.Remove(slotId);

            changedSlots.Add(new Tuple<short, ItemStack?>((short)newSlotId, newItem));
            changedSlots.Add(new Tuple<short, ItemStack?>((short)slotId, null));
        }

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DoWindowAction(windowId, slotId, action));

            // TODO Implement

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
            return InvokeOnNetMainThread(() => handler!.SendAnimation(animation, clientEntity.ID));
        }

        /// <summary>
        /// Close the specified inventory window
        /// </summary>
        /// <param name="windowId">Window ID</param>
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
        /// <param name="EntityID"></param>
        /// <param name="type">0: interact, 1: attack, 2: interact at</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if interaction succeeded</returns>
        public bool InteractEntity(int entityID, int type, Hand hand = Hand.MainHand)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => InteractEntity(entityID, type, hand));

            if (entities.ContainsKey(entityID))
            {
                if (type == 0)
                    return handler!.SendInteractEntity(entityID, type, (int)hand);
                else
                    return handler!.SendInteractEntity(entityID, type);
            }
            else return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="location">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <returns>TRUE if successfully placed</returns>
        public bool PlaceBlock(Location location, Direction blockFace, Hand hand = Hand.MainHand)
        {
            return InvokeOnNetMainThread(() => handler!.SendPlayerBlockPlacement((int)hand, location, blockFace, clientSequenceId));
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        public bool DigBlock(Location location, bool swingArms = true, bool lookAtBlock = true)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DigBlock(location, swingArms, lookAtBlock));

            // TODO select best face from current player location
            Direction blockFace = Direction.Down;

            // Look at block before attempting to break it
            if (lookAtBlock)
                UpdateLocation(GetLocation(), location);

            // Send dig start and dig end, will need to wait for server response to know dig result
            // See https://wiki.vg/How_to_Write_a_Client#Digging for more details
            return handler!.SendPlayerDigging(0, location, blockFace, clientSequenceId)
                && (!swingArms || DoAnimation((int)Hand.MainHand))
                && handler.SendPlayerDigging(2, location, blockFace, clientSequenceId);
        }

        /// <summary>
        /// Change active slot in the player inventory
        /// </summary>
        /// <param name="slot">Slot to activate (0 to 8)</param>
        /// <returns>TRUE if the slot was changed</returns>
        public bool ChangeSlot(short slot)
        {
            if (slot < 0 || slot > 8)
                return false;

            if (InvokeRequired)
                return InvokeOnNetMainThread(() => ChangeSlot(slot));

            currentSlot = Convert.ToByte(slot);
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
            if (entity.Type.EntityId == EntityType.PLAYER_ID)
                return SpectateByUUID(entity.UUID);
            return false;
        }

        /// <summary>
        /// Teleport to player/entity in spectator mode
        /// </summary>
        /// <param name="UUID">UUID of player/entity to teleport to</param>
        public bool SpectateByUUID(Guid UUID)
        {
            if(gameMode == GameMode.Spectator)
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
        /// Only called if <see cref="networkPacketEventEnabled"/> is set to True
        /// </remarks>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">A copy of Packet Data</param>
        /// <param name="isLogin">The packet is login phase or playing phase</param>
        /// <param name="isInbound">The packet is received from server or sent by client</param>
        public void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound) { }

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
        public void OnGameJoined()
        {
            handler!.SendBrandInfo(CornGlobal.BrandInfo.Trim());

            if (CornGlobal.MCSettings.Enabled)
                handler.SendClientSettings(
                    CornGlobal.MCSettings.Locale,
                    CornGlobal.MCSettings.RenderDistance,
                    CornGlobal.MCSettings.Difficulty,
                    CornGlobal.MCSettings.ChatMode,
                    CornGlobal.MCSettings.ChatColors,
                    CornGlobal.MCSettings.Skin_All,
                    CornGlobal.MCSettings.MainHand);

            ClearInventories();
        }

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        public void OnRespawn()
        {
            ClearTasks();

            if (worldAndMovementsRequested)
            {
                worldAndMovementsRequested = false;
                Debug.Log(Translations.Get("extra.terrainandmovement_enabled"));
            }

            world.Clear();
            
            entities.Clear();
            ClearInventories();

            Loom.QueueOnMainThread(() => {
                ChunkRenderManager?.ReloadWorld();
                EntityRenderManager?.ReloadEntityRenders();
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
                YawToSend = clientEntity.Yaw = yaw;
                PitchToSend = clientEntity.Pitch = pitch;

                clientEntity.Location = location;
                
                if (!locationReceived)
                    Loom.QueueOnMainThread(() => {
                        // Force refresh environment collider
                        ChunkRenderManager?.RebuildTerrainCollider(location.ToFloor());

                        // Then update player location
                        playerController?.SetLocation(location, yaw: yaw);
                    });

                locationReceived = true;
                
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
                if (!CornGlobal.ShowIllegalSignedChat && !message.isSystemChat && !(bool)message.isSignatureLegal!)
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

            Loom.QueueOnMainThread(
                () => EventManager.Instance.Broadcast<ChatMessageEvent>(new(messageText))
            );

        }

        /// <summary>
        /// Received a connection keep-alive from the server
        /// </summary>
        public void OnServerKeepAlive()
        {
            UpdateKeepAlive();
        }

        /// <summary>
        /// When an inventory is opened
        /// </summary>
        /// <param name="inventory">The inventory</param>
        /// <param name="inventoryID">Inventory ID</param>
        public void OnInventoryOpen(int inventoryID, Container inventory)
        {
            inventories[inventoryID] = inventory;

            if (inventoryID != 0)
            {
                Debug.Log(Translations.Get("extra.inventory_open", inventoryID, inventory.Title));
                Debug.Log(Translations.Get("extra.inventory_interact"));
            }
        }

        /// <summary>
        /// When an inventory is close
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        public void OnInventoryClose(int inventoryID)
        {
            if (inventories.ContainsKey(inventoryID))
            {
                if (inventoryID == 0)
                    inventories[0].Items.Clear(); // Don't delete player inventory
                else
                    inventories.Remove(inventoryID);
            }

            if (inventoryID != 0)
            {
                Debug.Log(Translations.Get("extra.inventory_close", inventoryID));
            }
        }

        /// <summary>
        /// When received window items from server.
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list, key = slot ID, value = Item information</param>
        public void OnWindowItems(byte inventoryID, Dictionary<int, Inventory.ItemStack> itemList, int stateId)
        {
            if (inventories.ContainsKey(inventoryID))
            {
                inventories[inventoryID].Items = itemList;
                inventories[inventoryID].StateID = stateId;
            }
        }

        /// <summary>
        /// When a slot is set inside window items
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        public void OnSetSlot(byte inventoryID, short slotID, ItemStack item, int stateId)
        {
            if (inventories.ContainsKey(inventoryID))
                inventories[inventoryID].StateID = stateId;

            // Handle inventoryID -2 - Add item to player inventory without animation
            if (inventoryID == 254)
                inventoryID = 0;
            // Handle cursor item
            if (inventoryID == 255 && slotID == -1)
            {
                inventoryID = 0; // Prevent key not found for some bots relied to this event
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
                    if (item == null || item.IsEmpty)
                    {
                        if (inventories[inventoryID].Items.ContainsKey(slotID))
                            inventories[inventoryID].Items.Remove(slotID);
                    }
                    else inventories[inventoryID].Items[slotID] = item;
                }
            }
        }

        /// <summary>
        /// Set client player's ID for later receiving player's own properties
        /// </summary>
        /// <param name="EntityID">Player Entity ID</param>
        public void OnReceivePlayerEntityID(int EntityID)
        {
            clientEntity.ID = EntityID;
            
        }

        /// <summary>
        /// Triggered when a new player joins the game
        /// </summary>
        /// <param name="player">player info</param>
        public void OnPlayerJoin(PlayerInfo player)
        {
            //Ignore placeholders eg 0000tab# from TabListPlus
            if (!StringHelper.IsValidName(player.Name))
                return;

            if (player.Name == username)
            {
                // 1.19+ offline server is possible to return different uuid
                this.uuidStr = player.UUID.ToString().Replace("-", string.Empty);
                this.uuid = player.UUID;
            }

            lock (onlinePlayers)
            {
                onlinePlayers[player.UUID] = player;
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
        /// Called when an entity spawned
        /// </summary>
        public void OnSpawnEntity(Entity entity)
        {
            // The entity should not already exist, but if it does, let's consider the previous one is being destroyed
            if (entities.ContainsKey(entity.ID))
                OnDestroyEntities(new[] { entity.ID });

            entities.Add(entity.ID, entity);

            Loom.QueueOnMainThread(() => {
                EntityRenderManager?.AddEntityRender(entity);
            });

        }

        /// <summary>
        /// Called when an entity effects
        /// </summary>
        public void OnEntityEffect(int entityid, Effects effect, int amplifier, int duration, byte flags, bool hasFactorData, Dictionary<string, object>? factorCodec) { }

        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        public void OnSpawnPlayer(int entityID, Guid uuid, Location location, byte Yaw, byte Pitch)
        {
            string? playerName = null;
            if (onlinePlayers.ContainsKey(uuid))
                playerName = onlinePlayers[uuid].Name;
            Entity playerEntity = new Entity(entityID, EntityPalette.INSTANCE.FromId(EntityType.PLAYER_ID), location, uuid, playerName);
            OnSpawnEntity(playerEntity);
        }

        /// <summary>
        /// Called on Entity Equipment
        /// </summary>
        /// <param name="entityid"> Entity ID</param>
        /// <param name="slot"> Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item"> Item)</param>
        public void OnEntityEquipment(int entityid, int slot, ItemStack item)
        {
            if (entities.ContainsKey(entityid))
            {
                Entity entity = entities[entityid];
                if (entity.Equipment.ContainsKey(slot))
                    entity.Equipment.Remove(slot);
                if (item != null)
                    entity.Equipment[slot] = item;
            }
        }

        /// <summary>
        /// Called when the Game Mode has been updated for a player
        /// </summary>
        /// <param name="playername">Player Name</param>
        /// <param name="uuid">Player UUID (Empty for initial gamemode on login)</param>
        /// <param name="gamemode">New Game Mode (0: Survival, 1: Creative, 2: Adventure, 3: Spectator).</param>
        public void OnGamemodeUpdate(Guid uuid, int gamemode)
        {
            
            if (uuid == Guid.Empty) // Initial gamemode on login
            {
                Loom.QueueOnMainThread(() =>{
                    this.gameMode = (GameMode) gamemode;
                    EventManager.Instance.Broadcast<GameModeUpdateEvent>(new(this.gameMode));
                });
            }
            else if (onlinePlayers.ContainsKey(uuid)) // Further regular gamemode change events
            {
                string playerName = onlinePlayers[uuid].Name;
                if (playerName == this.username)
                {
                    Loom.QueueOnMainThread(() =>{
                        this.gameMode = (GameMode) gamemode;
                        EventManager.Instance.Broadcast<GameModeUpdateEvent>(new(this.gameMode));

                        CornApp.Notify($"Gamemode updated to {this.gameMode}", Notification.Type.Success);
                    });
                }
            }
        }

        /// <summary>
        /// Called when entities dead/despawn.
        /// </summary>
        public void OnDestroyEntities(int[] Entities)
        {
            foreach (int a in Entities)
            {
                if (entities.ContainsKey(a))
                    entities.Remove(a);
                
            }

            Loom.QueueOnMainThread(() => {
                EntityRenderManager?.RemoveEntityRenders(Entities);
            });
        }

        /// <summary>
        /// Called when an entity's position changed within 8 block of its previous position.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="Dx"></param>
        /// <param name="Dy"></param>
        /// <param name="Dz"></param>
        /// <param name="onGround"></param>
        public void OnEntityPosition(int EntityID, Double Dx, Double Dy, Double Dz, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                Location location = entities[EntityID].Location;
                location.X += Dx;
                location.Y += Dy;
                location.Z += Dz;
                entities[EntityID].Location = location;

                Loom.QueueOnMainThread(() => {
                    EntityRenderManager?.MoveEntityRender(EntityID, location);
                });
            }

        }

        public void OnEntityRotation(int EntityID, byte yaw, byte pitch, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                var renderYaw = entities[EntityID].SetYawFromByte(yaw);
                var renderPitch = entities[EntityID].SetPitchFromByte(pitch);

                Loom.QueueOnMainThread(() => {
                    EntityRenderManager?.RotateEntityRender(EntityID, renderYaw, renderPitch);
                });
            }

        }

        public void OnEntityHeadLook(int EntityID, byte headYaw)
        {
            if (entities.ContainsKey(EntityID))
            {
                var renderHeadYaw = entities[EntityID].SetHeadYawFromByte(headYaw);

                Loom.QueueOnMainThread(() => {
                    EntityRenderManager?.RotateEntityRenderHead(EntityID, renderHeadYaw);
                });
            }

        }

        /// <summary>
        /// Called when an entity moved over 8 block.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <param name="onGround"></param>
        public void OnEntityTeleport(int EntityID, Double X, Double Y, Double Z, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                Location location = new Location(X, Y, Z);
                entities[EntityID].Location = location;

                Loom.QueueOnMainThread(() => {
                    EntityRenderManager?.MoveEntityRender(EntityID, location);
                });
            }
        }

        /// <summary>
        /// Called when received entity properties from server.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="prop"></param>
        public void OnEntityProperties(int EntityID, Dictionary<string, Double> prop) { }

        /// <summary>
        /// Called when the status of an entity have been changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="status">Status ID</param>
        public void OnEntityStatus(int entityID, byte status) { }

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
                EnvironmentManager!.SetTime(timeOfDay);
            });

            // calculate server tps
            if (lastAge != 0)
            {
                DateTime currentTime = DateTime.Now;
                long tickDiff = worldAge - lastAge;
                Double tps = tickDiff / (currentTime - lastTime).TotalSeconds;
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

            Loom.QueueOnMainThread(() => {
                EventManager.Instance.Broadcast<HealthUpdateEvent>(new(health, updateMaxHealth));
            });
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
                string playerName = player.Name;
                foreach (KeyValuePair<int, Entity> ent in entities)
                {
                    if (ent.Value.UUID == uuid && ent.Value.Name == playerName)
                    {
                        ent.Value.Latency = latency;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Called when held item change
        /// </summary>
        /// <param name="slot"> item slot</param>
        public void OnHeldItemChange(byte slot) => currentSlot = slot;

        /// Called when an update of the map is sent by the server, take a look at https://wiki.vg/Protocol#Map_Data for more info on the fields
        /// Map format and colors: https://minecraft.fandom.com/wiki/Map_item_format
        /// </summary>
        /// <param name="mapId">Map ID of the map being modified</param>
        /// <param name="scale">A scale of the Map, from 0 for a fully zoomed-in map (1 block per pixel) to 4 for a fully zoomed-out map (16 blocks per pixel)</param>
        /// <param name="trackingposition">Specifies whether player and item frame icons are shown </param>
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
        /// <param name="titletext"> title text</param>
        /// <param name="subtitletext"> suntitle text</param>
        /// <param name="actionbartext"> action bar text</param>
        /// <param name="fadein"> Fade In</param>
        /// <param name="stay"> Stay</param>
        /// <param name="fadeout"> Fade Out</param>
        /// <param name="json"> json text</param>
        public void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json) { }

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
        /// <param name="entityname">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectivename">The name of the objective the score belongs to</param>
        /// <param name="value">he score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        public void OnUpdateScore(string entityname, int action, string objectivename, int value) { }

        /// <summary>
        /// Called when the health of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="health">The health of the entity</param>
        public void OnEntityHealth(int entityID, float health)
        {
            if (entities.ContainsKey(entityID))
            {
                Entity entity = entities[entityID];

                entity.Health = health;
                entity.MaxHealth = Math.Max(entity.MaxHealth, health);
            }
            
        }

        /// <summary>
        /// Called when the metadata of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="metadata">The metadata of the entity</param>
        public void OnEntityMetadata(int entityID, Dictionary<int, object?> metadata)
        {
            if (entities.ContainsKey(entityID))
            {
                Entity entity = entities[entityID];
                entity.Metadata = metadata;
                if (entity.Type.ContainsItem && metadata.TryGetValue(7, out object? itemObj) && itemObj != null && itemObj.GetType() == typeof(ItemStack))
                {
                    var item = (ItemStack?) itemObj;
                    entity.Item = item;
                }
                if (metadata.TryGetValue(6, out object? poseObj) && poseObj != null && poseObj.GetType() == typeof(Int32))
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
            }
        }

        /// <summary>
        /// Called when tradeList is recieved after interacting with villager
        /// </summary>
        /// <param name="windowID">Window ID</param>
        /// <param name="trades">List of trades.</param>
        /// <param name="villagerInfo">Contains Level, Experience, IsRegularVillager and CanRestock .</param>
        public void OnTradeList(int windowID, List<VillagerTrade> trades, VillagerInfo villagerInfo) { }

        /// <summary>
        /// Called every player break block in gamemode 0
        /// </summary>
        /// <param name="entityId">Player ID</param>
        /// <param name="location">Block location</param>
        /// <param name="stage">Destroy stage, maximum 255</param>
        public void OnBlockBreakAnimation(int entityId, Location location, byte stage)
        {
            if (entities.ContainsKey(entityId))
            {
                Entity entity = entities[entityId];
            }
        }

        /// <summary>
        /// Called every animations of the hit and place block
        /// </summary>
        /// <param name="entityID">Player ID</param>
        /// <param name="animation">0 = LMB, 1 = RMB (RMB Corrent not work)</param>
        public void OnEntityAnimation(int entityID, byte animation)
        {
            if (entities.ContainsKey(entityID))
            {
                Entity entity = entities[entityID];
            }
        }

        /// <summary>
        /// Called every time rain(snow) starts or stops
        /// </summary>
        /// <param name="begin">true if the rain is starting</param>
        public void OnRainChange(bool begin) { }

        /// <summary>
        /// Called when a Synchronization sequence is recevied, this sequence need to be sent when breaking or placing blocks
        /// </summary>
        /// <param name="sequenceId">Sequence ID</param>
        public void OnBlockChangeAck(int sequenceId) => clientSequenceId = sequenceId;

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
        /// <param name="userName">The player's username received from the server</param>
        /// <param name="playerProperty">Tuple<Name, Value, Signature(empty if there is no signature)></param>
        public void OnLoginSuccess(Guid uuid, string userName, Tuple<string, string, string>[]? playerProperty) { }

        #endregion

    }
}
