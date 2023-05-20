#nullable enable
using System;
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;

namespace MinecraftClient.UI
{
    public class EntityHealthUI : FloatingUI
    {
        [SerializeField] private TMP_Text? levelText;
        [SerializeField] private FloatingValueBar? healthBar;
        [SerializeField] private string textFormat = "Lv.{0:0}";
        private float lastHealth, lastMaxHealth;

        public override void SetInfo(Entity entity)
        {
            this.entity = entity;

            if (levelText != null)
            {
                levelText.text = string.Format(textFormat, entity.MaxHealth * 2);

            }

            if (healthBar != null)
            {
                healthBar.MaxValue = lastMaxHealth = entity.MaxHealth;
                healthBar.CurValue = lastHealth = entity.Health;
            }
            
        }

        public void Update()
        {
            if (healthBar != null && entity != null)
            {
                if (entity.MaxHealth != lastMaxHealth || entity.Health != lastHealth)
                {
                    healthBar.MaxValue = lastMaxHealth = entity.MaxHealth;
                    healthBar.CurValue = lastHealth = entity.Health;
                }
            }
        }
    }
}