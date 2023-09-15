// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothで利用する仮想メッシュ構造
    /// ・メッシュの親子関係により頂点が連動する
    /// ・トライアングルだけでなくライン構造も認める
    /// ・スレッドで利用できるようにする
    /// ・頂点数は最大65535に制限、ただし一部を除きインデックスはintで扱う
    /// </summary>
    public partial class VirtualMesh : IDisposable, IValid
    {

        public string name = string.Empty;

        public ResultCode result = new ResultCode();

        /// <summary>
        /// メッシュタイプ
        /// </summary>
        public enum MeshType
        {
            /// <summary>
            /// 通常のメッシュ
            /// 座標計算：Transformからスキニング、法線接線計算：Transformからスキニング
            /// </summary>
            NormalMesh = 0,

            /// <summary>
            /// Transform連動メッシュ
            /// 座標計算：Transform直値、法線接線計算：Transform直値
            /// </summary>
            NormalBoneMesh = 1,

            /// <summary>
            /// プロキシメッシュ
            /// 法線接線の計算を接続するトライアングルから求める
            /// 座標計算：Transformからスキニング、法線接線計算：接続トライアングル平均化 or スキニング直値
            /// </summary>
            ProxyMesh = 2,

            /// <summary>
            /// Transform連動プロキシメッシュ
            /// シミュレーション前にTransformの復元を行い、シミュレーション後にTransformを書き込む
            /// 座標計算：Transform直値、法線接線計算：接続トライアングル平均化 or Transform直値
            /// </summary>
            ProxyBoneMesh = 3,

            /// <summary>
            /// マッピングメッシュ(MeshClothのみ)
            /// メッシュがプロキシメッシュにマッピングされた状態
            /// 座標計算：接続ProxyMesh頂点からスキニング、法線接線計算：接続ProxyMeshからスキニング
            /// </summary>
            Mapping = 4,
        }
        public MeshType meshType = MeshType.NormalMesh;

        /// <summary>
        /// Transformと直接連動するかどうか(=BoneCloth)
        /// </summary>
        public bool isBoneCloth = false;

        //=========================================================================================
        // メッシュ情報（基本）
        //=========================================================================================
        /// <summary>
        /// 現在の頂点が指す元の頂点インデックス
        /// </summary>
        public ExSimpleNativeArray<int> referenceIndices = new ExSimpleNativeArray<int>();

        /// <summary>
        /// 頂点属性
        /// </summary>
        public ExSimpleNativeArray<VertexAttribute> attributes = new ExSimpleNativeArray<VertexAttribute>();

        public ExSimpleNativeArray<float3> localPositions = new ExSimpleNativeArray<float3>();
        public ExSimpleNativeArray<float3> localNormals = new ExSimpleNativeArray<float3>();
        public ExSimpleNativeArray<float3> localTangents = new ExSimpleNativeArray<float3>();

        /// <summary>
        /// VirtualMeshのUVはTangent計算用でありテクスチャマッピング用ではないので注意！
        /// </summary>
        public ExSimpleNativeArray<float2> uv = new ExSimpleNativeArray<float2>();
        public ExSimpleNativeArray<VirtualMeshBoneWeight> boneWeights = new ExSimpleNativeArray<VirtualMeshBoneWeight>();

        public ExSimpleNativeArray<int3> triangles = new ExSimpleNativeArray<int3>();
        public ExSimpleNativeArray<int2> lines = new ExSimpleNativeArray<int2>();

        /// <summary>
        /// メッシュの基準トランスフォーム
        /// </summary>
        public int centerTransformIndex = -1;

        /// <summary>
        /// このメッシュの基準マトリックスと回転
        /// </summary>
        public float4x4 initLocalToWorld;
        public float4x4 initWorldToLocal;
        public quaternion initRotation;
        public quaternion initInverseRotation;
        public float3 initScale;

        /// <summary>
        /// 計算用スケール(X軸のみで判定)
        /// </summary>
        public float InitCalcScale => initScale.x;

        /// <summary>
        /// スキニングルートボーン
        /// </summary>
        public int skinRootIndex = -1;

        /// <summary>
        /// スキニングボーン
        /// </summary>
        public ExSimpleNativeArray<int> skinBoneTransformIndices = new ExSimpleNativeArray<int>();
        public ExSimpleNativeArray<float4x4> skinBoneBindPoses = new ExSimpleNativeArray<float4x4>();

        /// <summary>
        /// このメッシュで利用するTransformの情報
        /// </summary>
        public TransformData transformData;

        /// <summary>
        /// バウンディングボックス
        /// </summary>
        public NativeReference<AABB> boundingBox;

        /// <summary>
        /// 頂点の平均接続距離
        /// </summary>
        public NativeReference<float> averageVertexDistance;

        /// <summary>
        /// 頂点の最大接続距離
        /// </summary>
        public NativeReference<float> maxVertexDistance;

        public bool IsSuccess => result.IsSuccess();
        public bool IsError => result.IsError();
        public bool IsProcess => result.IsProcess();
        public int VertexCount => localPositions.Count;
        public int TriangleCount => triangles.Count;
        public int LineCount => lines.Count;
        public int SkinBoneCount => skinBoneTransformIndices.Count;
        public int TransformCount => transformData?.Count ?? 0;
        public bool IsProxy => meshType == MeshType.ProxyMesh || meshType == MeshType.ProxyBoneMesh;
        public bool IsMapping => meshType == MeshType.Mapping;

        //=========================================================================================
        // 結合／リダクション
        //=========================================================================================
        /// <summary>
        /// 結合された頂点範囲（結合もとメッシュが保持する）
        /// </summary>
        public DataChunk mergeChunk;

        /// <summary>
        /// 元の頂点からの結合とリダクション後の頂点へのインデックス
        /// </summary>
        public NativeArray<int> joinIndices;

        //=========================================================================================
        // プロキシメッシュ
        //=========================================================================================
        /// <summary>
        /// 頂点ごとの接続トライアングルインデックスと法線接線フリップフラグ（最大７つ）
        /// これは法線を再計算するために用いられるもので７つあれば十分であると判断したもの。
        /// そのため正確なトライアングル接続を表していない。
        /// データは12-20bitのuintでパックされている
        /// 12(hi) = 法線接線のフリップフラグ(法線:0x1,接線:0x2)。ONの場合フリップ。
        /// 20(low) = トライアングルインデックス。
        /// </summary>
        public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;

        /// <summary>
        /// 頂点ごとの接続頂点インデックス
        /// vertexToVertexIndexArrayは頂点ごとのデータ開始インデックス(22bit)とカウンタ(10bit)を１つのuintに結合したもの
        /// todo:ここはMultiHashMapのままで良いかもしれない
        /// </summary>
        public NativeArray<uint> vertexToVertexIndexArray;
        public NativeArray<ushort> vertexToVertexDataArray;

        /// <summary>
        /// エッジリスト
        /// </summary>
        public NativeArray<int2> edges;

        /// <summary>
        /// エッジ固有フラグ
        /// </summary>
        public const byte EdgeFlag_Cut = 0x1; // 切り口エッジ
        public NativeArray<ExBitFlag8> edgeFlags;

        /// <summary>
        /// エッジごとの接続トライアングルインデックス
        /// </summary>
        public NativeParallelMultiHashMap<int2, ushort> edgeToTriangles;

        /// <summary>
        /// 頂点ごとのバインドポーズ
        /// 頂点バインドにはスケール値は不要
        /// </summary>
        public NativeArray<float3> vertexBindPosePositions;
        public NativeArray<quaternion> vertexBindPoseRotations;

        /// <summary>
        /// 頂点ごとの計算姿勢からTransformへの変換回転(BoneClothのみ)
        /// </summary>
        public NativeArray<quaternion> vertexToTransformRotations;

        /// <summary>
        /// 各頂点のレベル（起点は０から）
        /// </summary>
        //public NativeArray<byte> vertexLevels;

        /// <summary>
        /// 各頂点の深さ(0.0-1.0)
        /// </summary>
        public NativeArray<float> vertexDepths;

        /// <summary>
        /// 各頂点のルートインデックス(-1=なし)
        /// </summary>
        public NativeArray<int> vertexRootIndices;

        /// <summary>
        /// 各頂点の親頂点インデックス(-1=なし)
        /// </summary>
        public NativeArray<int> vertexParentIndices;

        /// <summary>
        /// 各頂点の子頂点インデックスリスト
        /// </summary>
        public NativeArray<uint> vertexChildIndexArray;
        public NativeArray<ushort> vertexChildDataArray;

        /// <summary>
        /// 各頂点の親からの基準ローカル座標
        /// </summary>
        public NativeArray<float3> vertexLocalPositions;

        /// <summary>
        /// 各頂点の親からの基準ローカル回転
        /// </summary>
        public NativeArray<quaternion> vertexLocalRotations;

        /// <summary>
        /// 法線調整用回転
        /// </summary>
        public NativeArray<quaternion> normalAdjustmentRotations;

#if false
        /// <summary>
        /// 角度計算用基準法線の計算方法
        /// </summary>
        public enum NormalCalcMode
        {
            Auto = 0,
            X_Axis = 1,
            Y_Axis = 2,
            Z_Axis = 3,
            Point_Outside = 4,
            Point_Inside = 5,
            //Line_Outside = 6,
            //Line_Inside = 7,
        }
        //public NormalCalcMode normalCalcMode = NormalCalcMode.Auto;

        /// <summary>
        /// 各頂点の角度計算用基準ローカル回転
        /// -AngleLimitConstraintで使用する
        /// pitch/yaw個別制限はv1.0では実装しないので一旦停止させる
        /// </summary>
        //public NativeArray<quaternion> vertexAngleCalcLocalRotations;
#endif

        /// <summary>
        /// 最大レベル値
        /// </summary>
        //public NativeReference<int> vertexMaxLevel;

        /// <summary>
        /// ベースラインごとのフラグ
        /// </summary>
        public const byte BaseLineFlag_IncludeLine = 0x01; // ラインを含む
        public NativeArray<ExBitFlag8> baseLineFlags;

        /// <summary>
        /// ベースラインごとのデータ開始インデックス
        /// </summary>
        public NativeArray<ushort> baseLineStartDataIndices;

        /// <summary>
        /// ベースラインごとのデータ数
        /// </summary>
        public NativeArray<ushort> baseLineDataCounts;

        /// <summary>
        /// ベースラインデータ（頂点インデックス）
        /// </summary>
        public NativeArray<ushort> baseLineData;

        /// <summary>
        /// カスタムスキニングボーンの登録トランスフォームインデックス
        /// </summary>
        public int[] customSkinningBoneIndices;

        /// <summary>
        /// センター計算用の固定頂点リスト
        /// </summary>
        public ushort[] centerFixedList;

        /// <summary>
        /// プロキシメッシュ構築時のセンター位置（ローカル）
        /// 固定頂点が無い場合は(0,0,0)
        /// </summary>
        public NativeReference<float3> localCenterPosition;

        public int BaseLineCount => baseLineStartDataIndices.IsCreated ? baseLineStartDataIndices.Length : 0;
        public int EdgeCount => edges.IsCreated ? edges.Length : 0;
        public int CustomSkinningBoneCount => customSkinningBoneIndices?.Length ?? 0;
        public int CenterFixedPointCount => centerFixedList?.Length ?? 0;
        public int NormalAdjustmentRotationCount => normalAdjustmentRotations.IsCreated ? normalAdjustmentRotations.Length : 0;

        //=========================================================================================
        // マッピングメッシュ
        //=========================================================================================
        /// <summary>
        /// 接続しているプロキシメッシュ
        /// </summary>
        public VirtualMesh mappingProxyMesh;

        /// <summary>
        /// マッピングメッシュのセンタートランスフォーム情報
        /// 毎フレーム更新される
        /// Meshの場合はレンダラーのトランスフォームとなる
        /// </summary>
        public float3 centerWorldPosition;
        public quaternion centerWorldRotation;
        public float3 centerWorldScale;

        /// <summary>
        /// 初期状態でのマッピングメッシュへの変換マトリックスと変換回転
        /// この姿勢は初期化時に固定される
        /// </summary>
        public float4x4 toProxyMatrix;
        public quaternion toProxyRotation;

        /// <summary>
        /// マネージャへの登録ID
        /// </summary>
        public int mappingId;

        //=========================================================================================
        public VirtualMesh()
        {
            transformData = new TransformData();

            // 最小限のデータ
            averageVertexDistance = new NativeReference<float>(0.0f, Allocator.Persistent);
            maxVertexDistance = new NativeReference<float>(0.0f, Allocator.Persistent);


            // 作業中にしておく
            result.SetProcess();
        }

        public VirtualMesh(string name) : this()
        {
            this.name = name;
        }

        public void Dispose()
        {
            result.Clear();
            referenceIndices.Dispose();
            attributes.Dispose();
            localPositions.Dispose();
            localNormals.Dispose();
            localTangents.Dispose();
            uv.Dispose();
            boneWeights.Dispose();
            triangles.Dispose();
            lines.Dispose();
            skinBoneTransformIndices.Dispose();
            skinBoneBindPoses.Dispose();

            if (joinIndices.IsCreated)
                joinIndices.Dispose();

            if (vertexToTriangles.IsCreated)
                vertexToTriangles.Dispose();
            if (vertexToVertexIndexArray.IsCreated)
                vertexToVertexIndexArray.Dispose();
            if (vertexToVertexDataArray.IsCreated)
                vertexToVertexDataArray.Dispose();
            if (edges.IsCreated)
                edges.Dispose();
            if (edgeFlags.IsCreated)
                edgeFlags.Dispose();
            if (edgeToTriangles.IsCreated)
                edgeToTriangles.Dispose();
            if (vertexBindPosePositions.IsCreated)
                vertexBindPosePositions.Dispose();
            if (vertexBindPoseRotations.IsCreated)
                vertexBindPoseRotations.Dispose();
            if (vertexToTransformRotations.IsCreated)
                vertexToTransformRotations.Dispose();
            if (vertexRootIndices.IsCreated)
                vertexRootIndices.Dispose();
            if (vertexParentIndices.IsCreated)
                vertexParentIndices.Dispose();
            if (vertexChildIndexArray.IsCreated)
                vertexChildIndexArray.Dispose();
            if (vertexChildDataArray.IsCreated)
                vertexChildDataArray.Dispose();
            if (baseLineFlags.IsCreated)
                baseLineFlags.Dispose();
            if (baseLineStartDataIndices.IsCreated)
                baseLineStartDataIndices.Dispose();
            if (baseLineDataCounts.IsCreated)
                baseLineDataCounts.Dispose();
            if (baseLineData.IsCreated)
                baseLineData.Dispose();
            if (localCenterPosition.IsCreated)
                localCenterPosition.Dispose();
            if (vertexLocalPositions.IsCreated)
                vertexLocalPositions.Dispose();
            if (vertexLocalRotations.IsCreated)
                vertexLocalRotations.Dispose();
            if (normalAdjustmentRotations.IsCreated)
                normalAdjustmentRotations.Dispose();
            //if (vertexAngleCalcLocalRotations.IsCreated)
            //    vertexAngleCalcLocalRotations.Dispose();
            //if (vertexMaxLevel.IsCreated)
            //    vertexMaxLevel.Dispose();
            //if (vertexLevels.IsCreated)
            //    vertexLevels.Dispose();
            if (vertexDepths.IsCreated)
                vertexDepths.Dispose();
            if (boundingBox.IsCreated)
                boundingBox.Dispose();
            if (averageVertexDistance.IsCreated)
                averageVertexDistance.Dispose();
            if (maxVertexDistance.IsCreated)
                maxVertexDistance.Dispose();

            transformData?.Dispose();
        }

        public void SetName(string newName)
        {
            name = newName;
        }

        /// <summary>
        /// 最低限のデータ検証
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (transformData == null)
                return false;

            // レンダラーが存在する場合はその存在を確認する
            if (centerTransformIndex >= 0 && transformData.GetTransformFromIndex(centerTransformIndex) == null)
                return false;

            return true;
        }

        //=========================================================================================
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"===== {name} =====");
            sb.Append($"Result:{result}");
            sb.Append($", Type:{meshType}");
            sb.Append($", Vertex:{VertexCount}");
            sb.Append($", Line:{LineCount}");
            sb.Append($", Triangle:{TriangleCount}");
            sb.Append($", Edge:{EdgeCount}");
            sb.Append($", SkinBone:{SkinBoneCount}");
            sb.Append($", Transform:{transformData?.Count}");
            sb.Append($", BaseLine:{BaseLineCount}");
            sb.AppendLine();

            if (averageVertexDistance.IsCreated)
                sb.Append($"avgDist:{averageVertexDistance.Value}");
            if (maxVertexDistance.IsCreated)
                sb.Append($", maxDist:{maxVertexDistance.Value}");
            if (boundingBox.IsCreated)
                sb.Append($", AABB:{boundingBox.Value}");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
