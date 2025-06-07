#nullable enable
using System;
using System.Collections.Generic;

namespace CraftSharp.Protocol.Handlers
{
    internal static class DeclareCommands
    {
        private static int RootIdx;
        private static CommandNode[] Nodes = Array.Empty<CommandNode>();

        public static void Read(MinecraftDataTypes dataTypes, Queue<byte> packetData, int protocolVersion)
        {
            // TODO: Fix this
            // It crashes in 1.20.6+ , could not figure out why
            // it's hard to debug, so I'll just disable it for now
            if(protocolVersion > ProtocolMinecraft.MC_1_20_4_Version)
                return;
            
            int count = DataTypes.ReadNextVarInt(packetData);
            Nodes = new CommandNode[count];
            for (int i = 0; i < count; ++i)
            {
                byte flags = DataTypes.ReadNextByte(packetData);

                int childCount = DataTypes.ReadNextVarInt(packetData);
                int[] childs = new int[childCount];
                for (int j = 0; j < childCount; ++j)
                    childs[j] = DataTypes.ReadNextVarInt(packetData);

                int redirectNode = ((flags & 0x08) == 0x08) ? DataTypes.ReadNextVarInt(packetData) : -1;

                string? name = ((flags & 0x03) == 1 || (flags & 0x03) == 2) ? DataTypes.ReadNextString(packetData) : null;

                int parserId = ((flags & 0x03) == 2) ? DataTypes.ReadNextVarInt(packetData) : -1;
                Parser? parser = null;
                if ((flags & 0x03) == 2)
                {
                    if (protocolVersion <= ProtocolMinecraft.MC_1_19_2_Version)
                        parser = parserId switch
                        {
                            1 => new ParserFloat(dataTypes, packetData),
                            2 => new ParserDouble(dataTypes, packetData),
                            3 => new ParserInteger(dataTypes, packetData),
                            4 => new ParserLong(dataTypes, packetData),
                            5 => new ParserString(dataTypes, packetData),
                            6 => new ParserEntity(dataTypes, packetData),
                            8 => new ParserBlockPos(dataTypes, packetData),
                            9 => new ParserColumnPos(dataTypes, packetData),
                            10 => new ParserVec3(dataTypes, packetData),
                            11 => new ParserVec2(dataTypes, packetData),
                            18 => new ParserMessage(dataTypes, packetData),
                            27 => new ParserRotation(dataTypes, packetData),
                            29 => new ParserScoreHolder(dataTypes, packetData),
                            43 => new ParserResourceOrTag(dataTypes, packetData),
                            44 => new ParserResource(dataTypes, packetData),
                            50 => new ParserForgeEnum(dataTypes, packetData),
                            _ => new ParserEmpty(dataTypes, packetData),
                        };
                    else if (protocolVersion <= ProtocolMinecraft.MC_1_19_3_Version) // 1.19.3
                        parser = parserId switch
                        {
                            1 => new ParserFloat(dataTypes, packetData),
                            2 => new ParserDouble(dataTypes, packetData),
                            3 => new ParserInteger(dataTypes, packetData),
                            4 => new ParserLong(dataTypes, packetData),
                            5 => new ParserString(dataTypes, packetData),
                            6 => new ParserEntity(dataTypes, packetData),
                            8 => new ParserBlockPos(dataTypes, packetData),
                            9 => new ParserColumnPos(dataTypes, packetData),
                            10 => new ParserVec3(dataTypes, packetData),
                            11 => new ParserVec2(dataTypes, packetData),
                            18 => new ParserMessage(dataTypes, packetData),
                            27 => new ParserRotation(dataTypes, packetData),
                            29 => new ParserScoreHolder(dataTypes, packetData),
                            41 => new ParserResourceOrTag(dataTypes, packetData),
                            42 => new ParserResourceOrTag(dataTypes, packetData),
                            43 => new ParserResource(dataTypes, packetData),
                            44 => new ParserResource(dataTypes, packetData),
                            50 => new ParserForgeEnum(dataTypes, packetData),
                            _ => new ParserEmpty(dataTypes, packetData),
                        };
                    else if (protocolVersion <= ProtocolMinecraft.MC_1_20_2_Version)// 1.19.4 - 1.20.2
                        parser = parserId switch
                        {
                            1 => new ParserFloat(dataTypes, packetData),
                            2 => new ParserDouble(dataTypes, packetData),
                            3 => new ParserInteger(dataTypes, packetData),
                            4 => new ParserLong(dataTypes, packetData),
                            5 => new ParserString(dataTypes, packetData),
                            6 => new ParserEntity(dataTypes, packetData),
                            8 => new ParserBlockPos(dataTypes, packetData),
                            9 => new ParserColumnPos(dataTypes, packetData),
                            10 => new ParserVec3(dataTypes, packetData),
                            11 => new ParserVec2(dataTypes, packetData),
                            18 => new ParserMessage(dataTypes, packetData),
                            27 => new ParserRotation(dataTypes, packetData),
                            29 => new ParserScoreHolder(dataTypes, packetData),
                            40 => new ParserTime(dataTypes, packetData),
                            41 => new ParserResourceOrTag(dataTypes, packetData),
                            42 => new ParserResourceOrTag(dataTypes, packetData),
                            43 => new ParserResource(dataTypes, packetData),
                            44 => new ParserResource(dataTypes, packetData),
                            50 => protocolVersion == ProtocolMinecraft.MC_1_19_4_Version ?
                              new ParserForgeEnum(dataTypes, packetData) :
                              new ParserEmpty(dataTypes, packetData),   
                            51 => (protocolVersion >= ProtocolMinecraft.MC_1_20_Version &&
                                   protocolVersion <= ProtocolMinecraft.MC_1_20_2_Version) ? // 1.20 - 1.20.2
                              new ParserForgeEnum(dataTypes, packetData) :
                              new ParserEmpty(dataTypes, packetData),
                            _ => new ParserEmpty(dataTypes, packetData),
                        };
                    else if (protocolVersion is > ProtocolMinecraft.MC_1_20_2_Version and < ProtocolMinecraft.MC_1_20_6_Version)
                        // 1.20.3 - 1.20.4
                        parser = parserId switch
                        {
                            1 => new ParserFloat(dataTypes, packetData),
                            2 => new ParserDouble(dataTypes, packetData),
                            3 => new ParserInteger(dataTypes, packetData),
                            4 => new ParserLong(dataTypes, packetData),
                            5 => new ParserString(dataTypes, packetData),
                            6 => new ParserEntity(dataTypes, packetData),
                            8 => new ParserBlockPos(dataTypes, packetData),
                            9 => new ParserColumnPos(dataTypes, packetData),
                            10 => new ParserVec3(dataTypes, packetData),
                            11 => new ParserVec2(dataTypes, packetData),
                            18 => new ParserMessage(dataTypes, packetData),
                            27 => new ParserRotation(dataTypes, packetData),
                            30 => new ParserScoreHolder(dataTypes, packetData),
                            41 => new ParserTime(dataTypes, packetData),
                            42 => new ParserResourceOrTag(dataTypes, packetData),
                            43 => new ParserResourceOrTag(dataTypes, packetData),
                            44 => new ParserResource(dataTypes, packetData),
                            45 => new ParserResource(dataTypes, packetData),
                            52 => new ParserForgeEnum(dataTypes, packetData),
                            _ => new ParserEmpty(dataTypes, packetData),
                        };
                    else // 1.20.6+
                        parser = parserId switch
                        {
                            1 => new ParserFloat(dataTypes, packetData),
                            2 => new ParserDouble(dataTypes, packetData),
                            3 => new ParserInteger(dataTypes, packetData),
                            4 => new ParserLong(dataTypes, packetData),
                            5 => new ParserString(dataTypes, packetData),
                            6 => new ParserEntity(dataTypes, packetData),
                            30 => new ParserScoreHolder(dataTypes, packetData),
                            41 => new ParserTime(dataTypes, packetData),
                            42 => new ParserResourceOrTag(dataTypes, packetData),
                            43 => new ParserResourceOrTag(dataTypes, packetData),
                            44 => new ParserResource(dataTypes, packetData),
                            45 => new ParserResource(dataTypes, packetData),
                            52 => new ParserForgeEnum(dataTypes, packetData),
                            _ => new ParserEmpty(dataTypes, packetData),
                        };
                }

                string? suggestionsType = ((flags & 0x10) == 0x10) ? DataTypes.ReadNextString(packetData) : null;

                Nodes[i] = new(flags, childs, redirectNode, name, parser, suggestionsType, parserId);
            }
            RootIdx = DataTypes.ReadNextVarInt(packetData);

            //ConsoleIO.OnDeclareMinecraftCommand(ExtractRootCommand());
        }

