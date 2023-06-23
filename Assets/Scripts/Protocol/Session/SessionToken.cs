#nullable enable
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol.Session
{
    [Serializable]
    public class SessionToken
    {
        private static readonly Regex JwtRegex = new Regex("^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$");

        public string ID { get; set; }
        public string PlayerName { get; set; }
        public string PlayerID { get; set; }
        public string ClientID { get; set; }
        public string RefreshToken { get; set; }
        public string ServerIDhash { get; set; }
        public byte[]? ServerPublicKey { get; set; }

        public Task<bool>? SessionPreCheckTask = null;

        public SessionToken()
        {
            ID = string.Empty;
            PlayerName = string.Empty;
            PlayerID = string.Empty;
            ClientID = string.Empty;
            RefreshToken = string.Empty;
            ServerIDhash = string.Empty;
            ServerPublicKey = null;
        }

        public bool SessionPreCheck()
        {
            if (this.ID == string.Empty || this.PlayerID == string.Empty || this.ServerPublicKey == null)
                return false;
            if (Crypto.CryptoHandler.ClientAESPrivateKey == null)
                Crypto.CryptoHandler.ClientAESPrivateKey = Crypto.CryptoHandler.GenerateAESPrivateKey();
            string serverHash = Crypto.CryptoHandler.GetServerHash(ServerIDhash, ServerPublicKey, Crypto.CryptoHandler.ClientAESPrivateKey);
            if (ProtocolHandler.SessionCheck(PlayerID, ID, serverHash))
                return true;
            return false;
        }

        public override string ToString()
        {
            return String.Join(",", ID, PlayerName, PlayerID, ClientID, RefreshToken, ServerIDhash, 
                (ServerPublicKey == null) ? string.Empty : Convert.ToBase64String(ServerPublicKey));
        }

        public static SessionToken FromString(string tokenString)
        {
            string[] fields = tokenString.Split(',');
            if (fields.Length < 4)
                throw new InvalidDataException("Invalid string format");

            SessionToken session = new SessionToken();
            session.ID = fields[0];
            session.PlayerName = fields[1];
            session.PlayerID = fields[2];
            session.ClientID = fields[3];
            // Backward compatible with old session file without refresh token field
            if (fields.Length > 4)
                session.RefreshToken = fields[4];
            else
                session.RefreshToken = string.Empty;
            if (fields.Length > 5)
                session.ServerIDhash = fields[5];
            else
                session.ServerIDhash = string.Empty;
            if (fields.Length > 6)
            {
                try
                {
                    session.ServerPublicKey = Convert.FromBase64String(fields[6]);
                }
                catch
                {
                    session.ServerPublicKey = null;
                }
            }
            else
                session.ServerPublicKey = null;

            Guid temp;
            if (!JwtRegex.IsMatch(session.ID))
                throw new InvalidDataException("Invalid session ID");
            if (!StringHelper.IsValidName(session.PlayerName))
                throw new InvalidDataException("Invalid player name");
            if (!Guid.TryParseExact(session.PlayerID, "N", out temp))
                throw new InvalidDataException("Invalid player ID");
            if (!Guid.TryParseExact(session.ClientID, "N", out temp))
                throw new InvalidDataException("Invalid client ID");
            // No validation on refresh token because it is custom format token (not Jwt)

            return session;
        }

    }
}
