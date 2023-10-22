using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestiaBridge : MonoBehaviour
{
    [SerializeField] private GameObject bridgeFragment;

    [SerializeField] private GameObject bridgeBlock;
    [SerializeField] private GameObject bridgeEdge;
    [SerializeField] private GameObject portalFrame;
    [SerializeField] private GameObject portalBlock;
    [SerializeField] private GameObject regularRail;
    [SerializeField] private GameObject poweredRail;

    [SerializeField] private Transform viewerTransform;
    [SerializeField] private float viewerMoveSpeed = 5F;
    [SerializeField] private float viewerStopDistance = 5F;
    [SerializeField] private float enterPortalSpeed = 55F;

    private readonly Queue<CelestiaBridgeFragment> existingFragments = new();
    private int nextFragmentIndex = 0;
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

        yield return null;

        CreateFinalFragment(stopFragmentIndex);

        yield return null;

        callback.Invoke();
    }

    public void EnterPortal()
    {
        enterSignalReceived = true;
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
        var fragmentObj = GameObject.Instantiate(bridgeFragment);
        fragmentObj.transform.SetParent(transform, false);
        fragmentObj.name = $"Fragment #{fragIndex}";

        var fragment = fragmentObj.GetComponent<CelestiaBridgeFragment>();
        fragment.BuildFragment(fragIndex, bridgeBlock, bridgeEdge, regularRail, poweredRail);

        existingFragments.Enqueue(fragment);

        while (existingFragments.Count > maxFragmentCount)
        {
            var target = existingFragments.Dequeue();
            
            //Debug.Log($"Destroying {target.gameObject.name}");
            Destroy(target.gameObject);
        }
    }

    private void CreateFinalFragment(int fragIndex)
    {
        var fragmentObj = GameObject.Instantiate(bridgeFragment);
        fragmentObj.transform.SetParent(transform, false);
        fragmentObj.name = $"Final Fragment #{fragIndex}";

        var fragment = fragmentObj.GetComponent<CelestiaBridgeFragment>();
        fragment.BuildFinalFragment(fragIndex, bridgeBlock, bridgeEdge, portalFrame, portalBlock);
    }
}
