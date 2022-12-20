#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Rendering;

namespace MinecraftClient.UI
{
    public class NPCEntityInfoTag : EntityInfoTag
    {
        [SerializeField] private TMP_Text? nameText;
        [SerializeField] private TMP_Text? tagText;
        
        public override void SetInfo(InfoTagPanel panelParent, int entityId, EntityRender entityRender)
        {
            base.SetInfo(panelParent, entityId, entityRender);
            
            var entity = entityRender.Entity;

            if (nameText is not null) // Update name text
            {
                if (entity.CustomName is not null)
                    nameText.text = entity.CustomName;
                else if (entity.Name is not null)
                    nameText.text = entity.Name;
                else
                    nameText.text = entity.Type.ToString();
            }
            
            if (tagText is not null) // Update tag text
                tagText.text = $"<{entity.Type}>";

        }

        public override void UpdateInfo() { }
        
    }
}