using System;
using CraftSharp.Protocol;

namespace CraftSharp.Control
{
    public class ToolInteractionInfo : InteractionInfo
    {
        private readonly BlockLoc location; // Location for calculating distance
        private readonly ToolInteractionDefinition definition;

        public ToolInteractionInfo(int id, BlockLoc loc)
        {
            Id = id;
            location = loc;
        }

        public override string GetHintKey()
        {
            return string.Empty;
        }

        public override string[] GetParamTexts()
        {
            return Array.Empty<string>();
        }

        public override void RunInteraction(BaseCornClient client)
        {
            // switch (definition.Type)
            // {
            //     case ItemInteractionType.Place:
            //         client.PlaceBlock(location, Direction.Down);
            //         break;
            //     case ItemInteractionType.Break:
            //         client.DigBlock(location, Direction.Down, BaseCornClient.DiggingStatus.Started, true, false);
            //         break;
            // }
        }
    }
}