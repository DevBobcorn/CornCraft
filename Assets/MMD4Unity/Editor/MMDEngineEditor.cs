using UnityEngine;
using UnityEditor;

/// <summary>
/// MMDEngine用Inspector拡張
/// </summary>
[CustomEditor(typeof(MMDEngine))]
public sealed class MMDEngineEditor : Editor
{
    /// <summary>
    /// スタティックコンストラクタ
    /// </summary>
    static MMDEngineEditor()
    {
        ik_list_display_ = false;
    }
    
    /// <summary>
    /// 初回処理
    /// </summary>
    public void Awake()
    {
    }
    
    /// <summary>
    /// Inspector描画
    /// </summary>
    public override void OnInspectorGUI()
    {
        bool is_dirty = false;
        
        is_dirty = OnInspectorGUIforUseRigidbody() || is_dirty;
        is_dirty = OnInspectorGUIforIkList() || is_dirty;
        
        if (is_dirty) {
            //更新が有ったなら
            //Inspector更新
            EditorUtility.SetDirty(target);
        }
    }

    /// <summary>
    /// リジッドボティ使用の為のInspector描画
    /// </summary>
    /// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
    private bool OnInspectorGUIforUseRigidbody()
    {
        MMDEngine self = (MMDEngine)target;
        bool is_update = false;
        
        bool use_rigidbody = self.useRigidbody;
        use_rigidbody = EditorGUILayout.Toggle("Use Rigidbody", use_rigidbody);
        if (self.useRigidbody != use_rigidbody) {
            //変更が掛かったなら
            //Undo登録
#if !UNITY_4_2 //4.3以降
            Undo.RecordObject(self, "Use Rigidbody Change");
#else
            Undo.RegisterUndo(self, "Use Rigidbody Change");
#endif
            //更新
            self.useRigidbody = use_rigidbody;
            
            is_update = true;
        }
        return is_update;
    }
    
    /// <summary>
    /// IKリストの為のInspector描画
    /// </summary>
    /// <returns>更新が有ったか(true:更新有り, false:未更新)</returns>
    private bool OnInspectorGUIforIkList()
    {
        MMDEngine self = (MMDEngine)target;
        bool is_update = false;
        
        //IKリストツリータイトル
        ik_list_display_ = EditorGUILayout.Foldout(ik_list_display_, "IK List");
        //IKリストツリー内部
        if (ik_list_display_) {
            //IKリストを表示するなら
            GUIStyle style = new GUIStyle();
            style.margin.left = 10;
            EditorGUILayout.BeginVertical(style);
            {
                foreach (CCDIKSolver ik in self.ik_list) {
                    bool enabled = ik.enabled;
                    enabled = EditorGUILayout.Toggle(ik.name, enabled);
                    if (ik.enabled != enabled) {
                        //変更が掛かったなら
                        //Undo登録
#if !UNITY_4_2 //4.3以降
                        Undo.RecordObject(ik, "Enabled Change");
#else
                        Undo.RegisterUndo(ik, "Enabled Change");
#endif
                        //更新
                        ik.enabled = enabled;
                        //改変したIKのInspector更新
                        EditorUtility.SetDirty(ik);
                        
                        is_update = true;
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        return is_update;
    }

    private static    bool    ik_list_display_;    //IKリストの表示
}