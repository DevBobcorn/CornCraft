using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class IconSpritePanel : MonoBehaviour
    {
        private readonly Dictionary<ResourceLocation, (ResourceLocation, Image)> iconSprites = new();
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private GameObject iconSpritePrefab;

        public void AddIconSprite(ResourceLocation iconId, ResourceLocation spriteTypeId)
        {
            if (!iconSprites.TryGetValue(iconId, out var iconSprite))
            {
                var imageObj = Instantiate(iconSpritePrefab, panelRect.transform, false);
                var image = imageObj.GetComponent<Image>();

                iconSprite = (spriteTypeId, image);
            
                iconSprites.Add(iconId, iconSprite);
            }

            var spriteType = SpriteTypePalette.INSTANCE.GetById(spriteTypeId);
            SpriteType.SetupSpriteImage(spriteType, iconSprite.Item2);
        }

        public void RemoveIconSprite(ResourceLocation iconId)
        {
            if (iconSprites.TryGetValue(iconId, out var iconSprite))
            {
                Destroy(iconSprite.Item2.gameObject);
                
                iconSprites.Remove(iconId);
            }
        }
    }
}
