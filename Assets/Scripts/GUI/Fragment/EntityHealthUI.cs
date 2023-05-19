#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;
using MinecraftClient.Protocol;

namespace MinecraftClient.UI
{
    public class EntityHealthUI : FloatingUI
    {
        [SerializeField] private TMP_Text? levelText;
        [SerializeField] private FloatingValueBar? healthBar;
        [SerializeField] private string textFormat = "Lv.{0:0}";

        public override void SetInfo(FloatingUIManager manager, Entity entity)
        {
            this.manager = manager;
            this.entity = entity;

            if (levelText != null)
            {
                levelText.text = string.Format(textFormat, entity.MaxHealth);

            }
            
        }

        public void Update()
        {
            if (healthBar != null && entity != null)
            {
                healthBar.MaxValue = entity.MaxHealth;
                healthBar.CurValue = entity.Health;
            }
        }
    }
}