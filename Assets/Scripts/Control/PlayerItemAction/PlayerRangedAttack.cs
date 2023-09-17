#nullable enable
using System;
using UnityEngine;

namespace CraftSharp.Control
{
    [CreateAssetMenu(menuName = "CornCraft/PlayerRangedAttack")]
    public class PlayerRangedAttack : ScriptableObject
    {
        [SerializeField] [Range(-5F, -0.1F)] public float IdleTimeout = -0.9F;
        [SerializeField] public AnimationClip? DrawBow;
    }
}