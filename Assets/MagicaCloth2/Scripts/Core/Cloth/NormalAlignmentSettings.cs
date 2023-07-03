// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Normal adjustment settings.
    /// 法線調整設定
    /// </summary>
    [System.Serializable]
    public class NormalAlignmentSettings : IValid, IDataValidate, ITransform
    {
        public enum AlignmentMode
        {
            None = 0,

            /// <summary>
            /// Radiation from center of AABB.
            /// 中心から放射
            /// </summary>
            BoundingBoxCenter = 1,

            /// <summary>
            /// Emit from the specified transform.
            /// 指定トランスフォームから放射
            /// </summary>
            Transform = 2,
        }

        /// <summary>
        /// adjustment mode.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public AlignmentMode alignmentMode = AlignmentMode.None;

        /// <summary>
        /// Transform at which the radiation is centered.
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public Transform adjustmentTransform;


        public void DataValidate()
        {
        }

        public bool IsValid()
        {
            switch (alignmentMode)
            {
                case AlignmentMode.None:
                case AlignmentMode.BoundingBoxCenter:
                case AlignmentMode.Transform:
                    //case AlignmentMode.BoneWeight:
                    return true;
            }

            return false;
        }

        public NormalAlignmentSettings Clone()
        {
            return new NormalAlignmentSettings()
            {
                alignmentMode = alignmentMode,
                adjustmentTransform = adjustmentTransform,
            };
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// （このハッシュは実行時には利用されない編集用のもの）
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 0;
            hash += (int)alignmentMode * 105;
            if (adjustmentTransform)
            {
                hash += adjustmentTransform.GetInstanceID();
                hash += adjustmentTransform.position.GetHashCode();
            }

            return hash;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            if (adjustmentTransform)
                transformSet.Add(adjustmentTransform);
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            if (adjustmentTransform && replaceDict.ContainsKey(adjustmentTransform.GetInstanceID()))
            {
                adjustmentTransform = replaceDict[adjustmentTransform.GetInstanceID()];
            }
        }
    }
}
