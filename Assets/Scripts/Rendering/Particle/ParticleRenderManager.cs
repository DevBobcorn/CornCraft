using System;
using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Event;

namespace CraftSharp.Rendering
{
    public class ParticleRenderManager : MonoBehaviour
    {
        private readonly Dictionary<ParticleExtraDataType, IParticleRender> particleRenders = new();

        #nullable enable

        private Action<ParticleEvent>? particleCallback;

        private Action<ParticlesEvent>? particlesCallback;

        #nullable disable

        void Start()
        {
            var blockParticleRenderObj = new GameObject("Block Particle Render");
            blockParticleRenderObj.transform.SetParent(transform);
            var blockParticleRender = blockParticleRenderObj.AddComponent<BlockParticleRender>();
            particleRenders.Add(ParticleExtraDataType.Block, blockParticleRender);

            particleCallback = (e) => {
                var particleType = ParticleTypePalette.INSTANCE.GetByNumId(e.TypeNumId);

                if (particleRenders.TryGetValue(particleType.ExtraDataType, out IParticleRender render))
                {
                    render.AddParticle(e.Position, e.TypeNumId, e.ExtraData);
                }
            };

            particlesCallback = (e) => {
                var particleType = ParticleTypePalette.INSTANCE.GetByNumId(e.TypeNumId);

                if (particleRenders.TryGetValue(particleType.ExtraDataType, out IParticleRender render))
                {
                    render.AddParticles(e.Position, e.TypeNumId, e.ExtraData, e.Count);
                }
            };

            EventManager.Instance.Register(particleCallback);
            EventManager.Instance.Register(particlesCallback);
        }

        void OnDestroy()
        {
            if (particleCallback is not null)
                EventManager.Instance.Unregister(particleCallback);
            
            if (particlesCallback is not null)
                EventManager.Instance.Unregister(particlesCallback);
            
        }
    }
}