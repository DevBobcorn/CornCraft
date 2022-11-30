// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 複数の可変リストを１つのNativeArrayとして扱えるようにしたもの
    /// NativeMultiHashMapの代わりとして使用可能。
    /// NativeMultiHashMapとの違いは追加したデータ順が保持されること。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedMultiNativeList<T> : IDisposable where T : struct
    {
        /// <summary>
        /// ネイティブリスト
        /// </summary>
        NativeArray<T> nativeArray;

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 空インデックススタック
        /// </summary>
        List<ChunkData> emptyChunkList = new List<ChunkData>();

        /// <summary>
        /// 使用インデックスセット
        /// </summary>
        List<ChunkData> useChunkList = new List<ChunkData>();

        int chunkSeed;

        int initLength = 64;

        T emptyElement;

        int useLength;

        //=========================================================================================
        public FixedMultiNativeList()
        {
            nativeArray = new NativeArray<T>(initLength, Allocator.Persistent);
            nativeLength = nativeArray.Length;
            useLength = 0;
        }

        public void Dispose()
        {
            if (nativeArray.IsCreated)
            {
                nativeArray.Dispose();
            }
            nativeLength = 0;
            useLength = 0;
            emptyChunkList.Clear();
            useChunkList.Clear();
        }

        public void SetEmptyElement(T empty)
        {
            emptyElement = empty;
        }

        //=========================================================================================
        /// <summary>
        /// データチャンクの追加
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public ChunkData AddChunk(int length)
        {
            // 再利用チェック
            for (int i = 0; i < emptyChunkList.Count; i++)
            {
                var cdata = emptyChunkList[i];
                if (cdata.dataLength >= length)
                {
                    // このチャンクを再利用する
                    int remainder = cdata.dataLength - length;
                    if (remainder > 0)
                    {
                        // 分割
                        var rchunk = new ChunkData()
                        {
                            chunkNo = ++chunkSeed,
                            startIndex = cdata.startIndex + length,
                            dataLength = remainder,
                        };
                        emptyChunkList[i] = rchunk;
                    }
                    else
                    {
                        emptyChunkList.RemoveAt(i);
                    }
                    cdata.dataLength = length;

                    // 使用リストに追加
                    useChunkList.Add(cdata);

                    return cdata;
                }
            }

            // 新規追加
            var data = new ChunkData()
            {
                chunkNo = ++chunkSeed,
                startIndex = useLength,
                dataLength = length,
                useLength = 0,
            };
            useChunkList.Add(data);
            useLength += length;

            if (nativeArray.Length < useLength)
            {
                // 拡張
                int len = nativeArray.Length;
                while (len < useLength)
                    len += Mathf.Min(len, 4096);
                var nativeArray2 = new NativeArray<T>(len, Allocator.Persistent);
                nativeArray2.CopyFromFast(nativeArray);
                nativeArray.Dispose();

                nativeArray = nativeArray2;
                nativeLength = nativeArray.Length;
            }

            return data;
        }

        /// <summary>
        /// データチャンクの削除
        /// </summary>
        /// <param name="chunkNo"></param>
        public void RemoveChunk(int chunkNo)
        {
            for (int i = 0; i < useChunkList.Count; i++)
            {
                if (useChunkList[i].chunkNo == chunkNo)
                {
                    // このチャンクを削除する
                    var cdata = useChunkList[i];
                    useChunkList.RemoveAt(i);

                    // データクリア
                    nativeArray.SetValue(cdata.startIndex, cdata.dataLength, emptyElement);

                    // 前後の未使用チャンクと接続できるなら結合する
                    for (int j = 0; j < emptyChunkList.Count;)
                    {
                        var edata = emptyChunkList[j];
                        if ((edata.startIndex + edata.dataLength) == cdata.startIndex)
                        {
                            // 結合
                            edata.dataLength += cdata.dataLength;
                            cdata = edata;
                            emptyChunkList.RemoveAt(j);
                            continue;
                        }
                        else if (edata.startIndex == (cdata.startIndex + cdata.dataLength))
                        {
                            // 結合
                            cdata.dataLength += edata.dataLength;
                            emptyChunkList.RemoveAt(j);
                            continue;
                        }

                        j++;
                    }

                    // チャンクを空リストに追加
                    emptyChunkList.Add(cdata);

                    return;
                }
            }
        }

        /// <summary>
        /// データチャンクの削除
        /// </summary>
        /// <param name="chunk"></param>
        public void RemoveChunk(ChunkData chunk)
        {
            RemoveChunk(chunk.chunkNo);
        }

        /// <summary>
        /// チャンクにデータを追加する
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ChunkData AddData(ChunkData chunk, T data)
        {
            if (chunk.useLength == chunk.dataLength)
            {
                // 拡張

                // 新しいチャンクを割り当て
                int len = chunk.dataLength;
                len += Mathf.Min(len, 4096);
                var newChunk = AddChunk(len);

                // 現在のデータをコピーする
                nativeArray.CopyBlock(chunk.startIndex, newChunk.startIndex, chunk.dataLength);
                newChunk.useLength = chunk.useLength;

                // 現在のチャンクを破棄する
                RemoveChunk(chunk);
                chunk = newChunk;
            }

            nativeArray[chunk.startIndex + chunk.useLength] = data;
            chunk.useLength++;

            return chunk;
        }

        /// <summary>
        /// チャンクからデータを削除する
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ChunkData RemoveData(ChunkData chunk, T data)
        {
            // ※少し効率は悪いが検索して破棄する
            int index = chunk.startIndex;
            for (int i = 0; i < chunk.useLength; i++, index++)
            {
                if (data.Equals(nativeArray[index]))
                {
                    // Swap Backで削除
                    if (i < (chunk.useLength - 1))
                    {
                        nativeArray[index] = nativeArray[chunk.startIndex + chunk.useLength - 1];
                        nativeArray[chunk.startIndex + chunk.useLength - 1] = emptyElement;
                    }
                    chunk.useLength--;
                }
            }

            return chunk;
        }

        /// <summary>
        /// チャンクのデータをすべてクリアする
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public ChunkData ClearData(ChunkData chunk)
        {
            nativeArray.SetValue(chunk.startIndex, chunk.dataLength, emptyElement);
            chunk.useLength = 0;
            return chunk;
        }

        /// <summary>
        /// チャンクのデータに対してアクションを実行
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="act"></param>
        public void Process(ChunkData chunk, Action<T> act)
        {
            int index = chunk.startIndex;
            for (int i = 0; i < chunk.useLength; i++, index++)
            {
                act(nativeArray[index]);
            }
        }

        /// <summary>
        /// 確保されているネイティブ配列の要素数を返す
        /// </summary>
        public int Length
        {
            get
            {
                return nativeLength;
            }
        }

        /// <summary>
        /// 実際に利用されているチャンク数を返す
        /// </summary>
        public int ChunkCount
        {
            get
            {
                return useChunkList.Count;
            }
        }

        /// <summary>
        /// 実際に使用されている要素数を返す
        /// </summary>
        public int Count
        {
            get
            {
                int cnt = 0;
                foreach (var c in useChunkList)
                    cnt += c.dataLength;
                return cnt;
            }
        }

        public T this[int index]
        {
            get
            {
                return nativeArray[index];
            }
            //set
            //{
            //    nativeArray[index] = value;
            //}
        }

        //public void Clear()
        //{
        //    if (nativeArray0.IsCreated)
        //        nativeArray0.Dispose();
        //    nativeArray0 = new NativeArray<T>(initLength, Allocator.Persistent);
        //    nativeLength = initLength;
        //    useLength = 0;
        //    emptyChunkList.Clear();
        //    useChunkList.Clear();
        //}

        //public T[] ToArray()
        //{
        //    return nativeArray.ToArray();
        //}

        /// <summary>
        /// Jobで利用する場合はこの関数でNativeArrayに変換して受け渡す
        /// </summary>
        /// <returns></returns>
        public NativeArray<T> ToJobArray()
        {
            return nativeArray;
        }

        //=========================================================================================
        public override string ToString()
        {
            string str = string.Empty;

            str += "nativeList length=" + Length + "\n";
            str += "use chunk count=" + ChunkCount + "\n";
            str += "empty chunk count=" + emptyChunkList.Count + "\n";

            str += "<< use chunks >>\n";
            foreach (var cdata in useChunkList)
            {
                str += cdata;
            }

            str += "<< empty chunks >>\n";
            foreach (var cdata in emptyChunkList)
            {
                str += cdata;
            }

            return str;
        }
    }
}
