// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;


namespace MagicaCloth2
{
    /// <summary>
    /// クロスコンポーネントのギズモ表示
    /// </summary>
    public static class ClothEditorUtility
    {
        public static readonly Color MovePointColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        //public static readonly Color MovePointColor = Color.white;
        public static readonly Color FixedPointColor = new Color(1.0f, 0.0f, 0.0f, 0.8f);
        public static readonly Color InvalidPointColor = new Color(0.0f, 0.0f, 0.0f, 0.8f);
        public static readonly Color AngleLimitConeColor = new Color(0.0f, 0.349f, 0.725f, 0.325f);
        public static readonly Color AngleLimitWireColor = new Color(0.0f, 0.843f, 1.0f, 0.313f);
        public static readonly Color BaseTriangleColor = new Color(204 / 255f, 153 / 255f, 255 / 255f);
        public static readonly Color BaseLineColor = new Color(153 / 255f, 204 / 255f, 255 / 255f);
        public static readonly Color TriangleColor = new Color(1.0f, 0.0f, 0.8f, 1f);
        public static readonly Color LineColor = Color.cyan;
        public static readonly Color SkininngLine = Color.yellow;

        /// <summary>
        /// クロスペイント時のギズモ表示設定
        /// </summary>
        internal static readonly ClothDebugSettings PaintSettings = new ClothDebugSettings()
        {
            enable = true,
            position = false,
            collider = false,
            //basicShape = true,
            animatedShape = true,
        };


        //=========================================================================================
        static List<Vector3> positionBuffer0 = new List<Vector3>(1024);
        static List<Vector3> positionBuffer1 = new List<Vector3>(1024);
        static List<Vector3> positionBuffer2 = new List<Vector3>(1024);
        static List<int> segmentBuffer0 = new List<int>(2048);
        static List<int> segmentBuffer1 = new List<int>(2048);
        static List<int> segmentBuffer2 = new List<int>(2048);

