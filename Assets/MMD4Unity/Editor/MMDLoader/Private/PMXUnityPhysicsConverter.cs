using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MMD.PMX;

namespace MMD
{
    public class PMXUnityPhysicsConverter : PMXBasePhysicsConverter
    {
        public PMXUnityPhysicsConverter(MMDEngine engine, GameObject root_game_object, PMXFormat format, GameObject[] bones, float scale)
                : base(engine, root_game_object, format, bones, scale)
        {
            // Something else to do here...
            
        }

        public override void Convert()
        {
            GameObject[] rigids = CreateRigids();
            var physics_root_transform = AssignRigidbodyToBone(bone_game_objs, rigids);
            SetRigidsSettings(bone_game_objs, rigids);
            GameObject[] joints = CreateJoints(rigids);

            GlobalizeRigidbody(joints, physics_root_transform);

            // 非衝突グループ
            List<int>[] ignoreGroups = SettingIgnoreRigidGroups(rigids);
            int[] groupTarget = GetRigidbodyGroupTargets(rigids);

            MMDEngine.Initialize(engine_, groupTarget, ignoreGroups, rigids);
        }

        /// <summary>
        /// 剛体作成
        /// </summary>
        /// <returns>剛体</returns>
        GameObject[] CreateRigids()
        {
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(format_.meta_header.folder, "Physics"))) {
                AssetDatabase.CreateFolder(format_.meta_header.folder, "Physics");
            }
            
            // 剛体の登録
            GameObject[] result = format_.rigidbody_list.rigidbody.Select(x=>ConvertRigidbody(x)).ToArray();
            for (uint i = 0, i_max = (uint)result.Length; i < i_max; ++i) {
                // マテリアルの設定
                result[i].GetComponent<Collider>().material = CreatePhysicMaterial(format_.rigidbody_list.rigidbody, i);
                
            }
            
