using UnityEditor;
using UnityEngine;
using MMD.PMX;

namespace MMD
{
    public class PMXFernMaterialConverter : PMXBaseMaterialConverter
    {
        public PMXFernMaterialConverter(GameObject root_game_object, PMXFormat format, float scale)
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
            string result = "FernRender/URP/FERNNPR";
            result += "Standard";

            return result;
        }

        enum MaterialTypes
        {
            Body,
            Face,
            Hair,
            Cloth,
            Metal
        }

        enum DiffuseType
        {
            CelShading = 0,
            RampShading = 1,
            CellBandsShading = 2,
            PBRShading = 3
        }

        void SetupDiffuse(Material fernMat, DiffuseType type)
        {
            fernMat.SetFloat("_enum_diffuse", (float) type);
        }

        enum SpecularType
        {
            None = 0,
            PBR_GGX = 1,
            Stylized = 2,
            Blinn_Phong = 3
        }

        void SetupSpecular(Material fernMat, SpecularType type)
        {
            fernMat.SetFloat("_enum_specular", (float) type);
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
            // TODO: result.SetColor("_BaseColor", material.diffuse_color);
            result.SetColor("_BaseColor", Color.white);

            if (is_transparent) {
                SetSurfaceType(result, 2);
            } else {
                SetSurfaceType(result, 0);
            }

            SetupDiffuse(result, DiffuseType.CelShading);
            result.SetFloat("_CELLThreshold", 0.3F);
            result.SetFloat("_CELLSmoothing", 0.1F);
            result.SetColor("_HighColor", new Color32(255, 255, 255, 255));
            result.SetColor("_DarkColor", new Color32(220, 180, 150, 255));

            SetupSpecular(result, SpecularType.PBR_GGX);

            result.SetFloat("_Smoothness", 0F);
            //result.SetColor("_AmbColor", material.ambient_color);
            //result.SetFloat("_Opacity", material.diffuse_color.a);
            //result.SetColor("_SpecularColor", material.specular_color);
            //result.SetFloat("_Shininess", material.specularity);
            //result.SetFloat("_UseAlphaClipping", is_transparent ? 1F : 0F);
            // エッジ
            const float c_default_scale = 0.085f; //0.085fの時にMMDと一致する様にしているので、それ以外なら補正

            if (material.edge_size > 0F)
            {
                // Enable outline on this material
                result.SetFloat("_Outline", 1F);
                // Set outline width and color
                result.SetFloat("_OutlineWidth", material.edge_size * scale_ * 10F / c_default_scale);
                result.SetColor("_OutlineColor", material.edge_color);
            }
            //カスタムレンダーキュー
            /*{
                MMDEngine engine = root_game_object_.GetComponent<MMDEngine>();
                if (engine.enable_render_queue && is_transparent) {
                    //カスタムレンダーキューが有効 かつ マテリアルが透過なら
                    //マテリアル順に並べる
                    result.renderQueue = engine.render_queue_value + (int)material_index;
                } else {
                    //非透明なら
                    result.renderQueue = -1;
                }
            }*/
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
            // テクスチャが空でなければ登録
            if (null != main_texture) {
                //result.mainTexture = main_texture;
                //result.mainTextureScale = new Vector2(1, -1);
                result.SetTexture("_BaseMap", main_texture);
                result.SetTextureScale("_BaseMap", new Vector2(1, -1));
            }
            
            return result;
        }
    }
}