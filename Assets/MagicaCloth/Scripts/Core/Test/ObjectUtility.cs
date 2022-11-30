// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// オブジェクト生成ユーティリティ
    /// 主にデバッグ用
    /// </summary>
    public static class ObjectUtility
    {
        /// <summary>
        /// 範囲内に散らばったキューブを作成する
        /// </summary>
        /// <param name="count"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static GameObject[] PlaceRandomCubes(int count, Vector3 center, float radius)
        {
            var cubes = new GameObject[count];
            var cubeToCopy = MakeStrippedCube();

            for (int i = 0; i < count; i++)
            {
                var cube = GameObject.Instantiate(cubeToCopy);
                cube.transform.position = center + Random.insideUnitSphere * radius;
                cubes[i] = cube;
            }

            GameObject.Destroy(cubeToCopy);

            return cubes;
        }

        /// <summary>
        /// キューブを作成する
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static GameObject[] PlaceRandomCubes(int count)
        {
            return PlaceRandomCubes(count, Vector3.zero, 0.0f);
        }

        /// <summary>
        /// 影を落とさず当たり判定を取らないキューブを１つ作成する
        /// </summary>
        /// <returns></returns>
        public static GameObject MakeStrippedCube()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            //turn off shadows entirely
            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // disable collision
            var collider = cube.GetComponent<Collider>();
            collider.enabled = false;

            return cube;
        }

        public static GameObject[] PlaceRandomGameObject(int count, Vector3 center, float radius, GameObject prefab)
        {
            var objs = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                var obj = GameObject.Instantiate(prefab);
                obj.transform.position = center + Random.insideUnitSphere * radius;
                objs[i] = obj;
            }

            return objs;
        }
    }
}
