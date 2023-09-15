// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
#if MC2_DEBUG
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
#endif

namespace MagicaCloth2
{
#if MC2_DEBUG
    public static partial class VirtualMeshEditorUtility
    {
        //=========================================================================================
        static List<Vector3> positionBuffer0 = new List<Vector3>(1024);
        static List<Vector3> positionBuffer1 = new List<Vector3>(1024);
        static List<Vector3> positionBuffer2 = new List<Vector3>(1024);
        static List<int> segmentBuffer0 = new List<int>(2048);
        static List<int> segmentBuffer1 = new List<int>(2048);
        static List<int> segmentBuffer2 = new List<int>(2048);

        //=========================================================================================

        /// <summary>
        /// VirtualMeshのデバッグ表示（編集用）
        /// </summary>
        /// <param name="vmesh"></param>
        /// <param name="debugSettings"></param>
        public static void DrawGizmos(VirtualMesh vmesh, VirtualMeshDebugSettings debugSettings, bool selected, bool useHandles)
        {
            if (debugSettings.enable == false)
                return;
            if (vmesh == null || vmesh.IsSuccess == false || vmesh.VertexCount == 0)
                return;

            var t = vmesh.GetCenterTransform();
            if (t == null)
                return;

            using NativeArray<quaternion> dummyRotations = new NativeArray<quaternion>(0, Allocator.TempJob);
            DrawGizmosInternal(
                useHandles,
                true,
                selected,
                vmesh,
                debugSettings,
                t,
                0,
                vmesh.attributes.GetNativeArray(),
                vmesh.localPositions.GetNativeArray(),
                dummyRotations,
                vmesh.localNormals.GetNativeArray(),
                vmesh.localTangents.GetNativeArray(),
                vmesh.boneWeights.GetNativeArray(),
                vmesh.uv.GetNativeArray()
               );
        }


        /// <summary>
        /// Proxy/Mappingメッシュのデバッグ表示（ランタイム用）
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="vmesh"></param>
        /// <param name="debugSettings"></param>
        public static void DrawRuntimeGizmos(ClothProcess cprocess, bool isMapping, VirtualMesh vmesh, VirtualMeshDebugSettings debugSettings, bool selected, bool useHandles)
        {
            if (cprocess == null || cprocess.IsValid() == false)
                return;
            if (debugSettings.enable == false)
                return;
            if (MagicaManager.Team == null)
                return;
            if (MagicaManager.Team.ContainsTeamData(cprocess.TeamId) == false)
                return;

            if (vmesh == null || vmesh.IsSuccess == false)
                return;

            var t = vmesh.GetCenterTransform();
            if (t == null)
                return;

            // チームデータ
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);

            var tm = MagicaManager.Team;
            var vm = MagicaManager.VMesh;

            // メッシュタイプにより参照するデータを切り替える
            var attributes = vmesh.IsMapping ? vm.mappingAttributes : vm.attributes;
            var positions = vmesh.IsMapping ? vm.mappingPositions : vm.positions;
            var rotations = vmesh.IsMapping ? null : vm.rotations;
            var boneWeights = vmesh.IsMapping ? vm.mappingBoneWeights : vm.boneWeights;
            var uvs = vmesh.IsMapping ? null : vmesh.uv;
            int vstart = vmesh.IsMapping ? tm.mappingDataArray[vmesh.mappingId].mappingCommonChunk.startIndex : tdata.proxyCommonChunk.startIndex;

            using NativeArray<quaternion> dummyRotations = new NativeArray<quaternion>(0, Allocator.TempJob);
            using NativeArray<float2> dummyUvs = new NativeArray<float2>(0, Allocator.TempJob);

            DrawGizmosInternal(
                useHandles,
                isMapping ? true : false,
                selected,
                vmesh,
                debugSettings,
                t,
                vstart,
                attributes.GetNativeArray(),
                positions.GetNativeArray(),
                rotations != null ? rotations.GetNativeArray() : dummyRotations,
                vmesh.localNormals.GetNativeArray(),
                vmesh.localTangents.GetNativeArray(),
                boneWeights.GetNativeArray(),
                uvs != null ? uvs.GetNativeArray() : dummyUvs
                );
        }

