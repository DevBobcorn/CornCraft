using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using MMD.PMX;

using MagicaCloth2;

namespace MMD
{
    public class PMXMagicaPhysicsConverter : PMXBasePhysicsConverter
    {
        public PMXMagicaPhysicsConverter(GameObject root_game_object, PMXFormat format, GameObject[] bone_objs, float scale)
                : base(root_game_object, format, bone_objs, scale)
        {
            // Something else to do here...
            
        }

        public override void Convert()
        {
            for (uint boneIndex = 0;boneIndex < format_.bone_list.bone.Length;boneIndex++)
            {
                var bone = format_.bone_list.bone[boneIndex];
            }

            GameObject[] rigids = CreateRigids(out HashSet<uint> jointRigidbodySet, out Dictionary<uint, ColliderComponent> generatedColliders);

            var physics_root_transform = AssignRigidbodyToBone(rigids);

            // Bone index => Rigidbody index mapping
            var clothIndicies = jointRigidbodySet.Where(rIdx => rIdx != uint.MaxValue &&
                            GetRelBoneIndexFromNearbyRigidbody(rIdx) != uint.MaxValue)
                    .ToDictionary(rIdx => GetRelBoneIndexFromNearbyRigidbody(rIdx), rIdx => rIdx);
            
            CreateMagicaClothes(rigids, clothIndicies, generatedColliders, physics_root_transform);

        }

        /// <summary>
        /// 剛体作成
        /// </summary>
        /// <param name='jointRigidbodySet'>A set of indicies of rigidbodies whose colliders should be ignored</param>
        /// <param name='generatedColliders'>Rigidbody index => collider mapping table</param>
        /// <returns>剛体(GameObject Array)</returns>
        GameObject[] CreateRigids(out HashSet<uint> jointRigidbodySet, out Dictionary<uint, ColliderComponent> generatedColliders)
        {
            // Select all rigidbody indicies which are connected by joints
            // These rigidbodies are those in cloth/hair
            jointRigidbodySet = format_.rigidbody_joint_list.joint
                    .Select(x => x.rigidbody_b ).ToHashSet();

            var source = format_.rigidbody_list.rigidbody;
            var colliders = new Dictionary<uint, ColliderComponent>();

            // Generate all colliders for character body
            GameObject[] result = new GameObject[source.Length];
            generatedColliders = new Dictionary<uint, ColliderComponent>();

            for (uint rigidIndex = 0;rigidIndex < result.Length;rigidIndex++)
            {
                result[rigidIndex] = ConvertRigid(rigidIndex, source[rigidIndex],
                        jointRigidbodySet.Contains(rigidIndex), ref generatedColliders);
            }
            
            return result;
        }

        /// <summary>
        /// 剛体をMagica Clothes用に変換する
        /// </summary>
        /// <param name='rigidIndex'>Index of this rigidbody</param>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='isClothOrHair'>Whether this rigidbody is a part of cloth or hair</param>
        /// <param name='colliders'>Rigidbody index => collider mapping table</param>
        /// <returns>Magica Clothes用剛体ゲームオブジェクト</returns>
        GameObject ConvertRigid(uint rigidIndex, PMXFormat.Rigidbody rigidbody, bool isClothOrHair, ref Dictionary<uint, ColliderComponent> colliders)
        {
            GameObject result = new GameObject("r_" + rigidbody.name);
            
            //位置・回転の設定
            result.transform.position = rigidbody.collider_position * scale_;
            result.transform.rotation = Quaternion.Euler(rigidbody.collider_rotation * Mathf.Rad2Deg);
            
            if (!isClothOrHair) // Apply collider if not cloth/hair
            {
                // Colliderの設定
                ColliderComponent collider = rigidbody.shape_type switch
                {
                    PMXFormat.Rigidbody.ShapeType.Sphere    => EntrySphereCollider(rigidbody, result),
                    PMXFormat.Rigidbody.ShapeType.Capsule   => EntryCapsuleCollider(rigidbody, result),

                    // TODO: Find a proper way to handle box colliders
                    _                                       => null
                };

                if (collider is not null)
                {
                    colliders.Add(rigidIndex, collider);
                }
            }

            return result;
        }
        
