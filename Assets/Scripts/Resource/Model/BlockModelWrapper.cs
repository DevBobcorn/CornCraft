using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class BlockModelWrapper
    {
        public readonly JsonModel model;
        public readonly int2 zyRot;
        public readonly bool uvlock;

        public BlockModelWrapper(JsonModel model, int2 zyRot, bool uvlock)
        {
            this.model = model;
            this.zyRot = zyRot;
            this.uvlock = uvlock;
        }

        public static BlockModelWrapper FromJson(ResourcePackManager manager, Json.JSONData data)
        {
            if (data.Properties.ContainsKey("model"))
            {
                ResourceLocation modelIdentifier = ResourceLocation.fromString(data.Properties["model"].StringValue);
                // Check if the model can be found...
                if (manager.BlockModelTable.ContainsKey(modelIdentifier))
                {
                    int zr = 0, yr = 0;
                    bool uvlock = false;

                    if (data.Properties.ContainsKey("x")) // Block z rotation
                    {
                        zr = data.Properties["x"].StringValue switch
                        {
                            "90"  => 1,
                            "180" => 2,
                            "270" => 3,
                            _     => 0
                        };
                    }

                    if (data.Properties.ContainsKey("y")) // Block y rotation
                    {
                        yr = data.Properties["y"].StringValue switch
                        {
                            "90"  => 1,
                            "180" => 2,
                            "270" => 3,
                            _     => 0
                        };
                    }

                    if (data.Properties.ContainsKey("uvlock"))
                        bool.TryParse(data.Properties["uvlock"].StringValue, out uvlock);

                    return new BlockModelWrapper(manager.BlockModelTable[modelIdentifier], new int2(zr, yr), uvlock);
                }

            }

            Debug.LogWarning("Invalid block model wrapper!");
            return null;
        }
    }
}
