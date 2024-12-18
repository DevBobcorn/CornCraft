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
        private static readonly Regex JwtRegex = new("^[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+\\.[A-Za-z0-9-_]+$");

        public string Id { get; set; }
        public string PlayerName { get; set; }
        public string PlayerId { get; set; }
        public string ClientId { get; set; }
        public string RefreshToken { get; set; }
        public string ServerIdHash { get; set; }
        public byte[]? ServerPublicKey { get; set; }

        public Task<bool>? SessionPreCheckTask = null;

        public SessionToken()
        {
            Id = string.Empty;
            PlayerName = string.Empty;
            PlayerId = string.Empty;
            ClientId = string.Empty;
            RefreshToken = string.Empty;
            ServerIdHash = string.Empty;
            ServerPublicKey = null;
        }

        public bool SessionPreCheck()
        {
            if (Id == string.Empty || PlayerId == string.Empty || ServerPublicKey == null)
                return false;
            Crypto.CryptoHandler.ClientAESPrivateKey ??= Crypto.CryptoHandler.GenerateAESPrivateKey();
            string serverHash = Crypto.CryptoHandler.GetServerHash(ServerIdHash, ServerPublicKey, Crypto.CryptoHandler.ClientAESPrivateKey);
            if (ProtocolHandler.SessionCheck(PlayerId, Id, serverHash))
                return true;
            return false;
        }

        public override string ToString()
        {
            return string.Join(",", Id, PlayerName, PlayerId, ClientId, RefreshToken, ServerIdHash,
                (ServerPublicKey == null) ? string.Empty : Convert.ToBase64String(ServerPublicKey));
        }

        public static SessionToken FromString(string tokenString)
        {
            string[] fields = tokenString.Split(',');
            if (fields.Length < 4)
                throw new InvalidDataException("Invalid string format");

            SessionToken session = new()
            {
                Id = fields[0],
                PlayerName = fields[1],
                PlayerId = fields[2],
                ClientId = fields[3]
            };
            // Backward compatible with old session file without refresh token field
            if (fields.Length > 4)
                session.RefreshToken = fields[4];
            else
                session.RefreshToken = string.Empty;
            if (fields.Length > 5)
                session.ServerIdHash = fields[5];
            else
                session.ServerIdHash = string.Empty;
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
            if (!JwtRegex.IsMatch(session.Id))
                throw new InvalidDataException("Invalid session ID");
            if (!PlayerInfo.IsValidName(session.PlayerName))
                throw new InvalidDataException("Invalid player name");
            if (!Guid.TryParseExact(session.PlayerId, "N", out _))
                throw new InvalidDataException("Invalid player ID");
            if (!Guid.TryParseExact(session.ClientId, "N", out _))
                throw new InvalidDataException("Invalid client ID");
            // No validation on refresh token because it is custom format token (not Jwt)

            return session;
        }
    }
}