        /// <summary>
        /// Magica Sphere Colliderの設定
        /// </summary>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='obj'>Magica Clothes用剛体ゲームオブジェクト</param>
        /// <returns>The generated collider</returns>
        MagicaSphereCollider EntrySphereCollider(PMXFormat.Rigidbody rigidbody, GameObject obj)
        {
            var collider = obj.AddComponent<MagicaSphereCollider>();
            collider.SetSize(rigidbody.shape_size.x * scale_); // radius

            return collider;
        }

        /// <summary>
        /// Magica Capsule Colliderの設定
        /// </summary>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='obj'>Magica Clothes用剛体ゲームオブジェクト</param>
        /// <returns>The generated collider</returns>
        MagicaCapsuleCollider EntryCapsuleCollider(PMXFormat.Rigidbody rigidbody, GameObject obj)
        {
            var collider = obj.AddComponent<MagicaCapsuleCollider>();
            collider.direction = MagicaCapsuleCollider.Direction.Y;
            collider.SetSize(
                rigidbody.shape_size.x * scale_, // Start radius
                rigidbody.shape_size.x * scale_, // End radius
                (rigidbody.shape_size.y + rigidbody.shape_size.x * 2.0f) * scale_ // length (height)
            );

            return collider;
        }

        /// <summary>
        /// 剛体とボーンを接続する
        /// </summary>
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        /// <returns>Transform of the created physics object</returns>
        Transform AssignRigidbodyToBone(GameObject[] rigids)
        {
            // 物理演算ルートを生成してルートの子供に付ける
            Transform physics_root_transform = (new GameObject("Physics (For Magica Cloth 2)")).transform;
            physics_root_transform.parent = root_game_object_.transform;

            // 剛体の数だけ回す
            for (uint i = 0, i_max = (uint)rigids.Length; i < i_max; ++i) {
                // 剛体を親ボーンに格納
                uint rel_bone_index = GetRelBoneIndexFromNearbyRigidbody(i);

                /*
                if (rel_bone_index < bones_.Length && format_.bone_list.bone[rel_bone_index].parent_bone_index == uint.MaxValue) {
                    Debug.Log($"Parent of bone {format_.bone_list.bone[rel_bone_index].bone_name} is not assigned, setting it as a child of Physics object");
                    bones_[rel_bone_index].transform.parent = physics_root_transform;
                }
                */

                if (rel_bone_index < bone_game_objs.Length) {
                    //親と為るボーンが有れば
                    //それの子と為る
                    rigids[i].transform.parent = bone_game_objs[rel_bone_index].transform;
                    //Debug.Log($"Parent of [{rel_bone_index}] {rigids[i]} set to {bones[rel_bone_index]}");
                } else {
                    //親と為るボーンが無ければ
                    //物理演算ルートの子と為る
                    rigids[i].transform.parent = physics_root_transform;
                    //Debug.Log($"Parent of [{rel_bone_index}] {rigids[i]} set to Physics");
                }
            }

            return physics_root_transform;
        }

        /// <summary>
        /// 剛体とボーンを接続する
        /// </summary>
        /// <param name='bones'>ボーンのゲームオブジェクト</param>
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        uint GetRelBoneIndexFromNearbyRigidbody(uint rigidbody_index)
        {
            uint bone_count = (uint)format_.bone_list.bone.Length;
            //関連ボーンを探す
            uint result = format_.rigidbody_list.rigidbody[rigidbody_index].rel_bone_index;
            if (result < bone_count) {
                //関連ボーンが有れば
                return result;
            }
            //Debug.Log(string.Format("No matching bone found for rigidbody: {0}", rigidbody_index));
            //それでも無ければ
            //諦める
            result = uint.MaxValue;
            return result;
        }

        private bool doColliderStripping = true;

