using System;
using System.IO;
using System.Text;
using System.Threading;
using CraftSharp.Resource;
using UnityEngine;

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
                    var u = spriteDef.Properties.TryGetValue("use_item_model", out var val) && bool.Parse(val.StringValue);
                    var texId = spriteDef.Properties.TryGetValue("texture_id", out val) ? ResourceLocation.FromString(val.StringValue) : ResourceLocation.INVALID;

                    var t = new SpriteType(spriteTypeId, texId, u);

                    Loom.QueueOnMainThread(() =>
                    {
                        textureFlag.Finished = false;

                        Texture2D texture;
                        
                        if (textureFileTable.TryGetValue(texId, out var texturePath))
                        {
                            texture = new Texture2D(2, 2);
                            texture.LoadImage(File.ReadAllBytes(texturePath));
                            texture.filterMode = FilterMode.Point; // TODO: Allow customization
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