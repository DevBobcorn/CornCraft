#nullable enable
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Rendering;

public class MeleeWeapon : MonoBehaviour
{
    private readonly List<Collider> slashHits = new();
    private bool slashActive = false;

    public void StartSlash()
    {
        slashHits.Clear();
        slashActive = true;

    }

    public Collider[] EndSlash()
    {
        slashActive = false;

        return slashHits.ToArray();
    }

    void OnTriggerEnter(Collider hit)
    {
        if (!slashActive)
            return;
        
        if (!slashHits.Contains(hit))
        {
            EntityRender? entity;

            if (entity = hit.GetComponentInParent<EntityRender>())
            {
                slashHits.Add(hit);
                Debug.Log($"Slash hit {entity.gameObject.name}");
            }


        }
        
    }

}
