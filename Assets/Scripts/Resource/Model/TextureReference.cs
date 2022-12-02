namespace MinecraftClient.Resource
{
    public struct TextureReference
    {
        public bool isPointer;
        public string name;

        public TextureReference(bool pointer, string name)
        {
            isPointer = pointer;
            this.name = name;
        }
    }
}