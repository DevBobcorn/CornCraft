using MinecraftClient.Rendering;

namespace MinecraftClient.Resource
{
    public class ItemModel
    {
        public readonly ItemGeometry Geometry;
        public readonly RenderType RenderType;

        public ItemModel(ItemGeometry geometry, RenderType renderType)
        {
            Geometry = geometry;
            RenderType = renderType;
        }

    }

}