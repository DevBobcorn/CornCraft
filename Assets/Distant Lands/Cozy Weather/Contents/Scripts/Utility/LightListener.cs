using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DistantLands.Cozy
{

    public class LightListener : MonoBehaviour
    {

        public Material onMat;
        public Material offMat;
        private new Light light;
        private Renderer render;

        public void TurnOnLight()
        {

            if (light == null)
                light = GetComponent<Light>();
            if (render == null)
                render = GetComponent<Renderer>();

            render.material = onMat;
            light.enabled = true;

        }

        public void TurnOffLight()
        {

            if (light == null)
                light = GetComponent<Light>();
            if (render == null)
                render = GetComponent<Renderer>();

            render.material = offMat;
            light.enabled = false;
        }
    }
}