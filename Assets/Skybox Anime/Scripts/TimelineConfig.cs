using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimeSkybox
{
    public class TimelineConfig : MonoBehaviour
    {
        [Range(0f, 10f)]
        public float m_speed = 1.0f;

        private PlayableDirector playableDirector;

        // Start is called before the first frame update
        void Start()
        {
            playableDirector = GetComponent<PlayableDirector>();
            SetPlayableSpeed();
        }

        private void SetPlayableSpeed()
        {
            if (playableDirector != null)
            {
                var playableGraph = playableDirector.playableGraph;
                
                if (!playableGraph.IsValid())
                {
                    playableDirector.RebuildGraph();
                }

                if (playableGraph.IsValid())
                {
                    playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(m_speed);
                }
            }
        }
    }
}