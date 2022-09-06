﻿using System;
using MinecraftClient.Protocol.Handlers.PacketPalettes;

namespace MinecraftClient.Protocol.Handlers
{
    public class PacketTypeHandler
    {
        private int protocol;
        private bool forgeEnabled = false;

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
            PacketTypePalette p;
            
            if (protocol > ProtocolMinecraft.MC_1_19_Version)
                throw new NotImplementedException(Translations.Get("exception.palette.packet"));
            
            if (protocol <= ProtocolMinecraft.MC_1_14_Version)
                p = new PacketPalette113();
            else if (protocol <= ProtocolMinecraft.MC_1_15_Version)
                p = new PacketPalette114();
            else if (protocol <= ProtocolMinecraft.MC_1_15_2_Version)
                p = new PacketPalette115();
            else if (protocol <= ProtocolMinecraft.MC_1_16_1_Version)
                p = new PacketPalette116();
            else if (protocol <= ProtocolMinecraft.MC_1_16_5_Version)
                p = new PacketPalette1162();
            else if (protocol <= ProtocolMinecraft.MC_1_17_1_Version)
                p = new PacketPalette117();
            else if (protocol <= ProtocolMinecraft.MC_1_18_2_Version)
                p = new PacketPalette118();
            else
                p = new PacketPalette119();

            p.SetForgeEnabled(this.forgeEnabled);
            return p;
        }
    }
}