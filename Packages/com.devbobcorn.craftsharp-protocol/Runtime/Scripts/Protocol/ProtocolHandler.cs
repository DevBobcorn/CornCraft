using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using CraftSharp.Protocol.Handlers;
using CraftSharp.Protocol.Handlers.Forge;
using CraftSharp.Protocol.Session;
using System.Security.Authentication;
using UnityEngine;

namespace CraftSharp.Protocol
{
    /// <summary>
    /// Handle login, session, server ping and provide a protocol handler for interacting with a minecraft server.
    /// </summary>
    /// <remarks>
    /// Typical update steps for marking a new Minecraft version as supported:
    ///  - Add protocol ID in GetProtocolHandler()
    ///  - Add 1.X.X case in MCVer2ProtocolVersion()
    /// </remarks>
    public static class ProtocolHandler
    {
        /// <summary>
        /// Perform a DNS lookup for a Minecraft Service using the specified domain name
        /// </summary>
        /// <param name="domain">Input domain name, updated with target host if any, else left untouched</param>
        /// <param name="port">Updated with target port if any, else left untouched</param>
        /// <returns>TRUE if a Minecraft Service was found.</returns>
        public static bool MinecraftServiceLookup(ref string domain, ref ushort port)
        {
            bool foundService = false;
            string domainVal = domain;
            ushort portVal = port;

            if (!String.IsNullOrEmpty(domain) && domain.Any(c => char.IsLetter(c)))
            {
                AutoTimeout.Perform(() =>
                {
                    try
                    {
                        Debug.Log(Translations.Get("mcc.resolve", domainVal));
                        // TODO Find a DNS lookup better solution
                        var response = Dns.GetHostEntry(domainVal);
                        if (response.AddressList.Any())
                        {
                            var target = response.AddressList[0];
                            Debug.Log(Translations.Get("mcc.found", target, portVal, domainVal));
                            domainVal = target.ToString();
                            foundService = true;
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogError(Translations.Get("mcc.not_found", domainVal, e.GetType().FullName, e.Message));
                    }
                }, TimeSpan.FromSeconds(20));
            }

            domain = domainVal;
            port = portVal;
            return foundService;
        }

        #nullable enable
        /// <summary>
        /// Retrieve information about a Minecraft server
        /// </summary>
        /// <param name="serverIP">Server IP to ping</param>
        /// <param name="serverPort">Server Port to ping</param>
        /// <param name="protocolversion">Will contain protocol version, if ping successful</param>
        /// <returns>TRUE if ping was successful</returns>
        public static bool GetServerInfo(string serverIP, ushort serverPort, ref string versionName, ref int protocolversion, ref ForgeInfo? forgeInfo)
        {
            bool success = false;
            int protocolversionTmp = 0;
            ForgeInfo? forgeInfoTmp = null;
            string versionNameTmp = string.Empty;
            if (AutoTimeout.Perform(() =>
            {
                try
                {
                    if (ProtocolMinecraft.DoPing(serverIP, serverPort, ref versionNameTmp, ref protocolversionTmp, ref forgeInfoTmp))
                    {
                        success = true;
                    }
                    else
                        Debug.LogError(Translations.Get("error.unexpect_response"));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }, TimeSpan.FromSeconds(10)))
            {
                if (protocolversion != 0 && protocolversion != protocolversionTmp)
                    Debug.LogError(Translations.Get("error.version_different"));
                if (protocolversion == 0 && protocolversionTmp <= 1)
                    Debug.LogError(Translations.Get("error.no_version_report"));
                if (protocolversion == 0)
                    protocolversion = protocolversionTmp;
                forgeInfo = forgeInfoTmp;
                versionName = versionNameTmp;
                return success;
            }
            else
            {
                Debug.LogError(Translations.Get("error.connection_timeout"));
                return false;
            }
        }
        #nullable disable

        public static bool IsProtocolSupported(int protocol)
        {
            return ACCECPTED_VERSIONS.ContainsKey(protocol);
        }

        public static int GetMinSupported()
        {
            return ACCECPTED_VERSIONS.Min(x => x.Key);
        }

        public static int GetMaxSupported()
        {
            return ACCECPTED_VERSIONS.Max(x => x.Key);
        }

        /// <summary>
        /// Get a protocol handler for the specified Minecraft version
        /// </summary>
        /// <param name="client">Tcp Client connected to the server</param>
        /// <param name="protocolVersion">Protocol version to handle</param>
        /// <param name="handler">Handler with the appropriate callbacks</param>
        /// <returns></returns>
        public static IMinecraftCom GetProtocolHandler(TcpClient client, int protocolVersion, ForgeInfo forgeInfo, IMinecraftComHandler handler)
        {
            if (IsProtocolSupported(protocolVersion))
                return new ProtocolMinecraft(client, protocolVersion, handler, forgeInfo);
            throw new NotSupportedException(Translations.Get("exception.version_unsupported", protocolVersion));
        }

        /// <summary>
        /// Convert a human-readable Minecraft version number to network protocol version number
        /// </summary>
        /// <param name="mcVersion">The Minecraft version number</param>
        /// <returns>The protocol version number or 0 if could not determine protocol version: error, unknown, not supported</returns>
        public static int MCVer2ProtocolVersion(string mcVersion)
        {
            if (mcVersion.Contains('.'))
            {
                return mcVersion.Split(' ')[0].Trim() switch
                {
                    "1.16.2" => 751,
                    "1.16.3" => 753,
                    "1.16.4" or "1.16.5" => 754,
                    "1.17" => 755,
                    "1.17.1" => 756,
                    "1.18" or "1.18.1" => 757,
                    "1.18.2" => 758,
                    "1.19" => 759,
                    "1.19.1" or "1.19.2" => 760,
                    "1.19.3" => 761,
                    "1.19.4" => 762,
                    "1.20" or "1.20.1" => 763,
                    "1.20.2" => 764,
                    "1.20.3" or "1.20.4" => 765,
                    "1.20.5" or "1.20.6" => 766,
                    "1.21" or "1.21.1" => 767,
                    _ => 0,
                };
            }
            else
            {
                try
                {
                    return int.Parse(mcVersion);
                }
                catch
                {
                    return 0;
                }
            }
        }

        private static readonly Dictionary<int, string> ACCECPTED_VERSIONS = new()
        {
                [751] = "1.16.2",
                [753] = "1.16.3",
                [754] = "1.16.5",
                [755] = "1.17",
                [756] = "1.17.1",
                [757] = "1.18.1",
                [758] = "1.18.2",
                [759] = "1.19",
                [760] = "1.19.2",
                [761] = "1.19.3",
                [762] = "1.19.4",
                [763] = "1.20",
                [764] = "1.20.2",
                [765] = "1.20.3",
                [766] = "1.20.5",
                [767] = "1.21"
        };

        /// <summary>
        /// Convert a network protocol version number to human-readable Minecraft version number
        /// </summary>
        /// <remarks>Some Minecraft versions share the same protocol number. In that case, the lowest version for that protocol is returned.</remarks>
        /// <param name="protocol">The Minecraft protocol version number</param>
        /// <returns>The 1.X.X version number, or unknown if could not determine protocol version</returns>
        public static string ProtocolVersion2MCVer(int protocol)
        {
            return ACCECPTED_VERSIONS.GetValueOrDefault(protocol, "unknown");
        }

        /// <summary>
        /// Check if we can force-enable Forge support for a Minecraft version without using server Ping
        /// </summary>
        /// <param name="protocolVersion">Minecraft protocol version</param>
        /// <returns>TRUE if we can force-enable Forge support without using server Ping</returns>
        public static bool ProtocolMayForceForge(int protocol)
        {
            return ProtocolForge.ServerMayForceForge(protocol);
        }

        /// <summary>
        /// Server Info: Consider Forge to be enabled regardless of server Ping
        /// </summary>
        /// <param name="protocolVersion">Minecraft protocol version</param>
        /// <returns>ForgeInfo item stating that Forge is enabled</returns>
        public static ForgeInfo ProtocolForceForge(int protocol)
        {
            return ProtocolForge.ServerForceForge(protocol);
        }

        public enum LoginResult { OtherError, ServiceUnavailable, SSLError, Success, WrongPassword, AccountMigrated, NotPremium, LoginRequired, InvalidToken, InvalidResponse, NullError, UserCancel };

        /// <summary>
        /// Sign-in to Microsoft Account by asking user to open sign-in page using browser. 
        /// </summary>
        /// <remarks>
        /// The downside is this require user to copy and paste lengthy content from and to console.
        /// Sign-in page: 218 chars
        /// Response URL: around 1500 chars
        /// </remarks>
        /// <param name="code"></param>
        /// <param name="session"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public static LoginResult MicrosoftBrowserLogin(string code, out SessionToken session, ref string account)
        {
            var msaResponse = Microsoft.RequestAccessToken(code);
            return MicrosoftLogin(msaResponse, out session, ref account);
        }

        public static LoginResult MicrosoftLoginRefresh(string refreshToken, out SessionToken session, ref string account)
        {
            var msaResponse = Microsoft.RefreshAccessToken(refreshToken);
            return MicrosoftLogin(msaResponse, out session, ref account);
        }

        private static LoginResult MicrosoftLogin(Microsoft.LoginResponse msaResponse, out SessionToken session, ref string account)
        {
            session = new SessionToken() { ClientId = Guid.NewGuid().ToString().Replace("-", "") };

            try
            {
                var xblResponse = XboxLive.XblAuthenticate(msaResponse);
                var xsts = XboxLive.XSTSAuthenticate(xblResponse); // Might throw even password correct

                string accessToken = MinecraftWithXbox.LoginWithXbox(xsts.UserHash, xsts.Token);
                bool hasGame = MinecraftWithXbox.UserHasGame(accessToken);
                if (hasGame)
                {
                    var profile = MinecraftWithXbox.GetUserProfile(accessToken);
                    session.PlayerName = profile.UserName;
                    session.PlayerId = profile.UUID;
                    session.Id = accessToken;
                    session.RefreshToken = msaResponse.RefreshToken;
                    // Correct the account email if doesn't match
                    account = msaResponse.Email;
                    return LoginResult.Success;
                }
                else
                {
                    return LoginResult.NotPremium;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Microsoft authenticate failed: " + e.Message);
                Debug.LogError(e.StackTrace);
                return LoginResult.WrongPassword; // Might not always be wrong password
            }
        }

        /// <summary>
        /// Validates whether accessToken must be refreshed
        /// </summary>
        /// <param name="session">Session token to validate</param>
        /// <returns>Returns the status of the token (Valid, Invalid, etc.)</returns>
        public static LoginResult GetTokenValidation(SessionToken session)
        {
            var payload = JwtPayloadDecode.GetPayload(session.Id);
            var json = Json.ParseJson(payload);
            var expTimestamp = long.Parse(json.Properties["exp"].StringValue);
            var now = DateTime.Now;
            var tokenExp = UnixTimeStampToDateTime(expTimestamp);
            if (now < tokenExp)
            {
                // Still valid
                return LoginResult.Success;
            }
            else
            {
                // Token expired
                return LoginResult.LoginRequired;
            }
        }

        /// <summary>
        /// Refreshes invalid token
        /// </summary>
        /// <param name="currentsession">Login</param>
        /// <param name="session">In case of successful token refresh, will contain session information for multiplayer</param>
        /// <returns>Returns the status of the new token request (Success, Failure, etc.)</returns>
        public static LoginResult GetNewToken(SessionToken currentsession, out SessionToken session)
        {
            session = new SessionToken();
            try
            {
                string result = "";
                string json_request = "{ \"accessToken\": \"" + JsonEncode(currentsession.Id) + "\", \"clientToken\": \"" + JsonEncode(currentsession.ClientId) + "\", \"selectedProfile\": { \"id\": \"" + JsonEncode(currentsession.PlayerId) + "\", \"name\": \"" + JsonEncode(currentsession.PlayerName) + "\" } }";
                int code = DoHTTPSPost("authserver.mojang.com", "/refresh", json_request, ref result);
                if (code == 200)
                {
                    if (result == null)
                    {
                        return LoginResult.NullError;
                    }
                    else
                    {
                        Json.JSONData loginResponse = Json.ParseJson(result);
                        if (loginResponse.Properties.ContainsKey("accessToken")
                            && loginResponse.Properties.ContainsKey("selectedProfile")
                            && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("id")
                            && loginResponse.Properties["selectedProfile"].Properties.ContainsKey("name"))
                        {
                            session.Id = loginResponse.Properties["accessToken"].StringValue;
                            session.PlayerId = loginResponse.Properties["selectedProfile"].Properties["id"].StringValue;
                            session.PlayerName = loginResponse.Properties["selectedProfile"].Properties["name"].StringValue;
                            return LoginResult.Success;
                        }
                        else return LoginResult.InvalidResponse;
                    }
                }
                else if (code == 403 && result.Contains("InvalidToken"))
                {
                    return LoginResult.InvalidToken;
                }
                else
                {
                    Debug.LogError("Failed to authenticate, HTTP code: " + code);
                    return LoginResult.OtherError;
                }
            }
            catch
            {
                return LoginResult.OtherError;
            }
        }

        /// <summary>
        /// Check session using Mojang's Yggdrasil authentication scheme. Allows to join an online-mode server
        /// </summary>
        /// <param name="uuid">User's id</param>
        /// <param name="accesstoken">Session ID</param>
        /// <param name="serverhash">Server ID</param>
        /// <returns>TRUE if session was successfully checked</returns>
        public static bool SessionCheck(string uuid, string accesstoken, string serverhash)
        {
            try
            {
                string result = "";
                string json_request = "{\"accessToken\":\"" + accesstoken + "\",\"selectedProfile\":\"" + uuid + "\",\"serverId\":\"" + serverhash + "\"}";
                int code = DoHTTPSPost("sessionserver.mojang.com", "/session/minecraft/join", json_request, ref result);
                return (code >= 200 && code < 300);
            }
            catch { return false; }
        }

        /// <summary>
        /// Make a HTTPS GET request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="cookies">Cookies for making the request</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static int DoHTTPSGet(string host, string endpoint, string cookies, ref string result)
        {
            List<string> http_request = new()
            {
                "GET " + endpoint + " HTTP/1.1",
                "Cookie: " + cookies,
                "Cache-Control: no-cache",
                "Pragma: no-cache",
                "Host: " + host,
                "User-Agent: Java/1.6.0_27",
                "Accept-Charset: ISO-8859-1,UTF-8;q=0.7,*;q=0.7",
                "Connection: close",
                "",
                ""
            };
            return DoHTTPSRequest(http_request, host, ref result);
        }

        /// <summary>
        /// Make a HTTPS POST request to the specified endpoint of the Mojang API
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="endpoint">Endpoint for making the request</param>
        /// <param name="request">Request payload</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static int DoHTTPSPost(string host, string endpoint, string request, ref string result)
        {
            List<string> http_request = new()
            {
                "POST " + endpoint + " HTTP/1.1",
                "Host: " + host,
                "User-Agent: CornCraft/" + ProtocolSettings.Version,
                "Content-Type: application/json",
                "Content-Length: " + Encoding.ASCII.GetBytes(request).Length,
                "Connection: close",
                "",
                request
            };
            return DoHTTPSRequest(http_request, host, ref result);
        }

        #nullable enable
        /// <summary>
        /// Manual HTTPS request since we must directly use a TcpClient because of the proxy.
        /// This method connects to the server, enables SSL, do the request and read the response.
        /// </summary>
        /// <param name="headers">Request headers and optional body (POST)</param>
        /// <param name="host">Host to connect to</param>
        /// <param name="result">Request result</param>
        /// <returns>HTTP Status code</returns>
        private static int DoHTTPSRequest(List<string> headers, string host, ref string result)
        {
            static int str2int(string str)
            {
                try
                {
                    return Convert.ToInt32(str.Trim());
                }
                catch {
                    Debug.LogError(Translations.Get("error.setting.str2int", str));
                    return 0;
                }
            };

            string? postResult = null;
            int statusCode = 520;
            Exception? exception = null;
            AutoTimeout.Perform(() =>
            {
                try
                {
                    if (ProtocolSettings.DebugMode)
                        Debug.Log(Translations.Get("debug.request", host));

                    //TcpClient client = ProxyHandler.newTcpClient(host, 443, true);
                    TcpClient client = new(host, 443);
                    SslStream stream = new(client.GetStream());
                    stream.AuthenticateAsClient(host, null, SslProtocols.Tls12, true); // Enable TLS 1.2. Hotfix for #1780

                    stream.Write(Encoding.ASCII.GetBytes(String.Join("\r\n", headers.ToArray())));
                    System.IO.StreamReader sr = new(stream);
                    string raw_result = sr.ReadToEnd();

                    if (raw_result.StartsWith("HTTP/1.1"))
                    {
                        postResult = raw_result[(raw_result.IndexOf("\r\n\r\n") + 4)..];
                        statusCode = str2int(raw_result.Split(' ')[1]);
                    }
                    else statusCode = 520; // Web server is returning an unknown error
                }
                catch (Exception e)
                {
                    if (e is not System.Threading.ThreadAbortException)
                    {
                        exception = e;
                    }
                }
            }, TimeSpan.FromSeconds(30));
            if (postResult is not null)
                result = postResult;
            if (exception is not null)
                throw exception;
            return statusCode;
        }
        #nullable disable

        /// <summary>
        /// Encode a string to a json string.
        /// Will convert special chars to \u0000 unicode escape sequences.
        /// </summary>
        /// <param name="text">Source text</param>
        /// <returns>Encoded text</returns>
        private static string JsonEncode(string text)
        {
            StringBuilder result = new();

            foreach (char c in text)
            {
                if ((c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z'))
                {
                    result.Append(c);
                }
                else
                {
                    result.AppendFormat(@"\u{0:x4}", (int)c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert a TimeStamp (in second) to DateTime object
        /// </summary>
        /// <param name="unixTimeStamp">TimeStamp in second</param>
        /// <returns>DateTime object of the TimeStamp</returns>
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

    }
}