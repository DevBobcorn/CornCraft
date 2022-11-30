// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

namespace MagicaCloth
{
    /// <summary>
    /// コンポーネント種類
    /// メニューから一括ビルドする際の優先順位でもある
    /// </summary>
    public enum ComponentType
    {
        None,

        SphereCollider = 100,
        CapsuleCollider = 101,
        PlaneCollider = 102,

        DirectionalWind = 200,
        AreaWind = 201,

        RenderDeformer = 500,

        VirtualDeformer = 600,

        BoneCloth = 1000,
        BoneSpring = 1001,

        MeshCloth = 2000,
        MeshSpring = 2001,

        Avatar = 4000,
        AvatarParts = 4001,
    }
}
