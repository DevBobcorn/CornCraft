using UnityEditor;
using UnityEngine;
using MMD.PMX;

namespace MMD
{
    public class PMXMMDMaterialConverter : PMXBaseMaterialConverter
    {
        public PMXMMDMaterialConverter(GameObject root_game_object, PMXFormat format, float scale)
                : base(root_game_object, format, scale)
        {
            // Something else to do here...

        }

        /// <summary>
		/// MMDシェーダーパスの取得
		/// </summary>
		/// <returns>MMDシェーダーパス</returns>
		/// <param name='material'>シェーダーを設定するマテリアル</param>
		/// <param name='texture'>シェーダーに設定するメインテクスチャ</param>
		/// <param name='is_transparent'>透過か</param>
		string GetMmdShaderPath(PMXFormat.Material material, Texture2D texture, bool is_transparent) {
			string result = "MMD/";
			if (IsTransparentMaterial(is_transparent)) {
				result += "Transparent/";
			}
			result += "PMDMaterial";
			if (IsEdgeMaterial(material)) {
				result += "-with-Outline";
			}
			if (IsCullBackMaterial(material)) {
				result += "-CullBack";
			}
			if (IsNoCastShadowMaterial(material)) {
				result += "-NoCastShadow";
			}
#if MFU_ENABLE_NO_RECEIVE_SHADOW_SHADER	//影受け無しのシェーダはまだ無いので無効化
			if (IsNoReceiveShadowMaterial(material)) {
				result += "-NoReceiveShadow";
			}
#endif //MFU_ENABLE_NO_RECEIVE_SHADOW_SHADER
			return result;
		}
        
        /// <summary>
        /// マテリアルをUnity用に変換する
        /// </summary>
        /// <returns>Unity用マテリアル</returns>
        /// <param name='material_index'>PMX用マテリアルインデックス</param>
        /// <param name='is_transparent'>透過か</param>
        public override Material Convert(uint material_index, bool is_transparent)
        {
            PMXFormat.Material material = format_.material_list.material[material_index];

            //先にテクスチャ情報を検索
            Texture2D main_texture = null;
            if (material.usually_texture_index < format_.texture_list.texture_file.Length) {
                string texture_file_name = format_.texture_list.texture_file[material.usually_texture_index];
                string path = format_.meta_header.folder + "/" + texture_file_name;
                main_texture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            }
            
            //マテリアルに設定
            string shader_path = GetMmdShaderPath(material, main_texture, is_transparent);
            Material result = new Material(Shader.Find(shader_path));
        
            // シェーダに依って値が有ったり無かったりするが、設定してもエラーに為らない様なので全部設定
            result.SetColor("_Color", material.diffuse_color);
            result.SetColor("_AmbColor", material.ambient_color);
            result.SetFloat("_Opacity", material.diffuse_color.a);
            result.SetColor("_SpecularColor", material.specular_color);
            result.SetFloat("_Shininess", material.specularity);
            // エッジ
            const float c_default_scale = 0.085f; //0.085fの時にMMDと一致する様にしているので、それ以外なら補正
            result.SetFloat("_OutlineWidth", material.edge_size * scale_ / c_default_scale);
            result.SetColor("_OutlineColor", material.edge_color);
            //カスタムレンダーキュー
            {
                MMDEngine engine = root_game_object_.GetComponent<MMDEngine>();
                if (engine.enable_render_queue && IsTransparentMaterial(is_transparent)) {
                    //カスタムレンダーキューが有効 かつ マテリアルが透過なら
                    //マテリアル順に並べる
                    result.renderQueue = engine.render_queue_value + (int)material_index;
                } else {
                    //非透明なら
                    result.renderQueue = -1;
                }
            }
            
            // スフィアテクスチャ
            if (material.sphere_texture_index < format_.texture_list.texture_file.Length) {
                string sphere_texture_file_name = format_.texture_list.texture_file[material.sphere_texture_index];
                string path = format_.meta_header.folder + "/" + sphere_texture_file_name;
                Texture2D sphere_texture = (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                
                switch (material.sphere_mode) {
                case PMXFormat.Material.SphereMode.AddSphere: // 加算
                    result.SetTexture("_SphereAddTex", sphere_texture);
                    result.SetTextureScale("_SphereAddTex", new Vector2(1, -1));
                    break;
                case PMXFormat.Material.SphereMode.MulSphere: // 乗算
                    result.SetTexture("_SphereMulTex", sphere_texture);
                    result.SetTextureScale("_SphereMulTex", new Vector2(1, -1));
                    break;
                case PMXFormat.Material.SphereMode.SubTexture: // サブテクスチャ
                    //サブテクスチャ用シェーダーが無いので設定しない
                    break;
                default:
                    //empty.
                    break;
                }
            }
            
            // トゥーンテクスチャ
            {
                string toon_texture_name = null;
                string toon_texture_path = null;
                if (0 < material.common_toon) {
                    //共通トゥーン
                    string resource_path = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(Shader.Find("MMD/HalfLambertOutline")));
                    toon_texture_name = "toon" + material.common_toon.ToString("00") + ".bmp";
                    toon_texture_path = System.IO.Path.Combine(resource_path, toon_texture_name);
                } else if (material.toon_texture_index < format_.texture_list.texture_file.Length) {
                    //自前トゥーン
                    toon_texture_name = format_.texture_list.texture_file[material.toon_texture_index];
                    toon_texture_path = System.IO.Path.Combine(format_.meta_header.folder, toon_texture_name);
                }
                if (!string.IsNullOrEmpty(toon_texture_path)) {
                    Texture2D toon_texture = (Texture2D)AssetDatabase.LoadAssetAtPath(toon_texture_path, typeof(Texture2D));
                    result.SetTexture("_ToonTex", toon_texture);
                    result.SetTextureScale("_ToonTex", new Vector2(1, -1));
                }
            }

            // テクスチャが空でなければ登録
            if (null != main_texture) {
                result.mainTexture = main_texture;
                result.mainTextureScale = new Vector2(1, -1);
            }
            
            return result;
        }
    }
}