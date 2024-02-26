using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MMD
{
    public class PhysicsUtilWindow : EditorWindow
    {
        private Animator targetAnimator = null;
        private GameObject target = null;

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
                    EditorGUILayout.LabelField("Initialize Physics (KKS)", EditorStyles.boldLabel);

                    if (GUILayout.Button("Add Physics for Outfit"))
                    {
                        AddOutfitPhysics(target);
                    }
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

        private static List<Transform> GetBoneRoots(Transform armature, string rootPrefix)
        {
            List<Transform> roots = new();

            BoneSearch(armature, x => {
                if (x.name.StartsWith(rootPrefix))
                {
                    roots.Add(x);
                    return false; // Don't search its children
                }
                
                return true;
            });

            return roots;
        }

        private static void AddCloth(Transform physicsRoot, string name, List<Transform> clothRoots)
        {
            var clothObject = new GameObject(name);
            clothObject.transform.SetParent(physicsRoot);

            var cloth = clothObject.AddComponent<MagicaCloth2.MagicaCloth>();
            cloth.SerializeData.clothType = MagicaCloth2.ClothProcess.ClothType.BoneCloth;
            cloth.SerializeData.rootBones = clothRoots;

        }

        private static void AddOutfitPhysics(GameObject target)
        {
            var armatureRoot = target.transform.Find("Armature");
            var physicsRoot = target.transform.Find("Physics");

            if (physicsRoot == null)
            {
                physicsRoot = new GameObject("Physics").transform;
                physicsRoot.SetParent(target.transform);
                physicsRoot.localPosition = Vector3.zero;
            }

            // Hair physics
            var hairRoots = GetBoneRoots(armatureRoot, "cf_J_hair");
            AddCloth(physicsRoot, "Hair", hairRoots);

            // Skirt physics
            var skirtRoots = GetBoneRoots(armatureRoot, "cf_j_sk_");
            AddCloth(physicsRoot, "Skirt", skirtRoots);
        }

        #region GUI Util functions

        #endregion
    }
}
