using CraftSharp.Proxy;
using CraftSharp.Protocol.Session;

namespace CraftSharp
{
    public static class CornGlobal
    {
        public static string Version { get; private set; } = "0.1.0";
        public static string BrandInfo => $"CornCraft/{Version}";

        public static bool DisplaySystemMessages  { get; set; } =  true;
        public static bool DisplayXPBarMessages   { get; set; } =  true;
        public static bool MarkIllegallySignedMsg { get; set; } = false;
        public static bool MarkLegallySignedMsg   { get; set; } = false;
        public static bool MarkSystemMessage      { get; set; } = false;
        public static bool MarkModifiedMsg        { get; set; } = false;
        public static bool ShowModifiedChat       { get; set; } =  true;
        public static bool ShowIllegalSignedChat  { get; set; } =  true;
        public static bool SignMessageInCommand   { get; set; } = false;
        public static bool SignChat               { get; set; } = false;

        public static double MessageCooldown      { get; set; } = 1.0D;

        public static bool LoginWithSecureProfile { get; set; } = false;

        public static string Language { get; set; } = "en_us";
        public static bool DebugMode { get; set; } = true;

        // Custom app variables
        public static CacheType SessionCaching = CacheType.Disk;
        public static CacheType ProfileKeyCaching = CacheType.Disk;
        public static bool ResolveSrvRecords = true;

        // Proxy setup
        public static bool ProxyEnabledLogin { get; set; } = false;
        public static bool ProxyEnabledIngame { get; set; } = false;
        public static ProxyHandler.Type ProxyType { get; set; }
        public static string ProxyHost { get; set; }
        public static int ProxyPort { get; set; }
        public static string ProxyUsername { get; set; }
        public static string ProxyPassword { get; set; }

        // Minecraft Settings
        public static MCSettings MCSettings { get; } = new();

    }

}
