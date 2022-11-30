// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    public class AutoMove : MonoBehaviour
    {
        [SerializeField]
        private Vector3 direction = Vector3.up;

        [SerializeField]
        private float length = 0.5f;

        [SerializeField]
        [Range(0.1f, 10.0f)]
        private float interval = 2.0f;

        private Vector3 startPosition;
        private float time = 0;


        void Start()
        {
            startPosition = transform.localPosition;
        }

        void Update()
        {
            time += Time.deltaTime;
            float ang = (time % interval) / interval * Mathf.PI * 2.0f;
            //Vector3 offset = direction * Mathf.Sin(ang) * length;
            Vector3 offset = Vector3.Scale(direction, new Vector3(Mathf.Sin(ang), Mathf.Sin(ang), Mathf.Cos(ang))) * length;
            transform.localPosition = startPosition + offset;
        }

        public void OnMoveButton()
        {
            enabled = !enabled;
        }
    }
}
