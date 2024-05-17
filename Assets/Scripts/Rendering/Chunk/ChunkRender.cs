using System;
using System.Threading;
using UnityEngine;

namespace CraftSharp.Rendering
{
    [RequireComponent(typeof (MeshFilter), typeof (MeshRenderer))]
    public class ChunkRender : MonoBehaviour, IComparable<ChunkRender>
    {
        public static readonly RenderType[] TYPES = new RenderType[]
        {
            RenderType.SOLID,         RenderType.CUTOUT,
            RenderType.CUTOUT_MIPPED, RenderType.TRANSLUCENT,
            RenderType.WATER
        };

        public static int TypeIndex(RenderType type) => type switch
        {
            RenderType.SOLID         => 0,
            RenderType.CUTOUT        => 1,
            RenderType.CUTOUT_MIPPED => 2,
            RenderType.TRANSLUCENT   => 3,
            RenderType.WATER         => 4,
            
            _                        => 0
        };

        public int ChunkX, ChunkY, ChunkZ;
        public ChunkBuildState State = ChunkBuildState.Pending;

        public CancellationTokenSource TokenSource = null;
        public int Priority = 0;
        public MeshCollider InteractionCollider;

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

        public int CompareTo(ChunkRender chunkRender) => Priority - chunkRender.Priority;

        public void Unload()
        {
            TokenSource?.Cancel();
            Destroy(this.gameObject);
        }

        public override string ToString() => $"[ChunkRender {ChunkX}, {ChunkY}, {ChunkZ}]";

    }

}
