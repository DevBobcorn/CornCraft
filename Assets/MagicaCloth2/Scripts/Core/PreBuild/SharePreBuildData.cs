// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildデータの共有部分
    /// </summary>
    [System.Serializable]
    public class SharePreBuildData
    {
        public int version;
        public string buildId;
        public ResultCode buildResult;
        public Vector3 buildScale;

        public List<RenderSetupData.ShareSerializationData> renderSetupDataList = new List<RenderSetupData.ShareSerializationData>();

        public VirtualMesh.ShareSerializationData proxyMesh;
        public List<VirtualMesh.ShareSerializationData> renderMeshList = new List<VirtualMesh.ShareSerializationData>();

        public DistanceConstraint.ConstraintData distanceConstraintData;
        public TriangleBendingConstraint.ConstraintData bendingConstraintData;
        public InertiaConstraint.ConstraintData inertiaConstraintData;

        //=========================================================================================
        public ResultCode DataValidate()
        {
            if (version != Define.System.LatestPreBuildVersion)
                return new ResultCode(Define.Result.PreBuildData_VersionMismatch);

            if (buildScale.x < Define.System.Epsilon)
                return new ResultCode(Define.Result.PreBuildData_InvalidScale);

            if (buildResult.IsFaild())
                return buildResult;

            return ResultCode.Success;
        }

        public bool CheckBuildId(string buildId)
        {
            if (string.IsNullOrEmpty(buildId))
                return false;

            return this.buildId == buildId;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(1024);

            sb.AppendLine("<<<<< PreBuildData >>>>>");
            sb.AppendLine($"Version:{version}");
            sb.AppendLine($"BuildID:{buildId}");
            sb.AppendLine($"BuildResult:{buildResult.GetResultString()}");
            sb.AppendLine($"BuildScale:{buildScale}");
            sb.AppendLine(proxyMesh.ToString());
            sb.AppendLine($"renderMeshList:{renderMeshList.Count}");
            sb.AppendLine($"renderSetupDataList:{renderSetupDataList.Count}");

            return sb.ToString();
        }
    }
}
