using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimeSkybox
{
    public class AnimeSkyboxAsset : PlayableAsset
    {
        public List<MaterialProperty> properties; // 用于存储所有需要调整的属性

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AnimeSkyboxBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.properties = properties;

            return playable;
        }
    }
}