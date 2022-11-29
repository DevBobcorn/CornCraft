#nullable enable
using System;

namespace MinecraftClient.Mapping
{
    public class BlockInteractionInfo : InteractionInfo
    {
        public Location Location { get; }// Location for calculating distance
        public BlockInteractionDefinition Definition { get; }

        public BlockInteractionInfo(int id, Location loc, BlockInteractionDefinition def)
        {
            Id = id;
            Location = loc;
            Definition = def;
        }

        public override string GetHint()
        {
            return Definition.Hint;
        }

        public override void RunInteraction(CornClient game)
        {
            switch (Definition.Type)
            {
                case BlockInteractionType.Interact:
                    game.PlaceBlock(Location, Direction.Down);
                    break;
                case BlockInteractionType.Break:
                    game.DigBlock(Location, true, false);
                    break;
            }
            
        }

    }
}