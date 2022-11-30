// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// アバターパーツコンポーネント
    /// </summary>
    [HelpURL("https://magicasoft.jp/avatar-parts/")]
    [AddComponentMenu("MagicaCloth/MagicaAvatarParts")]
    public class MagicaAvatarParts : BaseComponent, IDataVerify
    {
        //=============================================================================================
        /// <summary>
        /// 親アバター
        /// </summary>
        private MagicaAvatar parentAvatar = null;

        /// <summary>
        /// このアバターパーツが保持するボーン辞書
        /// </summary>
        private Dictionary<string, Transform> boneDict = new Dictionary<string, Transform>();

        /// <summary>
        /// このアバターパーツが保持するMagicaコンポーネントのリスト
        /// </summary>
        private List<CoreComponent> magicaComponentList = null;

        //=========================================================================================
        public override ComponentType GetComponentType()
        {
            return ComponentType.AvatarParts;
        }

        //=============================================================================================
        public MagicaAvatar ParentAvatar
        {
            get
            {
                return parentAvatar;
            }
            set
            {
                parentAvatar = value;
            }
        }

        public bool HasParent
        {
            get
            {
                return parentAvatar != null;
            }
        }

        public int PartsId
        {
            get
            {
                return GetInstanceID();
            }
        }

        //=============================================================================================
        private void OnDestroy()
        {
            Dispose();
        }

        //=============================================================================================
        /// <summary>
        /// 破棄
        /// </summary>
        public void Dispose()
        {
            // 親から削除する
            if (parentAvatar != null)
            {
                parentAvatar.DetachAvatarParts(gameObject);
                parentAvatar = null;
            }
        }

        //=============================================================================================
        /// <summary>
        /// ゲームオブジェクト名が重複するトランスフォームのリストを返す
        /// </summary>
        /// <returns></returns>
        public List<Transform> CheckOverlappingTransform()
        {
            var boneHash = new HashSet<string>();
            var overlapList = new List<Transform>();

            var tlist = GetComponentsInChildren<Transform>();
            var root = transform;

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

        /// <summary>
        /// すべてのボーンを辞書に登録して返す
        /// この時にボーン名に重複があると着せ替えのときに問題を起こす可能性がある
        /// </summary>
        public Dictionary<string, Transform> GetBoneDict()
        {
            if (boneDict.Count > 0)
                return boneDict;

            boneDict.Clear();
            var tlist = GetComponentsInChildren<Transform>();

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
                }
            }

            //Debug.Log("boneDict:" + boneDict.Count);
            return boneDict;
        }

        /// <summary>
        /// このアバターパーツが保持するMagicaコンポーネントのリストを返す
        /// </summary>
        /// <returns></returns>
        public List<CoreComponent> GetMagicaComponentList()
        {
            if (magicaComponentList != null)
                return magicaComponentList;

            magicaComponentList = new List<CoreComponent>(GetComponentsInChildren<CoreComponent>());

            return magicaComponentList;
        }

        //=============================================================================================
        public int GetVersion()
        {
            return 1;
        }

        public void CreateVerifyData()
        {
            throw new System.NotImplementedException();
        }

        public Define.Error VerifyData()
        {
            if (Application.isPlaying)
            {
                // 実行中
                return Define.Error.None;
            }
            else
            {
                // エディット中
                // 重複トランスフォームチェック
                var olist = CheckOverlappingTransform();
                if (olist.Count > 0)
                    return Define.Error.OverlappingTransform;

                return Define.Error.None;
            }
        }

        public string GetInformation()
        {
            StaticStringBuilder.Clear();

            if (Application.isPlaying)
            {
                // 実行中
                if (ParentAvatar)
                {
                    StaticStringBuilder.Append("Connection parent avatar:");
                    StaticStringBuilder.AppendLine();
                    StaticStringBuilder.Append("    [", ParentAvatar.name, "]");
                }
                else
                {
                    StaticStringBuilder.Append("No connection.");
                }
            }
            else
            {
                // エディット中
                // 重複トランスフォームチェック
                var olist = CheckOverlappingTransform();
                if (olist.Count > 0)
                {
                    StaticStringBuilder.Append("There are duplicate game object names.");
                    foreach (var t in olist)
                    {
                        StaticStringBuilder.AppendLine();
                        StaticStringBuilder.Append("* ", t.name);
                    }
                }
                else
                {
                    StaticStringBuilder.Append("No problem.");
                }
            }

            return StaticStringBuilder.ToString();
        }
    }
}
