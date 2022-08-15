using UnityEngine;

namespace MinecraftClient.Rendering
{
    public static class ArrayUtil
    {
        public static Vector3[] GetConcated(Vector3[] orgArr, Vector3[] newArr)
        {
            var resArr = new Vector3[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            newArr.CopyTo(resArr, orgArr.Length);
            return resArr;
        }

        public static Vector2[] GetConcated(Vector2[] orgArr, Vector2[] newArr)
        {
            var resArr = new Vector2[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            newArr.CopyTo(resArr, orgArr.Length);
            return resArr;
        }

        public static int[] GetConcated(int[] orgArr, int[] newArr)
        {
            var resArr = new int[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            newArr.CopyTo(resArr, orgArr.Length);
            return resArr;
        }

    }

}
