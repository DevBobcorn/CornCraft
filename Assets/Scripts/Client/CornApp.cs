#nullable enable
using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;

using CraftSharp.Event;
using CraftSharp.Interaction;
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Handlers.Forge;
using CraftSharp.Protocol.Session;
using CraftSharp.Resource;
using CraftSharp.UI;

namespace CraftSharp
{
    public class CornApp : MonoBehaviour
    {
        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;

        private BaseCornClient? client = null;
        public BaseCornClient? Client => client;

        public static BaseCornClient? CurrentClient => Instance.Client;
        public static void SetCurrentClient(BaseCornClient c) => Instance.client = c;

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

                    // Check entity data version override
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
                Notify("Data for gameplay is not available!", Notification.Type.Error);
                updateStatus(">_<");
                startUpFlag.Failed = true;
                yield break;
            }

            // Load in-game translations
            var s = Path.DirectorySeparatorChar;
            var langFile = PathHelper.GetPackDirectoryNamed(
                    $"vanilla-{resourceVersion}{s}assets{s}minecraft{s}lang{s}{CornGlobal.Language}.json");
            
            Protocol.ChatParser.LoadTranslationRules(langFile);

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

            var packManager = ResourcePackManager.Instance;

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
                    Notify("Failed to download base resource pack!", Notification.Type.Error);
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
                Notify("Base resource pack is invalid!", Notification.Type.Error);
                updateStatus(">_<");
                startUpFlag.Failed = true;
                yield break;
            }
            // Then append overrides
            packManager.AddPack(new("vanilla_fix"));
            //packManager.AddPack(new("VanillaBDCraft 64x MC116"));
            // Load valid packs...
            loadFlag.Finished = false;
            // Load valid packs...
            loadFlag.Finished = false;
            Task.Run(() => packManager.LoadPacks(loadFlag, (status) => Loom.QueueOnMainThread(() => updateStatus(status))));
            while (!loadFlag.Finished) yield return null;
            
            // Load entity definitions
            loadFlag.Finished = false;
            Task.Run(() => EntityPalette.INSTANCE.PrepareData(entityVersion, loadFlag));
            while (!loadFlag.Finished) yield return null;

            if (loadFlag.Failed) // Cancel login if resources are not properly loaded
            {
                Notify("Failed to load all resources!", Notification.Type.Error);
                updateStatus(">_<");
                startUpFlag.Failed = true;
                yield break;
            }
        }

        private IEnumerator EnterWorldScene(string sceneName)
        {
            // Prepare scene and unity objects
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9F) yield return null;

            // Scene is loaded, activate it
            op.allowSceneActivation = true;
            bool fullyLoaded = false;

            // Wait till everything's ready
            op.completed += (operation) =>
            {
                client = Component.FindObjectOfType<BaseCornClient>();
                fullyLoaded = true;
            };

            while (!fullyLoaded) yield return null;
        }

        public void StartLoginCoroutine(bool online, SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp, ushort port,
                int protocol, ForgeInfo? forgeInfo, Action<bool> callback, Action<string> updateStatus, string accountLower)
        {
            StartCoroutine(StartLogin(online, session, playerKeyPair, serverIp, port, protocol, forgeInfo, callback, updateStatus, accountLower));
        }

        private IEnumerator StartLogin(bool online, SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp, ushort port,
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
            yield return EnterWorldScene(online ? "World" : "World Offline");

            // Start client
            if (client != null)
            {
                var succeeded = client!.StartClient(session, playerKeyPair, serverIp, port, protocol, forgeInfo, accountLower);
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
        public static void Notify(string notification) => EventManager.Instance.Broadcast<NotificationEvent>(new(notification));

        public static void Notify(string notification, Notification.Type type) => EventManager.Instance.Broadcast<NotificationEvent>(new(notification, 6F, type));

        public static void Notify(string notification, float duration, Notification.Type type) => EventManager.Instance.Broadcast<NotificationEvent>(new(notification, duration, type));


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