        private static string[] ExtractRootCommand()
        {
            List<string> commands = new();
            CommandNode root = Nodes[RootIdx];
            foreach (var child in root.Clildren)
            {
                string? childName = Nodes[child].Name;
                if (childName != null)
                    commands.Add(childName);
            }
            return commands.ToArray();
        }

        public static List<Tuple<string, string>> CollectSignArguments(string command)
        {
            List<Tuple<string, string>> needSigned = new();
            CollectSignArguments(RootIdx, command, needSigned);
            return needSigned;
        }

        private static void CollectSignArguments(int NodeIdx, string command, List<Tuple<string, string>> arguments)
        {
            CommandNode node = Nodes[NodeIdx];
            string last_arg = command;
            switch (node.Flags & 0x03)
            {
                case 0: // root
                    break;
                case 1: // literal
                    {
                        string[] arg = command.Split(' ', 2, StringSplitOptions.None);
                        if (!(arg.Length == 2 && node.Name! == arg[0]))
                            return;
                        last_arg = arg[1];
                    }
                    break;
                case 2: // argument
                    {
                        int argCnt = (node.Paser == null) ? 1 : node.Paser.GetArgCnt();
                        string[] arg = command.Split(' ', argCnt + 1, StringSplitOptions.None);
                        if ((node.Flags & 0x04) > 0)
                        {
                            if (node.Paser != null && node.Paser.GetName() == "minecraft:message")
                                arguments.Add(new(node.Name!, command));
                        }
                        if (!(arg.Length == argCnt + 1))
                            return;
                        last_arg = arg[^1];
                    }
                    break;
                default:
                    break;
            }

            while (Nodes[NodeIdx].RedirectNode >= 0)
                NodeIdx = Nodes[NodeIdx].RedirectNode;

            foreach (int childIdx in Nodes[NodeIdx].Clildren)
                CollectSignArguments(childIdx, last_arg, arguments);
        }

