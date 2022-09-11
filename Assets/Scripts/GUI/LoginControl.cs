#nullable enable
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Session;

namespace MinecraftClient.UI
{
    public class LoginControl : MonoBehaviour
    {
        private CornClient? game;
        private TMP_InputField? serverInput, usernameInput, passwordInput, authCodeInput;
        private Button?         loginButton, quitButton, authConfirmButton, authCancelButton;
        private TMP_Text?       loadStateInfoText, authLinkText;
        private CanvasGroup?    loginPanel, authPanel;

        private bool tryingConnect = false, authenticating = false, authCancelled = false;

        private readonly CornClient.LoadStateInfo loadStateInfo = new();

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
                if (sip.Length == 1 && host.Contains('.') && host.Any(c => char.IsLetter(c)) && CornCraft.ResolveSrvRecords)
                    //Domain name without port may need Minecraft SRV Record lookup
                    ProtocolHandler.MinecraftServiceLookup(ref host, ref port);
                return true;
            }

            return false;
        }
        
        public void TryConnectServer()
        {
            if (tryingConnect || loadStateInfo.loggingIn)
            {
                CornClient.ShowNotification("Already loggin' in!", Notification.Type.Warning);
                return;
            }
            tryingConnect = true;
            StartCoroutine(ConnectServer());
        }

        public IEnumerator ConnectServer()
        {
            string serverText = serverInput!.text;
            string account = usernameInput!.text;
            string password = passwordInput!.text;

            string accountLower = account.ToLower();

            string username; // In-game display name, will be set after connection

            SessionToken session = new SessionToken();
            PlayerKeyPair? playerKeyPair = null;

            var result = ProtocolHandler.LoginResult.LoginRequired;

            if (password == "-")
            {
                if (!CornCraft.IsValidName(account))
                {
                    CornClient.ShowNotification("The offline username is not valid!", Notification.Type.Warning);
                    tryingConnect = false;
                    loadStateInfo.infoText = ">_<";
                    yield break;
                }

                // Enter offline mode
                Translations.Notify("mcc.offline");
                result = ProtocolHandler.LoginResult.Success;
                session.PlayerID = "0";
                session.PlayerName = account;
            }
            else
            {   // Validate cached session or login new session.
                if (CornCraft.SessionCaching != CacheType.None && SessionCache.Contains(accountLower))
                {
                    session = SessionCache.Get(accountLower);
                    result = ProtocolHandler.GetTokenValidation(session);
                    if (result != ProtocolHandler.LoginResult.Success)
                    {
                        Translations.Log("mcc.session_invalid");
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
                        if (result != ProtocolHandler.LoginResult.Success && password == string.Empty)
                        {   // Request password
                            CornClient.ShowNotification("Please input your password!", Notification.Type.Warning);
                            tryingConnect = false;
                            loadStateInfo.infoText = ">_<";
                            yield break;
                        }
                    }
                    else
                        Translations.Log("mcc.session_valid", session.PlayerName);
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    Translations.Log("mcc.connecting", "Microsoft");
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
                        result = ProtocolHandler.MicrosoftBrowserLogin(code, out session, ref account);
                    }

                }
            }

            if (result == ProtocolHandler.LoginResult.Success)
            {
                string host;   // Server ip address
                ushort port = 25565; // Server port
                bool realmsEnabled = false;

                if (!ParseServerIP(serverText, out host, ref port))
                {
                    CornClient.ShowNotification("Failed to parse server name or address!", Notification.Type.Warning);
                    tryingConnect = false;
                    loadStateInfo.infoText = ">_<";
                    yield break;
                }

                if (CornCraft.SessionCaching != CacheType.None)
                    SessionCache.Store(accountLower, session);

                if (password != "-" && CornCraft.LoginWithSecureProfile)
                {
                    // Load cached profile key from disk if necessary
                    if (CornCraft.ProfileKeyCaching == CacheType.Disk)
                    {
                        bool cacheKeyLoaded = KeysCache.InitializeDiskCache();
                        if (CornCraft.DebugMode)
                            Translations.Log(cacheKeyLoaded ? "debug.keys_cache_ok" : "debug.keys_cache_fail");
                    }

                    if (CornCraft.ProfileKeyCaching != CacheType.None && KeysCache.Contains(accountLower))
                    {
                        playerKeyPair = KeysCache.Get(accountLower);
                        if (playerKeyPair.NeedRefresh())
                            Translations.Log("mcc.profile_key_invalid");
                        else
                            Translations.Log("mcc.profile_key_valid", session.PlayerName);
                    }

                    if (playerKeyPair == null || playerKeyPair.NeedRefresh())
                    {
                        Translations.Log("mcc.fetching_key");
                        playerKeyPair = KeyUtils.GetKeys(session.ID);
                        if (CornCraft.ProfileKeyCaching != CacheType.None && playerKeyPair != null)
                        {
                            KeysCache.Store(accountLower, playerKeyPair);
                        }
                    }
                }

                // Update the in-game user name
                username = session.PlayerName;
                bool isRealms = false;

                if (CornCraft.DebugMode)
                    Translations.Log("debug.session_id", session.ID);

                List<string> availableWorlds = new List<string>();
                if (realmsEnabled && !String.IsNullOrEmpty(session.ID))
                    availableWorlds = ProtocolHandler.RealmsListWorlds(username, session.PlayerID, session.ID);

                if (host == string.Empty)
                {   // Request host
                    CornClient.ShowNotification("Please input your host!", Notification.Type.Warning);
                    yield break;
                }

                // Get server version
                int protocolVersion = 0;
                ForgeInfo? forgeInfo = null;

                if (!isRealms)
                {
                    Translations.Log("mcc.retrieve"); // Retrieve server information
                    loadStateInfo.infoText = Translations.Get("mcc.retrieve");
                    yield return null;

                    if (!ProtocolHandler.GetServerInfo(host, port, ref protocolVersion, ref forgeInfo))
                    {
                        Translations.NotifyError("error.ping");
                        tryingConnect = false;
                        loadStateInfo.infoText = Translations.Get("error.ping");
                        yield break;
                    }
                }

                if (protocolVersion != 0) // Proceed to server login
                {
                    if (Protocol.ProtocolHandler.IsProtocolSupported(protocolVersion))
                    {
                        try // Login to Server
                        {
                            game!.StartLogin(session, playerKeyPair, host, port, protocolVersion, forgeInfo, loadStateInfo, accountLower);
                            tryingConnect = false;
                            yield break;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Unexpected error: " + e.StackTrace);
                        }
                    }
                    else
                        Translations.NotifyError("error.unsupported");
                }
                else // Unable to determine server version
                    Translations.NotifyError("error.determine");
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
                var translatedReason = Translations.Get(failureReason);
                failureMessage += translatedReason;
                loadStateInfo.infoText = translatedReason;
                CornClient.ShowNotification(translatedReason, Notification.Type.Error);

                if (result == ProtocolHandler.LoginResult.SSLError)
                    Translations.NotifyError("error.login.ssl_help");
                
                Debug.LogError(failureMessage);
            }

            tryingConnect = false;
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
                CornClient.ShowNotification("Authentication code is empty!", Notification.Type.Warning);
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
            game = CornClient.Instance;

            // Initialize controls
            var loginPanelObj = transform.Find("Login Panel");
            loginPanel = loginPanelObj.GetComponent<CanvasGroup>();
            var authPanelObj = transform.Find("Auth Panel");
            authPanel = authPanelObj.GetComponent<CanvasGroup>();

            serverInput   = loginPanelObj.transform.Find("Server Input").GetComponent<TMP_InputField>();
            usernameInput = loginPanelObj.transform.Find("Username Input").GetComponent<TMP_InputField>();
            passwordInput = loginPanelObj.transform.Find("Password Input").GetComponent<TMP_InputField>();
            loginButton   = loginPanelObj.transform.Find("Login Button").GetComponent<Button>();

            authCodeInput     = authPanelObj.transform.Find("Auth Code Input").GetComponent<TMP_InputField>();
            authLinkText      = authPanelObj.transform.Find("Auth Link Text").GetComponent<TMP_Text>();
            authCancelButton  = authPanelObj.transform.Find("Auth Cancel Button").GetComponent<Button>();
            authConfirmButton = authPanelObj.transform.Find("Auth Confirm Button").GetComponent<Button>();

            quitButton        = transform.Find("Quit Button").GetComponent<Button>();
            loadStateInfoText = transform.Find("Load State Info Text").GetComponent<TMP_Text>();

            // TODO Initialize with cached values
            serverInput.text = "192.168.1.7";

            // Hide auth panel on start
            HideAuthPanel();

            // Add listeners
            loginButton.onClick.AddListener(this.TryConnectServer);
            quitButton.onClick.AddListener(this.QuitGame);

            authCancelButton.onClick.AddListener(this.CancelAuth);
            authConfirmButton.onClick.AddListener(this.ConfirmAuth);

            loadStateInfo.infoText = $"CornCraft {CornCraft.Version} Powered by <u>Minecraft Console Client</u>";

        }

        void FixedUpdate()
        {
            if (loadStateInfoText!.text != loadStateInfo.infoText)
            {
                loadStateInfoText.text = loadStateInfo.infoText;
            }

        }

    }
}
