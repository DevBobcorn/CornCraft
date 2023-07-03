// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// Nativeバッファへのインターロック書き込み制御関連
    /// </summary>
    public static class InterlockUtility
    {
        /// <summary>
        /// 固定小数点への変換倍率
        /// </summary>
        const int ToFixed = 100000;

        /// <summary>
        /// 少数への復元倍率
        /// </summary>
        const float ToFloat = 0.00001f;


        //=========================================================================================
        /// <summary>
        /// 集計バッファの指定インデックスにfloat3を固定小数点として加算しカウンタをインクリメントする
        /// </summary>
        /// <param name="index"></param>
        /// <param name="add"></param>
        /// <param name="cntPt"></param>
        /// <param name="sumPt"></param>
        unsafe internal static void AddFloat3(int index, float3 add, int* cntPt, int* sumPt)
        {
            Interlocked.Increment(ref cntPt[index]);
            int3 iadd = (int3)(add * ToFixed);
            //Debug.Log($"InterlockAdd [{index}]:{iadd}");
            index *= 3;
            for (int i = 0; i < 3; i++, index++)
            {
                if (iadd[i] != 0)
                    Interlocked.Add(ref sumPt[index], iadd[i]);
            }
        }

        /// <summary>
        /// 集計バッファの指定インデックスにfloat3を固定小数点として加算する（カウントは操作しない）
        /// </summary>
        /// <param name="index"></param>
        /// <param name="add"></param>
        /// <param name="sumPt"></param>
        unsafe internal static void AddFloat3(int index, float3 add, int* sumPt)
        {
            int3 iadd = (int3)(add * ToFixed);
            index *= 3;
            for (int i = 0; i < 3; i++, index++)
            {
                if (iadd[i] != 0)
                    Interlocked.Add(ref sumPt[index], iadd[i]);
            }
        }

        /// <summary>
        /// 集計バッファのカウンタのみインクリメントする
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cntPt"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static void Increment(int index, int* cntPt)
        {
            Interlocked.Increment(ref cntPt[index]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static void Max(int index, float value, int* pt)
        {
            int ival = (int)value * ToFixed;
            int now = pt[index];
            int oldNow = now + 1;

            while (ival > now && now != oldNow)
            {
                oldNow = now;
                now = Interlocked.CompareExchange(ref pt[index], ival, now);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 ReadAverageFloat3(int index, in NativeArray<int> countArray, in NativeArray<int> sumArray)
        {
            int count = countArray[index];
            if (count == 0)
                return 0;

            int dataIndex = index * 3;

            // 集計
            float3 add = new float3(sumArray[dataIndex], sumArray[dataIndex + 1], sumArray[dataIndex + 2]);
            add /= count;

            // データは固定小数点なので戻す
            add *= ToFloat;

            return add;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 ReadFloat3(int index, in NativeArray<int> bufferArray)
        {
            int dataIndex = index * 3;
            float3 v = new float3(bufferArray[dataIndex], bufferArray[dataIndex + 1], bufferArray[dataIndex + 2]);

            // データは固定小数点なので戻す
            v *= ToFloat;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float ReadFloat(int index, in NativeArray<int> bufferArray)
        {
            return bufferArray[index] * ToFloat;
        }

#if false
        /// <summary>
        /// 指定アドレスにfloat値を加算する
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        unsafe public static void AddFloat(int* pt, int index, float value)
        {
            float current = UnsafeUtility.ReadArrayElement<float>(pt, index);
            int currenti = math.asint(current);
            while (true)
            {
                float next = current + value;
                int nexti = math.asint(next);
                int prev = Interlocked.CompareExchange(ref pt[index], nexti, currenti);
                if (prev == currenti)
                    return;
                else
                {
                    currenti = prev;
                    current = math.asfloat(prev);
                }
            }
        }
#endif

        //=========================================================================================
        /// <summary>
        /// 加算集計バッファを平均化してnextPosに加算する
        /// </summary>
        /// <param name="particleList"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal static JobHandle SolveAggregateBufferAndClear(in NativeList<int> particleList, float velocityAttenuation, JobHandle jobHandle)
        {
            var sm = MagicaManager.Simulation;

            if (velocityAttenuation > 1e-06f)
            {
                // 速度影響あり
                var job = new AggregateWithVelocityJob()
                {
                    jobParticleIndexList = particleList,

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    velocityPosArray = sm.velocityPosArray.GetNativeArray(),

                    velocityAttenuation = velocityAttenuation,
                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                };
                jobHandle = job.Schedule(particleList, 16, jobHandle);
            }
            else
            {
                // 速度影響なし
                var job = new AggregateJob()
                {
                    //velocityLimit = velocityLimit,
                    jobParticleIndexList = particleList,

                    nextPosArray = sm.nextPosArray.GetNativeArray(),

                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                };
                jobHandle = job.Schedule(particleList, 16, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct AggregateJob : IJobParallelForDefer
        {
            // 速度制限
            //public float velocityLimit;

            [Unity.Collections.ReadOnly]
            public NativeList<int> jobParticleIndexList;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // aggregate

            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
                int pindex = jobParticleIndexList[index];

                int count = countArray[pindex];
                if (count == 0)
                    return;

                int dataIndex = pindex * 3;

                // 集計
                float3 add = new float3(sumArray[dataIndex], sumArray[dataIndex + 1], sumArray[dataIndex + 2]);
                add /= count;

                // データは固定小数点なので戻す
                add *= ToFloat;

                // 速度制限
                //add = MathUtility.ClampVector(add, velocityLimit);

                // 書き出し
                nextPosArray[pindex] = nextPosArray[pindex] + add;

                // 集計バッファクリア
                countArray[pindex] = 0;
                sumArray[dataIndex] = 0;
                sumArray[dataIndex + 1] = 0;
                sumArray[dataIndex + 2] = 0;
            }
        }

        /// <summary>
        /// 速度影響あり
        /// </summary>
        [BurstCompile]
        struct AggregateWithVelocityJob : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeList<int> jobParticleIndexList;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // aggregate
            public float velocityAttenuation;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
                int pindex = jobParticleIndexList[index];

                int count = countArray[pindex];
                if (count == 0)
                    return;

                int dataIndex = pindex * 3;

                // 集計
                float3 add = new float3(sumArray[dataIndex], sumArray[dataIndex + 1], sumArray[dataIndex + 2]);
                add /= count;

                // データは固定小数点なので戻す
                add *= ToFloat;

                // 書き出し
                nextPosArray[pindex] = nextPosArray[pindex] + add;

                // 速度影響
                velocityPosArray[pindex] = velocityPosArray[pindex] + add * velocityAttenuation;

                // 集計バッファクリア
                countArray[pindex] = 0;
                sumArray[dataIndex] = 0;
                sumArray[dataIndex + 1] = 0;
                sumArray[dataIndex + 2] = 0;
            }
        }

        internal static JobHandle SolveAggregateBufferAndClear(in ExProcessingList<int> processingList, float velocityAttenuation, JobHandle jobHandle)
        {
            return SolveAggregateBufferAndClear(processingList.Buffer, processingList.Counter, velocityAttenuation, jobHandle);
        }

        unsafe internal static JobHandle SolveAggregateBufferAndClear(in NativeArray<int> particleArray, in NativeReference<int> counter, float velocityAttenuation, JobHandle jobHandle)
        {
            var sm = MagicaManager.Simulation;

            if (velocityAttenuation > 1e-06f)
            {
                // 速度影響あり
                var job = new AggregateWithVelocityJob2()
                {
                    particleIndexArray = particleArray,

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    velocityPosArray = sm.velocityPosArray.GetNativeArray(),

                    velocityAttenuation = velocityAttenuation,
                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                };
                jobHandle = job.Schedule((int*)counter.GetUnsafePtrWithoutChecks(), 16, jobHandle);
            }
            else
            {
                // 速度影響なし
                var job = new AggregateJob2()
                {
                    //velocityLimit = velocityLimit,
                    particleIndexArray = particleArray,

                    nextPosArray = sm.nextPosArray.GetNativeArray(),

                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                };
                jobHandle = job.Schedule((int*)counter.GetUnsafePtrWithoutChecks(), 16, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct AggregateJob2 : IJobParallelForDefer
        {
            // 速度制限
            //public float velocityLimit;

            [Unity.Collections.ReadOnly]
            public NativeArray<int> particleIndexArray;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // aggregate

            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
                int pindex = particleIndexArray[index];

                int count = countArray[pindex];
                if (count == 0)
                    return;

                int dataIndex = pindex * 3;

                // 集計
                float3 add = new float3(sumArray[dataIndex], sumArray[dataIndex + 1], sumArray[dataIndex + 2]);
                add /= count;

                // データは固定小数点なので戻す
                add *= ToFloat;

                // 速度制限
                //add = MathUtility.ClampVector(add, velocityLimit);

                // 書き出し
                nextPosArray[pindex] = nextPosArray[pindex] + add;

                // 集計バッファクリア
                countArray[pindex] = 0;
                sumArray[dataIndex] = 0;
                sumArray[dataIndex + 1] = 0;
                sumArray[dataIndex + 2] = 0;
            }
        }

        /// <summary>
        /// 速度影響あり
        /// </summary>
        [BurstCompile]
        struct AggregateWithVelocityJob2 : IJobParallelForDefer
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> particleIndexArray;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> velocityPosArray;

            // aggregate
            public float velocityAttenuation;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // 集計パーティクルごと
            public void Execute(int index)
            {
                int pindex = particleIndexArray[index];

                int count = countArray[pindex];
                if (count == 0)
                    return;

                int dataIndex = pindex * 3;

                // 集計
                float3 add = new float3(sumArray[dataIndex], sumArray[dataIndex + 1], sumArray[dataIndex + 2]);
                add /= count;

                // データは固定小数点なので戻す
                add *= ToFloat;

                // 書き出し
                nextPosArray[pindex] = nextPosArray[pindex] + add;

                // 速度影響
                velocityPosArray[pindex] = velocityPosArray[pindex] + add * velocityAttenuation;

                // 集計バッファクリア
                countArray[pindex] = 0;
                sumArray[dataIndex] = 0;
                sumArray[dataIndex + 1] = 0;
                sumArray[dataIndex + 2] = 0;
            }
        }


        /// <summary>
        /// 集計バッファのカウンタのみゼロクリアする
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal static JobHandle ClearCountArray(JobHandle jobHandle)
        {
            var sm = MagicaManager.Simulation;

            return JobUtility.Fill(sm.countArray, sm.countArray.Length, 0, jobHandle);
        }
    }
}
