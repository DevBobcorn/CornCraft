// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 描画の管理とメッシュ更新マネージャ
    /// </summary>
    public class RenderManager : IManager, IValid
    {
        /// <summary>
        /// 描画データをint型ハンドルで管理する
        /// </summary>
        Dictionary<int, RenderData> renderDataDict = new Dictionary<int, RenderData>();

        bool isValid = false;

        //=========================================================================================
        public void Initialize()
        {
            Dispose();

            // 更新処理
            MagicaManager.afterDelayedDelegate += PreRenderingUpdate;

            isValid = true;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Dispose()
        {
            isValid = false;

            lock (renderDataDict)
            {
                foreach (var rinfo in renderDataDict.Values)
                {
                    rinfo?.Dispose();
                }
            }
            renderDataDict.Clear();

            // 更新処理
            MagicaManager.afterDelayedDelegate -= PreRenderingUpdate;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// 管理するレンダラーの追加（メインスレッドのみ）
        /// </summary>
        /// <param name="ren"></param>
        /// <returns></returns>
        public int AddRenderer(Renderer ren)
        {
            if (isValid == false)
                return 0;
            Debug.Assert(ren);

            // 制御ハンドル
            int handle = ren.GetInstanceID();

            lock (renderDataDict)
            {
                if (renderDataDict.ContainsKey(handle) == false)
                {
                    // 新規
                    var rdata = new RenderData();
                    rdata.Initialize(ren);
                    renderDataDict.Add(handle, rdata);
                }

                // 参照カウント+
                renderDataDict[handle].AddReferenceCount();
            }

            return handle;
        }

        public bool RemoveRenderer(int handle)
        {
            if (isValid == false)
                return false;

            bool delete = false;

            //if(renderDataDict.ContainsKey(handle) == )

            //Debug.Log($"RemoveRenderer:{handle}");
            //Debug.Assert(ren);
            Debug.Assert(renderDataDict.ContainsKey(handle));

            lock (renderDataDict)
            {
                if (renderDataDict.ContainsKey(handle))
                {
                    var rdata = renderDataDict[handle];
                    if (rdata.RemoveReferenceCount() == 0)
                    {
                        // 破棄する
                        rdata.Dispose();

                        renderDataDict.Remove(handle);

                        delete = true;
                    }
                }
            }

            return delete;
        }

        public RenderData GetRendererData(int handle)
        {
            if (isValid == false)
                return null;

            lock (renderDataDict)
            {
                if (renderDataDict.ContainsKey(handle))
                    return renderDataDict[handle];
                else
                    return null;

            }
        }

        //=========================================================================================
        /// <summary>
        /// 有効化
        /// </summary>
        /// <param name="handle"></param>
        public void StartUse(ClothProcess cprocess, int handle)
        {
            GetRendererData(handle)?.StartUse(cprocess);
        }

        /// <summary>
        /// 無効化
        /// </summary>
        /// <param name="handle"></param>
        public void EndUse(ClothProcess cprocess, int handle)
        {
            GetRendererData(handle)?.EndUse(cprocess);
        }

        //=========================================================================================
        /// <summary>
        /// レンダリング前更新
        /// </summary>
        void PreRenderingUpdate()
        {
            // メッシュへの反映
            foreach (var rdata in renderDataDict.Values)
                rdata?.WriteMesh();
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== Render Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Render Manager. Invalid.");
            }
            else
            {
                sb.AppendLine($"Render Manager. Count({renderDataDict.Count})");

                foreach (var kv in renderDataDict)
                {
                    sb.Append(kv.Value.ToString());
                }
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
