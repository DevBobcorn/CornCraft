#nullable enable
using System;
using UnityEngine;

namespace CraftSharp
{
    public static class ColorConvert
    {
        public static int GetRGBA(Color32 color)
        {
            return (color.a << 24) + (color.r << 16) + (color.g << 8) + color.b;
        }

        public static int GetRGB(Color32 color)
        {
            return (color.r << 16) + (color.g << 8) + color.b;
        }

        public static int GetOpaqueRGB(Color32 color)
        {
            return (255 << 24) + (color.r << 16) + (color.g << 8) + color.b;
        }

        public static int GetOpaqueRGB(int rgb)
        {
            return (255 << 24) + (rgb & 0xFFFFFF);
        }

        public static Color32 GetColor32(int rgba)
        {
            return new((byte)((rgba & 0xFF0000) >> 16), (byte)((rgba & 0xFF00) >> 8), (byte)(rgba & 0xFF), (byte)(rgba >> 24));
        }

        public static Color32 GetOpaqueColor32(int rgb)
        {
            return new((byte)((rgb & 0xFF0000) >> 16), (byte)((rgb & 0xFF00) >> 8), (byte)(rgb & 0xFF), 255);
        }

        public static Color32 OpaqueColor32FromHexString(string hexrgb)
        {
            return GetOpaqueColor32(RGBFromHexString(hexrgb));
        }

        public static int OpaqueRGBFromHexString(string hexrgb)
        {
            return (255 << 24) + Convert.ToInt32(hexrgb, 16);
        }

        public static int RGBFromHexString(string hexrgb)
        {
            return Convert.ToInt32(hexrgb, 16);
        }

        public static string GetHexRGBString(Color32 color)
        {
            return $"{GetRGB(color) & 0xFFFFFF:X06}";
        }

        public static string GetHexRGBString(int rgb)
        {
            return $"{rgb & 0xFFFFFF:X06}";
        }

    }
}