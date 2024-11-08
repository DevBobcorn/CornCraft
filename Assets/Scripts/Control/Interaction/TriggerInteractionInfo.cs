using System.Collections;
using CraftSharp.Inventory;
using CraftSharp.Protocol;

namespace CraftSharp.Control
{
    public class TriggerInteractionInfo : InteractionInfo
    {
        private readonly BlockLoc location; // Location for calculating distance
        private readonly string[] paramTexts;
        private readonly TriggerInteractionDefinition definition;

        public TriggerInteractionInfo(int id, BlockLoc loc, ResourceLocation blockId, TriggerInteractionDefinition def)
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

        public TriggerInteractionDefinition GetDefinition()
        {
            return definition;
        }

        public override IEnumerator RunInteraction(BaseCornClient client)
        {
            switch (definition.Type)
            {
                case TriggerInteractionType.Interact:
                    client.PlaceBlock(location, Direction.Down);
                    break;
                case TriggerInteractionType.Break:
                    client.DigBlock(location, Direction.Down);

                    if (client is CornClientOnline clientOnline) clientOnline.DoAnimation((int)Hand.MainHand);

                    client.DigBlock(location, Direction.Down, BaseCornClient.DiggingStatus.Finished);
                    break;
            }

            yield return null;
        }
    }
}