// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Jobs;

namespace MagicaCloth2
{
    /// <summary>
    /// 各クロスコンポーネントの更新処理
    /// </summary>
    public class ClothManager : IManager, IValid
    {
        internal HashSet<ClothProcess> clothSet = new HashSet<ClothProcess>();
        internal HashSet<ClothProcess> boneClothSet = new HashSet<ClothProcess>();
        internal HashSet<ClothProcess> meshClothSet = new HashSet<ClothProcess>();

        //=========================================================================================
        /// <summary>
        /// マスタージョブハンドル
        /// </summary>
        JobHandle masterJob = default;

        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            clothSet.Clear();
            boneClothSet.Clear();
            meshClothSet.Clear();

            // 作業バッファ

            // 更新処理
            MagicaManager.afterEarlyUpdateDelegate -= TransformRestoreUpdate;
            MagicaManager.afterLateUpdateDelegate -= StartClothUpdate;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            clothSet.Clear();
            boneClothSet.Clear();
            meshClothSet.Clear();

            // 作業バッファ

            // 更新処理
            MagicaManager.afterEarlyUpdateDelegate += TransformRestoreUpdate;
            MagicaManager.afterLateUpdateDelegate += StartClothUpdate;

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        void ClearMasterJob()
        {
            masterJob = default;
        }

        void CompleteMasterJob()
        {
            masterJob.Complete();
        }

        //=========================================================================================
        internal int AddCloth(ClothProcess cprocess, in ClothParameters clothParams)
        {
            // この段階でProxyMeshは完成している
            if (isValid == false)
                return 0;

            // チーム登録
            var teamId = MagicaManager.Team.AddTeam(cprocess, clothParams);
            if (teamId == 0)
                return 0;

            clothSet.Add(cprocess);
            if (cprocess.clothType == ClothProcess.ClothType.BoneCloth)
                boneClothSet.Add(cprocess);
            else
                meshClothSet.Add(cprocess);

            return teamId;
        }

        internal void RemoveCloth(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            // チーム解除
            MagicaManager.Team.RemoveTeam(cprocess.TeamId);

            clothSet.Remove(cprocess);
            boneClothSet.Remove(cprocess);
            meshClothSet.Remove(cprocess);
        }

        //=========================================================================================
        /// <summary>
        /// BoneClothのTransform復元更新
        /// </summary>
        void TransformRestoreUpdate()
        {
            if (MagicaManager.Team.ActiveTeamCount > 0)
            {
                ClearMasterJob();
                masterJob = MagicaManager.Bone.RestoreTransform(masterJob);
                CompleteMasterJob();
            }
        }


        //=========================================================================================
        /// <summary>
        /// クロスコンポーネントの更新開始
        /// </summary>
        void StartClothUpdate()
        {
            if (MagicaManager.IsPlaying() == false)
                return;

            //-----------------------------------------------------------------
            // シミュレーション開始イベント
            MagicaManager.OnPreSimulation?.Invoke();

            //-----------------------------------------------------------------
            var tm = MagicaManager.Team;
            var vm = MagicaManager.VMesh;
            var sm = MagicaManager.Simulation;
            var bm = MagicaManager.Bone;
            var wm = MagicaManager.Wind;

            //Debug.Log($"StartClothUpdate. F:{Time.frameCount}");
            //Develop.DebugLog($"StartClothUpdate. F:{Time.frameCount}, dtime:{Time.deltaTime}, stime:{Time.smoothDeltaTime}");

            //-----------------------------------------------------------------
            // ■時間マネージャ更新
            MagicaManager.Time.FrameUpdate();

            // ■常に実行するチーム更新
            tm.AlwaysTeamUpdate();

            // ■ここで実行チーム数が０ならば終了
            if (tm.ActiveTeamCount == 0)
                return;

            int maxUpdateCount = tm.maxUpdateCount.Value;
            //Debug.Log($"maxUpdateCount:{maxUpdateCount}");

            // ■常に実行する風ゾーン更新
            wm.AlwaysWindUpdate();

            // ■作業バッファ更新
            sm.WorkBufferUpdate();

            //-----------------------------------------------------------------
            // マスタージョブ初期化
            ClearMasterJob();

            // ■トランスフォーム情報の読み込み
            masterJob = bm.ReadTransform(masterJob);

            // ■プロキシメッシュをスキニングし基本姿勢を求める
            masterJob = vm.PreProxyMeshUpdate(masterJob);

            //-----------------------------------------------------------------
            // チームのセンター姿勢の決定と慣性用の移動量計算
            masterJob = tm.CalcCenterAndInertiaAndWind(masterJob);

            // パーティクルリセットの適用
            masterJob = sm.PreSimulationUpdate(masterJob);

            // ■コライダーのローカル姿勢を求める
            masterJob = MagicaManager.Collider.PreSimulationUpdate(masterJob);

            //-----------------------------------------------------------------
            // ■クロスシミュレーション実行
            // ステップ実行
            for (int i = 0; i < maxUpdateCount; i++)
            {
                masterJob = sm.SimulationStepUpdate(maxUpdateCount, i, masterJob);
            }

            //-----------------------------------------------------------------
            // 表示位置の決定
            masterJob = sm.CalcDisplayPosition(masterJob);

            //-----------------------------------------------------------------
            // ■クロスシミュレーション後の頂点姿勢計算
            // プロキシメッシュの頂点から法線接線を求め姿勢を確定させる
            // ラインがある場合はベースラインごとに姿勢を整える
            // BoneClothの場合は頂点姿勢を連動するトランスフォームデータにコピーする
            masterJob = vm.PostProxyMeshUpdate(masterJob);

            // マッピングメッシュ
            int mappingCount = tm.MappingCount;
            if (mappingCount > 0)
            {
                // マッピングメッシュ頂点姿勢をプロキシメッシュからスキニングし求める
                // マッピングメッシュのローカル空間に座標変換する
                masterJob = vm.PostMappingMeshUpdate(masterJob);

                // レンダーデータへ反映する
                foreach (var cprocess in meshClothSet)
                {
                    if (cprocess == null || cprocess.IsValid() == false || cprocess.IsEnable == false)
                        continue;

                    int cnt = cprocess.renderMeshInfoList.Count;
                    for (int i = 0; i < cnt; i++)
                    {
                        var info = cprocess.renderMeshInfoList[i];
                        var renderData = MagicaManager.Render.GetRendererData(info.renderHandle);

                        // Position/Normal書き込み
                        masterJob = renderData.UpdatePositionNormal(info.mappingChunk, masterJob);

                        // BoneWeight書き込み
                        if (renderData.ChangeCustomMesh)
                        {
                            masterJob = renderData.UpdateBoneWeight(info.mappingChunk, masterJob);
                        }
                    }
                }
            }

            //-----------------------------------------------------------------
            // ■BoneClothのTransformへの書き込み
            masterJob = bm.WriteTransform(masterJob);

            //-----------------------------------------------------------------
            // ■コライダー更新後処理
            masterJob = MagicaManager.Collider.PostSimulationUpdate(masterJob);

            // ■チーム更新後処理
            masterJob = tm.PostTeamUpdate(masterJob);

            //-----------------------------------------------------------------
            // ジョブを即実行
            //JobHandle.ScheduleBatchedJobs();

            //-----------------------------------------------------------------
            // ■現在は即時実行のためここでジョブの完了待ちを行う
            CompleteMasterJob();

            //-----------------------------------------------------------------
            // シミュレーション終了イベント
            MagicaManager.OnPostSimulation?.Invoke();
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
