// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Diagnostics;

namespace MagicaCloth2
{
    /// <summary>
    /// 様々な処理の結果
    /// </summary>
    public struct ResultCode
    {
        volatile Define.Result result;

        public Define.Result Result => result;

        public static ResultCode None => new ResultCode(Define.Result.None);
        public static ResultCode Empty => new ResultCode(Define.Result.Empty);

        public ResultCode(Define.Result initResult)
        {
            result = initResult;
        }

        public void Clear()
        {
            result = Define.Result.None;
        }

        public void SetResult(Define.Result code)
        {
            result = code;
        }

        public void SetSuccess()
        {
            SetResult(Define.Result.Success);
        }

        public void SetCancel()
        {
            SetResult(Define.Result.Cancel);
        }

        public void SetError(Define.Result code = Define.Result.Error)
        {
            result = code;
#if MC2_DEBUG
            Develop.DebugLogError(GetResultString());
#endif
        }

        public void Set(ResultCode src)
        {
            result = src.result;
        }

        public void SetProcess()
        {
            SetResult(Define.Result.Process);
        }

        public bool IsResult(Define.Result code)
        {
            return result == code;
        }

        public bool IsNone() => result == Define.Result.None;
        public bool IsSuccess() => result == Define.Result.Success;
        public bool IsFaild() => !IsSuccess();
        public bool IsCancel() => result == Define.Result.Cancel;
        public bool IsNormal() => result < Define.Result.Warning;
        public bool IsWarning() => result >= Define.Result.Warning && result < Define.Result.Error;
        public bool IsError() => result >= Define.Result.Error;
        public bool IsProcess() => result == Define.Result.Process;

        public string GetResultString()
        {
            if (IsNormal())
                return result.ToString();
            else
                return $"({(int)result}) {result}";
        }

        /// <summary>
        /// 結果コードに対する追加情報を取得する。ない場合はnullが返る。
        /// </summary>
        /// <returns></returns>
        public string GetResultInformation()
        {
            switch (result)
            {
                case Define.Result.RenderSetup_Unreadable:
                    return "It is necessary to turn on [Read/Write] in the model import settings.";
                case Define.Result.RenderSetup_Over65535vertices:
                    return "Original mesh must have no more than 65,535 vertices";
                default:
                    return null;
            }
        }

        [Conditional("MC2_DEBUG")]
        public void DebugLog(bool error = true, bool warning = true, bool normal = true)
        {
            int num = (int)result;
            if (IsError() && error)
                Develop.DebugLogError(GetResultString());
            else if (IsWarning() && warning)
                Develop.DebugLogWarning(GetResultString());
            else if (normal)
                Develop.DebugLog(GetResultString());
        }
    }
}
