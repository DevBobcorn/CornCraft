// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{
    /// <summary>
    /// 配列の断片を管理する
    /// </summary>
    public struct DataChunk
    {
        /// <summary>
        /// 開始インデックス
        /// </summary>
        public int startIndex;

        /// <summary>
        /// データ数
        /// </summary>
        public int dataLength;

        public bool IsValid => dataLength > 0;

        public static DataChunk Empty
        {
            get
            {
                return new DataChunk();
            }
        }

        public DataChunk(int sindex, int length)
        {
            startIndex = sindex;
            dataLength = length;
        }

        public DataChunk(int sindex)
        {
            startIndex = sindex;
            dataLength = 1;
        }

        public void Clear()
        {
            startIndex = 0;
            dataLength = 0;
        }

        public override string ToString()
        {
            return $"[startIndex={startIndex}, dataLength={dataLength}]";
        }
    }
}
