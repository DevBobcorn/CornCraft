#nullable enable
using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;

using MinecraftClient.Event;
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Resource;
using MinecraftClient.UI;
using MinecraftClient.Mapping;


namespace MinecraftClient
{
    public class CornApp : MonoBehaviour
    {
        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;

        private CornClient? client = null;
        public CornClient? Client => client;

        public static CornClient? CurrentClient => Instance.Client;

        private static CornApp? instance;
        public static CornApp Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                
                var magic = new GameObject("Corn Craft");
                GameObject.DontDestroyOnLoad(magic);
                return instance = magic.AddComponent<CornApp>();
            }
        }

        private readonly ResourcePackManager packManager = new ResourcePackManager();
        public static ResourcePackManager ActivePackManager => Instance.packManager;

        // Runs before a scene gets loaded
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeApp()
        {
            Loom.Initialize();

            // Ensure CornApp instance is created
            var i = Instance;
        }

        private IEnumerator PrepareDataAndResource(int protocolVersion, DataLoadFlag startUpFlag, Action<string> updateStatus)
        {
            var versionDictPath = PathHelper.GetExtraDataFile("versions.json");

            var dataVersion     = string.Empty;
            var entityVersion   = string.Empty;
            var resourceVersion = string.Empty;

            try
            {
                // Read data version dictionary
                var versions = Json.ParseJson(File.ReadAllText(versionDictPath, Encoding.UTF8));
                var version = protocolVersion.ToString();

                if (versions.Properties.ContainsKey(version))
                {
                    var entries = versions.Properties[version].Properties;

                    dataVersion = entries["data"].StringValue;
                    if (entries.ContainsKey("entity"))
                        entityVersion = entries["entity"].StringValue;
                    else
                        entityVersion = dataVersion;
                    resourceVersion = entries["resource"].StringValue;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Data for protocol version {protocolVersion} is not available: {e.Message}");
                ShowNotification("Data for gameplay is not available!", Notification.Type.Error);
                updateStatus(">_<");
                startUpFlag.Failed = true;
                yield break;
            }

            // First load all possible Block States...
            var loadFlag = new DataLoadFlag();
            Task.Run(() => BlockStatePalette.INSTANCE.PrepareData(dataVersion, loadFlag));
            while (!loadFlag.Finished) yield return null;

            // Then load all Items...
            loadFlag.Finished = false;
            Task.Run(() => ItemPalette.INSTANCE.PrepareData(dataVersion, loadFlag));
            while (!loadFlag.Finished)
                yield return null;
            
            loadFlag.Finished = false;
            Task.Run(() => BlockInteractionManager.INSTANCE.PrepareData(loadFlag));
            while (!loadFlag.Finished) yield return null;

            // Load resource packs...
            packManager.ClearPacks();
            // Download base pack if not present
            if (!Directory.Exists(PathHelper.GetPackDirectoryNamed($"vanilla-{resourceVersion}"))) // Prepare resources first
            {
                Debug.Log($"Resources for {resourceVersion} not present. Downloading...");
                bool downloadSucceeded = false;
                yield return StartCoroutine(ResourceDownloader.DownloadResource(resourceVersion,
                        updateStatus, () => { },
                        (succeeded) => downloadSucceeded = succeeded));
                
                if (!downloadSucceeded)
                {
                    ShowNotification("Failed to download base resource pack!", Notification.Type.Error);
                    updateStatus(">_<");
                    startUpFlag.Failed = true;
                    yield break;
                }
            }
            // First add base resources
            ResourcePack basePack = new($"vanilla-{resourceVersion}");
            packManager.AddPack(basePack);
            // Check base pack availability
            if (!basePack.IsValid)
            {
                ShowNotification("Base resource pack is invalid!", Notification.Type.Error);
                updateStatus(">_<");
                startUpFlag.Failed = true;
                yield break;
            }
            // Then append overrides
            packManager.AddPack(new("vanilla_fix"));
            // Load valid packs...
            loadFlag.Finished = false;
            // Load valid packs...
            loadFlag.Finished = false;
            Task.Run(() => packManager.LoadPacks(loadFlag, (status) => Loom.QueueOnMainThread(() => updateStatus(status))));
            while (!loadFlag.Finished) yield return null;

            // Load biome definitions (After colormaps in resource packs are loaded) (on main thread)...
            yield return BiomePalette.INSTANCE.PrepareData(dataVersion, $"vanilla-{resourceVersion}", loadFlag);
            
            // Load entity definitions
            loadFlag.Finished = false;
            Task.Run(() => EntityPalette.INSTANCE.PrepareData(entityVersion, loadFlag));
            while (!loadFlag.Finished) yield return null;

            if (loadFlag.Failed) // Cancel login if resources are not properly loaded
            {
                ShowNotification("Failed to load all resources!", Notification.Type.Error);
                updateStatus(">_<");
                startUpFlag.Failed = true;
                yield break;
            }
        }

        private IEnumerator EnterWorldScene()
        {
            // Prepare scene and unity objects
            var op = SceneManager.LoadSceneAsync("World", LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9F) yield return null;

            // Scene is loaded, activate it
            op.allowSceneActivation = true;
            bool fullyLoaded = false;

            // Wait till everything's ready
            op.completed += (operation) =>
            {
                client = Component.FindObjectOfType<CornClient>();
                fullyLoaded = true;
            };

            while (!fullyLoaded) yield return null;
        }

        public void StartLoginCoroutine(SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp, ushort port,
                int protocol, ForgeInfo? forgeInfo, Action<bool> callback, Action<string> updateStatus, string accountLower)
        {
            StartCoroutine(StartLogin(session, playerKeyPair, serverIp, port, protocol, forgeInfo, callback, updateStatus, accountLower));
        }

        private IEnumerator StartLogin(SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp, ushort port,
                int protocol, ForgeInfo? forgeInfo, Action<bool> callback, Action<string> updateStatus, string accountLower)
        {
            // Prepare resources
            var dataLoadFlag = new DataLoadFlag();
            yield return PrepareDataAndResource(protocol, dataLoadFlag, updateStatus);
            if (dataLoadFlag.Failed)
            {
                callback(false);
                yield break;
            }

            // Clear client value
            client = null;

            // Enter world scene, and find the client instance in that scene
            updateStatus("status.info.enter_world_scene");
            yield return EnterWorldScene();

            // Start client
            if (client != null)
            {
                var succeeded = client!.StartClient(session, playerKeyPair, serverIp, port, protocol, forgeInfo, updateStatus, accountLower);
                callback(succeeded);
            }
            else // Failed to find client instance in scene
            {
                callback(false);
                BackToLogin();
            }

        }

        /// <summary>
        /// Should only be externally called by CornClient
        /// </summary>
        public void BackToLogin() => SceneManager.LoadScene("Login");

        // Should be called from the Unity thread only, not net read thread
        public static void ShowNotification(string notification) => EventManager.Instance.Broadcast<NotificationEvent>(new(notification));

        public static void ShowNotification(string notification, Notification.Type type) => EventManager.Instance.Broadcast<NotificationEvent>(new(notification, 6F, type));

        public static void ShowNotification(string notification, float duration, Notification.Type type) => EventManager.Instance.Broadcast<NotificationEvent>(new(notification, duration, type));


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
        }
    }
}