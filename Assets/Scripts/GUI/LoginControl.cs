#nullable enable
using System;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
using System.Threading.Tasks;

namespace MinecraftClient.UI
{
    public class LoginControl : MonoBehaviour
    {
        private const string LOCALHOST_ADDRESS = "127.0.0.1";

        [SerializeField] private TMP_InputField? serverInput, usernameInput, authCodeInput;
        [SerializeField] private Button?         loginButton, quitButton, authConfirmButton, authCancelButton;
        [SerializeField] private Button?         loginCloseButton, authLinkButton, authCloseButton, localhostButton;
        [SerializeField] private TMP_Text?       loadStateInfoText, usernameOptions, usernamePlaceholder, authLinkText;
        [SerializeField] private CanvasGroup?    loginPanel, usernamePanel, authPanel, loginPanelButton;
        [SerializeField] private TMP_Dropdown?   loginDropDown;

        private bool tryingConnect = false, authenticating = false, authCancelled = false;

        // Login auto-complete
        int  usernameIndex =  0;
        bool namesShown = false;
        private string[] cachedNames = { }, shownNames = { };

        /// <summary>
        /// Load server information in ServerIP and ServerPort variables from a "serverip:port" couple or server alias
        /// </summary>
        /// <returns>True if the server IP was valid and loaded, false otherwise</returns>
        public bool ParseServerIP(string server, out string host, ref ushort port)
        {
            server = server.ToLower();
            string[] sip = server.Split(':');
            host = sip[0];
            port = 25565;

            if (sip.Length > 1)
            {
                try
                {
                    port = Convert.ToUInt16(sip[1]);
                }
                catch (FormatException) { return false; }
            }

            if (host == "localhost" || host.Contains('.'))
            {
                // Server IP (IP or domain names contains at least a dot)
                if (sip.Length == 1 && host.Contains('.') && host.Any(c => char.IsLetter(c)) && CornGlobal.ResolveSrvRecords)
                    //Domain name without port may need Minecraft SRV Record lookup
                    ProtocolHandler.MinecraftServiceLookup(ref host, ref port);
                return true;
            }

            return false;
        }
        
        public void TryConnectServer()
        {
            if (tryingConnect)
            {
                CornApp.Notify("Already loggin' in!", Notification.Type.Warning);
                return;
            }

            tryingConnect = true;

            StartCoroutine(ConnectServer());
        }

