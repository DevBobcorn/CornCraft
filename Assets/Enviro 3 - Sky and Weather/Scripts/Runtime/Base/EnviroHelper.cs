using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enviro
{
    public static class EnviroHelper
    { 
        public static string assetPath = "Assets/Enviro 3 - Sky and Weather";
        public static Vector3 PingPong (Vector3 value)
        {
            Vector3 result = value;
    
            if (result.x > 1f)
                result.x = -1f;
            else if (result.x < -1f)
                result.x = 1f;

            if (result.y > 1f)
                result.y = -1f;
            else if (result.y < -1f)
                result.y = 1f;

            if (result.z > 1f)
                result.z = -1f;
            else if (result.z < -1f)
                result.z = 1f;

            return result;
        }

        public static Vector2 PingPong (Vector2 value)
        {
            Vector2 result = value;
    
            if (result.x > 1f)
                result.x = -1f;
            else if (result.x < -1f)
                result.x = 1f;

            if (result.y > 1f)
                result.y = -1f;
            else if (result.y < -1f)
                result.y = 1f;
                
            return result;
        }

        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        // Checks if Enviro Effects should render on this camera for URP/HDRP
        public static bool CanRenderOnCamera (Camera cam)
        {
            if(EnviroManager.instance != null)
            {
                if(cam.cameraType == CameraType.SceneView || cam.cameraType == CameraType.Reflection)
                   return true;

                if(cam == EnviroManager.instance.Camera)
                   return true;

                if(EnviroManager.instance.Objects.globalReflectionProbe != null && cam == EnviroManager.instance.Objects.globalReflectionProbe.renderCam)
                   return true;

                for (int i = 0; i < EnviroManager.instance.Cameras.Count; i++)
                {
                    if(cam == EnviroManager.instance.Cameras[i].camera)
                       return true;
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        ///Get the Light component from Enviro Directional light if lighting module is activated or any other active directional light
        public static Light GetDirectionalLight ()
        {
            Light result = null;

            if(EnviroManager.instance.Lighting != null)
            {
                if(EnviroManager.instance.Lighting.Settings.lightingMode == EnviroLighting.LightingMode.Single)
                {
                    if(EnviroManager.instance.Objects.directionalLight != null)
                       result = EnviroManager.instance.Objects.directionalLight;
                }
                else
                {
                    if(!EnviroManager.instance.isNight)
                    {
                        if(EnviroManager.instance.Objects.directionalLight != null)
                           result = EnviroManager.instance.Objects.directionalLight;
                    }
                    else
                    {
                        if(EnviroManager.instance.Objects.additionalDirectionalLight != null)
                           result = EnviroManager.instance.Objects.additionalDirectionalLight;
                    }
                }
            }
            else
            {
                //Find other Directional Lights in scene
                Light[] results = GameObject.FindObjectsOfType<Light>();
                for(int i = 0; i < results.Length; i++)
                {
                    if(results[i].type == LightType.Directional && results[i].gameObject.activeSelf && results[i].enabled)
                        {
                            result = results[i];
                            break;
                        }
                }
            }
            return result;
        }


        public static void CreateBuffer(ref ComputeBuffer buffer, int count, int stride)
        {
            if (buffer != null && buffer.count == count)
                return;

            if(buffer != null)
            {
                buffer.Release();
                buffer = null;
            }

            if (count <= 0)
                return;

            buffer = new ComputeBuffer(count, stride);
        }
        public static void ReleaseComputeBuffer(ref ComputeBuffer buffer)
        {
            if(buffer != null)
                buffer.Release();
                
            buffer = null;
        }


        public static Vector4 GetProjectionExtents(Camera camera)
        {
            return GetProjectionExtents(camera, 0.0f, 0.0f);
        }

        public static Vector4 GetProjectionExtents(Camera camera, float texelOffsetX, float texelOffsetY)
        {
            if (camera == null)
                return Vector4.zero;

            float oneExtentY = camera.orthographic ? camera.orthographicSize : Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView);
            float oneExtentX = oneExtentY * camera.aspect;
            float texelSizeX = oneExtentX / (0.5f * camera.pixelWidth);
            float texelSizeY = oneExtentY / (0.5f * camera.pixelHeight);
            float oneJitterX = texelSizeX * texelOffsetX;
            float oneJitterY = texelSizeY * texelOffsetY;

            return new Vector4(oneExtentX, oneExtentY, oneJitterX, oneJitterY);
        }

        public static Vector4 GetProjectionExtents(Camera camera, Camera.StereoscopicEye eye)
        {
            return GetProjectionExtents(camera, eye, 0.0f, 0.0f);
        }

        public static Vector4 GetProjectionExtents(Camera camera, Camera.StereoscopicEye eye, float texelOffsetX, float texelOffsetY)
        {
            Matrix4x4 inv = Matrix4x4.Inverse(camera.GetStereoProjectionMatrix(eye));
            Vector3 ray00 = inv.MultiplyPoint3x4(new Vector3(-1.0f, -1.0f, 0.95f));
            Vector3 ray11 = inv.MultiplyPoint3x4(new Vector3(1.0f, 1.0f, 0.95f));

            ray00 /= -ray00.z;
            ray11 /= -ray11.z; 

            float oneExtentX = 0.5f * (ray11.x - ray00.x);
            float oneExtentY = 0.5f * (ray11.y - ray00.y);
            float texelSizeX = oneExtentX / (0.5f * camera.pixelWidth);
            float texelSizeY = oneExtentY / (0.5f * camera.pixelHeight);
            float oneJitterX = 0.5f * (ray11.x + ray00.x) + texelSizeX * texelOffsetX;
            float oneJitterY = 0.5f * (ray11.y + ray00.y) + texelSizeY * texelOffsetY;

            return new Vector4(oneExtentX, oneExtentY, oneJitterX, oneJitterY);
        }

        //Find the default profile.
        public static EnviroModule GetDefaultPreset(string name)
        {
    #if UNITY_EDITOR
            string[] assets = UnityEditor.AssetDatabase.FindAssets(name, null);

            for (int idx = 0; idx < assets.Length; idx++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[idx]);

                if (path.Contains(".asset"))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<EnviroModule>(path);
                }
            }
    #endif
            return null;
        }

#if ENVIRO_HDRP
        public static UnityEngine.Rendering.VolumeProfile GetDefaultSkyAndFogProfile(string name)
        {
#if UNITY_EDITOR
            string[] assets = UnityEditor.AssetDatabase.FindAssets(name, null);

            for (int idx = 0; idx < assets.Length; idx++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(assets[idx]);

                if (path.Contains(name + ".asset")) 
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
                }
            }
#endif
            return null;
        }
#endif
    }
}
