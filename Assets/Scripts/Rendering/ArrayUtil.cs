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

        public static Vector3[] GetConcatedWithOffset(Vector3[] orgArr, Vector3[] newArr, Vector3 offset)
        {
            var resArr = new Vector3[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            for (int i = 0;i < newArr.Length;i++)
            {
                resArr[orgArr.Length + i] = newArr[i] + offset;
            }
            return resArr;
        }

        public static Vector2[] GetConcatedWithOffset(Vector2[] orgArr, Vector2[] newArr, Vector2 offset)
        {
            var resArr = new Vector2[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            for (int i = 0;i < newArr.Length;i++)
            {
                resArr[orgArr.Length + i] = newArr[i] + offset;
            }
            return resArr;
        }

        public static int[] GetConcatedWithOffset(int[] orgArr, int[] newArr, int offset)
        {
            var resArr = new int[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            for (int i = 0;i < newArr.Length;i++)
            {
                resArr[orgArr.Length + i] = newArr[i] + offset;
            }
            return resArr;
        }

        public static Vector3[] GetWithOffset(Vector3[] orgArr, Vector3 offset)
        {
            var resArr = new Vector3[orgArr.Length];
            for (int i = 0;i < orgArr.Length;i++)
            {
                resArr[i] = orgArr[i] + offset;
            }
            return resArr;
        }

        public static Vector2[] GetWithOffset(Vector2[] orgArr, Vector2 offset)
        {
            var resArr = new Vector2[orgArr.Length];
            for (int i = 0;i < orgArr.Length;i++)
            {
                resArr[i] = orgArr[i] + offset;
            }
            return resArr;
        }

        public static int[] GetWithOffset(int[] orgArr, int offset)
        {
            var resArr = new int[orgArr.Length];
            for (int i = 0;i < orgArr.Length;i++)
            {
                resArr[i] = orgArr[i] + offset;
            }
            return resArr;
        }

    }

}
