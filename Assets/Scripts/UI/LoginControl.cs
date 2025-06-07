using System;
using System.Linq;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using TMPro;

using CraftSharp.Protocol;
using CraftSharp.Protocol.Handlers.Forge;
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Session;
using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    public class LoginControl : MonoBehaviour
    {
        private const string LOCALHOST_ADDRESS = "127.0.0.1";

        [SerializeField] private TMP_InputField serverInput, usernameInput, authCodeInput;
        [SerializeField] private Button         loginButton, authConfirmButton, authCancelButton;
        [SerializeField] private Button         loginCloseButton, authLinkButton, authCloseButton, localhostButton;
        [SerializeField] private Button         localGameButton, loginPanelButton, quitButton;
        [SerializeField] private TMP_Text       loadStateInfoText, usernameOptions, usernamePlaceholder, authLinkText;
        [SerializeField] private CanvasGroup    loginPanel, usernamePanel, authPanel;
        [SerializeField] private TMP_Dropdown   loginDropDown;

        [SerializeField] private Button         enterGamePanel;

        [SerializeField] private BaseEnvironmentManager environmentManager;
        [SerializeField] private CelestiaBridge celestiaBridge;

        #nullable enable

        private bool preparingGame = false, authenticating = false, authCancelled = false;
        private bool resourceLoaded = false;
        private StartLoginInfo? loginInfo = null;

        #nullable disable

        // Login auto-complete
        private int  usernameIndex =  0;
        private bool namesShown = false;
        
        private string[] cachedOnlineNames = { }, cachedOfflineNames = { }, shownNames = { };

        /// <summary>
        /// Load server information in ServerIP and ServerPort variables from a "serverip:port" couple or server alias
        /// </summary>
        /// <returns>True if the server IP was valid and loaded, false otherwise</returns>
        private static bool ParseServerIP(string server, out string host, out ushort port)
        {
            server = server.ToLower();
            string[] sip = server.Split(':');
            host = sip[0];
            port = 25565;

            if (sip.Length > 1)
            {
                if (sip.Length == 2) // IPv4 with port
                {
                    try
                    {
                        port = Convert.ToUInt16(sip[1]);
                    }
                    catch (FormatException) { return false; }
                }
                else // IPv6 address maybe
                {
                    server = server.TrimStart('[');
                    sip = server.Split(']');
                    host = sip[0];

                    if (sip.Length > 1)
                    {
                        if (sip.Length == 2) // IPv6 with port
                        {
                            try
                            {
                                // Trim ':' before port
                                port = Convert.ToUInt16(sip[1].TrimStart(':'));
                            }
                            catch (FormatException) { return false; }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            if (host == "localhost" || host.Contains('.') || host.Contains(':'))
            {
                // IPv4 addresses or domain names contain at least a dot
                if (sip.Length == 1 && host.Contains('.') && host.Any(char.IsLetter) && ProtocolSettings.ResolveSrvRecords)
                {
                    //Domain name without port may need Minecraft SRV Record lookup
                    ProtocolHandler.MinecraftServiceLookup(ref host, ref port);
                }

                return true;
            }

            return false;
        }

        private void TryConnectDummyServer()
        {
            if (preparingGame)
            {
                CornApp.Notify(Translations.Get("login.logging_in"), Notification.Type.Warning);
                return;
            }
            preparingGame = true;

            const string serverVersionName = "<dummy>";
            var protocolVersion = CornClientOffline.DUMMY_PROTOCOL_VERSION;
            CornApp.Notify(Translations.Get("mcc.server_protocol", serverVersionName, protocolVersion));

            var session = new SessionToken { PlayerName = "OfflinePlayer" };
            var accountLower = CornClientOffline.DUMMY_USERNAME.ToLower();
            // Dummy authentication completed, hide the panel...
            HideLoginPanel();
            // Store current login info
            loginInfo = new StartLoginInfo(false, session, null, "<local>", 0,
                    protocolVersion, null, accountLower);
            StartCoroutine(StoreLoginInfoAndLoadResource(loginInfo));
        }

        private void TryConnectServer()
        {
            if (preparingGame)
            {
                CornApp.Notify(Translations.Get("login.logging_in"), Notification.Type.Warning);
                return;
            }
            preparingGame = true;

            StartCoroutine(ConnectServer());
        }

        private IEnumerator ConnectServer()
        {
            loginInfo = null;

            string serverText = serverInput.text;
            string account = usernameInput.text;
            string accountLower = account.ToLower();

            var session = new SessionToken();
            #nullable enable
            PlayerKeyPair? playerKeyPair = null;
            #nullable disable

            var result = ProtocolHandler.LoginResult.LoginRequired;
            var microsoftLogin = loginDropDown.value == 0; // Dropdown value is 0 if use Microsoft login

            // Login Microsoft/Offline player account
            if (!microsoftLogin)
            {
                if (!PlayerInfo.IsValidName(account))
                {
                    CornApp.Notify(Translations.Get("login.offline_username_invalid"), Notification.Type.Warning);
                    preparingGame = false;
                    loadStateInfoText.text = Translations.Get("login.login_failed");
                    yield break;
                }

                // Enter offline mode
                CornApp.Notify(Translations.Get("mcc.offline"));
                result = ProtocolHandler.LoginResult.Success;
                session.PlayerId = "0";
                session.PlayerName = account;
            }
            else
            {   // Validate cached session or login new session.
                if (ProtocolSettings.SessionCaching != ProtocolSettings.CacheType.None && SessionCache.Contains(accountLower))
                {
                    session = SessionCache.Get(accountLower);
                    result = ProtocolHandler.GetTokenValidation(session);
                    if (result != ProtocolHandler.LoginResult.Success)
                    {
                        Debug.Log(Translations.Get("mcc.session_invalid"));
                        // Try to refresh access token
                        if (!string.IsNullOrWhiteSpace(session.RefreshToken))
                        {
                            try
                            {
                                result = ProtocolHandler.MicrosoftLoginRefresh(session.RefreshToken, out session, ref account);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError("Refresh access token fail: " + e.Message);
                                result = ProtocolHandler.LoginResult.InvalidResponse;
                            }
                        }
                    }
                    else
                        Debug.Log(Translations.Get("mcc.session_valid", session.PlayerName));
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    Debug.Log(Translations.Get("mcc.connecting", "Microsoft"));
                    authenticating = true;

                    // Start brower and open the page...
                    var url = string.IsNullOrEmpty(account) ?
                        Protocol.Microsoft.SignInUrl :
                        Protocol.Microsoft.GetSignInUrlWithHint(account);

                    Protocol.Microsoft.OpenBrowser(url);
                    
                    // Show the browser auth panel...
                    ShowAuthPanel(url);

                    // Wait for the user to proceed or cancel
                    while (authenticating)
                        yield return null;
                    
                    if (authCancelled) // Authentication cancelled by user
                        result = ProtocolHandler.LoginResult.UserCancel;
                    else // Proceed authentication...
                    {
                        var code = authCodeInput.text.Trim();
                        try
                        {
                            result = ProtocolHandler.MicrosoftBrowserLogin(code, out session, ref account);
                        }
                        catch // Authentication failed (code expired or something...)
                        {
                            result = ProtocolHandler.LoginResult.OtherError;
                        }
                    }
                }
            }

            // Proceed to target server
            if (result == ProtocolHandler.LoginResult.Success)
            {
                if (!ParseServerIP(serverText, out var host, out var port) || host is null)
                {
                    CornApp.Notify(Translations.Get("login.server_name_invalid"), Notification.Type.Warning);
                    preparingGame = false;
                    loadStateInfoText.text = Translations.Get("login.login_failed");
                    yield break;
                }

                if (ProtocolSettings.SessionCaching != ProtocolSettings.CacheType.None && session is not null)
                    SessionCache.Store(accountLower, session);

                if (microsoftLogin && ProtocolSettings.LoginWithSecureProfile && session is not null)
                {
                    // Load cached profile key from disk if necessary
                    if (ProtocolSettings.ProfileKeyCaching == ProtocolSettings.CacheType.Disk)
                    {
                        var cacheKeyLoaded = KeysCache.InitializeDiskCache();
                        if (ProtocolSettings.DebugMode)
                            Debug.Log(Translations.Get(cacheKeyLoaded ? "debug.keys_cache_ok" : "debug.keys_cache_fail"));
                    }

                    if (ProtocolSettings.ProfileKeyCaching != ProtocolSettings.CacheType.None && KeysCache.Contains(accountLower))
                    {
                        playerKeyPair = KeysCache.Get(accountLower);
                        Debug.Log(playerKeyPair.NeedRefresh()
                            ? Translations.Get("mcc.profile_key_invalid")
                            : Translations.Get("mcc.profile_key_valid", session.PlayerName));
                    }

                    if (playerKeyPair == null || playerKeyPair.NeedRefresh())
                    {
                        Debug.Log(Translations.Get("mcc.fetching_key"));
                        playerKeyPair = KeyUtils.GetNewProfileKeys(session.Id);
                        if (ProtocolSettings.ProfileKeyCaching != ProtocolSettings.CacheType.None && playerKeyPair != null)
                        {
                            KeysCache.Store(accountLower, playerKeyPair);
                        }
                    }
                }

                if (ProtocolSettings.DebugMode && session is not null)
                    Debug.Log(Translations.Get("debug.session_id", session.Id));

                // Get server version
                Debug.Log(Translations.Get("mcc.retrieve")); // Retrieve server information
                loadStateInfoText.text = Translations.Get("mcc.retrieve");
                int protocolVersion = 0;
                #nullable enable
                ForgeInfo? forgeInfo = null;
                #nullable disable
                string receivedVersionName = string.Empty;

                bool pingResult = false;
                var pingTask = Task.Run(() => {
                    // ReSharper disable once AccessToModifiedClosure
                    pingResult = ProtocolHandler.GetServerInfo(host, port, ref receivedVersionName, ref protocolVersion, ref forgeInfo);
                });

                while (!pingTask.IsCompleted) yield return null;

                if (!pingResult)
                {
                    CornApp.Notify(Translations.Get("error.ping"), Notification.Type.Error);
                    preparingGame = false;
                    loadStateInfoText.text = Translations.Get("login.login_failed");
                    yield break;
                }
                
                CornApp.Notify(Translations.Get("mcc.server_protocol", receivedVersionName, protocolVersion));

                if (protocolVersion != 0 && session is not null) // Load corresponding data
                {
                    if (ProtocolHandler.IsProtocolSupported(protocolVersion))
                    {
                        // Authentication completed, hide the panel...
                        HideLoginPanel();
                        // Store current login info
                        loginInfo = new StartLoginInfo(true, session, playerKeyPair, host, port,
                                protocolVersion, null, accountLower);
                        // No need to yield return this coroutine because it's the last step here
                        StartCoroutine(StoreLoginInfoAndLoadResource(loginInfo));
                    }
                    else
                    {
                        int minSupported = ProtocolHandler.GetMinSupported();
                        int maxSupported = ProtocolHandler.GetMaxSupported();

                        if (protocolVersion > maxSupported || protocolVersion < minSupported)
                        {
                            // This version is not directly supported, yet might
                            // still be joinable if ViaBackwards' installed

                            protocolVersion = protocolVersion > maxSupported ? maxSupported : minSupported; // Try our luck

                            // Authentication completed, hide the panel...
                            HideLoginPanel();
                            // Store current login info
                            loginInfo = new StartLoginInfo(true, session, playerKeyPair, host, port,
                                    protocolVersion, null, accountLower);
                            // Display a notification
                            var altMcVersion = ProtocolHandler.ProtocolVersion2MCVer(protocolVersion);
                            CornApp.Notify($"Using alternative version {altMcVersion} (protocol v{protocolVersion})", Notification.Type.Warning);
                            // No need to yield return this coroutine because it's the last step here
                            StartCoroutine(StoreLoginInfoAndLoadResource(loginInfo));
                        }
                        else
                        {
                            CornApp.Notify(Translations.Get("error.unsupported"), Notification.Type.Error);
                            preparingGame = false;
                        }
                    }
                }
                else // Unable to determine server version
                {
                    CornApp.Notify(Translations.Get("error.determine"), Notification.Type.Error);
                    preparingGame = false;
                }
            }
            else
            {
                var failureMessage = Translations.Get("error.login");
                var failureReason = result switch
                {
                    ProtocolHandler.LoginResult.AccountMigrated      => "error.login.migrated",
                    ProtocolHandler.LoginResult.ServiceUnavailable   => "error.login.server",
                    ProtocolHandler.LoginResult.WrongPassword        => "error.login.blocked",
                    ProtocolHandler.LoginResult.InvalidResponse      => "error.login.response",
                    ProtocolHandler.LoginResult.NotPremium           => "error.login.premium",
                    ProtocolHandler.LoginResult.OtherError           => "error.login.network",
                    ProtocolHandler.LoginResult.SSLError             => "error.login.ssl",
                    ProtocolHandler.LoginResult.UserCancel           => "error.login.cancel",
                    _                                                => "error.login.unknown"
                };
                failureMessage += Translations.Get(failureReason);
                loadStateInfoText.text = Translations.Get("login.login_failed");
                CornApp.Notify(failureMessage, Notification.Type.Error);

                if (result == ProtocolHandler.LoginResult.SSLError)
                    CornApp.Notify(Translations.Get("error.login.ssl_help"), Notification.Type.Error);
                
                Debug.LogError(failureMessage);

                preparingGame = false;
            }
        }

        private IEnumerator StoreLoginInfoAndLoadResource(StartLoginInfo info)
        {
            loginInfo = info;

            var resLoadFlag = new DataLoadFlag();
            yield return StartCoroutine(CornApp.Instance.PrepareDataAndResource(info.ProtocolVersion,
                resLoadFlag, (status, progress) => loadStateInfoText.text = Translations.Get(status) + progress));
            
            if (resLoadFlag.Failed)
            {
                resourceLoaded = false;
                Debug.LogWarning("Resource load failed");
            }
            else
            {
                resourceLoaded = true;
                yield return StartCoroutine(celestiaBridge.StopAndMakePortal(() =>
                {
                    // Set enter game panel to active
                    enterGamePanel.gameObject.SetActive(true);
                    loadStateInfoText.text = Translations.Get("login.click_to_enter");
                }));
            }
        }

        private void ShowLoginPanel()
        {   // Show login panel and hide button
            loginPanel.alpha = 1F;
            loginPanel.blocksRaycasts = true;
            loginPanel.interactable = true;
            loginPanelButton.gameObject.SetActive(false);
            loginPanelButton.interactable = false;
        }

        private void HideLoginPanel()
        {   // Hide login panel and show button
            loginPanel.alpha = 0F;
            loginPanel.blocksRaycasts = false;
            loginPanel.interactable = false;
            loginPanelButton.gameObject.SetActive(true);
            loginPanelButton.interactable = true;
        }

        private void RefreshUsernames()
        {
            if (shownNames.Length > 0) // Show login candidates
            {
                StringBuilder str = new();
                for (int i = 0;i < shownNames.Length;i++)
                {
                    str.Append(
                        i == usernameIndex ? $"<color=yellow>{shownNames[i]}</color>" : shownNames[i]
                    ).Append('\n');
                }
                usernameOptions.text = str.ToString();
                usernamePanel.alpha = 1F;
                namesShown         = true;
            }
            else // Hide them away...
            {
                usernamePanel.alpha = 0F;
                namesShown        = false;
            }
        }

        private void UpdateUsernamePanel(string input)
        {
            var microsoftLogin = loginDropDown.value == 0;
            var cachedNames = microsoftLogin ? cachedOnlineNames : cachedOfflineNames;
            
            shownNames = cachedNames.Where(
                    login => login != input && login.Contains(input)).ToArray();

            RefreshUsernames();
        }

        private void HideUsernamePanel(string message)
        {
            usernameIndex = 0;
            usernamePanel.alpha = 0F;
            namesShown = false;
        }
        
        private void UpdateUsernameDefault()
        {
            var microsoftLogin = loginDropDown.value == 0;
            var cachedNames = microsoftLogin ? cachedOnlineNames : cachedOfflineNames;
            
            usernameInput.text = cachedNames.Length > 0 ? cachedNames[0] : string.Empty;
        }

        private void UpdateUsernamePlaceholder(int selection)
        {
            usernamePlaceholder.text = selection == 0 ? "Email Address" : "User Name";
        }

        private void CopyAuthLink()
        {
            GUIUtility.systemCopyBuffer = authLinkText.text;
            CornApp.Notify(Translations.Get("login.link_copied"), Notification.Type.Success);
        }

        private void PasteAuthCode()
        {
            authCodeInput.text = GUIUtility.systemCopyBuffer;
        }

        private void ShowAuthPanel(string url)
        {
            // Update auth panel link text
            authLinkText.text = url;

            // Clear existing text if any
            authCodeInput.text = string.Empty;

            authPanel.alpha = 1F;
            authPanel.blocksRaycasts = true;
            authPanel.interactable = true;

            authenticating = true;
            authCancelled = false;
        }

        private void HideAuthPanel()
        {
            authPanel.alpha = 0F;
            authPanel.blocksRaycasts = false;
            authPanel.interactable = false;

            authenticating = false;
        }

        private void CancelAuth()
        {
            authCancelled = true;
            HideAuthPanel();
        }

        private void ConfirmAuth()
        {
            var code = authCodeInput.text.Trim();

            if (string.IsNullOrEmpty(code))
            {
                CornApp.Notify(Translations.Get("login.auth_code_empty"), Notification.Type.Warning);
                return;
            }

            authCancelled = false;

            HideAuthPanel();
        }

        
        private IEnumerator UpdateLoginMode(int selection)
        {
            UpdateUsernamePlaceholder(selection);
            UpdateUsernameDefault();
            
            // Workaround for UGUI EventSystem click event bug, if the option being clicked is above the login button,
            // it'll trigger a null object exception every frame until the mouse pointer is moved out from the button area
            loginButton.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.2F);
            loginButton.gameObject.SetActive(true);
        }

        private IEnumerator EnterGame()
        {
            if (resourceLoaded && loginInfo is not null)
            {
                enterGamePanel.gameObject.SetActive(false); // Disable this panel after click

                celestiaBridge.EnterPortal();

                yield return new WaitForSecondsRealtime(2F);

                // We cannot directly use StartCoroutine to call StartLogin here, which will stop running when
                // this scene is unloaded and LoginControl object is destroyed
                CornApp.Instance.StartLoginCoroutine(loginInfo, _ => preparingGame = false,
                        status => loadStateInfoText.text = Translations.Get(status));
            }
        }

        private static void QuitGame()
        {
            Application.Quit();
        }

        private void Start()
        {
            // Set camera for environment manager
            if (environmentManager != null)
            {
                environmentManager.SetCamera(GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>());
            }

            // Generate default data or check update (Need to be done with priority because it contains translation texts)
            var extraDataDir = PathHelper.GetExtraDataDirectory();
            var builtinResLoad = BuiltinResourceHelper.ReadyBuiltinResource(
                    CornApp.CORN_CRAFT_BUILTIN_FILE_NAME, CornApp.CORN_CRAFT_BUILTIN_VERSION, extraDataDir,
                    _ => { }, () => { }, _ => { });
            
            while (builtinResLoad.MoveNext()) { /* Do nothing */ }
            
            // Initialize controls
            usernameOptions.text = string.Empty;
            usernamePanel.alpha  = 0F; // Hide at start
            enterGamePanel.gameObject.SetActive(false);
            enterGamePanel.onClick.AddListener(() => StartCoroutine(EnterGame()));
            loginDropDown.onValueChanged.AddListener(selection => StartCoroutine(UpdateLoginMode(selection)));

            //Load cached sessions from disk if necessary
            if (ProtocolSettings.SessionCaching == ProtocolSettings.CacheType.Disk)
            {
                var cacheLoaded = SessionCache.InitializeDiskCache();
                if (ProtocolSettings.DebugMode)
                    Debug.Log(Translations.Get(cacheLoaded ? "debug.session_cache_ok" : "debug.session_cache_fail"));

                if (cacheLoaded)
                {
                    cachedOnlineNames = SessionCache.GetCachedOnlineLogins();
                    cachedOfflineNames = SessionCache.GetCachedOfflineLogins();
                }
            }

            // TODO: Also initialize server with cached values
            serverInput.text = LOCALHOST_ADDRESS;
            UpdateUsernameDefault();
            
            loginDropDown.value = 0; // Online by default
            UpdateUsernamePlaceholder(0);
            if (cachedOnlineNames.Length > 0)
                usernameInput.text = cachedOnlineNames[0];

            // Prepare panels at start
            ShowLoginPanel();
            HideAuthPanel();

            // Add listeners
            localhostButton.onClick.AddListener(() => serverInput.text = LOCALHOST_ADDRESS);

            usernameInput.onValueChanged.AddListener(UpdateUsernamePanel);
            usernameInput.onSelect.AddListener(UpdateUsernamePanel);
            usernameInput.onEndEdit.AddListener(HideUsernamePanel);

            localGameButton.onClick.AddListener(TryConnectDummyServer);
            quitButton.onClick.AddListener(QuitGame);

            loginButton.onClick.AddListener(TryConnectServer);
            authLinkButton.onClick.AddListener(CopyAuthLink);
            authCancelButton.onClick.AddListener(CancelAuth);
            authConfirmButton.onClick.AddListener(ConfirmAuth);
            authCodeInput.GetComponentInChildren<Button>().onClick.AddListener(PasteAuthCode);

            loginCloseButton.onClick.AddListener(HideLoginPanel);
            loginPanelButton.onClick.AddListener(ShowLoginPanel);
            // Cancel auth, not just hide panel (so basically this button is totally the same as authCancelButton)...
            authCloseButton.onClick.AddListener(CancelAuth);

            // Used for testing MC format code parsing
            // loadStateInfoText.text = StringConvert.MC2TMP("Hello world §a[§a§a-1, §a1 §6[Bl§b[HHH]ah] Hello §c[Color RE§rD]  §a1§r] (blah)");
            loadStateInfoText.text = $"CornCraft {ProtocolSettings.Version} Powered by <u>Minecraft Console Client</u>";

            // Release cursor (Useful when re-entering login scene from game)
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            if (namesShown)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    usernameIndex = (usernameIndex + 1) % shownNames.Length;
                    RefreshUsernames();
                }
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    usernameIndex = (usernameIndex + shownNames.Length - 1) % shownNames.Length;
                    RefreshUsernames();
                }

                if (Keyboard.current.tabKey.wasPressedThisFrame) // Tab-complete
                {
                    if (usernameIndex >= 0 && usernameIndex < shownNames.Length)
                    {
                        usernameInput.text = shownNames[usernameIndex];
                        usernameInput.MoveTextEnd(false);
                    }
                }
            }
        }
    }
}
