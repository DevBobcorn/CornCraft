// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 各オブジェクトのデータハッシュ取得拡張
    /// データハッシュはGetHashCode()と違い、参照型でも値が不変となりデータ内容の比較に利用できます
    /// ・値型はそのままGetHashCode()を返す方式で問題ありません
    /// ・参照型で自分で定義したクラス／構造体は IDataHash インターフェースを継承し、int GetDataHash() を定義する必要があります
    /// ・参照型でシステムクラスの場合は、ここに拡張メソッドを定義してGetDataHash()を返す必要があります
    /// </summary>
    public static class DataHashExtensions
    {
        public const int NullHash = 397610387;
        public const int NumberHash = 932781045;

        /// <summary>
        /// すべてのObjectにGetDataHash()を定義
        /// デフォルトではGetHashCode()を返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetDataHash(this System.Object data)
        {
            var unityObj = data as UnityEngine.Object;
            if (!object.ReferenceEquals(unityObj, null))
            {
                if (unityObj != null)
                {
                    if (unityObj is Transform)
                        return (unityObj as Transform).name.GetHashCode();
                    else if (unityObj is GameObject)
                        return (unityObj as GameObject).name.GetHashCode();
                    else if (unityObj is Mesh)
                    {
                        // 頂点リストとトライアングルリストを調べる
                        var mesh = unityObj as Mesh;
                        int hash = 0;
                        hash += mesh.vertexCount.GetDataHash(); // 頂点数のみでよい
                        hash += mesh.triangles.Length.GetDataHash(); // トライアングル数のみでよい
                        hash += mesh.subMeshCount.GetDataHash();
                        hash += mesh.isReadable.GetDataHash();
                        return hash;
                    }
                    else
                        return NumberHash + data.GetHashCode();
                }
                else
                    return NullHash;
            }
            else
            {
                if (data != null)
                    return NumberHash + data.GetHashCode();
                else
                    return NullHash;
            }
        }

        public static int GetDataHash(this IDataHash data)
        {
            return data.GetDataHash();
        }

        //=========================================================================================
        /// <summary>
        /// ネイティブ配列からデータハッシュ値を求めて返す
        /// 自分で定義したクラス／構造体は IDataHash インターフェースを継承し、int GetDataHash() を定義する必要があります
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetDataHash<T>(this T[] data)
        {
            int hash = 0;
            if (data != null)
                foreach (var d in data)
                {
                    hash = hash * 31;

                    IDataHash dh = d as IDataHash;
                    if (dh != null)
                        hash += dh.GetDataHash();
                    else
                        hash += d.GetDataHash();
                }

            return hash;
        }

        /// <summary>
        /// リストからデータハッシュ値を求めて返す
        /// 自分で定義したクラス／構造体は IDataHash インターフェースを継承し、int GetDataHash() を定義する必要があります
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetDataHash<T>(this List<T> data)
        {
            int hash = 0;
            if (data != null)
                foreach (var d in data)
                {
                    hash = hash * 31;

                    IDataHash dh = d as IDataHash;
                    if (dh != null)
                        hash += dh.GetDataHash();
                    else
                        hash += d.GetDataHash();
                }

            return hash;
        }

        /// <summary>
        /// ネイティブ配列から配列数のデータハッシュを求めて返す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetDataCountHash<T>(this T[] data)
        {
            return data != null ? data.Length.GetDataHash() : NullHash;
        }

        /// <summary>
        /// リストからリスト数のデータハッシュを求めて返す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetDataCountHash<T>(this List<T> data)
        {
            return data != null ? data.Count.GetDataHash() : NullHash;
        }

        /// <summary>
        /// Vector3のデータハッシュを求めて返す
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static ulong GetVectorDataHash(Vector3 v)
        {
            var u = math.asuint(v);
            ulong x = u.x;
            ulong y = u.y;
            ulong z = u.z;
            x += 0x68DF0763u;
            y += 0x5A394F9Fu;
            z += 0xE094B323u;
            x = x ^ (x << 13);
            y = y ^ (y >> 17);
            z = z ^ (z << 15);
            x *= 0x9B13B92Du;
            y *= 0x4ABF0813u;
            z *= 0x86068063u;
            return x + y + z + 0xD75513F9u;
        }
    }
}
