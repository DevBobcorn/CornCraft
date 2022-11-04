using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class Health : Command
    {
        public override string CmdName { get { return "health"; } }
        public override string CmdUsage { get { return "health"; } }
        public override string CmdDesc { get { return "cmd.health.desc"; } }

        public override string Run(CornClient handler, string command, Dictionary<string, object> localVars)
        {
            var player = handler.PlayerData;
            return Translations.Get("cmd.health.response", player.Health, player.FoodSaturation, player.Level, player.TotalExperience);
        }
    }
}
