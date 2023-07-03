using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace MMD
{
    [CustomEditor(typeof(PMXScriptableObject))]
    public class PMXInspector : Editor
    {
        PMXImportConfig pmx_config;

        // last selected item
        private ModelAgent model_agent;
        private string message = "";

        /// <summary>
        /// 有効化処理
        /// </summary>
        private void OnEnable()
        {
            // デフォルトコンフィグ
            var config = MMD.Config.LoadAndCreate();
            pmx_config = config.pmx_config.Clone();
            
            // モデル情報
            if (config.inspector_config.use_pmx_preload)
            {
                var obj = (PMXScriptableObject)target;
                model_agent = new ModelAgent(obj.assetPath);
            }
            else
            {
                model_agent = null;
            }
        }

        /// <summary>
        /// Inspector上のGUI描画処理を行います
        /// </summary>
        public override void OnInspectorGUI()
        {
            // GUIの有効化
            GUI.enabled = !EditorApplication.isPlaying;

            // GUI描画
            pmx_config.OnGUIFunction();

            // Convertボタン
            EditorGUILayout.Space();
            if (message.Length != 0)
            {
                GUILayout.Label(message);
            }
            else
            {
                if (GUILayout.Button("Convert to Prefab"))
                {
                    if (null == model_agent) {
                        var obj = (PMXScriptableObject)target;
                        model_agent = new ModelAgent(obj.assetPath);
                    }
                    model_agent.CreatePrefab(pmx_config.physics_type
                                            , pmx_config.animation_type
                                            , pmx_config.use_ik
                                            , pmx_config.scale
                                            , pmx_config.entity_prefab
                                            , pmx_config.player_anim_controller
                                            );
                    message = "Loading done.";
                }
            }
            GUILayout.Space(40);

            // モデル情報
            if (model_agent == null) return;
            EditorGUILayout.LabelField("Model Name");
            EditorGUILayout.LabelField(model_agent.name, EditorStyles.textField);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Comment");
            EditorGUILayout.LabelField(model_agent.comment, EditorStyles.textField, GUILayout.Height(300));
        }
    }
}
