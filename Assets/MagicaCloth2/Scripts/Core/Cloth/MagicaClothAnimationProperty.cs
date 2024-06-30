// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp

using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// プロパティをアニメーションから制御するためのラッパー.
    /// Wrapper for controlling properties from animation.
    /// </summary>
    public partial class MagicaCloth
    {
        [HideInInspector]
        public float animationPoseRatioProperty;
        float _animationPoseRatioProperty;

        [HideInInspector]
        public float gravityProperty;
        float _gravityProperty;

        [HideInInspector]
        public float dampingProperty;
        float _dampingProperty;

        [HideInInspector]
        public float worldInertiaProperty;
        float _worldInertiaProperty;

        [HideInInspector]
        public float localInertiaProperty;
        float _localInertiaProperty;

        [HideInInspector]
        public float windInfluenceProperty;
        float _windInfluenceProperty;

        [HideInInspector]
        public float blendWeightProperty;
        float _blendWeightProperty;

        //=========================================================================================
        internal void InitAnimationProperty()
        {
            animationPoseRatioProperty = serializeData.animationPoseRatio;
            _animationPoseRatioProperty = animationPoseRatioProperty;

            gravityProperty = serializeData.gravity;
            _gravityProperty = gravityProperty;

            dampingProperty = serializeData.damping.value;
            _dampingProperty = dampingProperty;

            worldInertiaProperty = serializeData.inertiaConstraint.worldInertia;
            _worldInertiaProperty = worldInertiaProperty;

            localInertiaProperty = serializeData.inertiaConstraint.localInertia;
            _localInertiaProperty = localInertiaProperty;

            windInfluenceProperty = serializeData.wind.influence;
            _windInfluenceProperty = windInfluenceProperty;

            blendWeightProperty = serializeData.blendWeight;
            _blendWeightProperty = blendWeightProperty;
        }

        /// <summary>
        /// アニメーションによりMagicaClothのプロパティが変更されたときに呼び出される.
        /// Called when a property of MagicaCloth changes due to animation.
        /// </summary>
        void OnDidApplyAnimationProperties()
        {
            if (Application.isPlaying)
            {
                //Debug.Log($"Animated property changes. F:{Time.frameCount}");

                if (animationPoseRatioProperty != _animationPoseRatioProperty)
                {
                    _animationPoseRatioProperty = animationPoseRatioProperty;
                    serializeData.animationPoseRatio = animationPoseRatioProperty;
                    SetParameterChange();
                }

                if (gravityProperty != _gravityProperty)
                {
                    _gravityProperty = gravityProperty;
                    serializeData.gravity = gravityProperty;
                    SetParameterChange();
                }

                if (dampingProperty != _dampingProperty)
                {
                    _dampingProperty = dampingProperty;
                    serializeData.damping.value = dampingProperty;
                    SetParameterChange();
                }

                if (worldInertiaProperty != _worldInertiaProperty)
                {
                    _worldInertiaProperty = worldInertiaProperty;
                    serializeData.inertiaConstraint.worldInertia = worldInertiaProperty;
                    SetParameterChange();
                }

                if (localInertiaProperty != _localInertiaProperty)
                {
                    _localInertiaProperty = localInertiaProperty;
                    serializeData.inertiaConstraint.localInertia = localInertiaProperty;
                    SetParameterChange();
                }

                if (windInfluenceProperty != _windInfluenceProperty)
                {
                    _windInfluenceProperty = windInfluenceProperty;
                    serializeData.wind.influence = windInfluenceProperty;
                    SetParameterChange();
                }

                if (blendWeightProperty != _blendWeightProperty)
                {
                    _blendWeightProperty = blendWeightProperty;
                    serializeData.blendWeight = blendWeightProperty;
                    SetParameterChange();
                }
            }
        }
    }
}

