using System.Collections;
using CraftSharp.Inventory;
using CraftSharp.Protocol;

namespace CraftSharp.Control
{
    public sealed class ViewInteractionInfo : InteractionInfo
    {
        private readonly BlockLoc location; // Location for calculating distance

        public ViewInteraction Definition { get; }

        public override string HintKey => Definition.HintKey;

        public InteractionIconType IconType => Definition.IconType;

        public ResourceLocation IconItemId => Definition.IconItemId;

        public ViewInteractionInfo(int id, BlockLoc loc, ResourceLocation blockId, ViewInteraction def)
        {
            Id = id;
            ParamTexts = new string[] { ChatParser.TranslateString(blockId.GetTranslationKey("block")) };
            location = loc;
            Definition = def;
        }

        protected override IEnumerator RunInteraction(BaseCornClient client)
        {
            switch (Definition.Type)
            {
                case InteractionType.Interact:
                {
                    client.PlaceBlock(location, Direction.Down);

                    yield break;
                }
                case InteractionType.Break:
                {
                    client.DigBlock(location, Direction.Down);

                    if (client is CornClientOnline clientOnline)
                        clientOnline.DoAnimation((int)Hand.MainHand);

                    client.DigBlock(location, Direction.Down, DiggingStatus.Finished);

                    yield break;
                }
            }
        }
    }
}