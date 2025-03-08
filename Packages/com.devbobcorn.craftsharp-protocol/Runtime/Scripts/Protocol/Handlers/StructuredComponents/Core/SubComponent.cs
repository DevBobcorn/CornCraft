using System.Collections.Generic;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Core
{
    public abstract class SubComponent
    {
        protected DataTypes DataTypes { get; private set; }
        protected SubComponentRegistry SubComponentRegistry { get; private set; }

        public SubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry)
        {
            DataTypes = dataTypes;
            SubComponentRegistry = subComponentRegistry;
        }

        protected abstract void Parse(Queue<byte> data);
        public abstract Queue<byte> Serialize();
    }
}