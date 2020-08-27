#ifndef UNITY_PERCEPTION_PACKING_INCLUDED
#define UNITY_PERCEPTION_PACKING_INCLUDED

#define real float
#define real2 float2
#define real3 float3
#define real4 float4

// Packs an integer stored using at most 'numBits' into a [0..1] real.
real PackInt(uint i, uint numBits)
{
    uint maxInt = (1u << numBits) - 1u;
    return saturate(i * rcp(maxInt));
}

// Unpacks a [0..1] real into an integer of size 'numBits'.
uint UnpackInt(real f, uint numBits)
{
    uint maxInt = (1u << numBits) - 1u;
    return (uint)(f * maxInt + 0.5); // Round instead of truncating
}

#ifndef INTRINSIC_BITFIELD_EXTRACT
// Unsigned integer bit field extraction.
// Note that the intrinsic itself generates a vector instruction.
// Wrap this function with WaveReadLaneFirst() to get scalar output.
uint BitFieldExtract(uint data, uint offset, uint numBits)
{
    uint mask = (1u << numBits) - 1u;
    return (data >> offset) & mask;
}
#endif // INTRINSIC_BITFIELD_EXTRACT

//-----------------------------------------------------------------------------
// Float packing
//-----------------------------------------------------------------------------

// src must be between 0.0 and 1.0
uint PackFloatToUInt(real src, uint offset, uint numBits)
{
    return UnpackInt(src, numBits) << offset;
}

real UnpackUIntToFloat(uint src, uint offset, uint numBits)
{
    uint maxInt = (1u << numBits) - 1u;
    return real(BitFieldExtract(src, offset, numBits)) * rcp(maxInt);
}

#endif // UNITY_PACKING_INCLUDED
