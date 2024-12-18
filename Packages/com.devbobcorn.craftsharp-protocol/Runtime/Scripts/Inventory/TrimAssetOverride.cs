namespace CraftSharp.Inventory
{
    public record TrimAssetOverride(int ArmorMaterialType, string AssetName)
    {
        public int ArmorMaterialType { get; } = ArmorMaterialType;
        public string AssetName { get; } = AssetName;
    }
}