// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// メッシュスプリングのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaMeshSpring))]
    public class MagicaMeshSpringInspector : ClothEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            serializedObject.Update();
            Undo.RecordObject(scr, "CreateMeshSpring");

            // データ検証
            if (EditorApplication.isPlaying == false)
                VerifyData();

            // モニターボタン
            EditorInspectorUtility.MonitorButtonInspector();

            EditorGUI.BeginChangeCheck();

            // メイン
            MainInspector();

            // パラメータ
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorPresetUtility.DrawPresetButton(scr, scr.Params);
            {
                var cparam = serializedObject.FindProperty("clothParams");
                if (EditorInspectorUtility.AlgorithmInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.Algorithm), ConvertToLatestAlgorithmParameters))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Algorithm);
                if (EditorInspectorUtility.GravityInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Gravity);
                if (EditorInspectorUtility.ExternalForceInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ExternalForce);
                if (EditorInspectorUtility.DragInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Drag);
                if (EditorInspectorUtility.MaxVelocityInspector(cparam))
                    scr.Params.SetChangeParam(ClothParams.ParamType.MaxVelocity);
                if (EditorInspectorUtility.WorldInfluenceInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.WorldInfluence)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.WorldInfluence);
                if (EditorInspectorUtility.DistanceDisableInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.DistanceDisable)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.DistanceDisable);
                if (EditorInspectorUtility.ClampPositionInspector(cparam, true, scr.HasChangedParam(ClothParams.ParamType.ClampPosition)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.ClampPosition);
                if (EditorInspectorUtility.FullSpringInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.Spring)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.Spring);
                if (EditorInspectorUtility.AdjustRotationInspector(cparam, scr.HasChangedParam(ClothParams.ParamType.AdjustRotation)))
                    scr.Params.SetChangeParam(ClothParams.ParamType.AdjustRotation);
            }
            serializedObject.ApplyModifiedProperties();

            // データ作成
            if (EditorApplication.isPlaying == false)
            {
                EditorGUI.BeginDisabledGroup(CheckCreate() == false);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Create"))
                {
                    Undo.RecordObject(scr, "CreateMeshSpringData");
                    BuildManager.CreateComponent(scr);
                }
                GUI.backgroundColor = Color.white;

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                GUI.backgroundColor = Color.blue;
                if (GUILayout.Button("Reset Position"))
                {
                    scr.ResetCloth();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                // Sceneビュー更新
                SceneView.RepaintAll();
            }
        }

        //=========================================================================================
        /// <summary>
        /// 作成を実行できるか判定する
        /// </summary>
        /// <returns></returns>
        protected override bool CheckCreate()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;

            if (scr.Deformer == null)
                return false;

            if (scr.Deformer.VerifyData() != Define.Error.None)
                return false;

            return true;
        }

        /// <summary>
        /// データ検証
        /// </summary>
        private void VerifyData()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;
            if (scr.VerifyData() != Define.Error.None)
            {
                // 検証エラー
                serializedObject.ApplyModifiedProperties();
            }
        }

        //=========================================================================================
        void MainInspector()
        {
            MagicaMeshSpring scr = target as MagicaMeshSpring;

            EditorGUILayout.LabelField("Main Setup", EditorStyles.boldLabel);

            // マージメッシュデフォーマー
            EditorGUILayout.PropertyField(serializedObject.FindProperty("virtualDeformer"));

            EditorGUILayout.Space();

            // センタートランスフォーム
            scr.CenterTransform = EditorGUILayout.ObjectField(
                "Center Transform", scr.CenterTransform, typeof(Transform), true
                ) as Transform;
            scr.DirectionAxis = (MagicaMeshSpring.Axis)EditorGUILayout.EnumPopup("Direction Axis", scr.DirectionAxis);

            EditorGUILayout.Space();

            // チーム項目
            TeamBasicInspector();

            // カリング
            CullingInspector();
        }
    }
}
