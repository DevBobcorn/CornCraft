// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// メニューアイテム
    /// </summary>
    public class MenuItemScript
    {
        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Virtual Deformer", priority = 0)]
        static void AddMergeDeformer()
        {
            var obj = AddObject("Magica Virtual Deformer", true);
            obj.AddComponent<MagicaVirtualDeformer>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Bone Cloth", priority = 0)]
        static void AddBoneCloth()
        {
            var obj = AddObject("Magica Bone Cloth", true);
            obj.AddComponent<MagicaBoneCloth>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Bone Spring", priority = 0)]
        static void AddBoneSpring()
        {
            var obj = AddObject("Magica Bone Spring", true);
            obj.AddComponent<MagicaBoneSpring>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Mesh Cloth", priority = 0)]
        static void AddMeshCloth()
        {
            var obj = AddObject("Magica Mesh Cloth", true);
            obj.AddComponent<MagicaMeshCloth>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Mesh Spring", priority = 0)]
        static void AddMeshSpring()
        {
            var obj = AddObject("Magica Mesh Spring", true);
            obj.AddComponent<MagicaMeshSpring>();
            Selection.activeGameObject = obj;
        }

        /*[MenuItem("GameObject/Create Other/Magica Cloth/Merge Mesh Deformer", priority = 100)]
        static void AddMergeMeshDeformer()
        {
            var obj = AddObject("Merge Mesh Deformer", false);
            obj.AddComponent<MergeMeshDeformer>();
            Selection.activeGameObject = obj;
        }*/

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Sphere Collider", priority = 200)]
        static void AddSphereCollider()
        {
            var obj = AddObject("Magica Sphere Collider", true, true);
            obj.AddComponent<MagicaSphereCollider>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Capsule Collider", priority = 200)]
        static void AddCapsuleCollider()
        {
            var obj = AddObject("Magica Capsule Collider", true, true);
            obj.AddComponent<MagicaCapsuleCollider>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Plane Collider", priority = 200)]
        static void AddPlaneCollider()
        {
            var obj = AddObject("Magica Plane Collider", true, true);
            obj.AddComponent<MagicaPlaneCollider>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Directional Wind", priority = 200)]
        static void AddDirectionalWind()
        {
            var obj = AddObject("Magica Directional Wind", true);
            obj.AddComponent<MagicaDirectionalWind>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth/Magica Area Wind", priority = 200)]
        static void AddAreaWind()
        {
            var obj = AddObject("Magica Area Wind", true);
            obj.AddComponent<MagicaAreaWind>();
            Selection.activeGameObject = obj;
        }

        //=========================================================================================
        /// <summary>
        /// ヒエラルキーにオブジェクトを１つ追加する
        /// </summary>
        /// <param name="objName"></param>
        /// <returns></returns>
        static GameObject AddObject(string objName, bool addParentName, bool autoScale = false)
        {
            var parent = Selection.activeGameObject;

            GameObject obj = new GameObject(addParentName && parent ? objName + " (" + parent.name + ")" : objName);
            if (parent)
            {
                obj.transform.parent = parent.transform;
            }
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            if (autoScale && parent)
            {
                var scl = parent.transform.lossyScale;
                obj.transform.localScale = new Vector3(1.0f / scl.x, 1.0f / scl.y, 1.0f / scl.z);
            }
            else
                obj.transform.localScale = Vector3.one;

            return obj;
        }

        //=========================================================================================
        [MenuItem("Assets/Magica Cloth/Clean up sub-assets", priority = 201)]
        static void CleanUpSubAssets()
        {
            try
            {
                Debug.Log("Clean up MagicaCloth sub-asssets.");

                if (Selection.activeGameObject == null)
                    throw new Exception("No Object");
                var obj = Selection.activeGameObject;
                //Debug.Log(obj.name);

                // プレハブ判定
                if (PrefabUtility.IsPartOfAnyPrefab(obj) == false || PrefabUtility.IsPartOfPrefabAsset(obj) == false)
                    throw new Exception("No Prefab");

                // クリーンアップ
                ShareDataPrefabExtension.CleanUpSubAssets(obj);

                Debug.Log("Complete.");
            }
            catch
            {
                Debug.LogWarning("Run it against the Prefab in the Project window.");
            }
        }
    }
}
