#nullable enable
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace MinecraftClient.Rendering
{
    public class ReflectionCamera : MonoBehaviour
    {
        public Transform? targetPlane;
        public bool useShadowForReflection;
        public float reflectionTextureScale = 0.5F;
        public LayerMask reflectLayers;
        public Material? reflectMaterial;

        [RangeAttribute(-1F, 1F)] public float m_planeOffset = 0F;
        private Camera? _reflectionCamera;
        private RenderTexture? _reflectionTexture;

        private readonly int _planarReflectionTextureId = Shader.PropertyToID("_ReflectionTex");

        // Given position/normal of the plane, calculates plane in camera space.
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
            var offsetPos = pos + normal * m_planeOffset;
            var m = cam.worldToCameraMatrix;
            var cameraPosition = m.MultiplyPoint(offsetPos);
            var cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
        }

        private void UpdateCameraProperties(Camera src, Camera dest) {
            if (dest == null) return;

            // dest.CopyFrom(src);
            dest.aspect = src.aspect;
            dest.cameraType = src.cameraType;   // 这个参数不同步就错
            dest.clearFlags = src.clearFlags;
            dest.fieldOfView = src.fieldOfView;
            dest.depth = src.depth;
            dest.farClipPlane = src.farClipPlane;
            dest.focalLength = src.focalLength;
            dest.useOcclusionCulling = false;
            
            if (dest.gameObject.TryGetComponent(out UniversalAdditionalCameraData camData)) {  // TODO
                camData.renderShadows = useShadowForReflection; // turn off shadows for the reflection camera
            }
        }

        void Start()
        {
            _reflectionCamera = GetComponent<Camera>();

            _reflectionCamera.cameraType = CameraType.Reflection;

            if (targetPlane is null || _reflectionCamera is null)
            {
                Debug.LogError("Missing references found in reflection camera");
                return;
            }

            RenderPipelineManager.beginCameraRendering += RenderReflection;

        }

        void OnDestroy() {
            RenderPipelineManager.beginCameraRendering -= RenderReflection;

            if (_reflectionTexture) {
                RenderTexture.ReleaseTemporary(_reflectionTexture);
            }
        }

        void RenderReflection(ScriptableRenderContext context, Camera camera)
        {
            // we dont want to render planar reflections in reflections or previews
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                return;
            
            if (targetPlane is null || _reflectionCamera is null)
                return;

            Vector3 planeNormal = targetPlane.up;
            Vector3 planePos = targetPlane.position + planeNormal * m_planeOffset;

            UpdateCameraProperties(camera, _reflectionCamera);

            // Update reflection camera position
            Vector3 camPosPS = targetPlane.transform.worldToLocalMatrix.MultiplyPoint(camera.transform.position);
            Vector3 reflectCamPosPS = Vector3.Scale(camPosPS, new Vector3(1, -1, 1)) + new Vector3(0, m_planeOffset, 0);  // 反射相机平面空间
            Vector3 reflectCamPosWS = targetPlane.transform.localToWorldMatrix.MultiplyPoint(reflectCamPosPS);  // 将反射相机转换到世界空间
            _reflectionCamera.transform.position = reflectCamPosWS;

            // Update reflection camera rotation
            Vector3 camForwardPS = targetPlane.transform.worldToLocalMatrix.MultiplyVector(camera.transform.forward);
            Vector3 reflectCamForwardPS = Vector3.Scale(camForwardPS, new Vector3(1, -1, 1));
            Vector3 reflectCamForwardWS = targetPlane.transform.localToWorldMatrix.MultiplyVector(reflectCamForwardPS); 
            
            Vector3 camUpPS = targetPlane.transform.worldToLocalMatrix.MultiplyVector(camera.transform.up);
            Vector3 reflectCamUpPS = Vector3.Scale(camUpPS, new Vector3(-1, 1, -1));
            Vector3 reflectCamUpWS = targetPlane.transform.localToWorldMatrix.MultiplyVector(reflectCamUpPS); 
            _reflectionCamera.transform.rotation = Quaternion.LookRotation(reflectCamForwardWS, reflectCamUpWS);

            // Update reflection camera view frustum
            var clipPlane = CameraSpacePlane(_reflectionCamera, planePos, planeNormal, 1.0f);
            var newProjectionMat = camera.CalculateObliqueMatrix(clipPlane);
            _reflectionCamera.projectionMatrix = newProjectionMat;
            _reflectionCamera.cullingMask = reflectLayers;

            // Create reflection texture if not present
            if (_reflectionTexture == null) {
                var scale = UniversalRenderPipeline.asset.renderScale;

                var resX = (int)(camera.pixelWidth * scale * reflectionTextureScale);
                var resY = (int)(camera.pixelHeight * scale * reflectionTextureScale);

                const bool useHdr10 = true;
                const RenderTextureFormat hdrFormat = useHdr10 ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
                _reflectionTexture = RenderTexture.GetTemporary(resX, resY, 16,
                        GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
                
                _reflectionCamera.targetTexture =  _reflectionTexture;

                reflectMaterial?.SetTexture(_planarReflectionTextureId, _reflectionTexture);
            }

            // Do the rendering
            UniversalRenderPipeline.RenderSingleCamera(context, _reflectionCamera);

            //Shader.SetGlobalTexture(_planarReflectionTextureId, _reflectionTexture);
        }
    }
}