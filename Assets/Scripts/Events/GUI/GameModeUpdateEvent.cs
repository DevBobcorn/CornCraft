using MinecraftClient.Mapping;

namespace MinecraftClient.Event
{
    public class GameModeUpdateEvent : BaseEvent
    {
        public GameMode newGameMode;

        public GameModeUpdateEvent(GameMode gamemode)
        {
            newGameMode = gamemode;
        }

    }
}