using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

namespace MinecraftClient.Resource
{
    public class BlockGeometry
    {
        public const float MC_VERT_SCALE = 16F;
        public const float MC_UV_SCALE = 16F;

        public readonly Dictionary<CullDir, List<float3>> verticies = new();
        public readonly Dictionary<CullDir, List<float2>> uvs       = new();
        public readonly Dictionary<CullDir, List<int>> tintIndices  = new();
        public readonly Dictionary<CullDir, uint> vertIndexOffset   = new();

        public BlockGeometry()
        {
            // Initialize these collections...
            foreach (CullDir dir in Enum.GetValues(typeof (CullDir)))
            {
                verticies.Add(dir, new List<float3>());
                uvs.Add(dir, new List<float2>());
                tintIndices.Add(dir, new List<int>());
                vertIndexOffset.Add(dir, 0);
            }
        }

        public BlockGeometry(BlockModelWrapper wrapper) : this()
        {
            // First do things inherited from first constructor
            AppendWrapper(wrapper);
        }

        public void AppendWrapper(BlockModelWrapper wrapper)
        {
            // Build things up!
            foreach (var elem in wrapper.model.elements)
            {
                AppendElement(wrapper.model, elem, wrapper.zyRot, wrapper.uvlock);
            }

        }

        private readonly Dictionary<CullDir, float3[]> vertexArrs = new();
        private readonly Dictionary<CullDir, float2[]> txuvArrs = new();
        private readonly Dictionary<CullDir, int[]>    tintArrs = new();

        public BlockGeometry Finalize()
        {
            foreach (CullDir dir in Enum.GetValues(typeof (CullDir)))
            {
                vertexArrs.Add(dir, verticies[dir].ToArray());
                txuvArrs.Add(dir, uvs[dir].ToArray());
                tintArrs.Add(dir, tintIndices[dir].ToArray());
            }

            return this;
        }

        // Cache for array sizes, mapping cull flags
        // to corresponding vertex array sizes
        private readonly ConcurrentDictionary<int, int> sizeCache = new();

        private int CalculateArraySize(int cullFlags)
        {
            int vertexCount = vertexArrs[CullDir.NONE].Length;

            if ((cullFlags & (1 << 0)) != 0)
                vertexCount += vertexArrs[CullDir.UP].Length;

            if ((cullFlags & (1 << 1)) != 0)
                vertexCount += vertexArrs[CullDir.DOWN].Length;

            if ((cullFlags & (1 << 2)) != 0)
                vertexCount += vertexArrs[CullDir.SOUTH].Length;

            if ((cullFlags & (1 << 3)) != 0)
                vertexCount += vertexArrs[CullDir.NORTH].Length;

            if ((cullFlags & (1 << 4)) != 0)
                vertexCount += vertexArrs[CullDir.EAST].Length;

            if ((cullFlags & (1 << 5)) != 0)
                vertexCount += vertexArrs[CullDir.WEST].Length;

            return vertexCount;
        }

        public Tuple<float3[], float2[], int[]> GetDataForChunk(uint startVertOffset, float3 posOffset, int cullFlags)
        {
            // Compute value if absent
            int vertexCount = (sizeCache.ContainsKey(cullFlags)) ? sizeCache[cullFlags] : (sizeCache[cullFlags] = CalculateArraySize(cullFlags));

            var verts  = new float3[vertexCount];
            var txuvs  = new float2[vertexCount];
            var tintIndcs = new int[vertexCount];

            uint i;

            if (vertexArrs[CullDir.NONE].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.NONE].Length;i++)
                    verts[i] = vertexArrs[CullDir.NONE][i] + posOffset;
                txuvArrs[CullDir.NONE].CopyTo(txuvs, 0);
                tintArrs[CullDir.NONE].CopyTo(tintIndcs, 0);
            }

            uint vertOffset = (uint)vertexArrs[CullDir.NONE].Length;

