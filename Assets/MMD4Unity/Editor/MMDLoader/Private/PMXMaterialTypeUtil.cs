using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MMD
{
    public static class PMXMaterialTypeUtil
    {
        public static PMXMaterialType GuessMaterialType(PMX.PMXFormat.Material material)
        {
            var materialType = PMXMaterialType.Unknown;

            if (material.name.Contains("衣") || material.name.Contains("裙") || material.name.Contains("裤") ||
                    material.name.Contains("带") || material.name.Contains("花") || material.name.Contains("饰") ||
                    material.name.Contains("飾"))
            {
                materialType = PMXMaterialType.Cloth;
            }
            else if (material.name.Contains("脸") || material.name.Contains("顔") || material.name.Contains("颜"))
            {
                materialType = PMXMaterialType.Face;
            }
            else if (material.name.Contains("白目") || material.name.Contains("睫") ||
                    material.name.Contains("眉") || material.name.Contains("二重") ||
                    material.name.Contains("口") || material.name.Contains("唇") ||
                    material.name.Contains("牙") || material.name.Contains("齿") || material.name.Contains("歯"))
            {
                materialType = PMXMaterialType.Face;
            }
            else if (material.name.Contains("目") || material.name.Contains("眼") || material.name.Contains("瞳"))
            {
                // Use face material type for now
                materialType = PMXMaterialType.Face;
            }
            else if (material.name.Contains("发") || material.name.Contains("髪"))
            {
                materialType = PMXMaterialType.Hair;
            }
            else if (material.name.Contains("体") || material.name.Contains("肌"))
            {
                materialType = PMXMaterialType.Body;
            }

            return materialType;
        }
    }
}