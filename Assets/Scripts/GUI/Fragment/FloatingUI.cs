#nullable enable
using UnityEngine;
using TMPro;

using MinecraftClient.Mapping;
using MinecraftClient.Protocol;

namespace MinecraftClient.UI
{
    public abstract class FloatingUI : MonoBehaviour
    {
        protected FloatingUIManager? manager;
        protected Entity? entity;

        public abstract void SetInfo(FloatingUIManager manager, Entity entity);

        public virtual void Destroy()
        {
            // TODO Fade out

            Destroy(gameObject);
        }
    }
}