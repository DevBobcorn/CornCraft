using CraftSharp.Protocol;

namespace CraftSharp.Control
{
    public class BlockInteractionInfo : InteractionInfo
    {
        private readonly BlockLoc location; // Location for calculating distance
        private readonly string[] paramTexts;
        private readonly BlockInteractionDefinition definition;

        public BlockInteractionInfo(int id, BlockLoc loc, ResourceLocation blockId, BlockInteractionDefinition def)
        {
            Id = id;
            paramTexts = new string[] { ChatParser.TranslateString(blockId.GetTranslationKey("block")) };
            location = loc;
            definition = def;
        }

        public override InteractionIconType GetIconType()
        {
            return definition.IconType;
        }

        public override ResourceLocation GetIconItemId()
        {
            return definition.IconItemId;
        }

        public override string GetHintKey()
        {
            return definition.HintKey;
        }

        public override string[] GetParamTexts()
        {
            return paramTexts;
        }

        public BlockInteractionDefinition GetDefinition()
        {
            return definition;
        }

        public override void RunInteraction(BaseCornClient client)
        {
            switch (definition.Type)
            {
                case BlockInteractionType.Interact:
                    client.PlaceBlock(location, Direction.Down);
                    break;
                case BlockInteractionType.Break:
                    client.DigBlock(location, true, false);
                    break;
            }
        }
    }
}