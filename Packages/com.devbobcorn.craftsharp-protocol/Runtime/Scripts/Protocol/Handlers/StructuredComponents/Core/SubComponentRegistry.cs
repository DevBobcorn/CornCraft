using System;
using System.Collections.Generic;
using System.Reflection;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Core
{
    public abstract class SubComponentRegistry
    {
        private readonly Dictionary<ResourceLocation, Type> _subComponentParsers = new();

        private readonly DataTypes dataTypes;

        public SubComponentRegistry(DataTypes dataTypes)
        {
            this.dataTypes = dataTypes;
        }

        protected void RegisterSubComponent<T>(string name) where T : SubComponent
        {
            var id = ResourceLocation.FromString(name);
            
            if(_subComponentParsers.TryGetValue(id, out _)) 
                throw new Exception($"Sub component {name} already registered!");

            _subComponentParsers.Add(id, typeof (T));
        }

        public SubComponent ParseSubComponent(string name, Queue<byte> data)
        {
            var id = ResourceLocation.FromString(name);
            
            if(!_subComponentParsers.TryGetValue(id, out var subComponentParserType)) 
                throw new Exception($"Sub component {name} not registered!");

            var instance=  Activator.CreateInstance(subComponentParserType, dataTypes, this) as SubComponent ??
                throw new InvalidOperationException($"Could not create instance of a sub component parser type: {subComponentParserType.Name}");
            
            var parseMethod = instance.GetType().GetMethod("Parse", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException($"Sub component parser type {subComponentParserType.Name} does not have a Parse method.");
            parseMethod.Invoke(instance, new object[] { data });
            return instance;
        }
    }
}