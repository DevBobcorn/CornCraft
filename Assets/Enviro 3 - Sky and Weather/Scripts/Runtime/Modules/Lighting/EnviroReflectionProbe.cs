using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENVIRO_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Enviro 
{
    [AddComponentMenu("Enviro 3/Reflection Probe")]
    [RequireComponent(typeof(ReflectionProbe)), ExecuteInEditMode]
    public class EnviroReflectionProbe : MonoBehaviour
    {
        #region Public Var
        #region Standalone Settings
        public bool standalone = false;
        public bool updateReflectionOnGameTime = true;
        public float reflectionsUpdateTreshhold = 0.025f;
        public bool useTimeSlicing = true;
        #endregion
        public Camera renderCam;
        [HideInInspector]
        public ReflectionProbe myProbe;
        public bool customRendering = false;

    #if !ENVIRO_HDRP
        private EnviroRenderer enviroRenderer;
    #endif
        public bool useFog = false;
        #endregion

        #region Private Var
        // Privates
        public Camera bakingCam;
        public int renderId = -1;
        private bool currentMode = false;
        private int currentRes;
        private RenderTexture cubemap;
        private RenderTexture finalCubemap;
        private RenderTexture mirrorTexture;
        private RenderTexture renderTexture;
        private GameObject renderCamObj;
        private Material mirror = null;
        private Material bakeMat = null;
        private Material convolutionMat;
        private Coroutine refreshing;

        private int renderID;

    #if ENVIRO_HDRP
        public HDProbe hdprobe;
    #endif
        private static Quaternion[] orientations = new Quaternion[]
        {
                Quaternion.LookRotation(Vector3.right, Vector3.down),
                Quaternion.LookRotation(Vector3.left, Vector3.down),
                Quaternion.LookRotation(Vector3.up, Vector3.forward),
                Quaternion.LookRotation(Vector3.down, Vector3.back),
                Quaternion.LookRotation(Vector3.forward, Vector3.down),
                Quaternion.LookRotation(Vector3.back, Vector3.down)
        };

        private double lastRelfectionUpdate;
        #endregion
        ////////
        void OnEnable()
        {
            myProbe = GetComponent<ReflectionProbe>();

    #if ENVIRO_HDRP
            if (EnviroManager.instance != null)
            {
                hdprobe = GetComponent<HDProbe>();

                if(!standalone && myProbe != null)
                    myProbe.enabled = true;

                if (customRendering)
                {
                    if (hdprobe != null)
                    {
                        hdprobe.mode = ProbeSettings.Mode.Custom;
                        CreateCubemap();
                        CreateTexturesAndMaterial();
                        CreateRenderCamera();
                        currentRes = myProbe.resolution;
                        StartCoroutine(RefreshFirstTime());
                    }
                }
                else
                {
                    if (hdprobe != null)
                    {
                        hdprobe.mode = ProbeSettings.Mode.Realtime;
                        hdprobe.realtimeMode = ProbeSettings.RealtimeMode.OnDemand;
                        hdprobe.RequestRenderNextUpdate();
                    }
                }
            }
    #else

            if (!standalone && myProbe != null)
                myProbe.enabled = true;


            if (customRendering)
            {
                myProbe.mode = ReflectionProbeMode.Custom;
                myProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                CreateCubemap();
                CreateTexturesAndMaterial();
                CreateRenderCamera();
                currentRes = myProbe.resolution;
                StartCoroutine(RefreshFirstTime());        
            }
            else
            {
                myProbe.mode = ReflectionProbeMode.Realtime;
                myProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                //StartCoroutine(RefreshUnity());
                renderId = myProbe.RenderProbe();
            }
    #endif
        }
        void OnDisable()
        {
            Cleanup();

            if (!standalone && myProbe != null)
                myProbe.enabled = false;

            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        }
        private void Cleanup()
        {
            if (refreshing != null)
                StopCoroutine(refreshing);

            if (cubemap != null)
            {
                if (renderCam != null)
                    renderCam.targetTexture = null;

                DestroyImmediate(cubemap);
            }

            if (renderCamObj != null)
                DestroyImmediate(renderCamObj);

            if (mirrorTexture != null)
                DestroyImmediate(mirrorTexture);

            if (renderTexture != null)
                DestroyImmediate(renderTexture);
        }
        // Creation
        private void CreateRenderCamera()
        {
            if (renderCamObj == null)
            {
                renderCamObj = new GameObject();
                renderCamObj.name = "Reflection Probe Cam";
                renderCamObj.hideFlags = HideFlags.HideAndDontSave;
                renderCam = renderCamObj.AddComponent<Camera>();
                renderCam.gameObject.SetActive(true);
                renderCam.cameraType = CameraType.Reflection;
                renderCam.fieldOfView = 90;
                renderCam.farClipPlane = myProbe.farClipPlane;
                renderCam.nearClipPlane = myProbe.nearClipPlane;
                renderCam.clearFlags = (CameraClearFlags)myProbe.clearFlags;
                renderCam.backgroundColor = myProbe.backgroundColor;
                renderCam.allowHDR = myProbe.hdr;
                renderCam.targetTexture = cubemap;
                renderCam.enabled = false;

    #if VEGETATION_STUDIO_PRO
        //     VegetationStudioManager.Instance.AddCamera(renderCam);
    #endif

    #if !ENVIRO_HDRP
                if (EnviroManager.instance != null)
                {
                    enviroRenderer = renderCamObj.AddComponent<EnviroRenderer>();
                }
    #endif
            }
        }
        private void UpdateCameraSettings()
        {
            if (renderCam != null)
            {
                renderCam.cullingMask = myProbe.cullingMask;
    #if !ENVIRO_HDRP
              if (EnviroManager.instance != null)
                {
                 //Update Quality
                }
    #endif

            }
        }
        private Camera CreateBakingCamera()
        {
            GameObject tempCam = new GameObject();
            tempCam.name = "Reflection Probe Cam";
            Camera cam = tempCam.AddComponent<Camera>();
            cam.enabled = false;
            cam.gameObject.SetActive(true);
            cam.cameraType = CameraType.Reflection;
            cam.fieldOfView = 90;
            cam.farClipPlane = myProbe.farClipPlane;
            cam.nearClipPlane = myProbe.nearClipPlane;
            cam.cullingMask = myProbe.cullingMask;
            cam.clearFlags = (CameraClearFlags)myProbe.clearFlags;
            cam.backgroundColor = myProbe.backgroundColor;
            cam.allowHDR = myProbe.hdr;
            cam.targetTexture = cubemap;
    #if !ENVIRO_HDRP
            if (EnviroManager.instance != null)
                {
                    enviroRenderer = renderCamObj.AddComponent<EnviroRenderer>();
                }
    #endif
            tempCam.hideFlags = HideFlags.HideAndDontSave;
            return cam;
        }
        private void CreateCubemap()
        {
            if (cubemap != null && myProbe.resolution == currentRes)
                return;

            if (cubemap != null)
            {
                cubemap.Release();
                DestroyImmediate(cubemap);
            }

            if (finalCubemap != null)
            {
                finalCubemap.Release();
                DestroyImmediate(finalCubemap);
            }
                

            int resolution = myProbe.resolution;

            currentRes = resolution;
            RenderTextureFormat format = myProbe.hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            cubemap = new RenderTexture(resolution, resolution, 16, format, RenderTextureReadWrite.Linear);
            cubemap.dimension = TextureDimension.Cube;
            cubemap.useMipMap = true;
            cubemap.autoGenerateMips = false;
            cubemap.name = "Enviro Reflection Temp Cubemap";
            cubemap.filterMode = FilterMode.Trilinear;
            cubemap.Create();

            finalCubemap = new RenderTexture(resolution, resolution, 16, format, RenderTextureReadWrite.Linear);
            finalCubemap.dimension = TextureDimension.Cube;
            finalCubemap.useMipMap = true;
            finalCubemap.autoGenerateMips = false;
            finalCubemap.name = "Enviro Reflection Final Cubemap";
            finalCubemap.filterMode = FilterMode.Trilinear;
            finalCubemap.Create();
        }
        //Create the textures
        private void CreateTexturesAndMaterial()
        {
            if (mirror == null)
                mirror = new Material(Shader.Find("Hidden/Enviro/ReflectionProbe"));

            if (convolutionMat == null)
                convolutionMat = new Material(Shader.Find("Hidden/EnviroCubemapBlur"));

            int resolution = myProbe.resolution;

            RenderTextureFormat format = myProbe.hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            if (mirrorTexture == null || mirrorTexture.width != resolution || mirrorTexture.height != resolution)
            {
                if (mirrorTexture != null)
                    DestroyImmediate(mirrorTexture);

                mirrorTexture = new RenderTexture(resolution, resolution, 16, format, RenderTextureReadWrite.Linear);
                mirrorTexture.useMipMap = true;
                mirrorTexture.autoGenerateMips = false;
                mirrorTexture.name = "Enviro Reflection Mirror Texture";
                mirrorTexture.Create();
            }

            if (renderTexture == null || renderTexture.width != resolution || renderTexture.height != resolution)
            {
                if (renderTexture != null)
                    DestroyImmediate(renderTexture);

                renderTexture = new RenderTexture(resolution, resolution, 16, format, RenderTextureReadWrite.Linear);
                renderTexture.useMipMap = true;
                renderTexture.autoGenerateMips = false;
                renderTexture.name = "Enviro Reflection Target Texture";
                renderTexture.Create();
            }
        }
        // Refresh Methods
        public void RefreshReflection(bool timeSlice = false)
        {
    #if ENVIRO_HDRP
            if (customRendering)
            {
                if (refreshing != null)
                    return;

                CreateTexturesAndMaterial();

                if (renderCam == null)
                    CreateRenderCamera();

                UpdateCameraSettings();

                renderCam.transform.position = transform.position;
                renderCam.targetTexture = renderTexture;

                if (Application.isPlaying)
                {
                    if (!timeSlice)
                        refreshing = StartCoroutine(RefreshInstant(renderTexture, mirrorTexture));
                    else
                        refreshing = StartCoroutine(RefreshOvertime(renderTexture, mirrorTexture));
                }
                else
                {
                    refreshing = StartCoroutine(RefreshInstant(renderTexture, mirrorTexture));
                }
            }
            else
            {
            
                if(hdprobe != null)
                   hdprobe.RequestRenderNextUpdate();
            }
    #else
            if (customRendering)
            {
                if (refreshing != null)
                    return;

                CreateTexturesAndMaterial();

                if (renderCam == null)
                    CreateRenderCamera();

                UpdateCameraSettings();

                renderCam.transform.position = transform.position;
                renderCam.targetTexture = renderTexture;

                if (Application.isPlaying)
                {
                    if (!timeSlice)
                        refreshing = StartCoroutine(RefreshInstant(renderTexture, mirrorTexture));
                    else
                        refreshing = StartCoroutine(RefreshOvertime(renderTexture, mirrorTexture));
                }
                else
                {
                    refreshing = StartCoroutine(RefreshInstant(renderTexture, mirrorTexture));
                }
            } 
            else
            {
                renderId = myProbe.RenderProbe(); 
            }
    #endif
        }

        IEnumerator RefreshFirstTime()
        {
            yield return null;
            RefreshReflection(false);
            RefreshReflection(false);
        }


        public IEnumerator RefreshUnity()
        {
            yield return null;
            renderId = myProbe.RenderProbe();
        }


        public IEnumerator RefreshInstant(RenderTexture renderTex, RenderTexture mirrorTex)
        {
            CreateCubemap();

            yield return null;

            for (int face = 0; face < 6; face++)
            {
                renderCam.transform.rotation = orientations[face];
                renderCam.Render();

                if(mirrorTex != null)
                {
                    Graphics.Blit(renderTex, mirrorTex, mirror);
                    Graphics.CopyTexture(mirrorTex, 0, 0, cubemap, face, 0);   
                }   
            }

            ConvolutionCubemap();
    #if ENVIRO_HDRP
        if (hdprobe != null)
            hdprobe.SetTexture(ProbeSettings.Mode.Custom, finalCubemap);
    #else
            myProbe.customBakedTexture = finalCubemap;
    #endif
            refreshing = null;
        }

        /// <summary>
        /// Update Reflections with Time Slicing
        /// </summary>
        public IEnumerator RefreshOvertime(RenderTexture renderTex, RenderTexture mirrorTex)
        {
            CreateCubemap();

            for (int face = 0;  face < 6; face++)
            {
                yield return null;
                renderCam.transform.rotation = orientations[face];      
                renderCam.Render();

                if(mirrorTex != null)
                {         
                    Graphics.Blit(renderTex, mirrorTex, mirror);
                    Graphics.CopyTexture(mirrorTex, 0, 0, cubemap, face, 0);
                }
                //ClearTextures();           
            }

                ConvolutionCubemap();
    #if ENVIRO_HDRP
            if (hdprobe != null)
                hdprobe.SetTexture(ProbeSettings.Mode.Custom, finalCubemap);
    #else
                myProbe.customBakedTexture = finalCubemap;
    #endif
            refreshing = null;
        }

        /// <summary>
        /// Bakes one face per time into a render texture
        /// </summary>
        /// <param name="face"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        public RenderTexture BakeCubemapFace(int face, int res)
        {
            if (bakeMat == null)
                bakeMat = new Material(Shader.Find("Hidden/Enviro/BakeCubemap"));

            if (bakingCam == null)
                bakingCam = CreateBakingCamera();

            bakingCam.transform.rotation = orientations[face];
            RenderTexture temp = RenderTexture.GetTemporary(res, res, 0, RenderTextureFormat.ARGBFloat);
            bakingCam.targetTexture = temp;
            bakingCam.Render();
            RenderTexture tex = new RenderTexture(res, res, 0, RenderTextureFormat.ARGBFloat);
            Graphics.Blit(temp, tex, bakeMat);
            RenderTexture.ReleaseTemporary(temp);
            return tex;
        }

        private void ClearTextures()
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = mirrorTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
        }
    

        //This is not a proper convolution and very hacky to get anywhere near of unity realtime reflection probe mip convolution.
        private void ConvolutionCubemap()
        {
            int mipCount = 7;

            GL.PushMatrix();
            GL.LoadOrtho();

            cubemap.GenerateMips();

            float texel = 1f;
            switch(finalCubemap.width)
            {

                case 16:
                texel = 1f;
                break;

                case 32:
                texel = 1f;
                break;

                case 64:
                texel = 2f;
                break;

                case 128:
                texel = 4f;
                break;

                case 256:
                texel = 8f;
                break;

                case 512:
                texel = 14f;
                break;

                case 1024:
                texel = 30f;
                break;
    
                case 2048:
                texel = 60f;
                break;
            }

            float res = finalCubemap.width;

            for (int mip = 0; mip < mipCount + 1; mip++)
            {
                //Copy each face
                Graphics.CopyTexture(cubemap, 0, mip, finalCubemap, 0, mip);
                Graphics.CopyTexture(cubemap, 1, mip, finalCubemap, 1, mip);
                Graphics.CopyTexture(cubemap, 2, mip, finalCubemap, 2, mip);
                Graphics.CopyTexture(cubemap, 3, mip, finalCubemap, 3, mip);
                Graphics.CopyTexture(cubemap, 4, mip, finalCubemap, 4, mip);
                Graphics.CopyTexture(cubemap, 5, mip, finalCubemap, 5, mip);

                int dstMip = mip + 1;

                if (dstMip == mipCount)
                    break;
            
                float texelSize = (texel * dstMip) / res;
             
                convolutionMat.SetTexture("_MainTex", finalCubemap);
                convolutionMat.SetFloat("_Texel", texelSize);        
                convolutionMat.SetFloat("_Level", mip);
                convolutionMat.SetPass(0);

                res *= 0.75f;

                // Positive X
                Graphics.SetRenderTarget(cubemap, dstMip, CubemapFace.PositiveX);
                GL.Begin(GL.QUADS);
                GL.TexCoord3(1, 1, 1);
                GL.Vertex3(0, 0, 1);
                GL.TexCoord3(1, -1, 1);
                GL.Vertex3(0, 1, 1);
                GL.TexCoord3(1, -1, -1);
                GL.Vertex3(1, 1, 1);
                GL.TexCoord3(1, 1, -1);
                GL.Vertex3(1, 0, 1);
                GL.End();

                // Negative X
                Graphics.SetRenderTarget(cubemap, dstMip, CubemapFace.NegativeX);
                GL.Begin(GL.QUADS);
                GL.TexCoord3(-1, 1, -1);
                GL.Vertex3(0, 0, 1);
                GL.TexCoord3(-1, -1, -1);
                GL.Vertex3(0, 1, 1);
                GL.TexCoord3(-1, -1, 1);
                GL.Vertex3(1, 1, 1);
                GL.TexCoord3(-1, 1, 1);
                GL.Vertex3(1, 0, 1);
                GL.End();

                // Positive Y
                Graphics.SetRenderTarget(cubemap, dstMip, CubemapFace.PositiveY);
                GL.Begin(GL.QUADS);
                GL.TexCoord3(-1, 1, -1);
                GL.Vertex3(0, 0, 1);
                GL.TexCoord3(-1, 1, 1);
                GL.Vertex3(0, 1, 1);
                GL.TexCoord3(1, 1, 1);
                GL.Vertex3(1, 1, 1);
                GL.TexCoord3(1, 1, -1);
                GL.Vertex3(1, 0, 1);
                GL.End();

                // Negative Y
                Graphics.SetRenderTarget(cubemap, dstMip, CubemapFace.NegativeY);
                GL.Begin(GL.QUADS);
                GL.TexCoord3(-1, -1, 1);
                GL.Vertex3(0, 0, 1);
                GL.TexCoord3(-1, -1, -1);
                GL.Vertex3(0, 1, 1);
                GL.TexCoord3(1, -1, -1);
                GL.Vertex3(1, 1, 1);
                GL.TexCoord3(1, -1, 1);
                GL.Vertex3(1, 0, 1);
                GL.End();

                // Positive Z
                Graphics.SetRenderTarget(cubemap, dstMip, CubemapFace.PositiveZ);
                GL.Begin(GL.QUADS);
                GL.TexCoord3(-1, 1, 1);
                GL.Vertex3(0, 0, 1);
                GL.TexCoord3(-1, -1, 1);
                GL.Vertex3(0, 1, 1);
                GL.TexCoord3(1, -1, 1);
                GL.Vertex3(1, 1, 1);
                GL.TexCoord3(1, 1, 1);
                GL.Vertex3(1, 0, 1);
                GL.End();

                // Negative Z
                Graphics.SetRenderTarget(cubemap, dstMip, CubemapFace.NegativeZ);
                GL.Begin(GL.QUADS);
                GL.TexCoord3(1, 1, -1);
                GL.Vertex3(0, 0, 1);
                GL.TexCoord3(1, -1, -1);
                GL.Vertex3(0, 1, 1);
                GL.TexCoord3(-1, -1, -1);
                GL.Vertex3(1, 1, 1);
                GL.TexCoord3(-1, 1, -1);
                GL.Vertex3(1, 0, 1);
                GL.End();
            }

            GL.PopMatrix();

        }
        private void UpdateStandaloneReflection()
        {
            if ((EnviroManager.instance.Time.GetDateInHours() > lastRelfectionUpdate + reflectionsUpdateTreshhold ||EnviroManager.instance.Time.GetDateInHours() < lastRelfectionUpdate - reflectionsUpdateTreshhold) && updateReflectionOnGameTime)
            {
                lastRelfectionUpdate = EnviroManager.instance.Time.GetDateInHours();
                RefreshReflection(!useTimeSlicing);
            }
        }
        private void Update()
        {
            if (currentMode != customRendering)
            {
                currentMode = customRendering;

                if (customRendering)
                {
                    OnEnable();
                }
                else
                {
                    OnEnable();
                    Cleanup();
                }
            }

            if (EnviroManager.instance != null && standalone)
            {
                UpdateStandaloneReflection();
            
            }
        }
    }
}