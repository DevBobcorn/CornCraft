// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    public class MenuItemScript
    {
        //=========================================================================================
        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Cloth", priority = 200)]
        static void AddMagicaCloth()
        {
            var obj = AddObject("Magica Cloth", false, false);
            var comp = obj.AddComponent<MagicaCloth>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Sphere Collider", priority = 200)]
        static void AddSphereCollider()
        {
            var obj = AddObject("Magica Sphere Collider", true, true);
            var comp = obj.AddComponent<MagicaSphereCollider>();
            //comp.size = new Vector3(0.1f, 0.1f, 0.1f);
            comp.SetSize(new Vector3(0.1f, 0.1f, 0.1f));
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Capsule Collider", priority = 200)]
        static void AddCapsuleCollider()
        {
            var obj = AddObject("Magica Capsule Collider", true, true);
            var comp = obj.AddComponent<MagicaCapsuleCollider>();
            //comp.size = new Vector3(0.05f, 0.05f, 0.3f);
            comp.SetSize(new Vector3(0.05f, 0.05f, 0.3f));
            comp.direction = MagicaCapsuleCollider.Direction.Y;
            comp.radiusSeparation = false;
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Plane Collider", priority = 200)]
        static void AddPlaneCollider()
        {
            var obj = AddObject("Magica Plane Collider", true, true);
            var comp = obj.AddComponent<MagicaPlaneCollider>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Wind Zone", priority = 200)]
        static void AddWindZone()
        {
            var obj = AddObject("Magica Wind Zone", false, true);
            var comp = obj.AddComponent<MagicaWindZone>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Settings", priority = 200)]
        static void AddSettings()
        {
            var obj = AddObject("Magica Settings", false, true);
            var comp = obj.AddComponent<MagicaSettings>();
            Selection.activeGameObject = obj;
        }

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
        [MenuItem("Tools/Magica Cloth2/Manager information", false)]
        static void DispClothManagerInfo()
        {
            if (MagicaManager.IsPlaying() == false)
            {
                Debug.Log("This feature is run-time only.");
                return;
            }

            StringBuilder allsb = new StringBuilder();

            var timeManager = MagicaManager.Time;
            if (timeManager == null)
            {
                Debug.LogWarning("Time Manager is null!");
            }
            else
            {
                timeManager.InformationLog(allsb);
            }

            var teamManager = MagicaManager.Team;
            if (teamManager == null)
            {
                Debug.LogWarning("Team Manager is null!");
            }
            else
            {
                teamManager.InformationLog(allsb);
            }

            var vmeshManager = MagicaManager.VMesh;
            if (vmeshManager == null)
            {
                Debug.LogWarning("VMesh Manager is null!");
            }
            else
            {
                vmeshManager.InformationLog(allsb);
            }

            var transformManager = MagicaManager.Bone;
            if (transformManager == null)
            {
                Debug.LogWarning("Transform Manager is null!");
            }
            else
            {
                transformManager.InformationLog(allsb);
            }

            var simulationManager = MagicaManager.Simulation;
            if (simulationManager == null)
            {
                Debug.LogWarning("Simulation Manager is null!");
            }
            else
            {
                simulationManager.InformationLog(allsb);
            }

            var colliderManager = MagicaManager.Collider;
            if (colliderManager == null)
            {
                Debug.LogWarning("Collider Manager is null!");
            }
            else
            {
                colliderManager.InformationLog(allsb);
            }

            var windManager = MagicaManager.Wind;
            if (windManager == null)
            {
                Debug.LogWarning("Wind Manager is null!");
            }
            else
            {
                windManager.InformationLog(allsb);
            }

            var renderManager = MagicaManager.Render;
            if (renderManager == null)
            {
                Debug.LogWarning("Renderer Manager is null!");
            }
            else
            {
                renderManager.InformationLog(allsb);
            }

            // clipboard
            //GUIUtility.systemCopyBuffer = allsb.ToString();

            // file
            DateTime dt = DateTime.Now;
            var filename = dt.ToString("yyyy-MM-dd-HHmm-ss");
            StreamWriter sw = new StreamWriter($"./MagicaCloth2_SysInfo_{filename}.txt", false);
            sw.WriteLine(allsb.ToString());
            sw.Flush();
            sw.Close();
        }
    }
}
