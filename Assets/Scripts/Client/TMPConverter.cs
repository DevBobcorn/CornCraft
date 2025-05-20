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
            
            Stack<int> fieldColors = new();

            // lineColor: Color used outside of bracket fields
            // curColor:  Color used to print next character
            // lastColor: Color used to print last character
            int lineColor = DEFAULT_COLOR, curColor = DEFAULT_COLOR, lastColor = DEFAULT_COLOR;
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
                        curColor = pendingColor;

                        // Close all format tags, but don't append color tags yet. Color tags will be applied later
                        processed.Append(GetTMPCloseTags(formatFlag));
                        
                        // Preserve only the line color bit
                        formatFlag &= (1 << 5);
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
                            case 'r': // reset
                                text2append = GetTMPCloseTags(formatFlag);
                                formatFlag = 0;
                                if (fieldColors.Count == 0) // Not in a bracket field now
                                    lineColor = curColor = DEFAULT_COLOR;
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

                    if (curColor != DEFAULT_COLOR)
                        processed.Append(GetTMPColorTag(curColor));
                    
                    processed.Append("["); // Append part of original text
                    
                    lastColor = curColor;
                }
                else if (original[ptr] == ']') // Right bracket, a field ends...
                {
                    string text2append;

                    if (fieldColors.Count > 0) // End this bracket field and close the color tag of this field
                    {
                        int fieldColor = fieldColors.Pop();
                        text2append = fieldColor != DEFAULT_COLOR ? "]</color>" : "]";
                    }
                    else
                        text2append = "]";
                    
                    if (fieldColors.Count > 0) // Then we enter an outer field
                    {
                        if (fieldColors.Peek() != DEFAULT_COLOR)
                        {
                            // Switch to the color of this out bracket field
                            text2append += GetTMPColorTag(fieldColors.Peek());
                        }
                        processed.Append(text2append);
                    }
                    else // Then we're not in any bracket fields now, use line color
                    {
                        if (lineColor != DEFAULT_COLOR && curColor != lineColor) // Then apply(restore to) this color...
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
                        {
                            processed.Append(text2append);
                        }
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
    }
}