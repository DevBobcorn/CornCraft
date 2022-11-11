using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 開発用ユーティリティ
    /// 主にログ出力
    /// </summary>
    public static class Develop
    {
        /// <summary>
        /// 標準ログ出力
        /// </summary>
        /// <param name="str"></param>
        [System.Diagnostics.Conditional("MAGICACLOTH_DEBUG")]
        public static void Log(string str)
        {
            Debug.Log(str);
        }
    }
}
