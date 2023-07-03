// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 開発用ユーティリティ
    /// </summary>
    public static class Develop
    {
        public static void Log(in object mes)
        {
            Debug.Log($"[MC2] {mes}");
        }

        public static void LogWarning(in object mes)
        {
            Debug.LogWarning($"[MC2] {mes}");
        }

        public static void LogError(in object mes)
        {
            Debug.LogError($"[MC2] {mes}");
        }

        [System.Diagnostics.Conditional("MC2_LOG")]
        public static void DebugLog(in object mes)
        {
            Debug.Log($"[MC2 DEBUG] {mes}");
        }

        [System.Diagnostics.Conditional("MC2_DEBUG")]
        public static void DebugLogWarning(in object mes)
        {
            Debug.LogWarning($"[MC2 DEBUG] {mes}");
        }

        [System.Diagnostics.Conditional("MC2_DEBUG")]
        public static void DebugLogError(in object mes)
        {
            Debug.LogError($"[MC2 DEBUG] {mes}");
        }

        [System.Diagnostics.Conditional("MC2_DEBUG")]
        public static void Assert(bool condition)
        {
            // 何故かメッセージを追加すると機能しない..
            Debug.Assert(condition);
        }
    }
}
