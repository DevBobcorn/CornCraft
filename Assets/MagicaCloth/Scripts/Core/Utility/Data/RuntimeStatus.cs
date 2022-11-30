// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;

namespace MagicaCloth
{
    /// <summary>
    /// コンテンツの実行状態を管理する
    /// </summary>
    public class RuntimeStatus
    {
        // 以下は現在の状態フラグ
        /// <summary>
        /// 初期化処理が開始したかどうか
        /// </summary>
        bool initStart;

        /// <summary>
        /// 初期化が完了するとtrueになる（エラーの有無は問わない）
        /// </summary>
        bool init;

        /// <summary>
        /// 初期化エラーが発生するとtrueになる
        /// </summary>
        bool initError;

        /// <summary>
        /// コンテンツの有効状態の切り替え
        /// </summary>
        bool enable;

        /// <summary>
        /// ユーザー操作によるコンテンツの有効状態の切り替え(v1.2)
        /// </summary>
        bool userEnable = true;

        /// <summary>
        /// 実行中にエラーが発生した場合にtrueになる
        /// </summary>
        bool runtimeError;

        /// <summary>
        /// コンテンツが破棄された場合にtrueとなる
        /// </summary>
        bool dispose;

        /// <summary>
        /// コンテンツの現在の稼働状態
        /// </summary>
        bool isActive;

        /// <summary>
        /// コンテンツの内容に変更が発生した
        /// </summary>
        bool isDirty;

        /// <summary>
        /// 連動（親）ステータス
        /// 設定されている場合、こららのステータスがすべて停止中ならば自身も停止する
        /// </summary>
        internal HashSet<RuntimeStatus> parentStatusSet { get; private set; } = new HashSet<RuntimeStatus>();

        /// <summary>
        /// 連動（子）ステータス
        /// 設定されている場合、自身のアクティブ変更時に子のすべてのUpdateStatus()を呼び出す
        /// </summary>
        internal HashSet<RuntimeStatus> childStatusSet { get; private set; } = new HashSet<RuntimeStatus>();

        //=========================================================================================
        /// <summary>
        /// アクティブ変更時コールバック
        /// </summary>
        internal System.Action UpdateStatusAction;

        /// <summary>
        /// 連動がすべて切断された時のコールバッグ
        /// </summary>
        internal System.Action DisconnectedAction;

        /// <summary>
        /// この状態管理のオーナークラス
        /// </summary>
        internal System.Func<System.Object> OwnerFunc;

        //=========================================================================================
        /// <summary>
        /// 現在稼働中か判定する
        /// </summary>
        public bool IsActive
        {
            get
            {
                return isActive && !dispose;
            }
        }

        /// <summary>
        /// 初期化が開始されたか判定する
        /// </summary>
        /// <value></value>
        public bool IsInitStart
        {
            get
            {
                return initStart;
            }
        }

        /// <summary>
        /// 初期化済みか判定する（エラーの有無は問わない）
        /// </summary>
        public bool IsInitComplete
        {
            get
            {
                return init;
            }
        }

        /// <summary>
        /// 初期化が成功しているか判定する
        /// </summary>
        public bool IsInitSuccess
        {
            get
            {
                return init && !initError;
            }
        }

        /// <summary>
        /// 初期化が失敗しているか判定する
        /// </summary>
        public bool IsInitError
        {
            get
            {
                return init && initError;
            }
        }

        /// <summary>
        /// 破棄済みか判定する
        /// </summary>
        public bool IsDispose
        {
            get
            {
                return dispose;
            }
        }

        /// <summary>
        /// 内容に変更が発生したか判定する
        /// </summary>
        public bool IsDirty => isDirty;

        /// <summary>
        /// 初期化の開始フラグを立てる
        /// </summary>
        public void SetInitStart()
        {
            initStart = true;
        }

        /// <summary>
        /// 初期化済みフラグを立てる
        /// </summary>
        public void SetInitComplete()
        {
            init = true;
        }

        /// <summary>
        /// 初期化エラーフラグを立てる
        /// </summary>
        public void SetInitError()
        {
            initError = true;
        }

        /// <summary>
        /// 有効フラグを設定する
        /// </summary>
        /// <param name="sw"></param>
        /// <returns>フラグに変更があった場合はtrueが返る</returns>
        public bool SetEnable(bool sw)
        {
            bool ret = enable != sw;
            enable = sw;
            return ret;
        }

