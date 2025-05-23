using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    public class IconSpritePanel : MonoBehaviour
    {
        private static readonly int BLINK = Animator.StringToHash("Blink");
        private static readonly int BLINK_RATE = Animator.StringToHash("BlinkRate");
        
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private GameObject iconSpritePrefab;
        
        private readonly Dictionary<ResourceLocation, (ResourceLocation, Image, Image, TMP_Text, Image, Animator)> iconSprites = new();

        public void AddIconSprite(ResourceLocation iconId, ResourceLocation spriteTypeId)
        {
            if (!iconSprites.TryGetValue(iconId, out var iconSprite))
            {
                var imageObj = Instantiate(iconSpritePrefab, panelRect.transform, false);
                var bgImage = imageObj.GetComponent<Image>();
                var spImage = imageObj.transform.GetChild(1).GetComponent<Image>();
                var spFill = imageObj.transform.GetChild(0).GetComponent<Image>();
                var spText = imageObj.transform.GetChild(2).GetComponent<TMP_Text>();
                var animator = imageObj.GetComponent<Animator>();
                imageObj.AddComponent<DestroyAfterAnimation>();
            
                iconSprites.Add(iconId, iconSprite = (spriteTypeId, bgImage, spFill, spText, spImage, animator));

                spFill.fillAmount = 1F;
                animator.SetBool(BLINK, false);
                animator.SetFloat(BLINK_RATE, 1F);
            }

            var spriteType = SpriteTypePalette.INSTANCE.GetById(spriteTypeId);
            SpriteType.SetupSpriteImage(spriteType, iconSprite.Item5);
        }

        public void UpdateIconBlink(ResourceLocation iconId, bool blink, float blinkRate)
        {
            if (iconSprites.TryGetValue(iconId, out var iconSprite))
            {
                iconSprite.Item6.SetBool(BLINK, blink);
                if (blink) iconSprite.Item6.SetFloat(BLINK_RATE, blinkRate);
            }
        }
        
        public void UpdateIconFill(ResourceLocation iconId, float fill)
        {
            if (iconSprites.TryGetValue(iconId, out var iconSprite))
            {
                iconSprite.Item3.fillAmount = fill;
            }
        }

        public void UpdateIconText(ResourceLocation iconId, string text)
        {
            if (iconSprites.TryGetValue(iconId, out var iconSprite))
            {
                iconSprite.Item4.text = text;
            }
        }

        public void RemoveIconSprite(ResourceLocation iconId)
        {
            if (iconSprites.TryGetValue(iconId, out var iconSprite))
            {
                iconSprite.Item6.SetBool(DestroyAfterAnimation.EXPIRED, true);
                
                iconSprites.Remove(iconId);
            }
        }
    }
}
