#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

using CraftSharp.Protocol.Message;
using System.Linq;

namespace CraftSharp.Protocol
{
    /// <summary>
    /// This class parses JSON chat data from MC 1.6+ and returns the appropriate string to be printed.
    /// </summary>

    public static class ChatParser
    {
        public static readonly ResourceLocation CHAT_TYPE_ID = new("chat_type");

        public enum MessageType
        {
            CHAT,
            SAY_COMMAND,
            MSG_COMMAND_INCOMING,
            MSG_COMMAND_OUTGOING,
            TEAM_MSG_COMMAND_INCOMING,
            TEAM_MSG_COMMAND_OUTGOING,
            EMOTE_COMMAND,
            RAW_MSG
        };

        public class MessageTypePalette : IdentifierPalette<MessageType>
        {
            protected override string Name => "MessageType Palette";

            public void Register(ResourceLocation id, int numId, MessageType obj)
            {
                base.AddEntry(id, numId, obj);
            }

            public void RegisterDummy(ResourceLocation id, int numId, MessageType obj)
            {
                base.AddDirectionalEntry(id, numId, obj);
            }

            public void Clear()
            {
                base.ClearEntries();
            }

            protected override MessageType UnknownObject => MessageType.CHAT;
        }

        public static readonly MessageTypePalette MessageTypeRegistry = new();

        public static void ReadChatType((ResourceLocation id, int numId, object? obj)[] chatTypeList)
        {
            MessageTypeRegistry.Clear();
            
            foreach (var (chatName, chatId, _) in chatTypeList)
            {
                try
                {
                    // Convert to ResourceLocation first to make sure namespace is handled properly
                    var messageType = chatName.ToString() switch
                    {
                        "minecraft:chat"                      => MessageType.CHAT,
                        "minecraft:emote_command"             => MessageType.EMOTE_COMMAND,
                        "minecraft:msg_command_incoming"      => MessageType.MSG_COMMAND_INCOMING,
                        "minecraft:msg_command_outgoing"      => MessageType.MSG_COMMAND_OUTGOING,
                        "minecraft:say_command"               => MessageType.SAY_COMMAND,
                        "minecraft:team_msg_command_incoming" => MessageType.TEAM_MSG_COMMAND_INCOMING,
                        "minecraft:team_msg_command_outgoing" => MessageType.TEAM_MSG_COMMAND_OUTGOING,

                        _                                     => throw new InvalidDataException(),
                    };
                    
                    MessageTypeRegistry.Register(chatName, chatId, messageType);
                }
                catch
                {
                    Debug.LogWarning($"Unknown message type {chatName}. Treating as chat message.");
                    MessageTypeRegistry.RegisterDummy(chatName, chatId, MessageType.CHAT);
                }
            }
        }

        /// <summary>
        /// The main function to convert text from MC 1.6+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="json">JSON serialized text</param>
        /// <param name="links">Optional container for links from JSON serialized text</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseText(string json, List<string>? links = null)
        {
            var jsonData = Json.ParseJson(json);
            Debug.Log(jsonData.ToJson());
            return JSONData2String(jsonData, string.Empty, string.Empty, links);
        }

        /// <summary>
        /// The main function to convert text from MC 1.6+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="nbt">NBT dictionary</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseText(Dictionary<string, object> nbt)
        {
            var jsonData = Json.Object2JSONData(nbt);
            Debug.Log(jsonData.ToJson());
            return JSONData2String(jsonData, string.Empty, string.Empty, null);
        }

        /// <summary>
        /// The main function to convert text from MC 1.9+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="message">Message received</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseSignedChat(ChatMessage message)
        {
            string sender = message.isSenderJson ? ParseText(message.displayName!) : message.displayName!;
            string content;
            if (ProtocolSettings.ShowModifiedChat && message.unsignedContent != null)
            {
                content = ParseText(message.unsignedContent!);
                if (string.IsNullOrEmpty(content))
                    content = message.unsignedContent!;
            }
            else
            {
                content = message.isJson ? ParseText(message.content) : message.content;
                if (string.IsNullOrEmpty(content))
                    content = message.content!;
            }

            string text;
            List<string> usingData = new();

            MessageType chatType;
            if (message.chatTypeId == -1)
                chatType = MessageType.RAW_MSG;
            else if (!MessageTypeRegistry.TryGetByNumId(message.chatTypeId, out chatType))
                chatType = MessageType.CHAT;

            switch (chatType)
            {
                case MessageType.CHAT:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.text", usingData);
                    break;
                case MessageType.SAY_COMMAND:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.announcement", usingData);
                    break;
                case MessageType.MSG_COMMAND_INCOMING:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("commands.message.display.incoming", usingData);
                    break;
                case MessageType.MSG_COMMAND_OUTGOING:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("commands.message.display.outgoing", usingData);
                    break;
                case MessageType.TEAM_MSG_COMMAND_INCOMING:
                    usingData.Add(message.teamName!);
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.team.text", usingData);
                    break;
                case MessageType.TEAM_MSG_COMMAND_OUTGOING:
                    usingData.Add(message.teamName!);
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.team.sent", usingData);
                    break;
                case MessageType.EMOTE_COMMAND:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.emote", usingData);
                    break;
                case MessageType.RAW_MSG:
                    text = content;
                    break;
                default:
                    goto case MessageType.CHAT;
            }

            return text;
        }

