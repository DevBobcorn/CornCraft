using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Send : Command
    {
        public override string CmdName { get { return "send"; } }
        public override string CmdUsage { get { return "send <text>"; } }
        public override string CmdDesc { get { return "cmd.send.desc"; } }

        public override string Run(CornClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                handler.SendText(getArg(command));
                return "";
            }
            else return GetCmdDescTranslated();
        }
    }
}
