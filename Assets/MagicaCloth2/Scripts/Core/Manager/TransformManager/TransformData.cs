// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    /// <summary>
    /// TransformAccessArrayを中心とした一連のTransform管理クラス
    /// スレッドで利用できるように様々な工夫を行っている
    /// </summary>
    public class TransformData : IDisposable
    {
        internal List<Transform> transformList;

        /// <summary>
        /// フラグ（フラグはTransformManagerクラスで定義）
        /// </summary>
        internal ExSimpleNativeArray<ExBitFlag8> flagArray;

        /// <summary>
        /// 初期localPosition
        /// </summary>
        internal ExSimpleNativeArray<float3> initLocalPositionArray;

        /// <summary>
        /// 初期localRotation
        /// </summary>
        internal ExSimpleNativeArray<quaternion> initLocalRotationArray;

        /// <summary>
        /// ワールド座標
        /// </summary>
        internal ExSimpleNativeArray<float3> positionArray;

        /// <summary>
        /// ワールド回転
        /// </summary>
        internal ExSimpleNativeArray<quaternion> rotationArray;

        /// <summary>
        /// ワールド逆回転
        /// </summary>
        internal ExSimpleNativeArray<quaternion> inverseRotationArray;

        /// <summary>
        /// ワールドスケール
        /// Transform.lossyScaleと等価
        /// </summary>
        internal ExSimpleNativeArray<float3> scaleArray;

        /// <summary>
        /// ローカル座標
        /// </summary>
        internal ExSimpleNativeArray<float3> localPositionArray;

        /// <summary>
        /// ローカル回転
        /// </summary>
        internal ExSimpleNativeArray<quaternion> localRotationArray;

        /// <summary>
        /// トランスフォームのインスタンスID
        /// </summary>
        internal ExSimpleNativeArray<int> idArray;

        /// <summary>
        /// 親トランスフォームのインスタンスID(0=なし)
        /// </summary>
        internal ExSimpleNativeArray<int> parentIdArray;

        /// <summary>
        /// BoneClothのルートトランスフォームIDリスト
        /// </summary>
        internal List<int> rootIdList;

        /// <summary>
        /// Transformリストに変更があったかどうか
        /// </summary>
        bool isDirty = false;

        //=========================================================================================
        /// <summary>
        /// Job作業用トランスフォームアクセス配列
        /// データ構築時には利用しない
        /// </summary>
        internal TransformAccessArray transformAccessArray;


        //=========================================================================================
        /// <summary>
        /// 利用可能な空インデックス
        /// </summary>
        Queue<int> emptyStack;

        //=========================================================================================
        public TransformData()
        {
            Init(100);
        }

        public TransformData(int capacity)
        {
            // 領域のみ確保する
            Init(capacity);
        }

        public void Init(int capacity)
        {
            // 領域のみ確保する
            transformList = new List<Transform>(capacity);
            idArray = new ExSimpleNativeArray<int>(capacity, true);
            parentIdArray = new ExSimpleNativeArray<int>(capacity, true);
            flagArray = new ExSimpleNativeArray<ExBitFlag8>(capacity, true);
            initLocalPositionArray = new ExSimpleNativeArray<float3>(capacity, true);
            initLocalRotationArray = new ExSimpleNativeArray<quaternion>(capacity, true);
            positionArray = new ExSimpleNativeArray<float3>(capacity, true);
            rotationArray = new ExSimpleNativeArray<quaternion>(capacity, true);
            scaleArray = new ExSimpleNativeArray<float3>(capacity, true);
            localPositionArray = new ExSimpleNativeArray<float3>(capacity, true);
            localRotationArray = new ExSimpleNativeArray<quaternion>(capacity, true);
            inverseRotationArray = new ExSimpleNativeArray<quaternion>(capacity, true);
            emptyStack = new Queue<int>(capacity);
            isDirty = true;
        }

        public void Dispose()
        {
            transformList.Clear();
            idArray?.Dispose();
            parentIdArray?.Dispose();
            flagArray?.Dispose();
            initLocalPositionArray?.Dispose();
            initLocalRotationArray?.Dispose();
            positionArray?.Dispose();
            rotationArray?.Dispose();
            scaleArray?.Dispose();
            localPositionArray?.Dispose();
            localRotationArray?.Dispose();
            inverseRotationArray?.Dispose();
            emptyStack.Clear();

            // transformAccessArrayはメインスレッドのみ
            if (transformAccessArray.isCreated)
            {
                //if (MagicaManager.Discard != null)
                //    MagicaManager.Discard.AddMain(transformAccessArray);
                //else
                //    transformAccessArray.Dispose();
                transformAccessArray.Dispose();
            }
        }

        public int Count => transformList.Count;
        public int RootCount => rootIdList?.Count ?? 0;
        public bool IsDirty => isDirty;

        //=========================================================================================
        /// <summary>
        /// Transform単体を追加する(tidを指定するならスレッド可）
        /// すでに登録済みの同じトランスフォームがある場合はそのインデックスを返す
        /// </summary>
        /// <param name="t"></param>
        /// <param name="tid">0の場合はTransformからGetInstanceId()を即時設定する</param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public int AddTransform(Transform t, int tid = 0, int pid = 0, byte flag = TransformManager.Flag_Read, bool checkDuplicate = true)
        {
            int index;

            // 重複チェック
            if (checkDuplicate)
            {
                index = ReferenceIndexOf(transformList, t);
                if (index >= 0)
                    return index; // 発見
            }

            // 新規追加
            if (emptyStack.Count > 0)
            {
                index = emptyStack.Dequeue();
                transformList[index] = t;
                if (tid == 0)
                {
                    // Transformからデータを取得（メインスレッドのみ）
                    idArray[index] = t.GetInstanceID();
                    parentIdArray[index] = t.parent?.GetInstanceID() ?? 0;
                    initLocalPositionArray[index] = t.localPosition;
                    initLocalRotationArray[index] = t.localRotation;
                    positionArray[index] = t.position;
                    rotationArray[index] = t.rotation;
                    scaleArray[index] = t.lossyScale;
                    localPositionArray[index] = t.localPosition;
                    localRotationArray[index] = t.localRotation;
                    inverseRotationArray[index] = Quaternion.Inverse(t.rotation);
                    flagArray[index] = new ExBitFlag8(flag);
                }
                else
                {
                    // 最低限の情報上書き
                    idArray[index] = tid;
                    parentIdArray[index] = pid;
                    initLocalPositionArray[index] = 0;
                    initLocalRotationArray[index] = quaternion.identity;
                    positionArray[index] = 0;
                    rotationArray[index] = quaternion.identity;
                    scaleArray[index] = 1;
                    localPositionArray[index] = 0;
                    localRotationArray[index] = quaternion.identity;
                    inverseRotationArray[index] = quaternion.identity;
                    flagArray[index] = new ExBitFlag8(flag);
                }
            }
            else
            {
                index = Count;
                transformList.Add(t);
                if (tid == 0)
                {
                    // Transformからデータを取得（メインスレッドのみ）
                    idArray.Add(t.GetInstanceID());
                    parentIdArray.Add(t.parent?.GetInstanceID() ?? 0);
                    initLocalPositionArray.Add(t.localPosition);
                    initLocalRotationArray.Add(t.localRotation);
                    positionArray.Add(t.position);
                    rotationArray.Add(t.rotation);
                    scaleArray.Add(t.lossyScale);
                    localPositionArray.Add(t.localPosition);
                    localRotationArray.Add(t.localRotation);
                    inverseRotationArray.Add(Quaternion.Inverse(t.rotation));
                    flagArray.Add(new ExBitFlag8(flag));
                }
                else
                {
                    // 領域のみ確保
                    idArray.Add(tid);
                    parentIdArray.Add(pid);
                    initLocalPositionArray.Add(0);
                    initLocalRotationArray.Add(quaternion.identity);
                    positionArray.Add(0);
                    rotationArray.Add(quaternion.identity);
                    scaleArray.Add(1);
                    localPositionArray.Add(0);
                    localRotationArray.Add(quaternion.identity);
                    inverseRotationArray.Add(quaternion.identity);
                    flagArray.Add(new ExBitFlag8(flag));
                }
            }

            isDirty = true;

            return index;
        }

        /// <summary>
        /// レコード情報からTransformを登録する（スレッド可）
        /// すでに登録済みの同じトランスフォームがある場合はそのインデックスを返す
        /// </summary>
        /// <param name="record">トランスフォーム記録クラス</param>
        /// <param name="pid">親のインスタンスID</param>
        /// <param name="flag"></param>
        /// <param name="checkDuplicate">重複チェックの有無</param>
        /// <returns></returns>
        public int AddTransform(TransformRecord record, int pid = 0, byte flag = TransformManager.Flag_Read, bool checkDuplicate = true)
        {
            int index;

            // 重複チェック
            if (checkDuplicate)
            {
                index = ReferenceIndexOf(transformList, record.transform);
                if (index >= 0)
                    return index; // 発見
            }

            // 新規追加
            if (emptyStack.Count > 0)
            {
                index = emptyStack.Dequeue();
                transformList[index] = record.transform;
                idArray[index] = record.id;
                parentIdArray[index] = pid;
                initLocalPositionArray[index] = record.localPosition;
                initLocalRotationArray[index] = record.localRotation;
                positionArray[index] = record.position;
                rotationArray[index] = record.rotation;
                scaleArray[index] = record.scale;
                localPositionArray[index] = record.localPosition;
                localRotationArray[index] = record.localRotation;
                inverseRotationArray[index] = Quaternion.Inverse(record.rotation);
                flagArray[index] = new ExBitFlag8(flag);
            }
            else
            {
                index = Count;
                transformList.Add(record.transform);
                idArray.Add(record.id);
                parentIdArray.Add(pid);
                initLocalPositionArray.Add(record.localPosition);
                initLocalRotationArray.Add(record.localRotation);
                positionArray.Add(record.position);
                rotationArray.Add(record.rotation);
                scaleArray.Add(record.scale);
                localPositionArray.Add(record.localPosition);
                localRotationArray.Add(record.localRotation);
                inverseRotationArray.Add(Quaternion.Inverse(record.rotation));
                flagArray.Add(new ExBitFlag8(flag));
            }

            isDirty = true;
            return index;
        }

        /// <summary>
        /// 他のTransformDataから追加する
        /// </summary>
        /// <param name="srcData"></param>
        /// <param name="srcIndex"></param>
        /// <param name="checkDuplicate">重複チェックの有無</param>
        /// <returns></returns>
        public int AddTransform(TransformData srcData, int srcIndex, bool checkDuplicate = true)
        {
            int index;

            // 重複チェック
            Transform t = srcData.transformList[srcIndex];
            if (checkDuplicate)
            {
                index = ReferenceIndexOf(transformList, t);
                if (index >= 0)
                    return index; // 発見
            }

            // 新規追加
            int id = srcData.idArray[srcIndex];
            int pid = srcData.parentIdArray[srcIndex];
            var initPos = srcData.initLocalPositionArray[srcIndex];
            var initRot = srcData.initLocalRotationArray[srcIndex];
            var pos = srcData.positionArray[srcIndex];
            var rot = srcData.rotationArray[srcIndex];
            var scl = srcData.scaleArray[srcIndex];
            var lpos = srcData.localPositionArray[srcIndex];
            var lrot = srcData.localRotationArray[srcIndex];
            var irot = srcData.inverseRotationArray[srcIndex];
            var flag = srcData.flagArray[srcIndex];
            if (emptyStack.Count > 0)
            {
                index = emptyStack.Dequeue();
                transformList[index] = t;
                idArray[index] = id;
                parentIdArray[index] = pid;
                initLocalPositionArray[index] = initPos;
                initLocalRotationArray[index] = initRot;
                positionArray[index] = pos;
                rotationArray[index] = rot;
                scaleArray[index] = scl;
                localPositionArray[index] = lpos;
                localRotationArray[index] = lrot;
                inverseRotationArray[index] = irot;
                flagArray[index] = flag;
            }
            else
            {
                index = Count;
                transformList.Add(t);
                idArray.Add(id);
                parentIdArray.Add(pid);
                initLocalPositionArray.Add(initPos);
                initLocalRotationArray.Add(initRot);
                positionArray.Add(pos);
                rotationArray.Add(rot);
                scaleArray.Add(scl);
                localPositionArray.Add(lpos);
                localRotationArray.Add(lrot);
                inverseRotationArray.Add(irot);
                flagArray.Add(flag);
            }

            isDirty = true;
            return index;
        }

        /// <summary>
        /// トランスフォーム配列を追加し追加されたインデックスを返す（スレッド可）
        /// transform.GetInstanceID()のリストが必要
        /// </summary>
        /// <param name="tlist"></param>
        /// <returns></returns>
        public int[] AddTransformRange(List<Transform> tlist, List<int> idList, List<int> pidList, int copyCount = 0)
        {
            //int tcnt = tlist.Count;
            int tcnt = copyCount > 0 ? copyCount : tlist.Count;
            Debug.Assert(tcnt > 0 && tcnt < tlist.Count);

            int startIndex = Count;
            int[] indices = new int[tcnt];

            for (int i = 0; i < tcnt; i++)
            {
                transformList.Add(tlist[i]);
                idArray.Add(idList[i]);
                parentIdArray.Add(pidList[i]);
                indices[i] = startIndex + i;
            }

            // データは領域のみ追加
            flagArray.AddRange(tcnt, new ExBitFlag8(TransformManager.Flag_Read)); // フラグは読み込みとして初期化
            initLocalPositionArray.AddRange(tcnt);
            initLocalRotationArray.AddRange(tcnt);
            positionArray.AddRange(tcnt);
            rotationArray.AddRange(tcnt);
            scaleArray.AddRange(tcnt);
            localPositionArray.AddRange(tcnt);
            localRotationArray.AddRange(tcnt);
            inverseRotationArray.AddRange(tcnt);

            // 変更フラグ
            isDirty = true;

            return indices;
        }

        /// <summary>
        /// トランスフォームデータから指定したカウントのトランスフォームをコピーする（スレッド可）
        /// </summary>
        /// <param name="stdata"></param>
        /// <param name="copyCount"></param>
        /// <returns></returns>
        public int[] AddTransformRange(TransformData stdata, int copyCount = 0)
        {
            Debug.Assert(stdata != null);
            return AddTransformRange(
                stdata.transformList,
                new List<int>(stdata.idArray.ToArray()),
                new List<int>(stdata.parentIdArray.ToArray()),
                copyCount
                );
        }

        /// <summary>
        /// トランスフォーム配列と一部データを追加しインデックスを返す（スレッド可）
        /// 残りのデータは即時計算される
        /// ※ImportWorkからの作成用
        /// </summary>
        /// <param name="tlist"></param>
        /// <param name="idList"></param>
        /// <param name="positions"></param>
        /// <param name="rotations"></param>
        /// <param name="localToWorlds"></param>
        /// <returns></returns>
        public int[] AddTransformRange(
            List<Transform> tlist,
            List<int> idList,
            List<int> pidList,
            List<int> rootIds,
            NativeArray<float3> localPositions,
            NativeArray<quaternion> localRotations,
            NativeArray<float3> positions,
            NativeArray<quaternion> rotations,
            NativeArray<float3> scales,
            NativeArray<quaternion> inverseRotations
            )
        {
            int tcnt = tlist.Count;
            Debug.Assert(tcnt > 0);

            int startIndex = Count;
            int[] indices = new int[tcnt];

            transformList.AddRange(tlist);
            for (int i = 0; i < tcnt; i++)
            {
                idArray.Add(idList[i]);
                parentIdArray.Add(pidList[i]);
                indices[i] = startIndex + i;
            }

            // root id
            if (rootIds != null && rootIds.Count > 0)
            {
                if (rootIdList == null)
                    rootIdList = new List<int>(rootIds);
                else
                    rootIdList.AddRange(rootIds);
            }

            // データコピー
            flagArray.AddRange(tcnt, new ExBitFlag8(TransformManager.Flag_Read)); // フラグは読み込みとして初期化
            initLocalPositionArray.AddRange(localPositions);
            initLocalRotationArray.AddRange(localRotations);
            positionArray.AddRange(positions);
            rotationArray.AddRange(rotations);
            scaleArray.AddRange(scales);
            localPositionArray.AddRange(localPositions);
            localRotationArray.AddRange(localRotations);
            inverseRotationArray.AddRange(inverseRotations);

            // 変更フラグ
            isDirty = true;

            return indices;
        }

        /// <summary>
        /// 単体トランスフォームを削除する（スレッド可）
        /// 削除は配列インデックスで指定する
        /// 削除されたインデックスはキューに追加され再利用される
        /// </summary>
        /// <param name="index"></param>
        public void RemoveTransformIndex(int index)
        {
            // 削除
            transformList[index] = null;
            flagArray[index] = new ExBitFlag8(); // フラグのみクリア
            emptyStack.Enqueue(index);
        }

        /// <summary>
        /// Transform単体を追加する(tidを指定するならスレッド可）
        /// </summary>
        /// <param name="t"></param>
        /// <param name="tid">0の場合はTransformからGetInstanceId()を即時設定する</param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public int ReplaceTransform(int index, Transform t, int tid = 0, int pid = 0, byte flag = TransformManager.Flag_Read)
        {
            Debug.Assert(index < Count);

            transformList[index] = t;
            flagArray[index] = new ExBitFlag8(flag);
            if (tid == 0)
            {
                // Transformからデータを取得（メインスレッドのみ）
                idArray[index] = t.GetInstanceID();
                parentIdArray[index] = t.parent?.GetInstanceID() ?? 0;
                initLocalPositionArray[index] = t.localPosition;
                initLocalRotationArray[index] = t.localRotation;
                positionArray[index] = t.position;
                rotationArray[index] = t.rotation;
                scaleArray[index] = t.lossyScale;
                localPositionArray[index] = t.localPosition;
                localRotationArray[index] = t.localRotation;
            }
            else
            {
                // 領域のみ初期化
                idArray[index] = tid;
                parentIdArray[index] = pid;
                initLocalPositionArray[index] = 0;
                initLocalRotationArray[index] = quaternion.identity;
                positionArray[index] = 0;
                rotationArray[index] = quaternion.identity;
                scaleArray[index] = 1;
                localPositionArray[index] = 0;
                localRotationArray[index] = quaternion.identity;
            }

            isDirty = true;

            return index;
        }

        /// <summary>
        /// 純粋なクラスポインタのみのIndexOf()実装
        /// List.IndexOf()はスレッドでは利用できない。
        /// Unity.objectの(==)比較は様々な処理が入りGetInstanceId()を利用してしまうため。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        int ReferenceIndexOf<T>(List<T> list, T item) where T : class
        {
            if (list == null)
                return -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return i;
            }
            return -1;
        }

        //=========================================================================================
        /// <summary>
        /// 作業用バッファの更新（メインスレッドのみ）
        /// </summary>
        public void UpdateWorkData()
        {
            // 変更がある場合にはtransformAccessArrayを作り直す
            if (isDirty)
            {
                if (transformAccessArray.isCreated)
                    transformAccessArray.Dispose();

                transformAccessArray = new TransformAccessArray(transformList.ToArray());

                isDirty = false;
            }
        }

        //=========================================================================================
        /// <summary>
        /// Transformを初期姿勢で復元させるジョブを発行する（メインスレッドのみ）
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle RestoreTransform(int count, JobHandle jobHandle = default(JobHandle))
        {
            UpdateWorkData();

            var job = new RestoreTransformJob()
            {
                count = count,
                flagList = flagArray.GetNativeArray(),
                localPositionArray = initLocalPositionArray.GetNativeArray(),
                localRotationArray = initLocalRotationArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(transformAccessArray, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct RestoreTransformJob : IJobParallelForTransform
        {
            public int count;
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotationArray;

            public void Execute(int index, TransformAccess transform)
            {
                if (index >= count)
                    return;
                if (transform.isValid == false)
                    return;

                //byte flag = flagList[index];
                //if ((flag & Flag_Write) != 0)
                {
                    transform.localPosition = localPositionArray[index];
                    transform.localRotation = localRotationArray[index];
                }
            }
        }


        //=========================================================================================
        /// <summary>
        /// トランスフォームを読み込むジョブを発行する（メインスレッドのみ）
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle ReadTransform(JobHandle jobHandle = default(JobHandle))
        {
            UpdateWorkData();

            // todo:未来予測などがあると色々複雑化するところ

            var job = new ReadTransformJob()
            {
                flagList = flagArray.GetNativeArray(),
                positionArray = positionArray.GetNativeArray(),
                rotationArray = rotationArray.GetNativeArray(),
                scaleList = scaleArray.GetNativeArray(),
                localPositionArray = localPositionArray.GetNativeArray(),
                localRotationArray = localRotationArray.GetNativeArray(),
                inverseRotationArray = inverseRotationArray.GetNativeArray(),
            };
            jobHandle = job.ScheduleReadOnly(transformAccessArray, 16, jobHandle);

            return jobHandle;
        }

        public void ReadTransformRun()
        {
            UpdateWorkData();
            var job = new ReadTransformJob()
            {
                flagList = flagArray.GetNativeArray(),
                positionArray = positionArray.GetNativeArray(),
                rotationArray = rotationArray.GetNativeArray(),
                scaleList = scaleArray.GetNativeArray(),
                localPositionArray = localPositionArray.GetNativeArray(),
                localRotationArray = localRotationArray.GetNativeArray(),
                inverseRotationArray = inverseRotationArray.GetNativeArray(),
            };
            job.RunReadOnly(transformAccessArray);
        }

        [BurstCompile]
        struct ReadTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> scaleList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> localRotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> inverseRotationArray;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;

                //byte flag = flagList[index];
                //if ((flag & Flag_Read) != 0)
                {
                    var pos = transform.position;
                    var rot = transform.rotation;
                    float4x4 LtoW = transform.localToWorldMatrix;

                    positionArray[index] = pos;
                    rotationArray[index] = rot;
                    localPositionArray[index] = transform.localPosition;
                    localRotationArray[index] = transform.localRotation;

                    // lossyScale取得(現在はUnity2019.2.14以上のみ)
                    // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                    var irot = math.inverse(rot);
                    var m2 = math.mul(new float4x4(irot, float3.zero), LtoW);
                    var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                    scaleList[index] = scl;

                    // ワールド->ローカル変換用の逆クォータニオン
                    inverseRotationArray[index] = math.inverse(rot);
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// Transformを書き込むジョブを発行する（メインスレッドのみ）
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle WriteTransform(int count, JobHandle jobHandle = default(JobHandle))
        {
            var job = new WriteTransformJob()
            {
                count = count,
                flagList = flagArray.GetNativeArray(),
                worldPositions = positionArray.GetNativeArray(),
                worldRotations = rotationArray.GetNativeArray(),
                localPositions = localPositionArray.GetNativeArray(),
                localRotations = localRotationArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(transformAccessArray, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct WriteTransformJob : IJobParallelForTransform
        {
            public int count;
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> worldPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> worldRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotations;

            public void Execute(int index, TransformAccess transform)
            {
                if (index >= count)
                    return;
                if (transform.isValid == false)
                    return;

                var flag = flagList[index];
                if (flag.IsSet(TransformManager.Flag_WorldRotWrite))
                {
                    // ワールド回転のみ書き込む
                    //transform.position = worldPositions[index];
                    transform.rotation = worldRotations[index];
                }
                else if (flag.IsSet(TransformManager.Flag_LocalPosRotWrite))
                {
                    // ローカル座標・回転を書き込む
                    transform.localPosition = localPositions[index];
                    transform.localRotation = localRotations[index];
                }
            }
        }

        //=========================================================================================
        /// <summary>
        /// リダクション結果に基づいてTransformの情報を再編成する（スレッド可）
        /// </summary>
        /// <param name="vmesh"></param>
        /// <param name="workData"></param>
        public void OrganizeReductionTransform(VirtualMesh vmesh, ReductionWorkData workData)
        {
            // 新しいTransformに対して参照する古いインデックスのリストを作成する
            int newSkinBoneCount = workData.newSkinBoneCount.Value;
            var oldToNewIndexList = new List<int>(newSkinBoneCount + 2);

            // 最初にスキニング用ボーンを追加する
            foreach (var kv in workData.useSkinBoneMap)
            {
                // スキンボーンの実際のトランスフォームインデックスはskinBoneTransformIndicesに格納されている
                oldToNewIndexList.Add(vmesh.skinBoneTransformIndices[kv.Key]);
            }

            // skin rootを追加する
            int newSkinRootIndex = oldToNewIndexList.Count;
            oldToNewIndexList.Add(vmesh.skinRootIndex);

            // center transformを追加する
            int newCenterTransformIndex = oldToNewIndexList.Count;
            oldToNewIndexList.Add(vmesh.centerTransformIndex);

            // 新しいトランスフォームの数
            int newTransformCount = oldToNewIndexList.Count;

            // 新しい領域
            var newTransformList = new List<Transform>(newTransformCount);
            var newTransformIdArray = new ExSimpleNativeArray<int>(newTransformCount);
            var newParentIdArray = new ExSimpleNativeArray<int>(newTransformCount);
            var newFlagArray = new ExSimpleNativeArray<ExBitFlag8>(newTransformCount);
            var newInitLocalPositionArray = new ExSimpleNativeArray<float3>(newTransformCount);
            var newInitLocalRotationArray = new ExSimpleNativeArray<quaternion>(newTransformCount);
            var newPositionArray = new ExSimpleNativeArray<float3>(newTransformCount);
            var newRotationArray = new ExSimpleNativeArray<quaternion>(newTransformCount);
            var newScaleArray = new ExSimpleNativeArray<float3>(newTransformCount);

            // データコピー
            for (int i = 0; i < newTransformCount; i++)
            {
                int oldIndex = oldToNewIndexList[i];
                newTransformList.Add(transformList[oldIndex]);

                newTransformIdArray[i] = idArray[oldIndex];
                newParentIdArray[i] = parentIdArray[oldIndex];
                newFlagArray[i] = flagArray[oldIndex];
                newInitLocalPositionArray[i] = initLocalPositionArray[oldIndex];
                newInitLocalRotationArray[i] = initLocalRotationArray[oldIndex];
                newPositionArray[i] = positionArray[oldIndex];
                newRotationArray[i] = rotationArray[oldIndex];
                newScaleArray[i] = scaleArray[oldIndex];
            }

            // 以前の要素を破棄する
            transformList.Clear();
            idArray.Dispose();
            parentIdArray.Dispose();
            flagArray.Dispose();
            initLocalPositionArray.Dispose();
            initLocalRotationArray.Dispose();
            positionArray.Dispose();
            rotationArray.Dispose();
            scaleArray.Dispose();

            // 新しい要素に組み換え
            transformList = newTransformList;
            idArray = newTransformIdArray;
            parentIdArray = newParentIdArray;
            flagArray = newFlagArray;
            initLocalPositionArray = newInitLocalPositionArray;
            initLocalRotationArray = newInitLocalRotationArray;
            positionArray = newPositionArray;
            rotationArray = newRotationArray;
            scaleArray = newScaleArray;

            // 管理情報更新
            emptyStack.Clear();

            // Virtual Mesh修正
            vmesh.centerTransformIndex = newCenterTransformIndex;
            vmesh.skinRootIndex = newSkinRootIndex;

            // Dirty
            isDirty = true;
        }

        //=========================================================================================
        public Transform GetTransformFromIndex(int index)
        {
            return transformList[index];
        }

        /// <summary>
        /// IDのトランスフォームインデックスを返す
        /// 順次検索なのでコストに注意！
        /// </summary>
        /// <param name="id"></param>
        /// <returns>-1=見つからない</returns>
        public int GetTransformIndexFormId(int id)
        {
            var array = idArray.GetNativeArray();
            int cnt = Count;
            for (int i = 0; i < cnt; i++)
            {
                if (array[i] == id)
                    return i;
            }
            return -1;
        }

        public int GetTransformIdFromIndex(int index)
        {
            return idArray[index];
        }

        public int GetParentIdFromIndex(int index)
        {
            return parentIdArray[index];
        }

        public float4x4 GetLocalToWorldMatrix(int index)
        {
            var pos = positionArray[index];
            var rot = rotationArray[index];
            var scl = scaleArray[index];
            return Matrix4x4.TRS(pos, rot, scl);
        }

        public float4x4 GetWorldToLocalMatrix(int index)
        {
            return math.inverse(GetLocalToWorldMatrix(index));
        }
    }
}