        /// <summary>
        /// Get the classic color tag corresponding to a color name
        /// </summary>
        /// <param name="colorName">Color Name</param>
        /// <returns>Color code</returns>
        private static string Color2tag(string colorName)
        {
            if (colorName.StartsWith("#")) // #RRGGBB format color code
            {
                if (colorName.Length == 7)
                    return $"§{colorName}";
                
                Debug.LogWarning($"Invalid color code {colorName}");
                return string.Empty;
            }
            
            return colorName.ToLower() switch
            {
                "black"                         => "§0",
                "dark_blue"                     => "§1",
                "dark_green"                    => "§2",
                "dark_aqua" or "dark_cyan"      => "§3",
                "dark_red"                      => "§4",
                "dark_purple" or "dark_magenta" => "§5",
                "gold" or "dark_yellow"         => "§6",
                "gray"                          => "§7",
                "dark_gray"                     => "§8",
                "blue"                          => "§9",
                "green"                         => "§a",
                "aqua" or "cyan"                => "§b",
                "red"                           => "§c",
                "light_purple" or "magenta"     => "§d",
                "yellow"                        => "§e",
                "white"                         => "§f",
                _                               => string.Empty
            };
        }

        /// <summary>
        /// Set of translation rules for formatting text
        /// </summary>
        private static readonly Dictionary<string, string> translationRules = new();

        /// <summary>
        /// Rule initialization method.
        /// </summary>
        public static void LoadTranslationRules(string langFile)
        {
            translationRules.Clear(); // Clear loaded stuffs

            // Small default dictionary of translation rules
            translationRules["chat.type.admin"] = "[%s: %s]";
            translationRules["chat.type.announcement"] = "§d[%s] %s";
            translationRules["chat.type.emote"] = " * %s %s";
            translationRules["chat.type.text"] = "<%s> %s";
            translationRules["multiplayer.player.joined"] = "§e%s joined the game.";
            translationRules["multiplayer.player.left"] = "§e%s left the game.";
            translationRules["commands.message.display.incoming"] = "§7%s whispers to you: %s";
            translationRules["commands.message.display.outgoing"] = "§7You whisper to %s: %s";

            // Load the external dictionnary of translation rules or display an error message
            if (File.Exists(langFile))
            {
                var translations = Json.ParseJson(File.ReadAllText(langFile));
                foreach (var text in translations.Properties)
                    translationRules[text.Key] = text.Value.StringValue;

                if (ProtocolSettings.DebugMode)
                    Debug.Log(Translations.Get("chat.loaded"));
            }
            else // No external dictionary found.
                Debug.Log(Translations.Get("chat.not_found", langFile));
        }

