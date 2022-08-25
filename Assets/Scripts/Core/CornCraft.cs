using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

using MinecraftClient.Proxy;
using MinecraftClient.Protocol.Session;

namespace MinecraftClient
{
    public static class CornCraft
    {
        public const string Version = "1.0.0";
        public const string BrandInfo = "CornCraft/" + Version;

        public const char internalCmdChar = '/';

        #region Global Settings and Variables
        public static bool DebugMode { get; set; } = false;

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
        public static byte MCSettings_RenderDistance = 8;
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

        /// <summary>
        /// Set a custom %variable% which will be available through expandVars()
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <param name="varData">Value of the variable</param>
        /// <returns>True if the parameters were valid</returns>
        public static bool SetVar(string varName, object varData)
        {
            lock (AppVars)
            {
                varName = new string(varName.TakeWhile(char.IsLetterOrDigit).ToArray()).ToLower();
                if (varName.Length > 0)
                {
                    AppVars[varName] = varData;
                    return true;
                }
                else return false;
            }
        }

        /// <summary>
        /// Get a custom %variable% or null if the variable does not exist
        /// </summary>
        /// <param name="varName">Variable name</param>
        /// <returns>The value or null if the variable does not exists</returns>
        public static object GetVar(string varName)
        {
            if (AppVars.ContainsKey(varName))
                return AppVars[varName];
            return null;
        }

        /// <summary>
        /// Replace %variables% with their value from global AppVars
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="localContext">Optional local variables overriding global variables</param>
        /// <returns>Modifier string</returns>
        public static string ExpandVars(string str, Dictionary<string, object> localVars = null)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '%')
                {
                    bool varname_ok = false;
                    StringBuilder var_name = new StringBuilder();

                    for (int j = i + 1; j < str.Length; j++)
                    {
                        if (!char.IsLetterOrDigit(str[j]) && str[j] != '_')
                        {
                            if (str[j] == '%')
                                varname_ok = var_name.Length > 0;
                            break;
                        }
                        else var_name.Append(str[j]);
                    }

                    if (varname_ok)
                    {
                        string varname = var_name.ToString();
                        string varname_lower = varname.ToLower();
                        i = i + varname.Length + 1;

                        switch (varname_lower)
                        {
                            case "username": result.Append(CornClient.Instance?.GetUsername() ?? "<username>"); break;
                            //case "login": result.Append(Account); break;
                            case "serverip": result.Append(CornClient.Instance?.GetServerHost() ?? "<serverip>"); break;
                            case "serverport": result.Append(CornClient.Instance?.GetServerPort().ToString() ?? "<serverport>"); break;
                            default:
                                if (localVars != null && localVars.ContainsKey(varname_lower))
                                {
                                    result.Append(localVars[varname_lower].ToString());
                                }
                                else if (AppVars.ContainsKey(varname_lower))
                                {
                                    result.Append(AppVars[varname_lower].ToString());
                                }
                                else result.Append("%" + varname + '%');
                                break;
                        }
                    }
                    else result.Append(str[i]);
                }
                else result.Append(str[i]);
            }
            return result.ToString();
        }

        #endregion

        #region Util Functions

        /// <summary>
        /// Verify that a string contains only a-z A-Z 0-9 and _ characters.
        /// </summary>
        public static bool IsValidName(string username)
        {
            if (String.IsNullOrEmpty(username))
                return false;

            foreach (char c in username)
                if (!((c >= 'a' && c <= 'z')
                        || (c >= 'A' && c <= 'Z')
                        || (c >= '0' && c <= '9')
                        || c == '_') )
                    return false;

            return true;
        }

        /// <summary>
        /// Enumerate types in namespace through reflection
        /// </summary>
        /// <param name="nameSpace">Namespace to process</param>
        /// <param name="assembly">Assembly to use. Default is Assembly.GetExecutingAssembly()</param>
        /// <returns></returns>
        public static Type[] GetTypesInNamespace(string nameSpace, Assembly assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetExecutingAssembly(); }
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

        #endregion

    }

}
