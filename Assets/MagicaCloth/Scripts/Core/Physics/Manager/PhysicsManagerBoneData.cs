// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace MagicaCloth
{
    /// <summary>
    /// ボーンデータ
    /// </summary>
    public class PhysicsManagerBoneData : PhysicsManagerAccess
    {
        //=========================================================================================
        /// <summary>
        /// ボーン制御フラグ
        /// </summary>
        public const byte Flag_Reset = 0x01; // リセット
        public const byte Flag_Restore = 0x10;  // 復元許可フラグ
        public const byte Flag_Write = 0x20; // 書き込み許可フラグ

        /// <summary>
        /// 管理ボーンリスト
        /// </summary>
        public FixedTransformAccessArray boneList;

        /// <summary>
        /// ボーンフラグリスト
        /// </summary>
        public FixedNativeList<byte> boneFlagList;

        /// <summary>
        /// ボーンワールド位置リスト（※未来予測により補正される場合あり）
        /// </summary>
        public FixedNativeList<float3> bonePosList;

        /// <summary>
        /// ボーンワールド回転リスト（※未来予測により補正される場合あり）
        /// </summary>
        public FixedNativeList<quaternion> boneRotList;

        /// <summary>
        /// ボーンワールドスケールリスト（現在は初期化時に設定のみ不変）
        /// </summary>
        public FixedNativeList<float3> boneSclList;

        /// <summary>
        /// 親ボーンへのインデックス(-1=なし)
        /// </summary>
        public FixedNativeList<int> boneParentIndexList;

        /// <summary>
        /// ボーンワールド位置リスト（オリジナル）
        /// </summary>
        public FixedNativeList<float3> basePosList;

        /// <summary>
        /// ボーンワールド回転リスト（オリジナル）
        /// </summary>
        public FixedNativeList<quaternion> baseRotList;

        /// <summary>
        /// ボーンがUnityPhysicsで動作するかの参照カウンタ（１以上で動作）
        /// </summary>
        public FixedNativeList<short> boneUnityPhysicsList;

        /// <summary>
        /// ボーン未来予測位置リスト
        /// </summary>
        public FixedNativeList<float3> futurePosList;

        /// <summary>
        /// ボーン未来予測回転リスト
        /// </summary>
        public FixedNativeList<quaternion> futureRotList;

        //=========================================================================================
        /// <summary>
        /// 復元ボーンリスト
        /// </summary>
        public FixedTransformAccessArray restoreBoneList;

        /// <summary>
        /// 復元ボーンの復元ローカル座標リスト
        /// </summary>
        public FixedNativeList<float3> restoreBoneLocalPosList;

        /// <summary>
        /// 復元ボーンの復元ローカル回転リスト
        /// </summary>
        public FixedNativeList<quaternion> restoreBoneLocalRotList;

        /// <summary>
        /// 復元ボーンの参照ボーンインデックス
        /// </summary>
        public FixedNativeList<int> restoreBoneIndexList;

        //=========================================================================================
        // ここはライトボーンごと
        /// <summary>
        /// 書き込みボーンリスト
        /// </summary>
        public FixedTransformAccessArray writeBoneList;

        /// <summary>
        /// 書き込みボーンの参照ボーン姿勢インデックス（＋１が入るので注意！）
        /// </summary>
        public FixedNativeList<int> writeBoneIndexList;

        /// <summary>
        /// 書き込みボーンの対応するパーティクルインデックス
        /// </summary>
        public ExNativeMultiHashMap<int, int> writeBoneParticleIndexMap;

        /// <summary>
        /// 読み込みボーンに対応する書き込みボーンのインデックス辞書
        /// </summary>
        Dictionary<int, int> boneToWriteIndexDict = new Dictionary<int, int>();

        /// <summary>
        /// 書き込みボーンの確定位置
        /// 親がいる場合はローカル、いない場合はワールド格納
        /// </summary>
        public FixedNativeList<float3> writeBonePosList;

        /// <summary>
        /// 書き込みボーンの確定回転
        /// 親がいる場合はローカル、いない場合はワールド格納
        /// </summary>
        public FixedNativeList<quaternion> writeBoneRotList;

        //=========================================================================================
        /// <summary>
        /// ボーンリストに変化が合った場合にtrue
        /// </summary>
        public bool hasBoneChanged { get; private set; }

        /// <summary>
        /// プロファイラ用
        /// </summary>
        private CustomSampler SamplerReadBoneScale { get; set; }

        //=========================================================================================
        /// <summary>
        /// 初期設定
        /// </summary>
        public override void Create()
        {
            boneList = new FixedTransformAccessArray();
            boneFlagList = new FixedNativeList<byte>();
            bonePosList = new FixedNativeList<float3>();
            boneRotList = new FixedNativeList<quaternion>();
            boneSclList = new FixedNativeList<float3>();
            boneParentIndexList = new FixedNativeList<int>();
            basePosList = new FixedNativeList<float3>();
            baseRotList = new FixedNativeList<quaternion>();
            boneUnityPhysicsList = new FixedNativeList<short>();
            futurePosList = new FixedNativeList<float3>();
            futureRotList = new FixedNativeList<quaternion>();

            restoreBoneList = new FixedTransformAccessArray();
            restoreBoneLocalPosList = new FixedNativeList<float3>();
            restoreBoneLocalRotList = new FixedNativeList<quaternion>();
            restoreBoneIndexList = new FixedNativeList<int>();

            writeBoneList = new FixedTransformAccessArray();
            writeBoneIndexList = new FixedNativeList<int>();
            writeBoneParticleIndexMap = new ExNativeMultiHashMap<int, int>();
            writeBonePosList = new FixedNativeList<float3>();
            writeBoneRotList = new FixedNativeList<quaternion>();

            // プロファイラ用
            SamplerReadBoneScale = CustomSampler.Create("ReadBoneScale");
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public override void Dispose()
        {
            if (boneList == null)
                return;

            boneList.Dispose();
            boneFlagList.Dispose();
            bonePosList.Dispose();
            boneRotList.Dispose();
            boneSclList.Dispose();
            boneParentIndexList.Dispose();
            basePosList.Dispose();
            baseRotList.Dispose();
            boneUnityPhysicsList.Dispose();
            futurePosList.Dispose();
            futureRotList.Dispose();

            restoreBoneList.Dispose();
            restoreBoneLocalPosList.Dispose();
            restoreBoneLocalRotList.Dispose();
            restoreBoneIndexList.Dispose();

            writeBoneList.Dispose();
            writeBoneIndexList.Dispose();
            writeBoneParticleIndexMap.Dispose();
            writeBonePosList.Dispose();
            writeBoneRotList.Dispose();
        }

        //=========================================================================================
        /// <summary>
        /// 復元ボーン登録
        /// </summary>
        /// <param name="target"></param>
        /// <param name="lpos"></param>
        /// <param name="lrot"></param>
        /// <returns></returns>
        public int AddRestoreBone(Transform target, float3 lpos, quaternion lrot, int boneIndex)
        {
            int restoreBoneIndex;
            if (restoreBoneList.Exist(target))
            {
                // 参照カウンタ＋
                restoreBoneIndex = restoreBoneList.Add(target);
            }
            else
            {
                // 復元ローカル姿勢も登録
                restoreBoneIndex = restoreBoneList.Add(target);
                restoreBoneLocalPosList.Add(lpos);
                restoreBoneLocalRotList.Add(lrot);
                restoreBoneIndexList.Add(boneIndex);
                hasBoneChanged = true;
            }

            return restoreBoneIndex;
        }

        /// <summary>
        /// 復元ボーン削除
        /// </summary>
        /// <param name="restoreBoneIndex"></param>
        public void RemoveRestoreBone(int restoreBoneIndex)
        {
            restoreBoneList.Remove(restoreBoneIndex);

            if (restoreBoneList.Exist(restoreBoneIndex) == false)
            {
                // データも削除
                restoreBoneLocalPosList.Remove(restoreBoneIndex);
                restoreBoneLocalRotList.Remove(restoreBoneIndex);
                restoreBoneIndexList.Remove(restoreBoneIndex);
                hasBoneChanged = true;
            }
        }

        /// <summary>
        /// ボーンの復元カウントを返す
        /// </summary>
        public int RestoreBoneCount
        {
            get
            {
                return restoreBoneList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// 利用ボーン登録
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pindex"></param>
        /// <param name="addParent">親ボーンのインデックス保持の有無</param>
        /// <returns></returns>
        public int AddBone(Transform target, int pindex = -1, bool addParent = false)
        {
            int boneIndex;
            if (boneList.Exist(target))
            {
                // 参照カウンタ＋
                boneIndex = boneList.Add(target);
                // 親ボーンは後登録優先の上書き方式にする(v1.10.2)
                if (addParent)
                {
                    boneParentIndexList[boneIndex] = boneList.GetIndex(target.parent);
                }
                boneFlagList.Add(Flag_Reset); // 姿勢リセット
            }
            else
            {
                // 新規
                var pos = float3.zero;
                var rot = quaternion.identity;
                boneIndex = boneList.Add(target);
                boneFlagList.Add(Flag_Reset); // 姿勢リセット
                bonePosList.Add(pos);
                boneRotList.Add(rot);
                boneSclList.Add(float3.zero);
                if (addParent)
                    boneParentIndexList.Add(boneList.GetIndex(target.parent));
                else
                    boneParentIndexList.Add(-1);
                basePosList.Add(pos);
                baseRotList.Add(rot);
                boneUnityPhysicsList.Add(0);
                futurePosList.Add(pos);
                futureRotList.Add(rot);
                hasBoneChanged = true;
            }

            //Debug.Log("AddBone:" + target.name + " index:" + boneIndex + " parent?:" + boneParentIndexList[boneIndex]);

            // 書き込み設定
            if (pindex >= 0)
            {
                if (boneToWriteIndexDict.ContainsKey(boneIndex))
                {
                    Debug.LogWarning($"[{target.name}] is already registered as a write bone.");
                }
                else
                {
                    //Debug.Log("AddWriteBone:" + target.name + " index:" + boneIndex + " parent?:" + boneParentIndexList[boneIndex]);

                    if (writeBoneList.Exist(target))
                    {
                        // 参照カウンタ＋
                        writeBoneList.Add(target);
                    }
                    else
                    {
                        // 新規
                        writeBoneList.Add(target);
                        //Debug.Log("write bone index:" + boneIndex);
                        writeBoneIndexList.Add(boneIndex + 1); // +1を入れるので注意！
                        writeBonePosList.Add(float3.zero);
                        writeBoneRotList.Add(quaternion.identity);
                        hasBoneChanged = true;
                    }
                    int writeIndex = writeBoneList.GetIndex(target);

                    boneToWriteIndexDict.Add(boneIndex, writeIndex);

                    // 書き込み姿勢参照パーティクルインデックス登録
                    writeBoneParticleIndexMap.Add(writeIndex, pindex);
                }
            }

            return boneIndex;
        }

        /// <summary>
        /// 利用ボーン解除
        /// </summary>
        /// <param name="boneIndex"></param>
        /// <param name="pindex"></param>
        /// <returns></returns>
        public bool RemoveBone(int boneIndex, int pindex = -1)
        {
            //Debug.Log("RemoveBone: index:" + boneIndex + " parent?:" + boneParentIndexList[boneIndex]);

            bool del = false;
            boneList.Remove(boneIndex);
            if (boneList.Exist(boneIndex) == false)
            {
                // データも削除
                boneFlagList.Remove(boneIndex);
                bonePosList.Remove(boneIndex);
                boneRotList.Remove(boneIndex);
                boneSclList.Remove(boneIndex);
                boneParentIndexList.Remove(boneIndex);
                basePosList.Remove(boneIndex);
                baseRotList.Remove(boneIndex);
                boneUnityPhysicsList.Remove(boneIndex);
                futurePosList.Remove(boneIndex);
                futureRotList.Remove(boneIndex);
                hasBoneChanged = true;
                del = true;
            }

            // 書き込み設定から削除
            if (pindex >= 0 && boneToWriteIndexDict.ContainsKey(boneIndex))
            {
                int writeIndex = boneToWriteIndexDict[boneIndex];

                writeBoneList.Remove(writeIndex);
                writeBoneIndexList.Remove(writeIndex);
                writeBoneParticleIndexMap.Remove(writeIndex, pindex);
                writeBonePosList.Remove(writeIndex);
                writeBoneRotList.Remove(writeIndex);
                hasBoneChanged = true;

                if (writeBoneList.Exist(writeIndex) == false)
                {
                    boneToWriteIndexDict.Remove(boneIndex);
                    //Debug.Log("RemoveWriteBone: index:" + boneIndex);
                }
            }

            return del;
        }

        /// <summary>
        /// ボーンのUnityPhysics利用カウンタを増減させる
        /// </summary>
        /// <param name="boneIndex"></param>
        /// <param name="sw"></param>
        public void ChangeUnityPhysicsCount(int boneIndex, bool sw)
        {
            //Debug.Log($"Change Bone Physics Count [{boneIndex}]->{sw}");
            boneUnityPhysicsList[boneIndex] += (short)(sw ? 1 : -1);
            Debug.Assert(boneUnityPhysicsList[boneIndex] >= 0);
        }

        /// <summary>
        /// 未来予測をリセットする
        /// </summary>
        /// <param name="boneIndex"></param>
        public void ResetFuturePrediction(int boneIndex)
        {
            //Debug.Log($"ResetFuturePrediction:{boneIndex} F:{Time.frameCount}");
            var flag = boneFlagList[boneIndex];
            flag |= Flag_Reset;
            boneFlagList[boneIndex] = flag;
        }

        /// <summary>
        /// 読み込みボーン数を返す
        /// </summary>
        public int ReadBoneCount
        {
            get
            {
                return boneList.Count;
            }
        }

        /// <summary>
        /// 書き込みボーン数を返す
        /// </summary>
        public int WriteBoneCount
        {
            get
            {
                return writeBoneList.Count;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報のリセット
        /// </summary>
        public void ResetBoneFromTransform(bool fixedUpdate)
        {
            // ボーン姿勢リセット
            if (RestoreBoneCount > 0)
            {
                var job = new RestoreBoneJob()
                {
                    fixedUpdate = fixedUpdate,
                    boneUnityPhysicsList = boneUnityPhysicsList.ToJobArray(),
                    boneFlagList = boneFlagList.ToJobArray(),
                    restoreBoneLocalPosList = restoreBoneLocalPosList.ToJobArray(),
                    restoreBoneLocalRotList = restoreBoneLocalRotList.ToJobArray(),
                    restoreBoneIndexList = restoreBoneIndexList.ToJobArray(),
                };
                Compute.MasterJob = job.Schedule(restoreBoneList.GetTransformAccessArray(), Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢の復元
        /// </summary>
        [BurstCompile]
        struct RestoreBoneJob : IJobParallelForTransform
        {
            public bool fixedUpdate;

            [Unity.Collections.ReadOnly]
            public NativeArray<short> boneUnityPhysicsList;
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> boneFlagList;


            [Unity.Collections.ReadOnly]
            public NativeArray<float3> restoreBoneLocalPosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> restoreBoneLocalRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> restoreBoneIndexList;

            // 復元ボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                var bindex = restoreBoneIndexList[index];
                bool isUnityPhysics = boneUnityPhysicsList[bindex] > 0;
                if (isUnityPhysics == fixedUpdate)
                {
                    var flag = boneFlagList[bindex];
                    if ((flag & Flag_Restore) == 0)
                        return;

                    transform.localPosition = restoreBoneLocalPosList[index];
                    transform.localRotation = restoreBoneLocalRotList[index];
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報の読み込み
        /// </summary>
        public void ReadBoneFromTransform()
        {
            // ボーン姿勢読み込み
            if (ReadBoneCount > 0)
            {
                var updateTime = manager.UpdateTime;

                // 未来予測補間率
                float futureRate = updateTime.IsDelay ? updateTime.FuturePredictionRate : 0.0f;

                // 未来予測が不要ならば従来どおり
                if (futureRate < 0.01f)
                {
                    // ボーンから姿勢読み込み（ルートが別れていないとジョブが並列化できないので注意！）
                    var job = new ReadBoneJob0()
                    {
                        fixedUpdateCount = updateTime.FixedUpdateCount,

                        bonePosList = bonePosList.ToJobArray(),
                        boneRotList = boneRotList.ToJobArray(),
                        boneSclList = boneSclList.ToJobArray(),
                        basePosList = basePosList.ToJobArray(),
                        baseRotList = baseRotList.ToJobArray(),
                        futurePosList = futurePosList.ToJobArray(),
                        futureRotList = futureRotList.ToJobArray(),

                        boneUnityPhysicsList = boneUnityPhysicsList.ToJobArray(),
                        boneFlagList = boneFlagList.ToJobArray(),
                    };
                    Compute.MasterJob = job.Schedule(boneList.GetTransformAccessArray(), Compute.MasterJob);
                }
                else
                {
                    // 未来予測あり
                    // Update更新での未来予測補間率を求める
                    float normalFutureRatio = updateTime.DeltaTime > Define.Compute.Epsilon ?
                        math.clamp((updateTime.AverageDeltaTime / updateTime.DeltaTime) * futureRate, 0.0f, 2.0f) : 0.0f;

                    // FixedUpdate更新での未来予測補間率を求める
                    float fixedFutureRatio = updateTime.FixedUpdateCount > 0 ? (1.0f / updateTime.FixedUpdateCount) * futureRate : 0.0f;
#if true
                    // 次に予想されるフレーム時間を加算することにより実行されるFixedUpdateの回数を予測する
                    float fixedNextTime = Time.time + Time.smoothDeltaTime;
                    float fixedInterval = fixedNextTime - Time.fixedTime;
                    int nextFixedCount = math.max((int)(fixedInterval / Time.fixedDeltaTime), 1);
                    fixedFutureRatio *= nextFixedCount;
#endif

                    //Debug.Log($"normalFutureRatio = {normalFutureRatio}");

                    // ボーンから姿勢読み込み（ルートが別れていないとジョブが並列化できないので注意！）
                    var job = new ReadBoneJob1()
                    {
                        fixedUpdateCount = updateTime.FixedUpdateCount,
                        normalFutureRatio = normalFutureRatio,
                        fixedFutureRatio = fixedFutureRatio,
                        normalDeltaTime = Time.smoothDeltaTime,
                        fixedDeltaTime = Time.fixedDeltaTime,

                        bonePosList = bonePosList.ToJobArray(),
                        boneRotList = boneRotList.ToJobArray(),
                        boneSclList = boneSclList.ToJobArray(),
                        basePosList = basePosList.ToJobArray(),
                        baseRotList = baseRotList.ToJobArray(),
                        boneUnityPhysicsList = boneUnityPhysicsList.ToJobArray(),
                        futurePosList = futurePosList.ToJobArray(),
                        futureRotList = futureRotList.ToJobArray(),
                        boneFlagList = boneFlagList.ToJobArray(),
                    };
                    Compute.MasterJob = job.Schedule(boneList.GetTransformAccessArray(), Compute.MasterJob);
                }
            }
        }

        /// <summary>
        /// ボーン姿勢の読込み（未来予測なし)
        /// </summary>
        [BurstCompile]
        struct ReadBoneJob0 : IJobParallelForTransform
        {
            public int fixedUpdateCount;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> boneSclList;

            //[Unity.Collections.WriteOnly]
            public NativeArray<float3> basePosList;
            //[Unity.Collections.WriteOnly]
            public NativeArray<quaternion> baseRotList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> futurePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> futureRotList;

            [Unity.Collections.ReadOnly]
            public NativeArray<short> boneUnityPhysicsList;
            public NativeArray<byte> boneFlagList;

            // 読み込みボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                // UnityPhysicsモードで今回更新が無い場合は前回のTransfrom姿勢をボーン姿勢に設定して終了する
                bool unityPhysics = boneUnityPhysicsList[index] > 0;
                var flag = boneFlagList[index];
                bool reset = (flag & Flag_Reset) != 0;

                if (unityPhysics == false || fixedUpdateCount > 0 || reset)
                {
                    // 通常更新
                    // UnityPhysics更新かつフレーム更新ありの場合
                    // ボーンリセット時
                    float3 pos = transform.position;
                    quaternion rot = transform.rotation;

                    bonePosList[index] = pos;
                    boneRotList[index] = rot;

                    basePosList[index] = pos;
                    baseRotList[index] = rot;

                    futurePosList[index] = pos;
                    futureRotList[index] = rot;

                    // lossyScale取得(現在はUnity2019.2.14以上のみ)
                    // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                    float4x4 m = transform.localToWorldMatrix;
                    var irot = math.inverse(rot);
                    var m2 = math.mul(new float4x4(irot, float3.zero), m);
                    var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                    boneSclList[index] = scl;
                }
                else
                {
                    // UnityPhysics更新かつフレーム更新なしの場合
                    bonePosList[index] = basePosList[index];
                    boneRotList[index] = baseRotList[index];
                }

                // リセットフラグクリア
                if (reset && (unityPhysics == false || fixedUpdateCount > 0))
                {
                    flag = (byte)(flag & ~Flag_Reset);
                    boneFlagList[index] = flag;
                }
            }
        }

        /// <summary>
        /// ボーン姿勢の読込み（未来予測あり）
        /// </summary>
        [BurstCompile]
        struct ReadBoneJob1 : IJobParallelForTransform
        {
            public int fixedUpdateCount;
            public float normalFutureRatio;
            public float fixedFutureRatio;
            public float normalDeltaTime;
            public float fixedDeltaTime;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> boneSclList;

            public NativeArray<float3> basePosList;
            public NativeArray<quaternion> baseRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> boneUnityPhysicsList;
            public NativeArray<float3> futurePosList;
            public NativeArray<quaternion> futureRotList;
            public NativeArray<byte> boneFlagList;

            // 読み込みボーンごと
            public void Execute(int index, TransformAccess transform)
            {
                bool unityPhysics = boneUnityPhysicsList[index] > 0;
                var flag = boneFlagList[index];
                bool reset = (flag & Flag_Reset) != 0;

                if (unityPhysics == false || fixedUpdateCount > 0 || reset)
                {
                    // 通常更新
                    // UnityPhysics更新かつフレーム更新ありの場合
                    // ボーンリセット時
                    float3 pos = transform.position;
                    quaternion rot = transform.rotation;

                    if (reset)
                    {
                        // リセット
                        //Debug.Log($"reset bone :{index}");
                        basePosList[index] = pos;
                        baseRotList[index] = rot;

                        bonePosList[index] = pos;
                        boneRotList[index] = rot;

                        futurePosList[index] = pos;
                        futureRotList[index] = rot;
                    }
                    else
                    {
                        // 更新：未来予測
                        //Debug.Log($"read bone :{index}");
                        var oldPos = basePosList[index];
                        var oldRot = baseRotList[index];

                        basePosList[index] = pos;
                        baseRotList[index] = rot;

                        // 速度制限(v1.11.1)
                        float moveRatio = 0;
                        float angRatio = 0;
                        float deltaLength = math.distance(oldPos, pos);
                        float deltaAngle = math.degrees(math.abs(MathUtility.Angle(oldRot, rot)));
                        float dtime = unityPhysics ? fixedDeltaTime : normalDeltaTime;
                        if (dtime > Define.Compute.Epsilon)
                        {
                            float moveSpeed = deltaLength / dtime;
                            float angSpeed = deltaAngle / dtime;
                            //if (deltaLength > 1e-06f)
                            //    Debug.Log($"read bone :{index}, movesp:{moveSpeed}, angsp:{angSpeed}");
                            const float maxMoveSpeed = 1.0f;
                            moveRatio = moveSpeed > maxMoveSpeed ? maxMoveSpeed / moveSpeed : 1.0f;
                            const float maxAngleSpeed = 360.0f; // deg
                            angRatio = angSpeed > maxAngleSpeed ? maxAngleSpeed / angSpeed : 1.0f;
                        }

                        // 未来予測
                        float ratio = unityPhysics ? fixedFutureRatio : normalFutureRatio; // ボーンの更新モードにより変化
                        pos = math.lerp(oldPos, pos, 1.0f + ratio * moveRatio);
                        rot = math.slerp(oldRot, rot, 1.0f + ratio * angRatio);
                        rot = math.normalize(rot);

                        bonePosList[index] = pos;
                        boneRotList[index] = rot;

                        // 未来予測姿勢を記録しておく
                        futurePosList[index] = pos;
                        futureRotList[index] = rot;
                    }

                    // lossyScale取得(現在はUnity2019.2.14以上のみ)
                    // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                    float4x4 m = transform.localToWorldMatrix;
                    var irot = math.inverse(rot);
                    var m2 = math.mul(new float4x4(irot, float3.zero), m);
                    var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                    boneSclList[index] = scl;
                }
                else
                {
                    // UnityPhysics更新かつフレーム更新なしの場合
                    // 前回計算した未来予測姿勢を返す
                    bonePosList[index] = futurePosList[index];
                    boneRotList[index] = futureRotList[index];
                }

                // リセットフラグクリア
                if (reset && (unityPhysics == false || fixedUpdateCount > 0))
                {
                    flag = (byte)(flag & ~Flag_Reset);
                    boneFlagList[index] = flag;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// 書き込みボーン姿勢をローカル姿勢に変換する
        /// </summary>
        public void ConvertWorldToLocal()
        {
            if (WriteBoneCount > 0)
            {
                var job = new ConvertWorldToLocalJob()
                {
                    writeBoneIndexList = writeBoneIndexList.ToJobArray(),
                    boneFlagList = boneFlagList.ToJobArray(),
                    bonePosList = bonePosList.ToJobArray(),
                    boneRotList = boneRotList.ToJobArray(),
                    boneSclList = boneSclList.ToJobArray(),
                    boneParentIndexList = boneParentIndexList.ToJobArray(),

                    writeBonePosList = writeBonePosList.ToJobArray(),
                    writeBoneRotList = writeBoneRotList.ToJobArray(),
                };
                Compute.MasterJob = job.Schedule(writeBoneIndexList.Length, 16, Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢をローカル姿勢に変換する
        /// </summary>
        [BurstCompile]
        struct ConvertWorldToLocalJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> writeBoneIndexList;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> boneFlagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> boneSclList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> boneParentIndexList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> writeBonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> writeBoneRotList;

            // 書き込みボーンごと
            public void Execute(int index)
            {
                int bindex = writeBoneIndexList[index];
                if (bindex == 0)
                    return;
                bindex--; // +1が入っているので-1する

                // 書き込みフラグチェック
                var flag = boneFlagList[bindex];
                if ((flag & Flag_Write) == 0)
                    return;

                var pos = bonePosList[bindex];
                var rot = boneRotList[bindex];

                int parentIndex = boneParentIndexList[bindex];
                if (parentIndex >= 0)
                {
                    // 親がいる場合はローカル座標で書き込む
                    var ppos = bonePosList[parentIndex];
                    var prot = boneRotList[parentIndex];
                    var pscl = boneSclList[parentIndex];
                    var iprot = math.inverse(prot);

                    var v = pos - ppos;
                    var lpos = math.mul(iprot, v);
                    lpos /= pscl;
                    var lrot = math.mul(iprot, rot);

                    // マイナススケール対応
                    if (pscl.x < 0 || pscl.y < 0 || pscl.z < 0)
                        lrot = new quaternion(lrot.value * new float4(-math.sign(pscl), 1));

                    writeBonePosList[index] = lpos;
                    writeBoneRotList[index] = lrot;
                }
                else
                {
                    // 親がいない場合はワールド座標で書き込む
                    writeBonePosList[index] = pos;
                    writeBoneRotList[index] = rot;
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        public void WriteBoneToTransform(int bufferIndex)
        {
            if (WriteBoneCount > 0)
            {
                var job = new WriteBontToTransformJob2()
                {
                    fixedUpdateCount = manager.UpdateTime.FixedUpdateCount,

                    boneFlagList = boneFlagList.ToJobArray(bufferIndex),
                    writeBoneIndexList = writeBoneIndexList.ToJobArray(bufferIndex),
                    boneParentIndexList = boneParentIndexList.ToJobArray(),
                    writeBonePosList = writeBonePosList.ToJobArray(bufferIndex),
                    writeBoneRotList = writeBoneRotList.ToJobArray(bufferIndex),
                    boneUnityPhysicsList = boneUnityPhysicsList.ToJobArray(),
                };
                Compute.MasterJob = job.Schedule(writeBoneList.GetTransformAccessArray(), Compute.MasterJob);
            }
        }

        /// <summary>
        /// ボーン姿勢をトランスフォームに書き込む
        /// </summary>
        [BurstCompile]
        struct WriteBontToTransformJob2 : IJobParallelForTransform
        {
            public int fixedUpdateCount;

            [Unity.Collections.ReadOnly]
            public NativeArray<byte> boneFlagList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> writeBoneIndexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> boneParentIndexList;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> writeBonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> writeBoneRotList;
            [Unity.Collections.ReadOnly]
            public NativeArray<short> boneUnityPhysicsList;

            // 書き込みトランスフォームごと
            public void Execute(int index, TransformAccess transform)
            {
                if (index >= writeBoneIndexList.Length)
                    return;

                int bindex = writeBoneIndexList[index];
                if (bindex == 0)
                    return;
                bindex--; // +1が入っているので-1する

                // 書き込みフラグチェック
                var flag = boneFlagList[bindex];
                if ((flag & Flag_Write) == 0)
                    return;

                bool unityPhysics = boneUnityPhysicsList[bindex] > 0;
                if (unityPhysics == false || fixedUpdateCount > 0)
                {
                    var pos = writeBonePosList[index];
                    var rot = writeBoneRotList[index];

                    int parentIndex = boneParentIndexList[bindex];
                    //Debug.Log($"Write Bone:{bindex} Parent:{parentIndex} Pos:{pos}");

                    if (parentIndex >= 0)
                    {
                        // 親を参照する場合はローカル座標で書き込む
                        transform.localPosition = pos;
                        transform.localRotation = rot;
                    }
                    else
                    {
                        // 親がいない場合はワールドで書き込む
                        transform.position = pos;
                        transform.rotation = rot;
                    }
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// ボーン情報を書き込みバッファにコピーする
        /// これは遅延実行時のみ
        /// </summary>
        public void CopyBoneBuffer()
        {
            var job0 = new CopyBoneJob0()
            {
                bonePosList = writeBonePosList.ToJobArray(),
                boneRotList = writeBoneRotList.ToJobArray(),

                backBonePosList = writeBonePosList.ToJobArray(1),
                backBoneRotList = writeBoneRotList.ToJobArray(1),
            };
            var jobHandle0 = job0.Schedule(writeBonePosList.Length, 16);

            var job1 = new CopyBoneJob1()
            {
                writeBoneIndexList = writeBoneIndexList.ToJobArray(),

                backWriteBoneIndexList = writeBoneIndexList.ToJobArray(1),
            };
            var jobHandle1 = job1.Schedule(writeBoneIndexList.Length, 16);

            var job2 = new CopyBoneJob2()
            {
                boneFlagList = boneFlagList.ToJobArray(),
                backBoneFlagList = boneFlagList.ToJobArray(1),
            };
            var jobHandle2 = job2.Schedule(boneFlagList.Length, 16);

            Compute.MasterJob = JobHandle.CombineDependencies(jobHandle0, jobHandle1, jobHandle2);
        }

        [BurstCompile]
        struct CopyBoneJob0 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> bonePosList;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> boneRotList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> backBonePosList;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> backBoneRotList;

            public void Execute(int index)
            {
                backBonePosList[index] = bonePosList[index];
                backBoneRotList[index] = boneRotList[index];
            }
        }

        [BurstCompile]
        struct CopyBoneJob1 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<int> writeBoneIndexList;

            [Unity.Collections.WriteOnly]
            public NativeArray<int> backWriteBoneIndexList;

            public void Execute(int index)
            {
                backWriteBoneIndexList[index] = writeBoneIndexList[index];
            }
        }

        [BurstCompile]
        struct CopyBoneJob2 : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> boneFlagList;

            [Unity.Collections.WriteOnly]
            public NativeArray<byte> backBoneFlagList;

            public void Execute(int index)
            {
                backBoneFlagList[index] = boneFlagList[index];
            }
        }
    }
}
