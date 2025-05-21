using UnityEngine;
using TMPro;

using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    public class EntityHealthUI : FloatingUI
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private BaseValueBar healthBar;
        [SerializeField] private string textFormat = "Lv.{0:0}";

        private void UpdateMaxHealth(float prevVal, float newVal)
        {
            if (healthBar)
            {
                healthBar.MaxValue = newVal;
            }
        }
        
        private void UpdateHealth(float prevVal, float newVal)
        {
            if (healthBar)
            {
                healthBar.CurValue = newVal;
            }
        }

        private void OnDestroy()
        {
            // Unregister events for previous entity
            if (entityRender)
            {
                entityRender.MaxHealth.OnValueUpdate -= UpdateMaxHealth;
                entityRender.Health.OnValueUpdate -= UpdateHealth;
            }
        }

        public override void SetInfo(EntityRender entityRender)
        {
            // Unregister events for previous entity
            // NOTE: It is not recommended to call SetInfo for more than one entity
            if (this.entityRender)
            {
                this.entityRender.MaxHealth.OnValueUpdate -= UpdateMaxHealth;
                this.entityRender.Health.OnValueUpdate -= UpdateHealth;
            }

            this.entityRender = entityRender;

            // Register events for new entity
            if (this.entityRender)
            {
                this.entityRender.MaxHealth.OnValueUpdate += UpdateMaxHealth;
                this.entityRender.Health.OnValueUpdate += UpdateHealth;

                UpdateHealth(0F, this.entityRender.Health.Value);
                UpdateMaxHealth(0F, this.entityRender.MaxHealth.Value);
            }

            if (levelText)
            {
                // This is used for mimicking the UI format of some anime game
                // This text is no longer updated after first set
                levelText.text = string.Format(textFormat, entityRender.MaxHealth.Value * 2);
            }
        }
    }
}