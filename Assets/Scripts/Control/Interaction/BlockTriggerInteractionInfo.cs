using System.Collections;
using System.Threading.Tasks;

using CraftSharp.Inventory;
using CraftSharp.Protocol.Message;
using CraftSharp.UI;

namespace CraftSharp.Control
{
    public sealed class BlockTriggerInteractionInfo : BlockInteractionInfo, IIconProvider
    {
        public TriggerInteraction Definition { get; }

        public override string HintKey => Definition.HintKey;

        public ResourceLocation IconTypeId => Definition.IconTypeId;

        public ResourceLocation IconItemId => Definition.IconItemId;

        public BlockTriggerInteractionInfo(int id, Block block, BlockLoc loc, ResourceLocation blockId, TriggerInteraction def) : base(id, block, loc)
        {
            ParamTexts = new[] { ChatParser.TranslateString(blockId.GetTranslationKey("block")) };
            Definition = def;
        }

        protected override IEnumerator RunInteraction(BaseCornClient client)
        {
            while (true)
            {
                switch (Definition.Type)
                {
                    case InteractionType.Interact:
                    {
                        client.PlaceBlock(blockLoc, Direction.Down, 0.5F, 0.5F, 0.5F);

                        break;
                    }
                    case InteractionType.Break:
                    {
                        // Takes 30 to 40 milsecs to send, don't wait for it
                        Task.Run(() => client.DigBlock(blockLoc, Direction.Down, DiggingStatus.Started));

                        if (client is CornClientOnline clientOnline)
                            clientOnline.DoAnimation((int) Hand.MainHand);

                        // Takes 30 to 40 milsecs to send, don't wait for it
                        Task.Run(() => client.DigBlock(blockLoc, Direction.Down, DiggingStatus.Finished));

                        break;
                    }
                }

                if (Definition.Reusable) // Don't terminate execution
                {
                    yield return null;
                }
                else // Terminate execution
                {
                    yield break;
                }
            }
        }
    }
}