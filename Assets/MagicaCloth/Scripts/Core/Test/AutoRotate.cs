// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    public class AutoRotate : MonoBehaviour
    {
        [SerializeField]
        private Vector3 rotateAngle = Vector3.zero;

        [SerializeField]
        [Range(0.1f, 5.0f)]
        private float interval = 2.0f;

        private float time = 0;


        void Start()
        {
        }

        void Update()
        {
            time += Time.deltaTime;
            float ang = (time % interval) / interval * Mathf.PI * 2.0f;
            //var t = Mathf.Cos(ang);
            var t = Mathf.Sin(ang);

            transform.eulerAngles = rotateAngle * t;
        }

        public void OnMoveButton()
        {
            enabled = !enabled;
        }
    }
}
