using System;
using System.IO;
using System.Text;
using System.Threading;
using CraftSharp.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class SpriteTypePalette : IdentifierPalette<SpriteType>
    {
        public static readonly SpriteTypePalette INSTANCE = new();
        protected override string Name => "SpriteType Palette";
        protected override SpriteType UnknownObject => SpriteType.DUMMY_SPRITE_TYPE;

        /// <summary>
        /// Load sprite data from external files.
        /// </summary>
        /// <param name="flag">Data load flag</param>
        /// <param name="manager">Resource pack manager</param>
        public void PrepareData(DataLoadFlag flag, ResourcePackManager manager)
        {
            // Clear loaded stuff...
            ClearEntries();

            var spriteTypePath = PathHelper.GetExtraDataFile($"sprite_types.json");

            if (!File.Exists(spriteTypePath))
            {
                Debug.LogWarning("Sprite data not complete!");
                flag.Finished = true;
                flag.Failed = true;
                return;
            }

            var textureFileTable = manager.TextureFileTable;

            try
            {
                var spriteTypes = Json.ParseJson(File.ReadAllText(spriteTypePath, Encoding.UTF8));

                int numId = 0; // Numeral id doesn't matter, it's a client-side only thing

                DataLoadFlag textureFlag = new();

                foreach (var (key, spriteDef) in spriteTypes.Properties)
                {
                    var spriteTypeId = ResourceLocation.FromString(key);
                    var u1 = spriteDef.Properties.TryGetValue("use_item_model", out var val) && bool.Parse(val.StringValue); // False if not specified
                    var u2 = !spriteDef.Properties.TryGetValue("use_point_filter", out val) || bool.Parse(val.StringValue); // True if not specified
                    var invert = spriteDef.Properties.TryGetValue("invert_color", out val) && bool.Parse(val.StringValue); // False if not specified
                    var texId = spriteDef.Properties.TryGetValue("texture_id", out val) ? ResourceLocation.FromString(val.StringValue) : ResourceLocation.INVALID;
                    var imageType = spriteDef.Properties.TryGetValue("image_type", out val) ? SpriteType.GetImageType(val.StringValue) : SpriteType.SpriteImageType.Simple;

                    var t = new SpriteType(spriteTypeId, texId, imageType, u1);
                    
                    // Read type-specific data
                    if (imageType == SpriteType.SpriteImageType.Filled)
                    {
                        t.FillType = spriteDef.Properties.TryGetValue("fill_type", out val) ?
                            SpriteType.GetFillType(val.StringValue) : SpriteType.SpriteFillType.Left;
                        t.FillStart = spriteDef.Properties.TryGetValue("fill_start", out val) ?
                            float.Parse(val.StringValue) : 0F;
                        t.FillEnd = spriteDef.Properties.TryGetValue("fill_end", out val) ?
                            float.Parse(val.StringValue) : 1F;
                    }

                    Loom.QueueOnMainThread(() =>
                    {
                        textureFlag.Finished = false;

                        Texture2D texture;
                        
                        if (textureFileTable.TryGetValue(texId, out var texturePath))
                        {
                            texture = new Texture2D(2, 2);
                            texture.LoadImage(File.ReadAllBytes(texturePath));
                            texture.filterMode = u2 ? FilterMode.Point : FilterMode.Bilinear;

                            if (invert) // Invert all pixel colors
                            {
                                var pixels = texture.GetPixels32(); // Read pixel data

                                // Iterate through each pixel
                                for (int i = 0; i < pixels.Length; i++)
                                {
                                    // Invert RGB channels (subtract from 1.0f)
                                    pixels[i].r = (byte) (255 - pixels[i].r);
                                    pixels[i].g = (byte) (255 - pixels[i].g);
                                    pixels[i].b = (byte) (255 - pixels[i].b);
                                    // Alpha channel (pixels[i].a) remains unchanged
                                }

                                texture.SetPixels32(pixels); // Apply modified pixel data
                                texture.Apply(); // Upload changes to the GPU
                            }
                        }
                        else
                        {
                            texture = manager.GetMissingTexture();
                            texture.filterMode = FilterMode.Point;
                        }
                        t.CreateSprite(texture);

                        textureFlag.Finished = true;
                    });

                    while (!textureFlag.Finished) { Thread.Sleep(10); }
                        
                    AddEntry(spriteTypeId, numId++, t);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading sprite types: {e.Message}");
                flag.Failed = true;
            }
            finally
            {
                FreezeEntries();
                flag.Finished = true;
            }

            return;
        }

    }
}