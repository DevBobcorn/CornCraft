﻿// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Serialize data (1)
    /// function part.
    /// </summary>
    public partial class ClothSerializeData : IDataValidate, IValid, ITransform
    {
        public ClothSerializeData()
        {
        }

        /// <summary>
        /// クロスを構築するための最低限の情報が揃っているかチェックする
        /// Check if you have the minimum information to construct the cloth.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (clothType == ClothProcess.ClothType.BoneCloth)
            {
                if (rootBones == null || rootBones.Count == 0)
                    return false;
                if (rootBones.Count(x => x != null) == 0)
                    return false;
            }
            else if (clothType == ClothProcess.ClothType.MeshCloth)
            {
                if (sourceRenderers == null || sourceRenderers.Count == 0)
                    return false;
                if (sourceRenderers.Count(x => x != null) == 0)
                    return false;
            }
            else
                return false;

            return true;
        }

        public void DataValidate()
        {
            rotationalInterpolation = Mathf.Clamp01(rotationalInterpolation);
            rootRotation = Mathf.Clamp01(rootRotation);
            animationPoseRatio = Mathf.Clamp01(animationPoseRatio);

            reductionSetting.DataValidate();
            customSkinningSetting.DataValidate();
            normalAlignmentSetting.DataValidate();

            gravity = Mathf.Clamp(gravity, 0.0f, 20.0f);
            if (math.length(gravityDirection) > Define.System.Epsilon)
                gravityDirection = math.normalize(gravityDirection);
            else
                gravityDirection = 0;
            gravityFalloff = Mathf.Clamp01(gravityFalloff);
            stablizationTimeAfterReset = Mathf.Clamp01(stablizationTimeAfterReset);
            blendWeight = Mathf.Clamp01(blendWeight);

            damping.DataValidate(0.0f, 1.0f);
            radius.DataValidate(0.001f, 1.0f);
            inertiaConstraint.DataValidate();
            tetherConstraint.DataValidate();
            distanceConstraint.DataValidate();
            triangleBendingConstraint.DataValidate();
            angleRestorationConstraint.DataValidate();
            angleLimitConstraint.DataValidate();
            motionConstraint.DataValidate();
            colliderCollisionConstraint.DataValidate();
            selfCollisionConstraint.DataValidate();
            wind.DataValidate();
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// Hashcode for determining editor mesh updates.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 0;
            hash += (int)clothType;
            foreach (var ren in sourceRenderers)
                hash += ren?.GetInstanceID() ?? 0;
            foreach (var t in rootBones)
            {
                var stack = new Stack<Transform>(30);
                stack.Push(t);
                while (stack.Count > 0)
                {
                    var t2 = stack.Pop();
                    if (t2 == null)
                        continue;
                    hash += t2.GetInstanceID();
                    hash += t2.localPosition.GetHashCode();
                    hash += t2.localRotation.GetHashCode();
                    int cnt = t2.childCount;
                    for (int i = 0; i < cnt; i++)
                        stack.Push(t2.GetChild(i));
                }
            }
            hash += (int)connectionMode * 10;
            hash += reductionSetting.GetHashCode();
            hash += customSkinningSetting.GetHashCode();
            hash += normalAlignmentSetting.GetHashCode();
            hash += (int)paintMode;
            foreach (var map in paintMaps)
            {
                if (map)
                {
                    hash += map.GetInstanceID();
                    hash += map.isReadable ? 1 : 0;
                }
            }

            return hash;
        }

        /// <summary>
        /// ジョブで参照する構造体に変換して返す
        /// Convert to a structure to be referenced in the job and return.
        /// </summary>
        /// <returns></returns>
        public ClothParameters GetClothParameters()
        {
            var cparams = new ClothParameters();

            //cparams.solverFrequency = Define.System.SolverFrequency;
            cparams.gravity = gravity;
            cparams.gravityDirection = gravityDirection;
            cparams.gravityFalloff = gravityFalloff;
            cparams.stablizationTimeAfterReset = stablizationTimeAfterReset;
            cparams.blendWeight = blendWeight;
            cparams.dampingCurveData = damping.ConvertFloatArray() * 0.2f; // 20%
            cparams.radiusCurveData = radius.ConvertFloatArray();
            cparams.normalAxis = normalAxis;

            cparams.rotationalInterpolation = rotationalInterpolation;
            cparams.rootRotation = rootRotation;

            cparams.inertiaConstraint.Convert(inertiaConstraint);
            cparams.tetherConstraint.Convert(tetherConstraint);
            cparams.distanceConstraint.Convert(distanceConstraint);
            cparams.triangleBendingConstraint.Convert(triangleBendingConstraint);
            cparams.angleConstraint.Convert(angleRestorationConstraint, angleLimitConstraint);
            cparams.motionConstraint.Convert(motionConstraint);
            cparams.colliderCollisionConstraint.Convert(colliderCollisionConstraint);
            cparams.selfCollisionConstraint.Convert(selfCollisionConstraint);
            cparams.wind.Convert(wind);

            return cparams;
        }

        class TempBuffer
        {
            ClothProcess.ClothType clothType;
            List<Renderer> sourceRenderers;
            PaintMode paintMode;
            List<Texture2D> paintMaps;
            List<Transform> rootBones;
            RenderSetupData.BoneConnectionMode connectionMode;
            float rotationalInterpolation;
            float rootRotation;
            ClothUpdateMode updateMode;
            float animationPoseRatio;
            ReductionSettings reductionSetting;
            CustomSkinningSettings customSkinningSetting;
            NormalAlignmentSettings normalAlignmentSetting;
            ClothNormalAxis normalAxis;
            List<ColliderComponent> colliderList;
            MagicaCloth synchronization;
            float stablizationTimeAfterReset;
            float blendWeight;

            internal TempBuffer(ClothSerializeData sdata)
            {
                Push(sdata);
            }

            internal void Push(ClothSerializeData sdata)
            {
                clothType = sdata.clothType;
                sourceRenderers = new List<Renderer>(sdata.sourceRenderers);
                paintMode = sdata.paintMode;
                paintMaps = new List<Texture2D>(sdata.paintMaps);
                rootBones = new List<Transform>(sdata.rootBones);
                connectionMode = sdata.connectionMode;
                rotationalInterpolation = sdata.rotationalInterpolation;
                rootRotation = sdata.rootRotation;
                updateMode = sdata.updateMode;
                animationPoseRatio = sdata.animationPoseRatio;
                reductionSetting = sdata.reductionSetting.Clone();
                customSkinningSetting = sdata.customSkinningSetting.Clone();
                normalAlignmentSetting = sdata.normalAlignmentSetting.Clone();
                normalAxis = sdata.normalAxis;
                colliderList = new List<ColliderComponent>(sdata.colliderCollisionConstraint.colliderList);
                synchronization = sdata.selfCollisionConstraint.syncPartner;
                stablizationTimeAfterReset = sdata.stablizationTimeAfterReset;
                blendWeight = sdata.blendWeight;
            }

            internal void Pop(ClothSerializeData sdata)
            {
                sdata.clothType = clothType;
                sdata.sourceRenderers = sourceRenderers;
                sdata.paintMode = paintMode;
                sdata.paintMaps = paintMaps;
                sdata.rootBones = rootBones;
                sdata.connectionMode = connectionMode;
                sdata.rotationalInterpolation = rotationalInterpolation;
                sdata.rootRotation = rootRotation;
                sdata.updateMode = updateMode;
                sdata.animationPoseRatio = animationPoseRatio;
                sdata.reductionSetting = reductionSetting;
                sdata.customSkinningSetting = customSkinningSetting;
                sdata.normalAlignmentSetting = normalAlignmentSetting;
                sdata.normalAxis = normalAxis;
                sdata.colliderCollisionConstraint.colliderList = colliderList;
                sdata.selfCollisionConstraint.syncPartner = synchronization;
                sdata.stablizationTimeAfterReset = stablizationTimeAfterReset;
                sdata.blendWeight = blendWeight;
            }
        }

        /// <summary>
        /// パラメータをJsonへエクスポートする
        /// Export parameters to Json.
        /// </summary>
        /// <returns></returns>
        public string ExportJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// パラメータをJsonからインポートする
        /// Parameterブロックの値型のみがインポートされる
        /// Import parameters from Json.
        /// Only value types of Parameter blocks are imported.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public bool ImportJson(string json)
        {
            try
            {
                // 上書きしないプロパティを保持
                var temp = new TempBuffer(this);

                // Import
                JsonUtility.FromJsonOverwrite(json, this);

                // 上書きしないプロパティを書き戻し
                temp.Pop(this);

                // 検証
                DataValidate();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// 別のシリアライズデータからインポートする
        /// Import from another serialized data.
        /// </summary>
        /// <param name="sdata"></param>
        /// <param name="deepCopy">true = Copy all, false = parameter only</param>
        public void Import(ClothSerializeData sdata, bool deepCopy = false)
        {
            TempBuffer temp = deepCopy ? null : new TempBuffer(this);

            if (deepCopy)
            {
                clothType = sdata.clothType;
                sourceRenderers = new List<Renderer>(sdata.sourceRenderers);
                paintMode = sdata.paintMode;
                paintMaps = new List<Texture2D>(sdata.paintMaps);
                rootBones = new List<Transform>(sdata.rootBones);
                connectionMode = sdata.connectionMode;
                rotationalInterpolation = sdata.rotationalInterpolation;
                rootRotation = sdata.rootRotation;
                updateMode = sdata.updateMode;
                animationPoseRatio = sdata.animationPoseRatio;
                reductionSetting = sdata.reductionSetting.Clone();
                customSkinningSetting = sdata.customSkinningSetting.Clone();
                normalAlignmentSetting = sdata.normalAlignmentSetting.Clone();
                normalAxis = sdata.normalAxis;
                stablizationTimeAfterReset = sdata.stablizationTimeAfterReset;
                blendWeight = sdata.blendWeight;
            }

            // parameters
            gravity = sdata.gravity;
            gravityDirection = sdata.gravityDirection;
            gravityFalloff = sdata.gravityFalloff;
            damping = sdata.damping.Clone();
            radius = sdata.radius.Clone();
            inertiaConstraint = sdata.inertiaConstraint.Clone();
            tetherConstraint = sdata.tetherConstraint.Clone();
            distanceConstraint = sdata.distanceConstraint.Clone();
            triangleBendingConstraint = sdata.triangleBendingConstraint.Clone();
            angleRestorationConstraint = sdata.angleRestorationConstraint.Clone();
            angleLimitConstraint = sdata.angleLimitConstraint.Clone();
            motionConstraint = sdata.motionConstraint.Clone();
            colliderCollisionConstraint = sdata.colliderCollisionConstraint.Clone();
            selfCollisionConstraint = sdata.selfCollisionConstraint.Clone();
            wind = sdata.wind.Clone();

            if (deepCopy == false)
                temp.Pop(this);
        }

        /// <summary>
        /// 別のクロスコンポーネントからインポートする
        /// Import from another cloth component.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="deepCopy"></param>
        public void Import(MagicaCloth src, bool deepCopy = false)
        {
            Import(src.SerializeData, deepCopy);
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            foreach (var t in rootBones)
            {
                if (t)
                    transformSet.Add(t);
            }
            customSkinningSetting.GetUsedTransform(transformSet);
            normalAlignmentSetting.GetUsedTransform(transformSet);
        }

        public void ReplaceTransform(Dictionary<int, Transform> replaceDict)
        {
            for (int i = 0; i < rootBones.Count; i++)
            {
                var t = rootBones[i];
                if (t && replaceDict.ContainsKey(t.GetInstanceID()))
                {
                    rootBones[i] = replaceDict[t.GetInstanceID()];
                }
            }

            customSkinningSetting.ReplaceTransform(replaceDict);
            normalAlignmentSetting.ReplaceTransform(replaceDict);
        }
    }
}
