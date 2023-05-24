using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace Enviro
{
  public class Lightning : MonoBehaviour 
  {
    public Vector3 target;
    private LineRenderer lineRend;
    public Light myLight;
    public Material planeMat;
    public int arcs = 20;
    public float arcLength = 100.0f;
    public float arcVariation = 1.0f;
    public float inaccuracy = 0.5f;
    public int splits = 4;

    public Vector3 toTarget;
    private bool fadeOut;
    private float fadeTimer;

/*
    private ComputeBuffer cloudsLighning;

    struct LightningParams
    {
        public Vector3 pos;
        public float range;
        public float intensity;
    }

    LightningParams[] lightningParams;
*/
    void OnEnable () 
    {
        lineRend = gameObject.GetComponent<LineRenderer> ();
        CastBolt();
    }

    IEnumerator CreateLightningBolt()
    {

     /* if(Enviro.EnviroManager.instance.cloudModule != null)
      {
        lightningParams = new LightningParams[1];
        lightningParams[0].pos = transform.position;
        lightningParams[0].range = 10f; 
        lightningParams[0].intensity = 0.1f;
        Enviro.EnviroManager.instance.cloudModule.blendAndLightingMat.SetFloat("_LightningCount", 1);
        Enviro.EnviroHelper.CreateBuffer(ref cloudsLighning, 1, Marshal.SizeOf(typeof(LightningParams)));
        cloudsLighning.SetData(lightningParams);
        Enviro.EnviroManager.instance.cloudModule.blendAndLightingMat.SetBuffer("_Lightnings",cloudsLighning);
      }*/

      myLight.enabled = false;
      lineRend.widthMultiplier = 5;
      planeMat.SetFloat("_Intensity", 1f);

      lineRend.SetPosition(0, transform.position);
      lineRend.positionCount = 2;
      lineRend.SetPosition(1, transform.position);
      Vector3 lastPoint = transform.position;
      float dist = Vector3.Distance(transform.position, target);

      float arcDist = dist / arcs;

      for (int i = 1; i < arcs; i++)
      {
        planeMat.SetFloat("_Intensity", Random.Range(0f,2f));
        lineRend.positionCount =  i + 1;
        Vector3 fwd = target - lastPoint;
        fwd.Normalize ();
        Vector3 pos = Randomize (fwd, inaccuracy);
        pos *= Random.Range (arcLength * arcVariation, arcLength) * (arcDist);
        pos += lastPoint;
        lineRend.SetPosition (i, pos);
       
        if(i < arcs - 2)
        {
          for (int s = 0; s <= splits; s++)
          {
              StartCoroutine(CreateSplit(pos, target));
          }
        }

        lastPoint = pos;
        yield return new WaitForSeconds(Random.Range(0.001f,0.005f));
      }
      lineRend.SetPosition(arcs-1,target);

      //Animate Light and Main bolt
      myLight.transform.position = target;
      lineRend.material.SetFloat("_Intensity", 50f);
      planeMat.SetFloat("_Intensity", 20f);
      myLight.enabled = true;
      yield return new WaitForSeconds(Random.Range(0.025f,0.035f));
      lineRend.material.SetFloat("_Intensity", 1f);
      planeMat.SetFloat("_Intensity", 1f);
      myLight.enabled = false;
      yield return new WaitForSeconds(Random.Range(0.025f,0.035f));
      lineRend.material.SetFloat("_Intensity", 50f);
      planeMat.SetFloat("_Intensity", 20f);
      myLight.enabled = true;
      yield return new WaitForSeconds(Random.Range(0.025f,0.035f));
      lineRend.material.SetFloat("_Intensity", 1f);
      planeMat.SetFloat("_Intensity", 1f);
      myLight.enabled = false;
      yield return new WaitForSeconds(Random.Range(0.025f,0.035f));
      lineRend.material.SetFloat("_Intensity", 50f);
      planeMat.SetFloat("_Intensity", 0f);
      myLight.enabled = true;
      yield return new WaitForSeconds(Random.Range(0.025f,0.035f));
      myLight.enabled = false;
      fadeTimer = 50f;
      fadeOut = true;
      //lineRend.positionCount = 1;
     // Enviro.EnviroManager.instance.cloudModule.blendAndLightingMat.SetFloat("_LightningCount", 0);
    //  Enviro.EnviroManager.instance.cloudModule.blendAndLightingMat.SetBuffer("_Lightnings",cloudsLighning);
     // Enviro.EnviroHelper.ReleaseComputeBuffer(ref cloudsLighning);
    }

    IEnumerator CreateSplit(Vector3 pos, Vector3 targetP)
    {
      GameObject split = new GameObject();
      split.transform.SetParent(transform);
      split.transform.position = pos;
      LineRenderer splitRenderer = split.AddComponent<LineRenderer>();
      splitRenderer.material = lineRend.material;
      splitRenderer.positionCount = 2;
      splitRenderer.SetPosition(0, split.transform.position);
      splitRenderer.SetPosition(1, split.transform.position);     
 
      //Set a random target 
      toTarget = targetP - pos; 
      toTarget = Vector3.Normalize(toTarget);
      Vector3 posDown = new Vector3(toTarget.x,toTarget.y, toTarget.z * 0.1f);     
      Vector3 targetPos = (Random.insideUnitSphere * 500 + pos + toTarget * 500);
      
      Vector3 lastPoint = split.transform.position;
      float dist = Vector3.Distance(split.transform.position, targetPos);

      float arcDist = dist / 7;

      for (int i = 1; i < 8; i++)
      {
        splitRenderer.positionCount =  i + 1;
        Vector3 fwd = targetPos - lastPoint;
        fwd.Normalize ();
        Vector3 newPos = Randomize (fwd, inaccuracy);
        newPos *= Random.Range (1f * 1.5f, 1f) * (arcDist);
        newPos += lastPoint;
        splitRenderer.SetPosition (i, newPos);
        lastPoint = newPos;
        yield return new WaitForSeconds(Random.Range(0.004f,0.006f));
      }
      splitRenderer.SetPosition(7,targetPos);
      yield return new WaitForSeconds(Random.Range(0.2f,0.5f));
      DestroyImmediate(split);
    }
 
    public void CastBolt()
    {
        lineRend.positionCount = 1;
        StartCoroutine(CreateLightningBolt());
    }
    private Vector3 Randomize (Vector3 newVector, float devation) {
        newVector += new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)) * devation;
        newVector.Normalize();
        return newVector;
    }

    private void Update() 
    {
      if(fadeOut == true)
      {
        fadeTimer = Mathf.Lerp(fadeTimer,0f,10f * Time.deltaTime);
        lineRend.material.SetFloat("_Intensity", fadeTimer);

        if(fadeTimer <= 1f)
          {
            lineRend.positionCount = 1;
            fadeOut = false;
            DestroyImmediate(gameObject);
          }
      }
    }
 } 
}