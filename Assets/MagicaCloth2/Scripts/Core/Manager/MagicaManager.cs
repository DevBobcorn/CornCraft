// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using System.Linq;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor.Compilation;
using UnityEditor;
#if UNITY_2023_1_OR_NEWER
using UnityEditor.Build;
#endif
#endif

// コードストリッピングを無効化する 
[assembly: AlwaysLinkAssembly]

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothマネージャ
    /// </summary>
    public static partial class MagicaManager
    {
        //=========================================================================================
        /// <summary>
        /// 登録マネージャリスト
        /// </summary>
        static List<IManager> managers = null;

        public static TimeManager Time => managers?[0] as TimeManager;
        public static TeamManager Team => managers?[1] as TeamManager;
        public static ClothManager Cloth => managers?[2] as ClothManager;
        public static RenderManager Render => managers?[3] as RenderManager;
        public static TransformManager Bone => managers?[4] as TransformManager;
        public static VirtualMeshManager VMesh => managers?[5] as VirtualMeshManager;
        public static SimulationManager Simulation => managers?[6] as SimulationManager;
        public static ColliderManager Collider => managers?[7] as ColliderManager;
        public static WindManager Wind => managers?[8] as WindManager;

        //=========================================================================================
        // player loop delegate
        public delegate void UpdateMethod();

        /// <summary>
        /// フレームの開始時、すべてのEarlyUpdateの後、FixedUpdate()の前
        /// </summary>
        public static UpdateMethod afterEarlyUpdateDelegate;

        /// <summary>
        /// FixedUpdate()の後
        /// </summary>
        public static UpdateMethod afterFixedUpdateDelegate;

        /// <summary>
        /// Update()の後
        /// </summary>
        public static UpdateMethod afterUpdateDelegate;

        /// <summary>
        /// LateUpdate()の後
        /// </summary>
        public static UpdateMethod afterLateUpdateDelegate;

        /// <summary>
        /// LateUpdate()後の遅延処理後、yield nullの後
        /// </summary>
        public static UpdateMethod afterDelayedDelegate;

        /// <summary>
        /// レンダリング完了後
        /// </summary>
        public static UpdateMethod afterRenderingDelegate;

        /// <summary>
        /// 汎用的な定期更新
        /// ゲーム実行中はUpdate()後に呼び出さる。
        /// エディタではEditorApplication.updateデリゲートにより呼び出される。
        /// </summary>
        public static UpdateMethod defaultUpdateDelegate;


        //=========================================================================================
        static volatile bool isPlaying = false;

        //static bool isValid = false;

        //=========================================================================================
        /// <summary>
        /// Reload Domain 対策
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            Dispose();

            // Reload Domainを設定しているとstatic変数が実行時に初期化されない
            // そのためここでstatic変数の再初期化を行う必要がある
            Develop.DebugLog("SubsystemRegistration");

#if UNITY_EDITOR
            // スクリプトコンパイル開始コールバック
            CompilationPipeline.compilationStarted += OnStarted;
#endif

            // 各マネージャの初期化
            managers = new List<IManager>();
            managers.Add(new TimeManager()); // [0]
            managers.Add(new TeamManager()); // [1]
            managers.Add(new ClothManager()); // [2]
            managers.Add(new RenderManager()); // [3]
            managers.Add(new TransformManager()); // [4]
            managers.Add(new VirtualMeshManager()); // [5]
            managers.Add(new SimulationManager()); // [6]
            managers.Add(new ColliderManager()); // [7]
            managers.Add(new WindManager()); // [8]
            foreach (var manager in managers)
                manager.Initialize();

            // カスタム更新ループ登録
            InitCustomGameLoop();

            isPlaying = true;
            //isValid = true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタの実行状態が変更された場合に呼び出される
        /// </summary>
        [InitializeOnLoadMethod]
        static void PlayModeStateChange()
        {
            // プロジェクトセッティングにMagicaCloth2用デファインシンボルを登録する
            try
            {
#if UNITY_2023_1_OR_NEWER
                var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] newDefines);
                if (newDefines.Contains(Define.System.DefineSymbol) == false)
                {
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newDefines.Concat(new string[] { Define.System.DefineSymbol }).ToArray());
                }