            return result;
        }

        /// <summary>
        /// 剛体をUnity用に変換する
        /// </summary>
        /// <returns>Unity用剛体ゲームオブジェクト</returns>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        GameObject ConvertRigidbody(PMXFormat.Rigidbody rigidbody)
        {
            GameObject result = new GameObject("r_" + rigidbody.name);
            //result.AddComponent<Rigidbody>();    // 1つのゲームオブジェクトに複数の剛体が付く事が有るので本体にはrigidbodyを適用しない
            
            //位置・回転の設定
            result.transform.position = rigidbody.collider_position * scale_;
            result.transform.rotation = Quaternion.Euler(rigidbody.collider_rotation * Mathf.Rad2Deg);
            
            // Colliderの設定
            switch (rigidbody.shape_type) {
            case PMXFormat.Rigidbody.ShapeType.Sphere:
                EntrySphereCollider(rigidbody, result);
                break;
            case PMXFormat.Rigidbody.ShapeType.Box:
                EntryBoxCollider(rigidbody, result);
                break;
            case PMXFormat.Rigidbody.ShapeType.Capsule:
                EntryCapsuleCollider(rigidbody, result);
                break;
            default:
                throw new System.ArgumentException();
            }
            return result;
        }
        
        /// <summary>
        /// Sphere Colliderの設定
        /// </summary>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='obj'>Unity用剛体ゲームオブジェクト</param>
        void EntrySphereCollider(PMXFormat.Rigidbody rigidbody, GameObject obj)
        {
            SphereCollider collider = obj.AddComponent<SphereCollider>();
            collider.radius = rigidbody.shape_size.x * scale_;
        }

        /// <summary>
        /// Box Colliderの設定
        /// </summary>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='obj'>Unity用剛体ゲームオブジェクト</param>
        void EntryBoxCollider(PMXFormat.Rigidbody rigidbody, GameObject obj)
        {
            BoxCollider collider = obj.AddComponent<BoxCollider>();
            collider.size = rigidbody.shape_size * 2.0f * scale_;
        }

        /// <summary>
        /// Capsule Colliderの設定
        /// </summary>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='obj'>Unity用剛体ゲームオブジェクト</param>
        void EntryCapsuleCollider(PMXFormat.Rigidbody rigidbody, GameObject obj)
        {
            CapsuleCollider collider = obj.AddComponent<CapsuleCollider>();
            collider.radius = rigidbody.shape_size.x * scale_;
            collider.height = (rigidbody.shape_size.y + rigidbody.shape_size.x * 2.0f) * scale_;
        }

        /// <summary>
        /// 物理素材の作成
        /// </summary>
        /// <returns>物理素材</returns>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='index'>剛体インデックス</param>
        /// <param name='name'>Path as rigidbody file name</param>
        PhysicMaterial CreatePhysicMaterial(PMXFormat.Rigidbody[] rigidbodys, uint index)
        {
            PMXFormat.Rigidbody rigidbody = rigidbodys[index];
            PhysicMaterial material = new PhysicMaterial(format_.meta_header.name + "_r_" + rigidbody.name);
            material.bounciness = rigidbody.recoil;
            material.staticFriction = rigidbody.friction;
            material.dynamicFriction = rigidbody.friction;
            
            string name = GetFilePathString(rigidbody.name);
            string file_name = format_.meta_header.folder + "/Physics/" + index.ToString() + "_" + name + ".asset";
            AssetDatabase.CreateAsset(material, file_name);
            return material;
        }

        /// <summary>
        /// 剛体とボーンを接続する
        /// </summary>
        /// <param name='bones'>ボーンのゲームオブジェクト</param>
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        /// <returns>Transform of the created physics object</returns>
        Transform AssignRigidbodyToBone(GameObject[] bones, GameObject[] rigids)
        {
            // 物理演算ルートを生成してルートの子供に付ける
            Transform physics_root_transform = (new GameObject("Physics", typeof(PhysicsManager))).transform;
            physics_root_transform.parent = root_game_object_.transform;

            Debug.Log($"Bone count: {bones.Length}");
            
            // 剛体の数だけ回す
            for (uint i = 0, i_max = (uint)rigids.Length; i < i_max; ++i) {
                // 剛体を親ボーンに格納
                uint rel_bone_index = GetRelBoneIndexFromNearbyRigidbody(i);

                if (rel_bone_index < bones.Length) {
                    //親と為るボーンが有れば
                    //それの子と為る
                    rigids[i].transform.parent = bones[rel_bone_index].transform;
                    Debug.Log($"Parent of [{rel_bone_index}] {rigids[i]} set to {bones[rel_bone_index]}");
                } else {
                    //親と為るボーンが無ければ
                    //物理演算ルートの子と為る
                    rigids[i].transform.parent = physics_root_transform;
                    Debug.Log($"Parent of [{rel_bone_index}] {rigids[i]} set to Physics");
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
            Debug.Log(string.Format("No matching bone found for rigidbody: {0}", rigidbody_index));
            //それでも無ければ
            //諦める
            result = uint.MaxValue;
            return result;
        }

        /// <summary>
        /// 剛体の値を設定する
        /// </summary>
        /// <param name='bones'>ボーンのゲームオブジェクト</param>
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        void SetRigidsSettings(GameObject[] bones, GameObject[] rigid)
        {
            uint bone_count = (uint)format_.bone_list.bone.Length;
            for (uint i = 0, i_max = (uint)format_.rigidbody_list.rigidbody.Length; i < i_max; ++i) {
                PMXFormat.Rigidbody rigidbody = format_.rigidbody_list.rigidbody[i];
                GameObject target;
                if (rigidbody.rel_bone_index < bone_count) {
                    //関連ボーンが有るなら
                    //関連ボーンに付与する
                    target = bones[rigidbody.rel_bone_index];
                } else {
                    //関連ボーンが無いなら
                    //剛体に付与する
                    target = rigid[i];
                }
                UnityRigidbodySetting(rigidbody, target);
            }
        }

        /// <summary>
        /// Unity側のRigidbodyの設定を行う
        /// </summary>
        /// <param name='rigidbody'>PMX用剛体データ</param>
        /// <param name='targetBone'>設定対象のゲームオブジェクト</param>
        void UnityRigidbodySetting(PMXFormat.Rigidbody pmx_rigidbody, GameObject target)
        {
            Rigidbody rigidbody = target.GetComponent<Rigidbody>();
            if (null != rigidbody) {
                //減衰値は平均を取る
                float totMass = rigidbody.mass + pmx_rigidbody.weight;
                rigidbody.drag = (rigidbody.drag * rigidbody.mass + pmx_rigidbody.position_dim * pmx_rigidbody.weight) / totMass;
                rigidbody.angularDrag = (rigidbody.angularDrag * rigidbody.mass + pmx_rigidbody.rotation_dim * pmx_rigidbody.weight) / totMass;
                //既にRigidbodyが付与されているなら
                //質量は合算する
                rigidbody.mass = totMass;
            }
            else {
                //まだRigidbodyが付与されていないなら
                rigidbody = target.AddComponent<Rigidbody>();
                rigidbody.isKinematic = (PMXFormat.Rigidbody.OperationType.Static == pmx_rigidbody.operation_type);
                rigidbody.mass = Mathf.Max(float.Epsilon, pmx_rigidbody.weight);
                rigidbody.drag = pmx_rigidbody.position_dim;
                rigidbody.angularDrag = pmx_rigidbody.rotation_dim;
            }
        }

        /// <summary>
        /// ジョイント作成
        /// </summary>
        /// <returns>ジョイントのゲームオブジェクト</returns>
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        GameObject[] CreateJoints(GameObject[] rigids)
        {
            // ConfigurableJointの設定
            GameObject[] joints = SetupConfigurableJoint(rigids);
            return joints;
        }

        /// <summary>
        /// ConfigurableJointの設定
        /// </summary>
        /// <remarks>
        /// 先に設定してからFixedJointを設定する
        /// </remarks>
        /// <returns>ジョイントのゲームオブジェクト</returns>
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        GameObject[] SetupConfigurableJoint(GameObject[] rigids)
        {
            List<GameObject> result_list = new List<GameObject>();
            foreach (PMXFormat.Joint joint in format_.rigidbody_joint_list.joint) {
                //相互接続する剛体の取得
                Transform transform_a = rigids[joint.rigidbody_a].transform;
                Rigidbody rigidbody_a = transform_a.GetComponent<Rigidbody>();
                if (null == rigidbody_a) {
                    rigidbody_a = transform_a.parent.GetComponent<Rigidbody>();
                }
                Transform transform_b = rigids[joint.rigidbody_b].transform;
                Rigidbody rigidbody_b = transform_b.GetComponent<Rigidbody>();
                if (null == rigidbody_b) {
                    rigidbody_b = transform_b.parent.GetComponent<Rigidbody>();
                }
                if (rigidbody_a != rigidbody_b) {
                    //接続する剛体が同じ剛体を指さないなら
                    //(本来ならPMDの設定が間違っていない限り同一を指す事は無い)
                    //ジョイント設定
                    ConfigurableJoint config_joint = rigidbody_b.gameObject.AddComponent<ConfigurableJoint>();
                    config_joint.connectedBody = rigidbody_a;
                    SetAttributeConfigurableJoint(joint, config_joint);
                    
                    result_list.Add(config_joint.gameObject);
                }
            }
            return result_list.ToArray();
        }

        /// <summary>
        /// ConfigurableJointの値を設定する
        /// </summary>
        /// <param name='joint'>PMX用ジョイントデータ</param>
        /// <param name='conf'>Unity用ジョイント</param>
        void SetAttributeConfigurableJoint(PMXFormat.Joint joint, ConfigurableJoint conf)
        {
            SetMotionAngularLock(joint, conf);
            SetDrive(joint, conf);
        }

        /// <summary>
        /// ジョイントに移動・回転制限のパラメータを設定する
        /// </summary>
        /// <param name='joint'>PMX用ジョイントデータ</param>
        /// <param name='conf'>Unity用ジョイント</param>
        void SetMotionAngularLock(PMXFormat.Joint joint, ConfigurableJoint conf)
        {
            SoftJointLimit jlim;

            // Motionの固定
            if (joint.constrain_pos_lower.x == 0.0f && joint.constrain_pos_upper.x == 0.0f) {
                conf.xMotion = ConfigurableJointMotion.Locked;
            } else {
                conf.xMotion = ConfigurableJointMotion.Limited;
            }

            if (joint.constrain_pos_lower.y == 0.0f && joint.constrain_pos_upper.y == 0.0f) {
                conf.yMotion = ConfigurableJointMotion.Locked;
            } else {
                conf.yMotion = ConfigurableJointMotion.Limited;
            }

            if (joint.constrain_pos_lower.z == 0.0f && joint.constrain_pos_upper.z == 0.0f) {
                conf.zMotion = ConfigurableJointMotion.Locked;
            } else {
                conf.zMotion = ConfigurableJointMotion.Limited;
            }

            // 角度の固定
            if (joint.constrain_rot_lower.x == 0.0f && joint.constrain_rot_upper.x == 0.0f) {
                conf.angularXMotion = ConfigurableJointMotion.Locked;
            } else {
                conf.angularXMotion = ConfigurableJointMotion.Limited;
                float hlim = Mathf.Max(-joint.constrain_rot_lower.x, -joint.constrain_rot_upper.x); //回転方向が逆なので負数
                float llim = Mathf.Min(-joint.constrain_rot_lower.x, -joint.constrain_rot_upper.x);
                SoftJointLimit jhlim = new SoftJointLimit();
                jhlim.limit = Mathf.Clamp(hlim * Mathf.Rad2Deg, -180.0f, 180.0f);
                conf.highAngularXLimit = jhlim;

                SoftJointLimit jllim = new SoftJointLimit();
                jllim.limit = Mathf.Clamp(llim * Mathf.Rad2Deg, -180.0f, 180.0f);
                conf.lowAngularXLimit = jllim;
            }

            if (joint.constrain_rot_lower.y == 0.0f && joint.constrain_rot_upper.y == 0.0f) {
                conf.angularYMotion = ConfigurableJointMotion.Locked;
            } else {
                // 値がマイナスだとエラーが出るので注意
                conf.angularYMotion = ConfigurableJointMotion.Limited;
                float lim = Mathf.Min(Mathf.Abs(joint.constrain_rot_lower.y), Mathf.Abs(joint.constrain_rot_upper.y));//絶対値の小さい方
                jlim = new SoftJointLimit();
                jlim.limit = lim * Mathf.Clamp(Mathf.Rad2Deg, 0.0f, 180.0f);
                conf.angularYLimit = jlim;
            }

            if (joint.constrain_rot_lower.z == 0f && joint.constrain_rot_upper.z == 0f) {
                conf.angularZMotion = ConfigurableJointMotion.Locked;
            } else {
                conf.angularZMotion = ConfigurableJointMotion.Limited;
                float lim = Mathf.Min(Mathf.Abs(-joint.constrain_rot_lower.z), Mathf.Abs(-joint.constrain_rot_upper.z));//絶対値の小さい方//回転方向が逆なので負数
                jlim = new SoftJointLimit();
                jlim.limit = Mathf.Clamp(lim * Mathf.Rad2Deg, 0.0f, 180.0f);
                conf.angularZLimit = jlim;
            }
        }

        /// <summary>
        /// ジョイントにばねなどのパラメータを設定する
        /// </summary>
        /// <param name='joint'>PMX用ジョイントデータ</param>
        /// <param name='conf'>Unity用ジョイント</param>
        void SetDrive(PMXFormat.Joint joint, ConfigurableJoint conf)
        {
            JointDrive drive;

            // Position
            if (joint.spring_position.x != 0.0f) {
                drive = new JointDrive();
                drive.positionSpring = joint.spring_position.x * scale_;
                conf.xDrive = drive;
            }
            if (joint.spring_position.y != 0.0f) {
                drive = new JointDrive();
                drive.positionSpring = joint.spring_position.y * scale_;
                conf.yDrive = drive;
            }
            if (joint.spring_position.z != 0.0f) {
                drive = new JointDrive();
                drive.positionSpring = joint.spring_position.z * scale_;
                conf.zDrive = drive;
            }

            // Angular
            if (joint.spring_rotation.x != 0.0f) {
                drive = new JointDrive();
                drive.positionSpring = joint.spring_rotation.x;
                conf.angularXDrive = drive;
            }
            if (joint.spring_rotation.y != 0.0f || joint.spring_rotation.z != 0.0f) {
                drive = new JointDrive();
                drive.positionSpring = (joint.spring_rotation.y + joint.spring_rotation.z) * 0.5f;
                conf.angularYZDrive = drive;
            }

            conf.projectionMode = JointProjectionMode.PositionAndRotation;
        }

        /// <summary>
        /// 剛体のグローバル座標化
        /// </summary>
        /// <param name='joints'>ジョイントのゲームオブジェクト</param>
        /// <param name='physics_root_transform'>Physics object transform</param>
        protected void GlobalizeRigidbody(GameObject[] joints, Transform physics_root_transform)
        {
            PhysicsManager physics_manager = physics_root_transform.gameObject.GetComponent<PhysicsManager>();

            if ((null != joints) && (0 < joints.Length)) {
                // PhysicsManagerに移動前の状態を覚えさせる(幾つか重複しているので重複は削除)
                physics_manager.connect_bone_list = joints.Select(x=>x.gameObject)
                        .Distinct()
                        .Select(x=>new PhysicsManager.ConnectBone(x, x.transform.parent.gameObject))
                        .ToArray();
                
                //isKinematicで無くConfigurableJointを持つ場合はグローバル座標化
                foreach (ConfigurableJoint joint in joints.Where(x=>!x.GetComponent<Rigidbody>().isKinematic)
                        .Select(x=>x.GetComponent<ConfigurableJoint>())) {
                    joint.transform.parent = physics_root_transform;
                }
            }
        }

        /// <summary>
        /// 非衝突剛体の設定
        /// </summary>
        /// <returns>非衝突剛体のリスト</returns>
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        List<int>[] SettingIgnoreRigidGroups(GameObject[] rigids)
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
        /// <param name='rigids'>剛体のゲームオブジェクト</param>
        int[] GetRigidbodyGroupTargets(GameObject[] rigids)
        {
            return format_.rigidbody_list.rigidbody.Select(x=>(int)x.ignore_collision_group).ToArray();
        }
    }
}