// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#if MAGICACLOTH2_REDUCTION_DEBUG
using UnityEngine;
#endif

namespace MagicaCloth2
{
    /// <summary>
    /// 結合距離を拡大させながら段階的にリダクションする方式のベースクラス
    /// 高速化のため結合候補のペアのうち頂点が被らないものを１回のステップですべて結合させる。
    /// このため結合距離内にもかかわらず結合されないペアが発生することになる。
    /// この問題は手順を複数可実行することで徐々に収束させて解決する。
    /// </summary>
    public abstract class StepReductionBase : IDisposable
    {
        protected string name = string.Empty;
        protected VirtualMesh vmesh;
        protected ReductionWorkData workData;
        protected ResultCode result;

        protected float startMergeLength;
        protected float endMergeLength;
        protected int maxStep;
        protected bool dontMakeLine;
        protected float joinPositionAdjustment;

        protected int nowStepIndex;
        protected float nowMergeLength;
        protected float nowStepScale;

        //=========================================================================================
        /// <summary>
        /// 結合エッジ情報
        /// </summary>
        public struct JoinEdge : IComparable<JoinEdge>
        {
            public int2 vertexPair;
            public float cost;

            public bool Contains(in int2 pair)
            {
                if (vertexPair.x == pair.x || vertexPair.x == pair.y || vertexPair.y == pair.x || vertexPair.y == pair.y)
                    return true;
                else
                    return false;
            }

            public int CompareTo(JoinEdge other)
            {
                // コストの昇順
                if (cost != other.cost)
                    return cost < other.cost ? -1 : 1;
                else
                    return 0;
            }
        }
        protected NativeList<JoinEdge> joinEdgeList;

        // すでに結合された頂点セット
        private NativeParallelHashSet<int> completeVertexSet;

        // 結合された頂点ペア(x->yへ結合)
        private NativeList<int2> removePairList;

        // ステップごとの削減頂点数
        private NativeArray<int> resultArray;

        //=========================================================================================
        public StepReductionBase() { }

        public StepReductionBase(
            string name,
            VirtualMesh mesh,
            ReductionWorkData workingData,
            float startMergeLength,
            float endMergeLength,
            int maxStep,
            bool dontMakeLine,
            float joinPositionAdjustment
            )
        {
            this.name = name;
            this.vmesh = mesh;
            this.workData = workingData;
            this.result = ResultCode.None;
            this.startMergeLength = math.max(startMergeLength, 1e-09f);
            this.endMergeLength = math.max(endMergeLength, 1e-09f);
            this.maxStep = math.min(maxStep, 100);
            this.dontMakeLine = dontMakeLine;
            this.joinPositionAdjustment = joinPositionAdjustment;
        }

        public virtual void Dispose()
        {
            if (joinEdgeList.IsCreated)
                joinEdgeList.Dispose();
            if (completeVertexSet.IsCreated)
                completeVertexSet.Dispose();
            if (removePairList.IsCreated)
                removePairList.Dispose();
            if (resultArray.IsCreated)
                resultArray.Dispose();
        }

        public ResultCode Result => result;

        //=========================================================================================
        /// <summary>
        /// リダクション実行（スレッド可）
        /// </summary>
        /// <returns></returns>
        public ResultCode Reduction()
        {
            //bool success = false;
            result.Clear();

            try
            {
                // ステップ前初期化
                StepInitialize();

                // 開始範囲から終了範囲までを半径を拡大にしながら実行する
                InitStep();
                while (nowStepIndex < maxStep)
                {
                    // リダクションステップ実行
                    ReductionStep();

                    nowStepIndex++;
                    if (IsEndStep())
                        break;
                    NextStep();
                }
                // 最後に追加で数回実行する（残り物処理）
                if (nowStepIndex < maxStep)
                {
                    // リダクションステップ実行
                    ReductionStep();
                    nowStepIndex++;
                }

                //Debug.Log(nowStepIndex);

                // 頂点情報を整理する
                // 法線単位化
                // ボーンウエイトを１に平均化
                UpdateReductionResultJob();

                // 削除頂点数集計
                int removeVertexCount = 0;
                for (int i = 0; i < nowStepIndex; i++)
                {
                    removeVertexCount += resultArray[i];
                    //Debug.Log($"[Step:{i + 1}] => {resultArray[i]}");
                }
                workData.removeVertexCount += removeVertexCount;

                // 完了
                //success = true;
                result.SetSuccess();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                if (result.IsError() == false)
                {
                    if (this is SimpleDistanceReduction)
                        result.SetError(Define.Result.Reduction_SimpleDistanceException);
                    else if (this is ShapeDistanceReduction)
                        result.SetError(Define.Result.Reduction_ShapeDistanceException);
                    else
                        result.SetError(Define.Result.Reduction_Exception);
                }
            }
            finally
            {
                // 作業バッファを解放する（重要）
                // ★仮にタスクが例外やキャンセルされたとしてもこれで作成したバッファは正しくDispose()される
                //MagicaManager.Discard.Add();

                // 登録したジョブを解除する（重要）
                // ★仮にタスクが例外やキャンセルされたとしてもこれで発行したJobは正しくComplete()される
                //MagicaManager.Thread.DisposeJob(int);
            }

            return result;
        }

