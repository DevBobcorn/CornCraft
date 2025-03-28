using System.Collections.Generic;

namespace CraftSharp
{
    /// <summary>
    /// 1.20.5 - 1.21+
    /// </summary>
    public class EntityMetadataPalette1205 : EntityMetadataPalette
    {
        private readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings = new()
        {
            { 0, EntityMetaDataType.Byte },
            { 1, EntityMetaDataType.VarInt },
            { 2, EntityMetaDataType.VarLong },
            { 3, EntityMetaDataType.Float },
            { 4, EntityMetaDataType.String },
            { 5, EntityMetaDataType.Chat },
            { 6, EntityMetaDataType.OptionalChat },
            { 7, EntityMetaDataType.Slot },
            { 8, EntityMetaDataType.Boolean },
            { 9, EntityMetaDataType.Rotation },
            { 10, EntityMetaDataType.Position },
            { 11, EntityMetaDataType.OptionalPosition },
            { 12, EntityMetaDataType.Direction },
            { 13, EntityMetaDataType.OptionalUUID },
            { 14, EntityMetaDataType.BlockId },
            { 15, EntityMetaDataType.OptionalBlockId },
            { 16, EntityMetaDataType.Nbt },
            { 17, EntityMetaDataType.Particle },
            { 18, EntityMetaDataType.Particles },
            { 19, EntityMetaDataType.VillagerData },
            { 20, EntityMetaDataType.OptionalVarInt },
            { 21, EntityMetaDataType.Pose },
            { 22, EntityMetaDataType.CatVariant },
            { 23, EntityMetaDataType.WolfVariant },
            { 24, EntityMetaDataType.FrogVariant },
            { 25, EntityMetaDataType.OptionalGlobalPosition },
            { 26, EntityMetaDataType.PaintingVariant },
            { 27, EntityMetaDataType.SnifferState },
            { 28, EntityMetaDataType.ArmadilloState },
            { 29, EntityMetaDataType.Vector3 },
            { 30, EntityMetaDataType.Quaternion }
        };
            
        public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
        {
            return entityMetadataMappings;
        }
    }
}