void UnpackFloats_0F_15F_float(float Packed, out float ValA, out float ValB)
{
    int packedInt = (int) Packed;

    int aInt = packedInt & 0xFFF; // Lower 12 bits
    int bInt = packedInt >> 12;   // Higher 12 bits

    // Map values
    ValA = aInt * 15.0 / 4095.0;
    ValB = bInt / 4095.0;
}