        private static string InterpolateString(string template, List<string> data)
        {
            int usingIdx = 0;
            var result = new StringBuilder();
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] == '%' && i + 1 < template.Length)
                {
                    //Using string or int with %s or %d
                    if (template[i + 1] == 's' || template[i + 1] == 'd')
                    {
                        if (data.Count > usingIdx)
                        {
                            result.Append(data[usingIdx]);
                            usingIdx++;
                            i += 1;
                            continue;
                        }
                    }

                    //Using specified string or int with %1$s, %2$s...
                    else if (char.IsDigit(template[i + 1])
                        && i + 3 < template.Length && template[i + 2] == '$'
                        && (template[i + 3] == 's' || template[i + 3] == 'd'))
                    {
                        int specified_idx = template[i + 1] - '1';
                        if (data.Count > specified_idx)
                        {
                            result.Append(data[specified_idx]);
                            usingIdx++;
                            i += 3;
                            continue;
                        }
                    }
                }
                result.Append(template[i]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Format text using a specific formatting rule.
        /// Example : * %s %s + ["ORelio", "is doing something"] = * ORelio is doing something
        /// </summary>
        /// <param name="ruleName">Name of the rule, chosen by the server</param>
        /// <param name="usingData">Data to be used in the rule</param>
        /// <returns>Returns the formatted text according to the given data</returns>
        public static string TranslateString(string ruleName, List<string>? usingData = null)
        {
            if (translationRules.ContainsKey(ruleName))
            {
                if (usingData is not null)
                    return InterpolateString(translationRules[ruleName], usingData);
                else
                    return translationRules[ruleName];
                
            }
            else
                return usingData is null ? $"[{ruleName}]" : $"[{ruleName}] {string.Join(" ", usingData)}";
        }

        /// <summary>
        /// Format text using a specific formatting rule.
        /// Example : * %s %s + ["ORelio", "is doing something"] = * ORelio is doing something
        /// </summary>
        /// <param name="ruleName">Name of the rule, chosen by the server</param>
        /// <param name="translated">The formatted text according to the given data</param>
        /// <param name="usingData">Data to be used in the rule</param>
        /// <returns></returns>
        public static bool TryTranslateString(string ruleName, out string translated, List<string>? usingData = null)
        {
            if (translationRules.ContainsKey(ruleName))
            {
                translated = usingData is not null ? InterpolateString(translationRules[ruleName], usingData) : translationRules[ruleName];

                return true;
            }

            translated = string.Empty;

            return false;
        }

        private static readonly Dictionary<string, string> FormattingCodes = new() {
            {"obfuscated",    "§k"},
            {"bold",          "§l"},
            {"strikethrough", "§m"},
            {"underlined",    "§n"},
            {"italic",        "§o"}
        };
        
        /// <summary>
        /// Use a JSON Object to build the corresponding string
        /// </summary>
        /// <param name="data">JSON object to convert</param>
        /// <param name="parentColorCode">Last parent color code before entering child scope</param>
        /// <param name="parentFlags">Parent formatting flags</param>
        /// <param name="links">Container for links from JSON serialized text</param>
        /// <returns>returns the Minecraft-formatted string</returns>
        private static string JSONData2String(Json.JSONData data, string parentColorCode, string parentFlags, List<string>? links)
        {
            string extraResult = "";
            
            // Use parent formatting as own formatting by default
            string colorCode = parentColorCode;
            string flags = parentFlags;

            bool clearBeforePush = false;
            bool clearBeforePop = false;
            string pushFormatting = string.Empty;
            string popFormatting = string.Empty;
            
            switch (data.Type)
            {
                case Json.JSONData.DataType.Object:
                    
                    if (data.Properties.TryGetValue("color", out var val))
                    {
                        colorCode = Color2tag(val.StringValue);
                        if (colorCode != parentColorCode)
                        {
                            if (parentColorCode != string.Empty || parentFlags != string.Empty)
                                clearBeforePush = true;
                            clearBeforePop = true;
                            // If color formatting is overriden, other formatting
                            // are also cleared, so we use an assignment here.
                            pushFormatting += colorCode;
                        }
                    }
                    
                    foreach (var pair in FormattingCodes)
                    {
                        if (data.Properties.TryGetValue(pair.Key, out val))
                        {
                            var enable = val.StringValue is "true" or "1";
                            if (enable && !flags.Contains(pair.Value))
                            {
                                clearBeforePop = true;
                                flags += pair.Value; // Add this flag (clean before pop)
                            }
                            else if (!enable && flags.Contains(pair.Value))
                            {
                                clearBeforePush = true;
                                flags = flags.Replace(pair.Value, ""); // Remove this flag (clean before push)
                            }
                        }
                    }
                    pushFormatting += flags;
                    
                    if (clearBeforePush) pushFormatting = $"§r{pushFormatting}";
                    if (clearBeforePop) popFormatting = $"§r{parentColorCode}{parentFlags}";
                    
                    if (data.Properties.ContainsKey("clickEvent") && links != null)
                    {
                        Json.JSONData clickEvent = data.Properties["clickEvent"];
                        if (clickEvent.Properties.ContainsKey("action")
                            && clickEvent.Properties.ContainsKey("value")
                            && clickEvent.Properties["action"].StringValue == "open_url"
                            && !string.IsNullOrEmpty(clickEvent.Properties["value"].StringValue))
                        {
                            links.Add(clickEvent.Properties["value"].StringValue);
                        }
                    }
                    
                    if (data.Properties.TryGetValue("extra", out val)) // Nested text components
                    {
                        Json.JSONData[] extras = val.DataArray.ToArray();
                        extraResult = extras.Aggregate(extraResult, (current, item) => current + JSONData2String(item, colorCode, flags, links));
                    }
                    
                    if (data.Properties.TryGetValue("text", out val))
                    {
                        return pushFormatting + JSONData2String(val, colorCode, flags, links) + extraResult + popFormatting;
                    }
                    
                    if (data.Properties.TryGetValue(string.Empty, out val)) // Text field can be anonymous, do the same as above
                    {
                        return pushFormatting + JSONData2String(val, colorCode, flags, links) + extraResult + popFormatting;
                    }
                    
                    if (data.Properties.ContainsKey("translate"))
                    {
                        List<string> usingData = new List<string>();
                        if (data.Properties.ContainsKey("using") && !data.Properties.ContainsKey("with"))
                            data.Properties["with"] = data.Properties["using"];
                        if (data.Properties.TryGetValue("with", out val))
                        {
                            Json.JSONData[] array = val.DataArray.ToArray();
                            usingData.AddRange(array.Select(t => JSONData2String(t, colorCode, flags, links)));
                        }
                        return pushFormatting + TranslateString(data.Properties["translate"].StringValue, usingData) + extraResult + popFormatting;
                    }
                    
                    return extraResult;

                case Json.JSONData.DataType.Array:
                    string result = data.DataArray.Aggregate(string.Empty,
                        (current, item) => current + JSONData2String(item, colorCode, flags, links));
                    return pushFormatting + result + popFormatting;

                case Json.JSONData.DataType.String:
                    return pushFormatting + data.StringValue + popFormatting;
            }

            return string.Empty;
        }
    }
}
