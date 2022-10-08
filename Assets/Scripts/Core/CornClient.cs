using System;
using System.Collections;
using System.Collections.Generic;
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
using MinecraftClient.Protocol.Session;
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

        #region Client Control
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
                GameObject magic = new GameObject("Corn Craft");
                GameObject.DontDestroyOnLoad(magic);
                instance = magic.AddComponent<CornClient>();
            }
        }

        private static readonly List<string> cmdNames = new();
        private static readonly Dictionary<string, Command> cmds = new();
        private static bool internalCommandsLoaded = false;

        private Queue<Action> threadTasks = new();
        private object threadTasksLock = new();

        private Queue<string> chatQueue = new();
        private DateTime nextMessageSendTime = DateTime.MinValue;

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

        #region Networking
        private DateTime lastKeepAlive;
        private object lastKeepAliveLock = new();
        private long lastAge = 0L, timeOfDay = 0L;
        public long CurrentTimeOfDay { get { return timeOfDay + (long)(timeElapsedSinceUpdate * 20F); } }
        private float timeElapsedSinceUpdate = 0F;

        private DateTime lastTime;
        private double serverTPS = 0;
        private double averageTPS = 20;
        private const int maxSamples = 5;
        private List<double> tpsSamples = new(maxSamples);
        private double sampleSum = 0;
        public double GetServerTPS() { return averageTPS; }
        public float GetTickMilSec() { return (float)(1D / averageTPS); }
        public bool GetIsSupportPreviewsChat() { return isSupportPreviewsChat; }
        
        TcpClient tcpClient;
        IMinecraftCom handler;

        #nullable enable
        Tuple<Thread, CancellationTokenSource>? timeoutdetector = null;
        #nullable disable
        #endregion

        #region Notification
        // Should be called from the Unity thread only, not net read thread
        public static void ShowNotification(string notification)
        {
            EventManager.Instance.Broadcast<NotificationEvent>(new(notification));
        }

        public static void ShowNotification(string notification, Notification.Type type)
        {
            EventManager.Instance.Broadcast<NotificationEvent>(new(notification, 6F, type));
        }

        public static void ShowNotification(string notification, float duration, Notification.Type type)
        {
            EventManager.Instance.Broadcast<NotificationEvent>(new(notification, duration, type));
        }

        #endregion

        #region World Data and Player Movement
        private bool worldAndMovementsRequested = false;
        private World world = new();
        private Location location;
        private object locationLock = new();
        private bool locationReceived = false, localLocationUpdated = false;
        public bool LocationReceived { get { return locationReceived; } }
        private float? _yaw; // Used for calculation ONLY!!! Doesn't reflect the client yaw
        private float? _pitch; // Used for calculation ONLY!!! Doesn't reflect the client pitch
        private float playerYaw;
        private float playerPitch;
        private int sequenceId; // User for player block synchronization (Aka. digging, placing blocks, etc..)
        public Location GetCurrentLocation() { return location; }

        public float GetYaw() { return playerYaw; }
        public float GetPitch() { return playerPitch; }
        public World GetWorld() { return world; }
        #endregion

        #region Players and Entities
        private bool inventoryHandlingRequested = false;
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
        public GameMode GetGamemode() { return playerController is null ? 0 : playerController.GameMode; }
        public int GetPlayerEntityID() { return playerEntityID; }
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();
        private Dictionary<int, Entity> entities = new();
        #endregion

        #region Unity stuffs
        private readonly ResourcePackManager packManager = new ResourcePackManager();
        public ResourcePackManager PackManager { get { return packManager; } }
        
        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11)) // Toggle full screen
            {
                if (Screen.fullScreen)
                {
                    Screen.SetResolution(WINDOWED_APP_WIDTH, WINDOWED_APP_HEIGHT, false);
                    Screen.fullScreen = false;
                }
                else
                {
                    var maxRes = Screen.resolutions[Screen.resolutions.Length - 1];
                    Screen.SetResolution(maxRes.width, maxRes.height, true);
                    Screen.fullScreen = true;
                }
                
            }

            // Time update
            timeElapsedSinceUpdate += Time.unscaledDeltaTime;

        }

        void OnApplicationQuit()
        {
            if (Connected)
                StopClient();
            
        }

        private WorldRender worldRender;

        public WorldRender GetWorldRender()
        {
            return this.worldRender;
        }

        private EntityManager entityManager;

        public EntityManager GetEntityManager()
        {
            return this.entityManager;
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

        public PlayerController GetPlayerController() => playerController;
        public CameraController GetCameraController() => cameraController;

        public Vector3 GetCameraPosition()
        {
            if (cameraController is null)
                return Vector3.zero;
            
            return cameraController.ActiveCamera.transform.position;
        }

        public enum Perspective
        {
            FirstPerson, ThirdPerson
        }

        public Perspective CurrentPerspective = Perspective.FirstPerson;

        public void TogglePerspective()
        {
            if (CurrentPerspective == Perspective.FirstPerson)
                CurrentPerspective = Perspective.ThirdPerson;
            else
                CurrentPerspective = Perspective.FirstPerson;
            
            cameraController.SetPerspective(CurrentPerspective);

            EventManager.Instance.Broadcast<PerspectiveUpdateEvent>(new(CurrentPerspective));
        }

        #endregion

        #nullable enable
        public void StartLogin(SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp, ushort port, int protocol, ForgeInfo? forgeInfo, LoadStateInfo loadStateInfo, string accountLower)
        {
            if (loadStateInfo.loggingIn)
                return;
            
            loadStateInfo.loggingIn = true;

            this.sessionId = session.ID;
            if (!Guid.TryParse(session.PlayerID, out this.uuid))
                this.uuid = Guid.Empty;
            this.uuidStr = session.PlayerID;
            this.username = session.PlayerName;
            this.host = serverIp;
            this.port = port;
            this.protocolVersion = protocol;
            this.playerKeyPair = playerKeyPair;

            StartCoroutine(StartClient(session, playerKeyPair, serverIp, port, protocol, forgeInfo, loadStateInfo, accountLower));
        }

        IEnumerator StartClient(SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp, ushort port, int protocol, ForgeInfo? forgeInfo, LoadStateInfo loadStateInfo, string accountLower)
        {
            var wait = new WaitForSecondsRealtime(0.1F);
            loadStateInfo.loggingIn = true;

            if (!internalCommandsLoaded)
                LoadInternalCommands();

            // Create block palette first to prepare for resource loading
            if (protocolVersion > ProtocolMinecraft.MC_1_19_2_Version)
            {
                Translations.LogError("exception.palette.block");
                loadStateInfo.loggingIn = false;
                yield break;
            }
            
            var resourceVersion = string.Empty;
            var blockLoadFlag = new CoroutineFlag();

            Block.Palette = new BlockStatePalette();
            if (protocolVersion >= ProtocolMinecraft.MC_1_19_Version)
            {
                StartCoroutine(Block.Palette.PrepareData("1.19", blockLoadFlag, loadStateInfo));
                resourceVersion = "1.19.2";
            }
            else if (protocolVersion >= ProtocolMinecraft.MC_1_17_Version)
            {   // Treat 1.18.X as 1.17.X because there ain't a single block changed in 1.18
                StartCoroutine(Block.Palette.PrepareData("1.17", blockLoadFlag, loadStateInfo));
                resourceVersion = "1.17.1";
            }
            else if (protocolVersion >= ProtocolMinecraft.MC_1_16_Version)
            {
                StartCoroutine(Block.Palette.PrepareData("1.16", blockLoadFlag, loadStateInfo));
                resourceVersion = "1.16.5";
            }    
            else // TODO Implement More
            {
                Translations.LogError("exception.palette.block");
                loadStateInfo.loggingIn = false;
                yield break;
            }

            while (!blockLoadFlag.done)
                yield return wait;

            // Load texture atlas... TODO (Will be decently implemented in future)
            var atlasLoadFlag = new CoroutineFlag();
            StartCoroutine(AtlasManager.Load(resourceVersion, atlasLoadFlag, loadStateInfo));

            while (!atlasLoadFlag.done)
                yield return wait;
            
            // Load player skin overrides...
            SkinManager.Load();

            // Load resources...
            packManager.ClearPacks();

            ResourcePack pack = new ResourcePack($"vanilla-{resourceVersion}");
            packManager.AddPack(pack);

            // Load valid packs...
            var resLoadFlag = new CoroutineFlag();
            StartCoroutine(packManager.LoadPacks(resLoadFlag, loadStateInfo));

            while (!resLoadFlag.done)
                yield return wait;

            // Preserve camera in login scene for a while
            var loginCamera = Component.FindObjectOfType<Camera>();
            DontDestroyOnLoad(loginCamera.gameObject);

            // Prepare scene and unity objects
            var op = SceneManager.LoadSceneAsync("World", LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9F)
            {
                //Debug.Log("Loading: " + op.progress);
                yield return wait;
            }

            // Scene is loaded, activate it
            op.allowSceneActivation = true;

            // Wait a little bit...
            yield return wait;

            // Find screen control
            screenControl = Component.FindObjectOfType<ScreenControl>();
            var hudScreen = Component.FindObjectOfType<HUDScreen>();
            // Push HUD Screen on start
            screenControl.PushScreen(hudScreen);

            // Create world render
            var worldRenderObj = new GameObject("World Render");
            worldRender = worldRenderObj.AddComponent<WorldRender>();

            // Create entity manager
            var entityManagerObj = new GameObject("Entity Manager");
            entityManager = entityManagerObj.AddComponent<EntityManager>();

            // Create player entity
            var playerPrefab = Resources.Load<GameObject>("Prefabs/Entity/Client Slim Player Entity");
            var playerObj    = GameObject.Instantiate(playerPrefab);
            playerObj.name = $"{session.PlayerName} (Player)";
            playerObj.SetActive(true);
            playerController = playerObj.GetComponent<PlayerController>();

            // Destroy previous camera and create a new one for player
            Destroy(loginCamera.gameObject);
            var cameraPrefab = Resources.Load<GameObject>("Prefabs/Camera");
            var cameraObj    = GameObject.Instantiate(cameraPrefab);
            cameraObj.name = "Main Camera (In-Game)";
            cameraObj.SetActive(true);
            cameraController = cameraObj.GetComponent<CameraController>();
            cameraController.SetTarget(playerObj.transform);
            cameraController.SetPerspective(CurrentPerspective);

            EventManager.Instance.Broadcast<PerspectiveUpdateEvent>(new(CurrentPerspective));

            // Wait a little bit...
            yield return new WaitForSecondsRealtime(0.3F);

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
                tcpClient = ProxyHandler.newTcpClient(host, port);
                tcpClient.ReceiveBufferSize = 1024 * 1024;
                tcpClient.ReceiveTimeout = 30000; // 30 seconds

                Translations.LogError("error.connect");
                Debug.LogError(e.Message);
                Debug.LogError(e.StackTrace);
                Disconnect();
            }
            finally
            {
                loadStateInfo.loggingIn = false;
            }

        }
        #nullable disable

        /// <summary>
        /// Retrieve messages from the queue and send.
        /// Note: requires external locking.
        /// </summary>
        private void TrySendMessageToServer()
        {
            while (chatQueue.Count > 0 && nextMessageSendTime < DateTime.Now)
            {
                string text = chatQueue.Dequeue();
                handler.SendChatMessage(text, playerKeyPair);
                nextMessageSendTime = DateTime.Now + TimeSpan.FromSeconds(CornCraft.MessageCooldown);
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

            if (locationReceived && localLocationUpdated)
            {
                lock (locationLock)
                {
                    playerYaw = _yaw == null ? playerYaw : _yaw.Value;
                    playerPitch = _pitch == null ? playerPitch : _pitch.Value;

                    if (playerController is not null)
                        handler.SendLocationUpdate(location, playerController.IsOnGround, _yaw, _pitch);
                    
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

        #region Disconnect logic

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
                    entityManager?.UnloadEntities();

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
            world.Clear();

            // Go back to login scene
            Loom.QueueOnMainThread(() => {
                SceneManager.LoadScene("Login");
            });

            switch (reason)
            {
                case DisconnectReason.ConnectionLost:
                    StringConvert.Log(Translations.Get("mcc.disconnect.lost"));
                    break;

                case DisconnectReason.InGameKick:
                    StringConvert.Log(Translations.Get("mcc.disconnect.server") + message);
                    break;

                case DisconnectReason.LoginRejected:
                    StringConvert.Log(Translations.Get("mcc.disconnect.login") + message);
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
                InvokeOnNetMainThread(() => HandleCommandPromptText(text));
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
                        Loom.QueueOnMainThread(
                            () => ShowNotification(response.Length <= 35 ? response : $"{response[..32]}...")
                        );
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
                InvokeOnNetMainThread(() => handler.SendAutoCompleteText(text));
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
                else response = Translations.Get("icmd.list", String.Join(", ", cmdNames.ToArray()), CornCraft.internalCmdChar);
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

        public static void LoadInternalCommands()
        {
            /* Load commands from the 'Commands' namespace */

            if (!internalCommandsLoaded)
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
                            cmdNames.Add(cmd.CmdName.ToLower());
                            foreach (string alias in cmd.getCMDAliases())
                                cmds[alias.ToLower()] = cmd;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(e.Message);
                        }
                    }
                }
                internalCommandsLoaded = true;
            }
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

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <param name="inventoryID">Window ID of the requested inventory</param>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container? GetInventory(int inventoryID)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => GetInventory(inventoryID));

            if (inventories.ContainsKey(inventoryID))
                return inventories[inventoryID];
            return null;
        }
        #nullable disable

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container GetPlayerInventory()
        {
            return GetInventory(0)!;
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
            if (String.IsNullOrEmpty(text))
                return;

            int maxLength = handler.GetMaxChatMessageLength();

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

            return handler.SendRespawnPacket();
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified ID
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool SendEntityAction(EntityActionType entityAction)
        {
            return InvokeOnNetMainThread(() => handler.SendEntityAction(playerEntityID, (int)entityAction));
        }

        /// <summary>
        /// Use the item currently in the player's hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool UseItemOnHand()
        {
            return InvokeOnNetMainThread(() => handler.SendUseItem(0, this.sequenceId));
        }

        #nullable enable
        /// <summary>
        /// Try to merge a slot
        /// </summary>
        /// <param name="inventory">The container where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slotId">The ID of the slot of the item to be processed</param>
        /// <param name="curItem">The slot that was put down</param>
        /// <param name="curId">The ID of the slot being put down</param>
        /// <param name="changedSlots">Record changes</param>
        /// <returns>Whether to fully merge</returns>
        private static bool TryMergeSlot(Container inventory, Item item, int slotId, Item curItem, int curId, List<Tuple<short, Item?>> changedSlots)
        {
            int spaceLeft = curItem.Type.StackCount() - curItem.Count;
            if (curItem.Type == item!.Type && spaceLeft > 0)
            {
                // Put item on that stack
                if (item.Count <= spaceLeft)
                {
                    // Can fit into the stack
                    item.Count = 0;
                    curItem.Count += item.Count;

                    changedSlots.Add(new Tuple<short, Item?>((short)curId, curItem));
                    changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));

                    inventory.Items.Remove(slotId);
                    return true;
                }
                else
                {
                    item.Count -= spaceLeft;
                    curItem.Count += spaceLeft;

                    changedSlots.Add(new Tuple<short, Item?>((short)curId, curItem));
                }
            }
            return false;
        }

        /// <summary>
        /// Store items in a new slot
        /// </summary>
        /// <param name="inventory">The container where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slotId">The ID of the slot of the item to be processed</param>
        /// <param name="newSlotId">ID of the new slot</param>
        /// <param name="changedSlots">Record changes</param>
        private static void StoreInNewSlot(Container inventory, Item item, int slotId, int newSlotId, List<Tuple<short, Item?>> changedSlots)
        {
            Item newItem = new(item.Type, item.Count, item.NBT);
            inventory.Items[newSlotId] = newItem;
            inventory.Items.Remove(slotId);

            changedSlots.Add(new Tuple<short, Item?>((short)newSlotId, newItem));
            changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
        }

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            if (InvokeRequired)
                return InvokeOnNetMainThread(() => DoWindowAction(windowId, slotId, action));

            Item? item = null;
            if (inventories.ContainsKey(windowId) && inventories[windowId].Items.ContainsKey(slotId))
                item = inventories[windowId].Items[slotId];

            List<Tuple<short, Item?>> changedSlots = new List<Tuple<short, Item?>>(); // List<Slot ID, Changed Items>

            // Update our inventory base on action type
            var inventory = GetInventory(windowId)!;
            var playerInventory = GetInventory(0)!;
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
                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                            else
                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
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

                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
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
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                        else
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
                        break;
                    case WindowActionType.ShiftClick:
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
                                    else if (item != null && item.Count == 1 && (item.Type == ItemType.NetheriteIngot || 
                                        item.Type == ItemType.Emerald || item.Type == ItemType.Diamond || item.Type == ItemType.GoldIngot || 
                                        item.Type == ItemType.IronIngot) && !inventory.Items.ContainsKey(0))
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
                                    else if (item != null && item.Type == ItemType.BlazePowder)
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
                                    else if (item != null && (item.Type == ItemType.Potion || item.Type == ItemType.GlassBottle))
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
                                    else if (item != null && item.Type == ItemType.LapisLazuli)
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
                                    else if (item != null && item.Type == ItemType.FilledMap)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = upperEndSlot = 0;
                                    }
                                    else if (item != null && item.Type == ItemType.Map)
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
                                default: // TODO: Define more container type here
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
                                    if (inventory.Items.TryGetValue(i, out Item? curItem))
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
                                        changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
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
                                        if (inventory.Items.TryGetValue(i, out Item? curItem))
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
                                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                                    }
                                }
                                else
                                {
                                    int firstEmptySlot = -1;
                                    for (int i = lowerStartSlot; i <= hotbarEnd; ++i)
                                    {
                                        if (inventory.Items.TryGetValue(i, out Item? curItem))
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
                                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                                    }
                                }
                            }
                            else if (backpack2hotbar)
                            {
                                int hotbarEnd = lowerStartSlot + 1 * 9 - 1;

                                int firstEmptySlot = -1;
                                for (int i = lowerStartSlot; i <= hotbarEnd; ++i)
                                {
                                    if (inventory.Items.TryGetValue(i, out Item? curItem))
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
                                        changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                                }
                            }
                        }
                        break;
                    case WindowActionType.DropItem:
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            inventory.Items[slotId].Count--;
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                        }

                        if (inventory.Items[slotId].Count <= 0)
                        {
                            inventory.Items.Remove(slotId);
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
                        }

                        break;
                    case WindowActionType.DropItemStack:
                        inventory.Items.Remove(slotId);
                        changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
                        break;
                }
            }

            return handler.SendWindowAction(windowId, slotId, action, item, changedSlots, inventories[windowId].StateID);
        }
        #nullable disable

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
            return InvokeOnNetMainThread(() => handler.SendCreativeInventoryAction(slot, itemType, count, nbt));
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public bool DoAnimation(int animation)
        {
            return InvokeOnNetMainThread(() => handler.SendAnimation(animation, playerEntityID));
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
            return InvokeOnNetMainThread(() => handler.SendPlayerBlockPlacement((int)hand, location, blockFace, this.sequenceId));
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
                return InvokeOnNetMainThread(() => ChangeSlot(slot));

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
            return InvokeOnNetMainThread(() => handler.SendUpdateSign(location, line1, line2, line3, line4));
        }

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0.</param>
        public bool SelectTrade(int selectedSlot)
        {
            return InvokeOnNetMainThread(() => handler.SelectTrade(selectedSlot));
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
            return InvokeOnNetMainThread(() => handler.UpdateCommandBlock(location, command, mode, flags));
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
            if(GetGamemode() == GameMode.Spectator)
            {
                if(InvokeRequired)
                    return InvokeOnNetMainThread(() => SpectateByUUID(UUID));
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

            world.Clear();
            
            entities.Clear();
            ClearInventories();

            Loom.QueueOnMainThread(() => {
                worldRender.ReloadWorld();
                entityManager.ReloadEntities();
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
                () => EventManager.Instance.Broadcast<ChatMessageEvent>(new(messageText))
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

            Loom.QueueOnMainThread(() => {
                playerController?.SetEntityId(EntityID);
            });
            
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

            Loom.QueueOnMainThread(() => {
                entityManager?.AddEntity(entity);
            });

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
            {
                Loom.QueueOnMainThread(() =>{
                    playerController.GameMode = (GameMode)gamemode;
                });
            }

            // Further regular gamemode change events
            if (onlinePlayers.ContainsKey(uuid))
            {
                string playerName = onlinePlayers[uuid].Name;
                if (playerName == this.username)
                {
                    Loom.QueueOnMainThread(() =>{
                        var newMode = (GameMode)gamemode;
                        playerController.GameMode = newMode;

                        EventManager.Instance.Broadcast<GameModeUpdateEvent>(new(newMode));

                        ShowNotification("Gamemode updated to " + newMode, Notification.Type.Success);
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
                entityManager.RemoveEntities(Entities);
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
                    entityManager.MoveEntity(EntityID, location);
                });
            }

        }

        public void OnEntityRotation(int EntityID, float yaw, float pitch, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                entities[EntityID].Yaw = yaw;
                entities[EntityID].Pitch = pitch;

                Loom.QueueOnMainThread(() => {
                    entityManager.RotateEntity(EntityID, yaw, pitch);
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
                    entityManager.MoveEntity(EntityID, location);
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
        /// <param name="WorldAge"></param>
        /// <param name="TimeOfDay"></param>
        public void OnTimeUpdate(long WorldAge, long TimeOfDay)
        {
            // TimeUpdate sent every server tick hence used as timeout detect
            UpdateKeepAlive();

            this.timeOfDay = TimeOfDay;
            timeElapsedSinceUpdate = 0F;

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
                Translations.Notify("mcc.player_dead", CornCraft.internalCmdChar);

            Loom.QueueOnMainThread(() => {
                EventManager.Instance.Broadcast<HealthUpdateEvent>(new(health));
            });

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

        #nullable enable
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
        public void OnMapData(int mapId, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons, byte colsUpdated, byte rowsUpdated, byte mapColX, byte mapRowZ, byte[]? colors)
        {

        }
        #nullable disable

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

        #nullable enable
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
                if (entity.Type.ContainsItem() && metadata.TryGetValue(7, out object? itemObj) && itemObj != null && itemObj.GetType() == typeof(Item))
                {
                    Item item = (Item)itemObj;
                    if (item == null)
                        entity.Item = new Item(ItemType.Air, 0, null);
                    else entity.Item = item;
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
        #nullable disable

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
