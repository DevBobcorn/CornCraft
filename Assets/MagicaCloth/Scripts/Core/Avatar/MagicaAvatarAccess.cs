// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;

namespace MagicaCloth
{
    /// <summary>
    /// アバターの内容物参照
    /// </summary>
    public abstract class MagicaAvatarAccess : IDisposable
    {
        protected MagicaAvatar owner;

        protected MagicaAvatarRuntime Runtime
        {
            get
            {
                return owner.Runtime;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 親参照設定
        /// </summary>
        /// <param name="manager"></param>
        public void SetParent(MagicaAvatar avatar)
        {
            this.owner = avatar;
        }

        /// <summary>
        /// 初期設定
        /// </summary>
        public abstract void Create();

        /// <summary>
        /// 破棄
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 有効化
        /// </summary>
        public abstract void Active();

        /// <summary>
        /// 無効化
        /// </summary>
        public abstract void Inactive();
    }
}
