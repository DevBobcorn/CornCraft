// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using System.Text;

namespace MagicaCloth
{
    public static partial class Define
    {
        /// <summary>
        /// 結果コード
        /// </summary>
        public enum Error
        {
            None = 0, // なし(成功)
            Cancel = 1,

            // エラー
            EmptyData = 100,
            InvalidDataHash = 101,
            TooOldDataVersion = 102,
            HigherDataVersion = 103,

            MeshDataNull = 200,
            MeshDataHashMismatch = 201,
            MeshDataVersionMismatch = 202,

            ClothDataNull = 300,
            ClothDataHashMismatch = 301,
            ClothDataVersionMismatch = 302,

            ClothSelectionHashMismatch = 400,
            ClothSelectionVersionMismatch = 401,

            ClothTargetRootCountMismatch = 500,

            UseTransformNull = 600,
            UseTransformCountZero = 601,
            UseTransformCountMismatch = 602,

            DeformerNull = 700,
            DeformerHashMismatch = 701,
            DeformerVersionMismatch = 702,
            DeformerCountZero = 703,
            DeformerCountMismatch = 704,

            VertexCountZero = 800,
            VertexUseCountZero = 801,
            VertexCountMismatch = 802,

            RootListCountMismatch = 900,

            SelectionDataCountMismatch = 1000,
            SelectionCountZero = 1001,

            CenterTransformNull = 1100,

            SpringDataNull = 1200,
            SpringDataHashMismatch = 1201,
            SpringDataVersionMismatch = 1202,

            TargetObjectNull = 1300,

            SharedMeshNull = 1400,
            SharedMeshCannotRead = 1401,
            SharedMeshDifferent = 1402,
            SharedMeshDifferentVertexCount = 1403,

            MeshOptimizeMismatch = 1500,
            MeshVertexCount65535Over = 1501,
            MeshKeepQuads = 1502,

            BoneListZero = 1600,
            BoneListNull = 1601,

            RendererNotFound = 1700,
            MeshFilterNotFound = 1701,

            BuildNoTransformList = 8000,
            BuildReadOnlyPrefab = 8001,
            BuildFailedSaveAssets = 8002,
            BuildPrefabCannotSaved = 8003,
            BuildNotSceneObject = 8004,

            BuildInvalidComponent = 8100,
            BuildInvalidData = 8101,
            BuildInvalidMeshData = 8102,
            BuildInvalidGameObject = 8103,
            BuildInvalidPrefab = 8104,
            BuildInvalidRenderDeformer = 8105,
            BuildInvalidScene = 8106,
            BuildInvalidSelection = 8107,

            BuildMissingDeformer = 8200,
            BuildMissingSelection = 8201,
            BuildMissingMesh = 8202,
            BuildMissingScriptOnPrefab = 8203,


            // ここからはランタイムエラー(10000～)

            // ここからはワーニング(20000～)
            OverlappingTransform = 20000,
            AddOverlappingTransform = 20001,
            OldDataVersion = 20002,
            OldAlgorithm = 20003,
        }

        /// <summary>
        /// コードがエラーが無く正常か判定する
        /// </summary>
        /// <param name="err"></param>
        /// <returns></returns>
        public static bool IsNormal(Error err)
        {
            return err == Error.None;
        }

        /// <summary>
        /// コードがエラーか判定する
        /// </summary>
        /// <param name="err"></param>
        /// <returns></returns>
        public static bool IsError(Error err)
        {
            return err != Error.None && (int)err >= 100 && (int)err < 20000;
        }

        /// <summary>
        /// コードがワーニングか判定する
        /// </summary>
        /// <param name="err"></param>
        /// <returns></returns>
        public static bool IsWarning(Error err)
        {
            return (int)err >= 20000;
        }

        /// <summary>
        /// エラーメッセージを取得する
        /// </summary>
        /// <param name="err"></param>
        /// <returns></returns>
        public static string GetErrorMessage(Error err)
        {
            StringBuilder sb = new StringBuilder(512);

            // 基本エラーコード
            sb.AppendFormat("{0} ({1}) : {2}", IsError(err) ? "Error" : "Warning", (int)err, err.ToString());
            //if ((int)err < 20000)
            //    sb.AppendFormat("Error ({0}) : {1}", (int)err, err.ToString());
            //else
            //    sb.AppendFormat("Warning ({0}) : {1}", (int)err, err.ToString());

            // 個別の詳細メッセージ
            switch (err)
            {
                case Error.SharedMeshCannotRead:
                    sb.AppendLine();
                    sb.Append("Please turn On the [Read/Write Enabled] flag of the model importer.");
                    break;
                case Error.OldDataVersion:
                    sb.Clear();
                    sb.Append("Old data format!");
                    sb.AppendLine();
                    sb.Append("It may not work or the latest features may not be available.");
                    sb.AppendLine();
                    sb.Append("It is recommended to press the [Create] button and rebuild the data.");
                    break;
                case Error.EmptyData:
                    sb.Clear();
                    sb.Append("No Data.");
                    break;
                case Error.OldAlgorithm:
                    sb.Clear();
                    sb.Append("Old algorithms.");
                    sb.AppendLine();
                    sb.Append("Old algorithms will be removed in the future.");
                    sb.AppendLine();
                    sb.Append("Please use a more stable and up-to-date algorithm.");
                    sb.AppendLine();
                    sb.Append("The settings can be made from the [Algorithm] panel.");
                    break;
                case Error.MeshKeepQuads:
                    sb.AppendLine();
                    sb.Append("Keep Quads configuration is not supported.");
                    sb.AppendLine();
                    sb.Append("Please turn Off the [Keep Quads] flag of the model importer.");
                    break;
                case Error.RendererNotFound:
                    sb.AppendLine();
                    sb.Append("Creation failed. Renderer not found.");
                    break;
                case Error.MeshFilterNotFound:
                    sb.AppendLine();
                    sb.Append("Creation failed. MeshFilter not found.");
                    break;
            }

            return sb.ToString();

        }
    }
}
