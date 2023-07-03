using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using MinecraftClient.Control;
using MinecraftClient.Rendering;
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
                GameObject entity_prefab, AnimatorController player_anim_controller) {
            GameObject visual_game_object;
            string prefab_path;

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
            visual_game_object = PMXConverter.CreateGameObject(pmx_format, physics_type, animation_type, use_ik, scale);

            var char_name = pmx_format.meta_header.name;

            // プレファブパスの設定
            prefab_path = pmx_format.meta_header.folder + "/" + char_name + ".prefab";

            // プレファブ化
            //PrefabUtility.CreatePrefab(prefab_path, game_object, ReplacePrefabOptions.ConnectToPrefab);
            PrefabUtility.SaveAsPrefabAssetAndConnect(visual_game_object, prefab_path, InteractionMode.AutomatedAction);

            // Create Playable Player Entity GameObject
            if (entity_prefab != null)
            {
                // Prepare player controller gameobject
                var player_game_object = GameObject.Instantiate(entity_prefab);
                player_game_object.name = $"Client {char_name} Player Controller";
                var player_controller = player_game_object.GetComponent<PlayerController>();

                // Post-process visual game object
                visual_game_object.name = "Visual";
                visual_game_object.transform.SetParent(player_game_object.transform);
                player_controller.visualTransform = visual_game_object.transform;
                // Add and initialize player widgets
                visual_game_object.AddComponent<PlayerAnimatorWidget>();
                var accessory_widget = visual_game_object.AddComponent<PlayerAccessoryWidget>();
                var animator_render = player_controller.GetComponent<PlayerEntityRiggedRender>();
                var player_animator = visual_game_object.GetComponent<Animator>();
                player_animator.runtimeAnimatorController = player_anim_controller;
                animator_render.AssignFields(visual_game_object.transform, player_animator);
                var weapon_ref_object = new GameObject("Weapon Ref");
                weapon_ref_object.transform.SetParent(visual_game_object.transform);
                weapon_ref_object.transform.localPosition = new(0F, 0.7F, -0.35F);
                weapon_ref_object.transform.localEulerAngles = new(-85F, 0F, 90F);
                accessory_widget.weaponRef = weapon_ref_object.transform;

                // Setup camera reference
                var camera_ref_object = new GameObject("Camera Ref");
                camera_ref_object.transform.SetParent(player_game_object.transform);
                camera_ref_object.transform.localPosition = new(0F, 1.2F, 0F);
                player_controller.cameraRef = camera_ref_object.transform;

                // Update visual gameobject layer (do this last to ensure all children are present)
                int player_layer = player_game_object.layer;
                foreach (var child in visual_game_object.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = player_layer;
                }
            }

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