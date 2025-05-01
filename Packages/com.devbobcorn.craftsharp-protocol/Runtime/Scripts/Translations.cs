using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CraftSharp.Protocol;
using UnityEngine;

namespace CraftSharp
{
    public static class Translations
    {
        private static readonly Dictionary<string, string> translations;
        private static readonly Regex translationKeyRegex = new(@"\(\[(.*?)\]\)", RegexOptions.Compiled); // Extract string inside ([ ])

        /// <summary>
        /// Return a translation for the requested text. Support string formatting. If not found, return the original text
        /// </summary>
        /// <param name="msgName">text identifier</param>
        /// <param name="args"></param>
        /// <returns>Translated text or original text if not found</returns>
        /// <remarks>Useful when not sure msgName is a translation mapping key or a normal text</remarks>
        public static string Get(string msgName, params object[] args)
        {
            if (translations.ContainsKey(msgName))
            {
                return args.Length > 0 ? string.Format(translations[msgName], args) : translations[msgName];
            }
            
            return ChatParser.TryTranslateString(msgName, out var translated,
                args.Length > 0 ? args.Select(x => x.ToString()).ToList() : null) ? translated : msgName.ToUpper();
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

        private static string ReplaceKey(Match m) => Get(m.Groups[1].Value);

        /// <summary>
        /// Initialize app translations.
        /// </summary>
        static Translations()
        {
            translations = new Dictionary<string, string>();
            LoadTranslationsFile(ProtocolSettings.Language);
        }

        /// <summary>
        /// Load translation file for current language
        /// </summary>
        private static void LoadTranslationsFile(string language)
        {
            string path = string.Empty, defaultTexts;
            try {
                path = PathHelper.GetExtraDataFile($"app_lang{Path.DirectorySeparatorChar}{language}.lang");
                defaultTexts = File.ReadAllText(path);
            } catch {
                defaultTexts = string.Empty;
                Debug.LogWarning($"Failed to load default translation texts from {path}");
            }

            string[] engLang = defaultTexts.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); // use embedded translations
            ParseTranslationContent(engLang);
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

    }
}