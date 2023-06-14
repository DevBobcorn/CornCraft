float3 mod(float3 x, float3 y)
{
	return x - y * floor(x / y);
}

float4 mod(float4 x, float4 y)
{
	return x - y * floor(x / y);
}

float3 mod289(float3 x)
{
	return x - floor(x / 289.0) * 289.0;
}

float4 mod289(float4 x)
{
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 permute(float4 x)
{
	return mod289(((x*34.0) + 1.0)*x);
}

float3 fade(float3 t) {
	return t*t*t*(t*(t*6.0 - 15.0) + 10.0);
}

float4 taylorInvSqrt(float4 r)
{
	return 1.79284291400159 - 0.85373472095314 * r;
}

float2 fade(float2 t) {
	return t*t*t*(t*(t*6.0 - 15.0) + 10.0);
}
	
// Classic Perlin noise, periodic variant
float penoise(float2 P, float2 rep)
{
	float4 Pi = floor(P.xyxy) + float4(0.0, 0.0, 1.0, 1.0);
	float4 Pf = frac(P.xyxy) - float4(0.0, 0.0, 1.0, 1.0);
	Pi = mod(Pi, rep.xyxy); // To create noise with explicit period
	Pi = mod289(Pi);        // To avoid truncation effects in permutation
	float4 ix = Pi.xzxz;
	float4 iy = Pi.yyww;
	float4 fx = Pf.xzxz;
	float4 fy = Pf.yyww;

	float4 i = permute(permute(ix) + iy);

	float4 gx = frac(i * (1.0 / 41.0)) * 2.0 - 1.0;
	float4 gy = abs(gx) - 0.5;
	float4 tx = floor(gx + 0.5);
	gx = gx - tx;

	float2 g00 = float2(gx.x, gy.x);
	float2 g10 = float2(gx.y, gy.y);
	float2 g01 = float2(gx.z, gy.z);
	float2 g11 = float2(gx.w, gy.w);

	float4 norm = taylorInvSqrt(float4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
	g00 *= norm.x;
	g01 *= norm.y;
	g10 *= norm.z;
	g11 *= norm.w;

	float n00 = dot(g00, float2(fx.x, fy.x));
	float n10 = dot(g10, float2(fx.y, fy.y));
	float n01 = dot(g01, float2(fx.z, fy.z));
	float n11 = dot(g11, float2(fx.w, fy.w));

	float2 fade_xy = fade(Pf.xy);
	float2 n_x = lerp(float2(n00, n01), float2(n10, n11), fade_xy.x);
	float n_xy = lerp(n_x.x, n_x.y, fade_xy.y);
	return 2.3 * n_xy;
}

float CalculatePerlinTileing5(float2 p, float2 rep)
{

	float2 xy = p;
	float amplitude_factor = 0.5;
	float frequency_factor = 1.0;

	float a = 1.0;
	float perlin_value = 0.0;
	perlin_value += a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 1);
	perlin_value -= a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 1);
	perlin_value -= a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 1);
	perlin_value -= a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 1);
	perlin_value += a * penoise(xy, rep).r;

	return perlin_value;
}

float CalculatePerlinTileing5OLD(float2 p, float2 rep)
{

	float2 xy = p;
	float amplitude_factor = 0.5;
	float frequency_factor = 2.0;

	float a = 1.0;
	float perlin_value = 0.0;
	perlin_value += a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 0.12);
	perlin_value -= a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 0.03);
	perlin_value -= a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 0.01);
	perlin_value -= a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 0.01);
	perlin_value += a * penoise(xy, rep).r;

	return perlin_value;
}

float CalculatePerlinTileing(float2 p, float2 rep)
{

	float2 xy = p;
	float amplitude_factor = 0.5;
	float frequency_factor = 1.0;

	float a = 1.0;
	float perlin_value = 0.0;
	perlin_value += a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 1);
	perlin_value += a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 1);
	perlin_value += a * penoise(xy, rep).r; a *= amplitude_factor; xy *= (frequency_factor + 2);
	perlin_value -= a * penoise(xy, rep).r;

	return perlin_value;
}

///////////WORLEY
float set_range(float value, float low, float high) 
{
    return saturate((value - low)/(high - low));
}    

float dilate_perlin_worley(float p, float w, float x) 
{
    float curve = 0.75;

    if (x < 0.5) 
    {
        x = x / 0.5;
        float n = p + w * x;
        return n * lerp(1, 0.5, pow(x, curve));
    }
    else 
    {  
        x = (x - 0.5) / 0.5;
        float n = w + p * (1.0 - x);
        return n * lerp(0.5, 1.0, pow(x, max(1.0 / curve,0.0001)));
    }        
}  


#define UI0 1597334673
#define UI1 3812015801
#define UI2 uint2(UI0, UI1)
#define UI3 uint3(UI0, UI1, 2798796415)
#define UIF (1.0 / float(0xffffffffU))

