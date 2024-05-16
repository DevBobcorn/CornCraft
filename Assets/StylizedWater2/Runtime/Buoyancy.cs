//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

//#undef MATHEMATICS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

#if MATHEMATICS
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Vector4 = Unity.Mathematics.float4;
using Vector3 = Unity.Mathematics.float3;
using Vector2 = Unity.Mathematics.float2;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StylizedWater2
{
    public static partial class Buoyancy
    {
        private static WaveParameters waveParameters = new WaveParameters();
        private static Material lastMaterial;
        
        private static readonly int TimeParametersID = Shader.PropertyToID("_TimeParameters");
        
        private static void GetMaterialParameters(Material mat)
        {
            waveParameters.Update(mat);
        }
        
        //Returns the same value as _TimeParameters.x
        private static float _TimeParameters
        {
            get
            {
                if (WaterObject.CustomTime > 0) return WaterObject.CustomTime;
                
#if UNITY_EDITOR
                return Application.isPlaying ? Time.time : Shader.GetGlobalVector(TimeParametersID).x;
#else
                return Time.time;
#endif
            }
        }
        
        [Obsolete("Set the static 'WaterObject.CustomTime' parameter instead.", false)]
        public static void SetCustomTime(float value)
        {
            WaterObject.CustomTime = value;
        }

        private static Vector4 sine;
        private static Vector4 cosine;
        private static Vector4 dotABCD;
        private static Vector4 AB;
        private static Vector4 CD;
        private static Vector4 direction1;
        private static Vector4 direction2;
        private static Vector4 TIME;
        private static Vector2 planarPosition;
        
        private static Vector4 amp = new Vector4(0.3f, 0.35f, 0.25f, 0.25f);
        private static Vector4 freq = new Vector4(1.3f, 1.35f, 1.25f, 1.25f);
        private static Vector4 speed = new Vector4(1.2f, 1.375f, 1.1f, 1);
        private static Vector4 dir1 = new Vector4(0.3f, 0.85f, 0.85f, 0.25f);
        private static Vector4 dir2 = new Vector4(0.1f, 0.9f, -0.5f, -0.5f);
        private static Vector4 steepness = new Vector4(12f,12f,12f,12f);

        //Real frequency value per wave layer
        private static Vector4 frequency;
        //Output
        private static Vector3 offsets;

        /// <summary>
        /// Returns a position in world-space, where a ray cast from the origin in the direction hits the (flat) water level height
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="waterLevel">Water level height in world-space</param>
        /// <returns></returns>
        public static Vector3 FindWaterLevelIntersection(Vector3 origin, Vector3 direction, float waterLevel)
        {
            #if MATHEMATICS
            float upDot = dot(direction, UnityEngine.Vector3.up);
            float angle = (Mathf.Acos(upDot) * 180f) / Mathf.PI;

            float depth = waterLevel - origin.y;
            //Distance from origin to water level along direction
            float hypotenuse = depth / cos(Mathf.Deg2Rad * angle);
            
            return origin + (direction * hypotenuse);
            #else
            return Vector3.zero;
            #endif
        }

        /// <summary>
        /// Faux-raycast against the water surface
        /// </summary>
        /// <param name="waterObject">Water object component, used to get the water material and level (height)</param>
        /// <param name="origin">Ray origin</param>
        /// <param name="direction">Ray direction</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="hit">Reference to a RaycastHit, hit point and normal will be set</param>
        public static void Raycast(WaterObject waterObject, Vector3 origin, Vector3 direction,  bool dynamicMaterial, out RaycastHit hit)
        {
            Raycast(waterObject.material, waterObject.transform.position.y, origin, direction, dynamicMaterial, out hit);
        }

        private static RaycastHit hit = new RaycastHit();
        /// <summary>
        /// Faux-raycast against the water surface
        /// </summary>
        /// <param name="waterMat">Material using StylizedWater2 shader</param>
        /// <param name="waterLevel">Height of the reference water plane.</param>
        /// <param name="origin">Ray origin</param>
        /// <param name="direction">Ray direction</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="hit">Reference to a RaycastHit, hit point and normal will be set</param>
        public static void Raycast(Material waterMat, float waterLevel, Vector3 origin, Vector3 direction, bool dynamicMaterial, out RaycastHit hit)
        {
            Vector3 samplePos = FindWaterLevelIntersection(origin, direction, waterLevel);

            float waveHeight = SampleWaves(samplePos, waterMat, waterLevel, 1f, dynamicMaterial, out var normal);
            samplePos.y = waveHeight;

            hit = Buoyancy.hit;
            hit.normal = normal;
            hit.point = samplePos;
        }

        /// <summary>
        /// Given a position in world-space, returns the wave height and normal
        /// </summary>
        /// <param name="position">Sample position in world-space</param>
        /// <param name="waterObject">Water object component, used to get the water material and level (height)</param>
        /// <param name="rollStrength">Multiplier for the the normal strength</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="normal">Output upwards normal vector, perpendicular to the wave</param>
        /// <returns>Wave height, in world-space.</returns>
        public static float SampleWaves(UnityEngine.Vector3 position, WaterObject waterObject, float rollStrength, bool dynamicMaterial, out UnityEngine.Vector3 normal)
        {
            return SampleWaves(position, waterObject.material, waterObject.transform.position.y, rollStrength, dynamicMaterial, out normal);
        }

        private static void RecalculateParameters()
        {
            #if MATHEMATICS
            direction1 = dir1 * waveParameters.direction;
            direction2 = dir2 * waveParameters.direction;

            frequency = freq * (1-waveParameters.distance) * 3f;

            AB.x = steepness.x * waveParameters.steepness * direction1.x * amp.x;
            AB.y = steepness.x * waveParameters.steepness * direction1.y * amp.x;
            AB.z = steepness.x * waveParameters.steepness * direction1.z * amp.y;
            AB.w = steepness.x * waveParameters.steepness * direction1.w * amp.y;

            CD.x = steepness.z * waveParameters.steepness * direction2.x * amp.z;
            CD.y = steepness.z * waveParameters.steepness * direction2.y * amp.z;
            CD.z = steepness.w * waveParameters.steepness * direction2.z * amp.w;
            CD.w = steepness.w * waveParameters.steepness * direction2.w * amp.w;
            #endif
        }
        
        private static void SampleWaves(UnityEngine.Vector3 position, Material waterMat, float waterLevel, float rollStrength, bool dynamicMaterial, out UnityEngine.Vector3 offset, out UnityEngine.Vector3 normal)
        {
            Profiler.BeginSample("Buoyancy sampling");
            
            #if MATHEMATICS
			//If not desired to re-fetch the material properties every call, at least fetch them if the input material changed (since this is a static function)
			//In edit-mode, always do this as materials are most likely modified then
			if(!dynamicMaterial && Application.isPlaying)
			{
				//Fetch the material's wave parameters, so the exact calculations can be mirrored
				if (lastMaterial == null || lastMaterial.Equals(waterMat) == false)
				{
                    #if SWS_DEV
                    Debug.Log("SampleWaves: water material changed, re-fetching parameters");
                    #endif
                    
					GetMaterialParameters(waterMat);

                    lastMaterial = waterMat;
				}
			}
			else	
			{	
				GetMaterialParameters(waterMat);
            }

            TIME = (_TimeParameters * -waveParameters.animationSpeed * waveParameters.speed * speed);
            
            RecalculateParameters();

            offsets = Vector3.zero;
                
            planarPosition.x = position.x - WaterObject.PositionOffset.x;
            planarPosition.y = position.z - WaterObject.PositionOffset.z;

            for (int i = 0; i <= waveParameters.count; i++)
            {
                var t = 1f+((float)i / (float)waveParameters.count);

                frequency *= t;
                
                #if MATHEMATICS
                dotABCD.x = dot(direction1.xy, planarPosition) * frequency.x;
                dotABCD.y = dot(direction1.zw, planarPosition) * frequency.y;
                dotABCD.z = dot(direction2.xy, planarPosition) * frequency.z;
                dotABCD.w = dot(direction2.zw, planarPosition) * frequency.w;
                #endif

                sine.x = sin(dotABCD.x + TIME.x);
                sine.y = sin(dotABCD.y + TIME.y);
                sine.z = sin(dotABCD.z + TIME.z);
                sine.w = sin(dotABCD.w + TIME.w);

                cosine.x = cos(dotABCD.x + TIME.x);
                cosine.y = cos(dotABCD.y + TIME.y);
                cosine.z = cos(dotABCD.z + TIME.z);
                cosine.w = cos(dotABCD.w + TIME.w);
                
                offsets.x += dot(cosine, new Vector4(AB.x, AB.z, CD.x, CD.z));
                offsets.y += dot(sine, amp);
                offsets.z += dot(cosine, new Vector4(AB.y, AB.w, CD.y, CD.w));
            }
            
            rollStrength *= lerp(0.001f, 0.1f, waveParameters.steepness);

			normal.x = -offsets.x * rollStrength * waveParameters.height;
			normal.y = 2f;
			normal.z = -offsets.z * rollStrength * waveParameters.height;
            
            normal = normalize(normal);

            //Average height
            offsets.y /= waveParameters.count;
            offsets.y = (offsets.y* waveParameters.height) + waterLevel;

            offset = offsets;
            #else
            offset = Vector3.zero;
            normal = Vector3.zero;
            #endif
            
            Profiler.EndSample();
        }
        
        private static UnityEngine.Vector3 m_offset;
        /// <summary>
        /// Given a position in world-space, returns the wave height and normal
        /// </summary>
        /// <param name="position">Sample position in world-space</param>
        /// <param name="waterMat">Material using StylizedWater2 shader</param>
        /// <param name="waterLevel">Height of the reference water plane.</param>
        /// <param name="rollStrength">Multiplier for the the normal strength</param>
        /// <param name="dynamicMaterial">If true, the material's wave parameters will be re-fetched with every function call</param>
        /// <param name="normal">Output upwards normal vector, perpendicular to the wave</param>
        /// <returns>Wave height, in world-space.</returns>
        public static float SampleWaves(UnityEngine.Vector3 position, Material waterMat, float waterLevel, float rollStrength, bool dynamicMaterial, out UnityEngine.Vector3 normal)
        {
            SampleWaves(position, waterMat, waterLevel, rollStrength, dynamicMaterial, out m_offset, out normal);

            return m_offset.y;
        }
        
        /// <summary>
        /// Checks if the position is below the maximum possible wave height. Can be used as a fast broad-phase check, before actually using the more expensive SampleWaves function
        /// </summary>
        /// <param name="position"></param>
        /// <param name="waterObject"></param>
        /// <returns></returns>
        public static bool CanTouchWater(Vector3 position, WaterObject waterObject)
        {
            if (!waterObject) return false;
            
            return position.y < (waterObject.transform.position.y + WaveParameters.GetMaxWaveHeight(waterObject.material));
        }
        
        /// <summary>
        /// Checks if the position is below the maximum possible wave height. Can be used as a fast broad-phase check, before actually using the more expensive SampleWaves function
        /// </summary>
        public static bool CanTouchWater(Vector3 position, Material waterMaterial, float waterLevel)
        {
            return position.y < (waterLevel + WaveParameters.GetMaxWaveHeight(waterMaterial));
        }
    }
}