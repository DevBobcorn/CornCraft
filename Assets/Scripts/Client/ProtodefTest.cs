using CraftSharp.Protocol.ProtoDef;
using CraftSharp.Protocol.ProtoDef.Tests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CraftSharp
{
    public class ProtodefTest : MonoBehaviour
    {
        private static readonly JsonSerializerSettings serializerSettings = new()
        {
            Formatting = Formatting.Indented,
        };

        void Start()
        {
            List<string> protocolNames = new() { "d", "e", "f", "g", "h" };
            //List<string> protocolNames = new() { "751" };

            foreach (string protocolName in protocolNames)
            {
                TestProtocol(protocolName);
            }
        }

        private static void TestProtocol(string name)
        {
            var jsonPath = PathHelper.GetExtraDataFile($"protos{Path.DirectorySeparatorChar}protocol-{name}.json");
            var jsonText = File.ReadAllText(jsonPath);

            //Console.WriteLine($"Testing protocol-{name} ==========================================================");

            var jsonDoc = JsonConvert.DeserializeObject<JObject>(jsonText)!;
            PacketDefTypeHandlerBase.RegisterTypesRecursive(null, jsonDoc);

            var loaded = PacketDefTypeHandlerBase.LOADED_DEF_TYPES;

            //Console.WriteLine();

            var testPacket = new DummyPacketBuffer();

            switch (name)
            {
                case "d":
                    testPacket.WriteVarInt(123456);
                    testPacket.WriteString("Hello, world!");
                    testPacket.WritePString("喵呜喵呜", 42);
                    testPacket.WriteShort(1337);
                    testPacket.WriteString("Ciallo～(∠·ω< )⌒★");
                    testPacket.WriteVarInt(123456);
                    break;
                case "e":
                    testPacket.WriteByte(1);
                    testPacket.WriteString("Moew~");
                    testPacket.WriteByte(2);
                    testPacket.WriteUInt(32);
                    testPacket.WriteByte(3);
                    testPacket.WriteVarInt(233);
                    break;
                case "f":
                    testPacket.WriteShort(1);
                    testPacket.WritePString("Hi!", 42);
                    testPacket.WriteShort(2);
                    testPacket.WriteInt(1568354);
                    testPacket.WriteShort(0);
                    testPacket.WriteUShort(42);
                    break;
                case "g":
                    testPacket.WriteString(jsonText[..100] + "...");
                    testPacket.WriteLocation(new Location(18357644, 831, -20882616));
                    testPacket.WriteString("UwU");
                    testPacket.WriteLocation(new Location(42, 0, -2077));
                    testPacket.WritePString("OwO", 21);
                    testPacket.WriteLocation(new Location(0x1FFFFFF, 0x800, 0x2000000));
                    break;
                case "h":
                    testPacket.WriteByte(101);
                    testPacket.WriteByte(103);
                    testPacket.WriteByte(102);
                    testPacket.WriteVarInt(1234567);
                    testPacket.WriteString("MEOW");
                    testPacket.WriteInt(2300);
                    break;
            }
            
            var testPacketBytes = testPacket.GetBufferByteQueueClone();

            var testPacketRecord = new PacketRecord();
            var testPacketHandler = loaded[new ResourceLocation("universe/dreamland", "dream_thing")]!;

            var packetValue = testPacketHandler.ReadValue(testPacketRecord, string.Empty, testPacketBytes);

            //Console.WriteLine();

            Debug.Log(JsonConvert.SerializeObject(packetValue, serializerSettings));

            PacketDefTypeHandlerBase.ResetLoadedTypes();
        }
    }
}