        public IEnumerator ConnectServer()
        {
            string serverText = serverInput!.text;
            string account = usernameInput!.text;
            string accountLower = account.ToLower();

            SessionToken session = new SessionToken();
            PlayerKeyPair? playerKeyPair = null;

            var result = ProtocolHandler.LoginResult.LoginRequired;
            var microsoftLogin = loginDropDown!.value == 0; // Dropdown value is 0 if use Microsoft login

            if (!microsoftLogin)
            {
                if (!StringHelper.IsValidName(account))
                {
                    CornApp.Notify("The offline username is not valid!", Notification.Type.Warning);
                    tryingConnect = false;
                    loadStateInfoText!.text = ">_<";
                    yield break;
                }

                // Enter offline mode
                CornApp.Notify(Translations.Get("mcc.offline"));
                result = ProtocolHandler.LoginResult.Success;
                session.PlayerID = "0";
                session.PlayerName = account;
            }
            else
            {   // Validate cached session or login new session.
                if (CornGlobal.SessionCaching != CacheType.None && SessionCache.Contains(accountLower))
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
                        var code = authCodeInput!.text.Trim();
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

            if (result == ProtocolHandler.LoginResult.Success)
            {
                string host;   // Server ip address
                ushort port = 25565; // Server port

                if (!ParseServerIP(serverText, out host, ref port))
                {
                    CornApp.Notify("Failed to parse server name or address!", Notification.Type.Warning);
                    tryingConnect = false;
                    loadStateInfoText!.text = ">_<";
                    yield break;
                }

                if (CornGlobal.SessionCaching != CacheType.None)
                    SessionCache.Store(accountLower, session);

                if (microsoftLogin && CornGlobal.LoginWithSecureProfile)
                {
                    // Load cached profile key from disk if necessary
                    if (CornGlobal.ProfileKeyCaching == CacheType.Disk)
                    {
                        bool cacheKeyLoaded = KeysCache.InitializeDiskCache();
                        if (CornGlobal.DebugMode)
                            Debug.Log(Translations.Get(cacheKeyLoaded ? "debug.keys_cache_ok" : "debug.keys_cache_fail"));
                    }

                    if (CornGlobal.ProfileKeyCaching != CacheType.None && KeysCache.Contains(accountLower))
                    {
                        playerKeyPair = KeysCache.Get(accountLower);
                        if (playerKeyPair.NeedRefresh())
                            Debug.Log(Translations.Get("mcc.profile_key_invalid"));
                        else
                            Debug.Log(Translations.Get("mcc.profile_key_valid", session.PlayerName));
                    }

                    if (playerKeyPair == null || playerKeyPair.NeedRefresh())
                    {
                        Debug.Log(Translations.Get("mcc.fetching_key"));
                        playerKeyPair = KeyUtils.GetNewProfileKeys(session.ID);
                        if (CornGlobal.ProfileKeyCaching != CacheType.None && playerKeyPair != null)
                        {
                            KeysCache.Store(accountLower, playerKeyPair);
                        }
                    }
                }

                if (CornGlobal.DebugMode)
                    Debug.Log(Translations.Get("debug.session_id", session.ID));

                // Get server version
                Debug.Log(Translations.Get("mcc.retrieve")); // Retrieve server information
                loadStateInfoText!.text = Translations.Get("mcc.retrieve");
                int protocolVersion = 0;
                ForgeInfo? forgeInfo = null;
                string receivedVersionName = string.Empty;

                bool pingResult = false;
                var pingTask = Task.Run(() => {
                    pingResult = ProtocolHandler.GetServerInfo(host, port, ref receivedVersionName, ref protocolVersion, ref forgeInfo);
                });

                while (!pingTask.IsCompleted) yield return null;

                if (!pingResult)
                {
                    CornApp.Notify(Translations.Get("error.ping"), Notification.Type.Error);
                    tryingConnect = false;
                    loadStateInfoText!.text = ">_<";
                    yield break;
                }
                else
                {
                    CornApp.Notify(Translations.Get("mcc.server_protocol", receivedVersionName, protocolVersion));
                }

                if (protocolVersion != 0) // Proceed to server login
                {
                    if (Protocol.ProtocolHandler.IsProtocolSupported(protocolVersion))
                    {
                        // Authentication completed, hide the panel...
                        HideLoginPanel();

                        // We cannot directly use StartCoroutine to call StartLogin here, which will stop running when
                        // this scene is unloaded and LoginControl object is destroyed
                        CornApp.Instance.StartLoginCoroutine(session, playerKeyPair, host, port, protocolVersion, null,
                                (succeeded) => tryingConnect = false,
                                (status) => loadStateInfoText!.text = status, accountLower);

                        yield break;
                    }
                    else
                        CornApp.Notify(Translations.Get("error.unsupported"), Notification.Type.Error);
                }
                else // Unable to determine server version
                    CornApp.Notify(Translations.Get("error.determine"), Notification.Type.Error);
            }
            else
            {
                string failureMessage = Translations.Get("error.login");
                string failureReason = result switch
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
                loadStateInfoText!.text = ">_<";
                CornApp.Notify(failureMessage, Notification.Type.Error);

                if (result == ProtocolHandler.LoginResult.SSLError)
                    CornApp.Notify(Translations.Get("error.login.ssl_help"), Notification.Type.Error);
                
                Debug.LogError(failureMessage);
            }

            tryingConnect = false;
        }

        public void ShowLoginPanel()
        {   // Show login panel and hide button
            loginPanel!.alpha = 1F;
            loginPanel!.blocksRaycasts = true;
            loginPanel!.interactable = true;
            loginPanelButton!.alpha = 0F;
            loginPanelButton!.blocksRaycasts = false;
            loginPanelButton!.interactable = false;
        }

        public void HideLoginPanel()
        {   // Hide login panel and show button
            loginPanel!.alpha = 0F;
            loginPanel!.blocksRaycasts = false;
            loginPanel!.interactable = false;
            loginPanelButton!.alpha = 1F;
            loginPanelButton!.blocksRaycasts = true;
            loginPanelButton!.interactable = true;
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
                usernameOptions!.text = str.ToString();
                usernamePanel!.alpha = 1F;
                namesShown         = true;
            }
            else // Hide them away...
            {
                usernamePanel!.alpha = 0F;
                namesShown        = false;
            }
        }

        public void UpdateUsernamePanel(string message)
        {
            shownNames = cachedNames.Where(
                    (login) => login != message && login.Contains(message)).ToArray();

            RefreshUsernames();

        }

        public void HideUsernamePanel(string message)
        {
            usernameIndex = 0;
            usernamePanel!.alpha = 0F;
            namesShown        = false;
        }

        public void UpdateUsernamePlaceholder(int selection)
        {
            usernamePlaceholder!.text = selection == 0 ? "Email Address" : "User Name";
        }

