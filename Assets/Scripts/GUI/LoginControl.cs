#nullable enable
using System;
using System.Linq;
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
        private TMP_InputField? serverInput, usernameInput, passwordInput;
        private Button?         loginButton, quitButton;
        private TMP_Text?       loadStateInfoText;

        private bool tryingConnect = false;

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

        // Returns true if successfully connected
        public void ConnectServer()
        {
            if (tryingConnect) return;
            tryingConnect = true;

            string serverText = serverInput!.text;
            string account = usernameInput!.text;
            string password = passwordInput!.text;

            string accountLower = account.ToLower();

            string username; // In-game display name, will be set after connection

            string host;   // Server ip address
            ushort port = 25565; // Server port

            if (!ParseServerIP(serverText, out host, ref port))
            {
                CornClient.ShowNotification("Failed to parse server name or address!", Notification.Type.Warning);
                tryingConnect = false;
                loadStateInfo.infoText = "X_X";
                return;
            }

            bool MinecraftRealmsEnabled = false;

            // TODO Move to right place
            ProtocolHandler.AccountType accountType = ProtocolHandler.AccountType.Microsoft;

            SessionToken session = new SessionToken();
            PlayerKeyPair? playerKeyPair = null;

            ProtocolHandler.LoginResult result = ProtocolHandler.LoginResult.LoginRequired;

            if (password == "-")
            {
                if (!CornCraft.IsValidName(account))
                {
                    CornClient.ShowNotification("The username is not valid!", Notification.Type.Warning);
                    tryingConnect = false;
                    loadStateInfo.infoText = "X_X";
                    return;
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
                            catch (Exception ex)
                            {
                                Debug.LogError("Refresh access token fail: " + ex.Message);
                                result = ProtocolHandler.LoginResult.InvalidResponse;
                            }
                        }
                        if (result != ProtocolHandler.LoginResult.Success && password == string.Empty)
                        {   // Request password
                            CornClient.ShowNotification("Please input your password!", Notification.Type.Warning);
                            tryingConnect = false;
                            loadStateInfo.infoText = "X_X";
                            return;
                        }
                    }
                    else
                        Translations.Log("mcc.session_valid", session.PlayerName);
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    Translations.Log("mcc.connecting", accountType == ProtocolHandler.AccountType.Mojang ? "Minecraft.net" : "Microsoft");
                    result = ProtocolHandler.GetLogin(account, password, accountType, out session, ref account);
                }
            }

            if (result == ProtocolHandler.LoginResult.Success && CornCraft.SessionCaching != CacheType.None)
            {
                SessionCache.Store(accountLower, session);
            }

            if (result == ProtocolHandler.LoginResult.Success)
            {
                if (accountType == ProtocolHandler.AccountType.Microsoft && password != "-" && CornCraft.LoginWithSecureProfile)
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
                if (MinecraftRealmsEnabled && !String.IsNullOrEmpty(session.ID))
                    availableWorlds = ProtocolHandler.RealmsListWorlds(username, session.PlayerID, session.ID);

                if (host == string.Empty)
                {   // Request host
                    CornClient.ShowNotification("Please input your host!", Notification.Type.Warning);
                    return;
                }

                // Get server version
                int protocolVersion = 0;
                ForgeInfo? forgeInfo = null;

                if (!isRealms)
                {
                    Translations.Log("mcc.retrieve"); // Retrieve server information
                    if (!ProtocolHandler.GetServerInfo(host, port, ref protocolVersion, ref forgeInfo))
                    {
                        Translations.NotifyError("error.ping");
                        tryingConnect = false;
                        loadStateInfo.infoText = "X_X";
                        return;
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
                            return;
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
                string failureReason = string.Empty;
                switch (result)
                {
                    case ProtocolHandler.LoginResult.AccountMigrated: failureReason = "error.login.migrated"; break;
                    case ProtocolHandler.LoginResult.ServiceUnavailable: failureReason = "error.login.server"; break;
                    case ProtocolHandler.LoginResult.WrongPassword: failureReason = "error.login.blocked"; break;
                    case ProtocolHandler.LoginResult.InvalidResponse: failureReason = "error.login.response"; break;
                    case ProtocolHandler.LoginResult.NotPremium: failureReason = "error.login.premium"; break;
                    case ProtocolHandler.LoginResult.OtherError: failureReason = "error.login.network"; break;
                    case ProtocolHandler.LoginResult.SSLError: failureReason = "error.login.ssl"; break;
                    case ProtocolHandler.LoginResult.UserCancel: failureReason = "error.login.cancel"; break;
                    default: failureReason = "error.login.unknown"; break;
                }
                failureMessage += Translations.Get(failureReason);

                if (result == ProtocolHandler.LoginResult.SSLError)
                {
                    Translations.NotifyError("error.login.ssl_help");
                    tryingConnect = false;
                    loadStateInfo.infoText = "X_X";
                    return;
                }
                Debug.LogError(failureMessage);
            }

            tryingConnect = false;
            loadStateInfo.infoText = "X_X";
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        void Start()
        {
            game = CornClient.Instance;

            // Initialize controls
            var loginPanel = transform.Find("Login Panel");

            serverInput   = loginPanel.transform.Find("Server Input").GetComponent<TMP_InputField>();
            usernameInput = loginPanel.transform.Find("Username Input").GetComponent<TMP_InputField>();
            passwordInput = loginPanel.transform.Find("Password Input").GetComponent<TMP_InputField>();
            loginButton   = loginPanel.transform.Find("Login Button").GetComponent<Button>();

            quitButton        = transform.Find("Quit Button").GetComponent<Button>();
            loadStateInfoText = transform.Find("Load State Info Text").GetComponent<TMP_Text>();

            // TODO Initialize with loaded values
            serverInput.text = "192.168.1.7";
            usernameInput.text = "Corn";
            passwordInput.text = "-";

            // Add listeners
            loginButton.onClick.AddListener(this.ConnectServer);
            quitButton.onClick.AddListener(this.QuitGame);

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
