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

        public override void SetInfo(Entity entity)
        {
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

        public override void Destroy(Action callback)
        {
            callback?.Invoke();
            Destroy(gameObject);
        }
    }
}