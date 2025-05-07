using System;
using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    /// <summary>
    /// Represents a CornCraft Sprite Type
    /// </summary>
    public record SpriteType
    {
        public static readonly SpriteType DUMMY_SPRITE_TYPE = new(ResourceLocation.INVALID, ResourceLocation.INVALID, Array.Empty<ResourceLocation>(), 1F, SpriteImageType.Simple, false);

        public readonly ResourceLocation TypeId;
        
        public readonly ResourceLocation TextureId;
        public readonly ResourceLocation[] FlipbookTextureIds;
        public readonly bool UseItemModel;

        public class FlipbookTimer
        {
            public float Time = 0F;
            public int Frame = 0;
        }
        
        public enum SpriteImageType
        {
            Simple,
            Filled,
            Sliced
        }
        
        public enum SpriteFillType
        {
            Left,
            Right,
            Top,
            Bottom,
            Radial
        }

        public static SpriteImageType GetImageType(string imageType)
        {
            return imageType switch
            {
                "simple" => SpriteImageType.Simple,
                "filled" => SpriteImageType.Filled,
                "sliced" => SpriteImageType.Sliced,
                
                _ => SpriteImageType.Simple
            };
        }
        
        public static SpriteFillType GetFillType(string imageFillType)
        {
            return imageFillType switch
            {
                "left" => SpriteFillType.Left,
                "right" => SpriteFillType.Right,
                "top" => SpriteFillType.Top,
                "bottom" => SpriteFillType.Bottom,
                "radial" => SpriteFillType.Radial,
                
                _ => SpriteFillType.Left
            };
        }

        public Sprite Sprite { get; private set; }

        public Sprite[] FlipbookSprites { get; private set; }
        public float FlipbookInterval { get; private set; }
        
        public SpriteImageType ImageType { get; set; }
        
        // Fill settings, only used if ImageType is Filled
        public SpriteFillType FillType { get; set; }
        public float FillStart { get; set; } = 0;
        public float FillEnd { get; set; } = 1;

        public static void SetupSpriteImage(SpriteType spriteType, Image spriteImage)
        {
            spriteImage.sprite = spriteType.Sprite;
            
            switch (spriteType.ImageType)
            {
                case SpriteImageType.Simple:
                    spriteImage.type = Image.Type.Simple;
                    break;
                case SpriteImageType.Sliced:
                    spriteImage.type = Image.Type.Sliced;
                    break;
                case SpriteImageType.Filled:
                    spriteImage.type = Image.Type.Filled;
                    
                    spriteImage.fillMethod = spriteType.FillType switch
                    {
                        SpriteFillType.Left => Image.FillMethod.Horizontal,
                        SpriteFillType.Right => Image.FillMethod.Horizontal,
                        SpriteFillType.Top => Image.FillMethod.Vertical,
                        SpriteFillType.Bottom => Image.FillMethod.Vertical,
                        SpriteFillType.Radial => Image.FillMethod.Radial360,
                        _ => throw new ArgumentOutOfRangeException($"Undefined sprite fill type: {spriteType.FillType}")
                    };
                    if (spriteImage.fillMethod == Image.FillMethod.Horizontal)
                    {
                        spriteImage.fillOrigin = spriteType.FillType == SpriteFillType.Left
                            ? (int) Image.OriginHorizontal.Left : (int) Image.OriginHorizontal.Right;
                    }
                    else if (spriteImage.fillMethod == Image.FillMethod.Vertical)
                    {
                        spriteImage.fillOrigin = spriteType.FillType == SpriteFillType.Bottom
                            ? (int) Image.OriginVertical.Bottom : (int) Image.OriginVertical.Top;
                    }

                    spriteImage.fillAmount = 0F;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Undefined sprite image type: {spriteType.ImageType}");
            }
        }
        
        public static void UpdateFilledSpriteImage(SpriteType spriteType, Image spriteImage, int curValue, int maxValue)
        {
            if (spriteImage.type != Image.Type.Filled ||
                spriteType.ImageType != SpriteImageType.Filled) return;
            
            spriteImage.fillAmount = Mathf.Lerp(spriteType.FillStart, spriteType.FillEnd, curValue / (float) maxValue);
        }
        
        public SpriteType(ResourceLocation id, ResourceLocation textureId, ResourceLocation[] flipbookTextureIds,
            float flipbookInterval, SpriteImageType imageType, bool useItemModel)
        {
            TypeId = id;
            
            ImageType = imageType;
            TextureId = textureId;
            FlipbookTextureIds = flipbookTextureIds;
            FlipbookInterval = flipbookInterval;
            UseItemModel = useItemModel;
        }

        private Sprite CreateSprite(Texture2D texture)
        {
            int w = texture.width, h = texture.height;
            return Sprite.Create(texture, new Rect(0, 0, w, h), new(w / 2F, h / 2F));
        }

        public void CreateSprites(Texture2D texture, Texture2D[] flipbookTextures)
        {
            Sprite = CreateSprite(texture);
            FlipbookSprites = new Sprite[flipbookTextures.Length];

            for (int i = 0; i < flipbookTextures.Length; i++)
            {
                FlipbookSprites[i] = CreateSprite(flipbookTextures[i]);
            }
        }

        public override string ToString()
        {
            return TypeId.ToString();
        }
    }
}