#else
                var newDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');
                if (newDefines.Contains(Define.System.DefineSymbol) == false)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines.Concat(new string[] { Define.System.DefineSymbol }).ToArray());
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // エディタ状態変更時イベント処理
            EditorApplication.playModeStateChanged += (mode) =>
            {
                Develop.DebugLog($"PlayModeStateChanged:{mode} F:{UnityEngine.Time.frameCount}");

                if (mode == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    // ★ここではマネージャを破棄しない
                    // ★スケジュールされたジョブなどはエディタ実行停止後も完了まで稼働し続けるため。

                    // 実行状態の終了
                    isPlaying = false;
                    //isValid = false;
                    EnterdEditMode();

                    // エディタでの定期更新開始
                    EditorApplication.update += EditoruUpdate;
                }

                if (mode == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    // エディタでの定期更新終了
                    EditorApplication.update -= EditoruUpdate;
                }


                //if (mode == UnityEditor.PlayModeStateChange.ExitingEditMode || mode == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                //{
                //    // 各マネージャの終了
                //    // ★どうもこの呼出後に１フレームゲームが更新されてしまうようだ
                //    // ★なのですぐDispose()すると色々面倒なことになる
                //    //rendererManager?.Dispose();
                //    //rendererManager = null;
                //}
            };
        }

        /// <summary>
        /// スクリプトコンパイル開始
        /// </summary>
        /// <param name="obj"></param>
        static void OnStarted(object obj)
        {
            //Debug.Log($"スクリプトコンパイル開始");
            isPlaying = false;
            EnterdEditMode();
            //Dispose();

        }

        /// <summary>
        /// スクリプトコンパイル後
        /// </summary>
        //[DidReloadScripts(0)]
        //static void ReloadScripts()
        //{
        //    //Initialize();
        //}

        /// <summary>
        /// ゲームプレイの実行が停止したとき（エディタ環境のみ）
        /// </summary>
        static void EnterdEditMode()
        {
            //Debug.Log($"★Manager Enterd Edit Mode.");
            if (managers != null)
            {
                //int index = 0;
                foreach (var manager in managers)
                {
                    //Debug.Log($"Dispose [{index}] start.");
                    manager.EnterdEditMode();
                    //Debug.Log($"Dispose [{index}] end.");
                    //index++;
                }
            }
        }

        /// <summary>
        /// エディタでの定期更新
        /// </summary>
        static void EditoruUpdate()
        {
            defaultUpdateDelegate?.Invoke();
        }
