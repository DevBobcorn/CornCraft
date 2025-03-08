#nullable enable
using System;
using System.Collections.Generic;
using CraftSharp.Inventory;
using CraftSharp.Protocol.Handlers.StructuredComponents.Core;
using CraftSharp.Protocol.Message;

namespace CraftSharp.Protocol.Handlers.StructuredComponents.Components._1_20_6
{
    public class WrittenBlookContentComponent : StructuredComponent
    {
        public string RawTitle { get; set; } = null!;
        public bool HasFilteredTitle { get; set; }
        public string? FilteredTitle { get; set; }
        public string Author { get; set; } = null!;
        public int Generation { get; set; }
        public int NumberOfPages { get; set; }
        public List<BookPage> Pages { get; set; } = new();
        public bool Resolved { get; set; }

        public WrittenBlookContentComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
            : base(dataTypes, itemPalette, subComponentRegistry)
        {
            
        }
        
        public override void Parse(Queue<byte> data)
        {
            RawTitle = ChatParser.ParseText(DataTypes.ReadNextString(data));
            HasFilteredTitle = DataTypes.ReadNextBool(data);

            if (HasFilteredTitle)
                FilteredTitle = DataTypes.ReadNextString(data);
            
            Author = DataTypes.ReadNextString(data);
            Generation = DataTypes.ReadNextVarInt(data);
            NumberOfPages = DataTypes.ReadNextVarInt(data);

            for (var i = 0; i < NumberOfPages; i++)
            {
                var rawContent = ChatParser.ParseText(DataTypes.ReadNextString(data));
                var hasFilteredContent = DataTypes.ReadNextBool(data);
                var filteredContent = null as string;
                
                if(hasFilteredContent)
                    filteredContent = DataTypes.ReadNextString(data);
                
                Pages.Add(new BookPage(rawContent, hasFilteredContent, filteredContent));
            }

            Resolved = DataTypes.ReadNextBool(data);
        }

        public override Queue<byte> Serialize()
        {
            var data = new List<byte>();
            
            data.AddRange(DataTypes.GetString(RawTitle));
            data.AddRange(DataTypes.GetBool(HasFilteredTitle));

            if (HasFilteredTitle)
            {
                if(FilteredTitle is null)
                    throw new InvalidOperationException("Can not setialize WrittenBlookContentComponent1206 because HasFilteredTitle is true but FilteredTitle is null!");
                
                data.AddRange(DataTypes.GetString(FilteredTitle));
            }
            
            data.AddRange(DataTypes.GetString(Author));
            data.AddRange(DataTypes.GetVarInt(Generation));
            data.AddRange(DataTypes.GetVarInt(NumberOfPages));

            if (NumberOfPages != Pages.Count)
                throw new InvalidOperationException("Can not setialize WrittenBlookContentComponent1206 because NumberOfPages != Pages.Count!");

            foreach (var page in Pages)
            {
                data.AddRange(DataTypes.GetString(page.RawContent));
                data.AddRange(DataTypes.GetBool(page.HasFilteredContent));

                if (page.HasFilteredContent)
                {
                    if(page.FilteredContent is null)
                        throw new InvalidOperationException("Can not setialize WrittenBlookContentComponent1206 because page.HasFilteredContent = true, but FilteredContent is null!");
                    
                    data.AddRange(DataTypes.GetString(page.FilteredContent));
                }
            }
            data.AddRange(DataTypes.GetBool(Resolved));
            return new Queue<byte>(data);
        }
    }
}