float3 hash33(float3 p)
{
	uint3 q = uint3(int3(p)) * UI3;
	q = (q.x ^ q.y ^ q.z)*UI3;
	return -1. + 2. * float3(q) * UIF;
}

float2 hash22(float2 p)
{
	uint2 q = uint2(int2(p)) * UI3.xy;
	q = (q.x ^ q.y)*UI3.xy;
	return -1. + 2. * float2(q) * UIF;
}

// Gradient noise by iq (modified to be tileable)
float gradientNoise(float3 x, float freq)
{
    // grid
    float3 p = floor(x);
    float3 w = frac(x);
    
    // quintic interpolant
    float3 u = w * w * w * (w * (w * 6. - 15.) + 10.);

    
    // gradients
    float3 ga = hash33(fmod(p + float3(0., 0., 0.), freq));
    float3 gb = hash33(fmod(p + float3(1., 0., 0.), freq));
    float3 gc = hash33(fmod(p + float3(0., 1., 0.), freq));
    float3 gd = hash33(fmod(p + float3(1., 1., 0.), freq));
    float3 ge = hash33(fmod(p + float3(0., 0., 1.), freq));
    float3 gf = hash33(fmod(p + float3(1., 0., 1.), freq));
    float3 gg = hash33(fmod(p + float3(0., 1., 1.), freq));
    float3 gh = hash33(fmod(p + float3(1., 1., 1.), freq));
    
    // projections
    float va = dot(ga, w - float3(0., 0., 0.));
    float vb = dot(gb, w - float3(1., 0., 0.));
    float vc = dot(gc, w - float3(0., 1., 0.));
    float vd = dot(gd, w - float3(1., 1., 0.));
    float ve = dot(ge, w - float3(0., 0., 1.));
    float vf = dot(gf, w - float3(1., 0., 1.));
    float vg = dot(gg, w - float3(0., 1., 1.));
    float vh = dot(gh, w - float3(1., 1., 1.));
	
    // interpolation
    return va + 
           u.x * (vb - va) + 
           u.y * (vc - va) + 
           u.z * (ve - va) + 
           u.x * u.y * (va - vb - vc + vd) + 
           u.y * u.z * (va - vc - ve + vg) + 
           u.z * u.x * (va - vb - ve + vf) + 
           u.x * u.y * u.z * (-va + vb + vc - vd + ve - vf - vg + vh);
}

// Tileable 3D worley noise
float worleyNoise(float3 uv, float freq)
{    
    float3 id = floor(uv);
    float3 p = frac(uv);
    
    float minDist = 10000.;
    for (float x = -1.; x <= 1.; ++x)
    {
        for(float y = -1.; y <= 1.; ++y)
        {
            for(float z = -1.; z <= 1.; ++z)
            {
                float3 offset = float3(x, y, z);
            	float3 h = hash33(fmod(id + offset, float3(freq,freq,freq))) * .5 + .5;
    			h += offset;
            	float3 d = p - h;
           		minDist = min(minDist, dot(d, d));
            }
        }
    }
    
    // inverted worley noise
    return 1. - minDist;
}

float worleyFbm(float3 p, float freq)
{
    return worleyNoise(p*freq, freq) * .625 +
        	 worleyNoise(p*freq*2., freq*2.) * .25 +
        	 worleyNoise(p*freq*4., freq*4.) * .125;
}

float worleyNoise2D(float2 uv, float freq)
{    
    float2 id = floor(uv);
    float2 p = frac(uv);
    
    float minDist = 10000.;
    for (float x = -1.; x <= 1.; ++x)
    {
        for(float y = -1.; y <= 1.; ++y)
        {
            float2 offset = float2(x, y);
            float2 h = hash22(fmod(id + offset, float2(freq,freq))) * .5 + .5;
            h += offset;
            float2 d = p - h;
            minDist = min(minDist, dot(d, d));
        }
    }
    
    // inverted worley noise
    return 1. - minDist;
}



float worleyFbm2DFiller(float2 p, float freq)
{
    return worleyNoise2D(p*freq, freq) * .625 + worleyNoise2D(p*freq*2., freq*2.) * .225 + worleyNoise2D(p*freq*4., freq*4.) * .125;
}

float worleyFbm2D(float2 p, float freq)
{
    return worleyNoise2D(p*freq, freq) * .625 - worleyNoise2D(p*freq*2., freq*2.) * .225 + worleyNoise2D(p*freq*4., freq*4.) * .125;
}

float worley2(float2 p, float freq)
{
    float fbm = worleyFbm2D(p*freq * 4, 4) * 1.2;
    float worl = worleyNoise2D(p*freq*2, 1) * .425 - worleyNoise2D(p*freq*2, 1) * .225;

    return saturate(fbm - worl); 
} 