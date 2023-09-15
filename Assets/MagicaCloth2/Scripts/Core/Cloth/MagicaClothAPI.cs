// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class MagicaCloth
    {
        /// <summary>
        /// シリアライズデータ２の取得
        /// SerializeData2クラスはシステムが利用するパラメータクラスです。
        /// そのためユーザーによる変更は推奨されていません。
        /// 
        /// Acquisition of SerializedData2.
        /// The SerializeData2 class is a parameter class used by the system.
        /// Therefore, user modification is not recommended.
        /// </summary>
        /// <returns></returns>
        public ClothSerializeData2 GetSerializeData2()
        {
            return serializeData2;
        }

        /// <summary>
        /// クロスデータ構築完了後イベント
        /// Event after completion of cloth data construction.
        /// (true = Success, false = Failure)
        /// </summary>
        public Action<bool> OnBuildComplete;


        /// <summary>
        /// 初期化を実行します
        /// すでに初期化済みの場合は何もしません。
        /// perform initialization.
        /// If already initialized, do nothing.
        /// </summary>
        public void Initialize()
        {
            if (Application.isPlaying == false)
                return;

            Process.Init();
        }

        /// <summary>
        /// コンポーネントのStart()で実行される自動ビルドを無効にします
        /// Disable automatic builds that run on the component's Start().
        /// </summary>
        public void DisableAutoBuild()
        {
            if (Application.isPlaying == false)
                return;

            Process.SetState(ClothProcess.State_DisableAutoBuild, true);
        }

        /// <summary>
        /// コンポーネントを構築し実行します
        /// すべてのデータをセットアップしたあとに呼び出す必要があります
        /// build and run the component.
        /// Must be called after setting up all data.
        /// </summary>
        /// <returns>true=start build. false=build failed.</returns>
        public bool BuildAndRun()
        {
            if (Application.isPlaying == false)
                return false;

            DisableAutoBuild();

            if (Process.IsState(ClothProcess.State_Build))
            {
                Develop.LogError($"Already built.:{this.name}");
                return false;
            }

            // initialize generated data.
            if (Process.GenerateInitialization() == false)
                return false;

            // setting by type.
            if (serializeData.clothType == ClothProcess.ClothType.BoneCloth)
            {
                // BoneCloth用のセレクションデータの作成
                // ただしセレクションデータが存在し、かつユーザー定義されている場合は作成しない
                var nowSelection = serializeData2.selectionData;
                if (nowSelection == null || nowSelection.IsValid() == false || nowSelection.IsUserEdit() == false)
                {
                    if (Process.GenerateBoneClothSelection() == false)
                        return false;
                }
            }

            // build and run.
            Process.StartBuild();

            return true;
        }

        /// <summary>
        /// コンポーネントが保持するトランスフォームを置換します。
        /// 置換先のトランスフォーム名をキーとした辞書を渡します。
        /// Replaces a component's transform.
        /// Passes a dictionary keyed by the name of the transform to be replaced.
        /// </summary>
        /// <param name="targetTransformDict">Dictionary keyed by the name of the transform to be replaced.</param>
        public void ReplaceTransform(Dictionary<string, Transform> targetTransformDict)
        {
            // コンポーネントが利用しているすべてのTransformを取得します
            var useTransformSet = new HashSet<Transform>();
            Process.GetUsedTransform(useTransformSet);

            // 置換処理用の辞書を作成
            // key:置換対象トランスフォームのインスタンスID
            // value:入れ替えるトランスフォーム
            var replaceDict = new Dictionary<int, Transform>();
            foreach (var t in useTransformSet)
            {
                if (targetTransformDict.ContainsKey(t.name))
                {
                    replaceDict.Add(t.GetInstanceID(), targetTransformDict[t.name]);
                }
            }

            // 置換する
            Process.ReplaceTransform(replaceDict);
        }


        /// <summary>
        /// パラメータの変更を通知
        /// 実行中にパラメータを変更した場合はこの関数を呼ぶ必要があります
        /// You should call this function if you changed parameters during execution.
        /// </summary>
        public void SetParameterChange()
        {
            if (IsValid())
            {
                Process.DataUpdate();
            }
        }

        /// <summary>
        /// タイムスケールを変更します
        /// Change the time scale.
        /// </summary>
        /// <param name="timeScale">0.0-1.0</param>
        public void SetTimeScale(float timeScale)
        {
            if (IsValid())
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                tdata.timeScale = Mathf.Clamp01(timeScale);
            }
        }

        /// <summary>
        /// タイムスケールを取得します
        /// Get the time scale.
        /// </summary>
        /// <returns></returns>
        public float GetTimeScale()
        {
            if (IsValid())
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                return tdata.timeScale;
            }
            else
                return 1.0f;
        }

        /// <summary>
        /// シミュレーションを初期状態にリセットします
        /// Reset the simulation to its initial state.
        /// </summary>
        /// <param name="keepPose">If true, resume while maintaining posture.</param>
        public void ResetCloth(bool keepPose = false)
        {
            if (IsValid())
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                if (keepPose)
                {
                    // Keep
                    tdata.flag.SetBits(TeamManager.Flag_KeepTeleport, true);
                }
                else
                {
                    // Reset
                    tdata.flag.SetBits(TeamManager.Flag_Reset, true);
                    tdata.flag.SetBits(TeamManager.Flag_TimeReset, true);
                    tdata.flag.SetBits(TeamManager.Flag_CullingKeep, false);
                    Process.SetState(ClothProcess.State_CullingKeep, false);
                    Process.UpdateRendererUse();
                }
            }
        }

        /// <summary>
        /// 慣性の中心座標を取得します
        /// Get the center of inertia position.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCenterPosition()
        {
            if (IsValid())
            {
                ref var cdata = ref MagicaManager.Team.GetCenterDataRef(Process.TeamId);
                return ClothTransform.TransformPoint(cdata.frameLocalPosition);
            }
            else
                return Vector3.zero;
        }

        /// <summary>
        /// 外力を加えます
        /// Add external force.
        /// </summary>
        /// <param name="forceDirection"></param>
        /// <param name="forceVelocity">(m/s)</param>
        /// <param name="fmode"></param>
        public void AddForce(Vector3 forceDirection, float forceVelocity, ClothForceMode fmode = ClothForceMode.VelocityAdd)
        {
            if (IsValid() && forceDirection.magnitude > 0.0f && forceVelocity > 0.0f && fmode != ClothForceMode.None)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(Process.TeamId);
                tdata.forceMode = fmode;
                tdata.impactForce = forceDirection.normalized * forceVelocity;
            }
        }
    }
}
