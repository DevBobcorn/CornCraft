using System.Linq;
using UnityEditor;
using UnityEngine;
using MMD.PMX;

namespace MMD
{
    public class PMXFernKKSMaterialConverter : PMXBaseMaterialConverter
    {
        public PMXFernKKSMaterialConverter(GameObject root_game_object, PMXFormat format, float scale)
                : base(root_game_object, format, scale)
        {
            // Something else to do here...

        }

        public Color32 HAIR_DIFFUSE_HIGH = new Color32(255, 255, 255, 255);
        public Color32 HAIR_DIFFUSE_DARK = new Color32(255, 200, 180, 255);

        public Color32 SKIN_DIFFUSE_HIGH = new Color32(255, 255, 255, 255);
        public Color32 SKIN_DIFFUSE_DARK = new Color32(255, 200, 180, 255);

        private static readonly string[] HIDDEN_MATERIALS = {
            "gageye",
            "namida",
            "bonelyfans",
            //"tang"

        };
        
        private static bool ShouldHide(string materialBaseName)
        {
            return HIDDEN_MATERIALS.Any(x => materialBaseName.Contains(x));
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

            string materialBaseName = PMXKKSConverter.GetMaterialBaseName(material.name);

            var main_texture_path = $"{format_.meta_header.folder}/{material.name}_MT_CT.png";
            // KKS Only: Base textures are named "basename_MT_CT.png"
            Texture2D main_texture = (Texture2D)AssetDatabase.LoadAssetAtPath(main_texture_path, typeof(Texture2D));

            if (main_texture == null)
            {
                //Debug.LogWarning($"Main texture {main_texture_path} not found!");
            }

            // Guess material type from name
            var materialType = FernMaterialUtilFunctions.GuessMMDMaterialCategory(material.name);
            
            //マテリアルに設定
            string shader_path = FernMaterialUtilFunctions.GetShaderPath(materialType);
            Material result = new(Shader.Find(shader_path));
            // シェーダに依って値が有ったり無かったりするが、設定してもエラーに為らない様なので全部設定
            // TODO: result.SetColor("_BaseColor", material.diffuse_color);
            result.SetColor("_BaseColor", Color.white);

            if (is_transparent) {
                FernMaterialUtilFunctions.SetRenderType(result, FernMaterialRenderType.Cutout);
            } else {
                FernMaterialUtilFunctions.SetRenderType(result, FernMaterialRenderType.Opaque);
            }

            // Hide invisible materials
            if (ShouldHide(materialBaseName))
            {
                //result.SetFloat("_ZOffset", -1);
                FernMaterialUtilFunctions.SetRenderType(result, FernMaterialRenderType.Translucent);
                var invisibleColor = result.GetColor("_BaseColor");
                invisibleColor.a = 0F;
                result.SetColor("_BaseColor", invisibleColor);
            }

            switch (materialType)
            {
                case FernMaterialCategory.Face:
                    result.SetFloat("_enum_diffuse", 4); // Standard Diffuse => SDFFaceShading
                    result.SetFloat("_CELLThreshold", 0.3F);
                    result.SetFloat("_CELLSmoothing", 0.1F);
                    result.SetColor("_HighColor", SKIN_DIFFUSE_HIGH);
                    result.SetColor("_DarkColor", SKIN_DIFFUSE_DARK);
                    result.SetFloat("_enum_specular", 0); // Standard Specular => None
                    // Setup face SDF parameters
                    Texture2D faceSDFTex = (Texture2D) AssetDatabase.LoadAssetAtPath(
                            $"{format_.meta_header.folder}/FaceSDF.png", typeof (Texture2D));
                    result.SetTexture("_SDFFaceTex", faceSDFTex);
                    result.SetFloat("_SDFFaceArea", 95F);
                    result.SetFloat("_SDFShadingSoftness", 0.1F);
                    // Turn off shadow receiving
                    result.SetFloat("_RECEIVE_SHADOWS_OFF", 0F);
                    break;

                default:
                    result.SetFloat("_enum_diffuse", 0); // Standard Diffuse => CelShading
                    result.SetFloat("_CELLThreshold", 0.3F);
                    result.SetFloat("_CELLSmoothing", 0.1F);
                    result.SetColor("_HighColor", SKIN_DIFFUSE_HIGH);
                    result.SetColor("_DarkColor", SKIN_DIFFUSE_DARK);
                    result.SetFloat("_enum_specular", 0); // Standard Specular => None
                    break;
            }

            result.SetFloat("_Smoothness", 0F);
            // エッジ
            if (material.edge_size > 0F)
            {
                // Enable outline on this material
                result.SetFloat("_Outline", 1F);
                // Set outline width and color
                result.SetFloat("_OutlineWidth", material.edge_size / 0.2F);
                result.SetColor("_OutlineColor", material.edge_color);
            }

            // スフィアテクスチャ [Code removed]

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