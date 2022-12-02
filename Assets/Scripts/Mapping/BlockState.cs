using System.Collections.Generic;

namespace MinecraftClient.Mapping
{
    public class BlockState
    {
        public static readonly ResourceLocation AIR_ID      = new ResourceLocation("air");
        public static readonly ResourceLocation CAVE_AIR_ID = new ResourceLocation("cave_air");
        public static readonly ResourceLocation VOID_AIR_ID = new ResourceLocation("void_air");

        public static readonly BlockState AIR_STATE = new BlockState(new ResourceLocation("air"));

        public readonly ResourceLocation BlockId; // Something like 'minecraft:grass_block'
        public readonly Dictionary<string, string> Properties;

        public bool NoCollision = false;
        public bool NoOcclusion = false;
        public bool InWater = false;
        public bool InLava  = false;
        public bool InLiquid
        {
            get {
                return InWater || InLava;
            }
        }
        public bool LikeAir   = false;
        public bool FullSolid = true;

        public static BlockState FromString(string state)
        {
            var props = new Dictionary<string, string>();

            // Add namespace if absent...
            if (!state.Contains(':'))
                state = state.Insert(0, "minecraft:");
            
            // Assign block state property values
            if (state.Contains('['))
            {
                string[] parts = state.Split('[', 2);
                var blockId = ResourceLocation.fromString(parts[0]);
                string[] propStrs = parts[1].Substring(0, parts[1].Length - 1).Split(',');
                foreach (var prop in propStrs)
                {
                    string[] keyVal = prop.Split('=', 2);
                    props.Add(keyVal[0], keyVal[1]);
                }
                return new BlockState(blockId, props);
            }
            else
            {
                // Simple, only a block id
                return new BlockState(ResourceLocation.fromString(state), props);
            }

        }

        public BlockState(ResourceLocation blockId)
        {
            this.BlockId = blockId;
            this.Properties = new Dictionary<string, string>();
        }

        public BlockState(ResourceLocation blockId, Dictionary<string, string> props)
        {
            this.BlockId = blockId;
            this.Properties = props;
        }

        public override string ToString()
        {
            if (Properties.Count > 0)
            {
                var prop = Properties.GetEnumerator();
                prop.MoveNext();
                string propsText = $"{prop.Current.Key}={prop.Current.Value}";

                while (prop.MoveNext())
                    propsText += $",{prop.Current.Key}={prop.Current.Value}";

                return $"{BlockId}[{propsText}]";
            }
            else return BlockId.ToString();
        }

    }
}

