using System.Collections.Generic;

namespace CraftSharp
{
    /// <summary>
    /// 1.19.4 - 1.20.4
    /// </summary>
    public class EntityMetadataPalette1194 : EntityMetadataPalette
    {
        private readonly Dictionary<int, EntityMetadataType> entityMetadataMappings = new()
        {
            { 0, EntityMetadataType.Byte },
            { 1, EntityMetadataType.VarInt },
            { 2, EntityMetadataType.VarLong },
            { 3, EntityMetadataType.Float },
            { 4, EntityMetadataType.String },
            { 5, EntityMetadataType.Chat },
            { 6, EntityMetadataType.OptionalChat },
            { 7, EntityMetadataType.Slot },
            { 8, EntityMetadataType.Boolean },
            { 9, EntityMetadataType.Rotations },
            { 10, EntityMetadataType.Position },
            { 11, EntityMetadataType.OptionalPosition },
            { 12, EntityMetadataType.Direction },
            { 13, EntityMetadataType.OptionalUUID },
            { 14, EntityMetadataType.BlockState },
            { 15, EntityMetadataType.OptionalBlockState },
            { 16, EntityMetadataType.Nbt },
            { 17, EntityMetadataType.Particle },
            { 18, EntityMetadataType.VillagerData },
            { 19, EntityMetadataType.OptionalVarInt },
            { 20, EntityMetadataType.Pose },
            { 21, EntityMetadataType.CatVariant },
            { 22, EntityMetadataType.FrogVariant },
            { 23, EntityMetadataType.OptionalGlobalPosition },
            { 24, EntityMetadataType.PaintingVariant },
            { 25, EntityMetadataType.SnifferState },
            { 26, EntityMetadataType.Vector3 },
            { 27, EntityMetadataType.Quaternion },
        };
            
        public override Dictionary<int, EntityMetadataType> GetEntityMetadataMappingsList()
        {
            return entityMetadataMappings;
        }
    }
}