        internal class CommandNode
        {
            public byte Flags;
            public int[] Clildren;
            public int RedirectNode;
            public string? Name;
            public Parser? Paser;
            public string? SuggestionsType;
            public int ParserId; // Added for easy debug


            public CommandNode(byte Flags,
                        int[] Clildren,
                        int RedirectNode = -1,
                        string? Name = null,
                        Parser? Paser = null,
                        string? SuggestionsType = null,
                        int parserId = -1)
            {
                this.Flags = Flags;
                this.Clildren = Clildren;
                this.RedirectNode = RedirectNode;
                this.Name = Name;
                this.Paser = Paser;
                this.SuggestionsType = SuggestionsType;
                ParserId = parserId;
            }
        }

        internal abstract class Parser
        {
            public abstract string GetName();

            public abstract int GetArgCnt();

            public abstract bool Check(string text);
        }

        internal class ParserEmpty : Parser
        {

            public ParserEmpty(MinecraftDataTypes dataTypes, Queue<byte> packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "";
            }
        }

        internal class ParserFloat : Parser
        {
            private byte Flags;
            private float Min = float.MinValue, Max = float.MaxValue;

            public ParserFloat(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Flags = DataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = DataTypes.ReadNextFloat(packetData);
                if ((Flags & 0x02) > 0)
                    Max = DataTypes.ReadNextFloat(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:float";
            }
        }

        internal class ParserDouble : Parser
        {
            private byte Flags;
            private double Min = double.MinValue, Max = double.MaxValue;

