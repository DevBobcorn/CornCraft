using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using MMD.PMX;

namespace MMD
{
    public class PMXKKSConverter : PMXConverter
    {
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public PMXKKSConverter(bool isBaseModel)
        {
        }

        /// <summary>
        /// GameObjectを作成する
        /// </summary>
        /// <param name='format'>内部形式データ</param>
        /// <param name='physics_type'>剛体を使用するか</param>
        /// <param name='animation_type'>アニメーションタイプ</param>
        /// <param name='use_ik'>IKを使用するか</param>
        /// <param name='use_leg_d_bones'>Whether or not to directly use d-bones to manipulate leg animations.</param>
        /// <param name='scale'>スケール</param>
        public override GameObject CreateGameObject(PMXFormat format, PhysicsType physics_type, AnimationType animation_type, bool use_ik, bool use_leg_d_bones, float scale)
        {
            Debug.Log($"Creating prefab from {format.meta_header.folder}...");

            format_ = format;
            use_ik_ = use_ik;
            use_leg_d_bones_ = use_leg_d_bones;
            scale_ = scale;
            root_game_object_ = new GameObject(format_.meta_header.name);
            MMDEngine engine = root_game_object_.AddComponent<MMDEngine>(); //MMDEngine追加
            //スケール・エッジ幅
            engine.scale = scale_;

            //PMXファイルのインポート
            try {
                //PMX読み込みを試みる
                Debug.Log($"Reading outfit data from {format_.meta_header.folder + "/Outfit 00/model.pmx"}...");
                outfit_format_ = PMXLoaderScript.Import(format_.meta_header.folder + "/Outfit 00/model.pmx");
            } catch (System.FormatException) {
                Debug.LogWarning("Failed to read outfit pmx file.");
            }
            
            MeshCreationInfo[] creation_info = CreateMeshCreationInfo(format_);                                 // メッシュを作成する為の情報を作成
            Mesh[] mesh = CreateMesh(format_, creation_info);                                                   // メッシュの生成・設定
            Material[][] materials = CreateMaterials(format_, is_base_model: true, creation_info);              // マテリアルの生成・設定

            MeshCreationInfo[] o_creation_info = CreateMeshCreationInfo(outfit_format_);                        // メッシュを作成する為の情報を作成
            Mesh[] o_mesh = CreateMesh(outfit_format_, o_creation_info, save_name: "outfit");                   // メッシュの生成・設定
            Material[][] o_materials = CreateMaterials(outfit_format_, is_base_model: false, o_creation_info);  // マテリアルの生成・設定

            GameObject[] bones = CreateBones();                                               // ボーンの生成・設定

            // Outfit shares exactly same bones with base model
            SkinnedMeshRenderer[] renderers = BuildingBindpose(mesh, materials, bones);       // バインドポーズの作成
            SkinnedMeshRenderer[] o_renderers = BuildingBindpose(o_mesh, o_materials, bones); // バインドポーズの作成

            o_renderers[0].transform.parent.gameObject.name = "Mesh (Outfit)";

            CreateMorph(mesh, materials, bones, renderers, creation_info);                    // モーフの生成・設定(Base model only)

            // Fern NPR Face SDF helper script assigning [Code removed]

            /*

            // BoneController・IKの登録(use_ik_を使った判定はEntryBoneController()の中で行う)
            {
                engine.bone_controllers = EntryBoneController(bones);
                engine.ik_list = engine.bone_controllers.Where(x=>null != x.ik_solver)
                                                        .Select(x=>x.ik_solver)
                                                        .ToArray();
            }
    
            // Physics関連
            if (physics_type != PhysicsType.None)
            {
                PMXBasePhysicsConverter physConv = physics_type switch
                {
                    PhysicsType.UnityPhysics  => new PMXUnityPhysicsConverter(root_game_object_, format_, bones, scale_),
                    PhysicsType.MagicaCloth2  => new PMXMagicaPhysicsConverter(root_game_object_, format_, bones, scale_),

                    _                         => new PMXUnityPhysicsConverter(root_game_object_, format_, bones, scale_)
                };

                physConv.Convert();
            }

            // Mecanim設定
            if (AnimationType.LegacyAnimation != animation_type) {
                //アニメーター追加
                var avatar_setting = new AvatarSettingScript(root_game_object_, bones);
                switch (animation_type) {
                case AnimationType.GenericMecanim: //汎用アバターでのMecanim
                    avatar_setting.SettingGenericAvatar();
                    break;
                case AnimationType.HumanMecanim: //人型アバターでのMecanim
                    avatar_setting.SettingHumanAvatar(use_leg_d_bones);
                    break;
                default:
                    throw new System.ArgumentException();
                }
                
                string path = format_.meta_header.folder + "/";
                string name = GetFilePathString(format_.meta_header.name);
                string file_name = path + name + ".avatar.asset";
                avatar_setting.CreateAsset(file_name);
            } else {
                root_game_object_.AddComponent<Animation>();    // アニメーション追加
            }

            */

            return root_game_object_;
        }

