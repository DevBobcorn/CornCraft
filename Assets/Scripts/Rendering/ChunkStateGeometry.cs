using UnityEngine;

namespace MinecraftClient.Rendering
{
    public class ChunkStateGeometry
    {
        public static void Build(ref MeshBuffer buffer, int stateId, bool uv, int x, int y, int z, int cullFlags)
        {
            var table = CornClient.Instance?.PackManager.finalTable;

            if (table is null || !table.ContainsKey(stateId))
            {
                if (table is not null) // Disconnected already if table is null
                    Debug.LogWarning("Model for block state with id " + stateId + " is not available!");
                
                return;
            }

            var models = table[stateId].Geometries;
            //var chosen = (x + y + z) % models.Count;
            // TODO var model = models[chosen];
            var model = models[0];

            var data = model.GetDataForChunk(buffer.offset, new Vector3(z, y, x), cullFlags);

            //buffer.vert = buffer.vert.Concat(data.Item1).ToArray();
            //buffer.face = buffer.face.Concat(data.Item4).ToArray();
            //if (uv) buffer.uv = buffer.uv.Concat(data.Item2).ToArray();

            buffer.vert = ArrayUtil.GetConcated(buffer.vert, data.Item1);
            buffer.face = ArrayUtil.GetConcated(buffer.face, data.Item4);
            if (uv) buffer.uv = ArrayUtil.GetConcated(buffer.uv, data.Item2);

            buffer.offset += data.Item1.Length;

        }
    }
}