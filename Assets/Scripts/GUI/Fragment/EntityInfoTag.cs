#nullable enable
using UnityEngine;

using MinecraftClient.Mapping;
using MinecraftClient.Rendering;

namespace MinecraftClient.UI
{
    [RequireComponent(typeof (Animator))]
    public abstract class EntityInfoTag : MonoBehaviour
    {
        private static readonly int EXPIRED  = Animator.StringToHash("Expired");
        
        protected InfoTagPanel? parent;
        protected Animator? anim;

        protected int entityId;
        protected EntityRender? entityRender;

        public virtual void SetInfo(InfoTagPanel panelParent, int entityId, EntityRender entityRender)
        {
            parent = panelParent;

            this.entityId = entityId;
            this.entityRender = entityRender;


        }

        public virtual void Remove()
        {
            // Play fade away animation...
            anim?.SetBool(EXPIRED, true);
        }

        // Called by animator after hide animation ends...
        protected virtual void Expire()
        {
            parent?.ExpireTagInfo(entityId);
            Destroy(this.gameObject);
        }

        protected virtual void Awake()
        {
            anim = GetComponent<Animator>();

        }

        public abstract void UpdateInfo();
    }
}