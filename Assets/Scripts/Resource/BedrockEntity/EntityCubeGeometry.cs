using Unity.Mathematics;

namespace CraftSharp.Resource
{
    public static class EntityCubeGeometry
    {
        public static void Build(ref EntityVertexBuffer buffer, int texWidth, int texHeight, bool mirrorUV, float3 bonePivot, EntityModelCube cube)
        {
            // Unity                   Minecraft            Top Quad Vertices
            //  A +Z (East)             A +X (East)          v0---v1
            //  |                       |                    |     |
            //  *---> +X (South)        *---> +Z (South)     v2---v3

            // Origin: (u, v)
            // 
            //  O  A------B------K
            //     |      |      | sx = 2
            //     |      |      |
            //  C--D------E--F---L--M
            //  |  |      |  |      | sy = 1
            //  G--H------I--J------N
            //      sz = 6    sz = 6

            int vertOffset = buffer.vert.Length;
            int newLength = vertOffset + 24;

            var verts = new float3[newLength];
            var txuvs = new float2[newLength];

            buffer.vert.CopyTo(verts, 0);
            buffer.txuv.CopyTo(txuvs, 0);

            // cube position in bone local space
            float x = cube.Origin.x - bonePivot.x;
            float y = cube.Origin.y - bonePivot.y;
            float z = cube.Origin.z - bonePivot.z;

            float sx =  cube.Size.x;
            float sy =  cube.Size.y;
            float sz =  cube.Size.z;
            float u = cube.UV.x;
            float v = cube.UV.y;

            float y0 = 1F -             v / texHeight;
            float y1 = 1F -      (sx + v) / texHeight;
            float y2 = 1F - (sx + sy + v) / texHeight;

            var A = new float2(               (sx + u) / texWidth, y0);
            var B = new float2(          (sx + sz + u) / texWidth, y0);
            var K = new float2(     (sx + sz + sz + u) / texWidth, y0);

            var C = new float2(                      u / texWidth, y1);
            var D = new float2(               (sx + u) / texWidth, y1);
            var E = new float2(          (sx + sz + u) / texWidth, y1);
            var F = new float2(     (sx + sz + sx + u) / texWidth, y1);
            var L = new float2(     (sx + sz + sz + u) / texWidth, y1);
            var M = new float2((sx + sz + sx + sz + u) / texWidth, y1);

            var G = new float2(                      u / texWidth, y2);
            var H = new float2(               (sx + u) / texWidth, y2);
            var I = new float2(          (sx + sz + u) / texWidth, y2);
            var J = new float2(     (sx + sz + sx + u) / texWidth, y2);
            var N = new float2((sx + sz + sx + sz + u) / texWidth, y2);

            // Inflate after we get all uv coordinates so that they don't get affected
            if (cube.Inflate != 0F)
            {
                var inf = cube.Inflate; // This could be a negative value for deflation
                // Inflate the cube
                sx += inf * 2F;
                sy += inf * 2F;
                sz += inf * 2F;
                // Offset the cube
                x -= inf;
                y -= inf;
                z -= inf;
            }

            // Up
            verts[vertOffset]     = new( 0 + x, sy + y, sz + z); // 4 => 2
            verts[vertOffset + 1] = new(sx + x, sy + y, sz + z); // 5 => 3
            verts[vertOffset + 2] = new( 0 + x, sy + y,  0 + z); // 3 => 1
            verts[vertOffset + 3] = new(sx + x, sy + y,  0 + z); // 2 => 0
            if (mirrorUV)
            {
                txuvs[vertOffset]     = E; txuvs[vertOffset + 1] = B;
                txuvs[vertOffset + 2] = D; txuvs[vertOffset + 3] = A;
            }
            else
            {
                txuvs[vertOffset]     = D; txuvs[vertOffset + 1] = A;
                txuvs[vertOffset + 2] = E; txuvs[vertOffset + 3] = B;
            }
            vertOffset += 4;

            // Down
            verts[vertOffset]     = new( 0 + x,  0 + y,  0 + z); // 0 => 0
            verts[vertOffset + 1] = new(sx + x,  0 + y,  0 + z); // 1 => 1
            verts[vertOffset + 2] = new( 0 + x,  0 + y, sz + z); // 7 => 3
            verts[vertOffset + 3] = new(sx + x,  0 + y, sz + z); // 6 => 2
            if (mirrorUV)
            {
                txuvs[vertOffset]     = E; txuvs[vertOffset + 1] = B;
                txuvs[vertOffset + 2] = L; txuvs[vertOffset + 3] = K;
            }
            else
            {
                txuvs[vertOffset]     = L; txuvs[vertOffset + 1] = K;
                txuvs[vertOffset + 2] = E; txuvs[vertOffset + 3] = B;
            }
            vertOffset += 4;

            // South
            verts[vertOffset]     = new(sx + x, sy + y,  0 + z); // 2 => 1
            verts[vertOffset + 1] = new(sx + x, sy + y, sz + z); // 5 => 2
            verts[vertOffset + 2] = new(sx + x,  0 + y,  0 + z); // 1 => 0
            verts[vertOffset + 3] = new(sx + x,  0 + y, sz + z); // 6 => 3
            if (mirrorUV)
            {
                txuvs[vertOffset]     = M; txuvs[vertOffset + 1] = F;
                txuvs[vertOffset + 2] = N; txuvs[vertOffset + 3] = J;
            }
            else
            {
                txuvs[vertOffset]     = F; txuvs[vertOffset + 1] = M;
                txuvs[vertOffset + 2] = J; txuvs[vertOffset + 3] = N;
            }
            vertOffset += 4;

            // North (Facade)
            verts[vertOffset]     = new( 0 + x, sy + y, sz + z); // 4 => 2
            verts[vertOffset + 1] = new( 0 + x, sy + y,  0 + z); // 3 => 1
            verts[vertOffset + 2] = new( 0 + x,  0 + y, sz + z); // 7 => 3
            verts[vertOffset + 3] = new( 0 + x,  0 + y,  0 + z); // 0 => 0
            if (mirrorUV)
            {
                txuvs[vertOffset]     = E; txuvs[vertOffset + 1] = D;
                txuvs[vertOffset + 2] = I; txuvs[vertOffset + 3] = H;
            }
            else
            {
                txuvs[vertOffset]     = D; txuvs[vertOffset + 1] = E;
                txuvs[vertOffset + 2] = H; txuvs[vertOffset + 3] = I;
            }
            vertOffset += 4;

            // East
            verts[vertOffset]     = new(sx + x, sy + y, sz + z); // 5 => 1
            verts[vertOffset + 1] = new( 0 + x, sy + y, sz + z); // 4 => 0
            verts[vertOffset + 2] = new(sx + x,  0 + y, sz + z); // 6 => 2
            verts[vertOffset + 3] = new( 0 + x,  0 + y, sz + z); // 7 => 3
            if (mirrorUV)
            {
                txuvs[vertOffset]     = F; txuvs[vertOffset + 1] = E;
                txuvs[vertOffset + 2] = J; txuvs[vertOffset + 3] = I;
            }
            else
            {
                txuvs[vertOffset]     = C; txuvs[vertOffset + 1] = D;
                txuvs[vertOffset + 2] = G; txuvs[vertOffset + 3] = H;
            }
            vertOffset += 4;

            // West
            verts[vertOffset]     = new( 0 + x, sy + y,  0 + z); // 3 => 3
            verts[vertOffset + 1] = new(sx + x, sy + y,  0 + z); // 2 => 2
            verts[vertOffset + 2] = new( 0 + x,  0 + y,  0 + z); // 0 => 0
            verts[vertOffset + 3] = new(sx + x,  0 + y,  0 + z); // 1 => 1
            if (mirrorUV)
            {
                txuvs[vertOffset]     = D; txuvs[vertOffset + 1] = C;
                txuvs[vertOffset + 2] = H; txuvs[vertOffset + 3] = G;
            }
            else
            {
                txuvs[vertOffset]     = E; txuvs[vertOffset + 1] = F;
                txuvs[vertOffset + 2] = I; txuvs[vertOffset + 3] = J;
            }
            // Not necessary vertOffset += 4;

            // rotation pivot position in bone local space
            var rotPivot = cube.Pivot - bonePivot;

            Rotations.RotateVertices(ref verts, rotPivot, cube.Rotation, 16F, buffer.vert.Length);

            buffer.vert = verts;
            buffer.txuv = txuvs;
        }
    }
}