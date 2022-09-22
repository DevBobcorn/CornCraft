using MinecraftClient.Control;

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