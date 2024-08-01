using System.Collections.Generic;
using Unity.Mathematics;

namespace CraftSharp.Rendering
{
    public class LightCalculator
    {
        public const int LIGHT_CALC_BOX_SIZE = 3 << 4;

        public byte[,,] RecalculateLightValues(World world, int3 chunkPos)
        {
            var lightBlockage = new byte[LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE];
            var updatedValues = new byte[LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE];

            // Read cached block emission and bloackage data for updating
            world.GetLightDataCacheForChunk(chunkPos.x, chunkPos.y, chunkPos.z, false, lightBlockage);
            world.GetLightDataCacheForChunk(chunkPos.x, chunkPos.y, chunkPos.z, true,  updatedValues);

            // Create a update queue
            var updateQueue = new Queue<int3>();

            // Find all light sources
            for (int x = 0; x < LIGHT_CALC_BOX_SIZE; x++)
                for (int y = 0; y < LIGHT_CALC_BOX_SIZE; y++)
                    for (int z = 0; z < LIGHT_CALC_BOX_SIZE; z++)
                    {
                        if (updatedValues[x, y, z] > 1) // Emission > 1 (which means they can spread to neighbor cells)
                        {
                            updateQueue.Enqueue(new(x, y, z));
                        }
                    }
            
            // Define update function
            void tryUpdate(int x, int y, int z, int value)
            {
                // Convert to int during calculation in case of negative values
                byte v = (byte) math.max(0, value - lightBlockage[x, y, z]);

                if (updatedValues[x, y, z] < v) // If this cell needs updating
                {
                    updatedValues[x, y, z] = v;

                    if (v > 1) // Emission > 1 (which means they can spread to neighbor cells)
                    {
                        updateQueue.Enqueue(new(x, y, z));
                    }
                }
            }
            
            // Update!
            while (updateQueue.Count > 0)
            {
                var pos = updateQueue.Dequeue();
                // Decrease by 1 when spreading to neighbors
                var maxValForUpdate = updatedValues[pos.x, pos.y, pos.z] - 1;

                if (pos.x > 0)
                    tryUpdate(pos.x - 1, pos.y, pos.z, maxValForUpdate);
                
                if (pos.x < LIGHT_CALC_BOX_SIZE - 1)
                    tryUpdate(pos.x + 1, pos.y, pos.z, maxValForUpdate);
                
                if (pos.y > 0)
                    tryUpdate(pos.x, pos.y - 1, pos.z, maxValForUpdate);
                
                if (pos.y < LIGHT_CALC_BOX_SIZE - 1)
                    tryUpdate(pos.x, pos.y + 1, pos.z, maxValForUpdate);
                
                if (pos.z > 0)
                    tryUpdate(pos.x, pos.y, pos.z - 1, maxValForUpdate);
                
                if (pos.z < LIGHT_CALC_BOX_SIZE - 1)
                    tryUpdate(pos.x, pos.y, pos.z + 1, maxValForUpdate);
            }

            return updatedValues;
        }
    }
}