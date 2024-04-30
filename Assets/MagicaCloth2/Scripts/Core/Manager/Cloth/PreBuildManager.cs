// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// PreBuildの管理マネージャ
    /// </summary>
    public class PreBuildManager : IManager, IValid
    {
        /// <summary>
        /// 共有ビルドデータの復元データ
        /// </summary>
        internal class ShareDeserializationData : IDisposable
        {
            internal string buildId;
            internal ResultCode result;
            internal int referenceCount;

            internal List<RenderSetupData> renderSetupDataList = new List<RenderSetupData>();
            internal VirtualMesh proxyMesh = null;
            internal List<VirtualMesh> renderMeshList = new List<VirtualMesh>();

            internal DistanceConstraint.ConstraintData distanceConstraintData;
            internal TriangleBendingConstraint.ConstraintData bendingConstraintData;
            internal InertiaConstraint.ConstraintData inertiaConstraintData;

            public void Dispose()
            {
                foreach (var data in renderSetupDataList)
                {
                    if (data != null)
                    {
                        data.isManaged = false;
                        data.Dispose();
                    }
                }
                renderSetupDataList.Clear();

                if (proxyMesh != null)
                {
                    proxyMesh.isManaged = false;
                    proxyMesh.Dispose();
                    proxyMesh = null;
                }

                foreach (var rmesh in renderMeshList)
                {
                    if (rmesh != null)
                    {
                        rmesh.isManaged = false;
                        rmesh.Dispose();
                    }
                }
                renderMeshList.Clear();

                distanceConstraintData = null;
                bendingConstraintData = null;
                inertiaConstraintData = null;

                buildId = string.Empty;
                result.Clear();
                referenceCount = 0;
            }

            public void Deserialize(SharePreBuildData sharePreBuilddata)
            {
                result.SetProcess();

                try
                {
                    // データ検証
                    var validataResult = sharePreBuilddata.DataValidate();
                    if (validataResult.IsFaild())
                    {
                        result.Merge(validataResult);
                        throw new MagicaClothProcessingException();
                    }

                    // Deserialize
                    foreach (var sdata in sharePreBuilddata.renderSetupDataList)
                    {
                        renderSetupDataList.Add(RenderSetupData.ShareDeserialize(sdata));
                    }
                    proxyMesh = VirtualMesh.ShareDeserialize(sharePreBuilddata.proxyMesh);
                    foreach (var sdata in sharePreBuilddata.renderMeshList)
                    {
                        renderMeshList.Add(VirtualMesh.ShareDeserialize(sdata));
                    }
                    distanceConstraintData = sharePreBuilddata.distanceConstraintData;
                    bendingConstraintData = sharePreBuilddata.bendingConstraintData;
                    inertiaConstraintData = sharePreBuilddata.inertiaConstraintData;

                    result.SetSuccess();
                }
                catch (MagicaClothProcessingException)
                {
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    result.SetError(Define.Result.Deserialization_Exception);
                }
            }

            public int RenderMeshCount => renderMeshList?.Count ?? 0;

            public VirtualMeshContainer GetProxyMeshContainer()
            {
                return new VirtualMeshContainer()
                {
                    shareVirtualMesh = proxyMesh,
                    uniqueData = null,
                };
            }

            public VirtualMeshContainer GetRenderMeshContainer(int index)
            {
                if (index >= RenderMeshCount)
                    return null;

                return new VirtualMeshContainer()
                {
                    shareVirtualMesh = renderMeshList[index],
                    uniqueData = null,
                };
            }
        }

        Dictionary<SharePreBuildData, ShareDeserializationData> deserializationDict = new Dictionary<SharePreBuildData, ShareDeserializationData>();
        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
            foreach (var kv in deserializationDict)
            {
                kv.Value.Dispose();
            }
            deserializationDict.Clear();

            isValid = false;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== PreBuild Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"PreBuild Manager. Invalid.");
            }
            else
            {
                int cnt = deserializationDict.Count;
                sb.AppendLine($"Count:{cnt}");

                foreach (var kv in deserializationDict)
                {
                    sb.AppendLine($"[{kv.Key.buildId}] refcnt:{kv.Value.referenceCount}, result:{kv.Value.result.GetResultString()}, proxyMesh:{kv.Value.proxyMesh != null}");
                }
            }

            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }

        //=========================================================================================
        static readonly ProfilerMarker deserializationProfiler = new ProfilerMarker("PreBuild.Deserialization");

        /// <summary>
        /// PreBuildDataをデシリアライズし登録する
        /// すでに登録されていた場合は参照カウンタを加算する
        /// </summary>
        /// <param name="sdata"></param>
        /// <param name="referenceIncrement"></param>
        /// <returns></returns>
        internal ShareDeserializationData RegisterPreBuildData(SharePreBuildData sdata, bool referenceIncrement)
        {
            if (isValid == false)
                return null;
            if (sdata == null)
                return null;

            if (deserializationDict.ContainsKey(sdata) == false)
            {
                deserializationProfiler.Begin();
                //var span = new TimeSpan($"Deserialization [{sdata.buildId}]");

                // new
                var data = new ShareDeserializationData();
                data.buildId = sdata.buildId;

                data.Deserialize(sdata);

                deserializationDict.Add(sdata, data);

                deserializationProfiler.End();
                //span.Log();

                Develop.DebugLog($"RegisterPreBuildData.Deserialize [{sdata.buildId}] F:{Time.frameCount}");
            }

            var ddata = deserializationDict[sdata];

            // reference counter
            if (referenceIncrement)
                ddata.referenceCount++;

            Develop.DebugLog($"RegisterPreBuildData [{sdata.buildId}] C:{ddata.referenceCount} F:{Time.frameCount}");

            return ddata;
        }

        internal ShareDeserializationData GetPreBuildData(SharePreBuildData sdata)
        {
            if (sdata == null)
                return null;

            if (deserializationDict.ContainsKey(sdata))
                return deserializationDict[sdata];

            return null;
        }

        /// <summary>
        /// PreBuildDataのデシリアライズデータを解除する
        /// 参照カウンタが０でも破棄はしない
        /// </summary>
        /// <param name="sdata"></param>
        internal void UnregisterPreBuildData(SharePreBuildData sdata)
        {
            if (isValid == false)
                return;
            if (sdata == null)
                return;

            if (deserializationDict.ContainsKey(sdata))
            {
                var ddata = deserializationDict[sdata];

                // reference counter
                ddata.referenceCount--;

                Develop.DebugLog($"UnregisterPreBuildData [{sdata.buildId}] C:{ddata.referenceCount} F:{Time.frameCount}");
            }
            else
            {
                Develop.DebugLogWarning($"UnregisterPreBuildData not found! [{sdata.buildId}]");
            }
        }

        /// <summary>
        /// 未使用のデシリアライズデータをすべて破棄する
        /// </summary>
        internal void UnloadUnusedData()
        {
            var removeKeys = new List<SharePreBuildData>();

            foreach (var kv in deserializationDict)
            {
                if (kv.Value.referenceCount <= 0)
                    removeKeys.Add(kv.Key);
            }

            foreach (var key in removeKeys)
            {
                var ddata = deserializationDict[key];
                ddata.Dispose();
                deserializationDict.Remove(key);

                Develop.DebugLog($"Unload pre-build deserialization data [{key.buildId}] F:{Time.frameCount}");
            }
            removeKeys.Clear();

            Develop.DebugLog($"Unload pre-build deserialization data count:{deserializationDict.Count} F:{Time.frameCount}");
        }
    }
}
