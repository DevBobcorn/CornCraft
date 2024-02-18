namespace MMD
{
    public static class PMXMaterialTypeUtil
    {
        public static PMXMaterialCategory GuessMaterialType(string materialName)
        {
            var materialType = PMXMaterialCategory.Unknown;

            if (materialName.Contains("衣") || materialName.Contains("裙") || materialName.Contains("裤") ||
                    materialName.Contains("带") || materialName.Contains("花") || materialName.Contains("饰") ||
                    materialName.Contains("飾"))
            {
                materialType = PMXMaterialCategory.Clothes;
            }
            else if (materialName.Contains("脸") || materialName.Contains("顔") || materialName.Contains("颜"))
            {
                materialType = PMXMaterialCategory.Face;
            }
            else if (materialName.Contains("白目") || materialName.Contains("睫") ||
                    materialName.Contains("眉") || materialName.Contains("二重") ||
                    materialName.Contains("口") || materialName.Contains("唇") ||
                    materialName.Contains("牙") || materialName.Contains("齿") || materialName.Contains("歯"))
            {
                materialType = PMXMaterialCategory.Face;
            }
            else if (materialName.Contains("目") || materialName.Contains("眼") || materialName.Contains("瞳"))
            {
                // Use face material type for now
                materialType = PMXMaterialCategory.Face;
            }
            else if (materialName.Contains("发") || materialName.Contains("髪"))
            {
                materialType = PMXMaterialCategory.Hair;
            }
            else if (materialName.Contains("体") || materialName.Contains("肌"))
            {
                materialType = PMXMaterialCategory.Body;
            }

            return materialType;
        }
    }
}