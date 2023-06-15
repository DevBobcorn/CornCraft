#nullable enable
using MinecraftClient.Mapping;

namespace MinecraftClient.Interaction
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

        public override void RunInteraction(CornClient client)
        {
            switch (Definition.Type)
            {
                case BlockInteractionType.Interact:
                    client.PlaceBlock(Location, Direction.Down);
                    break;
                case BlockInteractionType.Break:
                    client.DigBlock(Location, true, false);
                    break;
            }
            
        }

    }
}