        void InitStep()
        {
            nowStepIndex = 0;
            nowMergeLength = startMergeLength;
            nowStepScale = 2.0f;
        }

        bool IsEndStep()
        {
            return nowMergeLength == endMergeLength;
        }

        void NextStep()
        {
            nowStepScale = math.max(nowStepScale * 0.93f, 1.1f);
            nowMergeLength = math.min(nowMergeLength * nowStepScale, endMergeLength);
        }

        /// <summary>
        /// リダクションステップ処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <param name="mergeLength"></param>
        /// <param name="vertexCount"></param>
        /// <param name="stepIndex"></param>
        /// <returns></returns>
        void ReductionStep()
        {
            //Debug.Log($"nowMergeLength:{nowMergeLength}");

            // ステップ前処理
            // 結合ペアリストのクリア
            PreReductionStep();

            // 結合ペアの追加
            CustomReductionStep();

            // ステップ後処理
            // 結合ペアリストから頂点が被らないペアを摘出し結合を実行する
            // 結合後に頂点の接続状態を最新に更新する
            PostReductionStep();
        }

        //=========================================================================================
        /// <summary>
        /// ステップ処理前初期化
        /// この関数をオーバーライドし必要なステップ前初期化を追加する
        /// </summary>
        protected virtual void StepInitialize()
        {
            int vcnt = vmesh.VertexCount;
            joinEdgeList = new NativeList<JoinEdge>(vcnt / 4, Allocator.Persistent);
            completeVertexSet = new NativeParallelHashSet<int>(vcnt, Allocator.Persistent);
            removePairList = new NativeList<int2>(vcnt, Allocator.Persistent);
            resultArray = new NativeArray<int>(maxStep, Allocator.Persistent);
        }

        /// <summary>
        /// この関数をオーバーライドしjoinEdgeListに削除候補のペアを追加する処理を記述する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        protected virtual void CustomReductionStep()
        {
        }

        //=========================================================================================
        /// <summary>
        /// リダクションステップ前処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void PreReductionStep()
        {
            // 作業用バッファクリア
            joinEdgeList.Clear();
        }

        //=========================================================================================
        /// <summary>
        /// リダクションステップ後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void PostReductionStep()
        {
            // 結合候補をソートする
            SortJoinEdge();

            // 結合リストを作成する
            DetermineJoinEdge();

            // 結合を実行する
            RunJoinEdge();

            // 頂点の接続状態を最新に更新する。すべて最新の生存ポイントを指すように変更する
            UpdateJoinAndLink();

            //Debug.Log($"[{name}] Step:{nowStepIndex} mergeLength:{nowMergeLength} RemoveVertex:{resultArray[nowStepIndex]}");
        }

        //=========================================================================================
        /// <summary>
        /// 結合候補のペアリストをコストの昇順でソートするジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void SortJoinEdge()
        {
            // コストの昇順でソートする
            joinEdgeList.Sort();
        }

        //=========================================================================================
        /// <summary>
        /// 結合候補から距離順位に頂点が被らないように結合ペアを選択するジョブを発行する
        /// 頂点が被らないのでこれらのペアは並列に結合処理を行っても問題なくなる
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void DetermineJoinEdge()
        {
            var reductionJob = new DeterminJoinEdgeJob()
            {
                stepIndex = nowStepIndex,
                mergeLength = nowMergeLength,
                joinEdgeList = joinEdgeList,
                completeVertexSet = completeVertexSet,
                removePairList = removePairList,
                resultArray = resultArray,
            };
            reductionJob.Run();
            //Debug.Log($"Step:{nowStepIndex} mergeLength:{nowMergeLength} RemoveVertex:{resultArray[nowStepIndex]}");
        }

        [BurstCompile]
        struct DeterminJoinEdgeJob : IJob
        {
            public int stepIndex;
            public float mergeLength;

