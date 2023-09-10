using UnityEditor;
using UnityEngine;
using MMD.PMX;

namespace MMD
{
    public class PMXNiloMaterialConverter : PMXBaseMaterialConverter
    {
        public PMXNiloMaterialConverter(GameObject root_game_object, PMXFormat format, float scale)
                : base(root_game_object, format, scale)
        {
            // Something else to do here...

        }

        /// <summary>
        /// MMDシェーダーパスの取得
        /// </summary>
        /// <returns>MMDシェーダーパス</returns>
        string GetShaderPath()
        {
            return "Nilo/ToonShader";
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
            string shader_path = GetShaderPath();
            Material result = new Material(Shader.Find(shader_path));
            // シェーダに依って値が有ったり無かったりするが、設定してもエラーに為らない様なので全部設定
            //result.SetColor("_Color", material.diffuse_color);
            result.SetColor("_BaseColor", material.diffuse_color);
            //result.SetColor("_AmbColor", material.ambient_color);
            //result.SetFloat("_Opacity", material.diffuse_color.a);
            //result.SetColor("_SpecularColor", material.specular_color);
            //result.SetFloat("_Shininess", material.specularity);
            result.SetFloat("_UseAlphaClipping", is_transparent ? 1F : 0F);
            // エッジ
            const float c_default_scale = 0.085f; //0.085fの時にMMDと一致する様にしているので、それ以外なら補正
            result.SetFloat("_OutlineWidth", material.edge_size * scale_ / c_default_scale);
            result.SetColor("_OutlineColor", material.edge_color);
            //カスタムレンダーキュー
            {
                MMDEngine engine = root_game_object_.GetComponent<MMDEngine>();
                if (engine.enable_render_queue && is_transparent) {
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