        //=========================================================================================
        /// <summary>
        /// 編集時のクロスデータの表示（すべてHandlesクラスで描画）
        /// </summary>
        /// <param name="editMesh"></param>
        /// <param name="drawSettings"></param>
        public static void DrawClothEditor(VirtualMesh editMesh, ClothDebugSettings drawSettings, ClothSerializeData serializeData, bool selected, bool direction, bool paint)
        {
            if (editMesh == null || editMesh.IsSuccess == false)
                return;

            if (drawSettings.enable == false || serializeData == null)
                return;

            // シーンカメラ
            var scam = SceneView.currentDrawingSceneView?.camera;
            if (scam == null)
                return;
            var crot = scam.transform.rotation;

            // 座標空間に合わせる
            var t = editMesh.GetCenterTransform();
            if (t == null)
                return;

            Handles.zTest = drawSettings.ztest ? UnityEngine.Rendering.CompareFunction.LessEqual : UnityEngine.Rendering.CompareFunction.Always;
            Handles.lighting = true;
            Handles.matrix = t.localToWorldMatrix;

            // シーンカメラ回転をローカル空間へ変換する
            crot = Quaternion.Inverse(t.rotation) * crot;

            int vcnt = editMesh.VertexCount;
            float worldToLocalScale = 1.0f / t.lossyScale.x;
            float drawPointSize = drawSettings.GetPointSize() * worldToLocalScale;
            float drawLineSize = drawSettings.GetLineSize() * worldToLocalScale;
            float3 cdir = crot * Vector3.forward; // ローカルカメラ方向

            float colorScale = selected ? 1.0f : 0.5f;

            // position
            if (drawSettings.position || drawSettings.animatedPosition)
            {
                using var pointList = new NativeList<ClothPainter.Point>(vcnt, Allocator.TempJob);

                for (int i = 0; i < vcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    var point = new ClothPainter.Point()
                    {
                        vindex = i,
                    };
                    pointList.AddNoResize(point);
                }
                if (pointList.Length > 0)
                {
                    // カメラ距離でソート
                    ClothPainter.CalcPointCameraDistance(editMesh.localPositions.GetNativeArray(), 0, t.localToWorldMatrix, scam.transform.position, pointList);

                    // 描画
                    int cnt = pointList.Length;
                    for (int i = 0; i < cnt; i++)
                    {
                        var p = pointList[i];

                        var attr = editMesh.attributes[p.vindex];
                        var col = InvalidPointColor;
                        if (attr.IsFixed()) col = FixedPointColor;
                        else if (attr.IsMove()) col = MovePointColor;
                        GizmoUtility.SetColor(col * colorScale, true);

                        // radius
                        float depth = editMesh.vertexDepths[p.vindex];
                        float radius = serializeData.radius.Evaluate(depth);
                        radius *= worldToLocalScale;
                        var pos = editMesh.localPositions[p.vindex];
                        GizmoUtility.DrawSphere(pos, drawSettings.CheckRadiusDrawing() ? radius : drawPointSize, true);
                    }
                }
            }

            // animated shape / shape
            if (drawSettings.animatedShape || drawSettings.shape)
            {
                positionBuffer0.Clear();
                segmentBuffer0.Clear();
                for (int i = 0; i < vcnt; i++)
                {
                    positionBuffer0.Add(editMesh.localPositions[i]);
                }

                GizmoUtility.SetColor(drawSettings.animatedShape ? BaseTriangleColor : TriangleColor, true);
                int tcnt = editMesh.TriangleCount;
                for (int i = 0; i < tcnt; i++)
                {
                    if (drawSettings.CheckTriangleDrawing(i) == false)
                        continue;

                    int3 tri = editMesh.triangles[i];

                    // attribute
                    //if (drawSettings.animatedShape == false)
                    //{
                    //    var attr0 = editMesh.attributes[tri.x];
                    //    var attr1 = editMesh.attributes[tri.y];
                    //    var attr2 = editMesh.attributes[tri.y];
                    //    if (attr0.IsInvalid() || attr1.IsInvalid() || attr2.IsInvalid())
                    //        continue;
                    //}

                    // 方向性
                    if (direction)
                    {
                        var tn = MathUtility.TriangleNormal(editMesh.localPositions[tri.x], editMesh.localPositions[tri.y], editMesh.localPositions[tri.z]);
                        if (math.dot(tn, cdir) >= 0.0f)
                            continue;
                    }

                    segmentBuffer0.Add(tri.x);
                    segmentBuffer0.Add(tri.y);
                    segmentBuffer0.Add(tri.y);
                    segmentBuffer0.Add(tri.z);
                    segmentBuffer0.Add(tri.z);
                    segmentBuffer0.Add(tri.x);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());

                segmentBuffer0.Clear();
                GizmoUtility.SetColor((drawSettings.animatedShape ? BaseLineColor : LineColor) * colorScale, true);
                int lcnt = editMesh.LineCount;
                for (int i = 0; i < lcnt; i++)
                {
                    int2 line = editMesh.lines[i];

                    // attribute
                    //if (drawSettings.animatedShape == false)
                    //{
                    //    var attr0 = editMesh.attributes[line.x];
                    //    var attr1 = editMesh.attributes[line.y];
                    //    if (attr0.IsInvalid() || attr1.IsInvalid())
                    //        continue;
                    //}

                    segmentBuffer0.Add(line.x);
                    segmentBuffer0.Add(line.y);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
            }

            // base line
            if (drawSettings.baseLine && editMesh.BaseLineCount > 0)
            {
                GizmoUtility.SetColor(new Color(1.0f, 0.27f, 0.0f) * colorScale, true);
                int bcnt = editMesh.BaseLineCount;
                for (int i = 0; i < bcnt; i++)
                {
                    int dstart = editMesh.baseLineStartDataIndices[i];
                    int dcnt = editMesh.baseLineDataCounts[i];
                    for (int j = 1; j < dcnt; j++)
                    {
                        int vindex = editMesh.baseLineData[dstart + j];
                        int pindex = editMesh.vertexParentIndices[vindex];
                        if (pindex >= 0)
                        {
                            var pos = editMesh.localPositions[vindex];
                            var ppos = editMesh.localPositions[pindex];
                            GizmoUtility.DrawLine(pos, ppos, true);
                        }
                    }
                }
            }

            // axis
            if (drawSettings.axis != ClothDebugSettings.DebugAxis.None || drawSettings.animatedAxis != ClothDebugSettings.DebugAxis.None)
            {
                positionBuffer0.Clear();
                positionBuffer1.Clear();
                positionBuffer2.Clear();
                segmentBuffer0.Clear();
                segmentBuffer1.Clear();
                segmentBuffer2.Clear();

                for (int i = 0; i < vcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    // attribute
                    //if (drawSettings.animatedAxis == ClothDebugSettings.DebugAxis.None)
                    //{
                    //    var attr = editMesh.attributes[i];
                    //    if (attr.IsInvalid())
                    //        continue;
                    //}

                    var pos = editMesh.localPositions[i];
                    var rot = MathUtility.ToRotation(editMesh.localNormals[i], editMesh.localTangents[i]);

                    // 基準軸の変換用ローカル回転
                    float len = drawLineSize;

                    // 基準軸
                    bool xaxis = false, yaxis = false, zaxis = false;
                    if (drawSettings.axis == ClothDebugSettings.DebugAxis.Normal || drawSettings.animatedAxis == ClothDebugSettings.DebugAxis.Normal)
                    {
                        switch (serializeData.normalAxis)
                        {
                            case ClothNormalAxis.Right:
                            case ClothNormalAxis.InverseRight:
                                xaxis = true;
                                break;
                            case ClothNormalAxis.Up:
                            case ClothNormalAxis.InverseUp:
                                yaxis = true;
                                break;
                            case ClothNormalAxis.Forward:
                            case ClothNormalAxis.InverseForward:
                                zaxis = true;
                                break;
                        }
                    }
                    if (drawSettings.axis == ClothDebugSettings.DebugAxis.All || drawSettings.animatedAxis == ClothDebugSettings.DebugAxis.All)
                    {
                        xaxis = yaxis = zaxis = true;
                    }

                    if (xaxis)
                    {
                        int index = positionBuffer0.Count;
                        positionBuffer0.Add(pos);
                        positionBuffer0.Add(pos + MathUtility.ToBinormal(rot) * len);
                        segmentBuffer0.Add(index);
                        segmentBuffer0.Add(index + 1);
                    }
                    if (yaxis)
                    {
                        int index = positionBuffer1.Count;
                        positionBuffer1.Add(pos);
                        positionBuffer1.Add(pos + MathUtility.ToNormal(rot) * len);
                        segmentBuffer1.Add(index);
                        segmentBuffer1.Add(index + 1);
                    }
                    if (zaxis)
                    {
                        int index = positionBuffer2.Count;
                        positionBuffer2.Add(pos);
                        positionBuffer2.Add(pos + MathUtility.ToTangent(rot) * len);
                        segmentBuffer2.Add(index);
                        segmentBuffer2.Add(index + 1);
                    }
                }

                if (positionBuffer0.Count > 0)
                {
                    Handles.color = Color.red * colorScale;
                    Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
                }
                if (positionBuffer1.Count > 0)
                {
                    Handles.color = Color.green * colorScale;
                    Handles.DrawLines(positionBuffer1.ToArray(), segmentBuffer1.ToArray());
                }
                if (positionBuffer2.Count > 0)
                {
                    Handles.color = Color.blue * colorScale;
                    Handles.DrawLines(positionBuffer2.ToArray(), segmentBuffer2.ToArray());
                }
            }

            // depth
            if (drawSettings.depth)
            {
                for (int i = 0; i < vcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    var pos = editMesh.localPositions[i];
                    var depth = editMesh.vertexDepths[i];
                    if (depth > 0.0f)
                        Handles.Label(pos, $"{depth:0.##}");
                }
            }
#if MC2_DEBUG
            // number
            else if (drawSettings.particleNumber || drawSettings.localNumber)
            {
                for (int i = 0; i < vcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    var pos = editMesh.localPositions[i];
                    Handles.Label(pos, i.ToString());
                }
            }
            // attribute
            else if (drawSettings.attribute)
            {
                for (int i = 0; i < vcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    var pos = editMesh.localPositions[i];
                    var attr = editMesh.attributes[i];
                    Handles.Label(pos, $"0x{attr.Value:X2}");
                }
            }
            // triangle number
            else if (drawSettings.triangleNumber)
            {
                int tcnt = editMesh.TriangleCount;
                for (int i = 0; i < tcnt; i++)
                {
                    if (drawSettings.CheckTriangleDrawing(i) == false)
                        continue;

                    int3 tri = editMesh.triangles[i];
                    var pos1 = editMesh.localPositions[tri.x];
                    var pos2 = editMesh.localPositions[tri.y];
                    var pos3 = editMesh.localPositions[tri.z];
                    var cen = MathUtility.TriangleCenter(pos1, pos2, pos3);
                    Handles.Label(cen, i.ToString());
                }
            }
#endif

            // inertia center
            if (paint == false && editMesh.CenterFixedPointCount > 0)
            {
                float3 pos = 0;
                int ccnt = editMesh.CenterFixedPointCount;
                for (int i = 0; i < ccnt; i++)
                {
                    pos += editMesh.localPositions[editMesh.centerFixedList[i]];
                }
                pos /= ccnt;
                GizmoUtility.SetColor(Color.magenta * colorScale, true);
                GizmoUtility.DrawSphere(pos, drawSettings.GetInertiaCenterRadius() * worldToLocalScale, true);
            }

            Handles.matrix = Matrix4x4.identity;

            // custom skinning bone
            if (paint == false)
            {
                Handles.color = SkininngLine * colorScale;
                var boneList = serializeData.customSkinningSetting.skinningBones;
                for (int i = 0; i < boneList.Count - 1; i++)
                {
                    var bone1 = boneList[i];
                    if (bone1 == null)
                        continue;
                    for (int j = i + 1; j < boneList.Count; j++)
                    {
                        var bone2 = boneList[j];
                        if (bone2 == null)
                            continue;

                        if (bone1.parent == bone2 || bone2.parent == bone1)
                        {
                            Handles.DrawLine(bone1.position, bone2.position);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 実行時のクロスデータの表示（すべてHandlesクラスで描画）
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="drawSettings"></param>
        public static void DrawClothRuntime(ClothProcess cprocess, ClothDebugSettings drawSettings, bool selected)
        {
            if (cprocess == null || cprocess.IsValid() == false)
                return;
            if (drawSettings.enable == false)
                return;
            if (MagicaManager.Team == null)
                return;
            if (MagicaManager.Team.ContainsTeamData(cprocess.TeamId) == false)
                return;

            // プロキシメッシュ
            var proxyMesh = cprocess.ProxyMesh;
            if (proxyMesh == null || proxyMesh.IsSuccess == false)
                return;

            // シーンカメラ
            var scam = SceneView.currentDrawingSceneView?.camera;
            if (scam == null)
                return;
            var crot = scam.transform.rotation;

            // チームデータ
            var tdata = MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);
            var cdata = MagicaManager.Team.centerDataArray[cprocess.TeamId];
            int pcnt = tdata.ParticleCount;
            int pstart = tdata.particleChunk.startIndex;
            int vstart = tdata.proxyCommonChunk.startIndex;
            float drawPointSize = drawSettings.GetPointSize();
            float drawLineSize = drawSettings.GetLineSize();

            // 座標空間に合わせる
            var t = cprocess.cloth.ClothTransform;
            if (t == null)
                return;

            Handles.zTest = drawSettings.ztest ? UnityEngine.Rendering.CompareFunction.LessEqual : UnityEngine.Rendering.CompareFunction.Always;
            Handles.matrix = Matrix4x4.identity;
            float colorScale = selected ? 1 : 0.5f;

            var sim = MagicaManager.Simulation;
            var vm = MagicaManager.VMesh;

            // 座標参照先
            var positionArray = drawSettings.IsReferOldPos() ? sim.oldPosArray : sim.dispPosArray;

            // position
            if (drawSettings.position)
            {
                using var pointList = new NativeList<ClothPainter.Point>(pcnt, Allocator.TempJob);

                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    var point = new ClothPainter.Point()
                    {
                        vindex = i,
                    };
                    pointList.AddNoResize(point);
                }
                if (pointList.Length > 0)
                {
                    // カメラ距離でソート
                    ClothPainter.CalcPointCameraDistance(positionArray.GetNativeArray(), pstart, float4x4.identity, scam.transform.position, pointList);

                    // 描画
                    int cnt = pointList.Length;
                    for (int i = 0; i < cnt; i++)
                    {
                        var p = pointList[i];

                        int pindex = pstart + p.vindex;
                        int cvindex = tdata.proxyCommonChunk.startIndex + p.vindex;

                        var attr = vm.attributes[cvindex];
                        var col = InvalidPointColor;
                        if (attr.IsFixed()) col = FixedPointColor;
                        else if (attr.IsMove()) col = MovePointColor;
                        GizmoUtility.SetColor(col * colorScale, true);

                        // radius
                        float depth = vm.vertexDepths[cvindex];
                        float radius = cprocess.parameters.radiusCurveData.EvaluateCurve(depth);
                        radius *= tdata.scaleRatio;
                        var pos = positionArray[pindex];
                        GizmoUtility.DrawSphere(pos, drawSettings.CheckRadiusDrawing() ? radius : drawPointSize, true);
                    }
                }
            }

            // rotation axis / area
            if (drawSettings.axis != ClothDebugSettings.DebugAxis.None)
            {
                positionBuffer0.Clear();
                positionBuffer1.Clear();
                positionBuffer2.Clear();
                segmentBuffer0.Clear();
                segmentBuffer1.Clear();
                segmentBuffer2.Clear();

                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = pstart + i;
                    int cvindex = tdata.proxyCommonChunk.startIndex + i;

                    // attribute
                    //if (vm.attributes[cvindex].IsInvalid())
                    //    continue;

                    var pos = positionArray[pindex];
                    var rot = vm.rotations[cvindex]; // 計算済みの前フレームの最終姿勢

                    float len = drawLineSize;

                    // 基準軸
                    bool xaxis = false, yaxis = false, zaxis = false;
                    if (drawSettings.axis == ClothDebugSettings.DebugAxis.Normal)
                    {
                        switch (cprocess.parameters.normalAxis)
                        {
                            case ClothNormalAxis.Right:
                            case ClothNormalAxis.InverseRight:
                                xaxis = true;
                                break;
                            case ClothNormalAxis.Up:
                            case ClothNormalAxis.InverseUp:
                                yaxis = true;
                                break;
                            case ClothNormalAxis.Forward:
                            case ClothNormalAxis.InverseForward:
                                zaxis = true;
                                break;
                        }
                    }
                    if (drawSettings.axis == ClothDebugSettings.DebugAxis.All)
                    {
                        xaxis = yaxis = zaxis = true;
                    }

                    if (xaxis)
                    {
                        int index = positionBuffer0.Count;
                        positionBuffer0.Add(pos);
                        positionBuffer0.Add(pos + MathUtility.ToBinormal(rot) * len);
                        segmentBuffer0.Add(index);
                        segmentBuffer0.Add(index + 1);
                    }
                    if (yaxis)
                    {
                        int index = positionBuffer1.Count;
                        positionBuffer1.Add(pos);
                        positionBuffer1.Add(pos + MathUtility.ToNormal(rot) * len);
                        segmentBuffer1.Add(index);
                        segmentBuffer1.Add(index + 1);
                    }
                    if (zaxis)
                    {
                        int index = positionBuffer2.Count;
                        positionBuffer2.Add(pos);
                        positionBuffer2.Add(pos + MathUtility.ToTangent(rot) * len);
                        segmentBuffer2.Add(index);
                        segmentBuffer2.Add(index + 1);
                    }
                }

                if (positionBuffer0.Count > 0)
                {
                    Handles.color = Color.red * colorScale;
                    Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
                }
                if (positionBuffer1.Count > 0)
                {
                    Handles.color = Color.green * colorScale;
                    Handles.DrawLines(positionBuffer1.ToArray(), segmentBuffer1.ToArray());
                }
                if (positionBuffer2.Count > 0)
                {
                    Handles.color = Color.blue * colorScale;
                    Handles.DrawLines(positionBuffer2.ToArray(), segmentBuffer2.ToArray());
                }
            }

#if MC2_DEBUG
            // collision normal
            if (drawSettings.collisionNormal)
            {
                GizmoUtility.SetColor(Color.yellow * colorScale, true);
                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = pstart + i;
                    var pos = positionArray[pindex];
                    float3 cn = sim.collisionNormalArray[pindex];
                    if (math.lengthsq(cn) > 1e-06f)
                    {
                        GizmoUtility.DrawLine(pos, pos + cn * drawPointSize, true);
                    }
                }
            }
#endif

            // animated shape
            if (drawSettings.animatedShape)
            {
                positionBuffer0.Clear();
                segmentBuffer0.Clear();
                for (int i = 0; i < pcnt; i++)
                {
                    positionBuffer0.Add(sim.basePosArray[pstart + i]);
                    //positionBuffer0.Add(sim.stepBasicPositionBuffer[pstart + i]);
                }

                GizmoUtility.SetColor(BaseTriangleColor, true);
                int tcnt = tdata.proxyTriangleChunk.dataLength;
                for (int i = 0; i < tcnt; i++)
                {
                    if (drawSettings.CheckTriangleDrawing(i) == false)
                        continue;

                    int tindex = tdata.proxyTriangleChunk.startIndex + i;
                    int3 tri = vm.triangles[tindex];

                    //int3 vtri = tri + tdata.proxyCommonChunk.startIndex;
                    //if (vm.attributes[vtri.x].IsInvalid() || vm.attributes[vtri.y].IsInvalid() || vm.attributes[vtri.z].IsInvalid())
                    //    continue;

                    segmentBuffer0.Add(tri.x);
                    segmentBuffer0.Add(tri.y);
                    segmentBuffer0.Add(tri.y);
                    segmentBuffer0.Add(tri.z);
                    segmentBuffer0.Add(tri.z);
                    segmentBuffer0.Add(tri.x);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());

                segmentBuffer0.Clear();
                GizmoUtility.SetColor(BaseLineColor * colorScale, true);
                int lcnt = proxyMesh.LineCount;
                for (int i = 0; i < lcnt; i++)
                {
                    int2 line = proxyMesh.lines[i];
                    segmentBuffer0.Add(line.x);
                    segmentBuffer0.Add(line.y);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
            }

            // shape
            if (drawSettings.shape)
            {
                positionBuffer0.Clear();
                segmentBuffer0.Clear();
                for (int i = 0; i < pcnt; i++)
                {
                    positionBuffer0.Add(positionArray[pstart + i]);
                }

                GizmoUtility.SetColor(TriangleColor * colorScale, true);
                int tcnt = tdata.proxyTriangleChunk.dataLength;
                for (int i = 0; i < tcnt; i++)
                {
                    if (drawSettings.CheckTriangleDrawing(i) == false)
                        continue;

                    int tindex = tdata.proxyTriangleChunk.startIndex + i;
                    int3 tri = vm.triangles[tindex];

                    //int3 vtri = tri + tdata.proxyCommonChunk.startIndex;
                    //if (vm.attributes[vtri.x].IsInvalid() || vm.attributes[vtri.y].IsInvalid() || vm.attributes[vtri.z].IsInvalid())
                    //    continue;

                    segmentBuffer0.Add(tri.x);
                    segmentBuffer0.Add(tri.y);
                    segmentBuffer0.Add(tri.y);
                    segmentBuffer0.Add(tri.z);
                    segmentBuffer0.Add(tri.z);
                    segmentBuffer0.Add(tri.x);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());

                segmentBuffer0.Clear();
                GizmoUtility.SetColor(LineColor * colorScale, true);
                int lcnt = proxyMesh.LineCount;
                for (int i = 0; i < lcnt; i++)
                {
                    int2 line = proxyMesh.lines[i];

                    //int2 vline = line + tdata.proxyCommonChunk.startIndex;
                    //if (vm.attributes[vline.x].IsInvalid() || vm.attributes[vline.y].IsInvalid())
                    //    continue;

                    segmentBuffer0.Add(line.x);
                    segmentBuffer0.Add(line.y);
                }
                Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
            }

            // base line
            if (drawSettings.baseLine && proxyMesh.BaseLineCount > 0)
            {
                GizmoUtility.SetColor(new Color(1.0f, 0.27f, 0.0f) * colorScale, true);
                int bcnt = proxyMesh.BaseLineCount;
                for (int i = 0; i < bcnt; i++)
                {
                    int dstart = proxyMesh.baseLineStartDataIndices[i];
                    int dcnt = proxyMesh.baseLineDataCounts[i];
                    for (int j = 1; j < dcnt; j++)
                    {
                        int vindex = proxyMesh.baseLineData[dstart + j];
                        int pindex = proxyMesh.vertexParentIndices[vindex];
                        if (pindex >= 0)
                        {
                            var pos = positionArray[pstart + vindex];
                            var ppos = positionArray[pstart + pindex];
                            GizmoUtility.DrawLine(pos, ppos, true);
                        }
                    }
                }
            }

            // animated position
            if (drawSettings.animatedPosition)
            {
                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = tdata.particleChunk.startIndex + i;

                    //int cvindex = tdata.proxyCommonChunk.startIndex + i;
                    //var attr = vm.attributes[cvindex];
                    //if (attr.IsInvalid())
                    //    continue;

                    var pos = sim.basePosArray[pindex];
                    //var pos = sim.stepBasicPositionBuffer[pindex];
                    GizmoUtility.SetColor(Color.cyan * colorScale, true);
                    GizmoUtility.DrawSphere(pos, drawPointSize, true);
                }
            }

            // animated axis
            if (drawSettings.animatedAxis != ClothDebugSettings.DebugAxis.None)
            {
                float size = drawLineSize;

                positionBuffer0.Clear();
                positionBuffer1.Clear();
                positionBuffer2.Clear();
                segmentBuffer0.Clear();
                segmentBuffer1.Clear();
                segmentBuffer2.Clear();

                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = tdata.particleChunk.startIndex + i;
                    int cvindex = tdata.proxyCommonChunk.startIndex + i;

                    //var attr = vm.attributes[cvindex];
                    //if (attr.IsInvalid())
                    //    continue;

                    var pos = sim.basePosArray[pindex];
                    var rot = sim.baseRotArray[pindex];
                    //var pos = sim.stepBasicPositionBuffer[pindex];
                    //var rot = sim.stepBasicRotationBuffer[pindex];

                    // 基準軸
                    bool xaxis = false, yaxis = false, zaxis = false;
                    if (drawSettings.animatedAxis == ClothDebugSettings.DebugAxis.Normal)
                    {
                        switch (cprocess.parameters.normalAxis)
                        {
                            case ClothNormalAxis.Right:
                            case ClothNormalAxis.InverseRight:
                                xaxis = true;
                                break;
                            case ClothNormalAxis.Up:
                            case ClothNormalAxis.InverseUp:
                                yaxis = true;
                                break;
                            case ClothNormalAxis.Forward:
                            case ClothNormalAxis.InverseForward:
                                zaxis = true;
                                break;
                        }
                    }
                    if (drawSettings.animatedAxis == ClothDebugSettings.DebugAxis.All)
                    {
                        xaxis = yaxis = zaxis = true;
                    }

                    if (xaxis)
                    {
                        int index = positionBuffer0.Count;
                        positionBuffer0.Add(pos);
                        positionBuffer0.Add(pos + MathUtility.ToBinormal(rot) * size);
                        segmentBuffer0.Add(index);
                        segmentBuffer0.Add(index + 1);
                    }
                    if (yaxis)
                    {
                        int index = positionBuffer1.Count;
                        positionBuffer1.Add(pos);
                        positionBuffer1.Add(pos + MathUtility.ToNormal(rot) * size);
                        segmentBuffer1.Add(index);
                        segmentBuffer1.Add(index + 1);
                    }
                    if (zaxis)
                    {
                        int index = positionBuffer2.Count;
                        positionBuffer2.Add(pos);
                        positionBuffer2.Add(pos + MathUtility.ToTangent(rot) * size);
                        segmentBuffer2.Add(index);
                        segmentBuffer2.Add(index + 1);
                    }
                }

                if (positionBuffer0.Count > 0)
                {
                    Handles.color = Color.red * colorScale;
                    Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
                }
                if (positionBuffer1.Count > 0)
                {
                    Handles.color = Color.green * colorScale;
                    Handles.DrawLines(positionBuffer1.ToArray(), segmentBuffer1.ToArray());
                }
                if (positionBuffer2.Count > 0)
                {
                    Handles.color = Color.blue * colorScale;
                    Handles.DrawLines(positionBuffer2.ToArray(), segmentBuffer2.ToArray());
                }
            }

#if false
            // distance constraint
            //for (int k = 0; k < DistanceConstraint.TypeCount; k++)
            {
                //bool drawFlag = false;
                //switch (k)
                //{
                //    case DistanceConstraint.Type_Vertical: drawFlag = drawSettings.verticalDistanceConstraint; break;
                //    case DistanceConstraint.Type_Horizontal: drawFlag = drawSettings.horizontalDistanceConstraint; break;
                //}
                //if (drawFlag == false)
                //    continue;

                var distanceConstraint = sim.distanceConstraint;
                //var nData = distanceConstraint.nativeData[k];

                var sc = tdata.distanceStartChunk;
                var dc = tdata.distanceDataChunk;
                if (sc.IsValid)
                {
                    //var col = Color.black;
                    //switch (k)
                    //{
                    //    case DistanceConstraint.Type_Vertical:
                    //        col = Color.yellow;
                    //        break;
                    //    case DistanceConstraint.Type_Horizontal:
                    //        col = Color.red;
                    //        break;
                    //}
                    //GizmoUtility.SetColor(col * colorScale, true);

                    positionBuffer0.Clear();
                    segmentBuffer0.Clear();
                    for (int i = 0; i < pcnt; i++)
                    {
                        positionBuffer0.Add(positionArray[pstart + i]);
                    }

                    for (int i = 0; i < pcnt; i++)
                    {
                        if (drawSettings.CheckParticleDrawing(i) == false)
                            continue;

                        int cindex = sc.startIndex + i;
                        var pack = distanceConstraint.indexArray[cindex];
                        DataUtility.Unpack10_22(pack, out int dcnt, out int dstart);
                        if (dcnt > 0)
                        {
                            for (int j = 0; j < dcnt; j++)
                            {
                                int targetIndex = distanceConstraint.dataArray[dc.startIndex + dstart + j];
                                segmentBuffer0.Add(i);
                                segmentBuffer0.Add(targetIndex);
                            }
                        }
                    }
                    Handles.DrawLines(positionBuffer0.ToArray(), segmentBuffer0.ToArray());
                }
            }
#endif

            // depth
            if (drawSettings.depth)
            {
                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = tdata.particleChunk.startIndex + i;
                    int cvindex = tdata.proxyCommonChunk.startIndex + i;

                    if (vm.attributes[cvindex].IsInvalid())
                        continue;

                    var pos = positionArray[pindex];
                    var depth = vm.vertexDepths[cvindex];
                    Handles.Label(pos, $"{depth:0.##}");
                }
            }
#if MC2_DEBUG
            // number
            else if (drawSettings.particleNumber || drawSettings.localNumber)
            {
                for (int i = 0; i < pcnt; i++)
                {
                    //if (drawSettings.CheckParticleDrawing(i) == false)
                    //    continue;

                    int pindex = tdata.particleChunk.startIndex + i;
                    var pos = positionArray[pindex];
                    if (drawSettings.particleNumber && drawSettings.CheckParticleDrawing(pindex))
                        Handles.Label(pos, pindex.ToString());
                    else if (drawSettings.localNumber && drawSettings.CheckParticleDrawing(i))
                        Handles.Label(pos, i.ToString());
                }
            }
            // attribute
            else if (drawSettings.attribute)
            {
                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = tdata.particleChunk.startIndex + i;
                    int cvindex = tdata.proxyCommonChunk.startIndex + i;

                    var pos = positionArray[pindex];
                    var attr = vm.attributes[cvindex];
                    Handles.Label(pos, $"0x{attr.Value:X2}");
                }
            }
            // friction
            else if (drawSettings.friction)
            {
                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = tdata.particleChunk.startIndex + i;
                    int cvindex = tdata.proxyCommonChunk.startIndex + i;

                    var pos = positionArray[pindex];
                    var friction = sim.frictionArray[pindex];
                    if (friction > 1e-06f)
                        Handles.Label(pos, $"{friction:0.##}");
                }
            }
            // static friction
            else if (drawSettings.staticFriction)
            {
                for (int i = 0; i < pcnt; i++)
                {
                    if (drawSettings.CheckParticleDrawing(i) == false)
                        continue;

                    int pindex = tdata.particleChunk.startIndex + i;
                    int cvindex = tdata.proxyCommonChunk.startIndex + i;

                    var pos = positionArray[pindex];
                    var friction = sim.staticFrictionArray[pindex];
                    if (friction > 1e-06f)
                        Handles.Label(pos, $"{friction:0.##}");
                }
            }
            // triangle number
            else if (drawSettings.triangleNumber)
            {
                int tcnt = tdata.proxyTriangleChunk.dataLength;
                for (int i = 0; i < tcnt; i++)
                {
                    if (drawSettings.CheckTriangleDrawing(i) == false)
                        continue;

                    int tindex = tdata.proxyTriangleChunk.startIndex + i;
                    int3 tri = vm.triangles[tindex];
                    var pos1 = positionArray[pstart + tri.x];
                    var pos2 = positionArray[pstart + tri.y];
                    var pos3 = positionArray[pstart + tri.z];
                    var cen = MathUtility.TriangleCenter(pos1, pos2, pos3);
                    Handles.Label(cen, i.ToString());
                }
            }
#endif