            [Unity.Collections.ReadOnly]
            public NativeList<JoinEdge> joinEdgeList;

            public NativeParallelHashSet<int> completeVertexSet;
            public NativeList<int2> removePairList;
            public NativeArray<int> resultArray;

            public void Execute()
            {
                // すでに結合された頂点セット
                completeVertexSet.Clear();
                removePairList.Clear();

                int cnt = 0;
                for (int i = 0; i < joinEdgeList.Length; i++)
                {
                    var joinEdge = joinEdgeList[i];
                    var vertexPair = joinEdge.vertexPair;

                    // 頂点がすでに結合されているならばスキップする
                    if (completeVertexSet.Contains(vertexPair.x) || completeVertexSet.Contains(vertexPair.y))
                        continue;

                    // このエッジを結合する
                    // 結合リストに追加する
                    removePairList.Add(vertexPair.xy);

                    // 処理済みマップに追加
                    completeVertexSet.Add(vertexPair.x);
                    completeVertexSet.Add(vertexPair.y);

                    cnt++;
                }

                // 削減頂点数を保存
                resultArray[stepIndex] = cnt;

            }
        }

        //=========================================================================================
        /// <summary>
        /// 結合ペアを実際に結合するジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void RunJoinEdge()
        {
            var organizeJoinEdgeJob = new JoinPairJob()
            {
                joinPositionAdjustment = joinPositionAdjustment,
                removePairList = removePairList,
                localPositions = vmesh.localPositions.GetNativeArray(),
                localNormals = vmesh.localNormals.GetNativeArray(),
                joinIndices = workData.vertexJoinIndices,
                vertexToVertexMap = workData.vertexToVertexMap,
                boneWeights = vmesh.boneWeights.GetNativeArray(),
                attributes = vmesh.attributes.GetNativeArray(),
            };
            organizeJoinEdgeJob.Run();
        }

        [BurstCompile]
        struct JoinPairJob : IJob
        {
            public float joinPositionAdjustment;

            [Unity.Collections.ReadOnly]
            public NativeList<int2> removePairList;

            public NativeArray<float3> localPositions;
            public NativeArray<float3> localNormals;
            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            public NativeArray<VertexAttribute> attributes;

            public NativeArray<int> joinIndices;

            public void Execute()
            {
                for (int index = 0; index < removePairList.Length; index++)
                {
                    int2 pair = removePairList[index];

                    // pair.x -> pair.yに結合する
                    int vindex1 = pair.x;
                    int vindex2 = pair.y;
                    float3 pos1 = localPositions[vindex1];
                    float3 pos2 = localPositions[vindex2];
                    float3 nor1 = localNormals[vindex1];
                    float3 nor2 = localNormals[vindex2];

                    // vertex2にvertex1を結合し、vertex1は削除としてマークする
                    // 結合(vertex1 -> vertex2)
                    joinIndices[vindex1] = vindex2;

                    // vertex2の新しい座標
                    // 各頂点の接続数に応じて結合位置のウエイトを変える（接続数が多いほど動かない）
                    float linkCnt1 = math.max(vertexToVertexMap.CountValuesForKey((ushort)vindex1) - 1, 1); // 接続トライアングルを想定して１を引く
                    float linkCnt2 = math.max(vertexToVertexMap.CountValuesForKey((ushort)vindex2) - 1, 1); // 接続トライアングルを想定して１を引く
                    float ratio = linkCnt2 / (linkCnt1 + linkCnt2);

                    // 頂点接合位置のユーザー調整(1.0の場合は平均位置になる）
                    ratio = math.lerp(ratio, 0.5f, joinPositionAdjustment);

                    // p2の座標を変更
                    float3 newpos = math.lerp(pos2, pos1, ratio);
                    localPositions[vindex2] = newpos;
                    localNormals[vindex2] = (nor1 + nor2);

                    // 接続数を結合する（重複は弾かれる）
                    var newLink = new FixedList512Bytes<ushort>();
                    foreach (ushort nindex in vertexToVertexMap.GetValuesForKey((ushort)vindex1))
                    {
                        if (nindex != vindex1 && nindex != vindex2)
                            newLink.Set(nindex);
                    }
                    foreach (ushort nindex in vertexToVertexMap.GetValuesForKey((ushort)vindex2))
                    {
                        if (nindex != vindex1 && nindex != vindex2)
                            newLink.Set(nindex);
                    }
                    vertexToVertexMap.Remove((ushort)vindex2);
                    for (int i = 0; i < newLink.Length; i++)
                    {
                        vertexToVertexMap.Add((ushort)vindex2, newLink[i]);
                    }
                    //Debug.Assert(newLink.Length > 0);

                    // p2にBoneWeightを結合
                    var bw = boneWeights[vindex2];
                    bw.AddWeight(boneWeights[vindex1]);
                    boneWeights[vindex2] = bw;

                    // 頂点属性
                    var attr1 = attributes[vindex1];
                    var attr2 = attributes[vindex2];
                    attributes[vindex2] = VertexAttribute.JoinAttribute(attr1, attr2);
                    attributes[vindex1] = VertexAttribute.Invalid; // 削除頂点は無効にする
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 接続状態を最新に更新するジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        void UpdateJoinAndLink()
        {
            // JoinIndexの状態を更新する。現在の最新の生存ポイントを指すように変更する
            var updateJoinIndexJob = new UpdateJoinIndexJob()
            {
                joinIndices = workData.vertexJoinIndices,
            };
            updateJoinIndexJob.Run(vmesh.VertexCount);

            // 頂点の接続頂点リストを最新に更新する。すべて最新の生存ポイントを指すように変更する
            var updateLinkIndexJob = new UpdateLinkIndexJob()
            {
                joinIndices = workData.vertexJoinIndices,
                vertexToVertexMap = workData.vertexToVertexMap,
            };
            updateLinkIndexJob.Run(vmesh.VertexCount);
        }

        [BurstCompile]
        struct UpdateJoinIndexJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> joinIndices;

            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];
                if (join >= 0)
                {
                    // 削除されている
                    // 最終的な生存ポイントに連結させる
                    while (joinIndices[join] >= 0)
                    {
                        join = joinIndices[join];
                    }
                    joinIndices[vindex] = join;
                }
            }
        }

