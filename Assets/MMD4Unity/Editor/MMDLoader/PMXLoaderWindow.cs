using UnityEngine;
using System.Collections;
using UnityEditor;

public class PMXLoaderWindow : EditorWindow {
    Object pmxFile;
    MMD.PMXImportConfig pmx_config;

    [MenuItem("MMD for Unity/PMX Loader")]
    static void Init() {
        var window = (PMXLoaderWindow)EditorWindow.GetWindow<PMXLoaderWindow>(true, "PMX Loader");
        window.Show();
    }

    void OnEnable()
    {
        // デフォルトコンフィグ
        pmxFile = null;
        pmx_config = MMD.Config.LoadAndCreate().pmx_config.Clone();
    }
    
    void OnGUI() {
        // GUIの有効化
        GUI.enabled = !EditorApplication.isPlaying;
        
        // GUI描画
        pmxFile = EditorGUILayout.ObjectField("PMX File" , pmxFile, typeof(Object), false);
        pmx_config.OnGUIFunction();
        
        {
            bool gui_enabled_old = GUI.enabled;
            GUI.enabled = !EditorApplication.isPlaying && (pmxFile != null);
            if (GUILayout.Button("Convert")) {
                LoadModel();
                pmxFile = null;        // 読み終わったので空にする 
            }
            GUI.enabled = gui_enabled_old;
        }
    }

    void LoadModel() {
        string file_path = AssetDatabase.GetAssetPath(pmxFile);
        MMD.ModelAgent model_agent = new MMD.ModelAgent(file_path);
        model_agent.CreatePrefab(pmx_config.physics_type
                                , pmx_config.animation_type
                                , pmx_config.use_ik
                                , pmx_config.use_leg_d_bones
                                , pmx_config.scale
                                , pmx_config.player_anim_controller
                                );
        
        // 読み込み完了メッセージ
        var window = LoadedWindow.Init();
        window.Text = string.Format(
            "----- model name -----\n{0}\n\n----- comment -----\n{1}",
            model_agent.name,
            model_agent.comment
        );
        window.Show();
    }
}
