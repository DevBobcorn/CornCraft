using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp
{
    public class CelestiaBridge : MonoBehaviour
    {
        [SerializeField] private GameObject bridgeFragmentPrefab;
        [SerializeField] private GameObject portalFragmentPrefab;

        [SerializeField] private Transform viewerTransform;
        [SerializeField] private float viewerMoveSpeed = 5F;
        [SerializeField] private float viewerStopDistance = 5F;
        [SerializeField] private float enterPortalSpeed = 55F;

        private readonly Queue<CelestiaBridgeFragment> existingFragments = new();
        private int nextFragmentIndex = 0;
        private CelestiaPortalFragment portalFragment;

        [SerializeField] private int viewFragmentCount = 2;
        [SerializeField] private int maxFragmentCount = 5;
        [SerializeField] private int fragmentLength = 15;

        private bool enterSignalReceived = false;
        private bool stopSignalReceived = false;
        private int stopFragmentIndex = 0;

        public IEnumerator StopAndMakePortal(Action callback)
        {
            stopSignalReceived = true;
            stopFragmentIndex = Mathf.CeilToInt(viewerTransform.position.z / fragmentLength) + viewFragmentCount;

            while (nextFragmentIndex < stopFragmentIndex)
            {
                CreateFragment(nextFragmentIndex);
                nextFragmentIndex++;

                yield return new WaitForSecondsRealtime(fragmentLength / viewerMoveSpeed);
            }

            portalFragment = CreatePortalFragment(stopFragmentIndex);

            while (!portalFragment.FrameGenerationComplete)
            {
                yield return null;
            }

            yield return null;

            callback.Invoke();
        }

        public void EnterPortal()
        {
            enterSignalReceived = true;
            // Make the portal glow
            portalFragment.EnablePortalEmission();
        }

        void Update()
        {
            if (enterSignalReceived)
            {
                viewerMoveSpeed += Time.deltaTime * enterPortalSpeed;
                viewerTransform.position += new Vector3(0F, 0F, viewerMoveSpeed * Time.deltaTime);
            }
            else if (stopSignalReceived)
            {
                var posLim = (stopFragmentIndex - 2) * fragmentLength;
                var speed = viewerMoveSpeed;
                var dist = posLim - viewerTransform.position.z;

                if (dist > 0F && dist < viewerStopDistance) // Slow down...
                {
                    speed *= dist / viewerStopDistance;
                }

                var newPos = viewerTransform.position + new Vector3(0F, 0F, speed * Time.deltaTime);
                newPos.z = Mathf.Min(newPos.z, posLim);
                
                viewerTransform.position = newPos;
            }
            else
            {
                viewerTransform.position += new Vector3(0F, 0F, viewerMoveSpeed * Time.deltaTime);

                int targetFragmentIndex = Mathf.RoundToInt(viewerTransform.position.z / fragmentLength) + viewFragmentCount;

                while (nextFragmentIndex < targetFragmentIndex)
                {
                    CreateFragment(nextFragmentIndex);
                    nextFragmentIndex++;
                }
            }
        }

        private void CreateFragment(int fragIndex)
        {
            var fragmentObj = GameObject.Instantiate(bridgeFragmentPrefab);
            fragmentObj.transform.SetParent(transform, false);
            fragmentObj.name = $"Fragment #{fragIndex}";

            var fragment = fragmentObj.GetComponent<CelestiaBridgeFragment>();
            fragment.BuildFragment(fragIndex);

            existingFragments.Enqueue(fragment);

            while (existingFragments.Count > maxFragmentCount)
            {
                var target = existingFragments.Dequeue();
                
                //Debug.Log($"Destroying {target.gameObject.name}");
                Destroy(target.gameObject);
            }
        }

        private CelestiaPortalFragment CreatePortalFragment(int fragIndex)
        {
            var fragmentObj = GameObject.Instantiate(portalFragmentPrefab);
            fragmentObj.transform.SetParent(transform, false);
            fragmentObj.name = $"Portal Fragment #{fragIndex}";

            var fragment = fragmentObj.GetComponent<CelestiaPortalFragment>();
            fragment.BuildFragment(fragIndex);

            return fragment;
        }
    }
}