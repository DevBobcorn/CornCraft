﻿#nullable enable
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
            return JSONData2String(Json.ParseJson(json), "", links);
        }

        public static string ParseText(Dictionary<string, object> nbt)
        {
            return NbtToString(nbt);
        }

        /// <summary>
        /// The main function to convert text from MC 1.9+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="message">Message received</param>
        /// <param name="links">Optional container for links from JSON serialized text</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseSignedChat(ChatMessage message, List<string>? links = null)
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
        /// <param name="colorname">Color Name</param>
        /// <returns>Color code</returns>
        private static string Color2tag(string colorname)
        {
            switch (colorname.ToLower())
            {
                /* MC 1.7+ Name           MC 1.6 Name           Classic tag */
                case "black":        /*  Blank if same  */      return "§0";
                case "dark_blue":                               return "§1";
                case "dark_green":                              return "§2";
                case "dark_aqua":       case "dark_cyan":       return "§3";
                case "dark_red":                                return "§4";
                case "dark_purple":     case "dark_magenta":    return "§5";
                case "gold":            case "dark_yellow":     return "§6";
                case "gray":                                    return "§7";
                case "dark_gray":                               return "§8";
                case "blue":                                    return "§9";
                case "green":                                   return "§a";
                case "aqua":            case "cyan":            return "§b";
                case "red":                                     return "§c";
                case "light_purple":    case "magenta":         return "§d";
                case "yellow":                                  return "§e";
                case "white":                                   return "§f";
                default: return "";
            }
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

            // Small default dictionnary of translation rules
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
        /// <param name="rulename">Name of the rule, chosen by the server</param>
        /// <param name="usingData">Data to be used in the rule</param>
        /// <returns>Returns the formatted text according to the given data</returns>
        public static string TranslateString(string rulename, List<string>? usingData = null)
        {
            if (translationRules.ContainsKey(rulename))
            {
                if (usingData is not null)
                    return InterpolateString(translationRules[rulename], usingData);
                else
                    return translationRules[rulename];
                
            }
            else
                return usingData is null ? $"[{rulename}]" : $"[{rulename}] {string.Join(" ", usingData)}";
        }

        /// <summary>
        /// Format text using a specific formatting rule.
        /// Example : * %s %s + ["ORelio", "is doing something"] = * ORelio is doing something
        /// </summary>
        /// <param name="rulename">Name of the rule, chosen by the server</param>
        /// <param name="translated">The formatted text according to the given data</param>
        /// <param name="usingData">Data to be used in the rule</param>
        /// <returns></returns>
        public static bool TryTranslateString(string rulename, out string translated, List<string>? usingData = null)
        {
            if (translationRules.ContainsKey(rulename))
            {
                if (usingData is not null)
                    translated = InterpolateString(translationRules[rulename], usingData);
                else
                    translated = translationRules[rulename];
                
                return true;
            }
            else
            {
                translated = string.Empty;

                return false;
            }
        }

        /// <summary>
        /// Use a JSON Object to build the corresponding string
        /// </summary>
        /// <param name="data">JSON object to convert</param>
        /// <param name="colorcode">Allow parent color code to affect child elements (set to "" for function init)</param>
        /// <param name="links">Container for links from JSON serialized text</param>
        /// <returns>returns the Minecraft-formatted string</returns>
        private static string JSONData2String(Json.JSONData data, string colorcode, List<string>? links)
        {
            string extra_result = "";
            switch (data.Type)
            {
                case Json.JSONData.DataType.Object:
                    if (data.Properties.ContainsKey("color"))
                    {
                        colorcode = Color2tag(JSONData2String(data.Properties["color"], "", links));
                    }
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
                    if (data.Properties.ContainsKey("extra"))
                    {
                        Json.JSONData[] extras = data.Properties["extra"].DataArray.ToArray();
                        foreach (Json.JSONData item in extras)
                            extra_result = extra_result + JSONData2String(item, colorcode, links) + "§r";
                    }
                    if (data.Properties.ContainsKey("text"))
                    {
                        return colorcode + JSONData2String(data.Properties["text"], colorcode, links) + extra_result;
                    }
                    else if (data.Properties.ContainsKey("translate"))
                    {
                        List<string> using_data = new List<string>();
                        if (data.Properties.ContainsKey("using") && !data.Properties.ContainsKey("with"))
                            data.Properties["with"] = data.Properties["using"];
                        if (data.Properties.ContainsKey("with"))
                        {
                            Json.JSONData[] array = data.Properties["with"].DataArray.ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                using_data.Add(JSONData2String(array[i], colorcode, links));
                            }
                        }
                        return colorcode + TranslateString(JSONData2String(data.Properties["translate"], "", links), using_data) + extra_result;
                    }
                    else return extra_result;

                case Json.JSONData.DataType.Array:
                    string result = "";
                    foreach (Json.JSONData item in data.DataArray)
                    {
                        result += JSONData2String(item, colorcode, links);
                    }
                    return result;

                case Json.JSONData.DataType.String:
                    return colorcode + data.StringValue;
            }

            return "";
        }

        private static string NbtToString(Dictionary<string, object> nbt)
        {
            if (nbt.Count == 1 && nbt.TryGetValue("", out object? rootMessage))
            {
                // Nameless root tag
                //return (string)rootMessage;
                if (rootMessage is string rootString)
                {
                    return rootString;
                }
                else
                {
                    return rootMessage is null ? "<null>" : rootMessage.ToString();
                }
            }

            string message = string.Empty;
            string colorCode = string.Empty;
            StringBuilder extraBuilder = new StringBuilder();
            foreach (var kvp in nbt)
            {
                string key = kvp.Key;
                object value = kvp.Value;

                switch (key)
                {
                    case "text":
                        {
                            message = (string)value;
                        }
                        break;
                    case "extra":
                        {
                            object[] extras = (object[])value;
                            for (var i = 0; i < extras.Length; i++)
                            {
                                var extraDict = extras[i] switch
                                {
                                    int => new Dictionary<string, object> { { "text", $"{extras[i]}" } },
                                    string => new Dictionary<string, object>
                                {
                                    { "text", (string)extras[i] }
                                },
                                    _ => (Dictionary<string, object>)extras[i]
                                };

                                extraBuilder.Append(NbtToString(extraDict) + "§r");
                            }
                        }
                        break;
                    case "translate":
                        {
                            if (nbt.TryGetValue("translate", out object translate))
                            {
                                var translateKey = (string)translate;
                                List<string> translateString = new();
                                if (nbt.TryGetValue("with", out object withComponent))
                                {
                                    var withs = (object[])withComponent;
                                    for (var i = 0; i < withs.Length; i++)
                                    {
                                        var withDict = withs[i] switch
                                        {
                                            int => new Dictionary<string, object> { { "text", $"{withs[i]}" } },
                                            string => new Dictionary<string, object>
                                        {
                                            { "text", (string)withs[i] }
                                        },
                                            _ => (Dictionary<string, object>)withs[i]
                                        };

                                        translateString.Add(NbtToString(withDict));
                                    }
                                }

                                message = TranslateString(translateKey, translateString);
                            }
                        }
                        break;
                    case "color":
                        {
                            if (nbt.TryGetValue("color", out object color))
                            {
                                colorCode = Color2tag((string)color);
                            }
                        }
                        break;
                }
            }

            return colorCode + message + extraBuilder.ToString();
        }
    }
}
