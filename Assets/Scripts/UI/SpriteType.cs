using UnityEngine;

namespace CraftSharp.UI
{
    /// <summary>
    /// Represents a CornCraft Sprite Type
    /// </summary>
    public record SpriteType
    {
        public static readonly SpriteType DUMMY_SPRITE_TYPE = new(ResourceLocation.INVALID, ResourceLocation.INVALID, false);

        public readonly ResourceLocation TypeId;
        
        public readonly ResourceLocation TextureId;
        public readonly bool UseItemModel;

        public Sprite Sprite { get; set; }
        
        public SpriteType(ResourceLocation id, ResourceLocation textureId, bool useItemModel)
        {
            TypeId = id;
            
            TextureId = textureId;
            UseItemModel = useItemModel;
        }

        public Sprite CreateSprite(Texture2D texture)
        {
            int w = texture.width, h = texture.height;

            Sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new(w / 2F, h / 2F));

            return Sprite;
        }

        public override string ToString()
        {
            return TypeId.ToString();
        }
    }
}
