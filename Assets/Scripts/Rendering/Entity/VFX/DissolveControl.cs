using System;
using System.Collections.Generic;
using UnityEngine;
using HSR.NPRShader;

namespace CraftSharp.Rendering
{
    public class DissolveControl : MonoBehaviour
    {
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
            if (original.HasTexture("_MainTex"))
            {
                created.SetTexture("_Texture", original.GetTexture("_MainTex"));
            }
            else if (original.HasTexture("_BaseMap"))
            {
                created.SetTexture("_Texture", original.GetTexture("_BaseMap"));
            }
            else if (original.HasTexture("_Texture"))
            {
                created.SetTexture("_Texture", original.GetTexture("_Texture"));
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

            var orginal2created = new Dictionary<Material, Material>();

            foreach (var renderer in m_Renderers)
            {
                var newMaterials = new List<Material>();

                foreach (var original in renderer.materials)
                {
                    if (orginal2created.ContainsKey(original))
                    {
                        newMaterials.Add(orginal2created[original]);
                    }
                    else
                    {
                        var created = CreateDissolveMaterial(original);
                        orginal2created.Add(original, created);

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