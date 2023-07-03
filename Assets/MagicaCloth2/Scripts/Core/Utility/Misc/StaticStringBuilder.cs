// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;

namespace MagicaCloth2
{
    /// <summary>
    /// 静的StringBuilderクラス
    /// </summary>
    public class StaticStringBuilder
    {
        private static StringBuilder stringBuilder = new StringBuilder(1024);

        /// <summary>
        /// StringBuilderのインスタンスを取得する
        /// </summary>
        public static StringBuilder Instance
        {
            get
            {
                return stringBuilder;
            }
        }

        /// <summary>
        /// 内部をクリアする
        /// </summary>
        public static void Clear()
        {
            stringBuilder.Length = 0;
        }

        /// <summary>
        /// 与えられた文字を結合する
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static StringBuilder Append(params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                stringBuilder.Append(args[i]);
            }
            return stringBuilder;
        }

        /// <summary>
        /// 与えられた文字列を結合し、最後に改行コードを挿入する
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static StringBuilder AppendLine(params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                stringBuilder.Append(args[i]);
            }
            stringBuilder.Append("\n");
            return stringBuilder;
        }

        /// <summary>
        /// 改行を追加する
        /// </summary>
        /// <returns></returns>
        public static StringBuilder AppendLine()
        {
            stringBuilder.Append("\n");
            return stringBuilder;
        }

        /// <summary>
        /// 与えられた文字を結合して、結合文字列を返す
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string AppendToString(params object[] args)
        {
            stringBuilder.Length = 0;
            for (int i = 0; i < args.Length; i++)
            {
                stringBuilder.Append(args[i]);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 文字列を返す
        /// </summary>
        /// <returns></returns>
        public static new string ToString()
        {
            return stringBuilder.ToString();
        }
    }
}
