using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class Set : Command
    {
        public override string CmdName { get { return "set"; } }
        public override string CmdUsage { get { return "set varname=value"; } }
        public override string CmdDesc { get { return "cmd.set.desc"; } }

        public override string Run(CornClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] temp = getArg(command).Split('=');
                if (temp.Length > 1)
                {
                    if (CornCraft.SetVar(temp[0], getArg(command).Substring(temp[0].Length + 1)))
                    {
                        return ""; //Success
                    }
                    else return Translations.Get("cmd.set.format");
                }
                else return GetCmdDescTranslated();
            }
            else return GetCmdDescTranslated();
        }
    }
}
