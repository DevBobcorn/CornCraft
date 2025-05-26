using UnityEngine;
using CraftSharp.Proxy;

namespace CraftSharp
{
    public static class ProtocolSettings
    {
        private static string version = "UwU";
        private static bool versionInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeVersion()
        {
            version = Application.version; // Application.version is only accessible from Unity thread
            versionInitialized = true;
        }
        
        public static string Version => versionInitialized ? version : "???";
        public static string BrandInfo => $"CornCraft/{Application.version}";

        public static bool DisplaySystemMessages  { get; set; } =  true;
        public static bool DisplayXpBarMessages   { get; set; } =  true;
        public static bool MarkIllegallySignedMsg { get; set; } = false;
        public static bool MarkLegallySignedMsg   { get; set; } = false;
        public static bool MarkSystemMessage      { get; set; } = false;
        public static bool MarkModifiedMsg        { get; set; } = false;
        public static bool ShowModifiedChat       { get; set; } =  true;
        public static bool ShowIllegalSignedChat  { get; set; } =  true;
        public static bool SignMessageInCommand   { get; set; } = false;
        public static bool SignChat               { get; set; } = false;

        public static bool CapturePackets         { get; set; } = false;
        public static double MessageCooldown      { get; set; } = 1.0D;

        public static bool LoginWithSecureProfile { get; set; } = false;

        public static string Language { get; set; } = "en_us";
        public static bool DebugMode { get; set; } = true;

        // Custom app variables
        public static readonly CacheType SessionCaching = CacheType.Disk;
        public static readonly CacheType ProfileKeyCaching = CacheType.Disk;
        public static readonly bool ResolveSrvRecords = true;

        // Proxy setup
        public static bool ProxyEnabledLogin { get; set; } = false;
        public static bool ProxyEnabledInGame { get; set; } = false;
        public static ProxyHandler.Type ProxyType { get; set; }
        public static string ProxyHost { get; set; }
        public static int ProxyPort { get; set; }
        public static string ProxyUsername { get; set; }
        public static string ProxyPassword { get; set; }

        // Minecraft Settings
        public static MCSettings MCSettings { get; } = new();

        public enum CacheType
        {
            /// <summary>
            /// Do not perform any session caching, always perform login requests from scratch.
            /// </summary>
            None,

            /// <summary>
            /// Cache session information in memory to reuse session tokens across server joins.
            /// </summary>
            Memory,

            /// <summary>
            /// Cache session information in a SessionCache file to share session tokens between different MCC instances.
            /// </summary>
            Disk
        };
    }
}
