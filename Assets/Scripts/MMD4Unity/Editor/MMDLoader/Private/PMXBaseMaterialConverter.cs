using UnityEngine;
using MMD.PMX;

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