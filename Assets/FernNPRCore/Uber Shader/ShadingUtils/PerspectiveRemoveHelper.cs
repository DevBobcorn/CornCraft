using System;
using FernNPRCore.Scripts.FernNPRRenderer;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace FernNPRCore.Scripts.ShadingUtils
{
    [ExecuteAlways]
    public class PerspectiveRemoveHelper : MonoBehaviour
    {
        [Range(-2,2)]
        public float inFluenceRange = 0.16f;
        [Range(0,1)]
        public float pmRemoveSlider = 1;
        public int subMeshIndex = 0; // TODO Add Editor To Every SubMesh
        public bool allSubMesh = true;
        public Transform foucsPoint;

        private MaterialPropertyBlock materialBlock;
        private Renderer _renderer;
        private static readonly int PmRemoveMatrix = Shader.PropertyToID("_PMRemove_Matrix");
        private static readonly int PmRemoveFocusPoint = Shader.PropertyToID("_PMRemove_FocusPoint");
        private static readonly int PmRemoveRange = Shader.PropertyToID("_PMRemove_Range");
        private static readonly int PmRemoveSlider = Shader.PropertyToID("_PmRemove_Slider");
        private static string PERSPECTIVEREMOVEKEYWORD = "_PERSPECTIVEREMOVE";

        private void OnEnable()
        {
            if (foucsPoint == null)
            {
                foucsPoint = transform;
            }
            _renderer = GetComponent<Renderer>();
            materialBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(materialBlock);
            foreach (var mat in _renderer.sharedMaterials)
            {
                CoreUtils.SetKeyword(mat, PERSPECTIVEREMOVEKEYWORD, true);
            }
            
            RenderPipelineManager.beginCameraRendering -= OnCameraRender;
            RenderPipelineManager.beginCameraRendering += OnCameraRender;
        }

        private void OnDisable()
        {
            materialBlock.SetFloat(PmRemoveSlider, 0);
            _renderer.SetPropertyBlock(materialBlock);
            foreach (var mat in _renderer.sharedMaterials)
            {
                CoreUtils.SetKeyword(mat, PERSPECTIVEREMOVEKEYWORD, false);
            }
            
            RenderPipelineManager.beginCameraRendering -= OnCameraRender;
        }

        private void OnCameraRender(ScriptableRenderContext scriptableRenderContext, Camera camera1)
        {
            if (camera1.orthographic)
            {
                materialBlock.Clear();
                materialBlock.SetFloat(PmRemoveSlider, 0);
                _renderer.SetPropertyBlock(materialBlock);
                return;
            }

            float fovAngle = camera1.fieldOfView * Mathf.Deg2Rad;
            float tanHalf = Mathf.Tan(fovAngle * 0.5f);

            //Get the distance on camera forward between target and current camera
            var position = camera1.transform.position;
            Vector3 linepoint = foucsPoint.position - position;
            Vector3 cForward = camera1.transform.forward;
            Vector3 p = Vector3.Project(linepoint, cForward);
            float targetDistance = p.magnitude;

            float a = camera1.aspect;
            float s = targetDistance * tanHalf;
            float width = a * s;
            float height = s;

            //Create OrthoProjectMatrix
            Matrix4x4 orthoProjectMatrix = Matrix4x4.Ortho(-width, width, height, -height,
                camera1.nearClipPlane, camera1.farClipPlane);
            orthoProjectMatrix = GL.GetGPUProjectionMatrix(orthoProjectMatrix, false);

            //Create ViewMatrix
            Matrix4x4 viewMatrix = Matrix4x4.TRS(position, camera1.transform.rotation, Vector3.one).inverse;
            if (SystemInfo.usesReversedZBuffer)
            {
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;
            }

            //Create VP Martix
            Matrix4x4 viewProject = orthoProjectMatrix * viewMatrix;
            materialBlock.Clear();
            materialBlock.SetMatrix(PmRemoveMatrix, viewProject);
            materialBlock.SetVector(PmRemoveFocusPoint, foucsPoint.position);
            materialBlock.SetFloat(PmRemoveSlider, pmRemoveSlider);
            materialBlock.SetFloat(PmRemoveRange, inFluenceRange == 0 ? 0.0001f : inFluenceRange);
            _renderer.SetPropertyBlock(materialBlock);
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(foucsPoint.position, inFluenceRange);
        }
    }
}