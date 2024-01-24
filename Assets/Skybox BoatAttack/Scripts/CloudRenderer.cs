using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class CloudRenderer : MonoBehaviour
{
    public Mesh[] cloudMeshes;
    public Material cloudMaterial;
    public ParticleSystem ps;
    public Transform viewerTransform;

    private NativeArray<ParticleSystem.Particle> particles;
    //private ParticleSystem.Particle[] parts;
    private MaterialPropertyBlock[] mpbs;
    
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += RenderPipelineManagerOnbeginCameraRendering;
        // TODO: PlanarReflections.BeginPlanarReflections += RenderPipelineManagerOnbeginCameraRendering;
        
        var main = ps.main;
        particles = new NativeArray<ParticleSystem.Particle>(main.maxParticles, Allocator.Persistent);
        //parts = new ParticleSystem.Particle[main.maxParticles];
        mpbs = new MaterialPropertyBlock[main.maxParticles];
        for (var index = 0; index < mpbs.Length; index++)
        {
            mpbs[index] = new MaterialPropertyBlock();
        }
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= RenderPipelineManagerOnbeginCameraRendering;
        // TODO: PlanarReflections.BeginPlanarReflections -= RenderPipelineManagerOnbeginCameraRendering;

        particles.Dispose();
    }

    private void RenderPipelineManagerOnbeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        DrawClouds(camera);
    }
    
    void Update()
    {
        var viewerPos = viewerTransform.position;
        viewerPos.y = 0F;
        transform.position = viewerPos;
    }

    public void DrawClouds(Camera camera)
    {
        if (!cloudMaterial || cloudMeshes == null || cloudMeshes.Length == 0) return;
        
        var camPos = camera.transform.position;
        var aliveCount = ps.GetParticles(particles);
        particles.OrderByDescending(x => Vector3.Distance(x.position, camPos));
        
        var scale = Vector3.zero;

        for (var index = 0; index < aliveCount; index++)
        {
            var particle = particles[index];
            if (particle.remainingLifetime <= 0.001f) continue;

            var pos = particle.position * CloudRenderHelper.SkyboxScale + camPos * (1 - CloudRenderHelper.SkyboxScale);

            var q = Quaternion.LookRotation(particle.position - camPos);
            Random.InitState((int)particle.randomSeed);
            var mesh = cloudMeshes[Random.Range(0, cloudMeshes.Length)];

            scale = particle.startSize3D;
            scale.x *= Random.value > 0.5f ? 1f : -1f;
            
            mpbs[index].SetVector("_BA_CloudData", new Vector4(scale.x, 0f, 0f, particle.GetCurrentColor(ps).a / 255f));
            
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(pos, q, scale * CloudRenderHelper.SkyboxScale),
                cloudMaterial, LayerMask.NameToLayer("3DSkybox"), camera, 0, mpbs[index], false, false, false);
        }
    }
}
