using System.Collections.Generic;

namespace CraftSharp
{
    /// <summary>
    /// For 1.19.3
    /// </summary>
    public class EntityMetadataPalette1193 : EntityMetadataPalette
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
            { 9, EntityMetadataType.Rotation },
            { 10, EntityMetadataType.Position },
            { 11, EntityMetadataType.OptionalPosition },
            { 12, EntityMetadataType.Direction },
            { 13, EntityMetadataType.OptionalUUID },
            { 14, EntityMetadataType.OptionalBlockId },
            { 15, EntityMetadataType.Nbt },
            { 16, EntityMetadataType.Particle },
            { 17, EntityMetadataType.VillagerData },
            { 18, EntityMetadataType.OptionalVarInt },
            { 19, EntityMetadataType.Pose },
            { 20, EntityMetadataType.CatVariant },
            { 21, EntityMetadataType.FrogVariant },
            { 22, EntityMetadataType.OptionalGlobalPosition },
            { 23, EntityMetadataType.PaintingVariant }
        };
            
        public override Dictionary<int, EntityMetadataType> GetEntityMetadataMappingsList()
        {
            return entityMetadataMappings;
        }
    }
}