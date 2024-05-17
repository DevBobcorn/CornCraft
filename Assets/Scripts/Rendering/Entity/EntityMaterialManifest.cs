#nullable enable
using System;
using UnityEngine;

namespace CraftSharp.Rendering
{
    [Serializable]
    public class EntityMaterialGroup
    {
        [SerializeField] public string GroupName = string.Empty;
        [SerializeField] public EntityMaterialEntry[] Materials = { };
    }

    [Serializable]
    public class EntityMaterialEntry
    {
        [SerializeField] public EntityRenderType RenderType = EntityRenderType.SOLID;
        [SerializeField] public string TextureId = string.Empty;
    }

    [CreateAssetMenu(menuName = "CornCraft/Entity Material Manifest", order = 1000)]
    public class EntityMaterialManifest : ScriptableObject
    {
        [SerializeField] public EntityMaterialGroup[] Materials = { };
    }
}