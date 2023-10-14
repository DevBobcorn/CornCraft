namespace CraftSharp.Control
{
    public class BlockInteractionInfo : InteractionInfo
    {
        public BlockLoc Location { get; }// Location for calculating distance
        public BlockInteractionDefinition Definition { get; }

        public BlockInteractionInfo(int id, BlockLoc location, BlockInteractionDefinition def)
        {
            Id = id;
            Location = location;
            Definition = def;
        }

        public override string GetHint()
        {
            return Definition.Hint;
        }

        public override void RunInteraction(BaseCornClient client)
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