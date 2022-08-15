using MinecraftClient.Mapping;
using System;
using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class Useblock : Command
    {
        public override string CmdName { get { return "useblock"; } }
        public override string CmdUsage { get { return "useblock <x> <y> <z>"; } }
        public override string CmdDesc { get { return "cmd.useblock.desc"; } }

        public override string Run(CornClient handler, string command, Dictionary<string, object> localVars)
        {
            if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length >= 3)
                {
                    int x = Convert.ToInt32(args[0]);
                    int y = Convert.ToInt32(args[1]);
                    int z = Convert.ToInt32(args[2]);
                    handler.PlaceBlock(new Location(x, y, z), Direction.Down);
                }
                else { return GetCmdDescTranslated();  }
            }
            return GetCmdDescTranslated();
        }
    }
}
