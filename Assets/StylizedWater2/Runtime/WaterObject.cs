using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StylizedWater2
{
    /// <summary>
    /// Attached to every mesh using the Stylized Water 2 shader
    /// Provides a generic way of identifying water objects and accessing their properties
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Stylized Water 2/Water Object")]
    [DisallowMultipleComponent]
    public class WaterObject : MonoBehaviour
    {
        /// <summary>
        /// Collection of all available WaterObject instances. Instances (un)register themselves in the OnEnable/OnDisable functions.
        /// </summary>
        public static readonly List<WaterObject> Instances = new List<WaterObject>();
        
        public Material material;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        private static Vector3 s_PositionOffset;
        private static readonly int _WaterPositionOffset = Shader.PropertyToID("_WaterPositionOffset");
        
        /// <summary>
        /// For use with floating-point origin systems. In the shader, the world-position (used for UV coordinates) will be offset by this value.
        /// Buoyancy calculations will also be offset to stay in sync.
        /// </summary>
        public static Vector3 PositionOffset
        {
            set
            {
                s_PositionOffset = value;
                Shader.SetGlobalVector(_WaterPositionOffset, s_PositionOffset);
            }
            internal get => s_PositionOffset;
        }
        
        private static float m_customTimeValue = -1f;
        private static readonly int CustomTimeID = Shader.PropertyToID("_CustomTime");

        /// <summary>
        /// Pass in any time value, any kind of animations will use this as a time index, including wave animations (and thus buoyancy calculations as well).
        /// This is typically used for network synchronized waves or cutscenes.
        /// To revert to using normal <see cref="Time.time"/>, pass in a value lower than <c>0</c>.
        /// </summary>
        /// <param name="value"></param>
        public static float CustomTime
        {
            set
            {
                m_customTimeValue = value;
                Shader.SetGlobalFloat(CustomTimeID, m_customTimeValue);
            }
            get => m_customTimeValue;
        }
        
        private MaterialPropertyBlock _props;
        public MaterialPropertyBlock props
        {
            get
            {
                //Fetch when required, execution order makes it unreliable otherwise
                if (_props == null)
                {
                    CreatePropertyBlock(meshRenderer);
                }
                return _props;
            }
            private set => _props = value;
        }

        private void CreatePropertyBlock(Renderer sourceRenderer)
        {
            _props = new MaterialPropertyBlock();
            sourceRenderer.GetPropertyBlock(_props);
        }

        private void Reset()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            CreatePropertyBlock(meshRenderer);
            meshFilter = GetComponent<MeshFilter>();
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        private void OnValidate()
        {
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            FetchWaterMaterial();
        }

        /// <summary>
        /// Grabs the material from the attached Mesh Renderer
        /// </summary>
        public Material FetchWaterMaterial()
        {
            if (meshRenderer)
            {
                material = meshRenderer.sharedMaterial;
                return material;
            }

            return null;
        }

        /// <summary>
        /// Applies to changes made to the Material Property Blocks ('props' property)
        /// </summary>
        public void ApplyInstancedProperties()
        {
            if(props != null) meshRenderer.SetPropertyBlock(props);
        }

        /// <summary>
        /// Checks if the position is below the maximum possible wave height. Can be used as a fast broad-phase check, before actually using the more expensive SampleWaves function
        /// </summary>
        /// <param name="position"></param>
        public bool CanTouch(Vector3 position)
        {
            return Buoyancy.CanTouchWater(position, this);
        }

        public void AssignMesh(Mesh mesh)
        {
            if (meshFilter) meshFilter.sharedMesh = mesh;
        }

        public void AssignMaterial(Material newMaterial)
        {
            if (meshRenderer) meshRenderer.sharedMaterial = newMaterial;
            material = newMaterial;
        }

        /// <summary>
        /// Creates a new GameObject with a MeshFilter, MeshRenderer and WaterObject component
        /// </summary>
        /// <param name="waterMaterial">If assigned, this material is automatically added to the MeshRenderer</param>
        /// <returns></returns>
        public static WaterObject New(Material waterMaterial = null, Mesh mesh = null)
        {
            GameObject go = new GameObject("Water Object", typeof(MeshFilter), typeof(MeshRenderer), typeof(WaterObject));
            go.layer = LayerMask.NameToLayer("Water");
            
            #if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Created Water Object");
            #endif
            
            WaterObject waterObject = go.GetComponent<WaterObject>();
            
            waterObject.meshRenderer = waterObject.gameObject.GetComponent<MeshRenderer>();
            waterObject.meshFilter = waterObject.gameObject.GetComponent<MeshFilter>();
            
            waterObject.meshFilter.sharedMesh = mesh;
            waterObject.meshRenderer.sharedMaterial = waterMaterial;
            waterObject.meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            waterObject.material = waterMaterial;

            return waterObject;
        }

        /// <summary>
        /// Attempt to find the WaterObject above or below the position. Checks against the bounds of ALL Water Object meshes by raycasting on the XZ plane
        /// </summary>
        /// <param name="position">Position in world-space (height is not relevant)</param>
        /// <param name="rotationSupport">Unless this is true, water rotated on the Y-axis will yield incorrect results (but is faster)</param>
        /// <returns></returns>
        public static WaterObject Find(Vector3 position, bool rotationSupport)
        {
            Ray ray = new Ray(position + (Vector3.up * 1000f), Vector3.down);
            
            foreach (WaterObject obj in Instances)
            {
                if (rotationSupport)
                {
                    //Local space
                    ray.origin = obj.transform.InverseTransformPoint(ray.origin);
                    if (obj.meshFilter.sharedMesh.bounds.IntersectRay(ray)) return obj;
                }
                else
                {
                    //Axis-aligned bounds
                    if (obj.meshRenderer.bounds.IntersectRay(ray)) return obj;
                }
            }
            
            return null;
        }
    }
}