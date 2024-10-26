#nullable enable
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

namespace CraftSharp.Protocol.Session
{
    [Serializable]
    public class SessionToken
    {
        private static readonly Regex JwtRegex = new Regex("^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$");

        public string Id { get; set; }
        public string PlayerName { get; set; }
        public string PlayerId { get; set; }
        public string ClientId { get; set; }
        public string RefreshToken { get; set; }
        public string ServerIdhash { get; set; }
        public byte[]? ServerPublicKey { get; set; }

        public Task<bool>? SessionPreCheckTask = null;

        public SessionToken()
        {
            Id = string.Empty;
            PlayerName = string.Empty;
            PlayerId = string.Empty;
            ClientId = string.Empty;
            RefreshToken = string.Empty;
            ServerIdhash = string.Empty;
            ServerPublicKey = null;
        }

        public bool SessionPreCheck()
        {
            if (this.Id == string.Empty || this.PlayerId == string.Empty || this.ServerPublicKey == null)
                return false;
            if (Crypto.CryptoHandler.ClientAESPrivateKey == null)
                Crypto.CryptoHandler.ClientAESPrivateKey = Crypto.CryptoHandler.GenerateAESPrivateKey();
            string serverHash = Crypto.CryptoHandler.GetServerHash(ServerIdhash, ServerPublicKey, Crypto.CryptoHandler.ClientAESPrivateKey);
            if (ProtocolHandler.SessionCheck(PlayerId, Id, serverHash))
                return true;
            return false;
        }

        public override string ToString()
        {
            return String.Join(",", Id, PlayerName, PlayerId, ClientId, RefreshToken, ServerIdhash, 
                (ServerPublicKey == null) ? string.Empty : Convert.ToBase64String(ServerPublicKey));
        }

        public static SessionToken FromString(string tokenString)
        {
            string[] fields = tokenString.Split(',');
            if (fields.Length < 4)
                throw new InvalidDataException("Invalid string format");

            SessionToken session = new SessionToken();
            session.Id = fields[0];
            session.PlayerName = fields[1];
            session.PlayerId = fields[2];
            session.ClientId = fields[3];
            // Backward compatible with old session file without refresh token field
            if (fields.Length > 4)
                session.RefreshToken = fields[4];
            else
                session.RefreshToken = string.Empty;
            if (fields.Length > 5)
                session.ServerIdhash = fields[5];
            else
                session.ServerIdhash = string.Empty;
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
            if (!JwtRegex.IsMatch(session.Id))
                throw new InvalidDataException("Invalid session Id");
            if (!PlayerInfo.IsValidName(session.PlayerName))
                throw new InvalidDataException("Invalid player name");
            if (!Guid.TryParseExact(session.PlayerId, "N", out temp))
                throw new InvalidDataException("Invalid player Id");
            if (!Guid.TryParseExact(session.ClientId, "N", out temp))
                throw new InvalidDataException("Invalid client Id");
            // No validation on refresh token because it is custom format token (not Jwt)

            return session;
        }

    }
}