        /// <summary>
        /// マテリアル作成
        /// </summary>
        /// <returns>マテリアル</returns>
        /// <param name='mats_owner'>The format containing the materials to process. <br>This can be different from 'format_'.</param>
        /// <param name='is_base_model'>True if base model, False if outfit.</param>
        /// <param name='creation_info'>メッシュ作成情報</param>
        private Material[][] CreateMaterials(PMXFormat mats_owner, bool is_base_model, MeshCreationInfo[] creation_info)
        {
            // 適当なフォルダに投げる
            string path = format_.meta_header.folder + "/Materials/";
            if (!System.IO.Directory.Exists(path)) { 
                AssetDatabase.CreateFolder(format_.meta_header.folder, "Materials");
            }
            
            //全マテリアルを作成
            Material[] materials = EntryAttributesForMaterials(mats_owner, is_base_model);
            CreateAssetForMaterials(mats_owner, materials);

            //メッシュ単位へ振り分け
            Material[][] result = new Material[creation_info.Length][];
            for (int i = 0, i_max = creation_info.Length; i < i_max; ++i) {
                result[i] = creation_info[i].value.Select(x=>materials[x.material_index]).ToArray();
            }

            return result;
        }

        private static readonly string[] BASE_MODEL_TRANSPARENT_MATERIALS = {
            "mayuge",
            "noseline",
            //"tooth",
            "eyeline",
            "namida",
            //"sirome",
            "hitomi",
            "gageye",
        };

        private static readonly string[] OUTFIT_MODEL_TRANSPARENT_MATERIALS = {
            
        };

        public static string GetMaterialBaseName(string materialName)
        {
            materialName = materialName.ToLower();

            return materialName;
        }

        private static bool IsTransparentByName(bool isBaseModel, string materialName)
        {
            materialName = GetMaterialBaseName(materialName);

            if (isBaseModel)
                return BASE_MODEL_TRANSPARENT_MATERIALS.Any(x => materialName.Contains(x));
            else
                return OUTFIT_MODEL_TRANSPARENT_MATERIALS.Any(x => materialName.Contains(x));
        }

        /// <summary>
        /// マテリアルに基本情報(シェーダー・カラー・テクスチャ)を登録する
        /// </summary>
        /// <returns>マテリアル</returns>
        private Material[] EntryAttributesForMaterials(PMXFormat mats_owner, bool is_base_model)
        {
            PMXBaseMaterialConverter materialConv = new PMXFernKKSMaterialConverter(root_game_object_, mats_owner, scale_);
            
            return Enumerable.Range(0, mats_owner.material_list.material.Length)
                    .Select(x => (uint) x)
                    .Select(x => materialConv.Convert(x, IsTransparentByName(is_base_model, mats_owner.material_list.material[x].name)))
                    .ToArray();
        }

        // Character outfix is stored in another pmx file
        protected PMXFormat outfit_format_;
    }
}