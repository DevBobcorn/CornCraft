// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
namespace MagicaCloth
{
    /// <summary>
    /// FixedChunkNativeList / FixedMultiNativeListで使用するチャンク情報
    /// </summary>
    public struct ChunkData
    {
        public int chunkNo;

        /// <summary>
        /// データ配列のこのチャンクの開始インデックス
        /// </summary>
        public int startIndex;

        /// <summary>
        /// データ数
        /// </summary>
        public int dataLength;

        /// <summary>
        /// データ数内の使用されているローカルインデックス
        /// (FixedMultiNativeListで使用)
        /// </summary>
        public int useLength;

        public void Clear()
        {
            chunkNo = 0;
            startIndex = 0;
            dataLength = 0;
            useLength = 0;
        }

        public bool IsValid()
        {
            return dataLength > 0;
        }

        public override string ToString()
        {
            string str = string.Empty;
            str += "[chunkNo=" + chunkNo + ",startIndex=" + startIndex + ",dataLength=" + dataLength + ",useLength=" + useLength + "\n";
            return str;
        }
    }
}