        [BurstCompile]
        struct UpdateLinkIndexJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> joinIndices;

            public NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap;

            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];

                // 自身が削除されている場合は無視
                if (join >= 0)
                    return;

                // 自身が生存している
                // 現在の接続インデックスから削除されたものを生存インデックスに入れ替える
                var newLinkSet = new FixedList512Bytes<ushort>();
                foreach (ushort i in vertexToVertexMap.GetValuesForKey((ushort)vindex))
                {
                    int tvindex = i;
                    int tjoin = joinIndices[tvindex];
                    if (tjoin >= 0)
                    {
                        // 削除されている
                        tvindex = tjoin;
                        Debug.Assert(joinIndices[tvindex] < 0);
                    }

                    // 自身は弾く
                    if (tvindex == vindex)
                        continue;

                    newLinkSet.Set((ushort)tvindex);
                }
                // 生存のみの新しいセットに入れ替え
                vertexToVertexMap.Remove((ushort)vindex);
                for (int i = 0; i < newLinkSet.Length; i++)
                {
                    vertexToVertexMap.Add((ushort)vindex, newLinkSet[i]);
                }
                //Debug.Assert(newLinkSet.Length > 0);
            }
        }

        //=========================================================================================
        /// <summary>
        /// リダクション後のデータを整える
        /// </summary>
        void UpdateReductionResultJob()
        {
            // 頂点法線の単位化、およびボーンウエイトを１に整える
            var finalVertexJob = new FinalMergeVertexJob()
            {
                joinIndices = workData.vertexJoinIndices,
                localNormals = vmesh.localNormals.GetNativeArray(),
                boneWeights = vmesh.boneWeights.GetNativeArray(),
            };
            finalVertexJob.Run(vmesh.VertexCount);
        }

        [BurstCompile]
        struct FinalMergeVertexJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> joinIndices;

            public NativeArray<float3> localNormals;
            public NativeArray<VirtualMeshBoneWeight> boneWeights;

            public void Execute(int vindex)
            {
                int join = joinIndices[vindex];
                if (join >= 0)
                {
                    // 削除されている
                    return;
                }

                // 法線単位化
                localNormals[vindex] = math.normalize(localNormals[vindex]);

                // ボーンウエイトを平均化
                var bw = boneWeights[vindex];
                bw.AdjustWeight();
                boneWeights[vindex] = bw;
            }
        }

        //=========================================================================================
        // Job Utility
        //=========================================================================================
        /// <summary>
        /// 頂点を結合して問題がないか調べる
        /// </summary>
        /// <param name="vertexToVertexArray"></param>
        /// <param name="vindex"></param>
        /// <param name="tvindex"></param>
        /// <param name="vlist"></param>
        /// <param name="tvlist"></param>
        /// <param name="dontMakeLine"></param>
        /// <returns></returns>
        protected static bool CheckJoin2(
            in NativeParallelMultiHashMap<ushort, ushort> vertexToVertexMap,
            int vindex,
            int tvindex,
            bool dontMakeLine
            )
        {
            // 結合後の接続リストを仮作成する
            var joinVlink = new FixedList512Bytes<ushort>();

            foreach (ushort index in vertexToVertexMap.GetValuesForKey((ushort)vindex))
            {
                if (index != vindex && index != tvindex)
                    joinVlink.Set(index);
            }
            foreach (ushort index in vertexToVertexMap.GetValuesForKey((ushort)tvindex))
            {
                if (index != vindex && index != tvindex)
                    joinVlink.Set(index);
            }

            // 点になるのはNG
            // 結合後に接続が０になる場合はNG
            if (joinVlink.Length == 0)
            {
                //Debug.LogWarning("joinVlink.Count = 0!");
                return false;
            }

            // 可能な限りラインを作らない（オプション）
            if (dontMakeLine)
            {
                // 結合後のトライアングルの外周をひと筆書きし、すべての外周頂点が使われているならOK!
                // 外周頂点が一筆書きできない場合は２つ以上のグループに分かれいる（X型になる）
                var stack = new FixedList512Bytes<ushort>();
                stack.Push(joinVlink[0]);
                while (stack.Length > 0)
                {
                    ushort index = stack.Pop();
                    if (joinVlink.Contains(index) == false)
                        continue;
                    joinVlink.RemoveItemAtSwapBack(index);

                    foreach (ushort nindex in vertexToVertexMap.GetValuesForKey((ushort)index))
                    {
                        if (joinVlink.Contains(nindex))
                        {
                            // next
                            stack.Push(nindex);
                        }
                    }
                }
                if (joinVlink.Length > 0)
                {
                    // 外周を一筆書きしたあとでもまだ頂点が残っている！
                    // これは頂点がX型になるのでNG！
                    return false;
                }
            }

            // 大丈夫
            return true;
        }

        /// <summary>
        /// 頂点を結合して問題がないか調べる
        /// </summary>
        /// <param name="vertexToVertexArray"></param>
        /// <param name="vindex"></param>
        /// <param name="tvindex"></param>
        /// <param name="vlist"></param>
        /// <param name="tvlist"></param>
        /// <param name="dontMakeLine"></param>
        /// <returns></returns>
        protected static bool CheckJoin(
            in NativeArray<FixedList128Bytes<ushort>> vertexToVertexArray,
            int vindex,
            int tvindex,
            in FixedList128Bytes<ushort> vlist,
            in FixedList128Bytes<ushort> tvlist,
            bool dontMakeLine
            )
        {
            // 結合後の接続リストを仮作成する
            var joinVlink = new FixedList128Bytes<ushort>();
            for (int i = 0; i < vlist.Length; i++)
            {
                int index = vlist[i];
                if (index != vindex && index != tvindex)
                    joinVlink.SetLimit((ushort)index);
            }
            for (int i = 0; i < tvlist.Length; i++)
            {
                int index = tvlist[i];
                if (index != vindex && index != tvindex)
                    joinVlink.SetLimit((ushort)index);
            }

            // 点になるのはNG
            // 結合後に接続が０になる場合はNG
            if (joinVlink.Length == 0)
            {
                //Debug.LogWarning("joinVlink.Count = 0!");
                return false;
            }

            // 可能な限りラインを作らない（オプション）
            if (dontMakeLine)
            {
                // 結合後のトライアングルの外周をひと筆書きし、すべての外周頂点が使われているならOK!
                // 外周頂点が一筆書きできない場合は２つ以上のグループに分かれいる（X型になる）
                var stack = new FixedList512Bytes<ushort>();
                stack.Push(joinVlink[0]);
                while (stack.Length > 0)
                {
                    ushort index = stack.Pop();
                    if (joinVlink.Contains(index) == false)
                        continue;
                    joinVlink.RemoveItemAtSwapBack(index);

                    var link = vertexToVertexArray[index];
                    for (int i = 0; i < link.Length; i++)
                    {
                        ushort nindex = link[i];
                        if (joinVlink.Contains(nindex))
                        {
                            // next
                            stack.Push(nindex);
                        }
                    }
                }
                if (joinVlink.Length > 0)
                {
                    // 外周を一筆書きしたあとでもまだ頂点が残っている！
                    // これは頂点がX型になるのでNG！
                    return false;
                }
            }

            // 大丈夫
            return true;
        }
    }
}
