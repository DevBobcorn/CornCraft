using UnityEngine;

namespace CraftSharp.Control
{
    public class AABBSelectionBox : MonoBehaviour
    {
        private static readonly int LINE_COLOR = Shader.PropertyToID("_Line_Color");
        private static readonly int LINE_LENGTH = Shader.PropertyToID("_Line_Length");
        private static readonly int BILLBOARD_AXIS = Shader.PropertyToID("_BillboardAxis");

        [SerializeField] private MeshRenderer[] lineMeshRenderers;
        [SerializeField] private Color lineColor = Color.cyan;

        private static MaterialPropertyBlock GetLengthPropertyBlock(int axis, float length, Color color)
        {
            var propBlock = new MaterialPropertyBlock();
            propBlock.SetColor(LINE_COLOR, color);
            propBlock.SetFloat(LINE_LENGTH, length);
            propBlock.SetInteger(BILLBOARD_AXIS, axis);

            return propBlock;
        }
        
        public void UpdateAABB(BlockShapeAABB aabb)
        {
            // Swap X and Z
            float minX = aabb.MinZ, minY = aabb.MinY, minZ = aabb.MinX;
            float maxX = aabb.MaxZ, maxY = aabb.MaxY, maxZ = aabb.MaxX;
            var halfX = (minX + maxX) / 2F;
            var halfY = (minY + maxY) / 2F;
            var halfZ = (minZ + maxZ) / 2F;
            var propX = GetLengthPropertyBlock(0, maxX - minX, lineColor);
            var propY = GetLengthPropertyBlock(1, maxY - minY, lineColor);
            var propZ = GetLengthPropertyBlock(2, maxZ - minZ, lineColor);

            lineMeshRenderers[0 ].SetPropertyBlock(propX);
            lineMeshRenderers[1 ].SetPropertyBlock(propX);
            lineMeshRenderers[2 ].SetPropertyBlock(propX);
            lineMeshRenderers[3 ].SetPropertyBlock(propX);

            lineMeshRenderers[4 ].SetPropertyBlock(propY);
            lineMeshRenderers[5 ].SetPropertyBlock(propY);
            lineMeshRenderers[6 ].SetPropertyBlock(propY);
            lineMeshRenderers[7 ].SetPropertyBlock(propY);

            lineMeshRenderers[8 ].SetPropertyBlock(propZ);
            lineMeshRenderers[9 ].SetPropertyBlock(propZ);
            lineMeshRenderers[10].SetPropertyBlock(propZ);
            lineMeshRenderers[11].SetPropertyBlock(propZ);

            lineMeshRenderers[0 ].transform.localPosition = new(halfX, minY, minZ);
            lineMeshRenderers[1 ].transform.localPosition = new(halfX, minY, maxZ);
            lineMeshRenderers[2 ].transform.localPosition = new(halfX, maxY, minZ);
            lineMeshRenderers[3 ].transform.localPosition = new(halfX, maxY, maxZ);

            lineMeshRenderers[4 ].transform.localPosition = new(minX, halfY, minZ);
            lineMeshRenderers[5 ].transform.localPosition = new(minX, halfY, maxZ);
            lineMeshRenderers[6 ].transform.localPosition = new(maxX, halfY, minZ);
            lineMeshRenderers[7 ].transform.localPosition = new(maxX, halfY, maxZ);

            lineMeshRenderers[8 ].transform.localPosition = new(minX, minY, halfZ);
            lineMeshRenderers[9 ].transform.localPosition = new(minX, maxY, halfZ);
            lineMeshRenderers[10].transform.localPosition = new(maxX, minY, halfZ);
            lineMeshRenderers[11].transform.localPosition = new(maxX, maxY, halfZ);

            for (int i = 0; i < 12; i++)
            {
                lineMeshRenderers[i].gameObject.SetActive(true);
            }
        }

        public void ClearAABB()
        {
            for (int i = 0; i < 12; i++)
            {
                lineMeshRenderers[i].gameObject.SetActive(false);
            }
        }
    }
}