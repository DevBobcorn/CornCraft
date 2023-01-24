#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    [AddComponentMenu("Bitgem/Water  Volume (Box)")]
    public class WaterVolumeBox : WaterVolumeBase
    {
        #region Public fields

        public Vector3 Dimensions = Vector3.zero;

        #endregion

        #region Public methods

        protected override void GenerateTiles(ref bool[,,] _tiles)
        {
            // calculate volume in tiles
            var maxX = Mathf.Clamp(Mathf.RoundToInt(Dimensions.x / TileSize), 1, MAX_TILES_X);
            var maxY = Mathf.Clamp(Mathf.RoundToInt(Dimensions.y / TileSize), 1, MAX_TILES_Y);
            var maxZ = Mathf.Clamp(Mathf.RoundToInt(Dimensions.z / TileSize), 1, MAX_TILES_Z);

            // populate the tiles with a box volume
            for (var x = 0; x < maxX; x++)
            {
                for (var y = 0; y < maxY; y++)
                {
                    for (var z = 0; z < maxZ; z++)
                    {
                        _tiles[x, y, z] = true;
                    }
                }
            }
        }

        public override void Validate()
        {
            // keep values sensible
            Dimensions.x = Mathf.Clamp(Dimensions.x, 1, MAX_TILES_X);
            Dimensions.y = Mathf.Clamp(Dimensions.y, 1, MAX_TILES_Y);
            Dimensions.z = Mathf.Clamp(Dimensions.z, 1, MAX_TILES_Z);
        }

        #endregion
    }
}