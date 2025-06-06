using System.Collections.Generic;

namespace CraftSharp
{
    /// <summary>
    /// 1.20.5 - 1.21+
    /// </summary>
    public class EntityMetadataPalette1205 : EntityMetadataPalette
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
            { 14, EntityMetadataType.BlockId },
            { 15, EntityMetadataType.OptionalBlockId },
            { 16, EntityMetadataType.Nbt },
            { 17, EntityMetadataType.Particle },
            { 18, EntityMetadataType.Particles },
            { 19, EntityMetadataType.VillagerData },
            { 20, EntityMetadataType.OptionalVarInt },
            { 21, EntityMetadataType.Pose },
            { 22, EntityMetadataType.CatVariant },
            { 23, EntityMetadataType.WolfVariant },
            { 24, EntityMetadataType.FrogVariant },
            { 25, EntityMetadataType.OptionalGlobalPosition },
            { 26, EntityMetadataType.PaintingVariant },
            { 27, EntityMetadataType.SnifferState },
            { 28, EntityMetadataType.ArmadilloState },
            { 29, EntityMetadataType.Vector3 },
            { 30, EntityMetadataType.Quaternion }
        };
            
        public override Dictionary<int, EntityMetadataType> GetEntityMetadataMappingsList()
        {
            return entityMetadataMappings;
        }
    }
}