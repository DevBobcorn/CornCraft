namespace MinecraftClient
{
    public struct ResourceLocation
    {
        public static readonly string DEFAULT_NAMESPACE = "minecraft";
        public static readonly ResourceLocation INVALID = new ResourceLocation(DEFAULT_NAMESPACE, "<missingno>");

        public readonly string nameSpace;
        public readonly string path;

        public ResourceLocation(string ns, string path)
        {
            this.nameSpace = ns;
            this.path = path;
        }

        public ResourceLocation(string path)
        {
            this.nameSpace = DEFAULT_NAMESPACE;
            this.path = path;
        }

        public static ResourceLocation fromString(string source)
        {
            if (source.Contains(':'))
            {
                string[] parts = source.Split(':', 2);
                return new ResourceLocation(parts[0], parts[1]);
            }
            return new ResourceLocation(source);
        }

        public static bool operator ==(ResourceLocation a, ResourceLocation b)
        {
            return a.nameSpace == b.nameSpace && a.path == b.path;
        }
        public static bool operator !=(ResourceLocation a, ResourceLocation b)
        {
            return a.nameSpace != b.nameSpace || a.path != b.path;
        }

        public override bool Equals(object obj)
        {
            if (obj is ResourceLocation)
            {
                ResourceLocation other = (ResourceLocation) obj;
                return this.nameSpace == other.nameSpace && this.path == other.path;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return nameSpace.GetHashCode() ^ path.GetHashCode();
        }

        public override string ToString()
        {
            return $"{nameSpace}:{path}";
        }

    }
}