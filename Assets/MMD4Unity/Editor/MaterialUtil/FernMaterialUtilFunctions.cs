using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MMD
{
    public static class FernMaterialUtilFunctions
    {
        public static FernMaterialCategory GuessMaterialCategory(string materialName)
        {
            var materialType = FernMaterialCategory.Unknown;

            if (materialName.Contains("衣") || materialName.Contains("裙") || materialName.Contains("裤") ||
                    materialName.Contains("带") || materialName.Contains("花") || materialName.Contains("饰") ||
                    materialName.Contains("飾"))
            {
                materialType = FernMaterialCategory.Clothes;
            }
            else if (materialName.Contains("脸") || materialName.Contains("顔") || materialName.Contains("颜"))
            {
                materialType = FernMaterialCategory.Face;
            }
            else if (materialName.Contains("白目") || materialName.Contains("睫") ||
                    materialName.Contains("眉") || materialName.Contains("二重") ||
                    materialName.Contains("口") || materialName.Contains("唇") ||
                    materialName.Contains("牙") || materialName.Contains("齿") || materialName.Contains("歯"))
            {
                materialType = FernMaterialCategory.Face;
            }
            else if (materialName.Contains("目") || materialName.Contains("眼") || materialName.Contains("瞳"))
            {
                // Use face material type for now
                materialType = FernMaterialCategory.Face;
            }
            else if (materialName.Contains("发") || materialName.Contains("髪"))
            {
                materialType = FernMaterialCategory.Hair;
            }
            else if (materialName.Contains("体") || materialName.Contains("肌"))
            {
                materialType = FernMaterialCategory.Body;
            }

            return materialType;
        }

        private static readonly HashSet<string> OPAQUE_MATERIALS = new()
        {
            "KK Tongue",
            "KK Teeth (tooth)",
            "KK Face",
            "KK Body",
        };

        private static readonly HashSet<string> CUTOUT_MATERIALS = new()
        {
            "KK Nose",
            "KK Gag00",
            "KK Gag01",
            "KK Gag02",
            "KK Eyewhites (sirome)",
            "KK EyeR (hitomi)",
            "KK EyeL (hitomi)",
        };

        private static readonly HashSet<string> TRANSLUCENT_MATERIALS = new()
        {
            "KK Tears",
            "KK Eyeline up",
            "KK Eyeline Kage",
            "KK Eyeline down",
            "KK Eyebrows (mayuge)",
        };

        public static FernMaterialRenderType GuessRenderType(string materialName)
        {
            if (OPAQUE_MATERIALS.Contains(materialName))
            {
                return FernMaterialRenderType.Opaque;
            }

            if (CUTOUT_MATERIALS.Contains(materialName))
            {
                return FernMaterialRenderType.Cutout;
            }

            if (TRANSLUCENT_MATERIALS.Contains(materialName))
            {
                return FernMaterialRenderType.Translucent;
            }

            if (materialName.Contains("hair_s_") || materialName.Contains("hair_f_") || materialName.Contains("hair_b_"))
            {
                return FernMaterialRenderType.Opaque;
            }

            return FernMaterialRenderType.Unknown;
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