            // 空間を戻す
            Handles.matrix = Matrix4x4.identity;

            // 以下はワールド
            // custom skinning bone
            {
                Handles.color = SkininngLine * colorScale;
                var boneList = cprocess.cloth.SerializeData.customSkinningSetting.skinningBones;
                for (int i = 0; i < boneList.Count - 1; i++)
                {
                    var bone1 = boneList[i];
                    if (bone1 == null)
                        continue;
                    for (int j = i + 1; j < boneList.Count; j++)
                    {
                        var bone2 = boneList[j];
                        if (bone2 == null)
                            continue;

                        if (bone1.parent == bone2 || bone2.parent == bone1)
                        {
                            Handles.DrawLine(bone1.position, bone2.position);
                        }
                    }
                }
            }


            // inertia center
            {
                var pos = cdata.nowWorldPosition;
                var rot = cdata.nowWorldRotation;
                GizmoUtility.SetColor(Color.magenta * colorScale, true);
                GizmoUtility.DrawSphere(pos, drawSettings.GetInertiaCenterRadius(), true);
                GizmoUtility.DrawCross(pos, rot, drawSettings.GetInertiaCenterRadius() * 2.0f, true);
            }

#if MC2_DEBUG
            // cell cube
            //if (drawSettings.cellCube)
            //{
            //    GizmoUtility.SetColor(Color.black * colorScale, true);
            //    GizmoUtility.DrawWireCube(tdata.cubeWorldCenter, Quaternion.identity, Vector3.one * tdata.cubeSize, true);
            //}
#endif
        }
    }
}
