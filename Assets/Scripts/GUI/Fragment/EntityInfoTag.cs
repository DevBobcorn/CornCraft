#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Rendering;

namespace MinecraftClient.UI
{
    public class EntityInfoTag : MonoBehaviour
    {
        private static readonly int EXPIRED  = Animator.StringToHash("Expired");
        
        [SerializeField] private TMP_Text? nameText;
        [SerializeField] private TMP_Text? tagText;

        private InfoTagPanel? parent;
        private int entityId;
        private Animator? anim;
        private EntityRender? entityRender;

        public void SetInfo(InfoTagPanel panelParent, int entityId, EntityRender entityRender)
        {
            parent = panelParent;

            this.entityId = entityId;
            this.entityRender = entityRender;
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

        public void Remove()
        {
            // Play fade away animation...
            anim?.SetBool(EXPIRED, true);
        }

        // Called by animator after hide animation ends...
        void Expire()
        {
            parent?.ExpireTagInfo(entityId);
            Destroy(this.gameObject);
        }

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        void Update()
        {
            
        }
        
    }
}