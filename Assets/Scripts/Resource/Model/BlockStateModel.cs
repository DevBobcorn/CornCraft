using System.Collections.Generic;

namespace MinecraftClient.Resource
{
    public class BlockStateModel
    {
        public readonly List<BlockGeometry> Geometries = new List<BlockGeometry>();

        public BlockStateModel(List<BlockGeometry> geometries)
        {
            Geometries = geometries;
        }

    }

}