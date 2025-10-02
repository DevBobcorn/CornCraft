using System;
using CraftSharp.Protocol.Handlers.PacketPalettes;

namespace CraftSharp.Protocol.Handlers
{
    public class PacketTypeHandler
    {
        private readonly int protocol;
        
        /// <summary>
        /// Initialize the handler
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        public PacketTypeHandler(int protocol)
        {
            this.protocol = protocol;
        }

        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler()
        {
            return GetTypeHandler(protocol);
        }
        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <param name="protocolVersion">Protocol version to use</param>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler(int protocolVersion)
        {
            PacketTypePalette p = protocolVersion switch
            {
                > ProtocolMinecraft.MC_1_21_3_Version => throw new NotImplementedException(Translations.Get("exception.palette.packet")),

                <= ProtocolMinecraft.MC_1_16_5_Version => new PacketPalette1165(),
                <= ProtocolMinecraft.MC_1_17_1_Version => new PacketPalette1171(),
                <= ProtocolMinecraft.MC_1_18_2_Version => new PacketPalette1182(),
                <= ProtocolMinecraft.MC_1_19_1_Version => new PacketPalette1191(),
                <= ProtocolMinecraft.MC_1_19_2_Version => new PacketPalette1192(),
                <= ProtocolMinecraft.MC_1_19_3_Version => new PacketPalette1193(),
                <= ProtocolMinecraft.MC_1_19_4_Version => new PacketPalette1194(),
                <= ProtocolMinecraft.MC_1_20_1_Version => new PacketPalette1194(),
                <= ProtocolMinecraft.MC_1_20_2_Version => new PacketPalette1202(),
                <= ProtocolMinecraft.MC_1_20_4_Version => new PacketPalette1204(),
                <= ProtocolMinecraft.MC_1_20_6_Version => new PacketPalette1206(),
                <= ProtocolMinecraft.MC_1_21_1_Version => new PacketPalette1211(),
                
                _                                      => new PacketPalette1213()
            };

            return p;
        }
    }
}
