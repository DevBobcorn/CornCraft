using System.Collections;
using CraftSharp.Inventory;
using CraftSharp.Protocol;

namespace CraftSharp.Control
{
    public class ViewInteractionInfo : InteractionInfo
    {
        private readonly BlockLoc location; // Location for calculating distance
        private readonly string[] paramTexts;
        private readonly ViewInteraction definition;

        public ViewInteractionInfo(int id, BlockLoc loc, ResourceLocation blockId, ViewInteraction def)
        {
            Id = id;
            paramTexts = new string[] { ChatParser.TranslateString(blockId.GetTranslationKey("block")) };
            location = loc;
            definition = def;
        }

        public InteractionIconType GetIconType()
        {
            return definition.IconType;
        }

        public ResourceLocation GetIconItemId()
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

        public ViewInteraction GetDefinition()
        {
            return definition;
        }

        public override IEnumerator RunInteraction(BaseCornClient client)
        {
            switch (definition.Type)
            {
                case InteractionType.Interact:
                    client.PlaceBlock(location, Direction.Down);
                    break;
                case InteractionType.Break:
                    client.DigBlock(location, Direction.Down);

                    if (client is CornClientOnline clientOnline) clientOnline.DoAnimation((int)Hand.MainHand);

                    client.DigBlock(location, Direction.Down, BaseCornClient.DiggingStatus.Finished);
                    break;
            }

            yield return null;
        }
    }
}