            public ParserDouble(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Flags = DataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = DataTypes.ReadNextDouble(packetData);
                if ((Flags & 0x02) > 0)
                    Max = DataTypes.ReadNextDouble(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:double";
            }
        }

        internal class ParserInteger : Parser
        {
            private byte Flags;
            private int Min = int.MinValue, Max = int.MaxValue;

            public ParserInteger(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Flags = DataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = DataTypes.ReadNextInt(packetData);
                if ((Flags & 0x02) > 0)
                    Max = DataTypes.ReadNextInt(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:integer";
            }
        }

        internal class ParserLong : Parser
        {
            private byte Flags;
            private long Min = long.MinValue, Max = long.MaxValue;

            public ParserLong(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Flags = DataTypes.ReadNextByte(packetData);
                if ((Flags & 0x01) > 0)
                    Min = DataTypes.ReadNextLong(packetData);
                if ((Flags & 0x02) > 0)
                    Max = DataTypes.ReadNextLong(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:long";
            }
        }

        internal class ParserString : Parser
        {
            private StringType Type;

            private enum StringType { SINGLE_WORD, QUOTABLE_PHRASE, GREEDY_PHRASE };

            public ParserString(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Type = (StringType)DataTypes.ReadNextVarInt(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "brigadier:string";
            }
        }

        internal class ParserEntity : Parser
        {
            private byte Flags;

            public ParserEntity(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Flags = DataTypes.ReadNextByte(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:entity";
            }
        }

        internal class ParserBlockPos : Parser
        {

            public ParserBlockPos(MinecraftDataTypes dataTypes, Queue<byte> packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 3;
            }

            public override string GetName()
            {
                return "minecraft:block_pos";
            }
        }

        internal class ParserColumnPos : Parser
        {

            public ParserColumnPos(MinecraftDataTypes dataTypes, Queue<byte> packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 3;
            }

            public override string GetName()
            {
                return "minecraft:column_pos";
            }
        }

        internal class ParserVec3 : Parser
        {

            public ParserVec3(MinecraftDataTypes dataTypes, Queue<byte> packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 3;
            }

            public override string GetName()
            {
                return "minecraft:vec3";
            }
        }

        internal class ParserVec2 : Parser
        {

            public ParserVec2(MinecraftDataTypes dataTypes, Queue<byte> packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 2;
            }

            public override string GetName()
            {
                return "minecraft:vec2";
            }
        }

        internal class ParserRotation : Parser
        {

            public ParserRotation(MinecraftDataTypes dataTypes, Queue<byte> packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 2;
            }

            public override string GetName()
            {
                return "minecraft:rotation";
            }
        }

        internal class ParserMessage : Parser
        {
            public ParserMessage(MinecraftDataTypes dataTypes, Queue<byte> packetData) { }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:message";
            }
        }

        internal class ParserScoreHolder : Parser
        {
            private byte Flags;

            public ParserScoreHolder(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Flags = DataTypes.ReadNextByte(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:score_holder";
            }
        }

        internal class ParserRange : Parser
        {
            private bool Decimals;

            public ParserRange(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Decimals = DataTypes.ReadNextBool(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:range";
            }
        }

        internal class ParserResourceOrTag : Parser
        {
            private string Registry;

            public ParserResourceOrTag(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Registry = DataTypes.ReadNextString(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:resource_or_tag";
            }
        }

        internal class ParserResource : Parser
        {
            private string Registry;

            public ParserResource(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                Registry = DataTypes.ReadNextString(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:resource";
            }
        }

        /// <summary>
        /// Undocumented parser type for 1.19.4+
        /// </summary>
        internal class ParserTime : Parser
        {
            public ParserTime(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                DataTypes.ReadNextInt(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "minecraft:time";
            }
        }

        internal class ParserForgeEnum : Parser
        {
            public ParserForgeEnum(MinecraftDataTypes dataTypes, Queue<byte> packetData)
            {
                DataTypes.ReadNextString(packetData);
            }

            public override bool Check(string text)
            {
                return true;
            }

            public override int GetArgCnt()
            {
                return 1;
            }

            public override string GetName()
            {
                return "forge:enum";
            }
        }
    }
}
