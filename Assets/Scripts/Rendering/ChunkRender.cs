using System;
using System.Threading;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    public class ChunkRender : MonoBehaviour, IComparable<ChunkRender>
    {
        public static readonly RenderType[] TYPES = new RenderType[]
        {
            RenderType.SOLID,         RenderType.CUTOUT,
            RenderType.CUTOUT_MIPPED, RenderType.TRANSLUCENT
        };

        public static int TypeIndex(RenderType type)
        {
            return type switch
            {
                RenderType.SOLID         => 0,
                RenderType.CUTOUT        => 1,
                RenderType.CUTOUT_MIPPED => 2,
                RenderType.TRANSLUCENT   => 3,
                _                        => 0
            };
        }

        public int ChunkX, ChunkY, ChunkZ;
        public Chunk Chunk;
        public BuildState State = BuildState.None;

        public CancellationTokenSource TokenSource = null;
        private int priority = 0;
        // Each bit in the layer mask represents the presence of a render type(layer)
        private int layerMask = 0;
        public int LayerMask { get { return layerMask; } }
        public ChunkRenderLayer[] Layers = new ChunkRenderLayer[TYPES.Length];
        public MeshCollider ChunkCollider;
        
        // Stores whether its neighbor chunks are present (when self is being built)...
        public bool ZNegDataPresent, ZPosDataPresent, XNegDataPresent, XPosDataPresent;

        public Location GetGlobalLocation(int x, int y, int z)
        {
            return new Location(
                ChunkX * Chunk.SizeX + x,
                ChunkY * Chunk.SizeY + y,
                ChunkZ * Chunk.SizeZ + z
            );
        }

        public void UpdatePriority(Location viewLocation)
        {
            // Get this chunk's build priority based on its current distance to the player,
            // a smaller value means a higher priority...
            priority = (int)(GetGlobalLocation(7, 7, 7).DistanceTo(viewLocation) / 16);
        }

        public int CompareTo(ChunkRender chunkRender)
        {
            return this.priority - chunkRender.priority;
        }

        public void UpdateNeighborStatus()
        {
            var world = CornClient.Instance?.GetWorld();
            if (world is null) return;
            // Check if neighbor chunks' data is currently present...
            ZNegDataPresent = world.isChunkColumnReady(ChunkX, ChunkZ - 1); // ZNeg neighbor
            ZPosDataPresent = world.isChunkColumnReady(ChunkX, ChunkZ + 1); // ZPos neighbor
            XNegDataPresent = world.isChunkColumnReady(ChunkX - 1, ChunkZ); // XNeg neighbor
            XPosDataPresent = world.isChunkColumnReady(ChunkX + 1, ChunkZ); // XPos neighbor
        }

        public void UpdateLayers(int layerMask)
        {
            this.layerMask = layerMask;
            for (int i = 0;i < TYPES.Length;i++)
            {   // This render types presents in this chunk render
                bool hasLayer = (layerMask & (1 << i)) != 0;
                if (hasLayer && Layers[i] is null) // Create this layer
                {
                    Layers[i] = CreateChunkLayer(this, TYPES[i]);
                }
                else if (!hasLayer && Layers[i] is not null) // Destroy this layer
                {
                    // Destroy the game object, not just the component
                    Destroy(Layers[i].gameObject);
                    Layers[i] = null;
                }
            }
        }

        public void UpdateCollider(Mesh colliderMesh)
        {
            if (ChunkCollider is null)
                ChunkCollider = gameObject.AddComponent<MeshCollider>();
            ChunkCollider.sharedMesh = colliderMesh;
        }

        public void ClearCollider()
        {
            ChunkCollider?.sharedMesh.Clear();
        }

        public void Unload()
        {
            //Debug.Log("Unloading Chunk " + ToString());
            TokenSource?.Cancel();

            for (int i = 0;i < TYPES.Length;i++)
            {
                if (Layers[i] is not null) // Destroy this layer
                {
                    Destroy(Layers[i].gameObject);
                    Layers[i] = null;
                }
            }

            Destroy(this.gameObject);

        }

        private static ChunkRenderLayer CreateChunkLayer(ChunkRender chunk, RenderType type)
        {
            // Create new chunk layer...
            GameObject layerObj = new GameObject(type.ToString());
            layerObj.layer = UnityEngine.LayerMask.NameToLayer("Terrain");
            ChunkRenderLayer newLayer = layerObj.AddComponent<ChunkRenderLayer>();
            // Set its parent to this chunk...
            layerObj.transform.parent = chunk.transform;
            layerObj.transform.localPosition = Vector3.zero;
            return newLayer;
        }

        public override string ToString()
        {
            return "[ChunkRender " + ChunkX + ", " + ChunkY + ", " + ChunkZ + "]";
        }

    }

}
