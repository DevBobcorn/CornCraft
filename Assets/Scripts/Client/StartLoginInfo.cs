#nullable enable
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Session;

namespace CraftSharp
{
    public record StartLoginInfo
    {
        public bool Online;
        public SessionToken Session;
        public PlayerKeyPair? Player;
        public string ServerIp;
        public ushort ServerPort;
        public int ProtocolVersion;
        public string AccountLower;
        public string? ReceivedVersionName;

        public StartLoginInfo(bool online, SessionToken session, PlayerKeyPair? player, string serverIp, ushort serverPort,
                int protocolVersion, string accountLower, string? receivedVersionName)
        {
            Online = online;
            Session = session;
            Player = player;
            ServerIp = serverIp;
            ServerPort = serverPort;
            ProtocolVersion = protocolVersion;
            AccountLower = accountLower;
            ReceivedVersionName = receivedVersionName;
        }
    }
}