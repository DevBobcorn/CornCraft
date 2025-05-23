using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimeSunDirection : MonoBehaviour
{
    [SerializeField] private float tiltAngle = 0F;
    [SerializeField] private float normalizedTOD = 0F;

    public Transform sunDir;
    public Transform moonDir;

    public void SetTime(float normalizedTOD)
    {
        this.normalizedTOD = normalizedTOD;

        UpdateDirections();
    }

    public void UpdateDirections()
    {
        float tiltDistance = Mathf.Sin(tiltAngle * Mathf.Deg2Rad);
        float orbCircleRadius = Mathf.Sqrt(1F - tiltDistance * tiltDistance);

        float orbAngle = normalizedTOD * 360F * Mathf.Deg2Rad;
        float x = Mathf.Cos(orbAngle) * orbCircleRadius;
        float y = Mathf.Sin(orbAngle) * orbCircleRadius;
        
        Vector3 orbPos = new(tiltDistance, y, x);

        if (sunDir) sunDir.rotation = Quaternion.LookRotation(-orbPos);
        if (moonDir) moonDir.rotation = Quaternion.LookRotation(orbPos);
    }

    void OnValidate()
    {
        UpdateDirections();
    }
}
