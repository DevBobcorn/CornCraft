using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Log : Command
    {
        public override string CmdName { get { return "log"; } }
        public override string CmdUsage { get { return "log <text>"; } }
        public override string CmdDesc { get { return "cmd.log.desc"; } }

        public override string Run(CornClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                UnityEngine.Debug.Log(getArg(command));
                return "";
            }
            else return GetCmdDescTranslated();
        }
    }
}