        void CreateMagicaClothes(GameObject[] rigid_game_objs, Dictionary<uint, uint> clothIndicies,
                Dictionary<uint, ColliderComponent> generatedColliders, Transform physics_root_transform)
        {
            var bone_list = format_.bone_list.bone;
            var rigidbody_list = format_.rigidbody_list.rigidbody;

            var clothRootBones = new HashSet<uint>();

            // Search for all root cloth/hair bones
            foreach (var (boneIndex, rigidIndex) in clothIndicies)
            {
                if (boneIndex >= 0 && boneIndex < bone_list.Length && bone_list[boneIndex] is not null) // Short-circuit logic
                {
                    var bone = bone_list[boneIndex];

                    if (bone.parent_bone_index == uint.MaxValue)
                    {
                        // The parent of this bone is the cloth/hair root, and it is not connected to body
                        clothRootBones.Add(boneIndex);

                        // Get the rigidbody that this cloth bone root is connected to
                        try
                        {
                            var connectedJoint = format_.rigidbody_joint_list.joint
                                    .First(x => x.rigidbody_b == rigidIndex);

                            var connectedRigidbodyIndex = connectedJoint.rigidbody_a;
                            var connectedRigidbody = rigidbody_list[connectedRigidbodyIndex];

                            var clothRootTransform = bone_game_objs[boneIndex].transform;

                            clothRootTransform.parent = rigid_game_objs[connectedRigidbodyIndex].transform;
                            //clothRootTransform.localEulerAngles = Vector3.zero;
                            //clothRootTransform.localPosition = Vector3.zero;

                            Debug.Log($"Disconnected cloth bone found: [{boneIndex}] {bone.bone_name}, connected it to rigidbidy [{connectedRigidbodyIndex}] {connectedRigidbody.name} (GameObject: {rigid_game_objs[connectedRigidbodyIndex]})");
                        }
                        catch
                        {
                            Debug.LogWarning($"Disconnected cloth bone found: [{boneIndex}] {bone.bone_name}, and no joint seem to connect to this bone.");
                        }
                        
                    }
                    else if (!clothIndicies.ContainsKey(bone.parent_bone_index))
                    {
                        // The bone itself is a part of cloth/hair, but its parent is not.
                        // This means that its parent is a transform on character body
                        // used for connecting this piece of cloth/hair

                        //Debug.Log($"[{boneIndex}] {bone.bone_name} Parent (Body) [{bone.parent_bone_index}]");
                        clothRootBones.Add(boneIndex);
                    }
                }
            }

            Regex reg = new Regex(@"(.*)_[0-9]+$");

            // Create magica components for these root bones
            while (clothRootBones.Count > 0)
            {
                uint takenBoneIndex = clothRootBones.First();
                var takenBoneName = bone_list[takenBoneIndex].bone_name;
                var game_object = new GameObject();
                var magica_cloth = game_object.AddComponent<MagicaCloth>();
                var cloth_data = magica_cloth.SerializeData;
                cloth_data.clothType = ClothProcess.ClothType.BoneCloth;

                var nameMatch = reg.Match(takenBoneName);
                string boneDisplayName;

                if (nameMatch.Success) // Got one root bone of a mesh (skirt, etc)
                {
                    var boneMeshName = nameMatch.Groups[1];
                    // Find all bones in this mesh
                    var boneMeshReg = new Regex(@$"{boneMeshName}_[0-9]+");

                    var meshBoneArray = clothRootBones.Where(boneIdx => boneMeshReg.
                            Match(bone_list[boneIdx].bone_name).Success).OrderBy(x => x).ToArray();
                    
                    foreach (var boneIndex in meshBoneArray)
                    {
                        // Take it out of the set
                        clothRootBones.Remove(boneIndex);
                        // Assign this bone
                        cloth_data.rootBones.Add(bone_game_objs[boneIndex].transform);
                    }

                    // Magica Bone Connection Mode - Line // Automatic Mesh
                    cloth_data.connectionMode = RenderSetupData.BoneConnectionMode.Line; // RenderSetupData.BoneConnectionMode.AutomaticMesh;
                    boneDisplayName = $"{boneMeshName} (Bone Group)";
                }
                else // Single line (Hair, ribbon, etc)
                {
                    // Take it out of the set
                    clothRootBones.Remove(takenBoneIndex);
                    // Assign this bone
                    cloth_data.rootBones.Add(bone_game_objs[takenBoneIndex].transform);
                    
                    // Magica Bone Connection Mode - Line
                    cloth_data.connectionMode = RenderSetupData.BoneConnectionMode.Line;
                    boneDisplayName = takenBoneName;
                }
                
                game_object.name = $"Magica Cloth - {boneDisplayName}";

                if (doColliderStripping)
                {
                    // Set colliders for this cloth/hair bone
                    // Get rigidIndex from cloth root bone index
                    var takenRigidbodyIndex = clothIndicies[takenBoneIndex];
                    var takenRigidbody = rigidbody_list[takenRigidbodyIndex];
                    // 16 bits as a 16-layer mask
                    ushort takenRigidbodyColGroupMask = takenRigidbody.ignore_collision_group;
                    // group index, should be within range [0, 16)
                    var takenRigidbodyColGroupIndex = takenRigidbody.group_index;
                    //Debug.Log($"Collision [{boneDisplayName}]: Mask: <{System.Convert.ToString(takenRigidbodyColGroupMask, 2)}> Self Group: [{takenRigidbodyColGroupIndex}]");
                    //Debug.Log($"Collision [{boneDisplayName}]: ColPos: {takenRigidbody.collider_position}");

                    var collidersInvolved = generatedColliders.Where(pair => { // pair: Rigidbody index => collider
                        var colRigidbody = rigidbody_list[pair.Key];
                        // TODO: Strip out those colliders that is far from this cloth
                        if ((takenRigidbody.collider_position - colRigidbody.collider_position).sqrMagnitude >= 15F)
                        {
                            // This collider is far from current cloth bone, ignore it
                            return false;
                        }
                        // Collider is not in the same group as current cloth
                        return (colRigidbody.group_index != takenRigidbodyColGroupIndex) &&
                        // and collider is not in a group which is ignored by current cloth
                                ((takenRigidbodyColGroupMask & (1 << colRigidbody.group_index)) != 0);
                    }).Select(x => x.Value).ToList();
                    // Set cloth colliders
                    cloth_data.colliderCollisionConstraint.colliderList = collidersInvolved;
                }
                else
                {
                    cloth_data.colliderCollisionConstraint.colliderList = generatedColliders.Select(x => x.Value).ToList();
                }

                cloth_data.angleRestorationConstraint.stiffness.SetValue(0.8F);
                cloth_data.inertiaConstraint.worldInertia = 1F;
                cloth_data.inertiaConstraint.depthInertia = 1F;
                cloth_data.gravity = 1F;

                //magica_cloth.SetParameterChange();

                game_object.transform.SetParent(physics_root_transform);
                game_object.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// 非衝突剛体の設定
        /// </summary>
        /// <returns>非衝突剛体のリスト(Group index => indicies of rigidbodies in this group)</returns>
        List<int>[] SettingIgnoreRigidGroups()
        {
            // 非衝突グループ用リストの初期化
            const int MaxGroup = 16;    // グループの最大数
            List<int>[] result = new List<int>[MaxGroup];
            for (int i = 0, i_max = MaxGroup; i < i_max; ++i) {
                result[i] = new List<int>();
            }

            // それぞれの剛体が所属している非衝突グループを追加していく
            for (int i = 0, i_max = format_.rigidbody_list.rigidbody.Length; i < i_max; ++i) {
                result[format_.rigidbody_list.rigidbody[i].group_index].Add(i);
            }
            return result;
        }

        /// <summary>
        /// グループターゲットの取得
        /// </summary>
        /// <returns>グループターゲット</returns>
        int[] GetRigidbodyGroupTargets()
        {
            return format_.rigidbody_list.rigidbody.Select(x=>(int)x.ignore_collision_group).ToArray();
        }
    }
}