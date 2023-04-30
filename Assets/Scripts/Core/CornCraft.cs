using System.Collections.Generic;
using MinecraftClient.Proxy;
using MinecraftClient.Protocol.Session;

namespace MinecraftClient
{
    public static class CornCraft
    {
        public const string Version = "1.0.0";
        public const string BrandInfo = "CornCraft/" + Version;
        public static bool DebugMode { get; set; } = true;

        public static bool DisplaySystemMessages  { get; set; } =  true;
        public static bool DisplayXPBarMessages   { get; set; } =  true;
        public static bool MarkIllegallySignedMsg { get; set; } = false;
        public static bool MarkLegallySignedMsg   { get; set; } = false;
        public static bool MarkSystemMessage      { get; set; } = false;
        public static bool MarkModifiedMsg        { get; set; } = false;
        public static bool ShowModifiedChat       { get; set; } = false;
        public static bool ShowIllegalSignedChat  { get; set; } = false;
        public static bool SignMessageInCommand   { get; set; } = false;
        public static bool SignChat               { get; set; } = false;

        public static double MessageCooldown      { get; set; } = 1.0D;

        public static bool LoginWithSecureProfile { get; set; } = false;

        // CornClient Language, not in-game language
        public static string Language { get; set; } = "en_GB";

        // Custom app variables
        private static readonly Dictionary<string, object> AppVars = new Dictionary<string, object>();
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
        public static bool MCSettings_Enabled = true;
        public static string MCSettings_Locale = "en_US";
        public static byte MCSettings_Difficulty = 0;
        public static byte MCSettings_RenderDistance = 10;
        public static byte MCSettings_ChatMode = 0;
        public static bool MCSettings_ChatColors = true;
        public static byte MCSettings_MainHand = 0;
        public static bool MCSettings_Skin_Hat = true;
        public static bool MCSettings_Skin_Cape = true;
        public static bool MCSettings_Skin_Jacket = false;
        public static bool MCSettings_Skin_Sleeve_Left = false;
        public static bool MCSettings_Skin_Sleeve_Right = false;
        public static bool MCSettings_Skin_Pants_Left = false;
        public static bool MCSettings_Skin_Pants_Right = false;
        public static byte MCSettings_Skin_All
        {
            get
            {
                return (byte)(
                      ((MCSettings_Skin_Cape ? 1 : 0) << 0)
                    | ((MCSettings_Skin_Jacket ? 1 : 0) << 1)
                    | ((MCSettings_Skin_Sleeve_Left ? 1 : 0) << 2)
                    | ((MCSettings_Skin_Sleeve_Right ? 1 : 0) << 3)
                    | ((MCSettings_Skin_Pants_Left ? 1 : 0) << 4)
                    | ((MCSettings_Skin_Pants_Right ? 1 : 0) << 5)
                    | ((MCSettings_Skin_Hat ? 1 : 0) << 6)
                );
            }
        }

        public static List<string> ResourceOverrides { get; } = new();

    }

}
