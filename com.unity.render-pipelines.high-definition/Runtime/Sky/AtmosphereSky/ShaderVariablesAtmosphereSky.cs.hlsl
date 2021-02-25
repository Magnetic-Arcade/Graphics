//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERVARIABLESATMOSPHERESKY_CS_HLSL
#define SHADERVARIABLESATMOSPHERESKY_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.AtmosphereConfig:  static fields
//
#define ATMOSPHERECONFIG_MAX_ATMOSPHERE_LIGHTS (2)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesAtmosphereSky
// PackingRules = Exact
CBUFFER_START(ShaderVariablesAtmosphereSky)
    float4 _AtmosphereLightDirection[2];
    float4 _AtmosphereLightColor[2];
    float4 _AtmosphereLightColorGlobalPostTransmittance[2];
    float4 _AtmosphereLightDiscLuminance[2];
    float4 _AtmosphereLightDiscCosHalfApexAngle[2];
    float4x4 _SkyViewLutReferential[2];
    float4 _SkyViewLutSizeAndInvSize;
    float3 _SkyWorldCameraOrigin;
    float _ASPUnused0;
    float4 _SkyPlanetCenterAndViewHeight;
    float4 _AtmosphereSkyLuminanceFactor;
    float _AtmosphereHeightFogContribution;
    float _AtmosphereBottomRadiusKm;
    float _AtmosphereTopRadiusKm;
    float _AtmosphereAerialPerspectiveStartDepthKm;
    float _AtmosphereCameraAerialPerspectiveVolumeDepthResolution;
    float _AtmosphereCameraAerialPerspectiveVolumeDepthResolutionInv;
    float _AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKm;
    float _AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKmInv;
    float _AtmosphereApplyCameraAerialPerspectiveVolume;
    float3 _ASPUnused2;
    float _MultiScatteringFactor;
    float _BottomRadiusKm;
    float _TopRadiusKm;
    float _ASPUnused3;
    float3 _RayleighScattering;
    float _RayleighDensityExpScale;
    float3 _MieScattering;
    float _MieDensityExpScale;
    float3 _MieExtinction;
    float _MiePhaseG;
    float3 _MieAbsorption;
    float _AbsorptionDensity0LayerWidth;
    float _AbsorptionDensity0ConstantTerm;
    float _AbsorptionDensity0LinearTerm;
    float _AbsorptionDensity1ConstantTerm;
    float _AbsorptionDensity1LinearTerm;
    float3 _AbsorptionExtinction;
    float _ASPUnused4;
    float3 _GroundAlbedo1;
    float _ASPUnused5;
CBUFFER_END


#endif
