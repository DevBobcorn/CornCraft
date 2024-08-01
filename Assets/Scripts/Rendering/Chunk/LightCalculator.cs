using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace CraftSharp.Rendering
{
    public class LightCalculator
    {
        public const int LIGHT_CALC_BOX_SIZE = 3 << 4;

        private static int dataTimeSum = 0, calcTimeSum = 0;
        public static int DataTimeSum => dataTimeSum;
        public static int CalcTimeSum => calcTimeSum;
        private static readonly Queue<int> dataTimeRecord = new(Enumerable.Repeat(0, 20));
        private static readonly Queue<int> calcTimeRecord = new(Enumerable.Repeat(0, 20));

        public static int GetAffectedNeighborMask(BlockLoc blockLoc)
        {
            int px = blockLoc.GetChunkBlockX();
            int py = blockLoc.GetChunkBlockY();
            int pz = blockLoc.GetChunkBlockZ();

            int result = 0;

            // Faces
            if (px >=  2) result |= (1 << 16);
            if (py >=  2) result |= (1 << 22);
            if (pz >=  2) result |= (1 << 14);
            if (px <= 13) result |= (1 << 10);
            if (py <= 13) result |= (1 <<  4);
            if (pz <= 13) result |= (1 << 12);

            // Edges
            if (px + py <= 12) result |= (1 <<  1); // Lower left
            if (px - py >=  3) result |= (1 <<  7); // Lower right
            if (py - px >=  3) result |= (1 << 19); // Upper left
            if (px + py >= 18) result |= (1 << 25); // Upper right

            if (pz + py <= 12) result |= (1 <<  3); // Lower left
            if (pz - py >=  3) result |= (1 <<  5); // Lower right
            if (py - pz >=  3) result |= (1 << 21); // Upper left
            if (pz + py >= 18) result |= (1 << 23); // Upper right

            if (px + pz <= 12) result |= (1 <<  9); // Lower left
            if (px - pz >=  3) result |= (1 << 15); // Lower right
            if (pz - px >=  3) result |= (1 << 11); // Upper left
            if (px + pz >= 18) result |= (1 << 17); // Upper right

            return result;
        }

        private readonly System.Diagnostics.Stopwatch sw = new();

        public byte[,,] RecalculateLightValues(World world, int3 chunkPos)
        {
            var boxMinX = (chunkPos.x - 1) << 4;
            var boxMinY = ((chunkPos.y - 1) << 4) + World.GetDimension().minY;
            var boxMinZ = (chunkPos.z - 1) << 4;

            var lightBlockage = new byte[LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE];
            var updatedValues = new byte[LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE];

            world.GetValuesFromSection(boxMinX, boxMinY, boxMinZ, LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE,
                    LIGHT_CALC_BOX_SIZE, bloc => bloc.State.LightBlockageLevel, lightBlockage);
            
            world.GetValuesFromSection(boxMinX, boxMinY, boxMinZ, LIGHT_CALC_BOX_SIZE, LIGHT_CALC_BOX_SIZE,
                    LIGHT_CALC_BOX_SIZE, bloc => bloc.State.LightEmissionLevel, updatedValues);
            
            sw.Restart();

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
            
            var time = (int) sw.ElapsedMilliseconds;

            lock (dataTimeRecord)
            {
                if (dataTimeRecord.TryDequeue(out int prev))
                {
                    dataTimeSum -= prev;
                    dataTimeRecord.Enqueue(time);
                    dataTimeSum += time;
                }
            }

            sw.Restart();
            
            // Define update function
            void tryUpdate(int x, int y, int z, int value)
            {
                // Convert to int during calculation in case of negative values
                byte v = (byte) math.max(0, value - (int) lightBlockage[x, y, z]);

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

            time = (int) sw.ElapsedMilliseconds;

            lock (calcTimeRecord)
            {
                if (calcTimeRecord.TryDequeue(out int prev))
                {
                    calcTimeSum -= prev;
                    calcTimeRecord.Enqueue(time);
                    calcTimeSum += time;
                }
            }

            return updatedValues;
        }
    }
}