namespace CraftSharp
{
    public enum GameMode
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }

    internal static class GameModeExtension
    {
        public static string GetIdentifier(this GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.Survival  => "survival",
                GameMode.Creative  => "creative",
                GameMode.Adventure => "adventure",
                GameMode.Spectator => "spectator",

                _                  => "unknown"
            };
        }
    }
}