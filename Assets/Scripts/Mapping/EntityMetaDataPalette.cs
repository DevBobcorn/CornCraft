#nullable enable
using MinecraftClient.Protocol.Handlers;
using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping
{
    public class EntityMetadataPalette
    {
        protected readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings;

        private EntityMetadataPalette(Dictionary<int, EntityMetaDataType> mappings)
        {
            entityMetadataMappings = mappings;
        }

        public Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
        {
            return entityMetadataMappings;
        }

        public EntityMetaDataType GetDataType(int typeId)
        {
            return GetEntityMetadataMappingsList()[typeId];
        }

        public static EntityMetadataPalette GetPalette(int protocolVersion)
        {
            return protocolVersion switch
            {
                <= ProtocolMinecraft.MC_1_19_2_Version => new EntityMetadataPalette(new()
                {
                    { 0, EntityMetaDataType.Byte },
                    { 1, EntityMetaDataType.VarInt },
                    { 2, EntityMetaDataType.Float },
                    { 3, EntityMetaDataType.String },
                    { 4, EntityMetaDataType.Chat },
                    { 5, EntityMetaDataType.OptionalChat },
                    { 6, EntityMetaDataType.Slot },
                    { 7, EntityMetaDataType.Boolean },
                    { 8, EntityMetaDataType.Rotation },
                    { 9, EntityMetaDataType.Position },
                    { 10, EntityMetaDataType.OptionalPosition },
                    { 11, EntityMetaDataType.Direction },
                    { 12, EntityMetaDataType.OptionalUuid },
                    { 13, EntityMetaDataType.OptionalBlockId },
                    { 14, EntityMetaDataType.Nbt },
                    { 15, EntityMetaDataType.Particle },
                    { 16, EntityMetaDataType.VillagerData },
                    { 17, EntityMetaDataType.OptionalVarInt },
                    { 18, EntityMetaDataType.Pose },
                    { 19, EntityMetaDataType.CatVariant },
                    { 20, EntityMetaDataType.FrogVariant },
                    { 21, EntityMetaDataType.OptionalGlobalPosition },
                    { 22, EntityMetaDataType.PaintingVariant }
                }),  // 1.13 - 1.19.2
                <= ProtocolMinecraft.MC_1_19_3_Version => new EntityMetadataPalette(new()
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
                    { 13, EntityMetaDataType.OptionalUuid },
                    { 14, EntityMetaDataType.OptionalBlockId },
                    { 15, EntityMetaDataType.Nbt },
                    { 16, EntityMetaDataType.Particle },
                    { 17, EntityMetaDataType.VillagerData },
                    { 18, EntityMetaDataType.OptionalVarInt },
                    { 19, EntityMetaDataType.Pose },
                    { 20, EntityMetaDataType.CatVariant },
                    { 21, EntityMetaDataType.FrogVariant },
                    { 22, EntityMetaDataType.OptionalGlobalPosition },
                    { 23, EntityMetaDataType.PaintingVariant }
                }),  // 1.19.3
                <= ProtocolMinecraft.MC_1_20_Version   => new EntityMetadataPalette(new()
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
                    { 13, EntityMetaDataType.OptionalUuid },
                    { 14, EntityMetaDataType.BlockId },
                    { 15, EntityMetaDataType.OptionalBlockId },
                    { 16, EntityMetaDataType.Nbt },
                    { 17, EntityMetaDataType.Particle },
                    { 18, EntityMetaDataType.VillagerData },
                    { 19, EntityMetaDataType.OptionalVarInt },
                    { 20, EntityMetaDataType.Pose },
                    { 21, EntityMetaDataType.CatVariant },
                    { 22, EntityMetaDataType.FrogVariant },
                    { 23, EntityMetaDataType.OptionalGlobalPosition },
                    { 24, EntityMetaDataType.PaintingVariant },
                    { 25, EntityMetaDataType.SnifferState },
                    { 26, EntityMetaDataType.Vector3 },
                    { 27, EntityMetaDataType.Quaternion }
                }),  // 1.19.4 - 1.20 +
                _ => throw new NotImplementedException()
            };
        }
    }
}