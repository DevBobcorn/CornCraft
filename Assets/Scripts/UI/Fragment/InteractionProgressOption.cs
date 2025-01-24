using UnityEngine;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class InteractionProgressOption : InteractionOption
    {
        [SerializeField] private RectTransform progressFill;

        public void UpdateProgress(float progress)
        {
            var parentWidth = ((RectTransform) progressFill.parent).rect.width;

            progressFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth * progress);
        }
    }
}