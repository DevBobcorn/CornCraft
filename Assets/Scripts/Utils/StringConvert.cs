using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace MinecraftClient
{
    public static class StringConvert
    {
        /// <summary>
        /// Convert the specified string to an integer, defaulting to zero if invalid argument
        /// </summary>
        /// <param name="str">String to parse as an integer</param>
        /// <returns>Integer value</returns>
        public static int str2int(string str)
        {
            try
            {
                return Convert.ToInt32(str.Trim());
            }
            catch {
                Translations.LogError("error.setting.str2int", str);
                return 0;
            }
        }

        /// <summary>
        /// Convert the specified string to a float number, defaulting to zero if invalid argument
        /// </summary>
        /// <param name="str">String to parse as a float number</param>
        /// <returns>Float number</returns>
        public static float str2float(string str)
        {
            float num;
            if (float.TryParse(str.Trim(), out num))
                return num;
            else
            {
                Translations.LogError("error.setting.str2int", str);
                return 0;
            }
        }

        /// <summary>
        /// Convert the specified string to a boolean value, defaulting to false if invalid argument
        /// </summary>
        /// <param name="str">String to parse as a boolean</param>
        /// <returns>Boolean value</returns>
        public static bool str2bool(string str)
        {
            if (String.IsNullOrEmpty(str))
                return false;
            str = str.Trim().ToLowerInvariant();
            return str == "true" || str == "1";
        }

        // Convert a Minecraft time of day to 24-hour string format
        // See https://minecraft.fandom.com/wiki/Daylight_cycle
        public static string TimeOfDay2String(long timeOfDay)
        {
            int tod = (int)(timeOfDay % 24000L);

            int hour = (tod / 1000 + 6) % 24;
            int minute = (int)((tod % 1000) / 1000F * 60F);

            return $"{hour:00}:{minute:00}";

        }

        private static string GetTMPCloseTags(int formatFlag)
        {
            var closeTags = new StringBuilder();

            if ((formatFlag & 1 << 0) != 0) // 1st bit on, obfuscated
                closeTags.Append("</rotate>");
            if ((formatFlag & 1 << 1) != 0) // 2nd bit on, bold
                closeTags.Append("</b>");
            if ((formatFlag & 1 << 2) != 0) // 3rd bit on, strikethrough
                closeTags.Append("</s>");
            if ((formatFlag & 1 << 3) != 0) // 4th bit on, underline
                closeTags.Append("</u>");
            if ((formatFlag & 1 << 4) != 0) // 5th bit on, italic
                closeTags.Append("</i>");
            if ((formatFlag & 1 << 5) != 0) // 6th bit on, line colored
                closeTags.Append("</color>");
            
            return closeTags.ToString();
        }

        private static string GetTMPColorTag(char color)
        {
            return color switch
            {   // Color codes...
                '0' => "<color=#000000>", // black
                '1' => "<color=#0000AA>", // dark_blue
                '2' => "<color=#00AA00>", // dark_green
                '3' => "<color=#00AAAA>", // dark_aqua
                '4' => "<color=#AA0000>", // dark_red
                '5' => "<color=#AA00AA>", // dark_purple
                '6' => "<color=#FFAA00>", // gold
                '7' => "<color=#AAAAAA>", // gray
                '8' => "<color=#555555>", // dark_gray
                '9' => "<color=#5555FF>", // blue
                'a' => "<color=#55FF55>", // green
                'b' => "<color=#55FFFF>", // aqua
                'c' => "<color=#FF5555>", // red
                'd' => "<color=#FF55FF>", // light_purple
                'e' => "<color=#FFFF55>", // yellow
                'f' => "<color=#FFFFFF>", // white
                'g' => "<color=#DDD605>", // minecoin_gold (this one's for BE, though)
                _   => string.Empty
            };
        }

        // Convert Minecraft formatting codes to TextMesh Pro format
        // The result text can somehow also be used in Unity Console
        public static string MC2TMP(string original)
        {
            var processed = new StringBuilder();
            
            Stack<char> fieldColors = new Stack<char>();

            // lineColor: Color used outside of bracket fields
            // curColor:  Color used to print next character
            // lastColor: Color used to print last character
            char lineColor = ' ', curColor = ' ', lastColor = ' ';
            int formatFlag = 0;

            for (int ptr = 0;ptr < original.Length;ptr++)
            {
                if (original[ptr] == '§' && (ptr + 1) < original.Length)
                {
                    // Skip section sign and read the formatting code...
                    ptr++;
                    // Make sure this code is in lower case...
                    char code = original.ToLower()[ptr];

                    if (code >= '0' && code <= '9' || code >= 'a' && code <= 'g')
                    {
                        if (lastColor == code) // Ignore it, and don't touch anything
                            continue;
                        
                        // Mark the new current color
                        curColor = code;

                        // Close all format tags, but don't append color tags yet. Color tags will be applied later
                        processed.Append(GetTMPCloseTags(formatFlag));
                        
                        // Preserve only the line color bit
                        formatFlag = formatFlag & (1 << 5);
                    }
                    else
                    {
                        string text2append = string.Empty;

                        switch (code)
                        {   // Format codes...
                            case'k':  // obfuscated
                                if ((formatFlag & (1 << 0)) == 0)
                                    text2append = "<rotate=45>";
                                formatFlag |= 1 << 0;
                                break;
                            case'l':  // bold
                                if ((formatFlag & (1 << 1)) == 0)
                                    text2append = "<b>";
                                formatFlag |= 1 << 1;
                                break;
                            case'm':  // strikethrough
                                if ((formatFlag & (1 << 2)) == 0)
                                    text2append = "<s>";
                                formatFlag |= 1 << 2;
                                break;
                            case'n':  // underline
                                if ((formatFlag & (1 << 3)) == 0)
                                    text2append = "<u>";
                                formatFlag |= 1 << 3;
                                break;
                            case'o':  // italic
                                if ((formatFlag & (1 << 4)) == 0)
                                    text2append = "<i>";
                                formatFlag |= 1 << 4;
                                break;
                            case 'r': // reset
                                text2append = GetTMPCloseTags(formatFlag);
                                formatFlag = 0;
                                if (fieldColors.Count == 0) // Not a a bracket field now
                                    lineColor = curColor = ' ';
                                break;
                            default:
                                text2append = string.Empty;
                                break;
                        };

                        processed.Append(text2append); // No original text appended, format tags only
                    }

                }
                else if (original[ptr] == '[') // Left bracket, a field starts...
                {
                    if ((formatFlag & (1 << 5)) != 0)
                    {
                        formatFlag -= (1 << 5); // Clear line color flag
                        processed.Append("</color>"); // Close line color tag
                    }

                    // push color
                    fieldColors.Push(curColor);

                    if (curColor != ' ')
                        processed.Append(GetTMPColorTag(curColor));
                    
                    processed.Append("["); // Append part of original text
                    
                    lastColor = curColor;
                }
                else if (original[ptr] == ']') // Right bracket, a field ends...
                {
                    string text2append;

                    if (fieldColors.Count > 0) // End this bracket field and close the color tag of this field
                    {
                        char fieldColor = fieldColors.Pop();

                        if (fieldColor != ' ')
                            text2append = "]</color>";
                        else
                            text2append = "]";
                    }
                    else
                        text2append = "]";
                    
                    if (fieldColors.Count > 0) // Then we enter an outer field
                    {
                        if (fieldColors.Peek() != ' ')
                        {
                            // Switch to the color of this out bracket field
                            text2append += GetTMPColorTag(fieldColors.Peek());
                        }
                    }
                    else // Then we're not in any bracket fields now, use line color
                    {
                        if (lineColor != ' ' && curColor != lineColor) // Then apply(restore to) this color...
                        {
                            // End line color if present
                            if ((formatFlag & (1 << 5)) != 0)
                                text2append = "</color>";
                            
                            // Then apply this color
                            text2append += GetTMPColorTag(lineColor);
                            formatFlag |= (1 << 5);

                            processed.Append(text2append);

                            curColor = lineColor;
                        }
                        else
                            processed.Append(text2append);
                    }

                    lastColor = curColor;
                }
                else // Normal characters, output as-is
                {
                    if (curColor != lastColor)
                    {
                        if (fieldColors.Count == 0) // Not currently in any bracket fields
                        {
                            processed.Append(GetTMPColorTag(curColor));
                            lineColor = curColor;

                            formatFlag |= 1 << 5; // Only the 'line colored' flag bit is on, other bits are all turned off
                        }
                    }

                    processed.Append(original[ptr]); // Append part of original text

                    lastColor = curColor;
                }

            }

            if (formatFlag != 0) // There're still unclosed tags, close 'em...
                processed.Append(GetTMPCloseTags(formatFlag));

            return processed.ToString();

        }

        /// <summary>
        /// Format the result and write it to Unity Console
        /// </summary>
        /// <param name="text">Text with formatting codes</param>
        public static void Log(string text)
        {
            Debug.Log(StringConvert.MC2TMP(text));
        }

        /// <summary>
        /// Format the result and write it to Unity Console as warning message
        /// </summary>
        /// <param name="text">Text with formatting codes</param>
        public static void LogWarning(string text)
        {
            Debug.LogWarning(StringConvert.MC2TMP(text));
        }

        /// <summary>
        /// Format the result and write it to Unity Console as error message
        /// </summary>
        /// <param name="text">Text with formatting codes</param>
        public static void LogError(string text)
        {
            Debug.LogError(StringConvert.MC2TMP(text));
        }

    }

}