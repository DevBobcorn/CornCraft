using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class BlockModelFace // A quad face
    {
        // Texture coords for left-upper and right-lower corners
        public float4 uv;
        public Rotations.UVRot rot = Rotations.UVRot.UV_0;
        public int tintIndex = -1;
        public string texName = string.Empty;
        public CullDir cullDir = CullDir.NONE;

        // The last 3 parameters are passed in so that we can generate uvs if they're not there...
        public static BlockModelFace FromJson(Json.JSONData data, FaceDir dir, float3 from, float3 to)
        {
            BlockModelFace face = new BlockModelFace();
            if (data.Properties.ContainsKey("texture"))
            {
                // Remove leading '#' if exists... (As a texture reference)
                face.texName = data.Properties["texture"].StringValue.TrimStart('#');
            }

            if (data.Properties.ContainsKey("uv"))
            {
                face.uv = VectorUtil.Json2Float4(data.Properties["uv"]);
            }
            else // uvs got omitted, we need to generate them by ourselves...
            {
                float lx = from.x, mx = to.x;
                float ly = from.y, my = to.y;
                float lz = from.z, mz = to.z;

                face.uv = dir switch
                {
                    FaceDir.UP    => new float4(lz, lx, mz, mx),
                    FaceDir.DOWN  => new float4(lz, lx, mz, mx),

                    FaceDir.SOUTH => new float4(lz, 16F - my, mz, 16F - ly),
                    FaceDir.NORTH => new float4(16F - mz, 16F - my, 16F - lz, 16F - ly),

                    FaceDir.EAST  => new float4(lx, 16F - my, mx, 16F - ly),
                    FaceDir.WEST  => new float4(16F - mx, 16F - my, 16F - lx, 16F - ly),

                    _             => new float4()
                };

            }

            // Check uv rotation even when uv is not present (which might be specified in child models)
            if (data.Properties.ContainsKey("rotation"))
            {
                face.rot = data.Properties["rotation"].StringValue switch
                {
                    "90"  => Rotations.UVRot.UV_90,
                    "180" => Rotations.UVRot.UV_180,
                    "270" => Rotations.UVRot.UV_270,
                    _     => Rotations.UVRot.UV_0
                };
            }

            if (data.Properties.ContainsKey("cullface"))
            {
                face.cullDir = Directions.CullDirFromName(data.Properties["cullface"].StringValue);
            }

            if (data.Properties.ContainsKey("tintindex"))
            {
                int.TryParse(data.Properties["tintindex"].StringValue, out face.tintIndex);
            }

            return face;
        }
    }    
}
