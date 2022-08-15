using UnityEngine;

public static class FindHelper
{
    public static Transform FindChildRecursively(Transform parent, string name)
    {
        Transform t = null;
        t = parent.Find(name);
        if (t == null)
        {
            foreach (Transform tran in parent)
            {
                t = FindChildRecursively(tran, name);
                if (t != null)
                {
                    return t;
                }
            }
        }

        return t;
    }
}
