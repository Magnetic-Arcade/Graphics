//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERVARIABLESATMOSPHERESKYCOMPUTE_CS_HLSL
#define SHADERVARIABLESATMOSPHERESKYCOMPUTE_CS_HLSL

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesAtmosphereSkyCompute
// PackingRules = Exact
CBUFFER_START(ShaderVariablesAtmosphereSkyCompute)
    float _SampleCountMin;
    float _SampleCountMax;
    float _DistanceToSampleCountMaxInv;
    float _SVAIPad0;
    float _FastSkySampleCountMin;
    float _FastSkySampleCountMax;
    float _FastSkyDistanceToSampleCountMaxInv;
    float _SVAIPad1;
    float4 _CameraAerialPerspectiveVolumeSizeAndInvSize;
    float _CameraAerialPerspectiveSampleCountPerSlice;
    float3 _SVAIPad3;
    float4 _TransmittanceLutSizeAndInvSize;
    float4 _MultiScatteredLuminanceLutSizeAndInvSize;
    float _TransmittanceSampleCount;
    float _MultiScatteringSampleCount;
    float _AerialPespectiveViewDistanceScale;
    float _SVAIPad4;
    float3 _SkyLuminanceFactor;
    float _SVAIPad5;
    int _StateFrameIndexMod8;
    float3 _SVAIPad6;
CBUFFER_END

#endif
