using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CraftSharp.Protocol.Handlers;
using CraftSharp.Protocol.Handlers.PacketPalettes;

namespace CraftSharp
{
    public class ProtodefExport : MonoBehaviour
    {
        private static readonly JsonSerializerSettings serializerSettings = new()
        {
            Formatting = Formatting.Indented,
        };

        void Start()
        {
            ExportProtocol("765", new PacketPalette1204(), false);
        }

        public static Dictionary<V, K> GetReversed<K, V>(IDictionary<K, V> dict)
        {
            var inverseDict = new Dictionary<V, K>();
            foreach (var kvp in dict)
            {
                if (!inverseDict.ContainsKey(kvp.Value))
                {
                    inverseDict.Add(kvp.Value, kvp.Key);
                }
            }
            return inverseDict;
        }

        private static void ExportProtocol(string protocolName, PacketTypePalette palette, bool inBound)
        {
            var jsonPath = PathHelper.GetExtraDataFile($"protos{Path.DirectorySeparatorChar}protocol-{protocolName}.json");
            var jsonText = File.ReadAllText(jsonPath);
            var jsonDoc = JsonConvert.DeserializeObject<JObject>(jsonText)!;

            var dir = inBound ? "toClient" : "toServer";

            var sb = new StringBuilder();
            var mappings = jsonDoc["play"][dir]["types"]["packet"][1][0]["type"][1]["mappings"];

            var num2Id = new Dictionary<int, string>();

            foreach (var type in (mappings as JObject)!.Properties())
            {
                int numId = Convert.ToInt32(type.Name, 16);
                num2Id.Add(numId, type.Value.ToString());
            }

            if (inBound)
            {
                var revLookup = GetReversed(palette.GetMappingIn());

                foreach (PacketTypesIn typeCs in Enum.GetValues(typeof (PacketTypesIn)))
                {
                    var nameInBrackets = $"[PacketTypesIn.{Enum.GetName(typeof (PacketTypesIn), typeCs)}]";
                    string packetId = string.Empty;

                    if (revLookup.ContainsKey(typeCs))
                    {
                        var numId = revLookup[typeCs];
                        packetId = "packet_" + num2Id.GetValueOrDefault(numId);
                    }

                    sb.AppendLine($"            {nameInBrackets,-45} = new ResourceLocation(\"play/{dir}\", \"{packetId}\"),");
                }
            }
            else
            {
                var revLookup = GetReversed(palette.GetMappingOut());

                foreach (PacketTypesOut typeCs in Enum.GetValues(typeof (PacketTypesOut)))
                {
                    var nameInBrackets = $"[PacketTypesOut.{Enum.GetName (typeof(PacketTypesOut), typeCs)}]";
                    string packetId = string.Empty;

                    if (revLookup.ContainsKey(typeCs))
                    {
                        var numId = revLookup[typeCs];
                        packetId = "packet_" + num2Id.GetValueOrDefault(numId);
                    }

                    sb.AppendLine($"            {nameInBrackets,-45} = new ResourceLocation(\"play/{dir}\", \"{packetId}\"),");
                }
            }

            GUIUtility.systemCopyBuffer = sb.ToString();
        }
    }
}
