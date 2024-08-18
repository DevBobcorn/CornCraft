using UnityEngine;

namespace AnimeSkybox
{
    [System.Serializable]
    public class MaterialProperty
    {
        public enum PropertyType
        {
            Float,
            Color
        }

        public PropertyType type;
        public string propertyName;

        public AnimationCurve curve;
        public Gradient gradient;
    }
}