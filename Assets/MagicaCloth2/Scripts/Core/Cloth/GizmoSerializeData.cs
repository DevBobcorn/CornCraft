// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネントのギズモ表示用シリアライズデータ
    /// </summary>
    [System.Serializable]
    public class GizmoSerializeData
    {
        public bool always = false;

        // Cloth
        public ClothDebugSettings clothDebugSettings = new ClothDebugSettings();

        // Virtual Mesh. これはデバッグ用
#if MC2_DEBUG
        public VirtualMeshDebugSettings proxyDebugSettings = new VirtualMeshDebugSettings();
        public VirtualMeshDebugSettings mappingDebugSettings = new VirtualMeshDebugSettings();
        public int debugMappingIndex = 0;
#endif // MC2_DEBUG

        public GizmoSerializeData()
        {
            clothDebugSettings.enable = true;
            clothDebugSettings.shape = true;
        }

        public bool IsAlways()
        {
            return always;
        }
    }
}
