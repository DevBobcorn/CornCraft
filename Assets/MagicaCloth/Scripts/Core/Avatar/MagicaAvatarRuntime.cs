// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// ランタイム処理
    /// </summary>
    public class MagicaAvatarRuntime : MagicaAvatarAccess
    {
        /// <summary>
        /// このアバターが保持するボーン辞書
        /// </summary>
        private Dictionary<string, Transform> boneDict = new Dictionary<string, Transform>();

        /// <summary>
        /// このアバターが保持するボーンの参照数
        /// </summary>
        private Dictionary<Transform, int> boneReferenceDict = new Dictionary<Transform, int>();

        /// <summary>
        /// アバターパーツリスト
        /// </summary>
        private List<MagicaAvatarParts> avatarPartsList = new List<MagicaAvatarParts>();

        /// <summary>
        /// このアバターが保持するコライダーリスト
        /// </summary>
        /// <typeparam name="ColliderComponent"></typeparam>
        /// <returns></returns>
        private List<ColliderComponent> colliderList = new List<ColliderComponent>();

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            CreateBoneDict();
            CreateColliderList();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// 有効化
        /// </summary>
        public override void Active()
        {
        }

        /// <summary>
        /// 無効化
        /// </summary>
        public override void Inactive()
        {
        }

        //=========================================================================================
        public int AvatarPartsCount
        {
            get
            {
                return avatarPartsList.Count;
            }
        }

        public MagicaAvatarParts GetAvatarParts(int index)
        {
            return avatarPartsList[index];
        }

        //=========================================================================================
        /// <summary>
        /// すべてのボーンを辞書に登録する
        /// この時にボーン名に重複があると着せ替えのときに問題を起こす可能性がある
        /// </summary>
        private void CreateBoneDict()
        {
            var tlist = owner.GetComponentsInChildren<Transform>();

            foreach (var t in tlist)
            {
                if (boneDict.ContainsKey(t.name))
                {
                    // Duplication name!
                    Debug.LogWarning(string.Format("{0} [{1}]", Define.GetErrorMessage(Define.Error.OverlappingTransform), t.name));
                }
                else
                {
                    boneDict.Add(t.name, t);
                    boneReferenceDict.Add(t, 1); // 参照数１で初期化
                }
            }
        }

        /// <summary>
        /// アバターが保持するコライダーをリスト化する
        /// </summary>
        private void CreateColliderList()
        {
            var clist = owner.GetComponentsInChildren<ColliderComponent>();
            if (clist != null && clist.Length > 0)
            {
                colliderList.AddRange(clist);
            }
        }

        /// <summary>
        /// 現在アバターが保有するコライダー数を取得する
        /// </summary>
        /// <returns></returns>
        public int GetColliderCount()
        {
            if (Application.isPlaying)
            {
                return colliderList.Count;
            }
            else
            {
                return owner.GetComponentsInChildren<ColliderComponent>().Length;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ゲームオブジェクト名が重複するトランスフォームのリストを返す
        /// </summary>
        /// <returns></returns>
        public List<Transform> CheckOverlappingTransform()
        {
            var boneHash = new HashSet<string>();
            var overlapList = new List<Transform>();

            var tlist = owner.GetComponentsInChildren<Transform>();
            var root = owner.transform;

            foreach (var t in tlist)
            {
                if (t == root)
                    continue;
                if (boneHash.Contains(t.name))
                {
                    overlapList.Add(t);
                }
                else
                {
                    boneHash.Add(t.name);
                }
            }

            return overlapList;
        }

        //=========================================================================================
        /// <summary>
        /// アバターパーツの追加
        /// </summary>
        /// <param name="parts"></param>
        public int AddAvatarParts(MagicaAvatarParts parts)
        {
            if (parts == null)
                return 0;

            //Debug.Log("AddAvatarParts:" + parts.name);

            // すでに着せ替え済みならば何もしない
            if (parts.HasParent)
                return parts.PartsId;

            // アクティブ化する
            if (parts.gameObject.activeSelf == false)
                parts.gameObject.SetActive(true);

            // 初期化（すでに初期化済みならば何もしない）
            owner.Init();

            // スキンメッシュレンダラーリスト
            var skinRendererList = parts.GetComponentsInChildren<SkinnedMeshRenderer>();
            //Debug.Log("skinRendererList:" + skinRendererList.Length);

            // Magicaコンポーネントリスト
            //var magicaComponentList = parts.GetComponentsInChildren<CoreComponent>();
            var magicaComponentList = parts.GetMagicaComponentList();
            //Debug.Log("magicaComponentList:" + magicaComponentList.Length);

            // パーツを子として追加する
            var root = owner.transform;
            var croot = parts.transform;
            parts.transform.SetParent(root, false);
            parts.transform.localPosition = Vector3.zero;
            parts.transform.localRotation = Quaternion.identity;
            parts.ParentAvatar = owner;
            avatarPartsList.Add(parts);


            // 必要なボーンを移植する
            var partsBoneDict = parts.GetBoneDict();
            foreach (var bone in partsBoneDict.Values)
            {
                if (bone != croot)
                    AddBone(root, croot, bone);
            }

            // すべてのボーン参照数を加算する
            foreach (var bone in partsBoneDict.Values)
            {
                if (bone != croot)
                {
                    var t = boneDict[bone.name];
                    boneReferenceDict[t]++;
                    //Debug.Log("reference[" + t.name + "]:" + boneReferenceDict[t]);
                }
            }

            // ボーンの交換情報作成
            var boneReplaceDict = new Dictionary<Transform, Transform>();
            foreach (var bone in partsBoneDict.Values)
            {
                if (bone != croot)
                {
                    boneReplaceDict.Add(bone, boneDict[bone.name]);
                }
                else
                {
                    boneReplaceDict.Add(bone, root);
                }
            }

#if false
            foreach (var kv in avatar.Runtime.boneReplaceDict)
            {
                if (kv.Key != kv.Value)
                {
                    Debug.Log("置換[" + kv.Key.name + "]->[" + kv.Value.name + "]");
                }
            }
#endif

            // スキンメッシュレンダラー置換
            foreach (var skinRenderer in skinRendererList)
            {
                ReplaceSkinMeshRenderer(skinRenderer, boneReplaceDict);
            }

            // Magicaコンポーネント置換
            foreach (var comp in magicaComponentList)
            {
                ReplaceMagicaComponent(comp, boneReplaceDict);
            }

            // Magicaコンポーネントに本体のコライダーを追加する
            if (colliderList.Count > 0)
            {
                foreach (var comp in magicaComponentList)
                {
                    var cloth = comp as BaseCloth;
                    if (cloth && cloth.TeamData.MergeAvatarCollider)
                    {
                        // 初期化
                        cloth.Init();

                        foreach (var col in colliderList)
                        {
                            cloth.AddCollider(col);
                        }
                    }
                }
            }

            // パーツの機能は停止させる
            parts.gameObject.SetActive(false);

            // イベント
            owner.OnAttachParts.Invoke(owner, parts);

            return parts.PartsId;
        }

        /// <summary>
        /// 指定ボーンを親に追加する
        /// </summary>
        /// <param name="root"></param>
        /// <param name="croot"></param>
        /// <param name="bone"></param>
        private void AddBone(Transform root, Transform croot, Transform bone)
        {
            if (boneDict.ContainsKey(bone.name))
            {
                // すでに登録済み
                return;
            }

            // ボーンを追加する親ボーンを検索する
            Transform attachBone = root;
            Transform before = bone;
            Transform t = bone.parent;
            while (t && t != croot)
            {
                if (boneDict.ContainsKey(t.name))
                {
                    attachBone = boneDict[t.name];
                    break;
                }

                before = t;
                t = t.parent;
            }

            // ボーン追加
            before.SetParent(attachBone, false);
            //Debug.Log("Add attach:" + attachBone.name + " before:" + before.name);

            // before以下を辞書に登録する
            var blist = before.GetComponentsInChildren<Transform>();
            foreach (var b in blist)
            {
                if (boneDict.ContainsKey(b.name))
                {
                    // Duplication name!
                    Debug.LogWarning(string.Format("{0} [{1}]", Define.GetErrorMessage(Define.Error.AddOverlappingTransform), b.name));
                }
                else
                {
                    boneDict.Add(b.name, b);
                    boneReferenceDict.Add(b, 0); // まず参照数０で初期化
                }
            }
        }

        /// <summary>
        /// スキンメッシュレンダラーのボーン置換
        /// </summary>
        /// <param name="skinRenderer"></param>
        private void ReplaceSkinMeshRenderer(SkinnedMeshRenderer skinRenderer, Dictionary<Transform, Transform> boneReplaceDict)
        {
            // ルートボーン置換
            skinRenderer.rootBone = MeshUtility.GetReplaceBone(skinRenderer.rootBone, boneReplaceDict);

            // ボーン置換
            var bones = skinRenderer.bones;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i] = MeshUtility.GetReplaceBone(bones[i], boneReplaceDict);
            }
            skinRenderer.bones = bones;
        }

        /// <summary>
        /// Magicaコンポーネントの置換
        /// </summary>
        /// <param name="comp"></param>
        private void ReplaceMagicaComponent(CoreComponent comp, Dictionary<Transform, Transform> boneReplaceDict)
        {
            comp.ChangeAvatar(boneReplaceDict);
        }

        /// <summary>
        /// アバターパーツの削除
        /// </summary>
        /// <param name="parts"></param>
        public void RemoveAvatarParts(MagicaAvatarParts parts)
        {
            //Debug.Log("RemoveAvatarParts:" + parts.name);
            if (parts == null)
                return;
            if (avatarPartsList.Contains(parts) == false)
                return;

            // 接続を切る
            parts.ParentAvatar = null;
            avatarPartsList.Remove(parts);

            // 参照数を１つ減らし削除するボーンをリスト化する
            var removeBoneList = new List<Transform>();
            var croot = parts.transform;
            foreach (var bone in parts.GetBoneDict().Values)
            {
                if (bone == null)
                    continue;

                if (bone != croot)
                {
                    var t = boneDict[bone.name];
                    boneReferenceDict[t]--;
                    if (boneReferenceDict[t] == 0)
                    {
                        boneReferenceDict.Remove(t);
                        boneDict.Remove(t.name);
                        removeBoneList.Add(t);
                    }
                    //Debug.Log("reference[" + t.name + "]:" + boneReferenceDict[t]);
                }
            }

            // ボーン削除
            foreach (var bone in removeBoneList)
            {
                if (bone)
                {
                    GameObject.Destroy(bone.gameObject);
                }
            }

#if false
            foreach (var bone in boneDict.Values)
            {
                if (bone)
                    Debug.Log("残 bone:" + bone.name);
            }
            foreach (var kv in boneReferenceDict)
            {
                if (kv.Key)
                    Debug.Log("残 reference[" + kv.Key.name + "]:" + kv.Value);
            }
#endif

            // 本体コライダーを削除する
            if (colliderList.Count > 0)
            {
                // Magicaコンポーネントリスト
                var magicaComponentList = parts.GetMagicaComponentList();

                foreach (var comp in magicaComponentList)
                {
                    var cloth = comp as BaseCloth;
                    if (cloth)
                    {
                        foreach (var col in colliderList)
                        {
                            cloth.RemoveCollider(col);
                        }
                    }
                }
            }

            // パーツ削除
            GameObject.Destroy(parts.gameObject);

            // イベント
            owner.OnDetachParts.Invoke(owner);
        }

        /// <summary>
        /// アバターパーツの削除(パーツID)
        /// </summary>
        /// <param name="partsId"></param>
        public void RemoveAvatarParts(int partsId)
        {
            var parts = avatarPartsList.Find((p) => p.PartsId == partsId);
            RemoveAvatarParts(parts);
        }
    }
}
