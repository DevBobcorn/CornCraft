using UnityEngine;

namespace CraftSharp.Control
{
    public class PlayerBaseSkill : ScriptableObject
    {
        [SerializeField] [Range(-5F, -0.1F)] public float IdleTimeout = -0.9F;

    }
}