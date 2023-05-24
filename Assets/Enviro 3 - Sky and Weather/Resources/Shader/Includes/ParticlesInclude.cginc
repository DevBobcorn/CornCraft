
struct EllipsZones
{
    float3 pos;
    float radius;
    float3 axis;
    float stretch; 
    float density;
    float feather;
    float padding1;
    float padding2;
}; 

StructuredBuffer<EllipsZones> _EllipsZones : register(t1);
float _EllipsZonesCount;

void WeatherEllipsoids(float3 pos, inout float density)
{
    for (int i = 0; i < _EllipsZonesCount; i++)
    {
        float3 dir = _EllipsZones[i].pos - pos;
        float3 axis = _EllipsZones[i].axis;
        float3 dirAlongAxis = dot(dir, axis) * axis;

        dir = dir + dirAlongAxis * _EllipsZones[i].stretch;
        float distsq = dot(dir, dir);
        float radius = _EllipsZones[i].radius;

        float feather = 1.0;
        feather = (1.0 - smoothstep (radius * feather, radius, distsq));

        float contribution = feather * _EllipsZones[i].density;
        density = clamp(density + contribution,0,1);
    }
}

