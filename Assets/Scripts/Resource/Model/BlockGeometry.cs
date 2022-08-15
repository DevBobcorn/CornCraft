using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public class BlockGeometry
    {
        public const float MC_VERT_SCALE = 16F;
        public const float MC_UV_SCALE = 16F;

        public readonly Dictionary<CullDir, List<Vector3>> verticies = new Dictionary<CullDir, List<Vector3>>();
        public readonly Dictionary<CullDir, List<int>> tris = new Dictionary<CullDir, List<int>>();
        public readonly Dictionary<CullDir, List<Vector2>> uvs = new Dictionary<CullDir, List<Vector2>>();
        public readonly Dictionary<CullDir, List<int>> tints = new Dictionary<CullDir, List<int>>();

        public readonly Dictionary<CullDir, int> vertIndexOffset = new Dictionary<CullDir, int>();

        public BlockGeometry()
        {
            // Initialize these collections...
            foreach (CullDir dir in Enum.GetValues(typeof (CullDir)))
            {
                verticies.Add(dir, new List<Vector3>());
                uvs.Add(dir, new List<Vector2>());
                tints.Add(dir, new List<int>());
                tris.Add(dir, new List<int>());
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

        // A '1' bit in cullFlags means shown, while a '0' indicates culled...
        public Tuple<Vector3[], Vector2[], int[], int[]> GetDataForChunk(int startVertOffset, Vector3 posOffset, int cullFlags)
        {
            var verts = new List<Vector3>();
            var triangles = new List<int>();
            var txuvs = uvs[CullDir.NONE].ToArray();
            var tintIndcs = tints[CullDir.NONE].ToArray();

            // These things are never culled:
            int bulkVertIndexOffset = startVertOffset;

            foreach (var vertex in verticies[CullDir.NONE])
                verts.Add(vertex + posOffset);

            foreach (var vertIndex in tris[CullDir.NONE]) // Apply extra offset when appending tris list
                triangles.Add(bulkVertIndexOffset + vertIndex);
            
            bulkVertIndexOffset = startVertOffset + verts.Count;

            if ((cullFlags & (1 << 0)) != 0) // 1st bit on, Unity +Y Shown (Up)
            {
                foreach (var vertex in verticies[CullDir.UP])
                    verts.Add(vertex + posOffset);

                foreach (var vertIndex in tris[CullDir.UP]) // Apply extra offset when appending tris list
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                
                txuvs     = ArrayUtil.GetConcated(txuvs,     uvs[CullDir.UP].ToArray());
                tintIndcs = ArrayUtil.GetConcated(tintIndcs, tints[CullDir.UP].ToArray());

                bulkVertIndexOffset = startVertOffset + verts.Count;
            }

            if ((cullFlags & (1 << 1)) != 0) // 2nd bit on, Unity -Y Shown (Down)
            {
                foreach (var vertex in verticies[CullDir.DOWN])
                    verts.Add(vertex + posOffset);
                
                foreach (var vertIndex in tris[CullDir.DOWN])
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                
                txuvs     = ArrayUtil.GetConcated(txuvs,     uvs[CullDir.DOWN].ToArray());
                tintIndcs = ArrayUtil.GetConcated(tintIndcs, tints[CullDir.DOWN].ToArray());
                
                bulkVertIndexOffset = startVertOffset + verts.Count;
            }

            if ((cullFlags & (1 << 2)) != 0) // 3rd bit on, Unity +X Shown (South)
            {
                foreach (var vertex in verticies[CullDir.SOUTH])
                    verts.Add(vertex + posOffset);
                
                foreach (var vertIndex in tris[CullDir.SOUTH])
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                
                txuvs     = ArrayUtil.GetConcated(txuvs,     uvs[CullDir.SOUTH].ToArray());
                tintIndcs = ArrayUtil.GetConcated(tintIndcs, tints[CullDir.SOUTH].ToArray());

                bulkVertIndexOffset = startVertOffset + verts.Count;
            }

            if ((cullFlags & (1 << 3)) != 0) // 4th bit on, Unity -X Shown (North)
            {
                foreach (var vertex in verticies[CullDir.NORTH])
                    verts.Add(vertex + posOffset);
                
                foreach (var vertIndex in tris[CullDir.NORTH])
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                
                txuvs     = ArrayUtil.GetConcated(txuvs,     uvs[CullDir.NORTH].ToArray());
                tintIndcs = ArrayUtil.GetConcated(tintIndcs, tints[CullDir.NORTH].ToArray());
                
                bulkVertIndexOffset = startVertOffset + verts.Count;
            }

            if ((cullFlags & (1 << 4)) != 0) // 5th bit on, Unity +Z Shown (East)
            {
                foreach (var vertex in verticies[CullDir.EAST])
                    verts.Add(vertex + posOffset);
                
                foreach (var vertIndex in tris[CullDir.EAST])
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                
                txuvs     = ArrayUtil.GetConcated(txuvs,     uvs[CullDir.EAST].ToArray());
                tintIndcs = ArrayUtil.GetConcated(tintIndcs, tints[CullDir.EAST].ToArray());

                bulkVertIndexOffset = startVertOffset + verts.Count;
            }

            if ((cullFlags & (1 << 5)) != 0) // 6th bit on, Unity -Z Shown (West)
            {
                foreach (var vertex in verticies[CullDir.WEST])
                    verts.Add(vertex + posOffset);

                foreach (var vertIndex in tris[CullDir.WEST])
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                
                txuvs     = ArrayUtil.GetConcated(txuvs,     uvs[CullDir.WEST].ToArray());
                tintIndcs = ArrayUtil.GetConcated(tintIndcs, tints[CullDir.WEST].ToArray());

                bulkVertIndexOffset = startVertOffset + verts.Count;
            }

            return Tuple.Create(verts.ToArray(), txuvs.ToArray(), tintIndcs.ToArray(), triangles.ToArray());

        }

        // A '1' bit in cullFlags means shown, while a '0' indicates culled...
        public Tuple<Vector3[], Vector2[], int[], int[]> GetData(int cullFlags)
        {
            // These things are never culled:
            List<Vector3> verts = verticies[CullDir.NONE];
            List<Vector2> txuvs = uvs[CullDir.NONE];
            List<int> tintIndcs = tints[CullDir.NONE];
            List<int> triangles = tris[CullDir.NONE];

            // First bulk is 'verticies[CullDir.NONE]' which has
            // 'verts.Count' vertices at this time...
            int bulkVertIndexOffset = verts.Count;

            if ((cullFlags & (1 << 0)) > 0) // 1st bit on, Unity +Y Shown (Up)
            {
                verts = verts.Concat(verticies[CullDir.UP]).ToList();
                txuvs = txuvs.Concat(uvs[CullDir.UP]).ToList();
                tintIndcs = tintIndcs.Concat(tints[CullDir.UP]).ToList();
                foreach (var vertIndex in tris[CullDir.UP])
                {   // Apply extra offset when appending tris list
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                }
                bulkVertIndexOffset = verts.Count;
            }

            if ((cullFlags & (1 << 1)) > 0) // 2nd bit on, Unity -Y Shown (Down)
            {
                verts = verts.Concat(verticies[CullDir.DOWN]).ToList();
                txuvs = txuvs.Concat(uvs[CullDir.DOWN]).ToList();
                tintIndcs = tintIndcs.Concat(tints[CullDir.DOWN]).ToList();
                foreach (var vertIndex in tris[CullDir.DOWN])
                {   // Apply extra offset when appending tris list
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                }
                bulkVertIndexOffset = verts.Count;
            }

            if ((cullFlags & (1 << 2)) > 0) // 3rd bit on, Unity +X Shown (South)
            {
                verts = verts.Concat(verticies[CullDir.SOUTH]).ToList();
                txuvs = txuvs.Concat(uvs[CullDir.SOUTH]).ToList();
                tintIndcs = tintIndcs.Concat(tints[CullDir.SOUTH]).ToList();
                foreach (var vertIndex in tris[CullDir.SOUTH])
                {   // Apply extra offset when appending tris list
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                }
                bulkVertIndexOffset = verts.Count;
            }

            if ((cullFlags & (1 << 3)) > 0) // 4th bit on, Unity -X Shown (North)
            {
                verts = verts.Concat(verticies[CullDir.NORTH]).ToList();
                txuvs = txuvs.Concat(uvs[CullDir.NORTH]).ToList();
                tintIndcs = tintIndcs.Concat(tints[CullDir.NORTH]).ToList();
                foreach (var vertIndex in tris[CullDir.NORTH])
                {   // Apply extra offset when appending tris list
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                }
                bulkVertIndexOffset = verts.Count;
            }

            if ((cullFlags & (1 << 4)) > 0) // 5th bit on, Unity +Z Shown (East)
            {
                verts = verts.Concat(verticies[CullDir.EAST]).ToList();
                txuvs = txuvs.Concat(uvs[CullDir.EAST]).ToList();
                tintIndcs = tintIndcs.Concat(tints[CullDir.EAST]).ToList();
                foreach (var vertIndex in tris[CullDir.EAST])
                {   // Apply extra offset when appending tris list
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                }
                bulkVertIndexOffset = verts.Count;
            }

            if ((cullFlags & (1 << 5)) > 0) // 6th bit on, Unity -Z Shown (West)
            {
                verts = verts.Concat(verticies[CullDir.WEST]).ToList();
                txuvs = txuvs.Concat(uvs[CullDir.WEST]).ToList();
                tintIndcs = tintIndcs.Concat(tints[CullDir.WEST]).ToList();
                foreach (var vertIndex in tris[CullDir.WEST])
                {   // Apply extra offset when appending tris list
                    triangles.Add(bulkVertIndexOffset + vertIndex);
                }
                bulkVertIndexOffset = verts.Count;
            }

            return Tuple.Create(verts.ToArray(), txuvs.ToArray(), tintIndcs.ToArray(), triangles.ToArray());

        }

        private void AppendElement(BlockModel model, BlockModelElement elem, Vector2Int zyRot, bool uvlock)
        {
            float lx = Mathf.Min(elem.from.x, elem.to.x) / MC_VERT_SCALE;
            float mx = Mathf.Max(elem.from.x, elem.to.x) / MC_VERT_SCALE;
            float ly = Mathf.Min(elem.from.y, elem.to.y) / MC_VERT_SCALE;
            float my = Mathf.Max(elem.from.y, elem.to.y) / MC_VERT_SCALE;
            float lz = Mathf.Min(elem.from.z, elem.to.z) / MC_VERT_SCALE;
            float mz = Mathf.Max(elem.from.z, elem.to.z) / MC_VERT_SCALE;

            Vector3[] elemVerts = new Vector3[]{
                new Vector3(lx, ly, lz), new Vector3(lx, ly, mz),
                new Vector3(lx, my, lz), new Vector3(lx, my, mz),
                new Vector3(mx, ly, lz), new Vector3(mx, ly, mz),
                new Vector3(mx, my, lz), new Vector3(mx, my, mz)
            };

            if (elem.rotAngle != 0F) // Apply model rotation...
                Rotations.RotateVertices(ref elemVerts, elem.pivot / MC_VERT_SCALE, elem.axis, -elem.rotAngle, elem.rescale); // TODO Check angle
            
            bool stateRotated = zyRot != Vector2Int.zero;

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

                Vector2[] remappedUVs = RemapUVs(face.uv / MC_UV_SCALE, texIdentifier, uvAreaRot);

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
                    tints[cullDir].Add(face.tintIndex);

                // Update vertex offset to current face's cull direction
                int offset = vertIndexOffset[cullDir];

                tris[cullDir] = tris[cullDir].Concat(new List<int>(){
                    offset + 0, offset + 1, offset + 2, offset + 2, offset + 1, offset + 3
                }).ToList();

                // Increament vertex index offset of this cull direction
                vertIndexOffset[cullDir] += 4; // Four vertices per quad
            }

        }

        private static Vector2[] RemapUVs(Vector4 uvs, ResourceLocation source, int areaRot)
        {
            return BlockTextureManager.GetUVs(source, uvs, areaRot);
        }

        private static Dictionary<Vector2Int, Dictionary<FaceDir, int>> CreateUVLockMap()
        {
            var areaRotMap = new Dictionary<Vector2Int, Dictionary<FaceDir, int>>();

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

                    areaRotMap.Add(new Vector2Int(rotz, roty), result);

                }
                
            }
            
            return areaRotMap;

        }

        private static readonly Dictionary<Vector2Int, Dictionary<FaceDir, int>> uvlockMap = CreateUVLockMap();

        private static Dictionary<Vector2Int, Dictionary<CullDir, CullDir>> CreateCullMap()
        {
            var cullRemap = new Dictionary<Vector2Int, Dictionary<CullDir, CullDir>>();
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

                    cullRemap.Add(new Vector2Int(rotz, roty), remap);

                }
            }

            return cullRemap;

        }

        private static readonly Dictionary<Vector2Int, Dictionary<CullDir, CullDir>> cullMap = CreateCullMap();

    }
}
