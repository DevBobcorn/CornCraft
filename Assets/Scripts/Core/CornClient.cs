using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using MinecraftClient.Commands;
using MinecraftClient.Control;
using MinecraftClient.Event;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Proxy;
using MinecraftClient.Rendering;
using MinecraftClient.Resource;
using MinecraftClient.UI;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockStatePalettes;
using MinecraftClient.Inventory;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MinecraftClient
{
    public class CornClient : MonoBehaviour, IMinecraftComHandler
    {
        private static readonly List<string> cmd_names = new();
        private static readonly Dictionary<string, Command> cmds = new();
        private static bool commandsLoaded = false;

        private Queue<Action> threadTasks = new();
        private object threadTasksLock = new();

        private Queue<string> chatQueue = new();
        private DateTime nextMessageSendTime = DateTime.MinValue;

        #region World Data and Player Movement
        private bool worldAndMovementsRequested = false;
        private World world = new();
        private Location location;
        private object locationLock = new();
        private bool locationReceived = false, localLocationUpdated = false;
        private float? _yaw; // Used for calculation ONLY!!! Doesn't reflect the client yaw
        private float? _pitch; // Used for calculation ONLY!!! Doesn't reflect the client pitch
        private float playerYaw;
        private float playerPitch;
        private double motionY;
        private CancellationTokenSource chunkProcessCancelSource = new();
        private int sequenceId; // User for player block synchronization (Aka. digging, placing blocks, etc..)
        public Location GetCurrentLocation() { return location; }

        public float GetYaw() { return playerYaw; }
        public float GetPitch() { return playerPitch; }
        public World GetWorld() { return world; }
        public CancellationToken GetChunkProcessCancelToken() { return chunkProcessCancelSource.Token; }
        #endregion

        #region Login Information
        private string host;
        private int port;
        private int protocolVersion;
        private string username;
        private string uuidStr;
        private Guid uuid;
        private string sessionId;
        private PlayerKeyPair playerKeyPair;
        private bool isSupportPreviewsChat;
        public string GetServerHost() { return host; }
        public int GetServerPort() { return port; }
        public int GetProtocolVersion() { return protocolVersion; }
        public string GetUsername() { return username; }
        public Guid GetUserUUID() { return uuid; }
        public string GetUserUUIDStr() { return uuidStr; }
        public string GetSessionID() { return sessionId; }
        #endregion

        #region Players and Entities
        private bool inventoryHandlingRequested = false;
        private int gamemode = 0;
        private int playerEntityID;
        private float playerHealth;
        private int playerFoodSaturation;
        private int playerLevel;
        private int playerTotalExperience;
        private Dictionary<int, Container> inventories = new();
        private byte CurrentSlot = 0;
        public float GetHealth() { return playerHealth; }
        public int GetSaturation() { return playerFoodSaturation; }
        public int GetLevel() { return playerLevel; }
        public int GetTotalExperience() { return playerTotalExperience; }
        public byte GetCurrentSlot() { return CurrentSlot; }
        public int GetGamemode() { return gamemode; }
        public int GetPlayerEntityID() { return playerEntityID; }
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();
        private Dictionary<int, Entity> entities = new();
        #endregion

        #region Server Updates
        private DateTime lastKeepAlive;
        private object lastKeepAliveLock = new();
        private long lastAge = 0;
        private DateTime lastTime;
        private double serverTPS = 0;
        private double averageTPS = 20;
        private const int maxSamples = 5;
        private List<double> tpsSamples = new(maxSamples);
        private double sampleSum = 0;
        public Double GetServerTPS() { return averageTPS; }
        public bool GetIsSupportPreviewsChat() { return isSupportPreviewsChat; }
        #endregion
        
        TcpClient tcpClient;
        IMinecraftCom handler;

        #nullable enable
        Tuple<Thread, CancellationTokenSource>? timeoutdetector = null;
        #nullable disable

        private static CornClient instance;
        public static CornClient Instance
        {
            get {
                EnsureInitialized();
                return instance;
            }
        }

        // Runs before a scene gets loaded
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeApp()
        {
            EnsureInitialized();
            Loom.Initialize();
        }

        public static void EnsureInitialized()
        {
            if (instance is null)
            {
                GameObject g = new GameObject("Corn Craft");
                GameObject.DontDestroyOnLoad(g);
                instance = g.AddComponent<CornClient>();
            }
        }

        #region Client Control
        private static bool connected = false;
        public static bool Connected { get { return connected; } }

        public static void StopClient()
        {
            if (instance is not null)
            {
                instance.Disconnect();
            }
        }

        #endregion

        #region Notification
        // Should be called from the Unity thread only, not net read thread
        public static void ShowNotification(string notification)
        {
            EventManager.Instance.Broadcast<NotificationEvent>(new NotificationEvent(notification));
        }

        public static void ShowNotification(string notification, Notification.Type type)
        {
            EventManager.Instance.Broadcast<NotificationEvent>(new NotificationEvent(notification, 6F, type));
        }

        public static void ShowNotification(string notification, float duration, Notification.Type type)
        {
            EventManager.Instance.Broadcast<NotificationEvent>(new NotificationEvent(notification, duration, type));
        }

        #endregion

        #region Unity stuff
        private readonly ResourcePackManager packManager = new ResourcePackManager();
        public ResourcePackManager PackManager { get { return packManager; } }
        
        void OnApplicationQuit()
        {
            if (CornClient.Connected)
            {
                CornClient.StopClient();
            }
        }

        private WorldRender worldRender;

        public WorldRender GetWorldRender()
        {
            return this.worldRender;
        }

        private ScreenControl screenControl;
        public ScreenControl ScreenControl { get { return screenControl; } }

        public bool IsPaused()
        {
            if (screenControl is not null)
                return screenControl.IsPaused;
            return true;
        }
        
        private PlayerController playerController;
        private CameraController cameraController;

        public PlayerController GetPlayerController()
        {
            return this.playerController;
        }

        #endregion

        private bool preparing = false;

        #nullable enable
        public void Login(string user, string uuid, string sessionID, PlayerKeyPair? playerKeyPair, string serverIp, ushort port, int protocol, ForgeInfo? forgeInfo, LoadStateInfo stateInfo)
        {
            if (preparing)
                return;
            
            preparing = true;

            this.sessionId = sessionID;
            if (!Guid.TryParse(uuid, out this.uuid))
                this.uuid = Guid.Empty;
            this.uuidStr = uuid;
            this.username = user;
            this.host = serverIp;
            this.port = port;
            this.protocolVersion = protocol;
            this.playerKeyPair = playerKeyPair;

            StartCoroutine(StartClient(user, uuid, sessionID, playerKeyPair, serverIp, port, protocol, forgeInfo, stateInfo));
        }

        public class CoroutineFlag
        {
            public bool done = false;
        }

        public class LoadStateInfo
        {
            public string infoText = string.Empty;
        }

        IEnumerator StartClient(string user, string uuid, string sessionID, PlayerKeyPair? playerKeyPair, string serverIp, ushort port, int protocol, ForgeInfo? forgeInfo, LoadStateInfo loadStateInfo)
        {
            var wait = new WaitForSecondsRealtime(0.1F);

            // Create block palette first to prepare for resource loading
            if (protocolVersion > ProtocolMinecraft.MC_1_19_2_Version)
                throw new NotImplementedException(Translations.Get("exception.palette.block"));
            
            var resourceVersion = string.Empty;

            if (protocolVersion >= ProtocolMinecraft.MC_1_19_Version)
            {
                Block.Palette = new Palette119();
                resourceVersion = "1.19.2";
            }
            else if (protocolVersion >= ProtocolMinecraft.MC_1_17_Version)
            {   // Treat 1.18.X as 1.17.X because there ain't a single block changed in 1.18
                Block.Palette = new Palette117();
                resourceVersion = "1.17.1";
            }
            else if (protocolVersion >= ProtocolMinecraft.MC_1_16_Version)
            {
                Block.Palette = new Palette116();
                resourceVersion = "1.16.5";
            }    
            else // TODO Implement More
            {
                Translations.LogError("exception.palette.block");
                yield break;
            }

            // Load texture atlas... (Will be decently implemented in future)
            BlockTextureManager.EnsureInitialized();
            BlockTextureManager.Load(resourceVersion);

            // Load resources...
            packManager.ClearPacks();

            ResourcePack pack = new ResourcePack("vanilla-" + resourceVersion);
            packManager.AddPack(pack);

            // Load valid packs...
            var resLoadFlag = new CoroutineFlag();
            var resLoad = StartCoroutine(packManager.LoadPacks(resLoadFlag, loadStateInfo));

            while (!resLoadFlag.done)
            {

                yield return wait;
            }

            // Prepare scene and unity objects
            var op = SceneManager.LoadSceneAsync("World", LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9F)
            {
                //Debug.Log("Loading: " + op.progress);
                yield return wait;
            }

            BlockTextureManager.EnsureInitialized();

            try // Setup tcp client
            {
                tcpClient = ProxyHandler.newTcpClient(host, port);
                tcpClient.ReceiveBufferSize = 1024 * 1024;
                tcpClient.ReceiveTimeout = 30000; // 30 seconds
            }
            catch (SocketException)
            {
                Translations.LogError("error.connect");
                Disconnect();
                preparing = false;
                yield break;
            }

            try // Initialize all palettes for resource loading
            {
                handler = Protocol.ProtocolHandler.GetProtocolHandler(tcpClient, protocol, forgeInfo, this);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Disconnect();
                preparing = false;
                yield break;
            }

            // Scene is loaded, activate it
            op.allowSceneActivation = true;

            // Wait a little bit...
            yield return wait;

            // Find Screen Control
            screenControl = Component.FindObjectOfType<ScreenControl>();
            var hudScreen = Component.FindObjectOfType<HUDScreen>();
            // Push HUD Screen on start
            screenControl.PushScreen(hudScreen);

            // Create World Render
            var worldRenderObj = new GameObject("World Render");
            worldRender = worldRenderObj.AddComponent<WorldRender>();

            // Create Player
            var playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
            var playerObj    = GameObject.Instantiate(playerPrefab);
            playerObj.SetActive(true);
            playerController = playerObj.GetComponent<PlayerController>();

            // Create Camera
            var cameraPrefab = Resources.Load<GameObject>("Prefabs/Camera");
            var cameraObj    = GameObject.Instantiate(cameraPrefab);
            cameraObj.SetActive(true);
            cameraController = cameraObj.GetComponent<CameraController>();

            cameraController.SetTarget(playerObj.transform);

            try
            {
                // Start update loop
                timeoutdetector = Tuple.Create(new Thread(new ParameterizedThreadStart(TimeoutDetector)), new CancellationTokenSource());
                timeoutdetector.Item1.Name = "Connection Timeout Detector";
                timeoutdetector.Item1.Start(timeoutdetector.Item2.Token);

                if (handler.Login(this.playerKeyPair)) // Login
                {
                    Translations.Notify("mcc.joined", CornCraft.internalCmdChar);
                    connected = true;
                }
                else
                {
                    Translations.LogError("error.login_failed");
                    Disconnect();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Debug.LogError(e.StackTrace);
                Disconnect();
                preparing = false;
                yield break;
            }
            finally
            {
                preparing = false;
            }

        }
        #nullable disable

        /// <summary>
        /// Called ~20 times per second by the protocol handler (on net read thread)
        /// </summary>
        public void OnUpdate()
        {
            lock (chatQueue)
            {
                if (chatQueue.Count > 0 && nextMessageSendTime < DateTime.Now)
                {
                    string text = chatQueue.Dequeue();
                    handler.SendChatMessage(text, playerKeyPair);
                    nextMessageSendTime = DateTime.Now + TimeSpan.FromSeconds(1);
                }
            }

            if (locationReceived && localLocationUpdated)
            {
                lock (locationLock)
                {
                    playerYaw = _yaw == null ? playerYaw : _yaw.Value;
                    playerPitch = _pitch == null ? playerPitch : _pitch.Value;
                    
                    // TODO
                    handler.SendLocationUpdate(location, Movement.IsOnGround(world, location), _yaw, _pitch);
                    //handler.SendLocationUpdate(location, isOnGround, _yaw, _pitch);
                    
                    // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
                    // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
                    _yaw = null;
                    _pitch = null;

                    localLocationUpdated = false;
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

        #region Connection Lost and Disconnect from Server

        /// <summary>
        /// Periodically checks for server keepalives and consider that connection has been lost if the last received keepalive is too old.
        /// </summary>
        #nullable enable
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
        #nullable disable

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

        private void PrepareDisconnect()
        {
            connected = false;

            // Dispose objects in current client
            Loom.QueueOnMainThread(
                // Called by protocol handler from net read thread
                // so it is neccessary to run it on Unity thread via Loom
                () => {
                    worldRender?.UnloadWorld();
                    screenControl?.ClearScreens();

                    // Release cursor anyway
                    Cursor.lockState = CursorLockMode.None;

                    // Clear objects in world scene (no need to destroy them manually, tho)
                    worldRender   = null;
                    screenControl = null;
                    playerController = null;
                    cameraController = null;

                    // Reset this marker, so that it's not gonna cause troubles next time we log in
                    locationReceived = false;

                    _yaw   = null;
                    _pitch = null;
                });
        }

        /// <summary>
        /// Disconnect the client from the server
        /// </summary>
        public void Disconnect()
        {
            PrepareDisconnect();

            handler?.Disconnect();
            handler?.Dispose();
            handler = null;

            timeoutdetector?.Item2.Cancel();
            timeoutdetector = null;

            tcpClient?.Close();
            tcpClient = null;
            
        }

        /// <summary>
        /// When connection has been lost, login was denied or player was kicked from the server
        /// </summary>
        public void OnConnectionLost(DisconnectReason reason, string message)
        {
            // Ensure Unity objects are cleared first
            PrepareDisconnect();

            // Clear world data
            chunkProcessCancelSource.Cancel();
            world.Clear();
            chunkProcessCancelSource = new();

            // Go back to login scene
            Loom.QueueOnMainThread(() => {
                SceneManager.LoadScene("Login");
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

        }

        #endregion

        #region Command prompt and internal MCC commands

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// </summary>
        public void CommandPrompt(string text)
        {
            try
            {
                InvokeOnNetReadThread(() => HandleCommandPromptText(text));
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
        }

        /// <summary>
        /// Allows the user to send chat messages or internal commands
        /// </summary>
        private void HandleCommandPromptText(string text)
        {
            text = text.Trim();
            if (text.Length > 0) // Not empty...
            {
                if (text[0] == CornCraft.internalCmdChar)
                {
                    string response = string.Empty;
                    string command = text.Substring(1); // Remove the leading command prefix
                    if (!PerformInternalCommand(CornCraft.ExpandVars(command), ref response) && CornCraft.internalCmdChar == '/')
                    {   // Not an internal command, send it to the server
                        SendText(text);
                    }
                    else if (response.Length > 0)
                    {   // Internal command performed, log the result...
                        Debug.Log(response);
                    }
                }
                else SendText(text);
            }
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
                InvokeOnNetReadThread(() => handler.SendAutoCompleteText(text));
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
        }

        #nullable enable
        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="response">May contain a confirmation or error message after processing the command, or "" otherwise.</param>
        /// <param name="localVars">Local variables passed along with the command</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>
        public bool PerformInternalCommand(string command, ref string response, Dictionary<string, object>? localVars = null)
        {
            /* Process the provided command */

            string command_name = command.Split(' ')[0].ToLower();
            if (command_name == "help")
            {
                if (Command.hasArg(command))
                {
                    string help_cmdname = Command.getArgs(command)[0].ToLower();
                    if (help_cmdname == "help")
                    {
                        response = Translations.Get("icmd.help");
                    }
                    else if (cmds.ContainsKey(help_cmdname))
                    {
                        response = cmds[help_cmdname].GetCmdDescTranslated();
                    }
                    else response = Translations.Get("icmd.unknown", command_name);
                }
                else response = Translations.Get("icmd.list", String.Join(", ", cmd_names.ToArray()), CornCraft.internalCmdChar);
            }
            else if (cmds.ContainsKey(command_name))
            {
                response = cmds[command_name].Run(this, command, localVars);
            }
            else
            {
                response = Translations.Get("icmd.unknown", command_name);
                return false;
            }

            return true;
        }
        #nullable disable

        public void LoadCommands()
        {
            /* Load commands from the 'Commands' namespace */

            if (!commandsLoaded)
            {
                Type[] cmds_classes = CornCraft.GetTypesInNamespace("MinecraftClient.Commands");
                foreach (Type type in cmds_classes)
                {
                    if (type.IsSubclassOf(typeof(Command)))
                    {
                        try
                        {
                            Command cmd = (Command)Activator.CreateInstance(type);
                            cmds[cmd.CmdName.ToLower()] = cmd;
                            cmd_names.Add(cmd.CmdName.ToLower());
                            foreach (string alias in cmd.getCMDAliases())
                                cmds[alias.ToLower()] = cmd;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(e.Message);
                        }
                    }
                }
                commandsLoaded = true;
            }
        }

        #endregion

        #region Thread-Invoke: Cross-thread method calls

        /// <summary>
        /// Invoke a task on the main thread, wait for completion and retrieve return value.
        /// </summary>
        /// <param name="task">Task to run with any type or return value</param>
        /// <returns>Any result returned from task, result type is inferred from the task</returns>
        /// <example>bool result = InvokeOnNetReadThread(methodThatReturnsAbool);</example>
        /// <example>bool result = InvokeOnNetReadThread(() => methodThatReturnsAbool(argument));</example>
        /// <example>int result = InvokeOnNetReadThread(() => { yourCode(); return 42; });</example>
        /// <typeparam name="T">Type of the return value</typeparam>
        public T InvokeOnNetReadThread<T>(Func<T> task)
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
        /// <example>InvokeOnNetReadThread(methodThatReturnsNothing);</example>
        /// <example>InvokeOnNetReadThread(() => methodThatReturnsNothing(argument));</example>
        /// <example>InvokeOnNetReadThread(() => { yourCode(); });</example>
        public void InvokeOnNetReadThread(Action task)
        {
            InvokeOnNetReadThread(() => { task(); return true; });
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
        /// Check if running on a different thread and InvokeOnNetReadThread is required
        /// </summary>
        /// <returns>True if calling thread is not the main thread</returns>
        public bool InvokeRequired
        {
            get
            {
                int callingThreadId = Thread.CurrentThread.ManagedThreadId;
                if (handler != null)
                {
                    return handler.GetNetReadThreadId() != callingThreadId;
                }
                else
                {
                    // net read thread not yet ready
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
            return handler.GetMaxChatMessageLength();
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
        /// Get all inventories. ID 0 is the player inventory.
        /// </summary>
        /// <returns>All inventories</returns>
        public Dictionary<int, Container> GetInventories()
        {
            return inventories;
        }

        /// <summary>
        /// Get all Entities
        /// </summary>
        /// <returns>All Entities</returns>
        public Dictionary<int, Entity> GetEntities()
        {
            return entities;
        }

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

        public int GetOwnLatency()
        {
            return onlinePlayers.ContainsKey(uuid) ? onlinePlayers[uuid].Ping : 0;
        }

        #nullable enable
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
        #nullable disable

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <param name="inventoryID">Window ID of the requested inventory</param>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container GetInventory(int inventoryID)
        {
            if (InvokeRequired)
                return InvokeOnNetReadThread(() => GetInventory(inventoryID));

            if (inventories.ContainsKey(inventoryID))
                return inventories[inventoryID];
            return null;
        }

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container GetPlayerInventory()
        {
            return GetInventory(0);
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
        public void SendText(string text)
        {
            lock (chatQueue)
            {
                if (String.IsNullOrEmpty(text))
                    return;
                int maxLength = handler.GetMaxChatMessageLength();
                if (text.Length > maxLength) //Message is too long?
                {
                    if (text[0] == '/')
                    {
                        //Send the first 100/256 chars of the command
                        text = text.Substring(0, maxLength);
                        chatQueue.Enqueue(text);
                    }
                    else
                    {
                        //Split the message into several messages
                        while (text.Length > maxLength)
                        {
                            chatQueue.Enqueue(text.Substring(0, maxLength));
                            text = text.Substring(maxLength, text.Length - maxLength);
                        }
                        chatQueue.Enqueue(text);
                    }
                }
                else chatQueue.Enqueue(text);
            }
        }

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        public bool SendRespawnPacket()
        {
            if (InvokeRequired)
                return InvokeOnNetReadThread<bool>(SendRespawnPacket);

            return handler.SendRespawnPacket();
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified ID
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool SendEntityAction(EntityActionType entityAction)
        {
            return InvokeOnNetReadThread(() => handler.SendEntityAction(playerEntityID, (int)entityAction));
        }

        /// <summary>
        /// Use the item currently in the player's hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool UseItemOnHand()
        {
            return InvokeOnNetReadThread(() => handler.SendUseItem(0, this.sequenceId));
        }

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            if (InvokeRequired)
                return InvokeOnNetReadThread(() => DoWindowAction(windowId, slotId, action));

            Item item = null;
            if (inventories.ContainsKey(windowId) && inventories[windowId].Items.ContainsKey(slotId))
                item = inventories[windowId].Items[slotId];

            List<Tuple<short, Item>> changedSlots = new List<Tuple<short, Item>>(); // List<Slot ID, Changed Items>

            // Update our inventory base on action type
            var inventory = GetInventory(windowId);
            var playerInventory = GetInventory(0);
            if (inventory != null)
            {
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
                                if (inventory.Items[slotId].Type == playerInventory.Items[-1].Type)
                                {
                                    int maxCount = inventory.Items[slotId].Type.StackCount();
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
                                    var itemTmp = playerInventory.Items[-1];
                                    playerInventory.Items[-1] = inventory.Items[slotId];
                                    inventory.Items[slotId] = itemTmp;
                                }
                            }
                            else
                            {
                                // Put cursor item to target
                                inventory.Items[slotId] = playerInventory.Items[-1];
                                playerInventory.Items.Remove(-1);
                            }

                            if (inventory.Items.ContainsKey(slotId))
                                changedSlots.Add(new Tuple<short, Item>((short)slotId, inventory.Items[slotId]));
                            else
                                changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
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

                                changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
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
                                if (inventory.Items[slotId].Type == playerInventory.Items[-1].Type)
                                {
                                    // Check item stacking
                                    if (inventory.Items[slotId].Count < inventory.Items[slotId].Type.StackCount())
                                    {
                                        // Drop 1 item count from cursor
                                        playerInventory.Items[-1].Count--;
                                        inventory.Items[slotId].Count++;
                                    }
                                }
                                else
                                {
                                    // Swap two items
                                    var itemTmp = playerInventory.Items[-1];
                                    playerInventory.Items[-1] = inventory.Items[slotId];
                                    inventory.Items[slotId] = itemTmp;
                                }
                            }
                            else
                            {
                                // Drop 1 item count from cursor
                                var itemTmp = playerInventory.Items[-1];
                                var itemClone = new Item(itemTmp.Type, 1, itemTmp.NBT);
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
                                        Item itemTmp = inventory.Items[slotId];
                                        playerInventory.Items[-1] = new Item(itemTmp.Type, itemTmp.Count / 2, itemTmp.NBT);
                                        inventory.Items[slotId].Count = itemTmp.Count / 2;
                                    }
                                    else
                                    {
                                        // Cannot be evenly divided. item count on cursor is always larger than item on inventory
                                        Item itemTmp = inventory.Items[slotId];
                                        playerInventory.Items[-1] = new Item(itemTmp.Type, (itemTmp.Count + 1) / 2, itemTmp.NBT);
                                        inventory.Items[slotId].Count = (itemTmp.Count - 1) / 2;
                                    }
                                }
                            }
                        }
                        if (inventory.Items.ContainsKey(slotId))
                            changedSlots.Add(new Tuple<short, Item>((short)slotId, inventory.Items[slotId]));
                        else
                            changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
                        break;
                    case WindowActionType.ShiftClick:
                        if (slotId == 0) break;
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            /* Target slot have item */

                            int upperStartSlot = 9;
                            int upperEndSlot = 35;

                            switch (inventory.Type)
                            {
                                case ContainerType.PlayerInventory:
                                    upperStartSlot = 9;
                                    upperEndSlot = 35;
                                    break;
                                case ContainerType.Crafting:
                                    upperStartSlot = 1;
                                    upperEndSlot = 9;
                                    break;
                                    // TODO: Define more container type here
                            }

                            // Cursor have item or not doesn't matter
                            // If hotbar already have same item, will put on it first until every stack are full
                            // If no more same item , will put on the first empty slot (smaller slot id)
                            // If inventory full, item will not move
                            int itemCount = inventory.Items[slotId].Count;
                            if (slotId <= upperEndSlot)
                            {
                                // Clicked slot is on upper side inventory, put it to hotbar
                                // Now try to find same item and put on them
                                var itemsClone = playerInventory.Items.ToDictionary(entry => entry.Key, entry => entry.Value);
                                foreach (KeyValuePair<int, Item> _item in itemsClone)
                                {
                                    if (_item.Key <= upperEndSlot) continue;

                                    int maxCount = _item.Value.Type.StackCount();
                                    if (_item.Value.Type == inventory.Items[slotId].Type && _item.Value.Count < maxCount)
                                    {
                                        // Put item on that stack
                                        int spaceLeft = maxCount - _item.Value.Count;
                                        if (inventory.Items[slotId].Count <= spaceLeft)
                                        {
                                            // Can fit into the stack
                                            inventory.Items[_item.Key].Count += inventory.Items[slotId].Count;
                                            inventory.Items.Remove(slotId);

                                            changedSlots.Add(new Tuple<short, Item>((short)_item.Key, inventory.Items[_item.Key]));
                                            changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
                                        }
                                        else
                                        {
                                            inventory.Items[slotId].Count -= spaceLeft;
                                            inventory.Items[_item.Key].Count = inventory.Items[_item.Key].Type.StackCount();

                                            changedSlots.Add(new Tuple<short, Item>((short)_item.Key, inventory.Items[_item.Key]));
                                        }
                                    }
                                }
                                if (inventory.Items[slotId].Count > 0)
                                {
                                    int[] emptySlots = inventory.GetEmpytSlots();
                                    int emptySlot = -2;
                                    foreach (int slot in emptySlots)
                                    {
                                        if (slot <= upperEndSlot) continue;
                                        emptySlot = slot;
                                        break;
                                    }
                                    if (emptySlot != -2)
                                    {
                                        var itemTmp = inventory.Items[slotId];
                                        inventory.Items[emptySlot] = new Item(itemTmp.Type, itemTmp.Count, itemTmp.NBT);
                                        inventory.Items.Remove(slotId);

                                        changedSlots.Add(new Tuple<short, Item>((short)emptySlot, inventory.Items[emptySlot]));
                                        changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
                                    }
                                    else if (inventory.Items[slotId].Count != itemCount)
                                    {
                                        changedSlots.Add(new Tuple<short, Item>((short)slotId, inventory.Items[slotId]));
                                    }
                                }
                            }
                            else
                            {
                                // Clicked slot is on hotbar, put it to upper inventory
                                // Now try to find same item and put on them
                                var itemsClone = playerInventory.Items.ToDictionary(entry => entry.Key, entry => entry.Value);
                                foreach (KeyValuePair<int, Item> _item in itemsClone)
                                {
                                    if (_item.Key < upperStartSlot) continue;
                                    if (_item.Key >= upperEndSlot) break;

                                    int maxCount = _item.Value.Type.StackCount();
                                    if (_item.Value.Type == inventory.Items[slotId].Type && _item.Value.Count < maxCount)
                                    {
                                        // Put item on that stack
                                        int spaceLeft = maxCount - _item.Value.Count;
                                        if (inventory.Items[slotId].Count <= spaceLeft)
                                        {
                                            // Can fit into the stack
                                            inventory.Items[_item.Key].Count += inventory.Items[slotId].Count;
                                            inventory.Items.Remove(slotId);

                                            changedSlots.Add(new Tuple<short, Item>((short)_item.Key, inventory.Items[_item.Key]));
                                            changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
                                        }
                                        else
                                        {
                                            inventory.Items[slotId].Count -= spaceLeft;
                                            inventory.Items[_item.Key].Count = inventory.Items[_item.Key].Type.StackCount();

                                            changedSlots.Add(new Tuple<short, Item>((short)_item.Key, inventory.Items[_item.Key]));
                                        }
                                    }
                                }
                                if (inventory.Items[slotId].Count > 0)
                                {
                                    int[] emptySlots = inventory.GetEmpytSlots();
                                    int emptySlot = -2;
                                    foreach (int slot in emptySlots)
                                    {
                                        if (slot < upperStartSlot) continue;
                                        if (slot >= upperEndSlot) break;
                                        emptySlot = slot;
                                        break;
                                    }
                                    if (emptySlot != -2)
                                    {
                                        var itemTmp = inventory.Items[slotId];
                                        inventory.Items[emptySlot] = new Item(itemTmp.Type, itemTmp.Count, itemTmp.NBT);
                                        inventory.Items.Remove(slotId);

                                        changedSlots.Add(new Tuple<short, Item>((short)emptySlot, inventory.Items[emptySlot]));
                                        changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
                                    }
                                    else if (inventory.Items[slotId].Count != itemCount)
                                    {
                                        changedSlots.Add(new Tuple<short, Item>((short)slotId, inventory.Items[slotId]));
                                    }
                                }
                            }
                        }
                        break;
                    case WindowActionType.DropItem:
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            inventory.Items[slotId].Count--;
                            changedSlots.Add(new Tuple<short, Item>((short)slotId, inventory.Items[slotId]));
                        }

                        if (inventory.Items[slotId].Count <= 0)
                        {
                            inventory.Items.Remove(slotId);
                            changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
                        }

                        break;
                    case WindowActionType.DropItemStack:
                        inventory.Items.Remove(slotId);
                        changedSlots.Add(new Tuple<short, Item>((short)slotId, null));
                        break;
                }
            }

            return handler.SendWindowAction(windowId, slotId, action, item, changedSlots, inventories[windowId].StateID);
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
        public bool DoCreativeGive(int slot, ItemType itemType, int count, Dictionary<string, object> nbt = null)
        {
            return InvokeOnNetReadThread(() => handler.SendCreativeInventoryAction(slot, itemType, count, nbt));
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public bool DoAnimation(int animation)
        {
            return InvokeOnNetReadThread(() => handler.SendAnimation(animation, playerEntityID));
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
                return InvokeOnNetReadThread(() => CloseInventory(windowId));

            if (inventories.ContainsKey(windowId))
            {
                if (windowId != 0)
                    inventories.Remove(windowId);
                return handler.SendCloseWindow(windowId);
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
                return InvokeOnNetReadThread<bool>(ClearInventories);

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
                return InvokeOnNetReadThread(() => InteractEntity(entityID, type, hand));

            if (entities.ContainsKey(entityID))
            {
                if (type == 0)
                {
                    return handler.SendInteractEntity(entityID, type, (int)hand);
                }
                else
                {
                    return handler.SendInteractEntity(entityID, type);
                }
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
            return InvokeOnNetReadThread(() => handler.SendPlayerBlockPlacement((int)hand, location, blockFace, this.sequenceId));
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
                return InvokeOnNetReadThread(() => DigBlock(location, swingArms, lookAtBlock));

            // TODO select best face from current player location
            Direction blockFace = Direction.Down;

            // Look at block before attempting to break it
            if (lookAtBlock)
                UpdateLocation(GetCurrentLocation(), location);

            // Send dig start and dig end, will need to wait for server response to know dig result
            // See https://wiki.vg/How_to_Write_a_Client#Digging for more details
            return handler.SendPlayerDigging(0, location, blockFace, this.sequenceId)
                && (!swingArms || DoAnimation((int)Hand.MainHand))
                && handler.SendPlayerDigging(2, location, blockFace, this.sequenceId);
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
                return InvokeOnNetReadThread(() => ChangeSlot(slot));

            CurrentSlot = Convert.ToByte(slot);
            return handler.SendHeldItemChange(slot);
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
            return InvokeOnNetReadThread(() => handler.SendUpdateSign(location, line1, line2, line3, line4));
        }

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0.</param>
        public bool SelectTrade(int selectedSlot)
        {
            return InvokeOnNetReadThread(() => handler.SelectTrade(selectedSlot));
        }

        /// <summary>
        /// Update command block
        /// </summary>
        /// <param name="location">command block location</param>
        /// <param name="command">command</param>
        /// <param name="mode">command block mode</param>
        /// <param name="flags">command block flags</param>
        public bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            return InvokeOnNetReadThread(() => handler.UpdateCommandBlock(location, command, mode, flags));
        }

        /// <summary>
        /// Teleport to player in spectator mode
        /// </summary>
        /// <param name="entity">Player to teleport to</param>
        /// Teleporting to other entityies is NOT implemented yet
        public bool Spectate(Entity entity)
        {
            if(entity.Type == EntityType.Player)
            {
                return SpectateByUUID(entity.UUID);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Teleport to player/entity in spectator mode
        /// </summary>
        /// <param name="UUID">UUID of player/entity to teleport to</param>
        public bool SpectateByUUID(Guid UUID)
        {
            if(GetGamemode() == 3)
            {
                if(InvokeRequired)
                    return InvokeOnNetReadThread(() => SpectateByUUID(UUID));
                return handler.SendSpectate(UUID);
            }
            else
            {
                return false;
            }
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
            handler.SendBrandInfo(CornCraft.BrandInfo.Trim());

            if (CornCraft.MCSettings_Enabled)
                handler.SendClientSettings(
                    CornCraft.MCSettings_Locale,
                    CornCraft.MCSettings_RenderDistance,
                    CornCraft.MCSettings_Difficulty,
                    CornCraft.MCSettings_ChatMode,
                    CornCraft.MCSettings_ChatColors,
                    CornCraft.MCSettings_Skin_All,
                    CornCraft.MCSettings_MainHand);


            if (inventoryHandlingRequested)
            {
                inventoryHandlingRequested = false;
                Debug.Log(Translations.Get("extra.inventory_enabled"));
            }

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

            chunkProcessCancelSource.Cancel();
            world.Clear();
            chunkProcessCancelSource = new();
            
            entities.Clear();
            ClearInventories();
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
            lock (locationLock)
            {
                this._yaw = yaw;
                this._pitch = pitch;

                this.location = location;
                localLocationUpdated = false;
                locationReceived = true;

                Loom.QueueOnMainThread(() => {
                    playerController.SetPosition(location);
                });
                
            }

        }

        /// <summary>
        /// Called by player controller each frame.
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="yaw">Yaw to look at</param>
        /// <param name="pitch">Pitch to look at</param>
        public void SyncLocation(Location location, float yaw, float pitch)
        {
            lock (locationLock)
            {
                if (this.location != location || playerYaw != yaw || playerPitch != pitch)
                {
                    this._yaw = yaw;
                    this._pitch = pitch;

                    this.location = location;
                    localLocationUpdated = true;
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
                if (!CornCraft.ShowIllegalSignedChat && !message.isSystemChat && !(bool)message.isSignatureLegal!)
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
                () => EventManager.Instance.Broadcast<ChatMessageEvent>(new ChatMessageEvent(messageText))
            );

            foreach (string link in links)
                Translations.Log("mcc.link", link);

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
        public void OnWindowItems(byte inventoryID, Dictionary<int, Inventory.Item> itemList, int stateId)
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
        public void OnSetSlot(byte inventoryID, short slotID, Item item, int stateId)
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
            playerEntityID = EntityID;
        }

        /// <summary>
        /// Triggered when a new player joins the game
        /// </summary>
        /// <param name="player">player info</param>
        public void OnPlayerJoin(PlayerInfo player)
        {
            //Ignore placeholders eg 0000tab# from TabListPlus
            if (!CornCraft.IsValidName(player.Name))
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

        #nullable enable
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
        #nullable disable

        /// <summary>
        /// Called when an entity spawned
        /// </summary>
        public void OnSpawnEntity(Entity entity)
        {
            // The entity should not already exist, but if it does, let's consider the previous one is being destroyed
            if (entities.ContainsKey(entity.ID))
                OnDestroyEntities(new[] { entity.ID });

            entities.Add(entity.ID, entity);
        }

        #nullable enable
        /// <summary>
        /// Called when an entity effects
        /// </summary>
        public void OnEntityEffect(int entityid, Effects effect, int amplifier, int duration, byte flags, bool hasFactorData, Dictionary<string, object>? factorCodec) { }
        #nullable disable

        #nullable enable
        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        public void OnSpawnPlayer(int entityID, Guid uuid, Location location, byte Yaw, byte Pitch)
        {
            string? playerName = null;
            if (onlinePlayers.ContainsKey(uuid))
                playerName = onlinePlayers[uuid].Name;
            Entity playerEntity = new Entity(entityID, EntityType.Player, location, uuid, playerName);
            OnSpawnEntity(playerEntity);
        }
        #nullable disable

        /// <summary>
        /// Called on Entity Equipment
        /// </summary>
        /// <param name="entityid"> Entity ID</param>
        /// <param name="slot"> Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item"> Item)</param>
        public void OnEntityEquipment(int entityid, int slot, Item item)
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
            // Initial gamemode on login
            if (uuid == Guid.Empty)
                this.gamemode = gamemode;

            // Further regular gamemode change events
            if (onlinePlayers.ContainsKey(uuid))
            {
                string playerName = onlinePlayers[uuid].Name;
                if (playerName == this.username)
                    this.gamemode = gamemode;
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
                {
                    entities.Remove(a);
                }
            }
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
                Location L = entities[EntityID].Location;
                L.X += Dx;
                L.Y += Dy;
                L.Z += Dz;
                entities[EntityID].Location = L;
            }

        }

        public void OnEntityRotation(int EntityID, float yaw, float pitch, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                entities[EntityID].Yaw = yaw;
                entities[EntityID].Pitch = pitch;
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
        /// <param name="WorldAge"></param>
        /// <param name="TimeOfDay"></param>
        public void OnTimeUpdate(long WorldAge, long TimeOfDay)
        {
            // TimeUpdate sent every server tick hence used as timeout detect
            UpdateKeepAlive();
            // calculate server tps
            if (lastAge != 0)
            {
                DateTime currentTime = DateTime.Now;
                long tickDiff = WorldAge - lastAge;
                Double tps = tickDiff / (currentTime - lastTime).TotalSeconds;
                lastAge = WorldAge;
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
                lastAge = WorldAge;
                lastTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Called when client player's health changed, e.g. getting attack
        /// </summary>
        /// <param name="health">Player current health</param>
        public void OnUpdateHealth(float health, int food)
        {
            playerHealth = health;
            playerFoodSaturation = food;

            if (health <= 0)
            {
                Debug.Log(Translations.Get("mcc.player_dead"));
            }

        }

        /// <summary>
        /// Called when experience updates
        /// </summary>
        /// <param name="Experiencebar">Between 0 and 1</param>
        /// <param name="Level">Level</param>
        /// <param name="TotalExperience">Total Experience</param>
        public void OnSetExperience(float Experiencebar, int Level, int TotalExperience)
        {
            playerLevel = Level;
            playerTotalExperience = TotalExperience;
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
        public void OnHeldItemChange(byte slot)
        {
            CurrentSlot = slot;
        }

        /// <summary>
        /// Called map data
        /// </summary>
        /// <param name="mapid"></param>
        /// <param name="scale"></param>
        /// <param name="trackingposition"></param>
        /// <param name="locked"></param>
        /// <param name="iconcount"></param>
        public void OnMapData(int mapid, byte scale, bool trackingposition, bool locked, int iconcount)
        {

        }

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
        public void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json)
        {

        }

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
                entities[entityID].Health = health;
            }
        }

        /// <summary>
        /// Called when the metadata of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="metadata">The metadata of the entity</param>
        public void OnEntityMetadata(int entityID, Dictionary<int, object> metadata)
        {
            if (entities.ContainsKey(entityID))
            {
                Entity entity = entities[entityID];
                entity.Metadata = metadata;
                if (entity.Type.ContainsItem() && metadata.ContainsKey(7) && metadata[7] != null && metadata[7].GetType() == typeof(Item))
                {
                    Item item = (Item)metadata[7];
                    if (item == null)
                        entity.Item = new Item(ItemType.Air, 0, null);
                    else entity.Item = item;
                }
                if (metadata.ContainsKey(6) && metadata[6] != null && metadata[6].GetType() == typeof(Int32))
                {
                    entity.Pose = (EntityPose)metadata[6];
                }
                if (metadata.ContainsKey(2) && metadata[2] != null && metadata[2].GetType() == typeof(string))
                {
                    entity.CustomNameJson = metadata[2].ToString();
                    entity.CustomName = ChatParser.ParseText(metadata[2].ToString());
                }
                if (metadata.ContainsKey(3) && metadata[3] != null && metadata[3].GetType() == typeof(bool))
                {
                    entity.IsCustomNameVisible = bool.Parse(metadata[3].ToString());
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
        public void OnRainChange(bool begin)
        {

        }

        /// <summary>
        /// Called when a Synchronization sequence is recevied, this sequence need to be sent when breaking or placing blocks
        /// </summary>
        /// <param name="sequenceId">Sequence ID</param>
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
        public void OnServerDataReceived(bool hasMotd, string motd, bool hasIcon, string iconBase64, bool previewsChat)
        {
            this.isSupportPreviewsChat = previewsChat;
        }

        /// <summary>
        /// Called when the protocol handler receives "Set Display Chat Preview" packet
        /// </summary>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        public void OnChatPreviewSettingUpdate(bool previewsChat)
        {
            this.isSupportPreviewsChat = previewsChat;
        }

        #nullable enable
        /// <summary>
        /// Called when the protocol handler receives "Login Success" packet
        /// </summary>
        /// <param name="uuid">The player's UUID received from the server</param>
        /// <param name="userName">The player's username received from the server</param>
        /// <param name="playerProperty">Tuple<Name, Value, Signature(empty if there is no signature)></param>
        public void OnLoginSuccess(Guid uuid, string userName, Tuple<string, string, string>[]? playerProperty)
        {

        }
        #nullable disable

        #endregion

    }
}
