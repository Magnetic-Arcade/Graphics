#ifndef MA_ATMOSPHERE_COMMON_INCLUDED
#define MA_ATMOSPHERE_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/AtmosphereSky/ShaderVariablesAtmosphereSky.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/AtmosphereSky/AtmosphereSkyUtils.hlsl"

// The constants below should match the one in SceneRendering.cpp
// Kilometers as unit for computations related to the sky and its atmosphere
#define M_TO_SKY_UNIT 0.001f
#define SKY_UNIT_TO_M (1.0f / M_TO_SKY_UNIT)

// Float accuracy offset in Sky unit (km, so this is 1m)
#define PLANET_RADIUS_OFFSET 0.001f

// The number of killometer per slice in the aerial pespective camera volume texture. (assuming a uniform depth distribution)
#define AP_KM_PER_SLICE 4.0f
#define AP_KM_PER_SLICE_INV (1.0f / AP_KM_PER_SLICE)

TEXTURE2D(_TransmittanceLutTexture); SAMPLER(sampler_TransmittanceLutTexture);
TEXTURE2D(_DistantSkyLightLutTexture); SAMPLER(sampler_DistantSkyLightLutTexture);
TEXTURE2D(_SkyViewLutTexture);  SAMPLER(sampler_SkyViewLutTexture);
TEXTURE3D(_CameraAerialPerspectiveVolume);  SAMPLER(sampler_CameraAerialPerspectiveVolume);

float4 GetAerialPerspectiveLuminanceTransmittance(float4 NDC, float3 SampledWorldPos, float3 CameraWorldPos)
{
#if UNITY_UV_STARTS_AT_TOP
    // Our world space, view space, screen space and NDC space are Y-up.
    // Our clip space is flipped upside-down due to poor legacy Unity design.
    // The flip is baked into the projection matrix, so we only have to flip
    // manually when going from CS to NDC and back.
    //NDC.y = -NDC.y;
#endif
	//float2 ScreenUv = NDC.xy * rcp(NDC.w) * 0.5 + 0.5;
	float2 ScreenUv = NDC.xy;

    float3 SampledToCameraVec = (SampledWorldPos - CameraWorldPos);
    float SampledToCameraLen = length(SampledToCameraVec);
	float tDepth = max(0.0f, SampledToCameraLen - _AtmosphereAerialPerspectiveStartDepthKm);

	float LinearSlice = tDepth * _AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKmInv;
	float LinearW = LinearSlice * _AtmosphereCameraAerialPerspectiveVolumeDepthResolutionInv; // Depth slice coordinate in [0,1]
	float NonLinW = sqrt(LinearW); // Squared distribution
	float NonLinSlice = NonLinW * _AtmosphereCameraAerialPerspectiveVolumeDepthResolution; 

	const float HalfSliceDepth = 0.70710678118654752440084436210485f; // sqrt(0.5f)
	float Weight = 1.0f;
	if (NonLinSlice < HalfSliceDepth)
	{
		// We multiply by weight to fade to 0 at depth 0. It works for luminance and opacity.
		Weight = saturate(NonLinSlice*NonLinSlice * 2.0f); // Square to have a linear falloff from the change of distribution above
	}

	float4 AP = SAMPLE_TEXTURE3D_LOD(_CameraAerialPerspectiveVolume, sampler_CameraAerialPerspectiveVolume, float3(ScreenUv, NonLinW), 0.0f);

	// Lerp to no contribution near the camera (careful as AP contains transmittance)
	AP.rgb *= Weight;
	AP.a = 1.0 - (Weight * (1.0f - AP.a));

	// Debug Slices
#if 0
	AP.rgba *= frac(clamp(NonLinSlice, 0, _AtmosphereCameraAerialPerspectiveVolumeDepthResolution));
	AP.r += LinearW <= 0.0f ? 0.5f : 0.0f;
	AP.g += LinearW >= 1.0f ? 0.5f : 0.0f;
	AP.b += Weight  <  1.0f ? 0.2f+0.2f*Weight : 0.0f;
#endif

	AP.rgb *= GetCurrentExposureMultiplier();

	return AP;
}

float4 GetAerialPerspectiveLuminanceTransmittanceWithFogOver(float4 NDC, float3 SampledWorldPos, float3 CameraWorldPos, float4 FogToApplyOver)
{
	float4 AP = GetAerialPerspectiveLuminanceTransmittance(NDC, SampledWorldPos, CameraWorldPos);
	float4 FinalFog;
	// Apply any other fog OVER aerial perspective because AP is usually optically thiner.
	FinalFog.rgb = FogToApplyOver.rgb + AP.rgb * FogToApplyOver.a;
	// And combine both transmittance.
	FinalFog.a   = FogToApplyOver.a * AP.a;

	return FinalFog;
}


float2 FromUnitToSubUvs(float2 uv, float4 SizeAndInvSize) { return (uv + 0.5f * SizeAndInvSize.zw) * (SizeAndInvSize.xy / (SizeAndInvSize.xy + 1.0f)); }
float2 FromSubUvsToUnit(float2 uv, float4 SizeAndInvSize) { return (uv - 0.5f * SizeAndInvSize.zw) * (SizeAndInvSize.xy / (SizeAndInvSize.xy - 1.0f)); }

