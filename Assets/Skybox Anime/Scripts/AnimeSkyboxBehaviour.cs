using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimeSkybox
{
    public class AnimeSkyboxBehaviour : PlayableBehaviour
    {
        public List<MaterialProperty> properties;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Material material = playerData as Material;
            if (material == null) return;
            
            RenderSettings.skybox = material; // 设置天空盒的材质

            float t = (float) (playable.GetTime() / playable.GetDuration()); // 缩放到0-1的范围
            
            if (properties != null)
            {
                foreach (var property in properties) // 遍历所有属性
                {
                    if (property.type == MaterialProperty.PropertyType.Float) // 如果是float类型
                    {
                        float value = property.curve.Evaluate(t);
                        material.SetFloat(property.propertyName, value);
                    }
                    else if (property.type == MaterialProperty.PropertyType.Color) // 如果是color类型
                    {
                        Color color = property.gradient.Evaluate(t);
                        material.SetColor(property.propertyName, color);
                    }
                }
            }
        }
    }
}