        public void CopyAuthLink()
        {
            GUIUtility.systemCopyBuffer = authLinkText!.text;
            CornApp.Notify("Link copied to clipboard.", Notification.Type.Success);
        }

        public void PasteAuthCode()
        {
            authCodeInput!.text = GUIUtility.systemCopyBuffer;
        }

        private void ShowAuthPanel(string url)
        {
            // Update auth panel link text
            authLinkText!.text = url;

            // Clear existing text if any
            authCodeInput!.text = string.Empty;

            authPanel!.alpha = 1F;
            authPanel!.blocksRaycasts = true;
            authPanel!.interactable = true;

            authenticating = true;
            authCancelled = false;
        }

        private void HideAuthPanel()
        {
            authPanel!.alpha = 0F;
            authPanel!.blocksRaycasts = false;
            authPanel!.interactable = false;

            authenticating = false;
        }

        public void CancelAuth()
        {
            authCancelled = true;
            HideAuthPanel();
        }

        public void ConfirmAuth()
        {
            var code = authCodeInput!.text.Trim();

            if (String.IsNullOrEmpty(code))
            {
                CornApp.Notify("Authentication code is empty!", Notification.Type.Warning);
                return;
            }

            authCancelled = false;

            HideAuthPanel();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        void Start()
        {
            // Initialize controls
            usernameOptions!.text = string.Empty;
            usernamePanel!.alpha  = 0F; // Hide at start

            loginDropDown!.onValueChanged.AddListener(this.UpdateUsernamePlaceholder);

            //Load cached sessions from disk if necessary
            if (CornGlobal.SessionCaching == CacheType.Disk)
            {
                bool cacheLoaded = SessionCache.InitializeDiskCache();
                if (CornGlobal.DebugMode)
                    Debug.Log(Translations.Get(cacheLoaded ? "debug.session_cache_ok" : "debug.session_cache_fail"));
                
                if (cacheLoaded)
                    cachedNames = SessionCache.GetCachedLogins();
            }

            // TODO Also initialize server with cached values
            serverInput!.text = LOCALHOST_ADDRESS;
            if (cachedNames.Length > 0)
                usernameInput!.text = cachedNames[0];
            
            loginDropDown.value = 0;
            UpdateUsernamePlaceholder(0);

            // Prepare panels at start
            ShowLoginPanel();
            HideAuthPanel();

            // Add listeners
            localhostButton!.onClick.AddListener(() => serverInput.text = LOCALHOST_ADDRESS);

            usernameInput!.onValueChanged.AddListener(this.UpdateUsernamePanel);
            usernameInput.onSelect.AddListener(this.UpdateUsernamePanel);
            usernameInput.onEndEdit.AddListener(this.HideUsernamePanel);

            loginButton!.onClick.AddListener(this.TryConnectServer);
            quitButton!.onClick.AddListener(this.QuitGame);

            authLinkButton!.onClick.AddListener(this.CopyAuthLink);
            authCancelButton!.onClick.AddListener(this.CancelAuth);
            authConfirmButton!.onClick.AddListener(this.ConfirmAuth);
            authCodeInput!.GetComponentInChildren<Button>().onClick.AddListener(this.PasteAuthCode);

            loginCloseButton!.onClick.AddListener(this.HideLoginPanel);
            loginPanelButton!.GetComponent<Button>().onClick.AddListener(this.ShowLoginPanel);
            // Cancel auth, not just hide panel (so basically this button is totally the same as authCancelButton)...
            authCloseButton!.onClick.AddListener(this.CancelAuth);

            // Used for testing MC format code parsing
            // loadStateInfoText!.text = StringConvert.MC2TMP("Hello world §a[§a§a-1, §a1 §6[Bl§b[HHH]ah] Hello §c[Color RE§rD]  §a1§r] (blah)");
            loadStateInfoText!.text = $"CornCraft {CornGlobal.Version} Powered by <u>Minecraft Console Client</u>";

            // Release cursor (Useful when re-entering login scene from game)
            Cursor.lockState = CursorLockMode.None;
        }

        void Update()
        {
            if (namesShown)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    usernameIndex = (usernameIndex + 1) % shownNames.Length;
                    RefreshUsernames();
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    usernameIndex = (usernameIndex + shownNames.Length - 1) % shownNames.Length;
                    RefreshUsernames();
                }

                if (Input.GetKeyDown(KeyCode.Tab)) // Tab-complete
                {
                    if (usernameIndex >= 0 && usernameIndex < shownNames.Length)
                    {
                        usernameInput!.text = shownNames[usernameIndex];
                        usernameInput!.MoveTextEnd(false);
                    }
                        
                }

            }
        }

    }
}
