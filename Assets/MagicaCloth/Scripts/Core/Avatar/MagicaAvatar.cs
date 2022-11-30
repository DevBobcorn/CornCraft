// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// アバター管理コンポーネント
    /// </summary>
    [HelpURL("https://magicasoft.jp/avatar/")]
    [AddComponentMenu("MagicaCloth/MagicaAvatar")]
    public partial class MagicaAvatar : CoreComponent
    {
        /// <summary>
        /// データバージョン
        /// </summary>
        private const int DATA_VERSION = 1;

        /// <summary>
        /// データリセットフラグ
        /// ※このフラグが立つとエディタ拡張側で自動的にデータが作成される
        /// </summary>
        [SerializeField]
        private bool dataReset;

        /// <summary>
        /// ランタイム処理
        /// </summary>
        MagicaAvatarRuntime runtime = new MagicaAvatarRuntime();

        //=========================================================================================
        /// <summary>
        /// アバターパーツ接続イベント
        /// Avatar parts attach event.
        /// </summary>
        public AvatarPartsAttachEvent OnAttachParts = new AvatarPartsAttachEvent();

        /// <summary>
        /// アバターパーツ分離イベント
        /// Avatar parts detach event.
        /// </summary>
        public AvatarPartsDetachEvent OnDetachParts = new AvatarPartsDetachEvent();

        //=========================================================================================
        public override ComponentType GetComponentType()
        {
            return ComponentType.Avatar;
        }

        //=========================================================================================
        /// <summary>
        /// データを識別するハッシュコードを作成して返す
        /// </summary>
        /// <returns></returns>
        public override int GetDataHash()
        {
            int hash = 0;
            return hash;
        }

        //=========================================================================================
        public bool DataReset
        {
            set
            {
                dataReset = value;
            }
            get
            {
                return dataReset;
            }
        }

        public MagicaAvatarRuntime Runtime
        {
            get
            {
                runtime.SetParent(this);
                return runtime;
            }
        }

        //=========================================================================================
        void Reset()
        {
            // 自動データ作成フラグを立てる
            DataReset = true;
        }

        void OnValidate()
        {
        }

        protected override void OnInit()
        {
            Runtime.Create();
        }

        protected override void OnDispose()
        {
            Runtime.Dispose();
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnActive()
        {
            Runtime.Active();
        }

        protected override void OnInactive()
        {
            Runtime.Inactive();
        }

        //=========================================================================================
        public override int GetVersion()
        {
            return DATA_VERSION;
        }

        /// <summary>
        /// エラーとするデータバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public override int GetErrorVersion()
        {
            return 0;
        }

        /// <summary>
        /// データを検証して結果を格納する
        /// </summary>
        /// <returns></returns>
        public override void CreateVerifyData()
        {
            base.CreateVerifyData();
        }

        /// <summary>
        /// 現在のデータが正常（実行できる状態）か返す
        /// </summary>
        /// <returns></returns>
        public override Define.Error VerifyData()
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
                var olist = Runtime.CheckOverlappingTransform();
                if (olist.Count > 0)
                    return Define.Error.OverlappingTransform;

                return Define.Error.None;
            }
        }

        public override string GetInformation()
        {
            StaticStringBuilder.Clear();

            if (Application.isPlaying)
            {
                // 実行中
                if (Runtime.AvatarPartsCount > 0)
                {
                    StaticStringBuilder.Append("Connection avatar parts:");
                    int cnt = Runtime.AvatarPartsCount;
                    for (int i = 0; i < cnt; i++)
                    {
                        StaticStringBuilder.AppendLine();
                        StaticStringBuilder.Append("    [", Runtime.GetAvatarParts(i).name, "]");
                    }
                }
                else
                {
                    StaticStringBuilder.Append("No avatar parts connected.");
                }
            }
            else
            {
                // エディット中
                // 重複トランスフォームチェック
                var olist = Runtime.CheckOverlappingTransform();
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

                StaticStringBuilder.AppendLine();
                StaticStringBuilder.Append("Collider : ", Runtime.GetColliderCount());
            }

            return StaticStringBuilder.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// 共有データオブジェクト収集
        /// </summary>
        /// <returns></returns>
        public override List<ShareDataObject> GetAllShareDataObject()
        {
            var slist = base.GetAllShareDataObject();
            return slist;
        }

        /// <summary>
        /// sourceの共有データを複製して再セットする
        /// 再セットした共有データを返す
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public override ShareDataObject DuplicateShareDataObject(ShareDataObject source)
        {
            return null;
        }
    }
}
