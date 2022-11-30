// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System;
using System.Linq;
using System.Collections.Generic;
#if MAGICACLOTH_ECS
using Unity.Entities;
#endif
using UnityEngine;
using UnityEngine.LowLevel;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaCloth物理マネージャ
    /// </summary>
    [HelpURL("https://magicasoft.jp/magica-cloth-physics-manager/")]
    public partial class MagicaPhysicsManager : CreateSingleton<MagicaPhysicsManager>
    {
        /// <summary>
        /// 更新管理
        /// </summary>
        [SerializeField]
        UpdateTimeManager updateTime = new UpdateTimeManager();

        /// <summary>
        /// パーティクルデータ
        /// </summary>
        PhysicsManagerParticleData particle = new PhysicsManagerParticleData();

        /// <summary>
        /// トランスフォームデータ
        /// </summary>
        PhysicsManagerBoneData bone = new PhysicsManagerBoneData();

        /// <summary>
        /// メッシュデータ
        /// </summary>
        PhysicsManagerMeshData mesh = new PhysicsManagerMeshData();

        /// <summary>
        /// チームデータ
        /// </summary>
        PhysicsManagerTeamData team = new PhysicsManagerTeamData();

        /// <summary>
        /// 風データ
        /// </summary>
        PhysicsManagerWindData wind = new PhysicsManagerWindData();

        /// <summary>
        /// 全コンポーネントデータ
        /// </summary>
        PhysicsManagerComponent component = new PhysicsManagerComponent();

        /// <summary>
        /// 物理計算処理
        /// </summary>
        PhysicsManagerCompute compute = new PhysicsManagerCompute();

        /// <summary>
        /// Unity2021.2以降でのGetVertexBuffer()による高速書き込みの利用
        /// 実行中は変更できない
        /// </summary>
        [SerializeField]
        private bool useFasterWrite = false;

        // コンピュートシェーダー
        private ComputeShader meshWriter = null;

        //=========================================================================================
        /// <summary>
        /// シミュレーション計算前イベント
        /// Simulation pre-calculation event.
        /// </summary>
        public PhysicsManagerPreUpdateEvent OnPreUpdate = new PhysicsManagerPreUpdateEvent();

        /// <summary>
        /// シミュレーション計算後イベント
        /// Simulation post-calculation event.
        /// </summary>
        public PhysicsManagerPostUpdateEvent OnPostUpdate = new PhysicsManagerPostUpdateEvent();

        //=========================================================================================
        /// <summary>
        /// 遅延実行の有無
        /// ランタイムで変更できるようにバッファリング
        /// </summary>
        private bool useDelay = false;

        /// <summary>
        /// Update()でのPlayerLoopチェック完了フラグ
        /// </summary>
        private bool updatePlayerLoop = false;

        /// <summary>
        /// マネージャ全体のアクティブフラグ
        /// </summary>
        private bool isActive = true;

        //=========================================================================================
        public UpdateTimeManager UpdateTime
        {
            get
            {
                return updateTime;
            }
        }

        public PhysicsManagerParticleData Particle
        {
            get
            {
                particle.SetParent(this);
                return particle;
            }
        }

        public PhysicsManagerBoneData Bone
        {
            get
            {
                bone.SetParent(this);
                return bone;
            }
        }

        public PhysicsManagerMeshData Mesh
        {
            get
            {
                mesh.SetParent(this);
                return mesh;
            }
        }

        public PhysicsManagerTeamData Team
        {
            get
            {
                team.SetParent(this);
                return team;
            }
        }

        public PhysicsManagerWindData Wind
        {
            get
            {
                wind.SetParent(this);
                return wind;
            }
        }

        public PhysicsManagerComponent Component
        {
            get
            {
                component.SetParent(this);
                return component;
            }
        }

        public PhysicsManagerCompute Compute
        {
            get
            {
                compute.SetParent(this);
                return compute;
            }
        }

        public bool IsDelay
        {
            get
            {
                return useDelay;
            }
        }

        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                // アクティブはコンポーネントのenableフラグで行う
                this.enabled = value;
            }
        }

        public bool IsFasterWrite
        {
            get
            {
                if (useFasterWrite)
                {
#if UNITY_2021_2_OR_NEWER
                    if (MeshWriterShader != null)
                    {
                        if (MeshWriterShader.IsSupported(0) && MeshWriterShader.IsSupported(1))
                        {
                            return true;
                        }
                    }
#endif
                }
                return false;
            }
        }

        internal ComputeShader MeshWriterShader
        {
            get
            {
                if (meshWriter == null)
                {
                    meshWriter = (ComputeShader)Resources.Load("MeshWriter");
                }
                return meshWriter;
            }
        }

        //=========================================================================================
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        protected override void InitSingleton()
        {
            Component.Create();
            Particle.Create();
            Bone.Create();
            Mesh.Create();
            Team.Create();
            Wind.Create();
            Compute.Create();
        }

        /// <summary>
        /// ２つ目の破棄されるマネージャの通知
        /// </summary>
        /// <param name="duplicate"></param>
        protected override void DuplicateDetection(MagicaPhysicsManager duplicate)
        {
            // 設定をコピーする
            UpdateMode = duplicate.UpdateMode;
            UpdatePerSeccond = duplicate.UpdatePerSeccond;
            FuturePredictionRate = duplicate.FuturePredictionRate;
        }

        protected void OnEnable()
        {
            if (isActive == false)
            {
                isActive = true;
                Component.UpdateComponentStatus();
            }
        }

        protected void OnDisable()
        {
            if (isActive == true)
            {
                isActive = false;
                Component.UpdateComponentStatus();
            }
        }

        private void Update()
        {
            // Unity2019.3以降の場合はUpdate時に一度カスタムループの登録チェックを行う
            // すでに登録されていればスルーし、登録されていなければ再登録する
            // これは他のアセットによりPlayerLoopが書き換えられてしまった場合の対策です
            if (updatePlayerLoop == false)
            {
                //Debug.Log("Update check!!");
                InitCustomGameLoop();
                updatePlayerLoop = true;
            }
        }

        private void FixedUpdate()
        {
            if (isActive)
            {
                UpdateTime.AddFixedUpdateCount();
            }
        }

        /// <summary>
        /// 破棄
        /// </summary>
        protected override void OnDestroy()
        {
            Compute.Dispose();
            Wind.Dispose();
            Team.Dispose();
            Mesh.Dispose();
            Bone.Dispose();
            Particle.Dispose();
            Component.Dispose();

            base.OnDestroy();
        }

        //=========================================================================================
        /// <summary>
        /// EarlyUpdateの後
        /// </summary>
        private void AfterEarlyUpdate()
        {
            //Debug.Log($"After Early Update! F:{Time.frameCount}");

            // フレーム開始時に行うチーム更新
            Team.EarlyUpdateTeamAlways();
        }

        //private void BeforeFixedUpdate()
        //{
        //    //Debug.Log("Before Fixed Update!" + Time.frameCount);

        //    // シミュレーションに必要なボーンの状態をもとに戻す(更新モード = UnityPhysics)
        //    if (Team.ActiveTeamCount > 0 && Team.PhysicsUpdateCount > 0)
        //    {
        //        Compute.InitJob();
        //        Compute.UpdateRestoreBone(PhysicsTeam.TeamUpdateMode.UnityPhysics);
        //        Compute.CompleteJob();
        //    }
        //}

        private void AfterFixedUpdate()
        {
            //Debug.Log("After Fixed Update!" + Time.frameCount);

            // シミュレーションに必要なボーンの状態をもとに戻す(更新モード = UnityPhysics)
            if (Team.ActiveTeamCount > 0 && Team.PhysicsUpdateCount > 0)
            {
                Compute.InitJob();
                Compute.UpdateRestoreBone(PhysicsTeam.TeamUpdateMode.UnityPhysics);
                Compute.CompleteJob();
            }
        }

        /// <summary>
        /// Update()後の更新
        /// </summary>
        private void AfterUpdate()
        {
            //Debug.Log("After Update!" + Time.frameCount);

            // シミュレーションに必要なボーンの状態をもとに戻す(更新モード = Normal)
            if (Team.ActiveTeamCount > 0 && Team.NormalUpdateCount > 0)
            {
                Compute.InitJob();
                Compute.UpdateRestoreBone(PhysicsTeam.TeamUpdateMode.Normal);
                Compute.CompleteJob();
            }
        }

        /// <summary>
        /// LateUpdate()前の更新
        /// </summary>
        private void BeforeLateUpdate()
        {
            //Debug.Log("Before Late Update!" + Time.frameCount);
            if (updateTime.GetUpdateLocation() == UpdateTimeManager.UpdateLocation.BeforeLateUpdate)
            {
                DefaultUpdate();
            }
        }

        /// <summary>
        /// LateUpdate()後の更新
        /// </summary>
        private void AfterLateUpdate()
        {
            //Debug.Log("After Late Update!" + Time.frameCount);
            //Debug.Log("dtime:" + Time.deltaTime + " smooth:" + Time.smoothDeltaTime);
            if (updateTime.GetUpdateLocation() == UpdateTimeManager.UpdateLocation.AfterLateUpdate)
            {
                DefaultUpdate();
            }
        }

        private void DefaultUpdate()
        {
            // 遅延実行の切り替え判定
            if (useDelay != UpdateTime.IsDelay)
            {
                if (useDelay == false)
                {
                    // 結果の保持
                    Compute.UpdateSwapBuffer();
                    Compute.UpdateSyncBuffer();
                }
                useDelay = UpdateTime.IsDelay;
            }

            // パーティクルコンポーネントのデータ更新処理
            Component.DataUpdateParticleComponent();

            if (useDelay == false)
            {
                // 即時
                OnPreUpdate.Invoke();
                Compute.UpdateTeamAlways();
                Compute.InitJob();
                Compute.UpdateReadBone();
                Compute.UpdateStartSimulation(updateTime);
                Compute.UpdateWriteBone();
                Compute.MeshCalculation();
                Compute.UpdateCompleteSimulation();
                Compute.NormalWritingMesh();
                OnPostUpdate.Invoke();
            }
        }

        /// <summary>
        /// PostLateUpdate.ScriptRunDelayedDynamicFrameRateの後
        /// LateUpdate()やアセットバンドルロード完了コールバックでクロスコンポーネントをインスタンス化すると、
        /// Start()が少し遅れてPostLateUpdateのScriptRunDelayedDynamicFrameRateで呼ばれることになる。
        /// 遅延実行時にこの処理が入ると、すでにクロスシミュレーションのジョブが開始されているため、
        /// Start()の初期化処理などでNativeリストにアクセスすると例外が発生してしまう。
        /// 従って遅延実行時はクロスコンポーネントのStart()が完了するScriptRunDelayedDynamicFrameRate
        /// の後にシミュレーションを開始するようにする。(v1.5.1)
        /// </summary>
        private void PostLateUpdate()
        {
            //Debug.Log("Post Late Update!" + Time.frameCount);
            if (useDelay)
            {
                // 遅延実行
                OnPreUpdate.Invoke();
                Compute.UpdateTeamAlways();
                Compute.InitJob();
                Compute.UpdateReadWriteBone();
                Compute.UpdateStartSimulation(updateTime);
                Compute.ScheduleJob();
                Compute.MeshCalculation();
                Compute.NormalWritingMesh(); // 前回の結果をメッシュに反映
                //Debug.Log($"Delay Job! F:{Time.frameCount}");
            }
        }

        /// <summary>
        /// レンダリング完了後の更新
        /// </summary>
        private void AfterRendering()
        {
            //Debug.Log($"After Rendering Update! F:{Time.frameCount}");
            if (useDelay)
            {
                // 遅延実行
                // シミュレーション終了待機
                Compute.UpdateCompleteSimulation();
                // 結果の保持
                Compute.UpdateSwapBuffer();
                Compute.UpdateSyncBuffer();
                OnPostUpdate.Invoke();
            }

            // シミュレーションに必要なボーンの状態をもとに戻す
            //Compute.InitJob();
            //Compute.UpdateRestoreBone();
            //Compute.CompleteJob();

            // FixedUpdateCountクリア
            UpdateTime.ResetFixedUpdateCount();
        }

        //=========================================================================================
        /// <summary>
        /// Reload Domain 対策
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            InitMember();
        }

        /// <summary>
        /// カスタム更新ループ登録
        /// </summary>
        [RuntimeInitializeOnLoadMethod()]
        public static void InitCustomGameLoop()
        {
            //Debug.Log("PhysicsManager.InitCustomGameLoop()");
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            // すでに設定されているならばスルー
            if (CheckRegist(ref playerLoop))
            {
                //Debug.Log("Skip!!");
                return;
            }

            // MagicaCloth用PlayerLoopを追加
            SetCustomGameLoop(ref playerLoop);

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>
        /// playerLoopにMagicaClothで必要なCustomPlayerLoopを追加します
        /// </summary>
        /// <param name="playerLoop"></param>
        public static void SetCustomGameLoop(ref PlayerLoopSystem playerLoop)
        {
#if false
            // debug
            foreach (var header in playerLoop.subSystemList)
            {
                Debug.LogFormat("------{0}------", header.type.Name);
                foreach (var subSystem in header.subSystemList)
                {
                    Debug.LogFormat("{0}.{1}", header.type.Name, subSystem.type.Name);
                }
            }
#endif

            PlayerLoopSystem afterEarlyUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaPhysicsManager),
                updateDelegate = () =>
                {
                    if (IsInstance())
                    {
                        Instance.AfterEarlyUpdate();
                    }
                }
            };

            //PlayerLoopSystem beforeFixedUpdate = new PlayerLoopSystem()
            //{
            //    type = typeof(MagicaPhysicsManager),
            //    updateDelegate = () =>
            //    {
            //        if (IsInstance())
            //        {
            //            Instance.BeforeFixedUpdate();
            //        }
            //    }
            //};

            PlayerLoopSystem afterFixedUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaPhysicsManager),
                updateDelegate = () =>
                {
                    if (IsInstance())
                    {
                        Instance.AfterFixedUpdate();
                    }
                }
            };

            PlayerLoopSystem afterUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaPhysicsManager),
                updateDelegate = () =>
                {
                    if (IsInstance())
                    {
                        Instance.AfterUpdate();
                    }
                }
            };

            PlayerLoopSystem beforeLateUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaPhysicsManager),
                updateDelegate = () =>
                {
                    if (IsInstance())
                    {
                        Instance.BeforeLateUpdate();
                    }
                }
            };

            PlayerLoopSystem afterLateUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaPhysicsManager),
                updateDelegate = () =>
                {
                    if (IsInstance())
                    {
                        Instance.AfterLateUpdate();
                    }
                }
            };

            PlayerLoopSystem postLateUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaPhysicsManager),
                updateDelegate = () =>
                {
                    if (IsInstance())
                    {
                        Instance.PostLateUpdate();
                    }
                }
            };

            PlayerLoopSystem afterRendering = new PlayerLoopSystem()
            {
                type = typeof(MagicaPhysicsManager),
                updateDelegate = () =>
                {
                    if (IsInstance())
                    {
                        Instance.AfterRendering();
                    }
                }
            };

            int sysIndex = 0;
            int index = 0;

            // early update
            sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == "EarlyUpdate");
            PlayerLoopSystem earlyUpdateSystem = playerLoop.subSystemList[sysIndex];
            var earlyUpdateSubsystemList = new List<PlayerLoopSystem>(earlyUpdateSystem.subSystemList);
            earlyUpdateSubsystemList.Add(afterEarlyUpdate);
            earlyUpdateSystem.subSystemList = earlyUpdateSubsystemList.ToArray();
            playerLoop.subSystemList[sysIndex] = earlyUpdateSystem;

            // fixed udpate
            //sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == "FixedUpdate");
            //PlayerLoopSystem fixedUpdateSystem = playerLoop.subSystemList[sysIndex];
            //var fixedUpdateSubsystemList = new List<PlayerLoopSystem>(fixedUpdateSystem.subSystemList);
            //fixedUpdateSubsystemList.Insert(0, beforeFixedUpdate);
            //fixedUpdateSystem.subSystemList = fixedUpdateSubsystemList.ToArray();
            //playerLoop.subSystemList[sysIndex] = fixedUpdateSystem;

            // after fixed update
            sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == "FixedUpdate");
            PlayerLoopSystem fixedUpdateSystem = playerLoop.subSystemList[sysIndex];
            var fixedUpdateSubsystemList = new List<PlayerLoopSystem>(fixedUpdateSystem.subSystemList);
            index = fixedUpdateSubsystemList.FindIndex(h => h.type.Name.Contains("ScriptRunBehaviourFixedUpdate"));
            fixedUpdateSubsystemList.Insert(index + 1, afterFixedUpdate); // FixedUpdate() after
            fixedUpdateSystem.subSystemList = fixedUpdateSubsystemList.ToArray();
            playerLoop.subSystemList[sysIndex] = fixedUpdateSystem;

            // update
            sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == "Update");
            PlayerLoopSystem updateSystem = playerLoop.subSystemList[sysIndex];
            var updateSubsystemList = new List<PlayerLoopSystem>(updateSystem.subSystemList);
            index = updateSubsystemList.FindIndex(h => h.type.Name.Contains("ScriptRunDelayedDynamicFrameRate"));
            updateSubsystemList.Insert(index + 1, afterUpdate); // Update() after
            updateSystem.subSystemList = updateSubsystemList.ToArray();
            playerLoop.subSystemList[sysIndex] = updateSystem;

            // late update
            sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == "PreLateUpdate");
            PlayerLoopSystem lateUpdateSystem = playerLoop.subSystemList[sysIndex];
            var lateUpdateSubsystemList = new List<PlayerLoopSystem>(lateUpdateSystem.subSystemList);
            index = lateUpdateSubsystemList.FindIndex(h => h.type.Name.Contains("ScriptRunBehaviourLateUpdate"));
            lateUpdateSubsystemList.Insert(index, beforeLateUpdate); // LateUpdate() before
            lateUpdateSubsystemList.Insert(index + 2, afterLateUpdate); // LateUpdate() after
            //lateUpdateSubsystemList.Insert(index + 1, afterLateUpdate); // LateUpdate() after
            lateUpdateSystem.subSystemList = lateUpdateSubsystemList.ToArray();
            playerLoop.subSystemList[sysIndex] = lateUpdateSystem;

            // post late update
            sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == "PostLateUpdate");
            PlayerLoopSystem postLateUpdateSystem = playerLoop.subSystemList[sysIndex];
            var postLateUpdateSubsystemList = new List<PlayerLoopSystem>(postLateUpdateSystem.subSystemList);
            index = postLateUpdateSubsystemList.FindIndex(h => h.type.Name.Contains("ScriptRunDelayedDynamicFrameRate"));
            postLateUpdateSubsystemList.Insert(index + 1, postLateUpdate); // postLateUpdate()
            postLateUpdateSystem.subSystemList = postLateUpdateSubsystemList.ToArray();
            playerLoop.subSystemList[sysIndex] = postLateUpdateSystem;

            // rendering
            sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == "PostLateUpdate");
            PlayerLoopSystem postLateSystem = playerLoop.subSystemList[sysIndex];
            var postLateSubsystemList = new List<PlayerLoopSystem>(postLateSystem.subSystemList);
            index = postLateSubsystemList.FindIndex(h => h.type.Name.Contains("FinishFrameRendering"));
            postLateSubsystemList.Insert(index + 1, afterRendering); // rendering after
            postLateSystem.subSystemList = postLateSubsystemList.ToArray();
            playerLoop.subSystemList[sysIndex] = postLateSystem;
        }

        /// <summary>
        /// MagicaClothのカスタムループが登録されているかチェックする
        /// </summary>
        /// <param name="playerLoop"></param>
        /// <returns></returns>
        private static bool CheckRegist(ref PlayerLoopSystem playerLoop)
        {
            var t = typeof(MagicaPhysicsManager);
            foreach (var subloop in playerLoop.subSystemList)
            {
                if (subloop.subSystemList != null && subloop.subSystemList.Any(x => x.type == t))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
