using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace MinecraftClient
{
    /// <summary>
    /// Allows to localize MinecraftClient in different languages
    /// </summary>
    /// <remarks>
    /// By ORelio (c) 2015-2018 - CDDL 1.0
    /// </remarks>
    public static class Translations
    {
        public static string TranslationsFile_FromMCDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\.minecraft\assets\objects\8b\8bf1298bd44b0e5b21d747394a8acd2c218e09ed"; //MC 1.17 en_GB.lang
        public static string TranslationsFile_Website_Index = "https://launchermeta.mojang.com/v1/packages/e5af543d9b3ce1c063a97842c38e50e29f961f00/1.17.json";
        public static string TranslationsFile_Website_Download = "http://resources.download.minecraft.net";

        private static Dictionary<string, string> translations;
        private static string translationFilePath = "Lang" + Path.DirectorySeparatorChar + "mcc";
        private static string defaultTranslation = "en.ini";
        private static Regex translationKeyRegex = new Regex(@"\(\[(.*?)\]\)", RegexOptions.Compiled); // Extract string inside ([ ])

        /// <summary>
        /// Return a tranlation for the requested text. Support string formatting
        /// </summary>
        /// <param name="msgName">text identifier</param>
        /// <returns>returns translation for this identifier</returns>
        public static string Get(string msgName, params object[] args)
        {
            if (translations.ContainsKey(msgName))
            {
                if (args.Length > 0)
                {
                    return string.Format(translations[msgName], args);
                }
                else return translations[msgName];
            }
            return msgName.ToUpper();
        }

        /// <summary>
        /// Return a tranlation for the requested text. Support string formatting. If not found, return the original text
        /// </summary>
        /// <param name="msgName">text identifier</param>
        /// <param name="args"></param>
        /// <returns>Translated text or original text if not found</returns>
        /// <remarks>Useful when not sure msgName is a translation mapping key or a normal text</remarks>
        public static string TryGet(string msgName, params object[] args)
        {
            if (translations.ContainsKey(msgName))
                return Get(msgName, args);
            else return msgName;
        }

        /// <summary>
        /// Replace the translation key inside a sentence to translated text. Wrap the key in ([translation.key])
        /// </summary>
        /// <example>
        /// e.g.  I only want to replace ([this])
        /// would only translate "this" without touching other words.
        /// </example>
        /// <param name="msg">Sentence for replace</param>
        /// <param name="args"></param>
        /// <returns>Translated sentence</returns>
        public static string Replace(string msg, params object[] args)
        {
            string translated = translationKeyRegex.Replace(msg, new MatchEvaluator(ReplaceKey));
            if (args.Length > 0)
                return string.Format(translated, args);
            else return translated;
        }

        private static string ReplaceKey(Match m)
        {
            return Get(m.Groups[1].Value);
        }

        /// <summary>
        /// Initialize translations depending on system language.
        /// English is the default for all unknown system languages.
        /// </summary>
        static Translations()
        {
            translations = new Dictionary<string, string>();
            LoadDefaultTranslationsFile();
        }

        /// <summary>
        /// Load default translation file (English)
        /// </summary>
        /// <remarks>
        /// This will be loaded during program start up.
        /// </remarks>
        private static void LoadDefaultTranslationsFile()
        {
            string path = string.Empty, defaultTexts;
            try {
                path = Application.streamingAssetsPath + "/Lang/en.ini";
                defaultTexts = File.ReadAllText(path);
            } catch {
                defaultTexts = string.Empty;
                Debug.LogWarning("Failed to load default translation texts from " + path);
            }

            string[] engLang = defaultTexts.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); // use embedded translations
            ParseTranslationContent(engLang);
        }

        /// <summary>
        /// Load translation file depends on system language or by giving a file path. Default to English if translation file does not exist
        /// </summary>
        public static void LoadExternalTranslationFile(string language)
        {
            /*
             * External translation files
             * These files are loaded from the installation directory as:
             * Lang/abc.ini, e.g. Lang/eng.ini which is the default language file
             * Useful for adding new translations of fixing typos without recompiling
             */

            // Try to convert Minecraft language file name to two letters language name
            if (language == "zh_cn")
                language = "zh-CHS";
            else if (language == "zh_tw")
                language = "zh-CHT";
            else
                language = language.Split('_')[0];

            string systemLanguage = string.IsNullOrEmpty(CultureInfo.CurrentCulture.Parent.Name) // Parent.Name might be empty
                    ? CultureInfo.CurrentCulture.Name
                    : CultureInfo.CurrentCulture.Parent.Name;
            string langDir = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar;
            string langFileSystemLanguage = langDir + systemLanguage + ".ini";
            string langFileConfigLanguage = langDir + language + ".ini";

            if (File.Exists(langFileConfigLanguage))
            {// Language set in ini config
                ParseTranslationContent(File.ReadAllLines(langFileConfigLanguage));
                return;
            }

            if (File.Exists(langFileSystemLanguage))
            {// Fallback to system language
                ParseTranslationContent(File.ReadAllLines(langFileSystemLanguage));
                return;
            }

        }

        /// <summary>
        /// Parse the given array to translation map
        /// </summary>
        /// <param name="content">Content of the translation file (in ini format)</param>
        private static void ParseTranslationContent(string[] content)
        {
            foreach (string lineRaw in content)
            {
                string line = lineRaw.Trim();
                if (line.Length <= 0)
                    continue;
                if (line.StartsWith("#")) // ignore comment line started with #
                    continue;
                if (line[0] == '[' && line[line.Length - 1] == ']') // ignore section
                    continue;

                string translationName = line.Split('=')[0];
                if (line.Length > (translationName.Length + 1))
                {
                    string translationValue = line.Substring(translationName.Length + 1).Replace("\\n", "\n");
                    translations[translationName] = translationValue;
                }
            }
        }

        /// <summary>
        /// Write the default translation file (English) to the disk.
        /// </summary>
        private static void WriteDefaultTranslation()
        {
            string defaultPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + translationFilePath + Path.DirectorySeparatorChar + defaultTranslation;

            if (!Directory.Exists(translationFilePath))
            {
                Directory.CreateDirectory(translationFilePath);
            }

            string path = string.Empty, defaultTexts;
            try {
                path = Application.streamingAssetsPath + "/Lang/en.ini";
                defaultTexts = File.ReadAllText(path);
            } catch {
                defaultTexts = string.Empty;
                Debug.LogWarning("Failed to load default translation texts from " + path);
            }

            File.WriteAllText(defaultPath, defaultTexts, Encoding.UTF8);
        }

        #region Console writing method wrapper

        /// <summary>
        /// Translate the key, format the result and write it to Unity Console
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="args"></param>
        public static void Log(string key, params object[] args)
        {
            string text = args.Length > 0 ? string.Format(Get(key), args) : Get(key);
            Debug.Log(StringConvert.MC2TMP(text));

            Loom.QueueOnMainThread(
                () => Notify(RemoveFormatting(text), 6F, UI.Notification.Type.Notification)
            );
        }

        /// <summary>
        /// Translate the key, format the result and write it to Unity Console as warning message
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="args"></param>
        public static void LogWarning(string key, params object[] args)
        {
            string text = args.Length > 0 ? string.Format(Get(key), args) : Get(key);
            Debug.LogWarning(StringConvert.MC2TMP("\u00a7e" + text));

            // Add yellow color prefix
            Loom.QueueOnMainThread(
                () => Notify(RemoveFormatting(text), 6F, UI.Notification.Type.Warning)
            );
        }

        /// <summary>
        /// Translate the key, format the result and write it to Unity Console as error message
        /// </summary>
        /// <param name="key">Translation key</param>
        /// <param name="args"></param>
        public static void LogError(string key, params object[] args)
        {
            string text = args.Length > 0 ? string.Format(Get(key), args) : Get(key);
            Debug.LogError(StringConvert.MC2TMP("\u00a74" + text));

            // Add red color prefix, and notify on Unity thread via Loom
            Loom.QueueOnMainThread(
                () => Notify(RemoveFormatting(text), 6F, UI.Notification.Type.Error)
            );
        }

        private static void Notify(string text, float duration, UI.Notification.Type type)
        {
            CornClient.ShowNotification(text, duration, type);
        }

        public static string RemoveFormatting(string original)
        {
            // Remove all Minecraft formatting codes from it
            return Regex.Replace(original, "\u00a7.", "");
        }

        #endregion
    }
}