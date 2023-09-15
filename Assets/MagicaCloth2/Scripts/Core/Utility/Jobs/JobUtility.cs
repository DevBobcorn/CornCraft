// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public static class JobUtility
    {
        //=========================================================================================
        /// <summary>
        /// 配列をValueで埋めるジョブを発行します
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="length"></param>
        /// <param name="value"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle Fill(NativeArray<int> array, int length, int value, JobHandle dependsOn = new JobHandle())
        {
            var job = new FillJob<int>() { value = value, array = array, };
            return job.Schedule(length, 32, dependsOn);
        }

        public static JobHandle Fill(NativeArray<Vector4> array, int length, Vector4 value, JobHandle dependsOn = new JobHandle())
        {
            var job = new FillJob<Vector4>() { value = value, array = array, };
            return job.Schedule(length, 32, dependsOn);
        }

        public static JobHandle Fill(NativeArray<VirtualMeshBoneWeight> array, int length, VirtualMeshBoneWeight value, JobHandle dependsOn = new JobHandle())
        {
            var job = new FillJob<VirtualMeshBoneWeight>() { value = value, array = array, };
            return job.Schedule(length, 32, dependsOn);
        }

        public static JobHandle Fill(NativeArray<byte> array, int length, byte value, JobHandle dependsOn = new JobHandle())
        {
            var job = new FillJob<byte>() { value = value, array = array, };
            return job.Schedule(length, 32, dependsOn);
        }

        public static void FillRun(NativeArray<int> array, int length, int value)
        {
            var job = new FillJob<int>() { value = value, array = array, };
            job.Run(length);
        }

        public static void FillRun(NativeArray<Vector4> array, int length, Vector4 value)
        {
            var job = new FillJob<Vector4>() { value = value, array = array, };
            job.Run(length);
        }

        public static void FillRun(NativeArray<quaternion> array, int length, quaternion value)
        {
            var job = new FillJob<quaternion>() { value = value, array = array, };
            job.Run(length);
        }

        public static void FillRun(NativeArray<VirtualMeshBoneWeight> array, int length, VirtualMeshBoneWeight value)
        {
            var job = new FillJob<VirtualMeshBoneWeight>() { value = value, array = array, };
            job.Run(length);
        }


        [BurstCompile]
        struct FillJob<T> : IJobParallelFor where T : unmanaged
        {
            public T value;

            [Unity.Collections.WriteOnly]
            public NativeArray<T> array;

            public void Execute(int index)
            {
                array[index] = value;
            }
        }

        /// <summary>
        /// 配列をValueで埋めるジョブを発行します(startIndexあり)
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <param name="value"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle Fill(NativeArray<int> array, int startIndex, int length, int value, JobHandle dependsOn = new JobHandle())
        {
            var job = new FillJob2<int>() { value = value, startIndex = startIndex, array = array, };
            return job.Schedule(length, 32, dependsOn);
        }

        [BurstCompile]
        struct FillJob2<T> : IJobParallelFor where T : unmanaged
        {
            public T value;
            public int startIndex;

            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<T> array;

            public void Execute(int index)
            {
                array[startIndex + index] = value;
            }
        }

        public static JobHandle Fill(NativeReference<int> reference, int value, JobHandle dependsOn = new JobHandle())
        {
            var job = new FillRefJob<int>() { value = value, reference = reference, };
            return job.Schedule(dependsOn);
        }

        [BurstCompile]
        struct FillRefJob<T> : IJob where T : unmanaged
        {
            public T value;

            [Unity.Collections.WriteOnly]
            public NativeReference<T> reference;

            public void Execute()
            {
                reference.Value = value;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 配列に連番を格納するジョブを発行します
        /// </summary>
        /// <param name="array"></param>
        /// <param name="length"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle SerialNumber(NativeArray<int> array, int length, JobHandle dependsOn = new JobHandle())
        {
            var job = new SerialNumberJob()
            {
                array = array,
            };
            return job.Schedule(length, 32, dependsOn);
        }

        public static void SerialNumberRun(NativeArray<int> array, int length)
        {
            var job = new SerialNumberJob()
            {
                array = array,
            };
            job.Run(length);

        }

        [BurstCompile]
        struct SerialNumberJob : IJobParallelFor
        {
            [Unity.Collections.WriteOnly]
            public NativeArray<int> array;

            public void Execute(int index)
            {
                array[index] = index;
            }
        }

        //=========================================================================================
        /// <summary>
        /// NativeHashSetのキーをNativeListに変換するジョブを発行します
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashSet"></param>
        /// <param name="list"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle ConvertHashSetToNativeList(NativeParallelHashSet<int> hashSet, NativeList<int> list, JobHandle dependsOn = new JobHandle())
        {
            var job = new ConvertHashSetToListJob<int>() { hashSet = hashSet, list = list, };
            return job.Schedule(dependsOn);
        }

        [BurstCompile]
        struct ConvertHashSetToListJob<T> : IJob where T : unmanaged, IEquatable<T>
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<T> hashSet;
            [Unity.Collections.WriteOnly]
            public NativeList<T> list;

            public void Execute()
            {
                foreach (var key in hashSet)
                {
                    list.AddNoResize(key);
                }
            }
        }

        //=========================================================================================
#if false
        /// <summary>
        /// NativeMultiHashMapのキーをNativeListに変換するジョブを発行する
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="hashMap"></param>
        /// <param name="keyList"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle ConvertMultiHashMapKeyToNativeList(
            NativeParallelMultiHashMap<int2, int> hashMap,
            NativeList<int2> keyList,
            JobHandle dependsOn = new JobHandle())
        {
            // todo:この処理は重い
            var job = new ConvertMultiHashMapKeyToListJob<int2, int>()
            {
                hashMap = hashMap,
                list = keyList,
            };
            return job.Schedule(dependsOn);
        }

        [BurstCompile]
        struct ConvertMultiHashMapKeyToListJob<T, U> : IJob where T : unmanaged, IEquatable<T> where U : unmanaged
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelMultiHashMap<T, U> hashMap;
            [Unity.Collections.WriteOnly]
            public NativeList<T> list;

            public void Execute()
            {
                var keySet = new NativeParallelHashSet<T>(hashMap.Count(), Allocator.Temp); // ここが問題となる可能性がある(unity2023.1.5事件)
                var keyArray = hashMap.GetKeyArray(Allocator.Temp); // ここが問題となる可能性がある(unity2023.1.5事件)
                // GetKeyArray()の結果はキーが重複しまた順不同なので注意！
                for (int i = 0; i < keyArray.Length; i++)
                    keySet.Add(keyArray[i]);

                foreach (var key in keySet)
                    list.Add(key);
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// NativeHashSetの内容をNativeListに変換するジョブを発行する
        /// ジェネリック型ジョブは明示的に型を<T>で指定する必要があるため型ごとに関数が発生します
        /// </summary>
        /// <param name="hashSet"></param>
        /// <param name="keyList"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle ConvertHashSetKeyToNativeList(
            NativeParallelHashSet<int2> hashSet,
            NativeList<int2> keyList,
            JobHandle dependsOn = new JobHandle())
        {
            var job = new ConvertHashSetKeyToListJob<int2>() { hashSet = hashSet, list = keyList, };
            return job.Schedule(dependsOn);
        }

        public static JobHandle ConvertHashSetKeyToNativeList(
            NativeParallelHashSet<int4> hashSet,
            NativeList<int4> keyList,
            JobHandle dependsOn = new JobHandle())
        {
            var job = new ConvertHashSetKeyToListJob<int4>() { hashSet = hashSet, list = keyList, };
            return job.Schedule(dependsOn);
        }

        [BurstCompile]
        struct ConvertHashSetKeyToListJob<T> : IJob where T : unmanaged, IEquatable<T>
        {
            [Unity.Collections.ReadOnly]
            public NativeParallelHashSet<T> hashSet;
            [Unity.Collections.WriteOnly]
            public NativeList<T> list;

            public void Execute()
            {
                // 順不同なので注意！
                foreach (var key in hashSet)
                    list.Add(key);
            }
        }

        //=========================================================================================
        /// <summary>
        /// AABBを計算して返すジョブを発行する(NativeArray)
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="length"></param>
        /// <param name="outAABB"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle CalcAABB(NativeArray<float3> positions, int length, NativeReference<AABB> outAABB, JobHandle dependsOn = new JobHandle())
        {
            var job = new CalcAABBJob()
            {
                length = length,
                positions = positions,
                outAABB = outAABB,
            };
            return job.Schedule(dependsOn);
        }

        public static void CalcAABBRun(NativeArray<float3> positions, int length, NativeReference<AABB> outAABB)
        {
            var job = new CalcAABBJob()
            {
                length = length,
                positions = positions,
                outAABB = outAABB,
            };
            job.Run();
        }

        /// <summary>
        /// AABBを計算して返すジョブを発行する(NativeList)
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="outAABB"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle CalcAABB(NativeList<float3> positions, NativeReference<AABB> outAABB, JobHandle dependsOn = new JobHandle())
        {
            var job = new CalcAABBDeferJob()
            {
                positions = positions,
                outAABB = outAABB,
            };
            return job.Schedule(dependsOn);
        }

        public static void CalcAABBRun(NativeList<float3> positions, NativeReference<AABB> outAABB)
        {
            var job = new CalcAABBDeferJob()
            {
                positions = positions,
                outAABB = outAABB,
            };
            job.Run();
        }

        [BurstCompile]
        struct CalcAABBJob : IJob
        {
            public int length;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;

            public NativeReference<AABB> outAABB;

            public void Execute()
            {
                outAABB.Value = CalcAABBInternal(positions, length);
            }
        }

        [BurstCompile]
        struct CalcAABBDeferJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeList<float3> positions;

            public NativeReference<AABB> outAABB;

            public void Execute()
            {
                outAABB.Value = CalcAABBInternal(positions.AsArray(), positions.Length);
            }
        }

        static AABB CalcAABBInternal(in NativeArray<float3> positions, int length)
        {
            if (positions.Length == 0)
            {
                return new AABB();
            }

            //float3 min = 0;
            //float3 max = 0;
            float3 min = float.MaxValue;
            float3 max = float.MinValue;

            for (int i = 0; i < length; i++)
            {
                float3 pos = positions[i];
                min = math.min(min, pos);
                max = math.max(max, pos);
            }

            var aabb = new AABB(min, max);
            return aabb;
        }

        //=========================================================================================
        /// <summary>
        /// スフィアマッピングを行いUVを算出するジョブを発行する
        /// このUVは接線計算用でありテクスチャ用ではないので注意！
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="length"></param>
        /// <param name="aabb"></param>
        /// <param name="outUVs"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle CalcUVWithSphereMapping(NativeArray<float3> positions, int length, NativeReference<AABB> aabb, NativeArray<float2> outUVs, JobHandle dependsOn = new JobHandle())
        {
            var job = new CalcUVJob()
            {
                positions = positions,
                aabb = aabb,
                uvs = outUVs,
            };
            return job.Schedule(length, 32, dependsOn);
        }

        public static void CalcUVWithSphereMappingRun(NativeArray<float3> positions, int length, NativeReference<AABB> aabb, NativeArray<float2> outUVs)
        {
            var job = new CalcUVJob()
            {
                positions = positions,
                aabb = aabb,
                uvs = outUVs,
            };
            job.Run(length);
        }

        [BurstCompile]
        struct CalcUVJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeReference<AABB> aabb;

            [Unity.Collections.WriteOnly]
            public NativeArray<float2> uvs;

            public void Execute(int index)
            {
                // 中心位置からのスフィアマッピング
                float3 lv = positions[index] - aabb.Value.Center;
                lv = math.normalize(lv);

                float u = math.atan2(lv.x, lv.z);
                u = math.clamp(math.unlerp(-math.PI, math.PI, u), 0.0f, 1.0f);

                float v = math.dot(math.up(), lv);
                v = math.clamp(math.unlerp(1.0f, -1.0f, v), 0.0f, 1.0f);

                //float2 uv = new float2(u, v); // こちらは横方向ベースになる
                float2 uv = new float2(v, u); // こちらは縦方向ベースになる

#if false
                float len = math.length(lv);
                float add = index * 0.001234f; // UVを微妙にずらすための加算値
                uv += len * 0.01f + add;
#endif
#if false
                //float len = math.length(lv);
                float add = index * 0.0001234f; // UVを微妙にずらすための加算値
                //uv += len * 0.1f + add;
                uv += add;
#endif
#if true
                // 方向ベクトル上に同じUVが生成されてしまうのを避けるためUVに距離を加算してずらす
                float add = index * 0.0001234f; // UVを微妙にずらすための加算値

                // 頂点間のUVの間隔を広めに取り、同じUVが発生しないようにインデックスを使い微妙に値をずらす
                uv = uv * 10.0f + add;
#endif

                //Debug.Log($"[{index}] {uv}");
                uvs[index] = uv;
            }
        }

        //=========================================================================================
        /// <summary>
        /// intデータを加算して新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        public struct AddIntDataCopyJob : IJobParallelFor
        {
            public int dstOffset;
            public int addData;

            // src
            [Unity.Collections.ReadOnly]
            public NativeArray<int> srcData;

            // dst
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int> dstData;

            public void Execute(int index)
            {
                int dindex = dstOffset + index;
                var data = srcData[index];
                data += addData;
                dstData[dindex] = data;
            }
        }

        /// <summary>
        /// int2データを加算して新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        public struct AddInt2DataCopyJob : IJobParallelFor
        {
            public int dstOffset;
            public int2 addData;

            // src
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> srcData;

            // dst
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int2> dstData;

            public void Execute(int index)
            {
                int dindex = dstOffset + index;
                var data = srcData[index];
                data += addData;
                dstData[dindex] = data;
            }
        }

        /// <summary>
        /// int3データを加算して新しい領域にコピーする
        /// </summary>
        [BurstCompile]
        public struct AddInt3DataCopyJob : IJobParallelFor
        {
            public int dstOffset;
            public int3 addData;

            // src
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> srcData;

            // dst
            [NativeDisableParallelForRestriction]
            [Unity.Collections.WriteOnly]
            public NativeArray<int3> dstData;

            public void Execute(int index)
            {
                int dindex = dstOffset + index;
                var data = srcData[index];
                data += addData;
                dstData[dindex] = data;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 座標を変換するジョブを発行します
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="length"></param>
        /// <param name="toM"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static JobHandle TransformPosition(NativeArray<float3> positions, int length, in float4x4 toM, JobHandle dependsOn = new JobHandle())
        {
            var job = new TransformPositionJob() { toM = toM, positions = positions, };
            return job.Schedule(length, 32, dependsOn);
        }

        public static void TransformPositionRun(NativeArray<float3> positions, int length, in float4x4 toM)
        {
            var job = new TransformPositionJob() { toM = toM, positions = positions, };
            job.Run(length);
        }

        public static JobHandle TransformPosition(NativeArray<float3> srcPositions, NativeArray<float3> dstPositions, int length, in float4x4 toM, JobHandle dependsOn = new JobHandle())
        {
            var job = new TransformPositionJob2() { toM = toM, srcPositions = srcPositions, dstPositions = dstPositions };
            return job.Schedule(length, 32, dependsOn);
        }

        public static void TransformPositionRun(NativeArray<float3> srcPositions, NativeArray<float3> dstPositions, int length, in float4x4 toM)
        {
            var job = new TransformPositionJob2() { toM = toM, srcPositions = srcPositions, dstPositions = dstPositions };
            job.Run(length);
        }

        [BurstCompile]
        public struct TransformPositionJob : IJobParallelFor
        {
            public float4x4 toM;
            public NativeArray<float3> positions;

            public void Execute(int vindex)
            {
                positions[vindex] = MathUtility.TransformPoint(positions[vindex], toM);
            }
        }

        [BurstCompile]
        public struct TransformPositionJob2 : IJobParallelFor
        {
            public float4x4 toM;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> srcPositions;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> dstPositions;

            public void Execute(int vindex)
            {
                dstPositions[vindex] = MathUtility.TransformPoint(srcPositions[vindex], toM);
            }
        }

        //=========================================================================================
        /// <summary>
        /// スタートインデックス＋データ数とデータの２つの配列からHashMapを構築して返す
        /// </summary>
        /// <param name="indexArray"></param>
        /// <param name="dataArray"></param>
        /// <returns></returns>
        public static NativeParallelMultiHashMap<int, ushort> ToNativeMultiHashMap(in NativeArray<uint> indexArray, in NativeArray<ushort> dataArray)
        {
            var map = new NativeParallelMultiHashMap<int, ushort>(dataArray.Length, Allocator.Persistent);

            var job = new ConvertArrayToMapJob<ushort>()
            {
                indexArray = indexArray,
                dataArray = dataArray,
                map = map,
            };
            job.Run();

            return map;
        }

        [BurstCompile]
        struct ConvertArrayToMapJob<TData> : IJob where TData : unmanaged
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> indexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<TData> dataArray;

            [Unity.Collections.WriteOnly]
            public NativeParallelMultiHashMap<int, TData> map;

            public void Execute()
            {
                int cnt = indexArray.Length;
                for (int i = 0; i < cnt; i++)
                {
                    DataUtility.Unpack12_20(indexArray[i], out var dcnt, out var dstart);

                    for (int j = 0; j < dcnt; j++)
                    {
                        var data = dataArray[dstart + j];
                        map.Add(i, data);
                    }
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// NativeReferenceをクリアするジョブを発行する
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public static JobHandle ClearReference(NativeReference<int> reference, JobHandle jobHandle)
        {
            var job = new ClearReferenceJob()
            {
                reference = reference,
            };
            return job.Schedule(jobHandle);
        }

        [BurstCompile]
        struct ClearReferenceJob : IJob
        {
            public NativeReference<int> reference;

            public void Execute()
            {
                reference.Value = 0;
            }
        }
    }
}
