#nullable enable
using UnityEngine;
using TMPro;

using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    public class EntityHealthUI : FloatingUI
    {
        [SerializeField] private TMP_Text? levelText;
        [SerializeField] private FloatingValueBar? healthBar;
        [SerializeField] private string textFormat = "Lv.{0:0}";
        private float lastHealth, lastMaxHealth;

        public override void SetInfo(EntityRender entityRender)
        {
            this.entityRender = entityRender;

            if (levelText != null)
            {
                levelText.text = string.Format(textFormat, entityRender.MaxHealth * 2);

            }

            if (healthBar != null)
            {
                healthBar.MaxValue = lastMaxHealth = entityRender.MaxHealth;
                healthBar.CurValue = lastHealth = entityRender.Health;
            }
        }

        public void Update()
        {
            if (healthBar != null && entityRender != null)
            {
                if (entityRender.MaxHealth != lastMaxHealth || entityRender.Health != lastHealth)
                {
                    healthBar.MaxValue = lastMaxHealth = entityRender.MaxHealth;
                    healthBar.CurValue = lastHealth = entityRender.Health;
                }
            }
        }
    }
}