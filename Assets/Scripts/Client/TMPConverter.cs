using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CraftSharp
{
    public static class TMPConverter
    {
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

        private static string GetTMPColorTag(int rgb)
        {
            return rgb == DEFAULT_COLOR ? string.Empty : $"<color=#{rgb & 0xFFFFFF:X06}>";
        }
        
        private static int GetColorRGB(char color)
        {
            return color switch
            {   // Color codes...
                '0' => 0x000000, // black
                '1' => 0x0000AA, // dark_blue
                '2' => 0x00AA00, // dark_green
                '3' => 0x00AAAA, // dark_aqua
                '4' => 0xAA0000, // dark_red
                '5' => 0xAA00AA, // dark_purple
                '6' => 0xFFAA00, // gold
                '7' => 0xAAAAAA, // gray
                '8' => 0x555555, // dark_gray
                '9' => 0x5555FF, // blue
                'a' => 0x55FF55, // green
                'b' => 0x55FFFF, // aqua
                'c' => 0xFF5555, // red
                'd' => 0xFF55FF, // light_purple
                'e' => 0xFFFF55, // yellow
                'f' => 0xFFFFFF, // white
                'g' => 0xDDD605, // minecoin_gold (this one's for BE, though)
                _   => DEFAULT_COLOR
            };
        }

        private const int DEFAULT_COLOR = -1;

        /// <summary>
        /// Convert Minecraft formatting codes to TextMesh Pro format <br/>
        /// The result text can somehow also be used in Unity Console
        /// </summary>
        public static string MC2TMP(string original)
        {
            Debug.Log($"MC2TMP original: \"{original}\"");
            var processed = new StringBuilder();

            // lastColor: Color used to print last character
            int lastColor = DEFAULT_COLOR;
            int formatFlag = 0;

            for (int ptr = 0; ptr < original.Length; ptr++)
            {
                if (original[ptr] == '§' && (ptr + 1) < original.Length)
                {
                    // Skip section sign and read the formatting code...
                    ptr++;
                    // Make sure this code is in lower case...
                    char code = original.ToLower()[ptr];

                    if (code is >= '0' and <= '9' or >= 'a' and <= 'g' or '#')
                    {
                        int pendingColor;
                        if (code == '#')
                        {
                            ptr += 1; // Skip the hash sign '#'
                            pendingColor = ColorConvert.RGBFromHexString(original[ptr..(ptr + 6)]);
                            ptr += 5; // Skip the color code, only skip 5 here and another will be skipped by the for loop
                        }
                        else
                        {
                            pendingColor = GetColorRGB(code);
                        }
                        
                        if (lastColor == pendingColor) // Ignore it, and don't touch anything
                            continue;
                        
                        // Mark the new current color
                        lastColor = pendingColor;

                        // Close all format tags
                        processed.Append(GetTMPCloseTags(formatFlag));
                        
                        // Apply new color tag
                        processed.Append(GetTMPColorTag(pendingColor));
                        
                        // Set only the line color bit
                        formatFlag = 1 << 5;
                    }
                    else
                    {
                        string text2append = string.Empty;

                        switch (code)
                        {   // Format codes...
                            case 'k':  // obfuscated
                                if ((formatFlag & (1 << 0)) == 0)
                                    text2append = "<rotate=45>";
                                formatFlag |= 1 << 0;
                                break;
                            case 'l':  // bold
                                if ((formatFlag & (1 << 1)) == 0)
                                    text2append = "<b>";
                                formatFlag |= 1 << 1;
                                break;
                            case 'm':  // strikethrough
                                if ((formatFlag & (1 << 2)) == 0)
                                    text2append = "<s>";
                                formatFlag |= 1 << 2;
                                break;
                            case 'n':  // underline
                                if ((formatFlag & (1 << 3)) == 0)
                                    text2append = "<u>";
                                formatFlag |= 1 << 3;
                                break;
                            case 'o':  // italic
                                if ((formatFlag & (1 << 4)) == 0)
                                    text2append = "<i>";
                                formatFlag |= 1 << 4;
                                break;
                            case '<': // interactable start
                                ptr++; // Skip '@' symbol before reading the action
                                var action = new StringBuilder();
                                while (original[ptr] != '§') // This character will be skipped in for loop
                                    action.Append(original[ptr++]);
                                text2append = $"<link=\"{action}\">";
                                break;
                            case '>': // interactable end
                                text2append = "</link>";
                                break;
                            case 'r': // reset
                                text2append = GetTMPCloseTags(formatFlag);
                                formatFlag &= 1 << 6; // Keep only interactable bit
                                lastColor = DEFAULT_COLOR;
                                break;
                            default:
                                text2append = string.Empty;
                                break;
                        };

                        processed.Append(text2append); // No original text appended, format tags only
                    }

                }
                else // Normal characters, output as-is
                {
                    processed.Append(original[ptr]); // Append part of original text
                }
            }

            if (formatFlag != 0) // There're still unclosed tags, close 'em...
                processed.Append(GetTMPCloseTags(formatFlag));

            return processed.ToString();
        }
    }
}