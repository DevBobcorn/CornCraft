// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

namespace MagicaCloth
{
    /// <summary>
    /// ビルド結果
    /// </summary>
    public class BuildResult
    {
        private int success = 0;
        private int failed = 0;
        private Define.Error lastError = Define.Error.None;

        //=========================================================================================
        public int SuccessCount => success;
        public int FailedCount => failed;
        public Define.Error LastError => lastError;

        //=========================================================================================
        public BuildResult()
        {
            success = 0;
            failed = 0;
            lastError = Define.Error.None;
        }

        public BuildResult(Define.Error err)
        {
            success = 0;
            failed = 1;
            lastError = err;
        }

        public void Merge(BuildResult src)
        {
            success += src.success;
            failed += src.failed;
            lastError = src.lastError;
        }

        public void SetError(Define.Error err)
        {
            failed++;
            lastError = err;
        }

        public void SetSuccess()
        {
            success++;
        }

        public string GetErrorMessage()
        {
            return Define.GetErrorMessage(lastError);
        }

        public bool IsSuccess()
        {
            return Define.IsNormal(lastError);
        }

        public bool IsError()
        {
            return Define.IsError(lastError);
        }
    }
}
