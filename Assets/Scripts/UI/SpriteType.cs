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
        public static readonly SpriteType DUMMY_SPRITE_TYPE = new(ResourceLocation.INVALID, ResourceLocation.INVALID, SpriteImageType.Simple, false);

        public readonly ResourceLocation TypeId;
        
        public readonly ResourceLocation TextureId;
        public readonly bool UseItemModel;
        
        public enum SpriteImageType
        {
            Simple,
            Filled,
            Flipbook
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
                "flipbook" => SpriteImageType.Flipbook,
                
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
        
        public SpriteImageType ImageType { get; set; }
        
        // Fill settings, only used if ImageType is Filled
        public SpriteFillType FillType { get; set; }
        public float FillStart { get; set; } = 0;
        public float FillEnd { get; set; } = 1;

        public static void SetupSpriteImage(SpriteType spriteType, Image spriteImage)
        {
            switch (spriteType.ImageType)
            {
                case SpriteImageType.Simple:
                case SpriteImageType.Flipbook:
                    spriteImage.type = Image.Type.Simple;
                    spriteImage.sprite = spriteType.Sprite;
                    break;
                case SpriteImageType.Filled:
                    spriteImage.type = Image.Type.Filled;
                    spriteImage.sprite = spriteType.Sprite;
                    
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
        
        public SpriteType(ResourceLocation id, ResourceLocation textureId, SpriteImageType imageType, bool useItemModel)
        {
            TypeId = id;
            
            ImageType = imageType;
            TextureId = textureId;
            UseItemModel = useItemModel;
        }

        public void CreateSprite(Texture2D texture)
        {
            int w = texture.width, h = texture.height;
            Sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new(w / 2F, h / 2F));
        }

        public override string ToString()
        {
            return TypeId.ToString();
        }
    }
}
