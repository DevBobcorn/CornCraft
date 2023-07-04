using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace MMD {
    
    public class ModelAgent {
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name='file'>読み込むファイルパス</param>
        public ModelAgent(string file_path) {
            if (string.IsNullOrEmpty(file_path)) {
                throw new System.ArgumentException();
            }
            file_path_ = file_path;
            header_ = null;
            try {
                //PMX読み込みを試みる
                header_ = PMXLoaderScript.GetHeader(file_path_);
            } catch (System.FormatException) {
                Debug.Log("Failed to load pmx file");
            }
        }

        /// <summary>
        /// プレファブを作成する
        /// </summary>
        /// <param name='physics_type'>Physicsタイプ</param>
        /// <param name='animation_type'>アニメーションタイプ</param>
        /// <param name='use_ik'>IKを使用するか</param>
        /// <param name='scale'>スケール</param>
        public void CreatePrefab(PMXConverter.PhysicsType physics_type, PMXConverter.AnimationType animation_type, bool use_ik, float scale,
                AnimatorController player_anim_controller) {
            //PMX Baseでインポートする
            //PMXファイルのインポート
            PMX.PMXFormat pmx_format = null;
            try {
                //PMX読み込みを試みる
                pmx_format = PMXLoaderScript.Import(file_path_);
            } catch (System.FormatException) {
                Debug.LogWarning("Failed to read pmx file.");
            }
            header_ = pmx_format.header;

            //ゲームオブジェクトの作成
            GameObject visualObj = PMXConverter.CreateGameObject(pmx_format, physics_type, animation_type, use_ik, scale);

            // Assign animator controller
            var player_animator = visualObj.GetComponent<Animator>();
            player_animator.runtimeAnimatorController = player_anim_controller;

            // プレファブパスの設定
            string prefabPath = pmx_format.meta_header.folder + "/" + pmx_format.meta_header.name + ".prefab";

            // プレファブ化
            //PrefabUtility.CreatePrefab(prefabPath, visualObj, ReplacePrefabOptions.ConnectToPrefab);
            PrefabUtility.SaveAsPrefabAssetAndConnect(visualObj, prefabPath, InteractionMode.AutomatedAction);

            // アセットリストの更新
            AssetDatabase.Refresh();
        }


        /// <summary>
        /// モデル名取得
        /// </summary>
        /// <value>モデル名</value>
        public string name {get{
            string result = null;
            if (null != header_) {
                result = header_.model_name;
            }
            return result;
        }}
    
        /// <summary>
        /// 英語表記モデル名取得
        /// </summary>
        /// <value>英語表記モデル名</value>
        public string english_name {get{
            string result = null;
            if (null != header_) {
                result = header_.model_english_name;
            }
            return result;
        }}
    
        /// <summary>
        /// モデル製作者からのコメント取得
        /// </summary>
        /// <value>モデル製作者からのコメント</value>
        public string comment {get{
            string result = null;
            if (null != header_) {
                result = header_.comment;
            }
            return result;
        }}
    
        /// <summary>
        /// モデル製作者からの英語コメント取得
        /// </summary>
        /// <value>モデル製作者からの英語コメント</value>
        public string english_comment {get{
            string result = null;
            if (null != header_) {
                    result = header_.english_comment;
            }
            return result;
        }}
        
        string                     file_path_;
        PMX.PMXFormat.Header    header_;
    }
}