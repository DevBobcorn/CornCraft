using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DistantLands.Cozy
{
    [ExecuteAlways]
    public class FXParent : MonoBehaviour
    {
        void OnEnable()
        {

            if (transform.parent == null)
                DestroyImmediate(gameObject);

        }
    }
}