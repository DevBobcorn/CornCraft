// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class ClothProcess
    {
        public ResultCode GenerateStatusCheck()
        {
            ResultCode result = new ResultCode();

            // スケール値チェック
            var scl = cloth.transform.lossyScale;
            if (Mathf.Approximately(scl.x, 0.0f) || Mathf.Approximately(scl.y, 0.0f) || Mathf.Approximately(scl.z, 0.0f))
            {
                // スケール値がゼロ
                result.SetError(Define.Result.Init_ScaleIsZero);
            }
            else if (scl.x < 0.0f || scl.y < 0.0f || scl.z < 0.0f)
            {
                // 負のスケール
                result.SetError(Define.Result.Init_NegativeScale);
            }
            else
            {
                float diff1 = Mathf.Abs(1.0f - scl.x / scl.y);
                float diff2 = Mathf.Abs(1.0f - scl.x / scl.z);
                const float diffTolerance = 0.01f; // 誤差(1%)
                if (diff1 > diffTolerance || diff2 > diffTolerance)
                {
                    // 一様スケールではない
                    result.SetError(Define.Result.Init_NonUniformScale);
                }
            }

            return result;
        }

        internal bool GenerateInitialization()
        {
            result.SetProcess();

            // シリアライズデータ(1)の検証
            if (cloth.SerializeData.IsValid() == false)
            {
                if (cloth.SerializeData.VerificationResult == Define.Result.Empty)
                    result.SetError(Define.Result.CreateCloth_InvalidSerializeData);
                else
                    result.SetError(cloth.SerializeData.VerificationResult);
                return false;
            }
            cloth.SerializeData.DataValidate();
            cloth.serializeData2.DataValidate();

            // 初期化実行
            Init();
            if (result.IsError())
                return false;

            return true;
        }

        internal bool GenerateBoneClothSelection()
        {
            // セレクションデータの構築
            var setupData = boneClothSetupData;
            int tcnt = setupData.skinBoneCount; // パーティクルトランスフォーム総数
            var selectionData = new SelectionData(tcnt);
            for (int i = 0; i < tcnt; i++)
            {
                float3 lpos = math.transform(setupData.initRenderWorldtoLocal, setupData.transformPositions[i]);
                selectionData.positions[i] = lpos;
                selectionData.attributes[i] = VertexAttribute.Move; // 移動で初期化
            }

            // 最大接続距離
            float maxLength = 0;
            for (int i = 0; i < tcnt; i++)
            {
                int pi = setupData.GetParentTransformIndex(i, true);
                if (pi >= 0)
                {
                    float length = math.distance(selectionData.positions[i], selectionData.positions[pi]);
                    maxLength = math.max(maxLength, length);
                }
            }
            selectionData.maxConnectionDistance = maxLength;

            // ルートを固定に設定
            foreach (var rt in cloth.SerializeData.rootBones)
            {
                if (rt)
                {
                    int index = setupData.GetTransformIndexFromId(rt.GetInstanceID());
                    selectionData.attributes[index] = VertexAttribute.Fixed;
                }
            }

            selectionData.userEdit = true; // 念のため
            cloth.GetSerializeData2().selectionData = selectionData;

            return true;
        }
    }
}
