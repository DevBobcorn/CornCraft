using System;
using System.Collections.Generic;
using UnityEngine;
using HSR.NPRShader;

namespace CraftSharp.Rendering
{
    public class DissolveControl : MonoBehaviour
    {
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
        private static readonly int TEXTURE = Shader.PropertyToID("_Texture");
        private static readonly int BASE_MAP = Shader.PropertyToID("_BaseMap");

        public Material dissolveMaterial;
        [NonSerialized] private readonly List<Renderer> m_Renderers = new();
        [NonSerialized] private readonly List<Material> m_DissolveMaterials = new();

        public string m_MaterialProperty = "_Dissolve";
        public float m_fSpeed = 1.0f;
        [Range(0,1.0f)]
        public float m_fRange = 0.0f;
        public bool m_bUseTime = true;

        private Material CreateDissolveMaterial(Material original)
        {
            var created = new Material(dissolveMaterial);

            // Set main texture from original material
            if (original.HasTexture(MAIN_TEX))
            {
                created.SetTexture(TEXTURE, original.GetTexture(MAIN_TEX));
            }
            else if (original.HasTexture(BASE_MAP))
            {
                created.SetTexture(TEXTURE, original.GetTexture(BASE_MAP));
            }
            else if (original.HasTexture(TEXTURE))
            {
                created.SetTexture(TEXTURE, original.GetTexture(TEXTURE));
            }
            
            return created;
        }

        private void Start()
        {
            // Disable HSR render control
            StarRailCharacterRenderingController hsr;

            if (hsr = GetComponent<StarRailCharacterRenderingController>())
            {
                hsr.enabled = false;
            }

            // Gather all renderers
            GetComponentsInChildren(true, m_Renderers);

            m_DissolveMaterials.Clear();

            var original2created = new Dictionary<Material, Material>();

            foreach (var renderer in m_Renderers)
            {
                var newMaterials = new List<Material>();

                foreach (var original in renderer.materials)
                {
                    if (original2created.TryGetValue(original, out var created))
                    {
                        newMaterials.Add(created);
                    }
                    else
                    {
                        created = CreateDissolveMaterial(original);
                        original2created.Add(original, created);

                        newMaterials.Add(created);
                        m_DissolveMaterials.Add(created);
                    }
                }

                renderer.SetSharedMaterials(newMaterials);
            }
        }

        private void Update()
        {
            if( m_bUseTime )
            {
                foreach (var material in m_DissolveMaterials)
                {
                    material.SetFloat( m_MaterialProperty, Mathf.Clamp( Mathf.Abs( Mathf.Sin( Time.time / m_fSpeed ) ), 0.0f, 1.0f ) );
                }
            }
            else
            {
                foreach (var material in m_DissolveMaterials)
                {
                    material.SetFloat( m_MaterialProperty, m_fRange );
                }
            }
        }
    }
}