// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// インポートアセットの判定
    /// </summary>
    public class ClothAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// データベースにアセットが追加／削除／移動／更新された場合にその完了後に一度呼び出される
        /// </summary>
        /// <param name="importedAssets"></param>
        /// <param name="deletedAssets"></param>
        /// <param name="movedAssets"></param>
        /// <param name="movedFromAssetPaths"></param>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
#if false
            foreach (string str in importedAssets)
            {
                Debug.Log("@Reimported Asset: " + str);
            }
            foreach (string str in deletedAssets)
            {
                Debug.Log("@Deleted Asset: " + str);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                Debug.Log("@Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }
#endif
            // アセットがインポートされたことによる編集用メッシュの更新判定
            ClothEditorManager.UpdateFromAssetImport(importedAssets);
        }
    }
}
