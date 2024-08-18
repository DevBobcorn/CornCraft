using UnityEngine;

namespace AnimeSkybox
{
    [ExecuteAlways]
    public class DirectionToSkybox : MonoBehaviour
    {
        public GameObject sun; // 模拟太阳的空物体
        public GameObject moon; // 模拟月亮的空物体
        public Material targetMaterial; // 你希望修改的Skybox材质
        public Material targetMaterialCloudTA;
        public Material targetMaterialCloudTB;
        public string sunDirectionPropertyName = "_SunDirection"; // 模拟太阳方向在Skybox材质上的属性名称
        public string moonDirectionPropertyName = "_MoonDirection"; // 模拟月亮方向在Skybox材质上的属性名称

        void Start()
        {
            if (targetMaterial == null)
            {
                Debug.LogError("Please assign a target Skybox material.");
                return;
            }
        }

        void Update()
        {
            Matrix4x4 LtoW = moon.transform.localToWorldMatrix;
            targetMaterial.SetMatrix("LToW",LtoW);
        
            if (targetMaterial == null)
                return;

            if (sun)
            {
                Vector3 sunDirection = -sun.transform.forward.normalized;
                targetMaterial.SetVector(sunDirectionPropertyName, sunDirection);
                targetMaterialCloudTA.SetVector(sunDirectionPropertyName, sunDirection);
                targetMaterialCloudTB.SetVector(sunDirectionPropertyName, sunDirection);
            }

            if (moon)
            {
                Vector3 moonDirection = -moon.transform.forward.normalized;
                targetMaterial.SetVector(moonDirectionPropertyName, moonDirection);
                targetMaterialCloudTA.SetVector(moonDirectionPropertyName, moonDirection);
                targetMaterialCloudTB.SetVector(moonDirectionPropertyName, moonDirection);
            }

            // 如果确实要在运行时更改材质属性，可能需要强制Unity重新绘制Skybox
            // DynamicGI.UpdateEnvironment();
        }
    }
}