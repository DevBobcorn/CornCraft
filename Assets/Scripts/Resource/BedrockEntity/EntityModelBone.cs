#nullable enable
using System.Linq;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class EntityModelBone
    {
        public string? ParentName = null;
        public string Name = string.Empty;
        public bool MirrorUV;

        public float3 Pivot;
        public float3 Rotation;

        public EntityModelCube[] Cubes = { };

        public static EntityModelBone FromJson(Json.JSONData data)
        {
            var boneName = data.Properties["name"].StringValue;

            var bonePivot = float3.zero;
            if (data.Properties.ContainsKey("pivot"))
            {
                bonePivot = VectorUtil.Json2SwappedFloat3(data.Properties["pivot"]);
                // Get opposite z
                bonePivot.z = -bonePivot.z;
            }

            string? parentName = null; // null means root bone
            if (data.Properties.ContainsKey("parent"))
            {
                parentName = data.Properties["parent"].StringValue;
            }

            var boneMirrorUV = false;
            if (data.Properties.ContainsKey("mirror"))
            {
                boneMirrorUV = data.Properties["mirror"].StringValue == "true";
            }

            var boneRotation = float3.zero;
            if (data.Properties.ContainsKey("rotation"))
            {
                boneRotation = VectorUtil.Json2SwappedFloat3(data.Properties["rotation"]);
                // Get opposite x
                boneRotation.x = -boneRotation.x;
            }

            EntityModelCube[] boneCubes;

            if (!data.Properties.ContainsKey("cubes"))
            {
                boneCubes = new EntityModelCube[] { };
            }
            else
            {
                boneCubes = data.Properties["cubes"].DataArray.Select(cubeData =>
                {
                    var origin = VectorUtil.Json2SwappedFloat3(cubeData.Properties["origin"]);
                    var size = VectorUtil.Json2SwappedFloat3(cubeData.Properties["size"]);
                    // Get opposite z
                    origin.z = -origin.z - size.z;

                    var uv = VectorUtil.Json2Float2(cubeData.Properties["uv"]);

                    var rotation = float3.zero;
                    if (cubeData.Properties.ContainsKey("rotation"))
                    {
                        rotation = VectorUtil.Json2SwappedFloat3(cubeData.Properties["rotation"]);
                        // Get opposite x
                        rotation.x = -rotation.x;
                    }

                    var pivot = float3.zero;
                    if (cubeData.Properties.ContainsKey("pivot"))
                    {
                        pivot = VectorUtil.Json2SwappedFloat3(cubeData.Properties["pivot"]);
                        // Get opposite z
                        pivot.z = -pivot.z;
                    }

                    var inflate = 0F;
                    if (cubeData.Properties.ContainsKey("inflate"))
                    {
                        inflate = float.Parse(cubeData.Properties["inflate"].StringValue);
                    }

                    return new EntityModelCube
                    {
                        Origin = origin,
                        Size = size,
                        UV = uv,
                        Inflate = inflate,
                        Pivot = pivot,
                        Rotation = rotation,
                    };
                } ).ToArray();
            }

            return new EntityModelBone
            {
                ParentName = parentName,
                Name = boneName,
                MirrorUV = boneMirrorUV,
                Pivot = bonePivot,
                Rotation = boneRotation,

                Cubes = boneCubes
            };
        }
    }
}