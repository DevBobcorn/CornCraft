//Distant Lands 2022



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    public abstract class FXProfile : ScriptableObject
    {

        [TransitionTime]
        [Tooltip("A curve modifier that is used to impact the speed of the transition for this effect.")]
        public AnimationCurve transitionTimeModifier = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        public VFXModule VFXMod;


        /// <summary>
        /// Plays the Cozy effect at maximum intensity.
        /// </summary>  
        public abstract void PlayEffect();

        /// <summary>
        /// Plays the Cozy effect at a set intensity.
        /// </summary>      
        /// <param name="weight">The weight (or intensity percentage) that this effect will play at. From 0.0 to 1.0</param>
        public abstract void PlayEffect(float weight);

        /// <summary>
        /// Stops the Cozy effect completely..
        /// </summary>  
        public abstract void StopEffect();

        /// <summary>
        /// Called to instantiate the Cozy effect.
        /// </summary>                                                                                                          
        /// <param name="VFX">Holds a reference to the Cozy Weather VFX Module.</param>
        public abstract bool InitializeEffect(VFXModule VFX);

    }


#if UNITY_EDITOR
    [CustomEditor(typeof(FXProfile))]
    [CanEditMultipleObjects]
    public abstract class E_FXProfile : Editor
    {

        public abstract float GetLineHeight();

        public abstract void RenderInWindow(Rect pos);

    }
#endif
}