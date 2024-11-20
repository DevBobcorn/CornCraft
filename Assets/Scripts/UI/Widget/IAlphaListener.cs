using UnityEngine;

namespace CraftSharp.UI
{
    public interface IAlphaListener
    {
        public void UpdateAlpha(float alpha);

        protected static Color GetColorWithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}