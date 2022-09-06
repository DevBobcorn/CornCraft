using UnityEngine;
using Unity.Mathematics;

namespace MinecraftClient.Rendering
{
    public class ChunkStateGeometry
    {
        // Build without collider
        public static void Build(ref MeshBuffer buffer, int stateId, int x, int y, int z, int cullFlags)
        {
            var table = CornClient.Instance?.PackManager.finalTable;

            if (table is null || !table.ContainsKey(stateId))
            {
                if (table is not null) // Disconnected already if table is null
                    Debug.LogWarning($"Model for block state with id {stateId} is not available!");
                
                return;
            }

            var models = table[stateId].Geometries;
            var chosen = (x + y + z) % models.Count;
            var model = models[chosen];

            var data = model.GetDataForChunk(buffer.offset, new float3(z, y, x), cullFlags);

            buffer.vert = ArrayUtil.GetConcated(buffer.vert, data.Item1);
            buffer.face = ArrayUtil.GetConcated(buffer.face, data.Item4);
            buffer.uv = ArrayUtil.GetConcated(buffer.uv, data.Item2);

            buffer.offset += (uint)data.Item1.Length;

        }

    }
}