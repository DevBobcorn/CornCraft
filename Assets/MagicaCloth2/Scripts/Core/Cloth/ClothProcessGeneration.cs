// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

using Unity.Mathematics;

namespace MagicaCloth2
{
    public partial class ClothProcess
    {
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
