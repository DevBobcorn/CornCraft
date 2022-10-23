using UnityEngine;
using Unity.Mathematics;

namespace MinecraftClient
{
    /// <summary>
    /// This class provides a few helper function for dealing with colors
    /// </summary>
    public static class ColorHelper
    {
        public static string GetPreview(int color)
        {
            var colorCode = $"{color:x}".PadLeft(6, '0');
            return $"<color=#{colorCode}>{colorCode}</color>";
        }

        public static int Unity2MC(Color color)
        {
            int r  = (int)(color.r * 255F);
            int g  = (int)(color.g * 255F);
            int b  = (int)(color.b * 255F);

            return (r << 16) | (g << 8) | b;
        }

        public static float3 MC2Float3(int color)
        {
            float r = ((color & 0xFF0000) >> 16) / 255F;
            float g = ((color &   0xFF00) >>  8) / 255F;
            float b = ((color &     0xFF))       / 255F;

            return new(r, g, b);
        }

    }
}