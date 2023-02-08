#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Rendering;

namespace MinecraftClient.UI
{
    public class MonsterEntityInfoTag : EntityInfoTag
    {
        [SerializeField] private TMP_Text? levelText;
        [SerializeField] private ValueBar? healthBar;

        public override void SetInfo(InfoTagPanel panelParent, int entityId, EntityRender entityRender)
        {
            base.SetInfo(panelParent, entityId, entityRender);

            var entity = entityRender.Entity;

            if (levelText is not null)
                levelText.text = $"Lv.{entity.MaxHealth}";
            
            if (healthBar is not null)
            {
                healthBar.MaxValue = 1F;
                healthBar.CurValue = 1F;
            }

        }

        public override void UpdateInfo()
        {
            var entity = entityRender?.Entity;

            if (entity is not null && healthBar is not null)
            {
                healthBar.CurValue = entity.Health / entity.MaxHealth;
            }
        }
    }
}