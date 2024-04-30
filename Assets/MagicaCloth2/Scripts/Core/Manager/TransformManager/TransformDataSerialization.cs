// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class TransformData
    {
        /// <summary>
        /// PreBuildの共有部分保存データ
        /// </summary>
        [System.Serializable]
        public class ShareSerializationData
        {
            public ExSimpleNativeArray<ExBitFlag8>.SerializationData flagArray;
            public ExSimpleNativeArray<float3>.SerializationData initLocalPositionArray;
            public ExSimpleNativeArray<quaternion>.SerializationData initLocalRotationArray;
        }

        public ShareSerializationData ShareSerialize()
        {
            var sdata = new ShareSerializationData();
            try
            {
                sdata.flagArray = flagArray.Serialize();
                sdata.initLocalPositionArray = initLocalPositionArray.Serialize();
                sdata.initLocalRotationArray = initLocalRotationArray.Serialize();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return sdata;
        }

        public static TransformData ShareDeserialize(ShareSerializationData sdata)
        {
            if (sdata == null)
                return null;

            var tdata = new TransformData();
            try
            {
                tdata.flagArray = new ExSimpleNativeArray<ExBitFlag8>(sdata.flagArray);
                tdata.initLocalPositionArray = new ExSimpleNativeArray<float3>(sdata.initLocalPositionArray);
                tdata.initLocalRotationArray = new ExSimpleNativeArray<quaternion>(sdata.initLocalRotationArray);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return tdata;
        }

        //=========================================================================================
        /// <summary>
        /// PreBuild固有部分の保存データ
        /// </summary>
        [System.Serializable]
        public class UniqueSerializationData : ITransform
        {
            public Transform[] transformArray;

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
                if (transformArray != null)
                {
                    foreach (var t in transformArray)
                    {
                        if (t)
                            transformSet.Add(t);
                    }
                }
            }

            public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
            {
                if (transformArray != null)
                {
                    for (int i = 0; i < transformArray.Length; i++)
                    {
                        var t = transformArray[i];
                        if (t)
                        {
                            int id = t.GetInstanceID();
                            if (id != 0 && replaceDict.ContainsKey(id))
                            {
                                transformArray[i] = replaceDict[id];
                            }
                        }
                    }
                }
            }
        }

        public UniqueSerializationData UniqueSerialize()
        {
            var sdata = new UniqueSerializationData();
            try
            {
                sdata.transformArray = transformList.ToArray();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return sdata;
        }
    }
}
