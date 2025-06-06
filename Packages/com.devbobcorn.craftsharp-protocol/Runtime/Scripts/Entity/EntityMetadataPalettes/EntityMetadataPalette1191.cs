using System.Collections.Generic;

namespace CraftSharp
{
    /// <summary>
    /// 1.13 - 1.19.2
    /// </summary>
    public class EntityMetadataPalette1191 : EntityMetadataPalette
    {
        private readonly Dictionary<int, EntityMetadataType> entityMetadataMappings = new()
        {
            { 0, EntityMetadataType.Byte },
            { 1, EntityMetadataType.VarInt },
            { 2, EntityMetadataType.Float },
            { 3, EntityMetadataType.String },
            { 4, EntityMetadataType.Chat },
            { 5, EntityMetadataType.OptionalChat },
            { 6, EntityMetadataType.Slot },
            { 7, EntityMetadataType.Boolean },
            { 8, EntityMetadataType.Rotation },
            { 9, EntityMetadataType.Position },
            { 10, EntityMetadataType.OptionalPosition },
            { 11, EntityMetadataType.Direction },
            { 12, EntityMetadataType.OptionalUUID },
            { 13, EntityMetadataType.OptionalBlockId },
            { 14, EntityMetadataType.Nbt },
            { 15, EntityMetadataType.Particle },
            { 16, EntityMetadataType.VillagerData },
            { 17, EntityMetadataType.OptionalVarInt },
            { 18, EntityMetadataType.Pose },
            { 19, EntityMetadataType.CatVariant },
            { 20, EntityMetadataType.FrogVariant },
            { 21, EntityMetadataType.OptionalGlobalPosition },
            { 22, EntityMetadataType.PaintingVariant }
        };
            
        public override Dictionary<int, EntityMetadataType> GetEntityMetadataMappingsList()
        {
            return entityMetadataMappings;
        }
    }
}