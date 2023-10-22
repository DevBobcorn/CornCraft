using System.Collections.Generic;
using UnityEngine;

public class CelestiaBridge : MonoBehaviour
{
    [SerializeField] private GameObject bridgeFragment;

    [SerializeField] private GameObject bridgeBlock;
    [SerializeField] private GameObject bridgeEdge;
    [SerializeField] private GameObject regularRail;
    [SerializeField] private GameObject poweredRail;

    [SerializeField] private Transform viewerTransform;
    [SerializeField] private float viewerMoveSpeed = 5F;

    private readonly Queue<CelestiaBridgeFragment> existingFragments = new();
    private int fragmentIndex = 0;
    [SerializeField] private int viewFragmentCount = 2;
    [SerializeField] private int maxFragmentCount = 5;
    [SerializeField] private int fragmentLength = 15;

    private bool stopSignalReceived = false;

    void StopGeneration()
    {
        stopSignalReceived = true;
    }

    void Update()
    {
        viewerTransform.position += new Vector3(0F, 0F, viewerMoveSpeed * Time.deltaTime);

        int targetFragmentIndex = Mathf.RoundToInt(viewerTransform.position.z / fragmentLength) + viewFragmentCount;

        while (fragmentIndex < targetFragmentIndex)
        {
            CreateFragment();
        }
    }

    private void CreateFragment()
    {
        var fragmentObj = GameObject.Instantiate(bridgeFragment);
        fragmentObj.transform.SetParent(transform, false);
        fragmentObj.name = $"Fragment #{fragmentIndex}";

        var fragment = fragmentObj.GetComponent<CelestiaBridgeFragment>();
        fragment.BuildFragment(fragmentIndex, bridgeBlock, bridgeEdge, regularRail, poweredRail);

        fragmentIndex++;

        existingFragments.Enqueue(fragment);

        while (existingFragments.Count > maxFragmentCount)
        {
            var target = existingFragments.Dequeue();
            
            //Debug.Log($"Destroying {target.gameObject.name}");
            Destroy(target.gameObject);
        }
    }
}
