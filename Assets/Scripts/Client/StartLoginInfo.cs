#nullable enable
using CraftSharp.Protocol.Handlers.Forge;
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
        public ForgeInfo? ForgeInfo;
        public string AccountLower;

        public StartLoginInfo(bool online, SessionToken session, PlayerKeyPair? player, string serverIp, ushort serverPort,
                int protocolVersion, ForgeInfo? forgeInfo, string accountLower)
        {
            Online = online;
            Session = session;
            Player = player;
            ServerIp = serverIp;
            ServerPort = serverPort;
            ProtocolVersion = protocolVersion;
            ForgeInfo = forgeInfo;
            AccountLower = accountLower;
        }
    }
}