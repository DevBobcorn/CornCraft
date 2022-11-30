// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// アバターのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaAvatar))]
    public class MagicaAvatarInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            MagicaAvatar scr = target as MagicaAvatar;

            // データ状態
            EditorInspectorUtility.DispVersionStatus(scr);
            EditorInspectorUtility.DispDataStatus(scr);

            serializedObject.Update();
            //Undo.RecordObject(scr, "CreateBoneCloth");

            // メイン
            MainInspector();

            // モニターボタン
            //EditorInspectorUtility.MonitorButtonInspector();

            //DrawDefaultInspector();

            // パーツリスト
            if (EditorApplication.isPlaying)
            {
                if (DrawPartsList())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // イベント
            Events();

            serializedObject.ApplyModifiedProperties();
        }

        //=========================================================================================
        private void MainInspector()
        {
            //MagicaAvatar scr = target as MagicaAvatar;
            //EditorGUILayout.Space();
        }

        private void Events()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnAttachParts"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnDetachParts"));
        }

        //=========================================================================================
        /// <summary>
        /// パーツ一覧表示
        /// </summary>
        private bool DrawPartsList()
        {
            MagicaAvatar scr = target as MagicaAvatar;
            bool change = false;

            EditorGUILayout.LabelField("Attach Parts", EditorStyles.boldLabel);

            // ドラッグ＆ドロップ
            change = DrawPartsDragAndDropArea();

            // パーツ一覧
            MagicaAvatarParts removeParts = null;
            for (int i = 0; i < scr.Runtime.AvatarPartsCount; i++)
            {
                var parts = scr.Runtime.GetAvatarParts(i);
                if (parts)
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Space(30);
                    EditorGUILayout.HelpBox(parts.name, MessageType.None);
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Remove"))
                    {
                        removeParts = parts;
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            if (removeParts)
            {
                // パーツ削除
                scr.DetachAvatarParts(removeParts);
                change = true;
            }

            return change;
        }

        /// <summary>
        /// アバターパーツのドラッグ＆ドロップ受け付け
        /// </summary>
        /// <returns></returns>
        private bool DrawPartsDragAndDropArea()
        {
            bool change = false;
            var evt = Event.current;

            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));

            var style = new GUIStyle(GUI.skin.box);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Box(dropArea, "Drag & Drop\n[Avatar Parts]", style);
            GUI.backgroundColor = Color.white;

            GameObject attachPartsObject = null;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    DragAndDrop.activeControlID = id;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            //Debug.Log("Drag Object:" + AssetDatabase.GetAssetPath(draggedObject));
                            //m_FilePath.stringValue = AssetDatabase.GetAssetPath(draggedObject);
                            //Debug.Log("GameObject:" + (draggedObject is GameObject));
                            //Debug.Log("AvatarParts:" + (draggedObject is MagicaAvatarParts));
                            if (draggedObject is GameObject)
                            {
                                var go = draggedObject as GameObject;
                                if (go.GetComponent<MagicaAvatarParts>())
                                {
                                    //Debug.Log("Avatar Parts!!");
                                    attachPartsObject = go;
                                }
                            }
                        }
                        DragAndDrop.activeControlID = 0;
                    }
                    Event.current.Use();
                    //change = true;
                    break;
            }

            if (attachPartsObject)
            {
                // パーツ追加
                MagicaAvatar scr = target as MagicaAvatar;
                scr.AttachAvatarParts(attachPartsObject);
                change = true;
            }

            return change;
        }
    }
}