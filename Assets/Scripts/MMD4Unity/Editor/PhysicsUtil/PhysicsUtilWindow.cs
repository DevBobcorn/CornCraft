using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MMD
{
    public class PhysicsUtilWindow : EditorWindow
    {
        private Animator targetAnimator = null;
        private GameObject target = null;
        private string pmxFilePath = "";
        private float pmxScale = 0.078F;

        [MenuItem("MMD for Unity/Chara Physics Util")]
        static void Init()
        {
            var window = GetWindow<PhysicsUtilWindow>(false, "Chara Physics Util");
            window.Show();
        }

        /// <summary>
        /// This is called when the selected object for edit is changed
        /// </summary>
        /// <param name="newTarget">The new selected object</param>
        void HandleTargetChange(GameObject newTarget)
        {
            target = newTarget;

            if (target == null || (targetAnimator = target.GetComponent<Animator>()) == null)
            {
                target = null;
                targetAnimator = null;
            }
            else
            {
                // Initialize target
            }
        }

        void DetectTargetChange()
        {
            var newTarget = Selection.activeObject as GameObject;

            if (newTarget != target)
            {
                HandleTargetChange(newTarget);

                Repaint();
            }
        }

        void OnFocus()
        {
            DetectTargetChange();
        }

        void OnSelectionChange()
        {
            DetectTargetChange();
        }

        private Vector2 scrollPos = Vector2.zero;

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos);

                EditorGUILayout.LabelField("Target Selection", EditorStyles.boldLabel);

                GUI.enabled = false; // Draw the object to make it clear who's the current target

                EditorGUILayout.ObjectField("Target", target, typeof (GameObject), true);

                GUI.enabled = true;

                GUILayout.Space(10);

                if (targetAnimator == null)
                {
                    GUILayout.Label("Please select a valid character object with Animator!");
                }
                else
                {
                    EditorGUILayout.LabelField("Initialize Physics (HSR)", EditorStyles.boldLabel);

                    if (GUILayout.Button("Regenerate Physics for Outfit"))
                    {
                        RegenOutfitPhysicsForHSR(target);
                    }

                    GUILayout.BeginHorizontal();

                        pmxFilePath = GUILayout.TextField(pmxFilePath);

                        pmxScale = EditorGUILayout.FloatField(pmxScale);

                        if (GUILayout.Button("Remove All Magica Colliders"))
                        {
                            RemoveAllColliders(target);
                        }

                        if (GUILayout.Button("Add Magica Colliders from .pmx File"))
                        {
                            AddCollidersFromPMX(pmxFilePath.Trim('\"'), pmxScale, target, targetAnimator);
                        }

                    GUILayout.EndHorizontal();
                }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private static void BoneSearch(Transform bone, Func<Transform, bool> query)
        {
            foreach (Transform child in bone)
            {
                var searchChildren = query.Invoke(child);

                if (searchChildren)
                {
                    BoneSearch(child, query);
                }
            }
        }

        private static List<Transform> GetBoneRoots(Transform armature, string rootPrefixLower)
        {
            List<Transform> roots = new();

            BoneSearch(armature, x => {
                if (x.name.ToLower().StartsWith(rootPrefixLower))
                {
                    roots.Add(x);
                    return false; // Don't search its children
                }
                
                return true;
            });

            return roots;
        }

        private static void AddCloth(Transform physicsRoot, string name, List<Transform> clothRoots, List<MagicaCloth2.ColliderComponent> colliders = null)
        {
            var clothObject = new GameObject(name);
            clothObject.transform.SetParent(physicsRoot);

            var cloth = clothObject.AddComponent<MagicaCloth2.MagicaCloth>();
            cloth.SerializeData.clothType = MagicaCloth2.ClothProcess.ClothType.BoneCloth;
            cloth.SerializeData.rootBones = clothRoots;

            if (colliders != null)
            {
                cloth.SerializeData.colliderCollisionConstraint.colliderList = colliders;
            }
        }

        private static void RegenOutfitPhysicsForHSR(GameObject target)
        {
            var armatureRoot = target.transform.Find("Main");
            var physicsRoot = target.transform.Find("Physics");

            if (physicsRoot == null)
            {
                physicsRoot = new GameObject("Physics").transform;
                physicsRoot.SetParent(target.transform);
                physicsRoot.localPosition = Vector3.zero;
            }
            else
            {
                // Clear previously generated clothes
                foreach (Transform transform in physicsRoot)
                {
                    DestroyImmediate(transform.gameObject);
                }
            }

            // Get all colliders
            Dictionary<HumanoidColliderLayer, MagicaCloth2.ColliderComponent[]> colliders;

            HumanoidColliderLayer getLayerFromName(GameObject go)
            {
                var name = go.name;
                
                try
                {
                    return (HumanoidColliderLayer) Enum.Parse(typeof (HumanoidColliderLayer), name[(name.LastIndexOf("#") + 1)..]);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to get collider layer from name: {e.Message}");
                    return HumanoidColliderLayer.Torso;
                }
            }

            var c = target.GetComponentsInChildren<MagicaCloth2.ColliderComponent>();
            colliders = c.GroupBy(x => getLayerFromName(x.gameObject), x => x).ToDictionary(x => x.Key, x => x.ToArray());

            List<MagicaCloth2.ColliderComponent> getCollidersByMask(int mask)
            {
                var result = new List<MagicaCloth2.ColliderComponent>();

                foreach (var pair in colliders)
                {
                    if (((int) pair.Key & mask) != 0)
                    {
                        result.AddRange(pair.Value);
                    }
                }

                return result;
            }

            // Hair physics
            var hairRoots = GetBoneRoots(armatureRoot, "hair");
            AddCloth(physicsRoot, "Hair", hairRoots, getCollidersByMask(0b00011));

            // Skirt physics
            var skirtRoots = GetBoneRoots(armatureRoot, "skirt");
            AddCloth(physicsRoot, "Skirt", skirtRoots, getCollidersByMask(0b11101));

            // Ribbon physics
            var ribbonRoots = GetBoneRoots(armatureRoot, "ribbon");
            AddCloth(physicsRoot, "Ribbon", ribbonRoots, getCollidersByMask(0b11101));

            AssetDatabase.Refresh();
        }

        enum HumanoidColliderLayer
        {
            Torso = 0b00001,
            Head  = 0b00010,
            Hips  = 0b00100,
            Legs  = 0b01000,
            Arms  = 0b10000,
        }

        private static readonly Dictionary<string, HumanBodyBones> PMX_BONE_MAPPING = new()
        {
            ["上半身"]     = HumanBodyBones.Spine,
            ["上半身2"]    = HumanBodyBones.UpperChest,
            ["首"]         = HumanBodyBones.Neck,
            ["頭"]         = HumanBodyBones.Head,

            ["下半身"]      = HumanBodyBones.Hips,

            ["右肩"]       = HumanBodyBones.RightShoulder,
            ["右腕"]       = HumanBodyBones.RightUpperArm,
            ["右ひじ"]     = HumanBodyBones.RightLowerArm,
            ["右手首"]     = HumanBodyBones.RightHand,
            ["左肩"]       = HumanBodyBones.LeftShoulder,
            ["左腕"]       = HumanBodyBones.LeftUpperArm,
            ["左ひじ"]     = HumanBodyBones.LeftLowerArm,
            ["左手首"]     = HumanBodyBones.LeftHand,
            
            ["右足"]       = HumanBodyBones.RightUpperLeg,
            ["右ひざ"]     = HumanBodyBones.RightLowerLeg,
            ["左足"]       = HumanBodyBones.LeftUpperLeg,
            ["左ひざ"]     = HumanBodyBones.LeftLowerLeg,
        };

        private static readonly Dictionary<string, HumanoidColliderLayer> PMX_BONE_LAYER = new()
        {
            ["上半身"]     = HumanoidColliderLayer.Torso,
            ["上半身2"]    = HumanoidColliderLayer.Torso,
            ["首"]         = HumanoidColliderLayer.Head,
            ["頭"]         = HumanoidColliderLayer.Head,

            ["下半身"]      = HumanoidColliderLayer.Hips,

            ["右肩"]       = HumanoidColliderLayer.Torso,
            ["右腕"]       = HumanoidColliderLayer.Arms,
            ["右ひじ"]     = HumanoidColliderLayer.Arms,
            ["右手首"]     = HumanoidColliderLayer.Arms,
            ["左肩"]       = HumanoidColliderLayer.Torso,
            ["左腕"]       = HumanoidColliderLayer.Arms,
            ["左ひじ"]     = HumanoidColliderLayer.Arms,
            ["左手首"]     = HumanoidColliderLayer.Arms,
            
            ["右足"]       = HumanoidColliderLayer.Legs,
            ["右ひざ"]     = HumanoidColliderLayer.Legs,
            ["左足"]       = HumanoidColliderLayer.Legs,
            ["左ひざ"]     = HumanoidColliderLayer.Legs,
        };

        private static readonly Dictionary<string, bool> ARM_BONES = new()
        {
            ["右腕"]   = true,
            ["右ひじ"] = true,
            ["右手首"] = true,
            ["左腕"]   = false,
            ["左ひじ"] = false,
            ["左手首"] = false,
        };

        private static Transform GetTransformForPMXBone(Transform root, Animator animator, string boneName)
        {
            if (PMX_BONE_MAPPING.ContainsKey(boneName))
            {
                return animator.GetBoneTransform(PMX_BONE_MAPPING[boneName]);
            }

            /*
            if (PMX_BONE_MAPPING_HSR.ContainsKey(boneName))
            {
                var a = root.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == PMX_BONE_MAPPING_HSR[boneName]);

                if (a == null)
                {
                    Debug.LogWarning(PMX_BONE_MAPPING_HSR[boneName] + " not found in target transform!");

                    return null;
                }
                return a;
            }
            */

            //Debug.LogWarning($"Could not find a counterpart in target for {boneName}!");

            return null;
        }

        private static void RemoveAllColliders(GameObject target)
        {
            var colliders = target.GetComponentsInChildren<MagicaCloth2.ColliderComponent>();

            foreach(var col in colliders)
            {
                DestroyImmediate(col.gameObject);
            }

            AssetDatabase.Refresh();
        }

        private static void AddCollidersFromPMX(string pmxFilePath, float pmxScale, GameObject target, Animator animator)
        {
            PMX.PMXFormat pmx_format = null;
            try {
                pmx_format = PMXLoaderScript.Import(pmxFilePath);
            } catch (FormatException) {
                Debug.LogWarning("Failed to read pmx file.");
            }

            var colliders = pmx_format.rigidbody_list.rigidbody.Where(
                    x => x.operation_type == PMX.PMXFormat.Rigidbody.OperationType.Static);
            
            var tRoot = new GameObject("Temp Root");
            tRoot.transform.parent = target.transform;
            tRoot.transform.localPosition = Vector3.zero;
            // Turn over
            tRoot.transform.localRotation = Quaternion.AngleAxis(180F, Vector3.up);

            foreach (var col in colliders)
            {
                var boneId = col.rel_bone_index;

                // Make sure it is attached to a bone
                if (boneId >= 0 && boneId < pmx_format.bone_list.bone.Length)
                {
                    var boneName = pmx_format.bone_list.bone[boneId].bone_name;

                    var boneTransform = GetTransformForPMXBone(target.transform, animator, boneName);
                    if (boneTransform != null)
                    {
                        var collider = PMXMagicaPhysicsConverter.AttachColliderFromRigid(tRoot.transform, boneTransform, col, pmxScale);

                        // Perform post-processing
                        if (collider != null)
                        {
                            collider.gameObject.name += $"#{PMX_BONE_LAYER[boneName]}";

                            // Make some transform adjustments for arm colliders, because MMD arm pose is different from model pose
                            if (ARM_BONES.ContainsKey(boneName))
                            {
                                var ct = collider.transform;
                                ct.localEulerAngles = new(0F, 0F, 90F);
                                
                                if (collider is MagicaCloth2.MagicaCapsuleCollider cc)
                                {
                                    var ccsize = cc.GetSize(); // (始点半径, 終点半径, 長さ)
                                    var length = ccsize.z - ccsize.x - ccsize.y;

                                    if (ARM_BONES[boneName])
                                    {
                                        ct.localPosition = new(-length / 2F, 0F, 0F);
                                    }
                                    else
                                    {
                                        ct.localPosition = new( length / 2F, 0F, 0F);
                                    }
                                }
                                else
                                {
                                    ct.localPosition = Vector3.zero;
                                }
                            }
                        }
                    }
                }
            }

            DestroyImmediate(tRoot);

            AssetDatabase.Refresh();
        }

        #region GUI Util functions

        #endregion
    }
}
