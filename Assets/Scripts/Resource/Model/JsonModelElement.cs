using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public class JsonModelElement // a cuboid element
    {
        public static readonly float3 FULL   = new(16, 16, 16);
        public static readonly float3 CENTER = new( 8,  8,  8);

        public float3 from = float3.zero, to = FULL, pivot = CENTER;
        public Dictionary<FaceDir, BlockModelFace> faces = new();
        public float rotAngle = 0F;
        public Rotations.Axis axis;
        public bool rescale = false;

        public static JsonModelElement FromJson(Json.JSONData data)
        {
            var elem = new JsonModelElement();
            if (data.Properties.ContainsKey("from") && data.Properties.ContainsKey("to") && data.Properties.ContainsKey("faces"))
            {
                // Read vertex positions with xz values swapped...
                elem.from = VectorUtil.Json2SwappedFloat3(data.Properties["from"]);
                elem.to = VectorUtil.Json2SwappedFloat3(data.Properties["to"]);
                
                var facesData = data.Properties["faces"].Properties;
                foreach (FaceDir faceDir in Enum.GetValues(typeof (FaceDir)))
                {
                    string dirName = faceDir.ToString().ToLower();
                    if (facesData.ContainsKey(dirName))
                    {
                        elem.faces.Add(faceDir, BlockModelFace.FromJson(facesData[dirName], faceDir, elem.from, elem.to));
                    }
                }

                // TODO shade
                if (data.Properties.ContainsKey("rotation"))
                {
                    var rotData = data.Properties["rotation"];
                    if (rotData.Properties.ContainsKey("origin") && rotData.Properties.ContainsKey("axis") && rotData.Properties.ContainsKey("angle"))
                    {
                        // Read pivot position with xz values swapped...
                        elem.pivot = VectorUtil.Json2SwappedFloat3(rotData.Properties["origin"]);
                        elem.axis = rotData.Properties["axis"].StringValue.ToLower() switch
                        {
                            // Swap X and Z axis for Unity...
                            "x" => Rotations.Axis.Z,
                            "y" => Rotations.Axis.Y,
                            "z" => Rotations.Axis.X,
                            _   => Rotations.Axis.Y
                        };
                        
                        // We don't need a restriction to the value here like vanilla Minecraft...
                        float.TryParse(rotData.Properties["angle"].StringValue, out elem.rotAngle);

                        if (rotData.Properties.ContainsKey("rescale"))
                            bool.TryParse(rotData.Properties["rescale"].StringValue, out elem.rescale);
                    }
                    else
                    {
                        Debug.LogWarning("Invalid element rotation!");
                    }
                }
            }
            else
            {
                Debug.LogWarning("Invalid block model element!");
            }
            return elem;
        }
    }
}
