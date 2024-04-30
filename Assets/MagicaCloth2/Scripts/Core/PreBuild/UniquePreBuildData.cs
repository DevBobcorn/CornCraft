// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildデータの固有部分
    /// </summary>
    [System.Serializable]
    public class UniquePreBuildData : ITransform
    {
        public int version;
        public ResultCode buildResult;

        public List<RenderSetupData.UniqueSerializationData> renderSetupDataList = new List<RenderSetupData.UniqueSerializationData>();

        public VirtualMesh.UniqueSerializationData proxyMesh;
        public List<VirtualMesh.UniqueSerializationData> renderMeshList = new List<VirtualMesh.UniqueSerializationData>();

        //=========================================================================================
        public ResultCode DataValidate()
        {
            if (version != Define.System.LatestPreBuildVersion)
                return new ResultCode(Define.Result.PreBuildData_VersionMismatch);

            if (buildResult.IsFaild())
                return buildResult;

            return ResultCode.Success;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            renderSetupDataList.ForEach(x => x?.GetUsedTransform(transformSet));
            proxyMesh?.GetUsedTransform(transformSet);
            renderMeshList.ForEach(x => x?.GetUsedTransform(transformSet));
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            renderSetupDataList.ForEach(x => x?.ReplaceTransform(replaceDict));
            proxyMesh?.ReplaceTransform(replaceDict);
            renderMeshList.ForEach(x => x?.ReplaceTransform(replaceDict));
        }
    }
}
