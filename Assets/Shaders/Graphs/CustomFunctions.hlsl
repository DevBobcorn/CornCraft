#ifndef CORN_FUNCTIONS_INCLUDED
#define CORN_FUNCTIONS_INCLUDED

void GetTexUVOffset_float(float AnimTime, float4 AnimInfo, out float2 TexUVOffset)
{
    uint frameCount = round(AnimInfo.x);

    if (frameCount > 1) {
        float frameInterval = AnimInfo.y;

        float cycleTime = fmod(AnimTime, frameInterval * frameCount);
        uint curFrame = floor(cycleTime / frameInterval);
        uint framePerRow = round(AnimInfo.w);
        
        TexUVOffset = float2((curFrame % framePerRow) * AnimInfo.z, (curFrame / framePerRow) * -AnimInfo.z);
    } else {
        TexUVOffset = float2(0, 0);
    }
}

#endif