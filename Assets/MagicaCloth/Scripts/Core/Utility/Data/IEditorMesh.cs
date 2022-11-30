// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// エディタでのメッシュ状態取得インターフェース
    /// ※これはエディタでの構築用です
    /// </summary>
    public interface IEditorMesh
    {
        /// <summary>
        /// メッシュのワールド座標/法線/接線を返す
        /// </summary>
        /// <param name="wposList"></param>
        /// <param name="wnorList"></param>
        /// <param name="wtanList"></param>
        /// <returns>頂点数</returns>
        int GetEditorPositionNormalTangent(out List<Vector3> wposList, out List<Vector3> wnorList, out List<Vector3> wtanList);

        /// <summary>
        /// メッシュのトライアングルリストを返す
        /// </summary>
        /// <returns></returns>
        List<int> GetEditorTriangleList();

        /// <summary>
        /// メッシュのラインリストを返す
        /// </summary>
        /// <returns></returns>
        List<int> GetEditorLineList();
    }
}
