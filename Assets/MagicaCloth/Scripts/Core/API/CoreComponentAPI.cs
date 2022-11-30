// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp

using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// CoreComponent API
    /// </summary>
    public abstract partial class CoreComponent : BaseComponent, IShareDataObject, IDataVerify, IEditorMesh, IEditorCloth, IDataHash, IBoneReplace
    {
        /// <summary>
        /// コンポーネントで使用されているすべてのボーンを返します。
        /// Returns all bones used in the component.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Transform> GetUsedComponentBones()
        {
            var boneDict = new Dictionary<string, Transform>();
            var bones = GetUsedBones();
            foreach (var t in bones)
                if (t)
                {
                    if (boneDict.ContainsKey(t.name))
                    {
                        if (boneDict[t.name] != t)
                            Debug.LogWarning($"The bone name is duplicated!:{t.name}");
                    }
                    else
                        boneDict.Add(t.name, t);
                }
            return boneDict;
        }

        /// <summary>
        /// コンポーネントで使用されているすべてのボーンの名前を返します。
        /// Returns all bone names used in the component.
        /// </summary>
        /// <returns></returns>
        public List<string> GetUsedComponentBoneNames()
        {
            return new List<string>(GetUsedComponentBones().Keys);
        }

        /// <summary>
        /// コンポーネントのボーンを入れ替え再セットアップします。
        /// Swap component bones and set up again.
        /// https://magicasoft.jp/en/corecomponent-2/
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public void ReplaceComponentBone(Dictionary<Transform, Transform> boneReplaceDict)
        {
            ChangeAvatar(boneReplaceDict);
        }

        /// <summary>
        /// コンポーネントのボーンを入れ替え再セットアップします。
        /// Swap component bones and set up again.
        /// https://magicasoft.jp/en/corecomponent-2/
        /// </summary>
        /// <param name="boneReplaceDict"></param>
        public void ReplaceComponentBone(Dictionary<string, Transform> boneReplaceDict)
        {
            ChangeAvatar(boneReplaceDict);
        }
    }
}
