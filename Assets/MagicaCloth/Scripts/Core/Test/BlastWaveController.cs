// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections;
using UnityEngine;

namespace MagicaCloth
{
    public class BlastWaveController : MonoBehaviour
    {
        public MagicaAreaWind wind;
        public float attenuationStartTime = 1.0f;
        public float attenuationTime = 1.0f;

        IEnumerator Start()
        {
            if (wind)
            {
                float main = wind.Main;

                // Wait until attenuation starts.
                yield return new WaitForSeconds(attenuationStartTime);

                // Attenuation.
                float time = 0;
                while (time < attenuationTime)
                {
                    float t = Mathf.Clamp01(1.0f - time / attenuationTime);

                    wind.Main = main * t;

                    time += Time.deltaTime;
                    yield return null;
                }

                // destroy.
                Destroy(gameObject);
            }
        }
    }
}