            if ((cullFlags & (1 << 0)) != 0 && vertexArrs[CullDir.UP].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.UP].Length;i++)
                    verts[i + vertOffset] = vertexArrs[CullDir.UP][i] + posOffset;
                txuvArrs[CullDir.UP].CopyTo(txuvs, vertOffset);
                tintArrs[CullDir.UP].CopyTo(tintIndcs, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.UP].Length;
            }

            if ((cullFlags & (1 << 1)) != 0 && vertexArrs[CullDir.DOWN].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.DOWN].Length;i++)
                    verts[i + vertOffset] = vertexArrs[CullDir.DOWN][i] + posOffset;
                txuvArrs[CullDir.DOWN].CopyTo(txuvs, vertOffset);
                tintArrs[CullDir.DOWN].CopyTo(tintIndcs, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.DOWN].Length;
            }

            if ((cullFlags & (1 << 2)) != 0 && vertexArrs[CullDir.SOUTH].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.SOUTH].Length;i++)
                    verts[i + vertOffset] = vertexArrs[CullDir.SOUTH][i] + posOffset;
                txuvArrs[CullDir.SOUTH].CopyTo(txuvs, vertOffset);
                tintArrs[CullDir.SOUTH].CopyTo(tintIndcs, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.SOUTH].Length;
            }

            if ((cullFlags & (1 << 3)) != 0 && vertexArrs[CullDir.NORTH].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.NORTH].Length;i++)
                    verts[i + vertOffset] = vertexArrs[CullDir.NORTH][i] + posOffset;
                txuvArrs[CullDir.NORTH].CopyTo(txuvs, vertOffset);
                tintArrs[CullDir.NORTH].CopyTo(tintIndcs, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.NORTH].Length;
            }

            if ((cullFlags & (1 << 4)) != 0 && vertexArrs[CullDir.EAST].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.EAST].Length;i++)
                    verts[i + vertOffset] = vertexArrs[CullDir.EAST][i] + posOffset;
                txuvArrs[CullDir.EAST].CopyTo(txuvs, vertOffset);
                tintArrs[CullDir.EAST].CopyTo(tintIndcs, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.EAST].Length;
            }

            if ((cullFlags & (1 << 5)) != 0 && vertexArrs[CullDir.WEST].Length > 0)
            {
                for (i = 0U;i < vertexArrs[CullDir.WEST].Length;i++)
                    verts[i + vertOffset] = vertexArrs[CullDir.WEST][i] + posOffset;
                txuvArrs[CullDir.WEST].CopyTo(txuvs, vertOffset);
                tintArrs[CullDir.WEST].CopyTo(tintIndcs, vertOffset);
                vertOffset += (uint)vertexArrs[CullDir.WEST].Length;
            }

            return Tuple.Create(verts, txuvs, tintIndcs);

        }

        private void AppendElement(BlockModel model, BlockModelElement elem, int2 zyRot, bool uvlock)
        {
            float lx = Mathf.Min(elem.from.x, elem.to.x) / MC_VERT_SCALE;
            float mx = Mathf.Max(elem.from.x, elem.to.x) / MC_VERT_SCALE;
            float ly = Mathf.Min(elem.from.y, elem.to.y) / MC_VERT_SCALE;
            float my = Mathf.Max(elem.from.y, elem.to.y) / MC_VERT_SCALE;
            float lz = Mathf.Min(elem.from.z, elem.to.z) / MC_VERT_SCALE;
            float mz = Mathf.Max(elem.from.z, elem.to.z) / MC_VERT_SCALE;

            float3[] elemVerts = new float3[]{
                new float3(lx, ly, lz), new float3(lx, ly, mz),
                new float3(lx, my, lz), new float3(lx, my, mz),
                new float3(mx, ly, lz), new float3(mx, ly, mz),
                new float3(mx, my, lz), new float3(mx, my, mz)
            };

            if (elem.rotAngle != 0F) // Apply model rotation...
                Rotations.RotateVertices(ref elemVerts, elem.pivot / MC_VERT_SCALE, elem.axis, -elem.rotAngle, elem.rescale); // TODO Check angle
            
            bool stateRotated = zyRot.x != 0 || zyRot.y != 0;

            if (stateRotated) // Apply state rotation...
                Rotations.RotateWrapper(ref elemVerts, zyRot);

            foreach (var facePair in elem.faces)
            {
                // Select the current face
                var face = facePair.Value;

                // Update current cull direcion...
                var cullDir = cullMap[zyRot][face.cullDir];

                switch (facePair.Key) // Build face in that direction
                {
                    case FaceDir.UP:    // Unity +Y
                        verticies[cullDir].Add(elemVerts[2]); // 0
                        verticies[cullDir].Add(elemVerts[3]); // 1
                        verticies[cullDir].Add(elemVerts[6]); // 2
                        verticies[cullDir].Add(elemVerts[7]); // 3
                        break;
                    case FaceDir.DOWN:  // Unity -Y
                        verticies[cullDir].Add(elemVerts[4]); // 0
                        verticies[cullDir].Add(elemVerts[5]); // 1
                        verticies[cullDir].Add(elemVerts[0]); // 2
                        verticies[cullDir].Add(elemVerts[1]); // 3
                        break;
                    case FaceDir.SOUTH: // Unity +X
                        verticies[cullDir].Add(elemVerts[6]); // 0
                        verticies[cullDir].Add(elemVerts[7]); // 1
                        verticies[cullDir].Add(elemVerts[4]); // 2
                        verticies[cullDir].Add(elemVerts[5]); // 3
                        break;
                    case FaceDir.NORTH: // Unity -X
                        verticies[cullDir].Add(elemVerts[3]); // 0
                        verticies[cullDir].Add(elemVerts[2]); // 1
                        verticies[cullDir].Add(elemVerts[1]); // 2
                        verticies[cullDir].Add(elemVerts[0]); // 3
                        break;
                    case FaceDir.EAST:  // Unity +Z
                        verticies[cullDir].Add(elemVerts[7]); // 0
                        verticies[cullDir].Add(elemVerts[3]); // 1
                        verticies[cullDir].Add(elemVerts[5]); // 2
                        verticies[cullDir].Add(elemVerts[1]); // 3
                        break;
                    case FaceDir.WEST:  // Unity -Z
                        verticies[cullDir].Add(elemVerts[2]); // 0
                        verticies[cullDir].Add(elemVerts[6]); // 1
                        verticies[cullDir].Add(elemVerts[0]); // 2
                        verticies[cullDir].Add(elemVerts[4]); // 3
                        break;
                }

                ResourceLocation texIdentifier = model.resolveTextureName(face.texName);

                // This value is mapped only when uvlock is on, according to this block state's
                // state rotation, and it rotates the area of texture which is used on the face
                int uvAreaRot = stateRotated && uvlock ? uvlockMap[zyRot][facePair.Key] : 0;

                float2[] remappedUVs = RemapUVs(face.uv / MC_UV_SCALE, texIdentifier, uvAreaRot);

                // This rotation doesn't change the area of texture used...
                // See https://minecraft.fandom.com/wiki/Model#Block_models
                switch (face.rot)
                {
                    case Rotations.UVRot.UV_90:
                        uvs[cullDir].Add(remappedUVs[2]); // 2
                        uvs[cullDir].Add(remappedUVs[0]); // 0
                        uvs[cullDir].Add(remappedUVs[3]); // 3
                        uvs[cullDir].Add(remappedUVs[1]); // 1
                        break;
                    case Rotations.UVRot.UV_180:
                        uvs[cullDir].Add(remappedUVs[3]); // 3
                        uvs[cullDir].Add(remappedUVs[2]); // 2
                        uvs[cullDir].Add(remappedUVs[1]); // 1
                        uvs[cullDir].Add(remappedUVs[0]); // 0
                        break;
                    case Rotations.UVRot.UV_270:
                        uvs[cullDir].Add(remappedUVs[1]); // 1
                        uvs[cullDir].Add(remappedUVs[3]); // 3
                        uvs[cullDir].Add(remappedUVs[0]); // 0
                        uvs[cullDir].Add(remappedUVs[2]); // 2
                        break;
                    default: // Including Rotations.UVRot.UV_0
                        uvs[cullDir].Add(remappedUVs[0]); // 0
                        uvs[cullDir].Add(remappedUVs[1]); // 1
                        uvs[cullDir].Add(remappedUVs[2]); // 2
                        uvs[cullDir].Add(remappedUVs[3]); // 3
                        break;
                }
                
                // And tint indices..
                for (int i = 0;i < 4;i++)
                    tintIndices[cullDir].Add(face.tintIndex);

                // Update vertex offset to current face's cull direction
                uint offset = vertIndexOffset[cullDir];

                // Increament vertex index offset of this cull direction
                vertIndexOffset[cullDir] += 4; // Four vertices per quad
            }

        }

        private static float2[] RemapUVs(float4 uvs, ResourceLocation source, int areaRot)
        {
            return BlockTextureManager.GetUVs(source, uvs, areaRot);
        }

        private static Dictionary<int2, Dictionary<FaceDir, int>> CreateUVLockMap()
        {
            var areaRotMap = new Dictionary<int2, Dictionary<FaceDir, int>>();

            for (int roty = 0;roty < 4;roty++)
            {
                for (int rotz = 0;rotz < 4;rotz++)
                {
                    // Store actual rotation values currently applied to these faces (due to vertex(mesh) rotation)
                    var localRot = new Dictionary<FaceDir, int>();

                    foreach (FaceDir dir in Enum.GetValues(typeof (FaceDir)))
                        localRot.Add(dir, 0);

                    switch (rotz)
                    {
                        case 0:
                            localRot[FaceDir.UP]   =  roty;
                            localRot[FaceDir.DOWN] = -roty;
                            break;
                        case 1: // Locally rotate 90 Deg Clockwise
                            localRot[FaceDir.UP]    =  2;
                            localRot[FaceDir.DOWN]  =  0;
                            localRot[FaceDir.WEST]  = -1;
                            localRot[FaceDir.EAST]  =  1;
                            localRot[FaceDir.SOUTH] =  roty;
                            localRot[FaceDir.NORTH] = -roty + 2;
                            break;
                        case 2: // Locally rotate 180 Deg
                            localRot[FaceDir.UP]    = -roty;
                            localRot[FaceDir.DOWN]  =  roty;
                            localRot[FaceDir.WEST]  =  2;
                            localRot[FaceDir.EAST]  =  2;
                            localRot[FaceDir.SOUTH] =  2;
                            localRot[FaceDir.NORTH] =  2;
                            break;
                        case 3: // Locally rotate 90 Deg Counter-Clockwise
                            localRot[FaceDir.UP]    =  0;
                            localRot[FaceDir.DOWN]  =  2;
                            localRot[FaceDir.WEST]  =  1;
                            localRot[FaceDir.EAST]  = -1;
                            localRot[FaceDir.SOUTH] = -roty;
                            localRot[FaceDir.NORTH] =  roty + 2;
                            break;
                    }

                    var result = new Dictionary<FaceDir, int>();

                    // Cancel horizontal texture rotations (front / right / back / left)
                    foreach (FaceDir dir in Enum.GetValues(typeof (FaceDir)))
                        result.Add(dir, (8 - localRot.GetValueOrDefault(dir, 0)) % 4);

                    areaRotMap.Add(new int2(rotz, roty), result);

                }
                
            }
            
            return areaRotMap;

        }

        private static readonly Dictionary<int2, Dictionary<FaceDir, int>> uvlockMap = CreateUVLockMap();

        private static Dictionary<int2, Dictionary<CullDir, CullDir>> CreateCullMap()
        {
            var cullRemap = new Dictionary<int2, Dictionary<CullDir, CullDir>>();
            var rotYMap = new CullDir[]{ CullDir.NORTH, CullDir.EAST, CullDir.SOUTH, CullDir.WEST };

            for (int roty = 0;roty < 4;roty++)
            {
                // First shift directions around Y axis...
                var rotYMapRotated = rotYMap.Skip(roty).Concat(rotYMap.Take(roty)).ToArray();
                var rotZMap = new CullDir[]{ rotYMapRotated[0], CullDir.DOWN, rotYMapRotated[2], CullDir.UP };
                for (int rotz = 0;rotz < 4;rotz++)
                {
                    //Debug.Log("Rotation: z: " + rotx + ", y: " + roty);
                    // Then shift directions around the rotated Z axis...
                    var rotZMapRotated = rotZMap.Skip(rotz).Concat(rotZMap.Take(rotz)).ToArray();

                    var rotYRemap = new Dictionary<CullDir, CullDir>(){
                        { rotYMap[0], rotYMapRotated[0] }, { rotYMap[1], rotYMapRotated[1] },
                        { rotYMap[2], rotYMapRotated[2] }, { rotYMap[3], rotYMapRotated[3] }
                    };

                    var rotZRemap = new Dictionary<CullDir, CullDir>(){
                        { rotZMap[0], rotZMapRotated[0] }, { rotZMap[1], rotZMapRotated[1] },
                        { rotZMap[2], rotZMapRotated[2] }, { rotZMap[3], rotZMapRotated[3] }
                    };

                    var remap = new Dictionary<CullDir, CullDir>();
                    foreach (CullDir original in Enum.GetValues(typeof (CullDir)))
                    {
                        CullDir target = rotZRemap.GetValueOrDefault(
                            rotYRemap.GetValueOrDefault(original, original),
                            rotYRemap.GetValueOrDefault(original, original)
                        );
                        //Debug.Log(original + " => " + target);
                        remap.Add(original, target);
                    }

                    cullRemap.Add(new int2(rotz, roty), remap);

                }
            }

            return cullRemap;

        }

        private static readonly Dictionary<int2, Dictionary<CullDir, CullDir>> cullMap = CreateCullMap();

    }
}
