using System;
using System.IO;
using UnityEngine;

namespace CraftSharp.Resource
{
    public static class TGALoader
    {
        private static byte[] DecodeRLE(byte[] buffer, int bytesPerPixel, int expectedLength, int offset)
        {
            byte[] elements = new byte[bytesPerPixel];
            byte[] decodeBuffer = new byte[expectedLength];

            int decoded = 0;

            while (decoded < expectedLength)
            {
                byte packet = buffer[offset++];

                if ((packet & 0x80) != 0) // RLE data run
                {
                    for (int i = 0;i < bytesPerPixel;i++)
                    {
                        elements[i] = buffer[offset++];
                    }
                    int count = (packet & 0x7F) + 1;
                    for (int i = 0;i < count;i++)
                    {
                        for (int j = 0;j < bytesPerPixel;j++)
                        {
                            decodeBuffer[decoded++] = elements[j];
                        }
                    }
                }
                else // Raw data run
                {
                    int count = (packet + 1) * bytesPerPixel;
                    for (int i = 0;i < count;i++)
                    {
                        decodeBuffer[decoded++] = buffer[offset++];
                    }
                }
            }
            return decodeBuffer;
        }
        
        public static Texture2D TextureFromTGA(byte[] tgaBytes)
        {
            using (var reader = new BinaryReader(new MemoryStream(tgaBytes)))
            {
                // We have to move the stream seek point to the beginning
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                // Image id length
                byte imageIdLength = reader.ReadByte();

                // Color table type: 0 or 1, 0 means no color table
                byte colorTableType = reader.ReadByte();
                // Image type: 0, 1, 2, 3, 9, 10 or 11
                // 0 - no image data
                //                  empty    mapped  truecolor  grayscale
                // uncompressed       0         1         2         3
                //   compressed                 9        10        11
                byte imageType = reader.ReadByte();
                bool compressed = (imageType & 0b1000) != 0; // Bit 3
                int colorType = imageType & 0b0111;

                // Color map information (5 bytes) and origin xy (4 bytes), skip
                reader.BaseStream.Seek(9, SeekOrigin.Current);

                // Image information
                short width = reader.ReadInt16();
                short height = reader.ReadInt16(); 
                int bitDepth = reader.ReadByte();

                // A byte of image specification information
                //reader.BaseStream.Seek(1, SeekOrigin.Current);
                byte imageSpec = reader.ReadByte();
                bool right2left = (imageSpec & 0b010000) != 0; // Bit 4
                bool top2bottom = (imageSpec & 0b100000) != 0; // Bit 5

                Debug.Log($"TGA Info: size: {width}x{height} type: {imageType} id-length: {imageIdLength} bit-depth: {bitDepth} [{imageSpec}] r2l: {right2left} t2b: {top2bottom}");

                if (imageType == 0) // Empty image
                {
                    Debug.LogWarning("TGA file doesn't contain image data!");
                    return new Texture2D(width, height);
                }

                if (colorTableType > 0) // Contains color table, not supprted
                {
                    Debug.LogWarning("TGA files with color tables are not supported!");
                    return new Texture2D(width, height);
                }

                var tex = new Texture2D(width, height);
                var colors = new Color32[width * height];

                int getPos(int index)
                {
                    int row = index / height;
                    int col = index % height;

                    if (right2left) // Flip horizontally
                    {
                        if (top2bottom) // Flip verically
                            return (height - 1 - row) * width + (width - 1 - col);
                        else
                            return height * width + (width - 1 - col);
                    }
                    else
                    {
                        if (top2bottom)
                            return (height - 1 - row) * width + col;
                        else // Do nothing, return as-is
                            return index;
                    }
                }

                byte[] pixelData;

                if (imageIdLength > 0) // Contains image id information, skip
                {
                    reader.BaseStream.Seek(imageIdLength, SeekOrigin.Current);
                }

                if (compressed)
                {
                    pixelData = DecodeRLE(tgaBytes, bitDepth / 8, width * height * (bitDepth / 8), 18 + imageIdLength);
                }
                else
                {
                    pixelData = reader.ReadBytes(width * height * (bitDepth / 8));
                }

                if (bitDepth == 32) // 32bit RGBA
                {
                    for (int i = 0, t = 0;i < width * height;i++, t += 4)
                    {
                        colors[getPos(i)] = new(pixelData[t + 2], pixelData[t + 1], pixelData[t], pixelData[t + 3]);
                    }
                }
                else if (bitDepth == 24) // 24bit RGB
                {
                    for (int i = 0, t = 0;i < width * height;i++, t += 3)
                    {
                        colors[getPos(i)] = new(pixelData[t + 2], pixelData[t + 1], pixelData[t], 255);
                    }
                }
                else if (bitDepth == 8) // 8bit Grayscale
                {
                    for (int i = 0; i < width * height; i++)
                    {
                        colors[getPos(i)] = new(pixelData[i], pixelData[i], pixelData[i], 255);
                    }
                }
                else
                {
                    Debug.LogWarning($"Bit depth of {bitDepth} is not supported, should be one of 8, 24 or 32");
                    return new Texture2D(width, height);
                }
     
                tex.SetPixels32(colors);
                tex.Apply();
                return tex;
            }
        }
    }
}