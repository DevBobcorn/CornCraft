using System;
using UnityEngine;

using CraftSharp.Rendering;

namespace CraftSharp.UI
{
    public abstract class FloatingUI : MonoBehaviour
    {
        protected EntityRender entityRender;

        public abstract void SetInfo(EntityRender entity);

        public virtual void Destroy(Action callback)
        {
            callback?.Invoke();
            Destroy(gameObject);
        }
    }
}