        /// <summary>
        /// ユーザー操作による有効フラグを設定する
        /// </summary>
        /// <param name="sw"></param>
        /// <returns>フラグに変更があった場合はtrueが返る</returns>
        public bool SetUserEnable(bool sw)
        {
            bool ret = userEnable != sw;
            userEnable = sw;
            return ret;
        }

        /// <summary>
        /// ランタイムエラーフラグを設定する
        /// </summary>
        /// <param name="sw"></param>
        /// <returns>フラグに変更があった場合はtrueが返る</returns>
        public bool SetRuntimeError(bool sw)
        {
            bool ret = runtimeError != sw;
            runtimeError = sw;
            return ret;
        }

        /// <summary>
        /// 破棄フラグを立てる
        /// </summary>
        /// <returns></returns>
        public void SetDispose()
        {
            dispose = true;
        }

        /// <summary>
        /// 変更フラグを立てる
        /// </summary>
        public void SetDirty()
        {
            isDirty = true;
        }

        /// <summary>
        /// 変更フラグをクリアする
        /// </summary>
        public void ClearDirty()
        {
            isDirty = false;
        }

        /// <summary>
        /// 現在のアクティブ状態を更新する
        /// </summary>
        /// <returns>アクティブ状態に変更があった場合はtrueが返る</returns>
        public bool UpdateStatus()
        {
            if (dispose)
                return false;

            // 初期化済み、有効状態、エラー状態、連動（親）ステータス状態、がすべてクリアならばアクティブ状態と判定
            var active = init && !initError && enable && userEnable && !runtimeError && IsParentStatusActive();

            // およびマネージャのアクティブ状態を判定
            if (MagicaPhysicsManager.IsInstance())
                active = active && MagicaPhysicsManager.Instance.IsActive;

            if (active != isActive)
            {
                isActive = active;

                // コールバック
                UpdateStatusAction?.Invoke();

                // すべての連動（子）ステータスの更新を呼び出す
                foreach (var status in childStatusSet)
                {
                    status?.UpdateStatus();
                }

                return true;
            }
            else
                return false;
        }

        //=========================================================================================
        /// <summary>
        /// 連動（親）ステータスを追加する
        /// </summary>
        /// <param name="status"></param>
        public void AddParentStatus(RuntimeStatus status)
        {
            parentStatusSet.Add(status);
        }

        /// <summary>
        /// 連動（親）ステータスを削除する
        /// </summary>
        /// <param name="status"></param>
        public void RemoveParentStatus(RuntimeStatus status)
        {
            parentStatusSet.Remove(status);
            parentStatusSet.Remove(null);

            // 連動切断アクション
            if (parentStatusSet.Count == 0 && childStatusSet.Count == 0)
                DisconnectedAction?.Invoke();
        }

        /// <summary>
        /// 連動（子）ステータスを追加する
        /// </summary>
        /// <param name="status"></param>
        public void AddChildStatus(RuntimeStatus status)
        {
            childStatusSet.Add(status);
        }

        /// <summary>
        /// 連動（子）ステータスを削除する
        /// </summary>
        /// <param name="status"></param>
        public void RemoveChildStatus(RuntimeStatus status)
        {
            childStatusSet.Remove(status);
            childStatusSet.Remove(null);

            // 連動切断アクション
            if (parentStatusSet.Count == 0 && childStatusSet.Count == 0)
                DisconnectedAction?.Invoke();
        }

        /// <summary>
        /// 親スタータスと連動する
        /// </summary>
        /// <param name="parent"></param>
        public void LinkParentStatus(RuntimeStatus parent)
        {
            AddParentStatus(parent);
            parent.AddChildStatus(this);
        }

        /// <summary>
        /// 親ステータスとの連動を解除する
        /// </summary>
        /// <param name="parent"></param>
        public void UnlinkParentStatus(RuntimeStatus parent)
        {
            RemoveParentStatus(parent);
            parent.RemoveChildStatus(this);
        }

        /// <summary>
        /// 連動（親）ステータスが１つでも稼働状態か調べる
        /// 連動ステータスが無い場合は稼働状態として返す
        /// </summary>
        /// <returns></returns>
        bool IsParentStatusActive()
        {
            if (parentStatusSet.Count == 0)
                return true;

            foreach (var status in parentStatusSet)
            {
                if (status != null && status.IsActive)
                    return true;
            }

            return false;
        }
    }
}
