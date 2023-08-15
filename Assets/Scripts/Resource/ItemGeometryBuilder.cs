using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class ItemGeometryBuilder
    {
        private const float MC_VERT_SCALE = 16F;
        private const float MC_UV_SCALE = 16F;

        private readonly Dictionary<DisplayPosition, float3x3> displayTransforms;
        private readonly List<float3> verticies = new();
        private readonly List<float3> uvs       = new();
        private readonly List<float4> uvAnims   = new();
        private readonly List<int> tintIndices  = new();

        public ItemGeometryBuilder(JsonModel model)
        {
            // Build things up!
            foreach (var elem in model.Elements)
                AppendElement(model, elem);
            
            displayTransforms = model.DisplayTransforms;
        }
        
        public ItemGeometry Build()
        {
            return new ItemGeometry(
                verticies.ToArray(),
                uvs.ToArray(),
                uvAnims.ToArray(),
                tintIndices.ToArray(),
                displayTransforms
            );
        }

        private void AppendElement(JsonModel model, JsonModelElement elem)
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

            foreach (var facePair in elem.faces)
            {
                // Select the current face
                var face = facePair.Value;

                switch (facePair.Key) // Build face in that direction
                {
                    case FaceDir.UP:    // Unity +Y
                        verticies.Add(elemVerts[2]); // 0
                        verticies.Add(elemVerts[3]); // 1
                        verticies.Add(elemVerts[6]); // 2
                        verticies.Add(elemVerts[7]); // 3
                        break;
                    case FaceDir.DOWN:  // Unity -Y
                        verticies.Add(elemVerts[4]); // 0
                        verticies.Add(elemVerts[5]); // 1
                        verticies.Add(elemVerts[0]); // 2
                        verticies.Add(elemVerts[1]); // 3
                        break;
                    case FaceDir.SOUTH: // Unity +X
                        verticies.Add(elemVerts[6]); // 0
                        verticies.Add(elemVerts[7]); // 1
                        verticies.Add(elemVerts[4]); // 2
                        verticies.Add(elemVerts[5]); // 3
                        break;
                    case FaceDir.NORTH: // Unity -X
                        verticies.Add(elemVerts[3]); // 0
                        verticies.Add(elemVerts[2]); // 1
                        verticies.Add(elemVerts[1]); // 2
                        verticies.Add(elemVerts[0]); // 3
                        break;
                    case FaceDir.EAST:  // Unity +Z
                        verticies.Add(elemVerts[7]); // 0
                        verticies.Add(elemVerts[3]); // 1
                        verticies.Add(elemVerts[5]); // 2
                        verticies.Add(elemVerts[1]); // 3
                        break;
                    case FaceDir.WEST:  // Unity -Z
                        verticies.Add(elemVerts[2]); // 0
                        verticies.Add(elemVerts[6]); // 1
                        verticies.Add(elemVerts[0]); // 2
                        verticies.Add(elemVerts[4]); // 3
                        break;
                }

                ResourceLocation texIdentifier = model.ResolveTextureName(face.texName);

                var uvInfo = ResourcePackManager.Instance.GetUVs(texIdentifier, face.uv / MC_UV_SCALE, 0);
                var remappedUVs = uvInfo.uvs;
                var animInfo = uvInfo.anim;

                // This rotation doesn't change the area of texture used...
                // See https://minecraft.fandom.com/wiki/Model#Block_models
                switch (face.rot)
                {
                    case Rotations.UVRot.UV_90:
                        uvs.Add(remappedUVs[2]); // 2
                        uvs.Add(remappedUVs[0]); // 0
                        uvs.Add(remappedUVs[3]); // 3
                        uvs.Add(remappedUVs[1]); // 1
                        break;
                    case Rotations.UVRot.UV_180:
                        uvs.Add(remappedUVs[3]); // 3
                        uvs.Add(remappedUVs[2]); // 2
                        uvs.Add(remappedUVs[1]); // 1
                        uvs.Add(remappedUVs[0]); // 0
                        break;
                    case Rotations.UVRot.UV_270:
                        uvs.Add(remappedUVs[1]); // 1
                        uvs.Add(remappedUVs[3]); // 3
                        uvs.Add(remappedUVs[0]); // 0
                        uvs.Add(remappedUVs[2]); // 2
                        break;
                    default: // Including Rotations.UVRot.UV_0
                        uvs.Add(remappedUVs[0]); // 0
                        uvs.Add(remappedUVs[1]); // 1
                        uvs.Add(remappedUVs[2]); // 2
                        uvs.Add(remappedUVs[3]); // 3
                        break;
                }
                
                // Add uv animation data
                uvAnims.Add(animInfo);
                uvAnims.Add(animInfo);
                uvAnims.Add(animInfo);
                uvAnims.Add(animInfo);
                
                // And tint indices..
                for (int i = 0;i < 4;i++)
                    tintIndices.Add(face.tintIndex);
                
            }
        }
    }
}