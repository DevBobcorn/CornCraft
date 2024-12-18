#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp
{
    public abstract class EntityMetadataPalette
    {
        public abstract Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList();

        public EntityMetaDataType GetDataType(int typeId)
        {
            return GetEntityMetadataMappingsList()[typeId];
        }

        public static EntityMetadataPalette GetPalette(int protocolVersion)
        {
            return protocolVersion switch
            {
                <= ProtocolMinecraft.MC_1_19_2_Version => new EntityMetadataPalette1191(),  // 1.13 - 1.19.2
                <= ProtocolMinecraft.MC_1_19_3_Version => new EntityMetadataPalette1193(),  // 1.19.3
                <= ProtocolMinecraft.MC_1_20_4_Version => new EntityMetadataPalette1194(),  // 1.19.4 - 1.20.4
                <= ProtocolMinecraft.MC_1_21_Version   => new EntityMetadataPalette1205(),  // 1.20.5 - 1.21+

                _ => throw new NotImplementedException()
            };
        }

    }
}