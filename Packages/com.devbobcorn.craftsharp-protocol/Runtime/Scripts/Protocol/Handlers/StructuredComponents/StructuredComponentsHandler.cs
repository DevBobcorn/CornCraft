using System;
using System.Collections.Generic;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Handlers.StructuredComponents.Registries;
using CraftSharp.Protocol.Handlers.StructuredComponents.Registries.Subcomponents;

namespace CraftSharp.Protocol.Handlers.StructuredComponents
{
    public class StructuredComponentsHandler
    {
        private StructuredComponentRegistry ComponentRegistry { get; }
        
        public StructuredComponentsHandler(
            int protocolVersion,
            ItemPalette itemPalette)
        {
            // Get the appropriate subcomponent registry type based on the protocol version and then instantiate it
            var subcomponentRegistryType = protocolVersion switch
            {
                ProtocolMinecraft.MC_1_20_6_Version => typeof(SubComponentRegistry1206),
                ProtocolMinecraft.MC_1_21_Version => typeof(SubComponentRegistry121),
                _ => throw new NotSupportedException($"Protocol version {protocolVersion} is not supported for subcomponent registries!")
            };

            var subcomponentRegistry = Activator.CreateInstance(subcomponentRegistryType) as SubComponentRegistry 
                                ?? throw new InvalidOperationException($"Failed to instantiate a component registry for type {nameof(subcomponentRegistryType)}");
            
            // Get the appropriate component registry type based on the protocol version and then instantiate it
            var registryType = protocolVersion switch
            {
                ProtocolMinecraft.MC_1_20_6_Version => typeof(StructuredComponentsRegistry1206),
                ProtocolMinecraft.MC_1_21_Version => typeof(StructuredComponentsRegistry121),
                _ => throw new NotSupportedException($"Protocol version {protocolVersion} is not supported for structured component registries!")
            };

            ComponentRegistry = Activator.CreateInstance(registryType, itemPalette, subcomponentRegistry) as StructuredComponentRegistry 
                                ?? throw new InvalidOperationException($"Failed to instantiate a component registry for type {nameof(registryType)}");
        }

        public StructuredComponent Parse(int componentId, Queue<byte> data)
        {
            return ComponentRegistry.ParseComponent(componentId, data);
        }
    }
}