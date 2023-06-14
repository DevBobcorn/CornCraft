using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace Enviro
{
    [Serializable]
    public class EnviroReflections
    {
        public enum GlobalReflectionResolution
        {
            R16,
            R32,
            R64,
            R128,
            R256,
            R512,
            R1024,
            R2048
        }

        [Tooltip("Set if enviro reflection probe should use custom rendering setup. For example to include post effectsin birp.")]
        public bool customRendering = true;
        [Tooltip("Set to use custom timeslicing when rendered in custom mode.")]
        public bool customRenderingTimeSlicing = true;

        [Tooltip("Set if enviro reflection probe should update faces individual on different frames.")]
        public ReflectionProbeTimeSlicingMode globalReflectionTimeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        [Tooltip("Enable/disable enviro reflection probe updates based on gametime changes..")]
        public bool globalReflectionsUpdateOnGameTime = true;
        [Tooltip("Enable/disable enviro reflection probe updates based on transform position changes..")]
        public bool globalReflectionsUpdateOnPosition = true;
        [Tooltip("Reflection probe intensity.")]
        [Range(0f, 2f)]
        public float globalReflectionsIntensity = 1.0f;
        [Tooltip("Reflection probe update rate based on game time.")]
        public float globalReflectionsTimeTreshold = 0.025f;
        [Tooltip("Reflection probe update rate based on camera position.")]
        public float globalReflectionsPositionTreshold = 0.5f;
        [Tooltip("Reflection probe scale. Increase that one to increase the area where reflection probe will influence your scene.")]
        [Range(10f, 10000f)]
        public float globalReflectionsScale = 10000f;
        [Tooltip("Reflection probe resolution.")]
        public GlobalReflectionResolution globalReflectionResolution = GlobalReflectionResolution.R256;
        [Tooltip("Reflection probe rendered Layers.")]
        public LayerMask globalReflectionLayers;
        [Tooltip("Enable this option to update the default reflection with global reflection probes cubemap. This can be needed for material that might not support direct reflection probes. (Instanced Indirect Rendering)")]
        public bool updateDefaultEnvironmentReflections = true;
        [Tooltip("Reflection cubemap used for default scene sky reflections in < Unity 2022.1 versions.")]
        public Cubemap defaultSkyReflectionTex;
    }

    [Serializable]
    [ExecuteInEditMode]
    public class EnviroReflectionsModule : EnviroModule
    {
        public Enviro.EnviroReflections Settings;
        public EnviroReflectionsModule preset;

        // Inspector
        public bool showReflectionControls;

        public float lastReflectionUpdate;
        public Vector3 lastReflectionUpdatePos;

        private Coroutine renderReflectionCoroutine;
        private Coroutine waitForProbeCoroutine;
        private Coroutine copyDefaultReflectionCoroutine;

        public override void Enable ()
        {
            if(EnviroManager.instance == null)
               return;

            Setup();

            // Update global reflections once on enable.
            if(EnviroManager.instance.Objects.globalReflectionProbe != null)
                EnviroManager.instance.StartCoroutine(WaitToRefreshReflection());
        }

        public override void Disable ()
        {
            if(EnviroManager.instance == null)
               return;

            Cleanup();
        }

        private void Cleanup()
        {
            if(EnviroManager.instance == null)
               return;

            if(EnviroManager.instance.Objects.globalReflectionProbe != null)
               DestroyImmediate(EnviroManager.instance.Objects.globalReflectionProbe.gameObject);
        }

        // Unity warns with "Attempting to update a disabled Reflection Probe" even though the probe is enabled.
        // We have to wait a frame before interacting with reflection probes to allow Unity time to do any
        // setup in its internal OnEnable(). Otherwise, we will receive a warning:
        // "Attempting to update a disabled Reflection Probe. Action will be ignored."
        private IEnumerator WaitToRefreshReflection() 
        {
            yield return null;
            EnviroManager.instance.Objects.globalReflectionProbe.RefreshReflection(false);
            UpdateDefaultReflectionTextureMode ();
        } 

        private void Setup()
        {
            if(EnviroManager.instance.Objects.globalReflectionProbe == null)
            {
                GameObject newReflectionProbe = new GameObject();
                newReflectionProbe.name = "Global Reflection Probe";
                newReflectionProbe.transform.SetParent(EnviroManager.instance.transform);
                newReflectionProbe.transform.localPosition = Vector3.zero;
                EnviroManager.instance.Objects.globalReflectionProbe = newReflectionProbe.AddComponent<EnviroReflectionProbe>();
            }
        } 

        public override void UpdateModule ()
        {
            if(EnviroManager.instance == null)
               return;

            if(EnviroManager.instance.Objects.globalReflectionProbe != null)
                UpdateReflection();
        }

        private void UpdateReflection()
        {
            EnviroReflectionProbe probe = EnviroManager.instance.Objects.globalReflectionProbe;

            SetupProbeSettings(probe);

            if(EnviroManager.instance.Time != null)
            {
                if ((lastReflectionUpdate < EnviroManager.instance.Time.Settings.timeOfDay || lastReflectionUpdate > EnviroManager.instance.Time.Settings.timeOfDay + (Settings.globalReflectionsTimeTreshold + 0.01f)) && Settings.globalReflectionsUpdateOnGameTime)
                {
                    RenderGlobalReflectionProbe();
                    lastReflectionUpdate = EnviroManager.instance.Time.Settings.timeOfDay + Settings.globalReflectionsTimeTreshold;
                }
            }

            if ((probe.transform.position.magnitude > lastReflectionUpdatePos.magnitude + Settings.globalReflectionsPositionTreshold || probe.transform.position.magnitude < lastReflectionUpdatePos.magnitude - Settings.globalReflectionsPositionTreshold) && Settings.globalReflectionsUpdateOnPosition)
            {
                RenderGlobalReflectionProbe();
                lastReflectionUpdatePos = probe.transform.position;
            }

            UpdateDefaultReflectionTextureMode ();
        }

        public void RenderGlobalReflectionProbe(bool forced = false)
        {
            EnviroReflectionProbe probe = EnviroManager.instance.Objects.globalReflectionProbe;

            if (probe == null)
                return;

            if(renderReflectionCoroutine != null)
            {
                EnviroManager.instance.StopCoroutine(renderReflectionCoroutine);
                renderReflectionCoroutine = null;
            }

            #if !ENVIRO_HDRP
                renderReflectionCoroutine = EnviroManager.instance.StartCoroutine(RenderGlobalReflectionProbeTimed(probe));

                if(Settings.updateDefaultEnvironmentReflections)
                {
                #if UNITY_2022_1_OR_NEWER
                // We don't need to copy the texture to a cubemap anmyore
                #else
                    // Prevent multiple coroutines from running at the same time
                    if (copyDefaultReflectionCoroutine != null) 
                    {
                        EnviroManager.instance.StopCoroutine(copyDefaultReflectionCoroutine);
                        copyDefaultReflectionCoroutine = null;
                    }
                    
                    if(Settings.customRendering)
                     copyDefaultReflectionCoroutine = EnviroManager.instance.StartCoroutine(CopyDefaultReflectionCustom(probe));
                    else
                     CopyDefaultReflectionUnity(probe);
                #endif
                } 
                
            #else
                renderReflectionCoroutine = EnviroManager.instance.StartCoroutine(RenderGlobalReflectionProbeTimed(probe));
            #endif
        }

        //Copy reflection probe to cubemap and assign as default reflections.
        private void CopyDefaultReflectionCubemap (EnviroReflectionProbe probe)
        {
             if(Settings.defaultSkyReflectionTex == null || Settings.defaultSkyReflectionTex.height != probe.myProbe.texture.height || Settings.defaultSkyReflectionTex.width != probe.myProbe.texture.width)
                { 
                    if(Settings.defaultSkyReflectionTex != null)
                    DestroyImmediate(Settings.defaultSkyReflectionTex);

                    Settings.defaultSkyReflectionTex = new Cubemap(probe.myProbe.resolution, probe.myProbe.hdr ? TextureFormat.RGBAHalf : TextureFormat.RGBA32, true);
                    Settings.defaultSkyReflectionTex.name = "Enviro Default Sky Reflection";
                }
 
                if(probe.myProbe.texture != null)
                   Graphics.CopyTexture(probe.myProbe.texture, Settings.defaultSkyReflectionTex as Texture);               
        }
 
        public void UpdateDefaultReflectionTextureMode ()
        {
            if(Settings.updateDefaultEnvironmentReflections)
            {
                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;

        #if UNITY_2022_1_OR_NEWER
                RenderSettings.customReflectionTexture = EnviroManager.instance.Objects.globalReflectionProbe.myProbe.texture;
        #else
                if(Settings.defaultSkyReflectionTex != null)
                   RenderSettings.customReflection = Settings.defaultSkyReflectionTex;
        #endif
            }
            else
            {
                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            }
        }

        //Update all probe settings.
        private void SetupProbeSettings(EnviroReflectionProbe probe)
        {
            int res = 128;

            switch (Settings.globalReflectionResolution)
            {
                case EnviroReflections.GlobalReflectionResolution.R16:
                     res = 16;
                break;
                case EnviroReflections.GlobalReflectionResolution.R32:
                     res = 32;
                break;
                case EnviroReflections.GlobalReflectionResolution.R64:
                     res = 64;
                break;
                case EnviroReflections.GlobalReflectionResolution.R128:
                     res = 128;
                break;
                case EnviroReflections.GlobalReflectionResolution.R256:
                     res = 256;
                break;
                case EnviroReflections.GlobalReflectionResolution.R512:
                     res = 512;
                break;
                case EnviroReflections.GlobalReflectionResolution.R1024:
                     res = 1024;
                break;
                case EnviroReflections.GlobalReflectionResolution.R2048:
                     res = 2048;
                break;
            }
#if !ENVIRO_HDRP
            probe.myProbe.cullingMask = Settings.globalReflectionLayers;
            probe.myProbe.intensity = Settings.globalReflectionsIntensity;
            probe.myProbe.size = new Vector3 (Settings.globalReflectionsScale,Settings.globalReflectionsScale,Settings.globalReflectionsScale);
            probe.myProbe.resolution = res;
            probe.customRendering = Settings.customRendering;
            probe.useTimeSlicing = Settings.customRenderingTimeSlicing;
            probe.myProbe.timeSlicingMode = Settings.globalReflectionTimeSlicingMode;
            RenderSettings.reflectionIntensity = Settings.globalReflectionsIntensity;
#else
            probe.customRendering = false;
            probe.myProbe.resolution = res;

            if(probe.hdprobe != null)
            {
                probe.hdprobe.settingsRaw.cameraSettings.culling.cullingMask = Settings.globalReflectionLayers;
                probe.hdprobe.settingsRaw.influence.boxSize = new Vector3 (Settings.globalReflectionsScale,Settings.globalReflectionsScale,Settings.globalReflectionsScale);
                probe.hdprobe.settingsRaw.influence.sphereRadius = Settings.globalReflectionsScale;
                probe.hdprobe.settingsRaw.lighting.multiplier = Settings.globalReflectionsIntensity;
            }
#endif
        }

        private IEnumerator CopyDefaultReflectionCustom(EnviroReflectionProbe probe)
        {
                if (Settings.customRenderingTimeSlicing)
                {
                    // Wait for seven frames so probe finished rendering
                    for (int i = 0; i < 8; i++)
                    {
                        yield return null;
                    }

                    CopyDefaultReflectionCubemap(probe);
                }
                else
                {
                    yield return null;
                    CopyDefaultReflectionCubemap(probe);
                }
        }
 
        private void CopyDefaultReflectionUnity(EnviroReflectionProbe probe)
        {
            if(probe.renderId == -1 || probe.myProbe.IsFinishedRendering(probe.renderId))
            {
                CopyDefaultReflectionCubemap(probe);
            }
            else
            {
                if (waitForProbeCoroutine != null) {
                    EnviroManager.instance.StopCoroutine(waitForProbeCoroutine);
                    waitForProbeCoroutine = null;
                }
                waitForProbeCoroutine = EnviroManager.instance.StartCoroutine(WaitForUnityProbe(probe));
            }
        } 

        private IEnumerator WaitForUnityProbe(EnviroReflectionProbe probe)
        {
            yield return null;
            CopyDefaultReflectionUnity(probe);
        }

        private IEnumerator RenderGlobalReflectionProbeTimed (EnviroReflectionProbe probe)
        {
        #if ENVIRO_HDRP
            yield return null;
            EnviroManager.instance.updateSkyAndLightingHDRP = false;
            yield return null;      
            probe.RefreshReflection();
            yield return null;
            EnviroManager.instance.updateSkyAndLightingHDRP = true;
        #else
            if(EnviroManager.instance.Lighting != null)
            {
                //Force a lighting update before rendering the probe as it might has not updated yet.
                EnviroManager.instance.Lighting.UpdateDirectLighting ();
                EnviroManager.instance.Lighting.UpdateAmbientLighting(true);
                yield return null;
                probe.RefreshReflection(Settings.customRenderingTimeSlicing);
            }
            else
            {
                probe.RefreshReflection(Settings.customRenderingTimeSlicing);
            }
        #endif 
        } 

        //Save and Load
        public void LoadModuleValues ()
        {
            if(preset != null)
            {
                Settings = JsonUtility.FromJson<Enviro.EnviroReflections>(JsonUtility.ToJson(preset.Settings));
            }
            else
            {
                Debug.Log("Please assign a saved module to load from!");
            }
        }

        public void SaveModuleValues ()
        {
#if UNITY_EDITOR
        EnviroReflectionsModule t =  ScriptableObject.CreateInstance<EnviroReflectionsModule>();
        t.name = "Reflections Module";
        t.Settings = JsonUtility.FromJson<Enviro.EnviroReflections>(JsonUtility.ToJson(Settings));

        string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EnviroHelper.assetPath + "/New " + t.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(t, assetPathAndName);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void SaveModuleValues (EnviroReflectionsModule module)
        {
            module.Settings = JsonUtility.FromJson<Enviro.EnviroReflections>(JsonUtility.ToJson(Settings));
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(module);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}