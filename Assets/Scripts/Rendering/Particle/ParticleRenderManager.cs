using System;
using System.Collections.Generic;
using UnityEngine;

using CraftSharp.Event;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class ParticleRenderManager : MonoBehaviour
    {
        private readonly Dictionary<ParticleExtraDataType, IParticleRender> particleRenders = new();

        private bool initialized = false;

        #nullable enable

        private Action<ParticlesEvent>? particlesCallback;

        #nullable disable

        private void EnsureInitialized()
        {
            if (initialized || !ResourcePackManager.Instance.Loaded) return;

            initialized = true;

            foreach (var render in particleRenders.Values)
            {
                render.Initialize();
            }
        }

        private void Start()
        {
            var blockParticleRenderObj = new GameObject("Block Particle Render");
            blockParticleRenderObj.transform.SetParent(transform);
            var blockParticleRender = blockParticleRenderObj.AddComponent<BlockParticleRender>();

            particleRenders.Add(ParticleExtraDataType.Block, blockParticleRender);

            particlesCallback = (e) =>
            {
                if (!initialized || !ResourcePackManager.Instance.Loaded) return;

                var particleType = ParticleTypePalette.INSTANCE.GetByNumId(e.TypeNumId);

                if (particleRenders.TryGetValue(particleType.ExtraDataType, out IParticleRender render))
                {
                    render.AddParticles(e.Position, e.TypeNumId, e.ExtraData, e.Count);
                }
            };

            EventManager.Instance.Register(particlesCallback);
        }

        private void Update()
        {
            EnsureInitialized();

            if (!initialized) return;

            foreach (var render in particleRenders.Values)
            {
                render.ManagedUpdate();
            }
        }

        private void OnDestroy()
        {
            if (particlesCallback is not null)
                EventManager.Instance.Unregister(particlesCallback);
        }
    }
}