using System.Collections;
using System.Threading.Tasks;

using CraftSharp.Inventory;
using CraftSharp.Protocol.Message;
using CraftSharp.UI;

namespace CraftSharp.Control
{
    public sealed class BlockTriggerInteractionInfo : BlockInteractionInfo, IIconProvider
    {
        public TriggerInteraction Interaction { get; }

        public override string HintKey => Interaction.HintKey;

        public ResourceLocation IconTypeId => Interaction.IconTypeId;

        public ResourceLocation IconItemId => Interaction.IconItemId;

        // Some trigger interactions are active only when player is holding certain items
        public bool Active = false;

        public BlockTriggerInteractionInfo(int id, Block block, BlockLoc loc, ResourceLocation blockId, TriggerInteraction def) : base(id, block, loc)
        {
            ParamTexts = new[] { ChatParser.TranslateString(blockId.GetTranslationKey("block")) };
            Interaction = def;
        }

        protected override IEnumerator RunInteraction(BaseCornClient client)
        {
            while (true)
            {
                switch (Interaction.Type)
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

                if (Interaction.Reusable) // Don't terminate execution
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