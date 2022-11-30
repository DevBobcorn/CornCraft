// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp


using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaAvatar API
    /// </summary>
    public partial class MagicaAvatar : CoreComponent
    {
        /// <summary>
        /// プレハブ状態のアバターパーツを取り付けます
        /// 取り付けるアバターパーツはプレハブからインスタンス化されます
        /// 取り付けたアバターパーツのIDを返します
        /// Attach avatar parts.
        /// Avatar parts to be attached are instantiated.
        //// Returns the attached avatar part ID.
        /// </summary>
        /// <param name="avatarPartsPrefab"></param>
        /// <param name="instanceAction">Action called after instantiation.</param>
        /// <returns></returns>
        public int AttachAvatarParts(GameObject avatarPartsPrefab, System.Action<GameObject> instanceAction = null)
        {
            var avatarPartsObject = Instantiate(avatarPartsPrefab);

            if (instanceAction != null)
                instanceAction(avatarPartsObject);

            return Runtime.AddAvatarParts(avatarPartsObject.GetComponent<MagicaAvatarParts>());
        }

        /// <summary>
        /// アバターパーツを取り外します
        /// 取り外されたアバターパーツは削除されます
        /// Remove avatar parts.
        /// Removed avatar parts will be deleted.
        /// </summary>
        /// <param name="partsId"></param>
        public void DetachAvatarParts(int partsId)
        {
            Runtime.RemoveAvatarParts(partsId);
        }

        /// <summary>
        /// アバターパーツを取り外します
        /// 取り外したアバターパーツは削除されます
        /// Remove avatar parts.
        /// Removed avatar parts will be deleted.
        /// </summary>
        /// <param name="avatarObject"></param>
        public void DetachAvatarParts(GameObject avatarPartsObject)
        {
            Runtime.RemoveAvatarParts(avatarPartsObject.GetComponent<MagicaAvatarParts>());
        }

        /// <summary>
        /// アバターパーツを取り外します
        /// 取り外したアバターパーツは削除されます
        /// Remove avatar parts.
        /// Removed avatar parts will be deleted.
        /// </summary>
        /// <param name="avatarObject"></param>
        public void DetachAvatarParts(MagicaAvatarParts parts)
        {
            Runtime.RemoveAvatarParts(parts);
        }
    }
}
