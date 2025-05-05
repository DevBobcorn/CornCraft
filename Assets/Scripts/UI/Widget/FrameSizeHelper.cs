using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Image))]
    public class FrameSizeHelper : BaseMeshEffect
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform rectTransform;

        [SerializeField] private float borderWidth = 5F;
        [SerializeField] private Vector4 cornerRadii = Vector4.zero;
        private Rect lastRect;

        private void Update()
        {
            if (rectTransform)
            {
                if (lastRect != rectTransform.rect)
                {
                    lastRect = rectTransform.rect;
                    graphic.SetVerticesDirty();
                }
            }
        }
        
        // See https://discussions.unity.com/t/big-problem-with-lacking-materialpropertyblock-for-ui-image/684361/11
        public override void ModifyMesh(VertexHelper vh) {
            if (!targetImage || !rectTransform)
            {
                return;
            }
            
            UIVertex vert = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++) {
                vh.PopulateUIVertex(ref vert, i);
                vert.uv1 = new Vector4(rectTransform.rect.width, rectTransform.rect.height, borderWidth, 0F);;
                vert.uv2 = cornerRadii;
                vh.SetUIVertex(vert, i);
            }
        }
    }
}