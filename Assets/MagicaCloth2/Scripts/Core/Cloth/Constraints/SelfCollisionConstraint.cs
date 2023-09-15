// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class SelfCollisionConstraint : IDisposable
    {
        public enum SelfCollisionMode
        {
            None = 0,

            /// <summary>
            /// PointPoint
            /// </summary>
            //Point = 1, // omit!

            /// <summary>
            /// PointTriangle + EdgeEdge + Intersect
            /// </summary>
            FullMesh = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// self-collision mode
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public SelfCollisionMode selfMode;

            /// <summary>
            /// primitive thickness.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData surfaceThickness = new CurveSerializeData(0.005f, 0.5f, 1.0f, false);

            /// <summary>
            /// mutual collision mode.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public SelfCollisionMode syncMode;

            /// <summary>
            /// Mutual Collision Opponent.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public MagicaCloth syncPartner;

            /// <summary>
            /// cloth weight.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float clothMass = 0.0f;

            public SerializeData()
            {
                selfMode = SelfCollisionMode.None;
                syncMode = SelfCollisionMode.None;
            }

            public void DataValidate()
            {
                surfaceThickness.DataValidate(Define.System.SelfCollisionThicknessMin, Define.System.SelfCollisionThicknessMax);
                clothMass = Mathf.Clamp01(clothMass);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    selfMode = selfMode,
                    surfaceThickness = surfaceThickness.Clone(),
                    syncMode = syncMode,
                    syncPartner = syncPartner,
                    clothMass = clothMass,
                };
            }

            public MagicaCloth GetSyncPartner()
            {
                return syncMode != SelfCollisionMode.None ? syncPartner : null;
            }
        }

        public struct SelfCollisionConstraintParams
        {
            public SelfCollisionMode selfMode;
            public float4x4 surfaceThicknessCurveData;
            public SelfCollisionMode syncMode;
            public float clothMass;

            public void Convert(SerializeData sdata)
            {
                selfMode = sdata.selfMode;
                surfaceThicknessCurveData = sdata.surfaceThickness.ConvertFloatArray();
                syncMode = sdata.syncMode;
                clothMass = sdata.clothMass;
            }
        }

        //=========================================================================================
        public const uint KindPoint = 0;
        public const uint KindEdge = 1;
        public const uint KindTriangle = 2;

        public const uint Flag_KindMask = 0x03000000; // 24~25bit
        public const uint Flag_Fix0 = 0x04000000;
        public const uint Flag_Fix1 = 0x08000000;
        public const uint Flag_Fix2 = 0x10000000;
        public const uint Flag_AllFix = 0x20000000;
        public const uint Flag_Ignore = 0x40000000; // 無効もしくは無視頂点が含まれる
        public const uint Flag_Enable = 0x80000000; // 接触判定有効

        struct Primitive
        {
            /// <summary>
            /// フラグとチームID
            /// 上位8bit = フラグ
            /// 下位24bit = チームID
            /// </summary>
            public uint flagAndTeamId;

            /// <summary>
            /// ソートリストへのインデックス（グローバル）
            /// </summary>
            public int sortIndex;

            /// <summary>
            /// プリミティグを構成するパーティクルインデックス
            /// </summary>
            public int3 particleIndices;

            public float3x3 nextPos;
            public float3x3 oldPos;
            //public float3x3 basePos;
            public float3 invMass;

            /// <summary>
            /// 厚み
            /// </summary>
            public float thickness;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsIgnore()
            {
                return (flagAndTeamId & Flag_Ignore) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasParticle(int p)
            {
                return p >= 0 && math.all(particleIndices - p) == false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetKind()
            {
                return (flagAndTeamId & Flag_KindMask) >> 24;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetTeamId()
            {
                return (int)(flagAndTeamId & 0xffffff);
            }

            /// <summary>
            /// 解決時のthicknessを計算する
            /// </summary>
            /// <param name="pri"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetSolveThickness(in Primitive pri)
            {
                return thickness + pri.thickness;
            }

            /// <summary>
            /// パーティクルインデックスが１つ以上重複しているか判定する
            /// </summary>
            /// <param name="pri"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AnyParticle(in Primitive pri)
            {
                for (int i = 0; i < 3; i++)
                {
                    int p = particleIndices[i];
                    if (p >= 0)
                    {
                        if (math.all(pri.particleIndices - p) == false)
                            return true;
                    }
                }
                return false;
            }
        }
        ExNativeArray<Primitive> primitiveArray;

        struct SortData : IComparable<SortData>
        {
            /// <summary>
            /// フラグとチームID
            /// 上位8bit = フラグ
            /// 下位24bit = チームID
            /// </summary>
            public uint flagAndTeamId;

            /// <summary>
            /// プリミティブインデックス（グローバル）
            /// </summary>
            public int primitiveIndex;

            public float2 firstMinMax;
            public float2 secondMinMax;
            public float2 thirdMinMax;

            public int CompareTo(SortData other)
            {
                return (int)math.sign(firstMinMax.x - other.firstMinMax.x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetKind()
            {
                return (flagAndTeamId & Flag_KindMask) >> 24;
            }
        }
        ExNativeArray<SortData> sortAndSweepArray;

        /// <summary>
        /// ポイントプリミティブ総数
        /// </summary>
        public int PointPrimitiveCount { get; private set; } = 0;

        /// <summary>
        /// エッジプリミティブ総数
        /// </summary>
        public int EdgePrimitiveCount { get; private set; } = 0;

        /// <summary>
        /// トライアングルプリミティブ総数
        /// </summary>
        public int TrianglePrimitiveCount { get; private set; } = 0;

        //=========================================================================================
        internal struct EdgeEdgeContact
        {
            public uint flagAndTeamId0;
            public uint flagAndTeamId1;
            public half thickness;
            public half s;
            public half t;
            public half3 n;
            public half2 edgeInvMass0;
            public half2 edgeInvMass1;
            public int2 edgeParticleIndex0;
            public int2 edgeParticleIndex1;

            public override string ToString()
            {
                return $"EdgeEdge f0:{flagAndTeamId0:X}, f1:{flagAndTeamId1:X}, p0:{edgeParticleIndex0}, p1:{edgeParticleIndex1}, inv0:{edgeInvMass0}, inv1:{edgeInvMass1}";
            }
        }
        NativeQueue<EdgeEdgeContact> edgeEdgeContactQueue;
        NativeList<EdgeEdgeContact> edgeEdgeContactList;

        internal struct PointTriangleContact
        {
            public uint flagAndTeamId0; // point
            public uint flagAndTeamId1; // triangle
            public half thickness;
            public half sign; // 押出方向(-1/+1)
            public int pointParticleIndex;
            public int3 triangleParticleIndex;
            public half pointInvMass;
            public half3 triangleInvMass;

            public override string ToString()
            {
                return $"PointTriangle f0:{flagAndTeamId0:X}, f1:{flagAndTeamId1:X}, pp:{pointParticleIndex}, pt:{triangleParticleIndex}, pinv:{pointInvMass}, tinv:{triangleInvMass}";
            }
        }
        NativeQueue<PointTriangleContact> pointTriangleContactQueue;
        NativeList<PointTriangleContact> pointTriangleContactList;

        /// <summary>
        /// 交差解決フラグ(パーティクルと連動)
        /// </summary>
        NativeArray<byte> intersectFlagArray;

        public int IntersectCount { get; private set; } = 0;

        //=========================================================================================
        public SelfCollisionConstraint()
        {
            primitiveArray = new ExNativeArray<Primitive>(0, true);
            sortAndSweepArray = new ExNativeArray<SortData>(0, true);

            edgeEdgeContactQueue = new NativeQueue<EdgeEdgeContact>(Allocator.Persistent);
            pointTriangleContactQueue = new NativeQueue<PointTriangleContact>(Allocator.Persistent);
            edgeEdgeContactList = new NativeList<EdgeEdgeContact>(Allocator.Persistent);
            pointTriangleContactList = new NativeList<PointTriangleContact>(Allocator.Persistent);

            intersectFlagArray = new NativeArray<byte>(0, Allocator.Persistent);

            //Develop.DebugLog($"UseQueueCount:{UseQueueCount}");
        }

        public void Dispose()
        {
            primitiveArray?.Dispose();
            primitiveArray = null;

            sortAndSweepArray?.Dispose();
            sortAndSweepArray = null;

            PointPrimitiveCount = 0;
            EdgePrimitiveCount = 0;
            TrianglePrimitiveCount = 0;

            edgeEdgeContactQueue.Dispose();
            pointTriangleContactQueue.Dispose();
            edgeEdgeContactList.Dispose();
            pointTriangleContactList.Dispose();

            intersectFlagArray.DisposeSafe();

            IntersectCount = 0;
        }

        /// <summary>
        /// データの有無を返す
        /// </summary>
        /// <returns></returns>
        public bool HasPrimitive()
        {
            return (PointPrimitiveCount + EdgePrimitiveCount + TrianglePrimitiveCount) > 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[SelfCollisionConstraint]");
            sb.AppendLine($"  -primitiveArray:{primitiveArray.ToSummary()}");
            sb.AppendLine($"  -sortAndSweepArray:{sortAndSweepArray.ToSummary()}");

            sb.AppendLine($"  -edgeEdgeContactQueue:{(edgeEdgeContactQueue.IsCreated ? edgeEdgeContactQueue.Count : 0)}");
            sb.AppendLine($"  -edgeEdgeContactList:{(edgeEdgeContactList.IsCreated ? edgeEdgeContactList.Length : 0)}");
            sb.AppendLine($"  -pointTriangleContactQueue:{(pointTriangleContactQueue.IsCreated ? pointTriangleContactQueue.Count : 0)}");
            sb.AppendLine($"  -pointTriangleContactList:{(pointTriangleContactList.IsCreated ? pointTriangleContactList.Length : 0)}");
            sb.AppendLine($"  -intersectFlagArray:{(intersectFlagArray.IsCreated ? intersectFlagArray.Length : 0)}");

            return sb.ToString();
        }

        //=========================================================================================
#if false
        internal class ConstraintData : IValid
        {
            public ResultCode result;

            /// <summary>
            /// 同期先proxyMeshのlocalPosを自proxyMesh空間に変換するマトリックス
            /// </summary>
            public float4x4 syncToSelfMatrix;

            public bool IsValid()
            {
                return math.any(syncToSelfMatrix.c0);
            }
        }

        internal static ConstraintData CreateData(
            int teamId, TeamManager.TeamData teamData, VirtualMesh proxyMesh, in ClothParameters parameters,
            int syncTeamId, TeamManager.TeamData syncTeamData, VirtualMesh syncProxyMesh)
        {
            var constraintData = new ConstraintData();

            try
            {
                if (proxyMesh.VertexCount == 0)
                    return null;

                var self2Params = parameters.selfCollisionConstraint2;

                // 同期チームとのFullMesh判定が必要な場合は、同期ProxyMeshのローカル頂点を時チームの座標空間に変換しておく
                //var syncMode = parameters.selfCollisionConstraint.syncMode;
                //if (syncTeamId > 0 && syncProxyMesh != null)
                //{
                //    // 同期proxyMeshを自proxyMesh空間に変換するマトリックス
                //    var toM = syncProxyMesh.CenterTransformTo(proxyMesh);
                //    constraintData.syncToSelfMatrix = toM;
                //}
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                constraintData.result.SetError(Define.Result.Constraint_CreateSelfCollisionException);
            }
            finally
            {
            }

            return constraintData;
        }
#endif

        //=========================================================================================
        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Register(ClothProcess cprocess)
        {
            UpdateTeam(cprocess.TeamId);
        }

        /// <summary>
        /// 制約データを解除する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Exit(ClothProcess cprocess)
        {
            if (cprocess != null && cprocess.TeamId > 0)
            {
                // Exitフラグを見て自動的にすべて解放される
                // また同期相手のフラグ更新も行う
                UpdateTeam(cprocess.TeamId);
            }
        }

        /// <summary>
        /// フラグおよびバッファの更新
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="tdata"></param>
        internal void UpdateTeam(int teamId)
        {
            var tm = MagicaManager.Team;

            if (tm.ContainsTeamData(teamId) == false)
                return;
            ref var tdata = ref tm.GetTeamDataRef(teamId);
            var oldFlag = tdata.flag;

            // チームが消滅中かどうか
            bool exit = tdata.flag.IsSet(TeamManager.Flag_Exit);

            // 自身の状況を判定する
            ref var parameter = ref tm.GetParametersRef(teamId);
            var selfMode = exit ? SelfCollisionMode.None : parameter.selfCollisionConstraint.selfMode;
            var syncMode = exit ? SelfCollisionMode.None : parameter.selfCollisionConstraint.syncMode;

            bool usePointPrimitive = false;
            bool useEdgePrimitive = false;
            bool useTrianglePrimitive = false;

            bool selfEdgeEdge = false;
            bool selfPointTriangle = false;
            bool selfTrianglePoint = false;
            bool selfEdgeTriangleIntersect = false;
            bool selfTriangleEdgeIntersect = false;

            bool syncEdgeEdge = false;
            bool syncPointTriangle = false;
            bool syncTrianglePoint = false;
            bool syncEdgeTriangleIntersect = false;
            bool syncTriangleEdgeIntersect = false;

            bool PsyncEdgeEdge = false;
            bool PsyncPointTriangle = false;
            bool PsyncTrianglePoint = false;
            bool PsyncEdgeTriangleIntersect = false;
            bool PsyncTriangleEdgeIntersect = false;

            if (selfMode == SelfCollisionMode.FullMesh)
            {
                if (tdata.EdgeCount > 0)
                {
                    selfEdgeEdge = true;
                    useEdgePrimitive = true;
                }
                if (tdata.TriangleCount > 0)
                {
                    selfPointTriangle = true;
                    selfTrianglePoint = true;
                    usePointPrimitive = true;
                    useTrianglePrimitive = true;
                }
                if (tdata.EdgeCount > 0 && tdata.TriangleCount > 0)
                {
                    selfEdgeTriangleIntersect = true;
                    selfTriangleEdgeIntersect = true;
                }
            }

            // sync
            if (syncMode != SelfCollisionMode.None && tm.ContainsTeamData(tdata.syncTeamId))
            {
                ref var stdata = ref tm.GetTeamDataRef(tdata.syncTeamId);
                if (syncMode == SelfCollisionMode.FullMesh)
                {
                    if (tdata.EdgeCount > 0 && stdata.EdgeCount > 0)
                    {
                        syncEdgeEdge = true;
                        useEdgePrimitive = true;
                    }
                    if (tdata.TriangleCount > 0)
                    {
                        syncTrianglePoint = true;
                        useTrianglePrimitive = true;
                    }
                    if (stdata.TriangleCount > 0)
                    {
                        syncPointTriangle = true;
                        usePointPrimitive = true;
                    }
                    if (tdata.EdgeCount > 0 && stdata.TriangleCount > 0)
                    {
                        syncEdgeTriangleIntersect = true;
                    }
                    if (tdata.TriangleCount > 0 && stdata.EdgeCount > 0)
                    {
                        syncTriangleEdgeIntersect = true;
                    }
                }
            }

            // sync parent
            if (tdata.syncParentTeamId.Length > 0 && exit == false)
            {
                for (int i = 0; i < tdata.syncParentTeamId.Length; i++)
                {
                    int parentTeamId = tdata.syncParentTeamId[i];
                    ref var ptdata = ref tm.GetTeamDataRef(parentTeamId);
                    if (ptdata.IsValid)
                    {
                        ref var parentParameter = ref tm.GetParametersRef(parentTeamId);
                        var parentSyncMode = parentParameter.selfCollisionConstraint.syncMode;
                        if (parentSyncMode == SelfCollisionMode.FullMesh)
                        {
                            if (ptdata.EdgeCount > 0 && tdata.EdgeCount > 0)
                            {
                                PsyncEdgeEdge = true;
                                useEdgePrimitive = true;
                            }
                            if (ptdata.TriangleCount > 0)
                            {
                                PsyncPointTriangle = true;
                                usePointPrimitive = true;
                            }
                            if (tdata.TriangleCount > 0)
                            {
                                PsyncTrianglePoint = true;
                                useTrianglePrimitive = true;
                            }
                            if (tdata.EdgeCount > 0 && ptdata.TriangleCount > 0)
                            {
                                PsyncEdgeTriangleIntersect = true;
                            }
                            if (tdata.TriangleCount > 0 && ptdata.EdgeCount > 0)
                            {
                                PsyncTriangleEdgeIntersect = true;
                            }
                        }
                    }
                }
            }

            // フラグ
            tdata.flag.SetBits(TeamManager.Flag_Self_PointPrimitive, usePointPrimitive);
            tdata.flag.SetBits(TeamManager.Flag_Self_EdgePrimitive, useEdgePrimitive);
            tdata.flag.SetBits(TeamManager.Flag_Self_TrianglePrimitive, useTrianglePrimitive);

            tdata.flag.SetBits(TeamManager.Flag_Self_EdgeEdge, selfEdgeEdge);
            tdata.flag.SetBits(TeamManager.Flag_Self_PointTriangle, selfPointTriangle);
            tdata.flag.SetBits(TeamManager.Flag_Self_TrianglePoint, selfTrianglePoint);
            tdata.flag.SetBits(TeamManager.Flag_Self_EdgeTriangleIntersect, selfEdgeTriangleIntersect);
            tdata.flag.SetBits(TeamManager.Flag_Self_TriangleEdgeIntersect, selfTriangleEdgeIntersect);

            tdata.flag.SetBits(TeamManager.Flag_Sync_EdgeEdge, syncEdgeEdge);
            tdata.flag.SetBits(TeamManager.Flag_Sync_PointTriangle, syncPointTriangle);
            tdata.flag.SetBits(TeamManager.Flag_Sync_TrianglePoint, syncTrianglePoint);
            tdata.flag.SetBits(TeamManager.Flag_Sync_EdgeTriangleIntersect, syncEdgeTriangleIntersect);
            tdata.flag.SetBits(TeamManager.Flag_Sync_TriangleEdgeIntersect, syncTriangleEdgeIntersect);

            tdata.flag.SetBits(TeamManager.Flag_PSync_EdgeEdge, PsyncEdgeEdge);
            tdata.flag.SetBits(TeamManager.Flag_PSync_PointTriangle, PsyncPointTriangle);
            tdata.flag.SetBits(TeamManager.Flag_PSync_TrianglePoint, PsyncTrianglePoint);
            tdata.flag.SetBits(TeamManager.Flag_PSync_EdgeTriangleIntersect, PsyncEdgeTriangleIntersect);
            tdata.flag.SetBits(TeamManager.Flag_PSync_TriangleEdgeIntersect, PsyncTriangleEdgeIntersect);

            // point buffer
            if (usePointPrimitive && tdata.selfPointChunk.IsValid == false)
            {
                // init
                int pointCount = tdata.ParticleCount;
                tdata.selfPointChunk = primitiveArray.AddRange(pointCount);
                sortAndSweepArray.AddRange(pointCount);
                int start = tdata.selfPointChunk.startIndex;
                InitPrimitive(teamId, tdata, KindPoint, start, start, pointCount);
                PointPrimitiveCount += pointCount;
            }
            else if (usePointPrimitive == false && tdata.selfPointChunk.IsValid)
            {
                // remove
                primitiveArray.Remove(tdata.selfPointChunk);
                sortAndSweepArray.Remove(tdata.selfPointChunk);
                PointPrimitiveCount -= tdata.selfPointChunk.dataLength;
                tdata.selfPointChunk.Clear();
            }

            // edge buffer
            if (useEdgePrimitive && tdata.selfEdgeChunk.IsValid == false)
            {
                // init
                int edgeCount = tdata.EdgeCount;
                tdata.selfEdgeChunk = primitiveArray.AddRange(edgeCount);
                sortAndSweepArray.AddRange(edgeCount);
                int start = tdata.selfEdgeChunk.startIndex;
                InitPrimitive(teamId, tdata, KindEdge, start, start, edgeCount);
                EdgePrimitiveCount += edgeCount;
            }
            else if (useEdgePrimitive == false && tdata.selfEdgeChunk.IsValid)
            {
                // remove
                primitiveArray.Remove(tdata.selfEdgeChunk);
                sortAndSweepArray.Remove(tdata.selfEdgeChunk);
                EdgePrimitiveCount -= tdata.selfEdgeChunk.dataLength;
                tdata.selfEdgeChunk.Clear();
            }

            // triangle buffer
            if (useTrianglePrimitive && tdata.selfTriangleChunk.IsValid == false)
            {
                // init
                int triangleCount = tdata.TriangleCount;
                tdata.selfTriangleChunk = primitiveArray.AddRange(triangleCount);
                sortAndSweepArray.AddRange(triangleCount);
                int start = tdata.selfTriangleChunk.startIndex;
                InitPrimitive(teamId, tdata, KindTriangle, start, start, triangleCount);
                TrianglePrimitiveCount += triangleCount;
            }
            else if (useTrianglePrimitive == false && tdata.selfTriangleChunk.IsValid)
            {
                // remove
                primitiveArray.Remove(tdata.selfTriangleChunk);
                sortAndSweepArray.Remove(tdata.selfTriangleChunk);
                TrianglePrimitiveCount -= tdata.selfTriangleChunk.dataLength;
                tdata.selfTriangleChunk.Clear();
            }

            // Intersect
            bool useIntersect = tdata.flag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 6);
            bool oldIntersect = oldFlag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 6);
            if (useIntersect && oldIntersect == false)
            {
                // init
                IntersectCount++; // チーム利用カウント
            }
            else if (useIntersect == false && oldIntersect)
            {
                // remove
                IntersectCount--;
            }

            // 同期対象に対して再帰する
            if (syncMode != SelfCollisionMode.None && tm.ContainsTeamData(tdata.syncTeamId))
            {
                UpdateTeam(tdata.syncTeamId);
            }
        }

        void InitPrimitive(int teamId, TeamManager.TeamData tdata, uint kind, int startPrimitive, int startSort, int length)
        {
            var job = new InitPrimitiveJob()
            {
                teamId = teamId,
                tdata = tdata,
                kind = kind,
                startPrimitive = startPrimitive,
                startSort = startSort,

                edges = MagicaManager.VMesh.edges.GetNativeArray(),
                triangles = MagicaManager.VMesh.triangles.GetNativeArray(),

                primitiveArray = primitiveArray.GetNativeArray(),
                sortArray = sortAndSweepArray.GetNativeArray(),
            };
            job.Run(length); // ここではRun()で実行する
        }

        [BurstCompile]
        struct InitPrimitiveJob : IJobParallelFor
        {
            public int teamId;
            public TeamManager.TeamData tdata;

            public uint kind;
            public int startPrimitive;
            public int startSort;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;

            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<SortData> sortArray;

            public void Execute(int index)
            {
                int pri_index = startPrimitive + index;
                int sort_index = startSort + index;

                var p = primitiveArray[pri_index];
                var s = sortArray[sort_index];

                // プリミティブを構成するパーティクルインデックス
                int3 particleIndices = -1;
                int pstart = tdata.particleChunk.startIndex;
                if (kind == KindPoint)
                {
                    particleIndices[0] = pstart + index;
                }
                else if (kind == KindEdge)
                {
                    int estart = tdata.proxyEdgeChunk.startIndex;
                    particleIndices.xy = edges[estart + index] + pstart;
                }
                else if (kind == KindTriangle)
                {
                    int tstart = tdata.proxyTriangleChunk.startIndex;
                    particleIndices.xyz = triangles[tstart + index] + pstart;
                }

                p.flagAndTeamId = (uint)teamId | (kind << 24);
                p.sortIndex = sort_index;
                p.particleIndices = particleIndices;

                s.primitiveIndex = pri_index;
                s.flagAndTeamId = (uint)teamId;

                primitiveArray[pri_index] = p;
                sortArray[sort_index] = s;
            }
        }

        /// <summary>
        /// 作業バッファ更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
            // 交差フラグバッファ
            if (IntersectCount > 0)
            {
                int pcnt = MagicaManager.Simulation.ParticleCount;
                intersectFlagArray.Resize(pcnt, options: NativeArrayOptions.ClearMemory);
            }

#if MC2_DEBUG && false
            // debug
            Develop.DebugLog($"PointPrimitive:{PointPrimitiveCount}, EdgePrimitive:{EdgePrimitiveCount}, TrianglePrimitive:{TrianglePrimitiveCount}, Intersect:{IntersectCount}");
            if (edgeEdgeContactQueue.Count > 0 || pointTriangleContactQueue.Count > 0)
                Develop.DebugLog($"EdgeEdge Contact:{edgeEdgeContactQueue.Count}, PointTriangle Contact:{pointTriangleContactQueue.Count}");
            edgeEdgeContactQueue.Clear();
            pointTriangleContactQueue.Clear();
#endif

        }

        /// <summary>
        /// ソート＆スイープ配列をデータsdで二分探索しその開始インデックスを返す
        /// </summary>
        /// <param name="sortAndSweepArray"></param>
        /// <param name="sd"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        static unsafe int BinarySearchSortAndlSweep(ref NativeArray<SortData> sortAndSweepArray, in SortData sd, in DataChunk chunk)
        {
            SortData* pt = (SortData*)sortAndSweepArray.GetUnsafeReadOnlyPtr();
            pt += chunk.startIndex;
            int sortIndex = NativeSortExtension.BinarySearch(pt, chunk.dataLength, sd);
            if (sortIndex < 0)
            {
                // インデックスが正の場合は値が正確に発見されたのでそのインデックスが返る
                // インデックスが負の場合は値が見つからずに最終的な探索インデックスが返る
                // この場合はそのインデックスを正に戻しそこから－１した場所が値の次のインデックスとなっている
                // 例:(-7) = 検索値はインデックス５と６の間
                sortIndex = math.max(-sortIndex - 1, 0);
            }
            sortIndex += chunk.startIndex;

            return sortIndex;
        }

        //=========================================================================================
        /// <summary>
        /// 制約の解決
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle SolverConstraint(int updateIndex, JobHandle jobHandle)
        {
            // 実行時セルフコリジョンの解決
            jobHandle = SolverRuntimeSelfCollision(updateIndex, jobHandle);

            // 絡まり解決
            jobHandle = SolveIntersect(jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// 実行時セルフコリジョンの解決
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe JobHandle SolverRuntimeSelfCollision(int updateIndex, JobHandle jobHandle)
        {
            if (HasPrimitive() == false)
                return jobHandle;

            // Broad phase ====================================================
            // コンタクトバッファ生成
            // !コンタクトバッファ生成は１フレームに１回しか実行しない
            // !ステップ２回目以降は各パーティクルが線形に移動するだけでコンタクト生成結果にあまり変化が無いためスキップさせる
            // !これは本来ならNGだがクオリティよりパフォーマンスを優先した実装となる
            // !そもそも毎ステップ生成したとしてもMagicaClothは反復が少ないので絡まるときは絡まる。
            // !MagicaClothではその絡まりを最後のSolveIntersect()で解くことに重点を置く
            if (updateIndex == 0)
            {
                // この生成が大変重い！
                jobHandle = SolverBroadPhase(jobHandle);
            }
            else
            {
                // !ステップ２回目以降はコンタクトバッファの内容に対して再度ブロードフェーズのみを実行する
                jobHandle = UpdateBroadPhase(jobHandle);
            }

            // Solver phase ====================================================
            // 接触の解決
            // !直列１回と同程度の堅牢さは並列では反復3~4回ほど必要
            var sm = MagicaManager.Simulation;
            for (int i = 0; i < Define.System.SelfCollisionSolverIteration; i++)
            {
                var solverEdgeEdgeJob = new SolverEdgeEdgeJob()
                {
                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    edgeEdgeContactArray = edgeEdgeContactList.AsDeferredJobArray(),
                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                };
                jobHandle = solverEdgeEdgeJob.Schedule(edgeEdgeContactList, 16, jobHandle);

                var solverPointTriangleJob = new SolverPointTriangleJob()
                {
                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    pointTriangleContactArray = pointTriangleContactList.AsDeferredJobArray(),
                    countArray = sm.countArray,
                    sumArray = sm.sumArray,
                };
                jobHandle = solverPointTriangleJob.Schedule(pointTriangleContactList, 16, jobHandle);

                // 集計
                const float attn = 0.0f; // 0.0f
                jobHandle = InterlockUtility.SolveAggregateBufferAndClear(sm.processingSelfParticle, attn, jobHandle);
            }

            return jobHandle;
        }

        /// <summary>
        /// コンタクトバッファ生成
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe JobHandle SolverBroadPhase(JobHandle jobHandle)
        {
            var tm = MagicaManager.Team;
            var sm = MagicaManager.Simulation;
            var vm = MagicaManager.VMesh;

            // バッファクリア
            jobHandle = new ClearBufferJob()
            {
                edgeEdgeContactQueue = edgeEdgeContactQueue,
                pointTriangleContactQueue = pointTriangleContactQueue,
            }.Schedule(jobHandle);

            // プリミティブ更新 =======================================================
            if (PointPrimitiveCount > 0)
            {
                // PointTriangle
                var job2 = new UpdatePrimitiveJob()
                {
                    kind = KindPoint,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    attributes = vm.attributes.GetNativeArray(),
                    depthArray = vm.vertexDepths.GetNativeArray(),

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    oldPosArray = sm.oldPosArray.GetNativeArray(),
                    frictionArray = sm.frictionArray.GetNativeArray(),
                    //stepBasicPositionBuffer = sm.stepBasicPositionBuffer,

                    primitiveArray = primitiveArray.GetNativeArray(),
                    sortAndSweepArray = sortAndSweepArray.GetNativeArray(),

                    processingArray = sm.processingSelfPointTriangle.Buffer,
                };
                jobHandle = job2.Schedule(sm.processingSelfPointTriangle.GetJobSchedulePtr(), 16, jobHandle);
            }
            if (EdgePrimitiveCount > 0)
            {
                // EdgeEdge
                var job = new UpdatePrimitiveJob()
                {
                    kind = KindEdge,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    attributes = vm.attributes.GetNativeArray(),
                    depthArray = vm.vertexDepths.GetNativeArray(),

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    oldPosArray = sm.oldPosArray.GetNativeArray(),
                    frictionArray = sm.frictionArray.GetNativeArray(),
                    //stepBasicPositionBuffer = sm.stepBasicPositionBuffer,

                    primitiveArray = primitiveArray.GetNativeArray(),
                    sortAndSweepArray = sortAndSweepArray.GetNativeArray(),

                    processingArray = sm.processingSelfEdgeEdge.Buffer,
                };
                jobHandle = job.Schedule(sm.processingSelfEdgeEdge.GetJobSchedulePtr(), 16, jobHandle);
            }
            if (TrianglePrimitiveCount > 0)
            {
                // TrianglePoint
                var job = new UpdatePrimitiveJob()
                {
                    kind = KindTriangle,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    attributes = vm.attributes.GetNativeArray(),
                    depthArray = vm.vertexDepths.GetNativeArray(),

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    oldPosArray = sm.oldPosArray.GetNativeArray(),
                    frictionArray = sm.frictionArray.GetNativeArray(),
                    //stepBasicPositionBuffer = sm.stepBasicPositionBuffer,

                    primitiveArray = primitiveArray.GetNativeArray(),
                    sortAndSweepArray = sortAndSweepArray.GetNativeArray(),

                    processingArray = sm.processingSelfTrianglePoint.Buffer,
                };
                jobHandle = job.Schedule(sm.processingSelfTrianglePoint.GetJobSchedulePtr(), 16, jobHandle);
            }

            // sort ===========================================================
            // チームごと、およびpoint/edge/triangle別
            var sortJob = new SortJob()
            {
                teamDataArray = tm.teamDataArray.GetNativeArray(),
                primitiveArray = primitiveArray.GetNativeArray(),
                sortAndSweepArray = sortAndSweepArray.GetNativeArray(),
            };
            jobHandle = sortJob.Schedule(tm.TeamCount * 3, 1, jobHandle); // 3 = (Point/Edge/Triangle)

            // Broad phase ====================================================
            // EdgeEdge
            if (EdgePrimitiveCount > 0)
            {
                // Edge -> Edge
                var broadEdgeEdgeJob = new EdgeEdgeBroadPhaseJob()
                {
                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    primitiveArray = primitiveArray.GetNativeArray(),
                    sortAndSweepArray = sortAndSweepArray.GetNativeArray(),

                    processingEdgeEdgeArray = sm.processingSelfEdgeEdge.Buffer,

                    edgeEdgeContactQueue = edgeEdgeContactQueue.AsParallelWriter(),

                    intersectFlagArray = intersectFlagArray,
                };
                jobHandle = broadEdgeEdgeJob.Schedule(sm.processingSelfEdgeEdge.GetJobSchedulePtr(), 16, jobHandle);
            }

            // PointTriangle
            if (PointPrimitiveCount > 0)
            {
                // Point -> Triangle
                var broadPointTriangleJob = new PointTriangleBroadPhaseJob()
                {
                    mainKind = KindPoint,

                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    triangles = vm.triangles.GetNativeArray(),
                    attributes = vm.attributes.GetNativeArray(),

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    oldPosArray = sm.oldPosArray.GetNativeArray(),
                    frictionArray = sm.frictionArray.GetNativeArray(),

                    primitiveArray = primitiveArray.GetNativeArray(),
                    sortAndSweepArray = sortAndSweepArray.GetNativeArray(),

                    processingPointTriangleArray = sm.processingSelfPointTriangle.Buffer,

                    pointTriangleContactQueue = pointTriangleContactQueue.AsParallelWriter(),

                    intersectFlagArray = intersectFlagArray,
                };
                jobHandle = broadPointTriangleJob.Schedule(sm.processingSelfPointTriangle.GetJobSchedulePtr(), 16, jobHandle);
            }
            if (TrianglePrimitiveCount > 0)
            {
                // Triangle -> Point
                var broadTrianglePointJob = new PointTriangleBroadPhaseJob()
                {
                    mainKind = KindTriangle,

                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    triangles = vm.triangles.GetNativeArray(),
                    attributes = vm.attributes.GetNativeArray(),

                    nextPosArray = sm.nextPosArray.GetNativeArray(),
                    oldPosArray = sm.oldPosArray.GetNativeArray(),
                    frictionArray = sm.frictionArray.GetNativeArray(),

                    primitiveArray = primitiveArray.GetNativeArray(),
                    sortAndSweepArray = sortAndSweepArray.GetNativeArray(),

                    processingPointTriangleArray = sm.processingSelfTrianglePoint.Buffer,

                    pointTriangleContactQueue = pointTriangleContactQueue.AsParallelWriter(),

                    intersectFlagArray = intersectFlagArray,
                };
                jobHandle = broadTrianglePointJob.Schedule(sm.processingSelfTrianglePoint.GetJobSchedulePtr(), 16, jobHandle);
            }

            // ToList
            var toListJob1 = new EdgeEdgeToListJob()
            {
                edgeEdgeContactQueue = edgeEdgeContactQueue,
                edgeEdgeContactList = edgeEdgeContactList,
            }.Schedule(jobHandle);
            var toListJob2 = new PointTriangleToListJob()
            {
                pointTriangleContactQueue = pointTriangleContactQueue,
                pointTriangleContactList = pointTriangleContactList,
            }.Schedule(jobHandle);
            jobHandle = JobHandle.CombineDependencies(toListJob1, toListJob2);

            return jobHandle;
        }

        [BurstCompile]
        struct ClearBufferJob : IJob
        {
            [Unity.Collections.WriteOnly]
            public NativeQueue<EdgeEdgeContact> edgeEdgeContactQueue;

            [Unity.Collections.WriteOnly]
            public NativeQueue<PointTriangleContact> pointTriangleContactQueue;

            public void Execute()
            {
                edgeEdgeContactQueue.Clear();
                pointTriangleContactQueue.Clear();
            }
        }

#if false
        [BurstCompile]
        struct SetupContactGroupJob : IJob
        {
            public int teamCount;
            public int useQueueCount;

            // team
            public NativeArray<TeamManager.TeamData> teamDataArray;

            public void Execute()
            {
                // まずセルフコリジョンが有効で同期していないチームのグループインデックスを決定する
                int groupIndex = 0;
                var restTeam = new NativeList<int>(teamCount, Allocator.Temp);
                for (int i = 1; i < teamCount; i++)
                {
                    var tdata = teamDataArray[i];
                    if (tdata.IsValid == false || tdata.IsEnable == false || tdata.IsStepRunning == false)
                        continue;
                    if (tdata.flag.TestAny(TeamManager.Flag_Self_PointPrimitive, 3) == false)
                        continue;

                    // このチームはセルフコリジョンを実行する
                    if (tdata.flag.IsSet(TeamManager.Flag_Synchronization))
                    {
                        // 同期
                        // 後で解決する
                        restTeam.Add(i);
                    }
                    else
                    {
                        // コンタクトグループを割り振る
                        tdata.selfQueueIndex = groupIndex % useQueueCount;
                        //Debug.Log($"tid:{i} Main selfQueueIndex:{tdata.selfQueueIndex}");
                        teamDataArray[i] = tdata;
                        groupIndex++;
                    }
                }

                // 同期チームは同期先のグループIDを指す
                if (restTeam.Length > 0)
                {
                    foreach (var teamId in restTeam)
                    {
                        var tdata = teamDataArray[teamId];
                        var stdata = teamDataArray[tdata.syncTeamId];
                        while (stdata.syncTeamId != 0)
                        {
                            stdata = teamDataArray[stdata.syncTeamId];
                        }
                        tdata.selfQueueIndex = stdata.selfQueueIndex;
                        teamDataArray[teamId] = tdata;
                        //Debug.Log($"tid:{teamId} Sync selfQueueIndex:{tdata.selfQueueIndex}");
                    }
                }
            }
        }
#endif

        [BurstCompile]
        struct UpdatePrimitiveJob : IJobParallelForDefer
        {
            // プリミティブ種類
            public uint kind;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;
            //[Unity.Collections.ReadOnly]
            //public NativeArray<float3> stepBasicPositionBuffer;

            // constraint
            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingArray;

            // プリミティブごと
            public void Execute(int index)
            {
                uint pack = processingArray[index];
                int teamId = DataUtility.Unpack32Hi(pack);
                int l_index = DataUtility.Unpack32Low(pack);

                // チームはこのステップで有効であることが保証されている
                var tdata = teamDataArray[teamId];
                int pstart = tdata.particleChunk.startIndex;

                var param = parameterArray[teamId].selfCollisionConstraint;

                // primitive
                int pri_index = 0;
                switch (kind)
                {
                    case KindPoint:
                        pri_index = tdata.selfPointChunk.startIndex + l_index;
                        break;
                    case KindEdge:
                        pri_index = tdata.selfEdgeChunk.startIndex + l_index;
                        break;
                    case KindTriangle:
                        pri_index = tdata.selfTriangleChunk.startIndex + l_index;
                        break;
                }
                var primitive = primitiveArray[pri_index];
                uint flag = primitive.flagAndTeamId;

                // プリミティブ更新
                int ac = (int)kind + 1; // 軸の数
                uint fix_flag = Flag_Fix0;
                bool ignore = false;
                int fixcnt = 0;
                float depth = 0;
                for (int i = 0; i < ac; i++)
                {
                    int pindex = primitive.particleIndices[i];
                    primitive.nextPos[i] = nextPosArray[pindex];
                    primitive.oldPos[i] = oldPosArray[pindex];
                    //primitive.basePos[i] = stepBasicPositionBuffer[pindex];
                    int vindex = tdata.proxyCommonChunk.startIndex + pindex - pstart;
                    var attr = attributes[vindex];
                    if (attr.IsMove())
                        flag &= ~fix_flag;
                    else
                    {
                        flag |= fix_flag;
                        fixcnt++;
                    }
                    fix_flag <<= 1;
                    if (attr.IsInvalid())
                        ignore = true;
                    primitive.invMass[i] = MathUtility.CalcSelfCollisionInverseMass(frictionArray[pindex], attr.IsDontMove(), param.clothMass);
                    depth += depthArray[vindex];
                }
                if (fixcnt == ac)
                    flag |= Flag_AllFix;
                else
                    flag &= ~Flag_AllFix;
                if (ignore)
                    flag |= Flag_Ignore;
                primitive.flagAndTeamId = flag;
                depth /= ac;
                //float thickness = parameterArray[teamId].selfCollisionConstraint.surfaceThicknessCurveData.EvaluateCurve(depth);
                float thickness = param.surfaceThicknessCurveData.EvaluateCurve(depth);
                thickness *= tdata.scaleRatio; // team scale
                primitive.thickness = thickness;
                primitiveArray[pri_index] = primitive;

                // AABB
                var aabb = new AABB(math.min(primitive.nextPos[0], primitive.oldPos[0]), math.max(primitive.nextPos[0], primitive.oldPos[0]));
                for (int i = 1; i < ac; i++)
                {
                    aabb.Encapsulate(primitive.nextPos[i]);
                    aabb.Encapsulate(primitive.oldPos[i]);
                }
                aabb.Expand(thickness); // 厚み
                //aabb.Expand(0.03f); // 厚み

                // update sort
                int sortIndex = primitive.sortIndex;
                var sd = sortAndSweepArray[sortIndex];
                sd.flagAndTeamId = primitive.flagAndTeamId;
                sd.firstMinMax = new float2(aabb.Min.y, aabb.Max.y);
                sd.secondMinMax = new float2(aabb.Min.x, aabb.Max.x);
                sd.thirdMinMax = new float2(aabb.Min.z, aabb.Max.z);
                sortAndSweepArray[sortIndex] = sd;
                //Debug.Log($"Update Primitive[{pri_index}] pindices:{primitive.particleIndices}, flag:0x{primitive.flagAndTeamId >> 24:X}");
            }
        }

        [BurstCompile]
        unsafe struct SortJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // constraint
            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<SortData> sortAndSweepArray;

            // チームごと(Point/Edge/Triangle)
            public void Execute(int index)
            {
                int teamId = index / 3;
                int type = index % 3; // 0=Point, 1=Edge, 2=Triangle

                if (teamId == 0)
                    return;

                var tdata = teamDataArray[teamId];
                if (tdata.IsProcess == false)
                    return;

                DataChunk sortChunk = default(DataChunk);
                switch (type)
                {
                    case 0:
                        sortChunk = tdata.selfPointChunk;
                        break;
                    case 1:
                        sortChunk = tdata.selfEdgeChunk;
                        break;
                    case 2:
                        sortChunk = tdata.selfTriangleChunk;
                        break;
                }
                if (sortChunk.IsValid == false)
                    return;

                // 書き込みポインタ
                SortData* pt = (SortData*)sortAndSweepArray.GetUnsafePtr();
                pt += sortChunk.startIndex;
                NativeSortExtension.Sort(pt, sortChunk.dataLength);

                // ソート後のインデックスをプリミティブに書き戻す
                for (int i = 0; i < sortChunk.dataLength; i++)
                {
                    int sortIndex = sortChunk.startIndex + i;
                    var sd = sortAndSweepArray[sortIndex];
                    var primitive = primitiveArray[sd.primitiveIndex];
                    primitive.sortIndex = sortIndex;
                    primitiveArray[sd.primitiveIndex] = primitive;
                    //Debug.Log($"sort[{i}] primitive:{sd.primitiveIndex}, prisortidx:{primitive.sortIndex}, first:{sd.firstMinMax}, second:{sd.secondMinMax}, third:{sd.thirdMinMax}");
                }
            }
        }

        [BurstCompile]
        struct PointTriangleBroadPhaseJob : IJobParallelForDefer
        {
            public uint mainKind;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // constraint
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingPointTriangleArray;

            // contact buffer
            [Unity.Collections.WriteOnly]
            public NativeQueue<PointTriangleContact>.ParallelWriter pointTriangleContactQueue;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> intersectFlagArray;

            // 解決PointTriangleごと
            public void Execute(int index)
            {
                uint pack = processingPointTriangleArray[index];
                int teamId = DataUtility.Unpack32Hi(pack);
                int l_index = DataUtility.Unpack32Low(pack);

                // チームはこのステップで有効であることが保証されている
                var tdata = teamDataArray[teamId];

                // メインとサブのチャンク
                bool isPoint = mainKind == KindPoint;
                var mainChunk = isPoint ? tdata.selfPointChunk : tdata.selfTriangleChunk;
                var subChunk = isPoint ? tdata.selfTriangleChunk : tdata.selfPointChunk;

                // Main Primitive
                int pri_index = mainChunk.startIndex + l_index;
                var primitive0 = primitiveArray[pri_index];
                var sd0 = sortAndSweepArray[primitive0.sortIndex];

                // 無効判定
                if (primitive0.IsIgnore())
                    return;

                // 交差中ならば無効
                if (tdata.flag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 6))
                {
                    int ac = isPoint ? 1 : 3;
                    for (int i = 0; i < ac; i++)
                    {
                        if (intersectFlagArray[primitive0.particleIndices[i]] > 0)
                            return;
                    }
                }

                //=============================================================
                // Self
                //=============================================================
                if (tdata.flag.IsSet(isPoint ? TeamManager.Flag_Self_PointTriangle : TeamManager.Flag_Self_TrianglePoint))
                {
                    SweepTest(-1, ref primitive0, sd0, subChunk, true);
                }

                //=============================================================
                // Sync
                //=============================================================
                if (tdata.flag.IsSet(isPoint ? TeamManager.Flag_Sync_PointTriangle : TeamManager.Flag_Sync_TrianglePoint) && tdata.syncTeamId > 0)
                {
                    var stdata = teamDataArray[tdata.syncTeamId];
                    SweepTest(-1, ref primitive0, sd0, isPoint ? stdata.selfTriangleChunk : stdata.selfPointChunk, false);
                }

                //=============================================================
                // Parent Sync
                //=============================================================
                if (tdata.flag.IsSet(isPoint ? TeamManager.Flag_PSync_PointTriangle : TeamManager.Flag_PSync_TrianglePoint))
                {
                    int cnt = tdata.syncParentTeamId.Length;
                    for (int j = 0; j < cnt; j++)
                    {
                        int parentTeamId = tdata.syncParentTeamId[j];
                        var stdata = teamDataArray[parentTeamId];
                        if (stdata.flag.IsSet(isPoint ? TeamManager.Flag_Sync_TrianglePoint : TeamManager.Flag_Sync_PointTriangle))
                        {
                            SweepTest(-1, ref primitive0, sd0, isPoint ? stdata.selfTriangleChunk : stdata.selfPointChunk, false);
                        }
                    }
                }
            }

            void SweepTest(int sortIndex, ref Primitive primitive0, in SortData sd0, in DataChunk subChunk, bool connectionCheck)
            {
                Debug.Assert(subChunk.IsValid);

                // スイープ
                if (sortIndex < 0)
                    sortIndex = BinarySearchSortAndlSweep(ref sortAndSweepArray, sd0, subChunk);

                float end = sd0.firstMinMax.y;
                int endIndex = subChunk.startIndex + subChunk.dataLength;
                while (sortIndex < endIndex)
                {
                    var sd1 = sortAndSweepArray[sortIndex];
                    sortIndex++;

                    // first
                    if (sd1.firstMinMax.x <= end)
                    {
                        // second
                        if (sd1.secondMinMax.y < sd0.secondMinMax.x || sd1.secondMinMax.x > sd0.secondMinMax.y)
                            continue;

                        // third
                        if (sd1.thirdMinMax.y < sd0.thirdMinMax.x || sd1.thirdMinMax.x > sd0.thirdMinMax.y)
                            continue;

                        // この時点で両方のAABBは衝突している
                        var primitive1 = primitiveArray[sd1.primitiveIndex];

                        // 無効判定
                        if (primitive1.IsIgnore())
                            continue;

                        // プリミティブ同士が接続している場合は無効
                        if (connectionCheck && primitive0.AnyParticle(primitive1))
                            continue;

                        // 両方のプリミティブが完全固定ならば無効
                        if ((primitive0.flagAndTeamId & Flag_AllFix) != 0 && (primitive1.flagAndTeamId & Flag_AllFix) != 0)
                            continue;

                        // 交差判定
                        // 厚みとSCR
                        float solveThickness = primitive0.GetSolveThickness(primitive1);
                        float scr = solveThickness * Define.System.SelfCollisionSCR;
                        if (solveThickness < 0.0001f)
                            continue;

                        // 接触予測判定
                        if (mainKind == KindPoint)
                            BroadPointTriangle(ref primitive0, ref primitive1, solveThickness, scr, Define.System.SelfCollisionPointTriangleAngleCos);
                        else
                            BroadPointTriangle(ref primitive1, ref primitive0, solveThickness, scr, Define.System.SelfCollisionPointTriangleAngleCos);
                    }
                    else
                        break;
                }
            }

            void BroadPointTriangle(ref Primitive p_pri, ref Primitive t_pri, float thickness, float scr, float ang)
            {
                // 変位
                var dA = p_pri.nextPos.c0 - p_pri.oldPos.c0;
                var dB0 = t_pri.nextPos.c0 - t_pri.oldPos.c0;
                var dB1 = t_pri.nextPos.c1 - t_pri.oldPos.c1;
                var dB2 = t_pri.nextPos.c2 - t_pri.oldPos.c2;

                //=========================================================
                // 衝突予測と格納
                //=========================================================
                float3 uvw, cp;
                // 移動前ポイントと移動前トライアングルへの最近接点
                cp = MathUtility.ClosestPtPointTriangle(p_pri.oldPos.c0, t_pri.oldPos.c0, t_pri.oldPos.c1, t_pri.oldPos.c2, out uvw);

                // 最近接点座標の変位を求める
                float3 dt = dB0 * uvw.x + dB1 * uvw.y + dB2 * uvw.z;

                // 最近接点ベクトル
                float3 cv = cp - p_pri.oldPos.c0;
                float cvlen = math.length(cv);
                if (cvlen > Define.System.Epsilon)
                {
                    var n = cv / cvlen;

                    // 変位dp,dtをnに投影して距離チェック
                    float l0 = math.dot(n, dA);
                    float l1 = math.dot(n, dt);
                    float l = cvlen - l0 + l1;

                    // ダイナミックThicknessテスト
#if false
                    if (p_pri.GetTeamId() == t_pri.GetTeamId())
                    {
                        var bcp = MathUtility.ClosestPtPointTriangle(p_pri.basePos.c0, t_pri.basePos.c0, t_pri.basePos.c1, t_pri.basePos.c2, out _);
                        float blen = math.distance(p_pri.basePos.c0, bcp);
                        //blen *= 0.7f;
                        if (blen < 0.005f)
                            return;
                        //if (thickness > blen)
                        //    Debug.Log($"PT thickness:{thickness}, blen:{blen}");
                        thickness = math.min(thickness, blen);
                        scr = thickness * Define.System.SelfCollisionSCR;
                    }
#endif

                    // 接続判定
                    if (l < (thickness + scr))
                    {
                        //=========================================================
                        // 方向性判定
                        //=========================================================
                        float sign = 0;
                        // 移動前トライアングル法線
                        float3 otn = MathUtility.TriangleNormal(t_pri.oldPos.c0, t_pri.oldPos.c1, t_pri.oldPos.c2);

                        // 移動前のパーティクル方向性
                        n = math.normalize(p_pri.oldPos.c0 - cp);
                        float dot = math.dot(otn, n);

                        // 移動前にトライアングル面に対してほぼ水平ならば無視する
                        if (math.abs(dot) >= ang)
                            sign = math.sign(dot);
                        else
                            return;

                        //=========================================================
                        // 接触情報作成
                        //=========================================================
                        var contact = new PointTriangleContact();
                        contact.flagAndTeamId0 = p_pri.flagAndTeamId | Flag_Enable;
                        contact.flagAndTeamId1 = t_pri.flagAndTeamId;
                        contact.thickness = (half)thickness;
                        contact.sign = (half)sign;
                        contact.pointParticleIndex = p_pri.particleIndices.x;
                        contact.triangleParticleIndex = t_pri.particleIndices;
                        contact.pointInvMass = (half)p_pri.invMass.x;
                        contact.triangleInvMass = (half3)t_pri.invMass;
                        //Debug.Log(contact.ToString());

                        pointTriangleContactQueue.Enqueue(contact);
                    }
                }
            }
        }

        [BurstCompile]
        struct EdgeEdgeBroadPhaseJob : IJobParallelForDefer
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // constraint
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingEdgeEdgeArray;

            // contact buffer
            [Unity.Collections.WriteOnly]
            public NativeQueue<EdgeEdgeContact>.ParallelWriter edgeEdgeContactQueue;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> intersectFlagArray;

            // 解決エッジごと
            public void Execute(int index)
            {
                uint pack = processingEdgeEdgeArray[index];
                int teamId = DataUtility.Unpack32Hi(pack);
                int l_index = DataUtility.Unpack32Low(pack);

                // チームはこのステップで有効であることが保証されている
                var tdata = teamDataArray[teamId];

                int pri_index = tdata.selfEdgeChunk.startIndex + l_index;
                var primitive0 = primitiveArray[pri_index];

                // 無効
                if (primitive0.IsIgnore())
                    return;

                // 交差中ならば無効
                if (tdata.flag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 6))
                {
                    if (intersectFlagArray[primitive0.particleIndices.x] > 0)
                        return;
                    if (intersectFlagArray[primitive0.particleIndices.y] > 0)
                        return;
                }

                // sort
                var sd0 = sortAndSweepArray[primitive0.sortIndex];

                //=============================================================
                // Self
                //=============================================================
                if (tdata.flag.IsSet(TeamManager.Flag_Self_EdgeEdge))
                {
                    SweepTest(primitive0.sortIndex + 1, ref primitive0, sd0, tdata.selfEdgeChunk, true);
                }

                //=============================================================
                // Sync
                //=============================================================
                if (tdata.flag.IsSet(TeamManager.Flag_Sync_EdgeEdge) && tdata.syncTeamId > 0)
                {
                    var stdata = teamDataArray[tdata.syncTeamId];
                    SweepTest(-1, ref primitive0, sd0, stdata.selfEdgeChunk, false);
                }

                //=============================================================
                // Parent Sync
                //=============================================================
                if (tdata.flag.IsSet(TeamManager.Flag_PSync_EdgeEdge))
                {
                    int cnt = tdata.syncParentTeamId.Length;
                    for (int j = 0; j < cnt; j++)
                    {
                        int parentTeamId = tdata.syncParentTeamId[j];
                        var stdata = teamDataArray[parentTeamId];
                        if (stdata.flag.IsSet(TeamManager.Flag_Sync_EdgeEdge))
                        {
                            SweepTest(-1, ref primitive0, sd0, stdata.selfEdgeChunk, false);
                        }
                    }
                }
            }

            void SweepTest(int sortIndex, ref Primitive primitive0, in SortData sd0, in DataChunk subChunk, bool connectionCheck)
            {
                // スイープ
                if (sortIndex < 0)
                    sortIndex = BinarySearchSortAndlSweep(ref sortAndSweepArray, sd0, subChunk);
                float end = sd0.firstMinMax.y;
                int endIndex = subChunk.startIndex + subChunk.dataLength;
                while (sortIndex < endIndex)
                {
                    var sd1 = sortAndSweepArray[sortIndex];
                    sortIndex++;

                    // first
                    if (sd1.firstMinMax.x <= end)
                    {
                        // second
                        if (sd1.secondMinMax.y < sd0.secondMinMax.x || sd1.secondMinMax.x > sd0.secondMinMax.y)
                            continue;

                        // third
                        if (sd1.thirdMinMax.y < sd0.thirdMinMax.x || sd1.thirdMinMax.x > sd0.thirdMinMax.y)
                            continue;

                        // この時点で両方のAABBは衝突している
                        var primitive1 = primitiveArray[sd1.primitiveIndex];

                        // 無効判定
                        if (primitive1.IsIgnore())
                            continue;

                        // プリミティブ同士が接続している場合は無効
                        if (connectionCheck && primitive0.AnyParticle(primitive1))
                            continue;

                        // 両方のプリミティブが完全固定ならば無効
                        if ((primitive0.flagAndTeamId & Flag_AllFix) != 0 && (primitive1.flagAndTeamId & Flag_AllFix) != 0)
                            continue;

                        // 交差判定
                        // 厚みとSCR
                        float solveThickness = primitive0.GetSolveThickness(primitive1);
                        float scr = solveThickness * Define.System.SelfCollisionSCR;
                        if (solveThickness < 0.0001f)
                            continue;

                        // 接触予測判定
                        BroadEdgeEdge(ref primitive0, ref primitive1, solveThickness, scr);
                    }
                    else
                        break;
                }
            }

            void BroadEdgeEdge(ref Primitive pri0, ref Primitive pri1, float thickness, float scr)
            {
                // 移動前の２つの線分の最近接点
                float s, t;
                float3 cA, cB;
                float csqlen = MathUtility.ClosestPtSegmentSegment(pri0.oldPos[0], pri0.oldPos[1], pri1.oldPos[0], pri1.oldPos[1], out s, out t, out cA, out cB);
                float clen = math.sqrt(csqlen); // 最近接点の距離
                if (clen < 1e-09f)
                    return;

                // 押出法線
                float3 n = (cA - cB) / clen;

                // 最近接点での変位
                var dA0 = pri0.nextPos[0] - pri0.oldPos[0];
                var dA1 = pri0.nextPos[1] - pri0.oldPos[1];
                var dB0 = pri1.nextPos[0] - pri1.oldPos[0];
                var dB1 = pri1.nextPos[1] - pri1.oldPos[1];
                float3 da = math.lerp(dA0, dA1, s);
                float3 db = math.lerp(dB0, dB1, t);

                // 変位da,dbをnに投影して距離チェック
                float l0 = math.dot(n, da);
                float l1 = math.dot(n, db);
                float l = clen + l0 - l1;

                // ダイナミックThicknessテスト
#if false
                if (pri0.GetTeamId() == pri1.GetTeamId())
                {
                    float bsqlen = MathUtility.ClosestPtSegmentSegment(pri0.basePos[0], pri0.basePos[1], pri1.basePos[0], pri1.basePos[1], out _, out _, out _, out _);
                    float blen = math.sqrt(bsqlen);
                    //blen *= 0.7f;
                    if (blen < 0.005f)
                        return;
                    //if (thickness > blen)
                    //    Debug.Log($"EE thickness:{thickness}, blen:{blen}");
                    thickness = math.min(thickness, blen);
                    scr = thickness * Define.System.SelfCollisionSCR;
                }
#endif

                // 接触判定
                if (l > (thickness + scr))
                    return;

                //=========================================================
                // 接触情報作成
                //=========================================================
                var contact = new EdgeEdgeContact();
                contact.flagAndTeamId0 = pri0.flagAndTeamId | Flag_Enable;
                contact.flagAndTeamId1 = pri1.flagAndTeamId;
                contact.thickness = (half)thickness;
                contact.s = (half)s;
                contact.t = (half)t;
                contact.n = (half3)n;
                contact.edgeInvMass0 = (half2)pri0.invMass.xy;
                contact.edgeInvMass1 = (half2)pri1.invMass.xy;
                contact.edgeParticleIndex0 = pri0.particleIndices.xy;
                contact.edgeParticleIndex1 = pri1.particleIndices.xy;
                //Debug.Log(contact.ToString());

                // キューへの振り分け
                edgeEdgeContactQueue.Enqueue(contact);
            }
        }

        [BurstCompile]
        struct EdgeEdgeToListJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeQueue<EdgeEdgeContact> edgeEdgeContactQueue;

            [NativeDisableParallelForRestriction]
            public NativeList<EdgeEdgeContact> edgeEdgeContactList;

            public void Execute()
            {
                edgeEdgeContactList.Clear();
                if (edgeEdgeContactQueue.Count > 0)
                    edgeEdgeContactList.AddRange(edgeEdgeContactQueue.ToArray(Allocator.Temp));
            }
        }

        [BurstCompile]
        struct PointTriangleToListJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeQueue<PointTriangleContact> pointTriangleContactQueue;

            [NativeDisableParallelForRestriction]
            public NativeList<PointTriangleContact> pointTriangleContactList;

            public void Execute()
            {
                pointTriangleContactList.Clear();
                if (pointTriangleContactQueue.Count > 0)
                    pointTriangleContactList.AddRange(pointTriangleContactQueue.ToArray(Allocator.Temp));
            }
        }

        JobHandle UpdateBroadPhase(JobHandle jobHandle)
        {
            var sm = MagicaManager.Simulation;

            // EdgeEdge
            var updateJob1 = new UpdateEdgeEdgeBroadPhaseJob()
            {
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                oldPosArray = sm.oldPosArray.GetNativeArray(),
                edgeEdgeContactList = edgeEdgeContactList,
            };
            jobHandle = updateJob1.Schedule(edgeEdgeContactList, 16, jobHandle);

            // PointTriangle
            var updateJob2 = new UpdatePointTriangleBroadPhaseJob()
            {
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                oldPosArray = sm.oldPosArray.GetNativeArray(),
                pointTriangleContactList = pointTriangleContactList,
            };
            jobHandle = updateJob2.Schedule(pointTriangleContactList, 16, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct UpdateEdgeEdgeBroadPhaseJob : IJobParallelForDefer
        {
            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;

            [NativeDisableParallelForRestriction]
            public NativeList<EdgeEdgeContact> edgeEdgeContactList;

            // コンタクトごと
            public void Execute(int index)
            {
                var contact = edgeEdgeContactList[index];

                var oldPosA0 = oldPosArray[contact.edgeParticleIndex0.x];
                var oldPosA1 = oldPosArray[contact.edgeParticleIndex0.y];
                var oldPosB0 = oldPosArray[contact.edgeParticleIndex1.x];
                var oldPosB1 = oldPosArray[contact.edgeParticleIndex1.y];

                var nextPosA0 = nextPosArray[contact.edgeParticleIndex0.x];
                var nextPosA1 = nextPosArray[contact.edgeParticleIndex0.y];
                var nextPosB0 = nextPosArray[contact.edgeParticleIndex1.x];
                var nextPosB1 = nextPosArray[contact.edgeParticleIndex1.y];

                // 移動前の２つの線分の最近接点
                float s, t;
                float3 cA, cB;
                float csqlen = MathUtility.ClosestPtSegmentSegment(oldPosA0, oldPosA1, oldPosB0, oldPosB1, out s, out t, out cA, out cB);
                float clen = math.sqrt(csqlen); // 最近接点の距離
                if (clen < 1e-09f)
                    return;

                // 押出法線
                float3 n = (cA - cB) / clen;

                // 最近接点での変位
                var dA0 = nextPosA0 - oldPosA0;
                var dA1 = nextPosA1 - oldPosA1;
                var dB0 = nextPosB0 - oldPosB0;
                var dB1 = nextPosB1 - oldPosB1;
                float3 da = math.lerp(dA0, dA1, s);
                float3 db = math.lerp(dB0, dB1, t);

                // 変位da,dbをnに投影して距離チェック
                float l0 = math.dot(n, da);
                float l1 = math.dot(n, db);
                float l = clen + l0 - l1;

                // 接触判定
                float scr = contact.thickness;
                if (l > (contact.thickness + scr))
                {
                    contact.flagAndTeamId0 &= ~Flag_Enable;
                }
                else
                {
                    contact.flagAndTeamId0 |= Flag_Enable;
                    contact.s = (half)s;
                    contact.t = (half)t;
                    contact.n = (half3)n;
                }

                edgeEdgeContactList[index] = contact;
            }
        }

        [BurstCompile]
        struct UpdatePointTriangleBroadPhaseJob : IJobParallelForDefer
        {
            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;

            [NativeDisableParallelForRestriction]
            public NativeList<PointTriangleContact> pointTriangleContactList;

            // コンタクトごと
            public void Execute(int index)
            {
                var contact = pointTriangleContactList[index];

                bool enable = false;

                var oldPosA = oldPosArray[contact.pointParticleIndex];
                var oldPosB0 = oldPosArray[contact.triangleParticleIndex.x];
                var oldPosB1 = oldPosArray[contact.triangleParticleIndex.y];
                var oldPosB2 = oldPosArray[contact.triangleParticleIndex.z];

                var nextPosA = nextPosArray[contact.pointParticleIndex];
                var nextPosB0 = nextPosArray[contact.triangleParticleIndex.x];
                var nextPosB1 = nextPosArray[contact.triangleParticleIndex.y];
                var nextPosB2 = nextPosArray[contact.triangleParticleIndex.z];

                // 変位
                var dA = nextPosA - oldPosA;
                var dB0 = nextPosB0 - oldPosB0;
                var dB1 = nextPosB1 - oldPosB1;
                var dB2 = nextPosB2 - oldPosB2;

                //=========================================================
                // 衝突予測と格納
                //=========================================================
                float3 uvw, cp;
                // 移動前ポイントと移動前トライアングルへの最近接点
                cp = MathUtility.ClosestPtPointTriangle(oldPosA, oldPosB0, oldPosB1, oldPosB2, out uvw);

                // 最近接点座標の変位を求める
                float3 dt = dB0 * uvw.x + dB1 * uvw.y + dB2 * uvw.z;

                // 最近接点ベクトル
                float3 cv = cp - oldPosA;
                float cvlen = math.length(cv);
                if (cvlen > Define.System.Epsilon)
                {
                    var n = cv / cvlen;

                    // 変位dp,dtをnに投影して距離チェック
                    float l0 = math.dot(n, dA);
                    float l1 = math.dot(n, dt);
                    float l = cvlen - l0 + l1;

                    // 接続判定
                    float scr = contact.thickness;
                    if (l < (contact.thickness + scr))
                    {
                        enable = true;
                    }
                }

                if (enable)
                    contact.flagAndTeamId0 |= Flag_Enable;
                else
                    contact.flagAndTeamId0 &= ~Flag_Enable;

                pointTriangleContactList[index] = contact;
            }
        }

        [BurstCompile]
        unsafe struct SolverEdgeEdgeJob : IJobParallelForDefer
        {
            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // contact
            [Unity.Collections.ReadOnly]
            public NativeArray<EdgeEdgeContact> edgeEdgeContactArray;

            // output
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // コンタクトごと
            public void Execute(int index)
            {
                //Debug.Log($"EdgeEdgeContactCount:{edgeEdgeContactQueue.Count}");

                var contact = edgeEdgeContactArray[index];
                if ((contact.flagAndTeamId0 & Flag_Enable) == 0)
                    return;

                var nextPosA0 = nextPosArray[contact.edgeParticleIndex0.x];
                var nextPosA1 = nextPosArray[contact.edgeParticleIndex0.y];
                var nextPosB0 = nextPosArray[contact.edgeParticleIndex1.x];
                var nextPosB1 = nextPosArray[contact.edgeParticleIndex1.y];

                float s = contact.s;
                float t = contact.t;
                float3 n = contact.n;
                float thickness = contact.thickness;

                // 移動前に接触判定を行った位置の移動後の位置a/bと方向ベクトル
                float3 a = math.lerp(nextPosA0, nextPosA1, s);
                float3 b = math.lerp(nextPosB0, nextPosB1, t);
                float3 v = a - b;

                // 接触法線に現在の距離を投影させる
                float l = math.dot(n, v);
                //Debug.Log($"A({pA0}-{pA1}), B({pB0}-{pB1}) s:{s}, t:{t} l:{l}");
                if (l > thickness)
                    return;

                float invMassA0 = contact.edgeInvMass0.x;
                float invMassA1 = contact.edgeInvMass0.y;
                float invMassB0 = contact.edgeInvMass1.x;
                float invMassB1 = contact.edgeInvMass1.y;

                // 離す距離
                float C = thickness - l;

                // お互いを離す
                float b0 = 1.0f - s;
                float b1 = s;
                float b2 = 1.0f - t;
                float b3 = t;
                float3 grad0 = n * b0;
                float3 grad1 = n * b1;
                float3 grad2 = -n * b2;
                float3 grad3 = -n * b3;

                float S = invMassA0 * b0 * b0 + invMassA1 * b1 * b1 + invMassB0 * b2 * b2 + invMassB1 * b3 * b3;
                if (S == 0.0f)
                    return;

                S = C / S;

                float3 _A0 = S * invMassA0 * grad0;
                float3 _A1 = S * invMassA1 * grad1;
                float3 _B0 = S * invMassB0 * grad2;
                float3 _B1 = S * invMassB1 * grad3;

                //=====================================================
                // 書き込み
                //=====================================================
                int* cntPt = (int*)countArray.GetUnsafePtr();
                int* sumPt = (int*)sumArray.GetUnsafePtr();
                if ((contact.flagAndTeamId0 & Flag_Fix0) == 0)
                    InterlockUtility.AddFloat3(contact.edgeParticleIndex0.x, _A0, cntPt, sumPt);
                //nextPosArray[contact.edgeParticleIndex0.x] = nextPosA0 + _A0;
                if ((contact.flagAndTeamId0 & Flag_Fix1) == 0)
                    InterlockUtility.AddFloat3(contact.edgeParticleIndex0.y, _A1, cntPt, sumPt);
                //nextPosArray[contact.edgeParticleIndex0.y] = nextPosA1 + _A1;
                if ((contact.flagAndTeamId1 & Flag_Fix0) == 0)
                    InterlockUtility.AddFloat3(contact.edgeParticleIndex1.x, _B0, cntPt, sumPt);
                //nextPosArray[contact.edgeParticleIndex1.x] = nextPosB0 + _B0;
                if ((contact.flagAndTeamId1 & Flag_Fix1) == 0)
                    InterlockUtility.AddFloat3(contact.edgeParticleIndex1.y, _B1, cntPt, sumPt);
                //nextPosArray[contact.edgeParticleIndex1.y] = nextPosB1 + _B1;
            }
        }

        [BurstCompile]
        unsafe struct SolverPointTriangleJob : IJobParallelForDefer
        {
            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // contact
            [Unity.Collections.ReadOnly]
            public NativeArray<PointTriangleContact> pointTriangleContactArray;

            // output
            [NativeDisableParallelForRestriction]
            public NativeArray<int> countArray;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sumArray;

            // コンタクトごと
            public void Execute(int index)
            {
                //Debug.Log($"PointTriangleContactCount:{pointTriangleContactQueue.Count}");

                var contact = pointTriangleContactArray[index];
                if ((contact.flagAndTeamId0 & Flag_Enable) == 0)
                    return;

                // 接触距離
                float thickness = contact.thickness;

                // トライアングル情報
                int3 tp = contact.triangleParticleIndex;
                float3 nextPos0 = nextPosArray[tp.x];
                float3 nextPos1 = nextPosArray[tp.y];
                float3 nextPos2 = nextPosArray[tp.z];
                float invMass0 = contact.triangleInvMass.x;
                float invMass1 = contact.triangleInvMass.y;
                float invMass2 = contact.triangleInvMass.z;

                // 移動後トライアングル法線
                float3 tn = MathUtility.TriangleNormal(nextPos0, nextPos1, nextPos2);

                // 対象パーティクル情報
                int t_pindex = contact.pointParticleIndex;
                float3 nextPos = nextPosArray[t_pindex];
                float invMass = contact.pointInvMass;

                //=====================================================
                // 衝突の解決
                //=====================================================
                // 移動後ポイントと移動後トライアングルへの最近接点
                float3 uvw;
                MathUtility.ClosestPtPointTriangle(nextPos, nextPos0, nextPos1, nextPos2, out uvw);

                // 押し出し方向（移動後のトライアングル法線）
                // 移動前に裏側ならば反転させる
                float sign = contact.sign;
                float3 n = tn * sign;

                // 押し出し法線方向に投影した距離
                float dist = math.dot(n, nextPos - nextPos0);
                //Debug.Log($"dist:{dist}");
                if (dist >= thickness)
                    return;

                // 引き離す距離
                float restDist = thickness;

                // 押し出し
                float C = dist - restDist;

                float3 grad = n;
                float3 grad0 = -n * uvw[0];
                float3 grad1 = -n * uvw[1];
                float3 grad2 = -n * uvw[2];

                float s = invMass + invMass0 * uvw.x * uvw.x + invMass1 * uvw.y * uvw.y + invMass2 * uvw.z * uvw.z;
                if (s == 0.0f)
                    return;
                s = C / s;

                float3 corr = -s * invMass * grad;
                float3 corr0 = -s * invMass0 * grad0;
                float3 corr1 = -s * invMass1 * grad1;
                float3 corr2 = -s * invMass2 * grad2;

                //=====================================================
                // 書き込み
                //=====================================================
                int* cntPt = (int*)countArray.GetUnsafePtr();
                int* sumPt = (int*)sumArray.GetUnsafePtr();
                if ((contact.flagAndTeamId0 & Flag_Fix0) == 0)
                    InterlockUtility.AddFloat3(t_pindex, corr, cntPt, sumPt);
                //nextPosArray[t_pindex] = nextPos + corr;
                if ((contact.flagAndTeamId1 & Flag_Fix0) == 0)
                    InterlockUtility.AddFloat3(tp.x, corr0, cntPt, sumPt);
                //nextPosArray[tp.x] = nextPos0 + corr0;
                if ((contact.flagAndTeamId1 & Flag_Fix1) == 0)
                    InterlockUtility.AddFloat3(tp.y, corr1, cntPt, sumPt);
                //nextPosArray[tp.y] = nextPos1 + corr1;
                if ((contact.flagAndTeamId1 & Flag_Fix2) == 0)
                    InterlockUtility.AddFloat3(tp.z, corr2, cntPt, sumPt);
                //nextPosArray[tp.z] = nextPos2 + corr2;

                //Debug.Log($"Solve:{contact.ToString()}");
            }
        }

        //=========================================================================================
        /// <summary>
        /// 交差（絡まり）の解決
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        unsafe JobHandle SolveIntersect(JobHandle jobHandle)
        {
            if (IntersectCount == 0)
                return jobHandle;

            var tm = MagicaManager.Team;
            var sm = MagicaManager.Simulation;

            // 交差フラグクリア
            jobHandle = JobUtility.Fill(intersectFlagArray, intersectFlagArray.Length, 0, jobHandle);

            // EdgePrimitiveのnextPos更新
            var updateJob1 = new IntersectUpdatePrimitiveJob()
            {
                kind = KindEdge,
                teamDataArray = tm.teamDataArray.GetNativeArray(),
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                primitiveArray = primitiveArray.GetNativeArray(),
                processingArray = sm.processingSelfEdgeEdge.Buffer,
            };
            jobHandle = updateJob1.Schedule(sm.processingSelfEdgeEdge.GetJobSchedulePtr(), 16, jobHandle);

            // TrianglePrimitiveのnextPos更新
            var updateJob2 = new IntersectUpdatePrimitiveJob()
            {
                kind = KindTriangle,
                teamDataArray = tm.teamDataArray.GetNativeArray(),
                nextPosArray = sm.nextPosArray.GetNativeArray(),
                primitiveArray = primitiveArray.GetNativeArray(),
                processingArray = sm.processingSelfTrianglePoint.Buffer,
            };
            jobHandle = updateJob2.Schedule(sm.processingSelfTrianglePoint.GetJobSchedulePtr(), 16, jobHandle);

            // EdgeTriangle交差判定
            // !重い処理なのでステップごとに分割して少しずつ実行する
            int stepCount = sm.SimulationStepCount;
            int execNumber = stepCount % Define.System.SelfCollisionIntersectDiv;

            // !正確にはここでは交差を実際には解決しない
            // !交差しているEdgeTriangleのパーティクルにフラグを付け、次のステップのセルフ衝突判定から除外することで絡まりを解く
            // !衝突判定を行わないことでパーティクルは自由になり復元制約の効果で元の姿勢に戻ろうとする（この時からまりが解ける）
            // !この方法は論文にない独自のもので精度も高くないが安価なコストで交差をある程度解消することでできる（ランタイム向き）

            // Edgeベース
            var intersectJob1 = new IntersectEdgeTriangleJob()
            {
                mainKind = KindEdge,
                execNumber = execNumber,
                div = Define.System.SelfCollisionIntersectDiv,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                primitiveArray = primitiveArray.GetNativeArray(),
                sortAndSweepArray = sortAndSweepArray.GetNativeArray(),
                processingEdgeEdgeArray = sm.processingSelfEdgeEdge.Buffer,
                intersectFlagArray = intersectFlagArray,
            };
            jobHandle = intersectJob1.Schedule(sm.processingSelfEdgeEdge.GetJobSchedulePtr(), 16, jobHandle);

            // Triangleベース
            var intersectJob2 = new IntersectEdgeTriangleJob()
            {
                mainKind = KindTriangle,
                execNumber = execNumber,
                div = Define.System.SelfCollisionIntersectDiv,

                teamDataArray = tm.teamDataArray.GetNativeArray(),
                primitiveArray = primitiveArray.GetNativeArray(),
                sortAndSweepArray = sortAndSweepArray.GetNativeArray(),
                processingEdgeEdgeArray = sm.processingSelfTrianglePoint.Buffer,
                intersectFlagArray = intersectFlagArray,
            };
            jobHandle = intersectJob2.Schedule(sm.processingSelfTrianglePoint.GetJobSchedulePtr(), 16, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct IntersectUpdatePrimitiveJob : IJobParallelForDefer
        {
            // プリミティブ種類
            public uint kind;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;

            // constraint
            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingArray;

            public void Execute(int index)
            {
                uint pack = processingArray[index];
                int teamId = DataUtility.Unpack32Hi(pack);
                int l_index = DataUtility.Unpack32Low(pack);

                // チームはこのステップで有効であることが保証されている
                var tdata = teamDataArray[teamId];
                if (kind == KindEdge && tdata.flag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 3) == false)
                    return;
                if (kind == KindTriangle && tdata.flag.TestAny(TeamManager.Flag_Self_TriangleEdgeIntersect, 3) == false)
                    return;

                // primitive
                int pri_index = 0;
                switch (kind)
                {
                    case KindPoint:
                        pri_index = tdata.selfPointChunk.startIndex + l_index;
                        break;
                    case KindEdge:
                        pri_index = tdata.selfEdgeChunk.startIndex + l_index;
                        break;
                    case KindTriangle:
                        pri_index = tdata.selfTriangleChunk.startIndex + l_index;
                        break;
                }
                var primitive = primitiveArray[pri_index];

                // プリミティブnextPos更新
                int ac = (int)kind + 1; // 軸の数
                for (int i = 0; i < ac; i++)
                {
                    int pindex = primitive.particleIndices[i];
                    primitive.nextPos[i] = nextPosArray[pindex];
                }
                primitiveArray[pri_index] = primitive;
            }
        }

        [BurstCompile]
        struct IntersectEdgeTriangleJob : IJobParallelForDefer
        {
            public uint mainKind;
            public int execNumber;
            public int div;

            // team
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // constraint
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<SortData> sortAndSweepArray;

            // processing
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> processingEdgeEdgeArray;

            // out
            [NativeDisableParallelForRestriction]
            public NativeArray<byte> intersectFlagArray;

            // 解決Edge/Triangleごと
            public void Execute(int index)
            {
#if true
                // 分割実行判定
                if (index % div != execNumber)
                    return;
#endif

                uint pack = processingEdgeEdgeArray[index];
                int teamId = DataUtility.Unpack32Hi(pack);
                int l_index = DataUtility.Unpack32Low(pack);

                // チームはこのステップで有効であることが保証されている
                var tdata = teamDataArray[teamId];
                if (mainKind == KindEdge && tdata.flag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 3) == false)
                    return;
                if (mainKind == KindTriangle && tdata.flag.TestAny(TeamManager.Flag_Self_TriangleEdgeIntersect, 3) == false)
                    return;

                // メインとサブのチャンク
                bool isEdge = mainKind == KindEdge;
                var mainChunk = isEdge ? tdata.selfEdgeChunk : tdata.selfTriangleChunk;
                var subChunk = isEdge ? tdata.selfTriangleChunk : tdata.selfEdgeChunk;

                // メインプリミティブ情報
                int priIndex0 = mainChunk.startIndex + l_index;
                var primitive0 = primitiveArray[priIndex0];
                var sd0 = sortAndSweepArray[primitive0.sortIndex];

                //=============================================================
                // Self
                //=============================================================
                if (tdata.flag.IsSet(isEdge ? TeamManager.Flag_Self_EdgeTriangleIntersect : TeamManager.Flag_Self_TriangleEdgeIntersect))
                {
                    SweepTest(ref primitive0, sd0, subChunk, true);
                }

                //=============================================================
                // Sync
                //=============================================================
                if (tdata.flag.IsSet(isEdge ? TeamManager.Flag_Sync_EdgeTriangleIntersect : TeamManager.Flag_Sync_TriangleEdgeIntersect))
                {
                    var stdata = teamDataArray[tdata.syncTeamId];
                    SweepTest(ref primitive0, sd0, isEdge ? stdata.selfTriangleChunk : stdata.selfEdgeChunk, false);
                }

                //=============================================================
                // Parent Sync
                //=============================================================
                if (tdata.flag.IsSet(isEdge ? TeamManager.Flag_PSync_EdgeTriangleIntersect : TeamManager.Flag_PSync_TriangleEdgeIntersect))
                {
                    int cnt = tdata.syncParentTeamId.Length;
                    for (int j = 0; j < cnt; j++)
                    {
                        int parentTeamId = tdata.syncParentTeamId[j];
                        var stdata = teamDataArray[parentTeamId];
                        if (stdata.flag.IsSet(isEdge ? TeamManager.Flag_Sync_TriangleEdgeIntersect : TeamManager.Flag_Sync_EdgeTriangleIntersect))
                        {
                            SweepTest(ref primitive0, sd0, isEdge ? stdata.selfTriangleChunk : stdata.selfEdgeChunk, false);
                        }
                    }
                }
            }

            void SweepTest(ref Primitive primitive0, in SortData sd0, in DataChunk subChunk, bool connectionCheck)
            {
                // スイープ
                int sortIndex = BinarySearchSortAndlSweep(ref sortAndSweepArray, sd0, subChunk);
                float end = sd0.firstMinMax.y;
                int endIndex = subChunk.startIndex + subChunk.dataLength;
                while (sortIndex < endIndex)
                {
                    var sd1 = sortAndSweepArray[sortIndex];
                    sortIndex++;

                    // first
                    if (sd1.firstMinMax.x <= end)
                    {
                        // second
                        if (sd1.secondMinMax.y < sd0.secondMinMax.x || sd1.secondMinMax.x > sd0.secondMinMax.y)
                            continue;

                        // third
                        if (sd1.thirdMinMax.y < sd0.thirdMinMax.x || sd1.thirdMinMax.x > sd0.thirdMinMax.y)
                            continue;

                        // この時点で両方のAABBは衝突している
                        var primitive1 = primitiveArray[sd1.primitiveIndex];

                        // プリミティブ同士が接続している場合は無効
                        if (connectionCheck && primitive0.AnyParticle(primitive1))
                            continue;

                        // 両方のプリミティブが完全固定ならば無効
                        if ((primitive0.flagAndTeamId & Flag_AllFix) != 0 && (primitive1.flagAndTeamId & Flag_AllFix) != 0)
                            continue;

                        // 交差判定
                        if (mainKind == KindEdge)
                        {
                            IntersectTest(ref primitive0, ref primitive1);
                        }
                        else
                        {
                            IntersectTest(ref primitive1, ref primitive0);
                        }
                    }
                    else
                        break;
                }
            }

            void IntersectTest(ref Primitive epri, ref Primitive tpri)
            {
                //Debug.Log($"IntersectTest. edge:{epri.particleIndices.xy}, tri:{tpri.particleIndices.xyz}");

                // 線分とトライアングルの交差判定
                var p = epri.nextPos.c0;
                var q = epri.nextPos.c1;
                var qp = p - q;

                float3 a = tpri.nextPos.c0;
                float3 b = tpri.nextPos.c1;
                float3 c = tpri.nextPos.c2;
                var ac = c - a;
                var ab = b - a;
                float3 n = math.cross(ab, ac);

                float d = math.dot(qp, n);

                // 水平は無効
                if (math.abs(d) < Define.System.Epsilon)
                    return;

                // 法線裏側からの侵入に対応
                if (d < 0.0f)
                {
                    p = epri.nextPos.c1;
                    qp = -qp;
                    d = -d;
                }

                var ap = p - a;
                var t = math.dot(ap, n);
                if (t < 0.0f)
                    return;
                if (t > d)
                    return;

                float3 e = math.cross(qp, ap);
                var v = math.dot(ac, e);
                if (v < 0.0f || v > d)
                    return;
                var w = -math.dot(ab, e);
                if (w < 0.0f || (v + w) > d)
                    return;

                // 交差
                // EdgeとTriangleにフラグを立てる
                intersectFlagArray[epri.particleIndices.x] = 1;
                intersectFlagArray[epri.particleIndices.y] = 1;
                intersectFlagArray[tpri.particleIndices.x] = 1;
                intersectFlagArray[tpri.particleIndices.y] = 1;
                intersectFlagArray[tpri.particleIndices.z] = 1;

                //Debug.Log($"Intersect.[{execNumber}] Edge:{epri.particleIndices.xy}, Tri:{tpri.particleIndices.xyz}");
            }
        }
    }
}