void getTransmittanceLutUvs(
	in float viewHeight, in float viewZenithCosAngle, in float BottomRadius, in float TopRadius,
	out float2 UV)
{
	float H = sqrt(max(0.0f, TopRadius * TopRadius - BottomRadius * BottomRadius));
	float Rho = sqrt(max(0.0f, viewHeight * viewHeight - BottomRadius * BottomRadius));

	float Discriminant = viewHeight * viewHeight * (viewZenithCosAngle * viewZenithCosAngle - 1.0f) + TopRadius * TopRadius;
	float D = max(0.0f, (-viewHeight * viewZenithCosAngle + sqrt(Discriminant))); // Distance to atmosphere boundary

	float Dmin = TopRadius - viewHeight;
	float Dmax = Rho + H;
	float Xmu = (D - Dmin) / (Dmax - Dmin);
	float Xr = Rho / H;

	UV = float2(Xmu, Xr);
	//UV = float2(fromUnitToSubUvs(UV.x, TRANSMITTANCE_TEXTURE_WIDTH), fromUnitToSubUvs(UV.y, TRANSMITTANCE_TEXTURE_HEIGHT)); // No real impact so off
}

void SkyViewLutParamsToUv(
	in bool IntersectGround, in float ViewZenithCosAngle, in float3 ViewDir, in float ViewHeight, in float BottomRadius, in float4 SkyViewLutSizeAndInvSize,
	out float2 UV)
{
	float Vhorizon = sqrt(ViewHeight * ViewHeight - BottomRadius * BottomRadius);
	float CosBeta = Vhorizon / ViewHeight;				// GroundToHorizonCos
	float Beta = FastACos(CosBeta);
	float ZenithHorizonAngle = PI - Beta;
	float ViewZenithAngle = FastACos(ViewZenithCosAngle);

	if (!IntersectGround)
	{
		float Coord = ViewZenithAngle / ZenithHorizonAngle;
		Coord = 1.0f - Coord;
		Coord = sqrt(Coord);
		Coord = 1.0f - Coord;
		UV.y = Coord * 0.5f;
	}
	else
	{
		float Coord = (ViewZenithAngle - ZenithHorizonAngle) / Beta;
		Coord = sqrt(Coord);
		UV.y = Coord * 0.5f + 0.5f;
	}

	{
		//NOTE: UV.x = (atan2Fast(-ViewDir.y, -ViewDir.x) + PI) / (2.0f * PI);
		UV.x = (FastAtan2(-ViewDir.z, -ViewDir.x) + PI) / (2.0f * PI);
		//UV.y = 1.0 - UV.y;
	}

	// Constrain uvs to valid sub texel range (avoid zenith derivative issue making LUT usage visible)
	UV = FromUnitToSubUvs(UV, SkyViewLutSizeAndInvSize);
}

float3x3 GetSkyViewLutReferential(in float3 WorldPos, in float3 ViewForward, in float3 ViewRight)
{
#if defined(USING_STEREO_MATRICES)
	return (float3x3) _SkyViewLutReferential[unity_StereoEyeIndex];
#else
	return (float3x3) _SkyViewLutReferential[0];
#endif
	// return (float3x3)_SkyViewLutReferential;
	// float3	Up = normalize(WorldPos);
	// float3	Forward = ViewForward;		// This can make texel visible when the camera is rotating. Use constant worl direction instead?
	// float3	Left = normalize(cross(Forward, Up));
	// if (abs(dot(Forward, Up)) > 0.99f)
	// {
	// 	Left = -ViewRight;
	// }
	// Forward = normalize(cross(Up, Left));
	// //float3x3 LocalReferencial = transpose(float3x3(Forward, Left, Up));
	// float3x3 LocalReferencial = transpose(float3x3(Forward, Up, Left));
	// return LocalReferencial;
}

float3 GetAtmosphereTransmittance(float3 WorldPos, float3 WorldDir)
{
	// For each view height entry, transmittance is only stored from zenith to horizon. Earth shadow is not accounted for.
	// It does not contain earth shadow in order to avoid texel linear interpolation artefact when LUT is low resolution.
	// As such, at the most shadowed point of the LUT when close to horizon, pure black with earth shadow is never hit.
	// That is why we analytically compute the virtual planet shadow here.
	const float2 Sol = RayIntersectSphere(WorldPos, WorldDir, float4(float3(0.0f, 0.0f, 0.0f), _BottomRadiusKm));
	if (Sol.x > 0.0f || Sol.y > 0.0f)
	{
		return 0.0f;
	}

	const float PHeight = length(WorldPos);
	const float3 UpVector = WorldPos / PHeight;
	const float LightZenithCosAngle = dot(WorldDir, UpVector);
	float2 TransmittanceLutUv;
	getTransmittanceLutUvs(PHeight, LightZenithCosAngle, _BottomRadiusKm, _TopRadiusKm, TransmittanceLutUv);
	const float3 TransmittanceToLight = SAMPLE_TEXTURE2D_LOD(_TransmittanceLutTexture, sampler_TransmittanceLutTexture, TransmittanceLutUv, 0.0f).rgb;
	return TransmittanceToLight;
}

float3 GetLightDiskLuminance(float3 WorldPos, float3 WorldDir, float3 AtmosphereLightDirection, float AtmosphereLightDiscCosHalfApexAngle, float3 AtmosphereLightDiscLuminance)
{
	const float ViewDotLight = dot(WorldDir, AtmosphereLightDirection);
	const float CosHalfApex = AtmosphereLightDiscCosHalfApexAngle;
	
	if (ViewDotLight > CosHalfApex)
	{
		float3 TransmittanceToLight = GetAtmosphereTransmittance(WorldPos, WorldDir);
		return TransmittanceToLight * AtmosphereLightDiscLuminance;
	}
	
	return 0.0f;
}

#endif // D_ATMOSPHERE_COMMON
