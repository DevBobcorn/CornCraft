using CraftSharp.Protocol.Handlers;

namespace CraftSharp.Protocol.ProtoDef
{
    public class RawByteArray
    {
        public readonly byte[] Data;

        public RawByteArray(byte[] data)
        {
            Data = data;
        }

        public override string ToString()
        {
            return DataTypes.ByteArrayToString(Data);
        }
    }
}
