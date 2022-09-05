using Unity.Mathematics;

namespace MinecraftClient.Rendering
{
    public static class ArrayUtil
    {
        public static float3[] GetConcated(float3[] orgArr, float3[] newArr)
        {
            var resArr = new float3[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            newArr.CopyTo(resArr, orgArr.Length);
            return resArr;
        }

        public static float2[] GetConcated(float2[] orgArr, float2[] newArr)
        {
            var resArr = new float2[orgArr.Length + newArr.Length];
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

        public static uint[] GetConcated(uint[] orgArr, uint[] newArr)
        {
            var resArr = new uint[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            newArr.CopyTo(resArr, orgArr.Length);
            return resArr;
        }

        public static float3[] GetConcatedWithOffset(float3[] orgArr, float3[] newArr, float3 offset)
        {
            var resArr = new float3[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            for (int i = 0;i < newArr.Length;i++)
            {
                resArr[orgArr.Length + i] = newArr[i] + offset;
            }
            return resArr;
        }

        public static float2[] GetConcatedWithOffset(float2[] orgArr, float2[] newArr, float2 offset)
        {
            var resArr = new float2[orgArr.Length + newArr.Length];
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

        public static uint[] GetConcatedWithOffset(uint[] orgArr, uint[] newArr, uint offset)
        {
            var resArr = new uint[orgArr.Length + newArr.Length];
            orgArr.CopyTo(resArr, 0);
            for (int i = 0;i < newArr.Length;i++)
            {
                resArr[orgArr.Length + i] = newArr[i] + offset;
            }
            return resArr;
        }

        public static float3[] GetWithOffset(float3[] orgArr, float3 offset)
        {
            var resArr = new float3[orgArr.Length];
            for (int i = 0;i < orgArr.Length;i++)
            {
                resArr[i] = orgArr[i] + offset;
            }
            return resArr;
        }

        public static float2[] GetWithOffset(float2[] orgArr, float2 offset)
        {
            var resArr = new float2[orgArr.Length];
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

        public static uint[] GetWithOffset(uint[] orgArr, uint offset)
        {
            var resArr = new uint[orgArr.Length];
            for (uint i = 0;i < orgArr.Length;i++)
            {
                resArr[i] = orgArr[i] + offset;
            }
            return resArr;
        }

    }

}
