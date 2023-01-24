#region Using statements

using Bitgem.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class WaterVolumeBase : MonoBehaviour
    {
        #region Constants

        public const int MAX_TILES_X = 100;
        public const int MAX_TILES_Y = 50;
        public const int MAX_TILES_Z = 100;

        #endregion

        #region Flag lists

        [System.Flags]
        public enum TileFace : int
        {
            NegX = 1,
            PosX = 2,
            NegZ = 4,
            PosZ = 8
        }

        #endregion

        #region Private fields

        protected bool isDirty = true;

        private UnityEngine.Mesh mesh = null;
        private MeshFilter meshFilter = null;

        private bool[,,] tiles = null;

        #endregion

        #region Public fields

        [FlagEnum]
        public TileFace IncludeFaces = TileFace.NegX | TileFace.NegZ | TileFace.PosX | TileFace.PosZ;

        [FlagEnum]
        public TileFace IncludeFoam = TileFace.NegX | TileFace.NegZ | TileFace.PosX | TileFace.PosZ;

        [Range(0.1f, 100f)]
        public float TileSize = 1f;

        public bool ShowDebug = true;
        public bool RealtimeUpdates = false;

        #endregion

        #region Private methods

        private void ensureReferences()
        {
            // ensure a mesh filter
            if (meshFilter == null)
            {
                mesh = null;
                meshFilter = gameObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }

            // ensure a mesh
            if (mesh == null)
            {
                mesh = meshFilter.sharedMesh;
                if (mesh == null || mesh.name != "WaterVolume-" + gameObject.GetInstanceID())
                {
                    mesh = new UnityEngine.Mesh();
                    mesh.name = "WaterVolume-" + gameObject.GetInstanceID();
                }
            }

            // apply the mesh to the filter
            meshFilter.sharedMesh = mesh;
        }

        #endregion

        #region Public methods

        public float? GetHeight(Vector3 _position)
        {
            // convert the position to a tile
            var x = Mathf.FloorToInt((_position.x - transform.position.x + 0.5f) / TileSize);
            var z = Mathf.FloorToInt((_position.z - transform.position.z + 0.5f) / TileSize);

            // check if out of bounds
            if (x < 0 || x >= MAX_TILES_X || z < 0 || z >= MAX_TILES_Z)
            {
                return null;
            }

            // find the highest active water block in the column
            // TODO : could be reworked to cater for gaps
            for (var y = MAX_TILES_Y - 1; y >= 0; y--)
            {
                if (tiles[x, y, z])
                {
                    return transform.position.y + y * TileSize;
                }
            }

            // no water in the column
            return null;
        }

        public void Rebuild()
        {
            Debug.Log("rebuilding water volume \"" + gameObject.name + "\"");

            // ensure references to components before trying to use them
            ensureReferences();

            // delete any existing mesh
            mesh.Clear();

            // allow any child class to generate the tiles to build from
            tiles = new bool[MAX_TILES_X, MAX_TILES_Y, MAX_TILES_Z];
            GenerateTiles(ref tiles);

            // prepare buffers for the mesh data
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var indices = new List<int>();

            // iterate the tiles
            for (var x = 0; x < MAX_TILES_X; x++)
            {
                for (var y = 0; y < MAX_TILES_Y; y++)
                {
                    for (var z = 0; z < MAX_TILES_Z; z++)
                    {
                        // check there is water here
                        if (!tiles[x, y, z])
                        {
                            continue;
                        }

                        // calculate tile position
                        var x0 = x * TileSize - 0.5f;
                        var x1 = x0 + TileSize;
                        var y0 = y * TileSize - 0.5f;
                        var y1 = y0 + TileSize;
                        var z0 = z * TileSize - 0.5f;
                        var z1 = z0 + TileSize;
                        var ux0 = x0 + transform.position.x;
                        var ux1 = x1 + transform.position.x;
                        var uy0 = y0 + transform.position.y;
                        var uy1 = y1 + transform.position.y;
                        var uz0 = z0 + transform.position.z;
                        var uz1 = z1 + transform.position.z;

                        // check for edges
                        var negX = x == 0 || !tiles[x - 1, y, z];
                        var posX = x == MAX_TILES_X - 1 || !tiles[x + 1, y, z];
                        var negY = y == 0 || !tiles[x, y - 1, z];
                        var posY = y == MAX_TILES_Y - 1 || !tiles[x, y + 1, z];
                        var negZ = z == 0 || !tiles[x, y, z - 1];
                        var posZ = z == MAX_TILES_Z - 1 || !tiles[x, y, z + 1];
                        var negXnegZ = !negX && !negZ && x > 0 && z > 0 && !tiles[x - 1, y, z - 1];
                        var negXposZ = !negX && !posZ && x > 0 && z < MAX_TILES_Z && !tiles[x - 1, y, z + 1];
                        var posXposZ = !posX && !posZ && x < MAX_TILES_X && z < MAX_TILES_Z && !tiles[x + 1, y, z + 1];
                        var posXnegZ = !posX && !negZ && x < MAX_TILES_X && z > 0 && !tiles[x + 1, y, z - 1];
                        var faceNegX = negX && (IncludeFaces & TileFace.NegX) == TileFace.NegX;
                        var facePosX = posX && (IncludeFaces & TileFace.PosX) == TileFace.PosX;
                        var faceNegZ = negZ && (IncludeFaces & TileFace.NegZ) == TileFace.NegZ;
                        var facePosZ = posZ && (IncludeFaces & TileFace.PosZ) == TileFace.PosZ;
                        var foamNegX = negX && (IncludeFoam & TileFace.NegX) == TileFace.NegX;
                        var foamPosX = posX && (IncludeFoam & TileFace.PosX) == TileFace.PosX;
                        var foamNegZ = negZ && (IncludeFoam & TileFace.NegZ) == TileFace.NegZ;
                        var foamPosZ = posZ && (IncludeFoam & TileFace.PosZ) == TileFace.PosZ;
                        var foamNegXnegZ = negXnegZ && ((IncludeFoam & TileFace.NegX) == TileFace.NegX || (IncludeFoam & TileFace.NegZ) == TileFace.NegZ);
                        var foamNegXposZ = negXposZ && ((IncludeFoam & TileFace.PosX) == TileFace.PosX || (IncludeFoam & TileFace.PosZ) == TileFace.PosZ);
                        var foamPosXposZ = posXposZ && ((IncludeFoam & TileFace.NegZ) == TileFace.NegZ || (IncludeFoam & TileFace.PosZ) == TileFace.PosZ);
                        var foamPosXnegZ = posXnegZ && ((IncludeFoam & TileFace.PosZ) == TileFace.PosZ || (IncludeFoam & TileFace.NegZ) == TileFace.NegZ);

                        // create the top face
                        if (y == MAX_TILES_Y - 1 || !tiles[x, y + 1, z])
                        {
                            vertices.Add(new Vector3(x0, y1, z0));
                            vertices.Add(new Vector3(x0, y1, z1));
                            vertices.Add(new Vector3(x1, y1, z1));
                            vertices.Add(new Vector3(x1, y1, z0));
                            normals.Add(new Vector3(0, 1, 0));
                            normals.Add(new Vector3(0, 1, 0));
                            normals.Add(new Vector3(0, 1, 0));
                            normals.Add(new Vector3(0, 1, 0));
                            uvs.Add(new Vector2(ux0, uz0));
                            uvs.Add(new Vector2(ux0, uz1));
                            uvs.Add(new Vector2(ux1, uz1));
                            uvs.Add(new Vector2(ux1, uz0));
                            colors.Add(foamNegX || foamNegZ || foamNegXnegZ ? Color.red : Color.black);
                            colors.Add(foamNegX || foamPosZ || foamNegXposZ ? Color.red : Color.black);
                            colors.Add(foamPosX || foamPosZ || foamPosXposZ ? Color.red : Color.black);
                            colors.Add(foamPosX || foamNegZ || foamPosXnegZ ? Color.red : Color.black);
                            var v = vertices.Count - 4;
                            if (foamNegX && foamPosZ || foamPosX && foamNegZ)
                            {
                                indices.Add(v + 1);
                                indices.Add(v + 2);
                                indices.Add(v + 3);
                                indices.Add(v + 3);
                                indices.Add(v);
                                indices.Add(v + 1);
                            }
                            else
                            {
                                indices.Add(v);
                                indices.Add(v + 1);
                                indices.Add(v + 2);
                                indices.Add(v + 2);
                                indices.Add(v + 3);
                                indices.Add(v);
                            }
                        }

                        // create the side faces
                        if (faceNegX)
                        {
                            vertices.Add(new Vector3(x0, y0, z1));
                            vertices.Add(new Vector3(x0, y1, z1));
                            vertices.Add(new Vector3(x0, y1, z0));
                            vertices.Add(new Vector3(x0, y0, z0));
                            normals.Add(new Vector3(-1, 0, 0));
                            normals.Add(new Vector3(-1, 0, 0));
                            normals.Add(new Vector3(-1, 0, 0));
                            normals.Add(new Vector3(-1, 0, 0));
                            uvs.Add(new Vector2(uz1, uy0));
                            uvs.Add(new Vector2(uz1, uy1));
                            uvs.Add(new Vector2(uz0, uy1));
                            uvs.Add(new Vector2(uz0, uy0));
                            colors.Add(Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(Color.black);
                            var v = vertices.Count - 4;
                            indices.Add(v);
                            indices.Add(v + 1);
                            indices.Add(v + 2);
                            indices.Add(v + 2);
                            indices.Add(v + 3);
                            indices.Add(v);
                        }
                        if (facePosX)
                        {
                            vertices.Add(new Vector3(x1, y0, z0));
                            vertices.Add(new Vector3(x1, y1, z0));
                            vertices.Add(new Vector3(x1, y1, z1));
                            vertices.Add(new Vector3(x1, y0, z1));
                            normals.Add(new Vector3(1, 0, 0));
                            normals.Add(new Vector3(1, 0, 0));
                            normals.Add(new Vector3(1, 0, 0));
                            normals.Add(new Vector3(1, 0, 0));
                            uvs.Add(new Vector2(uz0, uy0));
                            uvs.Add(new Vector2(uz0, uy1));
                            uvs.Add(new Vector2(uz1, uy1));
                            uvs.Add(new Vector2(uz1, uy0));
                            colors.Add(Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(Color.black);
                            var v = vertices.Count - 4;
                            indices.Add(v);
                            indices.Add(v + 1);
                            indices.Add(v + 2);
                            indices.Add(v + 2);
                            indices.Add(v + 3);
                            indices.Add(v);
                        }
                        if (faceNegZ)
                        {
                            vertices.Add(new Vector3(x0, y0, z0));
                            vertices.Add(new Vector3(x0, y1, z0));
                            vertices.Add(new Vector3(x1, y1, z0));
                            vertices.Add(new Vector3(x1, y0, z0));
                            normals.Add(new Vector3(0, 0, -1));
                            normals.Add(new Vector3(0, 0, -1));
                            normals.Add(new Vector3(0, 0, -1));
                            normals.Add(new Vector3(0, 0, -1));
                            uvs.Add(new Vector2(ux0, uy0));
                            uvs.Add(new Vector2(ux0, uy1));
                            uvs.Add(new Vector2(ux1, uy1));
                            uvs.Add(new Vector2(ux1, uy0));
                            colors.Add(Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(Color.black);
                            var v = vertices.Count - 4;
                            indices.Add(v);
                            indices.Add(v + 1);
                            indices.Add(v + 2);
                            indices.Add(v + 2);
                            indices.Add(v + 3);
                            indices.Add(v);
                        }
                        if (facePosZ)
                        {
                            vertices.Add(new Vector3(x1, y0, z1));
                            vertices.Add(new Vector3(x1, y1, z1));
                            vertices.Add(new Vector3(x0, y1, z1));
                            vertices.Add(new Vector3(x0, y0, z1));
                            normals.Add(new Vector3(0, 0, 1));
                            normals.Add(new Vector3(0, 0, 1));
                            normals.Add(new Vector3(0, 0, 1));
                            normals.Add(new Vector3(0, 0, 1));
                            uvs.Add(new Vector2(ux1, uy0));
                            uvs.Add(new Vector2(ux1, uy1));
                            uvs.Add(new Vector2(ux0, uy1));
                            uvs.Add(new Vector2(ux0, uy0));
                            colors.Add(Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(posY ? Color.red : Color.black);
                            colors.Add(Color.black);
                            var v = vertices.Count - 4;
                            indices.Add(v);
                            indices.Add(v + 1);
                            indices.Add(v + 2);
                            indices.Add(v + 2);
                            indices.Add(v + 3);
                            indices.Add(v);
                        }
                    }
                }
            }

            // apply the buffers
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);

            // update
            mesh.RecalculateBounds();
            //mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            meshFilter.sharedMesh = mesh;

            // flag as clean
            isDirty = false;
        }

        #endregion

        #region Virtual methods

        protected virtual void GenerateTiles(ref bool[,,] _tiles) { }
        public virtual void Validate() { }

        #endregion

        #region MonoBehaviour events

        void OnValidate()
        {
            // keep tile size in a sensible range
            TileSize = Mathf.Clamp(TileSize, 0.1f, 100f);

            // allow any child class to perform validation
            Validate();

            // flag as needing rebuilding
            isDirty = true;
        }

        void Update()
        {
            // rebuild if needed
            if (isDirty || (!Application.isPlaying && RealtimeUpdates))
            {
                Rebuild();
            }
        }

        #endregion
    }
}