#endif


        /// <summary>
        /// マネージャの破棄
        /// </summary>
        static void Dispose()
        {
            Develop.DebugLog("Manager Dispose!");

            if (managers != null)
            {
                foreach (var manager in managers)
                    manager.Dispose();
                managers = null;
            }

            // clear static member.
            OnPreSimulation = null;
            OnPostSimulation = null;
        }

        public static bool IsPlaying()
        {
            //return isPlaying;
            return isPlaying && Application.isPlaying;
        }

        //=========================================================================================
        /// <summary>
        /// カスタム更新ループ登録
        /// すでに登録されている場合は何もしない
        /// Custom update loop registration.
        /// Do nothing if already registered.
        /// </summary>
        public static void InitCustomGameLoop()
        {
            //Debug.Log("PhysicsManager.InitCustomGameLoop()");
            PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            // すでに設定されているならばスルー
            if (CheckRegist(ref playerLoop))
            {
                //Develop.DebugLog("SetCustomGameLoop Skip!!");
                return;
            }

            // MagicaCloth用PlayerLoopを追加
            SetCustomGameLoop(ref playerLoop);

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        static void SetCustomGameLoop(ref PlayerLoopSystem playerLoop)
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

            // after early update 
            // フレームの開始時、すべてのEarlyUpdateの後、FixedUpdate()の前
            PlayerLoopSystem afterEarlyUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaManager),
                updateDelegate = () => afterEarlyUpdateDelegate?.Invoke()
            };
            AddPlayerLoop(afterEarlyUpdate, ref playerLoop, "EarlyUpdate", string.Empty, last: true);

            // after fixed update 
            // FixedUpdate()の後
            PlayerLoopSystem afterFixedUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaManager),
                updateDelegate = () =>
                {
                    afterFixedUpdateDelegate?.Invoke();
                }
            };
            AddPlayerLoop(afterFixedUpdate, ref playerLoop, "FixedUpdate", "ScriptRunBehaviourFixedUpdate");

            // after update 
            // Update()の後
            PlayerLoopSystem afterUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaManager),
                updateDelegate = () =>
                {
                    afterUpdateDelegate?.Invoke();

                    // 実行時の汎用定期更新呼び出し
                    if (Application.isPlaying)
                    {
                        defaultUpdateDelegate?.Invoke();
                    }
                }
            };
            AddPlayerLoop(afterUpdate, ref playerLoop, "Update", "ScriptRunDelayedTasks");

            // after late update 
            // LateUpdate()の後
            PlayerLoopSystem afterLateUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaManager),
                updateDelegate = () => afterLateUpdateDelegate?.Invoke()
            };
            AddPlayerLoop(afterLateUpdate, ref playerLoop, "PreLateUpdate", "ScriptRunBehaviourLateUpdate");

            // after delayed update
            // LateUpdate()後の遅延処理後、yield nullの後
            // LateUpdate()やアセットバンドルロード完了コールバックでコンポーネントをインスタンス化すると、
            // Start()が少し遅れてPostLateUpdateのScriptRunDelayedDynamicFrameRateで呼ばれることになる。
            // 遅延実行時にこの処理が入ると、すでにクロスシミュレーションのジョブが開始されているため、
            // Start()の初期化処理などでNativeリストにアクセスすると例外が発生してしまう。
            // 従って遅延実行時はクロスコンポーネントのStart()が完了するScriptRunDelayedDynamicFrameRate
            // の後にシミュレーションを開始するようにする。
            PlayerLoopSystem afterDelayedUpdate = new PlayerLoopSystem()
            {
                type = typeof(MagicaManager),
                updateDelegate = () => afterDelayedDelegate?.Invoke()
            };
            AddPlayerLoop(afterDelayedUpdate, ref playerLoop, "PostLateUpdate", "ScriptRunDelayedDynamicFrameRate");

            // after rendering
            // レンダリング完了後
            PlayerLoopSystem afterRendering = new PlayerLoopSystem()
            {
                type = typeof(MagicaManager),
                updateDelegate = () => afterRenderingDelegate?.Invoke()
            };
            AddPlayerLoop(afterRendering, ref playerLoop, "PostLateUpdate", "FinishFrameRendering");
        }

        /// <summary>
        /// methodをPlayerLoopの(categoryName:systemName)の次に追加する
        /// </summary>
        /// <param name="method"></param>
        /// <param name="playerLoop"></param>
        /// <param name="categoryName"></param>
        /// <param name="systemName"></param>
        static void AddPlayerLoop(PlayerLoopSystem method, ref PlayerLoopSystem playerLoop, string categoryName, string systemName, bool last = false)
        {
            int sysIndex = Array.FindIndex(playerLoop.subSystemList, (s) => s.type.Name == categoryName);
            PlayerLoopSystem category = playerLoop.subSystemList[sysIndex];
            var systemList = new List<PlayerLoopSystem>(category.subSystemList);

            if (last)
            {
                // 最後に追加
                systemList.Add(method);
            }
            else
            {
                int index = systemList.FindIndex(h => h.type.Name.Contains(systemName));
                systemList.Insert(index + 1, method);
            }

            category.subSystemList = systemList.ToArray();
            playerLoop.subSystemList[sysIndex] = category;
        }

        /// <summary>
        /// MagicaClothのカスタムループが登録されているかチェックする
        /// </summary>
        /// <param name="playerLoop"></param>
        /// <returns></returns>
        static bool CheckRegist(ref PlayerLoopSystem playerLoop)
        {
            var t = typeof(MagicaManager);
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
