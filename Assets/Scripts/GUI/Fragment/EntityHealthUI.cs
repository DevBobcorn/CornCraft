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

        private void UpdateHealth()
        {
            if (healthBar != null && entityRender != null)
            {
                healthBar.MaxValue = entityRender.MaxHealth.Value;
                healthBar.CurValue = entityRender.Health.Value;
            }
        }

        public void Destroy()
        {
            // Unregister events for previous entity
            if (entityRender != null)
            {
                entityRender.MaxHealth.OnValueUpdate -= (_, _) => UpdateHealth();
                entityRender.Health.OnValueUpdate -= (_, _) => UpdateHealth();
            }
        }

        public override void SetInfo(EntityRender entityRender)
        {
            // Unregister events for previous entity
            // NOTE: It is not recommended to call SetInfo for more than one entity
            if (this.entityRender != null)
            {
                this.entityRender.MaxHealth.OnValueUpdate -= (_, _) => UpdateHealth();
                this.entityRender.Health.OnValueUpdate -= (_, _) => UpdateHealth();
            }

            this.entityRender = entityRender;

            // Register events for new entity
            if (this.entityRender != null)
            {
                this.entityRender.MaxHealth.OnValueUpdate += (_, _) => UpdateHealth();
                this.entityRender.Health.OnValueUpdate += (_, _) => UpdateHealth();
            }

            if (levelText != null)
            {
                // This is used for mimicking the UI format of some anime game
                // This text is no longer updated after first set
                levelText.text = string.Format(textFormat, entityRender.MaxHealth.Value * 2);
            }

            UpdateHealth();
        }
    }
}