using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using MMD.PMX;

namespace MMD
{
    public class PMXKKSConverter : PMXConverter
    {
        private readonly bool isBaseModel;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public PMXKKSConverter(bool isBaseModel)
        {
            this.isBaseModel = isBaseModel;
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
            format_ = format;
            use_ik_ = use_ik;
            use_leg_d_bones_ = use_leg_d_bones;
            scale_ = scale;
            root_game_object_ = new GameObject(format_.meta_header.name);
            MMDEngine engine = root_game_object_.AddComponent<MMDEngine>(); //MMDEngine追加
            //スケール・エッジ幅
            engine.scale = scale_;
            
            MeshCreationInfo[] creation_info = CreateMeshCreationInfo();                // メッシュを作成する為の情報を作成
            Mesh[] mesh = CreateMesh(creation_info);                                    // メッシュの生成・設定
            Material[][] materials = CreateMaterials(creation_info);                    // マテリアルの生成・設定

            Debug.Log("Mesh and materials created");

            GameObject[] bones = CreateBones();                                            // ボーンの生成・設定
            SkinnedMeshRenderer[] renderers = BuildingBindpose(mesh, materials, bones);    // バインドポーズの作成
            CreateMorph(mesh, materials, bones, renderers, creation_info);                 // モーフの生成・設定

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
        /// <param name='creation_info'>メッシュ作成情報</param>
        private Material[][] CreateMaterials(MeshCreationInfo[] creation_info)
        {
            // 適当なフォルダに投げる
            string path = format_.meta_header.folder + "/Materials/";
            if (!System.IO.Directory.Exists(path)) { 
                AssetDatabase.CreateFolder(format_.meta_header.folder, "Materials");
            }
            
            //全マテリアルを作成
            Material[] materials = EntryAttributesForMaterials();
            CreateAssetForMaterials(materials);

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
        private Material[] EntryAttributesForMaterials()
        {
            PMXBaseMaterialConverter materialConv = new PMXFernKKSMaterialConverter(root_game_object_, format_, scale_);
            
            return Enumerable.Range(0, format_.material_list.material.Length)
                    .Select(x => (uint) x)
                    .Select(x => materialConv.Convert(x, IsTransparentByName(isBaseModel, format_.material_list.material[x].name)))
                    .ToArray();
        }
    }
}