        static void DrawGizmosInternal(
            bool useHandles,
            bool isLocal,
            bool selected,
            VirtualMesh vmesh,
            VirtualMeshDebugSettings debugSettings,
            Transform center,
            int vstart,
            NativeArray<VertexAttribute> attributes,
            NativeArray<float3> positions,
            NativeArray<quaternion> rotations,
            NativeArray<float3> normals,
            NativeArray<float3> tangents,
            NativeArray<VirtualMeshBoneWeight> boneWeights,
            NativeArray<float2> uvs
            )
        {
            // 座標空間に合わせる
            if (isLocal)
            {
                Handles.matrix = center.localToWorldMatrix;
                if (useHandles == false)
                    Gizmos.matrix = center.localToWorldMatrix;
            }
            else
            {
                Handles.matrix = Matrix4x4.identity;
                Gizmos.matrix = Matrix4x4.identity;
            }

            // シーンカメラ
            var scam = SceneView.currentDrawingSceneView?.camera;
            if (scam == null)
                return;
            quaternion camRot = scam.transform.rotation;
            quaternion invCamRot = math.inverse(camRot);
            int vcnt = vmesh.VertexCount;

            // 表示スケール調整
            var scl = isLocal ? 1.0f / (center.lossyScale.magnitude / 1.7305f) : 1.0f;
            var drawPointSize = debugSettings.pointSize * scl;
            float drawAxisSize = debugSettings.lineSize * scl;

            bool hasRotation = rotations.IsCreated && rotations.Length > 0;
            bool hasUv = uvs.IsCreated && uvs.Length > 0;
            float colorScale = selected ? 1 : 0.5f;

            // position
            if (debugSettings.position)
            {
                for (int i = 0; i < vcnt; i++)
                {
                    if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                        continue;

                    var pos = positions[vstart + i];
                    var attr = attributes[vstart + i];
                    var col = Color.black;
                    if (attr.IsFixed()) col = Color.red;
                    if (attr.IsMove()) col = Color.green;
                    GizmoUtility.SetColor(col * colorScale, useHandles);

                    GizmoUtility.DrawSphere(pos, drawPointSize, useHandles);
                }
            }

            // rotation axis
            if (debugSettings.axis && hasRotation)
            {
                positionBuffer0.Clear();
                positionBuffer1.Clear();
                positionBuffer2.Clear();
                segmentBuffer0.Clear();
                segmentBuffer1.Clear();
                segmentBuffer2.Clear();

                for (int i = 0; i < vcnt; i++)
                {
                    if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                        continue;

                    var pos = positions[vstart + i];
                    var y = hasRotation ? MathUtility.ToNormal(rotations[vstart + i]) : normals[vstart + i];
                    var z = hasRotation ? MathUtility.ToTangent(rotations[vstart + i]) : tangents[vstart + i];
                    var x = math.cross(y, z);

                    positionBuffer0.Add(pos);
                    positionBuffer0.Add(pos + x * drawAxisSize);
                    segmentBuffer0.Add(i * 2);
                    segmentBuffer0.Add(i * 2 + 1);

                    positionBuffer1.Add(pos);
                    positionBuffer1.Add(pos + y * drawAxisSize);
                    segmentBuffer1.Add(i * 2);
                    segmentBuffer1.Add(i * 2 + 1);

                    positionBuffer2.Add(pos);
                    positionBuffer2.Add(pos + z * drawAxisSize);
                    segmentBuffer2.Add(i * 2);
                    segmentBuffer2.Add(i * 2 + 1);
                }

                // x
                Handles.color = Color.red * colorScale;
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
                // y
                Handles.color = Color.green * colorScale;
                Handles.DrawLines(positionBuffer1.ToArray(), segmentBuffer1.ToArray());
                // z
                Handles.color = Color.blue * colorScale;
                Handles.DrawLines(positionBuffer2.ToArray(), segmentBuffer2.ToArray());
            }

            // bone weight
            if (debugSettings.boneWeight)
            {
                for (int i = 0; i < vcnt; i++)
                {
                    if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                        continue;
                    var pos = positions[vstart + i];
                    var bw = boneWeights[vstart + i];
                    Handles.Label(pos, $"[{bw.boneIndices.x},{bw.boneIndices.y},{bw.boneIndices.z},{bw.boneIndices.w}] w({bw.weights.x:0.###}, {bw.weights.y:0.###}, {bw.weights.z:0.###}, {bw.weights.w:0.###})");
                }
            }
            // number
            else if (debugSettings.indexNumber)
            {
                for (int i = 0; i < vcnt; i++)
                {
                    if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                        continue;
                    var pos = positions[vstart + i];
                    Handles.Label(pos, i.ToString());
                }
            }
            // uv
            else if (debugSettings.uv)
            {
                if (vmesh.IsProxy)
                {
                    // プロキシのみ
                    for (int i = 0; i < vcnt; i++)
                    {
                        if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                            continue;
                        var pos = positions[vstart + i];
                        var uv = vmesh.uv[i];
                        Handles.Label(pos, $"({uv.x:0.####}, {uv.y:0.####})");
                    }
                }
            }
            // depth
            else if (debugSettings.depth)
            {
                if (vmesh.IsProxy)
                {
                    if (vmesh.vertexDepths.IsCreated)
                    {
                        for (int i = 0; i < vcnt; i++)
                        {
                            if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                                continue;
                            var pos = positions[vstart + i];
                            var depth = vmesh.vertexDepths[i];
                            Handles.Label(pos, $"{depth:0.##}");
                        }
                    }
                }
            }
            // root index
            else if (debugSettings.rootIndex)
            {
                if (vmesh.IsProxy)
                {
                    if (vmesh.vertexRootIndices.IsCreated)
                    {
                        for (int i = 0; i < vcnt; i++)
                        {
                            if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                                continue;
                            var pos = positions[vstart + i];
                            int rootIndex = vmesh.vertexRootIndices[i];
                            if (rootIndex >= 0)
                                Handles.Label(pos, $"{rootIndex}");
                        }
                    }
                }
            }
            // parent index
            else if (debugSettings.parentIndex)
            {
                if (vmesh.IsProxy)
                {
                    if (vmesh.vertexParentIndices.IsCreated)
                    {
                        for (int i = 0; i < vcnt; i++)
                        {
                            if (i < debugSettings.vertexMinIndex || i > debugSettings.vertexMaxIndex)
                                continue;
                            var pos = positions[vstart + i];
                            int parentIndex = vmesh.vertexParentIndices[i];
                            //if (parentIndex < 0)
                            Handles.Label(pos, $"{parentIndex}");
                        }
                    }
                }
            }

            // triangls
            if (debugSettings.triangle)
            {
                positionBuffer0.Clear();
                segmentBuffer0.Clear();

                switch (vmesh.meshType)
                {
                    case VirtualMesh.MeshType.NormalMesh:
                        GizmoUtility.SetColor(Color.magenta * colorScale, useHandles);
                        break;
                    case VirtualMesh.MeshType.NormalBoneMesh:
                        GizmoUtility.SetColor(Color.magenta * colorScale, useHandles);
                        break;
                    case VirtualMesh.MeshType.ProxyMesh:
                        GizmoUtility.SetColor(new Color(0.8666f, 0.627f, 0.8666f) * colorScale, useHandles);
                        break;
                    case VirtualMesh.MeshType.ProxyBoneMesh:
                        GizmoUtility.SetColor(new Color(0.8666f, 0.627f, 0.8666f) * colorScale, useHandles);
                        break;
                    case VirtualMesh.MeshType.Mapping:
                        GizmoUtility.SetColor(new Color(0.851f, 0.644f, 0.125f) * colorScale, useHandles);
                        break;
                }
                for (int i = 0; i < vmesh.TriangleCount; i++)
                {
                    if (i < debugSettings.triangleMinIndex || i > debugSettings.triangleMaxIndex)
                        continue;
                    int3 tri = vmesh.triangles[i];
                    var pos1 = positions[vstart + tri.x];
                    var pos2 = positions[vstart + tri.y];
                    var pos3 = positions[vstart + tri.z];
                    int index = positionBuffer0.Count;
                    positionBuffer0.Add(pos1);
                    positionBuffer0.Add(pos2);
                    positionBuffer0.Add(pos3);
                    segmentBuffer0.Add(index);
                    segmentBuffer0.Add(index + 1);
                    segmentBuffer0.Add(index + 1);
                    segmentBuffer0.Add(index + 2);
                    segmentBuffer0.Add(index + 2);
                    segmentBuffer0.Add(index);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
            }

            // line
            if (debugSettings.line)
            {
                positionBuffer0.Clear();
                segmentBuffer0.Clear();

                GizmoUtility.SetColor(Color.cyan * colorScale, useHandles);
                for (int i = 0; i < vmesh.LineCount; i++)
                {
                    int2 line = vmesh.lines[i];
                    var pos1 = positions[vstart + line.x];
                    var pos2 = positions[vstart + line.y];
                    positionBuffer0.Add(pos1);
                    positionBuffer0.Add(pos2);
                    segmentBuffer0.Add(i * 2);
                    segmentBuffer0.Add(i * 2 + 1);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
            }

            // edge number
            if (debugSettings.edgeNumber)
            {
                int ecnt = vmesh.EdgeCount;
                for (int i = 0; i < ecnt; i++)
                {
                    if (i < debugSettings.edgeMinIndex || i > debugSettings.edgeMaxIndex)
                        continue;
                    int2 edge = vmesh.edges[i];
                    var pos1 = positions[vstart + edge.x];
                    var pos2 = positions[vstart + edge.y];
                    var c = (pos1 + pos2) * 0.5f;
                    Handles.Label(c, i.ToString());
                }
            }

            // triangle normal
            if (debugSettings.triangleNormal || debugSettings.triangleNumber || debugSettings.triangleTangent)
            {
                positionBuffer0.Clear();
                segmentBuffer0.Clear();
                positionBuffer1.Clear();
                segmentBuffer1.Clear();

                Color colF = Color.yellow;
                Color colB = new Color(1.0f, 0.3f, 0.3f, 1.0f);
                for (int i = 0; i < vmesh.TriangleCount; i++)
                {
                    if (i < debugSettings.triangleMinIndex || i > debugSettings.triangleMaxIndex)
                        continue;
                    int3 tri = vmesh.triangles[i];
                    var pos1 = positions[vstart + tri.x];
                    var pos2 = positions[vstart + tri.y];
                    var pos3 = positions[vstart + tri.z];
                    var n = MathUtility.TriangleNormal(pos1, pos2, pos3);
                    var c = MathUtility.TriangleCenter(pos1, pos2, pos3);
                    if (debugSettings.triangleNormal)
                    {
                        //var dir = math.mul(invCamRot, n);
                        //GizmoUtility.SetColor((dir.z < 0.0f ? colF : colB) * colorScale, useHandles);
                        //GizmoUtility.DrawLine(c, c + n * drawAxisSize, useHandles);
                        positionBuffer0.Add(c);
                        positionBuffer0.Add(c + n * drawAxisSize);
                        segmentBuffer0.Add(i * 2);
                        segmentBuffer0.Add(i * 2 + 1);
                    }
                    if (debugSettings.triangleTangent && hasUv)
                    {
                        var uv1 = uvs[vstart + tri.x];
                        var uv2 = uvs[vstart + tri.y];
                        var uv3 = uvs[vstart + tri.z];
                        var tn = MathUtility.TriangleTangent(pos1, pos2, pos3, uv1, uv2, uv3);
                        positionBuffer1.Add(c);
                        positionBuffer1.Add(c + tn * drawAxisSize);
                        segmentBuffer1.Add(i * 2);
                        segmentBuffer1.Add(i * 2 + 1);
                    }
                    if (debugSettings.triangleNumber)
                        Handles.Label(c, i.ToString());
                }

                if (debugSettings.triangleNormal)
                {
                    GizmoUtility.SetColor(colF * colorScale, useHandles);
                    Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
                }
                if (debugSettings.triangleTangent && hasUv)
                {
                    GizmoUtility.SetColor(colB * colorScale, useHandles);
                    Handles.DrawLines(positionBuffer1.ToArray(), segmentBuffer1.ToArray());
                }
            }

            // base line
            if (vmesh.IsProxy)
            {
                // プロキシのみ
                if (debugSettings.baseLine && vmesh.BaseLineCount > 0)
                {
                    GizmoUtility.SetColor(new Color(1.0f, 0.27f, 0.0f) * colorScale, useHandles);
                    positionBuffer0.Clear();
                    segmentBuffer0.Clear();

                    for (int i = 0; i < vcnt; i++)
                    {
                        int pindex = vmesh.vertexParentIndices[i];
                        if (pindex >= 0)
                        {
                            var pos = positions[vstart + i];
                            var ppos = positions[vstart + pindex];
                            //GizmoUtility.DrawLine(pos, ppos, useHandles);
                            positionBuffer0.Add(pos);
                            positionBuffer0.Add(ppos);
                            segmentBuffer0.Add(i * 2);
                            segmentBuffer0.Add(i * 2 + 1);
                        }
                    }
                    Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
                }
            }

            // 空間を戻す
            Handles.matrix = Matrix4x4.identity;
            if (useHandles == false)
                Gizmos.matrix = Matrix4x4.identity;

            // bone name
            if (debugSettings.boneName)
            {
                int cnt = vmesh.transformData.Count;
                for (int i = 0; i < cnt; i++)
                {
                    var t = vmesh.transformData.GetTransformFromIndex(i);
                    if (t)
                    {
                        var pos = t.position;
                        Handles.Label(pos, $"[{i}] {t.name}");
                    }
                }
            }
        }
    }
#endif // MC_DEBUG
}
