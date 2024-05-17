namespace CraftSharp.Rendering
{
    public enum EntityRenderType
    {
        SOLID,
        CUTOUT,
        CUTOUT_CULLOFF,          // Skeleton body, etc.
        TRANSLUCENT,

        SOLID_EMISSIVE,
        CUTOUT_EMISSIVE,
        CUTOUT_CULLOFF_EMISSIVE,
        TRANSLUCENT_EMISSIVE
    }
}