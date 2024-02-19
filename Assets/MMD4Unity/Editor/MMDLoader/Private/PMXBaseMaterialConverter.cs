using UnityEngine;
using MMD.PMX;
using UnityEngine.Rendering;

namespace MMD
{
    public abstract class PMXBaseMaterialConverter
    {
        protected GameObject            root_game_object_;
        protected PMXFormat             format_;
        protected float                 scale_;

        public PMXBaseMaterialConverter(GameObject root_game_object, PMXFormat format, float scale)
        {
            root_game_object_ = root_game_object;
            format_ = format;
            scale_ = scale;
        }

        // See LWGUI.Helper.SetSurfaceType()
		public static void SetSurfaceType(Material mat, int surfaceType)
		{
            mat.SetFloat("_Surface", surfaceType);

            if (surfaceType == 0) // Opaque
            {
                mat.renderQueue = (int) RenderQueue.Geometry;
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.SetInt("_SrcBlend", (int) BlendMode.One);
                mat.SetInt("_DstBlend", (int) BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1);
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
            else if (surfaceType == 1) // Transparent
            {
                mat.renderQueue = (int) RenderQueue.Transparent;
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHATEST_ON");
            }
            else if (surfaceType == 2) // Clip
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

        /// <summary>
        /// 透過マテリアル確認
        /// </summary>
        /// <returns>true:透過, false:不透明</returns>
        /// <param name='is_transparent'>透過か</param>
        protected bool IsTransparentMaterial(bool is_transparent) {
            return is_transparent;
        }

        /// <summary>
        /// エッジマテリアル確認
        /// </summary>
        /// <returns>true:エッジ有り, false:無エッジ</returns>
        /// <param name='material'>シェーダーを設定するマテリアル</param>
        protected bool IsEdgeMaterial(PMXFormat.Material material) {
            bool result;
            if (0 != (PMXFormat.Material.Flag.Edge & material.flag)) {
                //エッジ有りなら
                result = true;
            } else {
                //エッジ無し
                result = false;
            }
            return result;
        }
        
        /// <summary>
        /// 背面カリングマテリアル確認
        /// </summary>
        /// <returns>true:背面カリングする, false:背面カリングしない</returns>
        /// <param name='material'>シェーダーを設定するマテリアル</param>
        protected bool IsCullBackMaterial(PMXFormat.Material material) {
            bool result;
            if (0 != (PMXFormat.Material.Flag.Reversible & material.flag)) {
                //両面描画なら
                //背面カリングしない
                result = false;
            } else {
                //両面描画で無いなら
                //背面カリングする
                result = true;
            }
            return result;
        }
        
        /// <summary>
        /// 無影マテリアル確認
        /// </summary>
        /// <returns>true:無影, false:影放ち</returns>
        /// <param name='material'>シェーダーを設定するマテリアル</param>
        protected bool IsNoCastShadowMaterial(PMXFormat.Material material) {
            bool result;
            if (0 != (PMXFormat.Material.Flag.CastShadow & material.flag)) {
                //影放ち
                result = false;
            } else {
                //無影
                result = true;
            }
            return result;
        }
        
        /// <summary>
        /// 影受け無しマテリアル確認
        /// </summary>
        /// <returns>true:影受け無し, false:影受け</returns>
        /// <param name='material'>シェーダーを設定するマテリアル</param>
        protected bool IsNoReceiveShadowMaterial(PMXFormat.Material material) {
            bool result;
            if (0 != (PMXFormat.Material.Flag.ReceiveSelfShadow & material.flag)) {
                //影受け
                result = false;
            } else {
                //影受け無し
                result = true;
            }
            return result;
        }

        public abstract Material Convert(uint material_index, bool is_transparent);
    }
}