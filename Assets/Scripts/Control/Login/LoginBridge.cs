using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LoginBridge : MonoBehaviour
{
    [SerializeField] private GameObject bridgeFragment;

    [SerializeField] private GameObject bridgeBlock;
    [SerializeField] private GameObject bridgeEdge;
    [SerializeField] private GameObject bridgePillar;

    [SerializeField] private Transform viewerTransform;

    private readonly Queue<BridgeFragment> existingFragments = new();
    private int fragmentIndex = 0;
    [SerializeField] private int viewFragmentCount = 2;
    [SerializeField] private int maxFragmentCount = 5;
    [SerializeField] private int fragmentLength = 15;

    void Update()
    {
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

        var fragment = fragmentObj.GetComponent<BridgeFragment>();
        fragment.BuildFragment(fragmentIndex, bridgeBlock, bridgeEdge);

        fragmentIndex++;

        existingFragments.Enqueue(fragment);

        while (existingFragments.Count > maxFragmentCount)
        {
            var target = existingFragments.Dequeue();
            
            Debug.Log($"Destroying {target.gameObject.name}");
            Destroy(target.gameObject);
        }
    }
}
