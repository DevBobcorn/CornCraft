// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildの保存データ
    /// </summary>
    [System.Serializable]
    public class PreBuildSerializeData : ITransform
    {
        /// <summary>
        /// 有効状態
        /// Valid state.
        /// </summary>
        public bool enabled = false;

        /// <summary>
        /// ビルド識別ID
        /// </summary>
        public string buildId;

        /// <summary>
        /// ビルドデータの共有部分
        /// このデータは複数のインスタンスで共有されるためScriptableObjectとして外部アセットとして保存される
        /// </summary>
        public PreBuildScriptableObject preBuildScriptableObject = null;

        /// <summary>
        /// ビルドデータの固有部分
        /// このデータはインスタンスごとに固有となる
        /// </summary>
        public UniquePreBuildData uniquePreBuildData;

        //=========================================================================================
        /// <summary>
        /// PreBuild利用の有無
        /// </summary>
        /// <returns></returns>
        public bool UsePreBuild() => enabled;

        /// <summary>
        /// PreBuildデータの検証
        /// </summary>
        /// <returns></returns>
        public ResultCode DataValidate()
        {
            if (uniquePreBuildData == null)
                return new ResultCode(Define.Result.PreBuildData_Empty);

            var preBuildData = GetSharePreBuildData();
            if (preBuildData == null)
                return new ResultCode(Define.Result.PreBuildData_Empty);

            var result = preBuildData.DataValidate();
            if (result.IsFaild())
                return result;

            result = uniquePreBuildData.DataValidate();
            if (result.IsFaild())
                return result;

            return ResultCode.Success;
        }

        public SharePreBuildData GetSharePreBuildData()
        {
            if (preBuildScriptableObject == null)
                return null;

            if (string.IsNullOrEmpty(buildId))
                return null;

            return preBuildScriptableObject.GetPreBuildData(buildId); ;
        }

        /// <summary>
        /// ビルドIDを生成する(英数字８文字)
        /// </summary>
        /// <returns></returns>
        public static string GenerateBuildID()
        {
            Guid g = Guid.NewGuid();
            return g.ToString().Substring(0, 8);
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            uniquePreBuildData.GetUsedTransform(transformSet);
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            uniquePreBuildData.ReplaceTransform(replaceDict);
        }
    }
}
