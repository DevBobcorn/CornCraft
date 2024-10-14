#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Type definition for "bitfield"
    /// </summary>
    public class UtilType_bitfield : TemplateType_memberContainer
    {
        public UtilType_bitfield(ResourceLocation typeId,
                (string n, int sz, bool s)[] fieldArray) : base(typeId)
        {
            bitfieldEntries = fieldArray.Select(a =>
                    new BitFieldEntry(a.n, a.sz, a.s)).ToArray();

            totalBitCount = fieldArray.Sum(a => a.sz);

            if (totalBitCount < 0 || totalBitCount % 8  != 0)
            {
                throw new InvalidDataException($"Total bit count must not be negative and must be a mutiple of 8. Got {totalBitCount}.");
            }
        }

        class BitFieldEntry
        {
            public BitFieldEntry(string name, int size, bool signed)
            {
                Name = name;
                Size = size;
                Signed = signed;

                if (string.IsNullOrEmpty(Name))
                {
                    throw new InvalidDataException("Bitfield entry must have a name!");
                }
            }

            public readonly string Name;
            public readonly int Size;
            public readonly bool Signed;
        }

        private readonly BitFieldEntry[] bitfieldEntries;
        private readonly int totalBitCount;

        public override Dictionary<string, object?> ReadValueAsType(PacketRecord rec, string parentPath, Queue<byte> cache)
        {
            var byteCount = totalBitCount / 8;
            var data = DataTypes.ReadData(byteCount, cache);

            // Field name -> field value
            var bitfieldDict = new Dictionary<string, object?>();

            int bitIndex = 0;

            for (int i = 0; i < bitfieldEntries.Length; i++)
            {
                var entry = bitfieldEntries[i];
                var bits = 0L;

                for (int b = 0; b < entry.Size; b++)
                {
                    // Get the bit at bit index. (bitIndex / 8) is the byte
                    // index, and right shifting (7 - (bitIndex % 8)) moves
                    // the target bit to the lowest bit, &1 to mask the bit
                    var bit = (data[bitIndex / 8] >> (7 - (bitIndex % 8))) & 1;

                    // Left shift previously read bits, and append the new bit
                    #pragma warning disable CS0675
                    // Disable CS0675 because it can only be either 0x0 or 0x1
                    bits = (bits << 1) | bit;
                    #pragma warning restore CS0675

                    bitIndex += 1;
                }

                if (entry.Signed)
                {
                    if ((bits & (1L << (entry.Size - 1))) != 0)
                    {
                        bits -= (1L << entry.Size);
                    }
                    else // long is not negative
                    {
                        // Do nothing
                    }
                }

                bitfieldDict.Add(entry.Name, bits);

                rec.WriteEntry(parentPath, entry.Name, I64_ID, bits); // Use type id of i64
            }

            return bitfieldDict;
        }
    }
}
