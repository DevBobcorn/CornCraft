using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MMD
{
    public static class AnimeMaterialUtilFunctions
    {
        public static AnimeMaterialCategory GuessMMDMaterialCategory(string materialName)
        {
            var materialType = AnimeMaterialCategory.Unknown;

            if (materialName.Contains("衣") || materialName.Contains("裙") || materialName.Contains("裤") ||
                    materialName.Contains("带") || materialName.Contains("花") || materialName.Contains("饰") ||
                    materialName.Contains("飾") || materialName.Contains("袖") || materialName.Contains("靴") ||
                    materialName.Contains("鞋") || materialName.Contains("袜") || materialName.Contains("套"))
            {
                materialType = AnimeMaterialCategory.Clothes;
            }
            else if (materialName.Contains("脸") || materialName.Contains("顔") || materialName.Contains("颜"))
            {
                materialType = AnimeMaterialCategory.Face;
            }
            else if (materialName.Contains("白目") || materialName.Contains("眼白") ||
                    materialName.Contains("睫") || materialName.Contains("眉") || materialName.Contains("二重") ||
                    materialName.Contains("口") || materialName.Contains("唇") || materialName.Contains("舌") ||
                    materialName.Contains("牙") || materialName.Contains("齿") || materialName.Contains("歯"))
            {
                //materialType = FernMaterialCategory.Face;
                materialType = AnimeMaterialCategory.Body;
            }
            else if (materialName.Contains("目") || materialName.Contains("眼") || materialName.Contains("瞳"))
            {
                // Use face material type for now
                materialType = AnimeMaterialCategory.Eye;
            }
            else if (materialName.Contains("发") || materialName.Contains("髪"))
            {
                materialType = AnimeMaterialCategory.Hair;
            }
            else if (materialName.Contains("体") || materialName.Contains("肌") || materialName.Contains("肤"))
            {
                materialType = AnimeMaterialCategory.Body;
            }

            return materialType;
        }

        /// <summary>
        /// シェーダーパスの取得
        /// </summary>
        /// <returns>シェーダーパス</returns>
        public static string GetShaderPath(AnimeMaterialCategory type)
        {
            string result = "Honkai Star Rail/Character/";

            result += type switch
            {
                AnimeMaterialCategory.Body => "Body",
                AnimeMaterialCategory.BodyTransparent => "Body (Transparent)",
                AnimeMaterialCategory.Face => "Face",
                AnimeMaterialCategory.Hair => "Hair",
                //AnimeMaterialCategory.Eye  => "Eye",
                AnimeMaterialCategory.Eye  => "Face", // Eye base doesn't use a separate shader

                _                          => "Body",
            };
            return result;
        }

		public static void SetRenderType(Material mat, AnimeMaterialRenderType renderType)
		{
            if (renderType != AnimeMaterialRenderType.Unknown)
            {
                mat.SetFloat("_Surface", (int) renderType);
            }
            else
            {
                return; // Leave other things unchanged
            }

            if (renderType == AnimeMaterialRenderType.Opaque) // Opaque
            {
                mat.renderQueue = (int) RenderQueue.Geometry;
                //mat.SetOverrideTag("RenderType", "Opaque");
                mat.SetInt("_SrcBlend", (int) BlendMode.One);
                mat.SetInt("_DstBlend", (int) BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1);
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
            else if (renderType == AnimeMaterialRenderType.Translucent) // Transparent
            {
                mat.renderQueue = (int) RenderQueue.Transparent;
                //mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
            else if (renderType == AnimeMaterialRenderType.Cutout) // Clip
            {
                mat.renderQueue = (int) RenderQueue.AlphaTest;
                //mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.SetInt("_SrcBlend", (int) BlendMode.One);
                mat.SetInt("_DstBlend", (int) BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
            }

            if (mat.HasProperty("_QueueOffset"))
            {
                mat.renderQueue += (int)mat.GetFloat("_QueueOffset");
            }
            /*
            if (mat.HasProperty("_DepthPrePass"))
            {
                mat.SetShaderPassEnabled("SRPDefaultUnlit", mat.GetFloat("_DepthPrePass")>0);
                mat.SetShaderPassEnabled("ShadowCaster", mat.GetFloat("_CasterShadow")>0);
            }
            */
		}
    }
}