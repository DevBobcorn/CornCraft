void UnpackFloats_0F_15F_float(float Packed, out float ValA, out float ValB)
{
    int packedInt = (int) Packed;

    int aInt = packedInt & 0xFFF; // Lower 12 bits
    int bInt = packedInt >> 12;   // Higher 12 bits

    // Map values from [1, 4095] to [0F, 15F]: 15F * (val / 4095F)
    ValA = aInt / 273.0;
    ValB = bInt / 273.0;
}