using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MMD
{
    public static class FernMaterialUtilFunctions
    {
        public static FernMaterialCategory GuessMMDMaterialCategory(string materialName)
        {
            var materialType = FernMaterialCategory.Unknown;

            if (materialName.Contains("衣") || materialName.Contains("裙") || materialName.Contains("裤") ||
                    materialName.Contains("带") || materialName.Contains("花") || materialName.Contains("饰") ||
                    materialName.Contains("飾") || materialName.Contains("袖") || materialName.Contains("靴") ||
                    materialName.Contains("鞋") || materialName.Contains("袜") || materialName.Contains("套"))
            {
                materialType = FernMaterialCategory.Clothes;
            }
            else if (materialName.Contains("脸") || materialName.Contains("顔") || materialName.Contains("颜"))
            {
                materialType = FernMaterialCategory.Face;
            }
            else if (materialName.Contains("白目") || materialName.Contains("眼白") ||
                    materialName.Contains("睫") || materialName.Contains("眉") || materialName.Contains("二重") ||
                    materialName.Contains("口") || materialName.Contains("唇") || materialName.Contains("舌") ||
                    materialName.Contains("牙") || materialName.Contains("齿") || materialName.Contains("歯"))
            {
                //materialType = FernMaterialCategory.Face;
                materialType = FernMaterialCategory.Body;
            }
            else if (materialName.Contains("目") || materialName.Contains("眼") || materialName.Contains("瞳"))
            {
                // Use face material type for now
                materialType = FernMaterialCategory.Eye;
            }
            else if (materialName.Contains("发") || materialName.Contains("髪"))
            {
                materialType = FernMaterialCategory.Hair;
            }
            else if (materialName.Contains("体") || materialName.Contains("肌") || materialName.Contains("肤"))
            {
                materialType = FernMaterialCategory.Body;
            }

            return materialType;
        }

        /// <summary>
        /// Fernシェーダーパスの取得
        /// </summary>
        /// <returns>Fernシェーダーパス</returns>
        public static string GetShaderPath(FernMaterialCategory type)
        {
            string result = "FernRender/URP/FERNNPR";

            result += type switch
            {
                FernMaterialCategory.Face => "Face",
                FernMaterialCategory.Hair => "Hair",
                FernMaterialCategory.Eye  => "Eye",
                _                         => "Standard",
            };
            return result;
        }

        // See LWGUI.Helper.SetSurfaceType()
		public static void SetRenderType(Material mat, FernMaterialRenderType renderType)
		{
            if (renderType != FernMaterialRenderType.Unknown)
            {
                mat.SetFloat("_Surface", (int) renderType);
            }
            else
            {
                return; // Leave other things unchanged
            }

            if (renderType == FernMaterialRenderType.Opaque) // Opaque
            {
                mat.renderQueue = (int) RenderQueue.Geometry;
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.SetInt("_SrcBlend", (int) BlendMode.One);
                mat.SetInt("_DstBlend", (int) BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1);
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
            else if (renderType == FernMaterialRenderType.Translucent) // Transparent
            {
                mat.renderQueue = (int) RenderQueue.Transparent;
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
            else if (renderType == FernMaterialRenderType.Cutout) // Clip
            {
                mat.renderQueue = (int) RenderQueue.AlphaTest;
                mat.SetOverrideTag("RenderType", "TransparentCutout");
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
            
            if (mat.HasProperty("_DepthPrePass"))
            {
                mat.SetShaderPassEnabled("SRPDefaultUnlit", mat.GetFloat("_DepthPrePass")>0);
                mat.SetShaderPassEnabled("ShadowCaster", mat.GetFloat("_CasterShadow")>0);
            }
		}
    
        public static FernMaterialRenderType GetRenderType(Material mat)
        {
            return mat.GetTag("RenderType", true, "Unknown") switch
            {
                "Opaque"            => FernMaterialRenderType.Opaque,
                "TransparentCutout" => FernMaterialRenderType.Cutout,
                "Transparent"       => FernMaterialRenderType.Translucent,

                _                   => FernMaterialRenderType.Unknown
            };
        }
    }
}