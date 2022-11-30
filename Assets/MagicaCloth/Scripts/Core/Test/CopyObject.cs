// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections;
using UnityEngine;

namespace MagicaCloth
{
    public class CopyObject : MonoBehaviour
    {
        public int seed = 0;
        public int count = 1;
        public float radius = 5;
        public GameObject prefab;
        //public bool delay = false;
        public int delayFrame = 0;

        private void Awake()
        {
        }

        void Start()
        {
            StartCoroutine(CreateObject());
        }

        IEnumerator CreateObject()
        {
            Random.InitState(seed);
            for (int i = 0; i < count; i++)
            {
                var obj = GameObject.Instantiate(prefab);
                var lpos = Random.insideUnitCircle * radius;
                obj.transform.position = transform.position + new Vector3(lpos.x, 0.0f, lpos.y);

                //if (delay)
                //    yield return null;
                if (delayFrame > 0)
                    for (int j = 0; j < delayFrame; j++)
                        yield return null;
            }
        }
    }
}
