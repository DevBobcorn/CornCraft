#nullable enable
using System;
using System.Linq;
using CraftSharp.Protocol.Message;
using CraftSharp.Protocol.ProfileKey;

namespace CraftSharp.Protocol
{
    public class PlayerInfo
    {
        public readonly Guid UUID;

        public readonly string Name;

        // Tuple<Name, Value, Signature(empty if there is no signature)
        public readonly Tuple<string, string, string?>[]? Property;

        public int Gamemode;

        public int Ping;

        public string? DisplayName;

        public bool Listed = true;

        // For message signature

        public int MessageIndex = -1;

        public Guid ChatUUID = Guid.Empty;

        private PublicKey? PublicKey;

        private DateTime? KeyExpiresAt;

        private bool lastMessageVerified;

        private byte[]? precedingSignature;

        public PlayerInfo(Guid uuid, string name, Tuple<string, string, string?>[]? property,
            int gamemode, int ping, string? displayName, long? timeStamp, byte[]? publicKey, byte[]? signature)
        {
            UUID = uuid;
            Name = name;
            if (property != null)
                Property = property;
            Gamemode = gamemode;
            Ping = ping;
            DisplayName = displayName;
            lastMessageVerified = false;
            if (timeStamp != null && publicKey != null && signature != null)
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)timeStamp);
                KeyExpiresAt = dateTimeOffset.UtcDateTime;
                try
                {
                    PublicKey = new PublicKey(publicKey, signature);
                    lastMessageVerified = true;
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    PublicKey = null;
                }
            }
            precedingSignature = null;
        }

        public PlayerInfo(string name, Guid uuid)
        {
            Name = name;
            UUID = uuid;
            Gamemode = -1;
            Ping = 0;
            lastMessageVerified = true;
            precedingSignature = null;
        }

        /// <summary>
        /// Verify that a string contains only a-z A-Z 0-9 or _ and meanwhile being not longer than 16 characters.
        /// </summary>
        public static bool IsValidName(string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            if (username.Any(c => c is not 
                (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')))
            {
                return false;
            }

            return username.Length <= 16;
        }

        public void ClearPublicKey()
        {
            ChatUUID = Guid.Empty;
            PublicKey = null;
            KeyExpiresAt = null;
        }

        public void SetPublicKey(Guid chatUUID, long publicKeyExpiryTime, byte[] encodedPublicKey, byte[] publicKeySignature)
        {
            ChatUUID = chatUUID;
            KeyExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(publicKeyExpiryTime).UtcDateTime;
            try
            {
                PublicKey = new PublicKey(encodedPublicKey, publicKeySignature);
                lastMessageVerified = true;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                PublicKey = null;
            }
        }

        public bool IsMessageChainLegal()
        {
            return lastMessageVerified;
        }

        public bool IsKeyExpired()
        {
            return DateTime.Now.ToUniversalTime() > KeyExpiresAt;
        }

        /// <summary>
        /// Verify message - 1.19
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <returns>Is this message valid</returns>
        public bool VerifyMessage(string message, long timestamp, long salt, ref byte[] signature)
        {
            if (PublicKey == null || IsKeyExpired())
                return false;
            
            DateTimeOffset timeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);

            byte[] saltByte = BitConverter.GetBytes(salt);
            Array.Reverse(saltByte);

            return PublicKey.VerifyMessage(message, UUID, timeOffset, ref saltByte, ref signature);
        }

        /// <summary>
        /// Verify message - 1.19.1 and 1.19.2
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <param name="messagePrecedingSignature">Preceding message signature</param>
        /// <param name="lastSeenMessages">LastSeenMessages</param>
        /// <returns>Is this message chain valid</returns>
        public bool VerifyMessage(string message, long timestamp, long salt, ref byte[] signature, ref byte[]? messagePrecedingSignature, LastSeenMessageList lastSeenMessages)
        {
            if (lastMessageVerified == false)
                return false;
            if (PublicKey == null || IsKeyExpired() || (precedingSignature != null && messagePrecedingSignature == null))
            {
                lastMessageVerified = false;
                return false;
            }
            if (precedingSignature != null && !precedingSignature.SequenceEqual(messagePrecedingSignature!))
            {
                lastMessageVerified = false;
                return false;
            }

            DateTimeOffset timeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);

            byte[] saltByte = BitConverter.GetBytes(salt);
            Array.Reverse(saltByte);

            bool res = PublicKey.VerifyMessage(message, UUID, timeOffset, ref saltByte, ref signature, ref messagePrecedingSignature, lastSeenMessages);

            lastMessageVerified = res;
            precedingSignature = signature;

            return res;
        }

        /// <summary>
        /// Verify message head - 1.19.1 and 1.19.2
        /// </summary>
        /// <param name="messagePrecedingSignature">Preceding message signature</param>
        /// <param name="headerSignature">Message signature</param>
        /// <param name="bodyDigest">Message body hash</param>
        /// <returns>Is this message chain valid</returns>
        public bool VerifyMessageHead(ref byte[]? messagePrecedingSignature, ref byte[] headerSignature, ref byte[] bodyDigest)
        {
            if (lastMessageVerified == false)
                return false;
            if (PublicKey == null || IsKeyExpired() || (precedingSignature != null && messagePrecedingSignature == null))
            {
                lastMessageVerified = false;
                return false;
            }
            if (precedingSignature != null && !precedingSignature.SequenceEqual(messagePrecedingSignature!))
            {
                lastMessageVerified = false;
                return false;
            }

            bool res = PublicKey.VerifyHeader(UUID, ref bodyDigest, ref headerSignature, ref messagePrecedingSignature);

            lastMessageVerified = res;
            precedingSignature = headerSignature;

            return res;
        }

        /// <summary>
        /// Verify message - 1.19.3 and above
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="playerUUID">Player UUID</param>
        /// <param name="chatUUID">Chat UUID</param>
        /// <param name="messageIndex">Message index</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <param name="previousMessageSignatures">Previous message signatures</param>
        /// <returns>Is this message chain valid</returns>
        public bool VerifyMessage(string message, Guid playerUUID, Guid chatUUID, int messageIndex, long timestamp, long salt, ref byte[] signature, Tuple<int, byte[]?>[] previousMessageSignatures)
        {
            if (PublicKey == null || IsKeyExpired())
                return false;

            // net.minecraft.server.network.ServerPlayNetworkHandler#validateMessage
            return true;
        }
    }
}