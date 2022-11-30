// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    public class WindController : MonoBehaviour
    {
        [SerializeField]
        private WindZone unityWindZone = null;
        [SerializeField]
        private float unityWindZoneScale = 0.1f;
        [SerializeField]
        private Renderer arrowRenderer = null;
        [SerializeField]
        private Gradient arrowGradient = new Gradient();
        [SerializeField]
        private List<Transform> rotationTransforms = new List<Transform>();
        [SerializeField]
        private GameObject blastWavePrefab;
        [SerializeField]
        private float blastWaveSpawnRadius = 3.0f;

        private float angleY = 0.0f;
        private float angleX = 0.0f;

        void Start()
        {
        }



        public void OnDirectionY(float value)
        {
            angleY = value;
            UpdateDirection();
        }

        public void OnDirectionX(float value)
        {
            angleX = value;
            UpdateDirection();
        }

        public void OnMain(float value)
        {
            Wind.Main = value;

            // Link Unit Wind Zone
            if (unityWindZone)
            {
                unityWindZone.windMain = value * unityWindZoneScale;
            }

            // arrow color
            if (arrowRenderer)
            {
                var t = Mathf.InverseLerp(0.0f, 50.0f, value);
                var col = arrowGradient.Evaluate(t);
                arrowRenderer.material.color = col;
            }
        }

        public void OnTurbulence(float value)
        {
            Wind.Turbulence = value;
        }

        public void OnFrequency(float value)
        {
            Wind.Frequency = value;
        }

        public void OnBlastWave()
        {
            if (blastWavePrefab == null)
                return;

            // position
            var lpos = Random.insideUnitSphere * blastWaveSpawnRadius;
            lpos.y = 0;
            var pos = transform.TransformPoint(lpos);

            // spawn blast wave
            Instantiate(blastWavePrefab, pos, Quaternion.identity);
        }

        private MagicaDirectionalWind Wind
        {
            get
            {
                return GetComponent<MagicaDirectionalWind>();
            }
        }

        private void UpdateDirection()
        {
            var lrot = Quaternion.Euler(angleX, angleY, 0.0f);
            foreach (var t in rotationTransforms)
                if (t)
                    t.localRotation = lrot;

            //transform.rotation = Quaternion.Euler(angleX, angleY, 0.0f);
            Wind.DirectionAngleX = angleX;
            Wind.DirectionAngleY = angleY;
        }
    }
}
