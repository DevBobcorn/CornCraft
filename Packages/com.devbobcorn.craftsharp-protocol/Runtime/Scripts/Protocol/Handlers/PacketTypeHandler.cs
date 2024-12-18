using System;
using CraftSharp.Protocol.Handlers.PacketPalettes;

namespace CraftSharp.Protocol.Handlers
{
    public class PacketTypeHandler
    {
        private readonly int protocol;
        private readonly bool forgeEnabled = false;

        /// <summary>
        /// Initialize the handler
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        public PacketTypeHandler(int protocol)
        {
            this.protocol = protocol;
        }
        /// <summary>
        /// Initialize the handler
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        /// <param name="forgeEnabled">Is forge enabled or not</param>
        public PacketTypeHandler(int protocol, bool forgeEnabled)
        {
            this.protocol = protocol;
            this.forgeEnabled = forgeEnabled;
        }
        /// <summary>
        /// Initialize the handler
        /// </summary>
        public PacketTypeHandler() { }

        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler()
        {
            return GetTypeHandler(this.protocol);
        }
        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler(int protocol)
        {
            PacketTypePalette p = protocol switch
            {
                > ProtocolMinecraft.MC_1_21_Version => throw new NotImplementedException(Translations.Get("exception.palette.packet")),

                <= ProtocolMinecraft.MC_1_16_1_Version => new PacketPalette116(),
                <= ProtocolMinecraft.MC_1_16_5_Version => new PacketPalette1162(),
                <= ProtocolMinecraft.MC_1_17_1_Version => new PacketPalette117(),
                <= ProtocolMinecraft.MC_1_18_2_Version => new PacketPalette118(),
                <= ProtocolMinecraft.MC_1_19_Version   => new PacketPalette119(),
                <= ProtocolMinecraft.MC_1_19_2_Version => new PacketPalette1192(),
                <= ProtocolMinecraft.MC_1_19_3_Version => new PacketPalette1193(),
                <= ProtocolMinecraft.MC_1_19_4_Version => new PacketPalette1194(),
                <= ProtocolMinecraft.MC_1_20_Version   => new PacketPalette1194(),
                <= ProtocolMinecraft.MC_1_20_2_Version => new PacketPalette1202(),
                <= ProtocolMinecraft.MC_1_20_4_Version => new PacketPalette1204(),
                <= ProtocolMinecraft.MC_1_20_6_Version => new PacketPalette1206(),
                
                _                                      => new PacketPalette121()
            };

            p.SetForgeEnabled(this.forgeEnabled);
            return p;
        }
    }
}
