using System;
using System.Threading;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.Rendering
{
    [RequireComponent(typeof (MeshFilter), typeof (MeshRenderer))]
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
        public int Priority = 0;
        public MeshCollider InteractionCollider;
        
        // Stores whether its neighbor chunks are present (when self is being built)...
        public bool ZNegDataPresent, ZPosDataPresent, XNegDataPresent, XPosDataPresent;
        public bool AllNeighborDataPresent
        {
            get {
                return ZNegDataPresent && ZPosDataPresent && XNegDataPresent && XPosDataPresent;
            }
        }

        public void UpdateCollider(Mesh colliderMesh)
        {
            if (InteractionCollider is null)
                InteractionCollider = gameObject.AddComponent<MeshCollider>();
            InteractionCollider.sharedMesh = colliderMesh;
        }

        public void ClearCollider()
        {
            InteractionCollider?.sharedMesh.Clear();
        }

        public int CompareTo(ChunkRender chunkRender)
        {
            return this.Priority - chunkRender.Priority;
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

        public void Unload()
        {
            //Debug.Log("Unloading Chunk " + ToString());
            TokenSource?.Cancel();
            Destroy(this.gameObject);
        }

        public override string ToString()
        {
            return $"[ChunkRender {ChunkX}, {ChunkY}, {ChunkZ}]";
        }

    }

}
