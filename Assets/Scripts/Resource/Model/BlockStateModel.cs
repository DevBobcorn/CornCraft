using System.Collections.Generic;

namespace MinecraftClient.Resource
{
    public class BlockStateModel
    {
        public readonly BlockGeometry[] Geometries;

        public BlockStateModel(List<BlockGeometry> geometries)
        {
            Geometries = geometries.ToArray();
        }

    }

}