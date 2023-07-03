// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class CustomSkinningSettings : IValid, IDataValidate, ITransform
    {
        /// <summary>
        /// valid state.
        /// 有効状態
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public bool enable = false;

        /// <summary>
        /// bones for skinning.
        /// Calculated from the parent-child structure line of bones registered here.
        /// スキニング用ボーン
        /// ここに登録されたボーンの親子構造ラインから算出される
        /// [NG] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public List<Transform> skinningBones = new List<Transform>();

        public void DataValidate()
        {
            //angularAttenuation = Mathf.Clamp01(angularAttenuation);
            //distanceReduction = Mathf.Clamp01(distanceReduction);
            //distancePow = Mathf.Clamp(distancePow, 0.1f, 5.0f);
        }

        public bool IsValid()
        {
            if (enable == false)
                return false;
            if (skinningBones.Count == 0)
                return false;
            if (skinningBones.Any(n => n != null) == false)
                return false;

            return true;
        }

        public CustomSkinningSettings Clone()
        {
            return new CustomSkinningSettings()
            {
                enable = enable,
                skinningBones = new List<Transform>(skinningBones),
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

            // おそらくカスタムスキニングは不要
#if false
            hash += enable.GetHashCode() * 101;
            hash += angularAttenuation.GetHashCode();
            hash += distanceReduction.GetHashCode();
            hash += distancePow.GetHashCode();
            foreach (var t in skinningBones)
                hash += t?.GetInstanceID() ?? 0;
#endif

            return hash;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            foreach (var t in skinningBones)
            {
                if (t)
                    transformSet.Add(t);
            }
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            for (int i = 0; i < skinningBones.Count; i++)
            {
                var t = skinningBones[i];
                if (t && replaceDict.ContainsKey(t.GetInstanceID()))
                {
                    skinningBones[i] = replaceDict[t.GetInstanceID()];
                }
            }
        }
    }
}
