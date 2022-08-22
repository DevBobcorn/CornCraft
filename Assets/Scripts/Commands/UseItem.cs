using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    class UseItem : Command
    {
        public override string CmdName { get { return "useitem"; } }
        public override string CmdUsage { get { return "useitem"; } }
        public override string CmdDesc { get { return "cmd.useitem.desc"; } }

        public override string Run(CornClient handler, string command, Dictionary<string, object> localVars)
        {
            handler.UseItemOnHand();
            return Translations.Get("cmd.useitem.use");
        }
    }
}
