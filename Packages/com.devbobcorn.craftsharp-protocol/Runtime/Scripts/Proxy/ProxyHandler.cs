using System.Net.Sockets;
using Starksoft.Net.Proxy;
using UnityEngine;

namespace CraftSharp.Proxy
{
    /// <summary>
    /// Automatically handle proxies according to the app Settings.
    /// Note: Underlying proxy handling is taken from Starksoft, LLC's Biko Library.
    /// This library is open source and provided under the MIT license. More info at biko.codeplex.com.
    /// </summary>

    public static class ProxyHandler
    {
        public enum Type { HTTP, SOCKS4, SOCKS4a, SOCKS5 };

        private static readonly ProxyClientFactory factory = new();
        private static IProxyClient proxy;
        private static bool proxy_ok = false;

        /// <summary>
        /// Create a regular TcpClient or a proxied TcpClient according to the app Settings.
        /// </summary>
        /// <param name="host">Target host</param>
        /// <param name="port">Target port</param>
        /// <param name="login">True if the purpose is logging in to a Minecraft account</param>

        public static TcpClient NewTcpClient(string host, int port, bool login = false)
        {
            try
            {
                if (login ? ProtocolSettings.ProxyEnabledLogin : ProtocolSettings.ProxyEnabledInGame)
                {
                    ProxyType innerProxytype = ProxyType.Http;

                    switch (ProtocolSettings.ProxyType)
                    {
                        case Type.HTTP: innerProxytype = ProxyType.Http; break;
                        case Type.SOCKS4: innerProxytype = ProxyType.Socks4; break;
                        case Type.SOCKS4a: innerProxytype = ProxyType.Socks4a; break;
                        case Type.SOCKS5: innerProxytype = ProxyType.Socks5; break;
                    }

                    if (ProtocolSettings.ProxyUsername != "" && ProtocolSettings.ProxyPassword != "")
                    {
                        proxy = factory.CreateProxyClient(innerProxytype, ProtocolSettings.ProxyHost, ProtocolSettings.ProxyPort, ProtocolSettings.ProxyUsername, ProtocolSettings.ProxyPassword);
                    }
                    else proxy = factory.CreateProxyClient(innerProxytype, ProtocolSettings.ProxyHost, ProtocolSettings.ProxyPort);

                    if (!proxy_ok)
                    {
                        Debug.Log(Translations.Get("proxy.connected", ProtocolSettings.ProxyHost, ProtocolSettings.ProxyPort));
                        proxy_ok = true;
                    }

                    return proxy.CreateConnection(host, port);
                }
                else return new TcpClient(host, port);
            }
            catch (ProxyException e)
            {
                Debug.Log(e.Message);
                proxy = null;
                throw new SocketException((int)SocketError.HostUnreachable);
            }
        }
    }
}
