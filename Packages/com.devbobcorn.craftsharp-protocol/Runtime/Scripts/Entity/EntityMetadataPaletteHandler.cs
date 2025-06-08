using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp
{
    public static class EntityMetadataPaletteHandler
    {
        public static EntityMetadataPalette GetPalette(int protocolVersion)
        {
            return protocolVersion switch
            {
                <= ProtocolMinecraft.MC_1_19_2_Version => new EntityMetadataPalette1191(),  // 1.13 - 1.19.2
                <= ProtocolMinecraft.MC_1_19_3_Version => new EntityMetadataPalette1193(),  // 1.19.3
                <= ProtocolMinecraft.MC_1_20_4_Version => new EntityMetadataPalette1194(),  // 1.19.4 - 1.20.4
                <= ProtocolMinecraft.MC_1_21_4_Version => new EntityMetadataPalette1205(),  // 1.20.5 - 1.21.4
                <= ProtocolMinecraft.MC_1_21_5_Version => new EntityMetadataPalette1215(),  // 1.21.5

                _ => throw new NotImplementedException()
            };
        }
    }
}