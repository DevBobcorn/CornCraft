#nullable enable

namespace CraftSharp.Inventory
{
    public record BookPage(string RawContent, bool HasFilteredContent, string? FilteredContent)
    {
        public string RawContent { get; } = RawContent;
        public bool HasFilteredContent { get; } = HasFilteredContent;
        public string? FilteredContent { get; } = FilteredContent;
    }
}