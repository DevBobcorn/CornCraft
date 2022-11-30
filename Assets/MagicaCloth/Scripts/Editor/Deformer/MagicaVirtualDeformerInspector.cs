// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 仮想デフォーマーのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaVirtualDeformer))]
    public class MagicaVirtualDeformerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            MagicaVirtualDeformer scr = target as MagicaVirtualDeformer;

            //DrawDefaultInspector();

            serializedObject.Update();

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            Undo.RecordObject(scr, "CreateVirtualDeformer");

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            DrawVirtualDeformerInspector();

            // データ作成
            if (EditorApplication.isPlaying == false)
            {
                EditorGUILayout.Space();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Create"))
                {
                    Undo.RecordObject(scr, "CreateVirtualMeshData");
                    BuildManager.CreateComponent(scr);
                }
                GUI.backgroundColor = Color.white;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawVirtualDeformerInspector()
        {
            MagicaVirtualDeformer scr = target as MagicaVirtualDeformer;

            serializedObject.Update();

            //EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.renderDeformerList"), true);
            EditorInspectorUtility.DrawObjectList<MagicaRenderDeformer>(
                serializedObject.FindProperty("deformer.renderDeformerList"),
                scr.gameObject,
                true, true,
                () => scr.gameObject.transform.root.GetComponentsInChildren<MagicaRenderDeformer>()
                );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reduction Setting", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.mergeVertexDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.mergeTriangleDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.sameSurfaceAngle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.useSkinning"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.maxWeightCount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deformer.weightPow"));

            serializedObject.ApplyModifiedProperties();
        }

        //=========================================================================================
        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaVirtualDeformer scr = target as MagicaVirtualDeformer;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                //scr.SetVerifyError();
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
