// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

namespace MagicaCloth
{
    /// <summary>
    /// アバターパーツ接続イベント
    /// Avatar parts attach event.
    /// </summary>
    [System.Serializable]
    public class AvatarPartsAttachEvent : UnityEngine.Events.UnityEvent<MagicaAvatar, MagicaAvatarParts>
    {
    }

    /// <summary>
    /// アバターパーツ分離イベント
    /// Avatar parts detach event.
    /// </summary>
    [System.Serializable]
    public class AvatarPartsDetachEvent : UnityEngine.Events.UnityEvent<MagicaAvatar>
    {
    }

    /// <summary>
    /// マネージャ計算前イベント
    /// </summary>
    [System.Serializable]
    public class PhysicsManagerPreUpdateEvent : UnityEngine.Events.UnityEvent
    {
    }

    /// <summary>
    /// マネージャ計算後イベント
    /// </summary>
    [System.Serializable]
    public class PhysicsManagerPostUpdateEvent : UnityEngine.Events.UnityEvent
    {
    }
}
