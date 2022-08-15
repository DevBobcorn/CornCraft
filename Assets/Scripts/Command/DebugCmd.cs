using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class DebugCmd : Command
    {
        public override string CmdName { get { return "debug"; } }
        public override string CmdUsage { get { return "debug [on|off]"; } }
        public override string CmdDesc { get { return "cmd.debug.desc"; } }

        public override string Run(CornClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                CornCraft.DebugMode = (getArg(command).ToLower() == "on");
            }
            else CornCraft.DebugMode = !CornCraft.DebugMode;
            return Translations.Get(CornCraft.DebugMode ? "cmd.debug.state_on" : "cmd.debug.state_off");
        }
    }
}
