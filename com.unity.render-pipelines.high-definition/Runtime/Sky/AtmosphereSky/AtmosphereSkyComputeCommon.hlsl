#ifndef MA_SKY_ATMOSPHERE_COMMON_INCLUDED
#define MA_SKY_ATMOSPHERE_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/AtmosphereSky/AtmosphereSkyCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/AtmosphereSky/ShaderVariablesAtmosphereSkyCompute.cs.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#ifndef COLORED_TRANSMITTANCE_ENABLED	// Never used, UE4 does not supports dual blending
#define COLORED_TRANSMITTANCE_ENABLED 0
#endif

#ifndef MULTISCATTERING_APPROX_ENABLED
#define MULTISCATTERING_APPROX_ENABLED 0
#endif

#ifndef HIGHQUALITY_MULTISCATTERING_APPROX_ENABLED
#define HIGHQUALITY_MULTISCATTERING_APPROX_ENABLED 0
#endif

#ifndef FASTSKY_ENABLED
#define FASTSKY_ENABLED 0
#endif

#ifndef FASTAERIALPERSPECTIVE_ENABLED
#define FASTAERIALPERSPECTIVE_ENABLED 0
#endif

#ifndef SOURCE_DISK_ENABLED
#define SOURCE_DISK_ENABLED 0
#endif

#ifndef PER_PIXEL_NOISE
#define PER_PIXEL_NOISE 0
#endif

#ifndef SECOND_ATMOSPHERE_LIGHT_ENABLED
#define SECOND_ATMOSPHERE_LIGHT_ENABLED 0
#endif

#ifndef RENDERSKY_ENABLED
#define RENDERSKY_ENABLED 0
#endif

#ifndef TRANSMITTANCE_PASS
#define TRANSMITTANCE_PASS 0
#endif

#ifndef MULTISCATT_PASS
#define MULTISCATT_PASS 0
#endif

#ifndef SKYLIGHT_PASS
#define SKYLIGHT_PASS 0
#endif

// View data is not available for passes running once per scene (and not once per view).
#define VIEWDATA_AVAILABLE (TRANSMITTANCE_PASS!=1 && MULTISCATT_PASS!=1 && SKYLIGHT_PASS!=1)

 // FASTSKY mapping is done based on Light0 as main light
#define FASTSKY_LIGHT_INDEX 0

// Only available for shaders ran per view, so this constant should not be accessed in common code such as IntegrateSingleScatteredLuminance for instance.
//float AerialPerspectiveStartDepthKm;

#define ViewPreExposure 1.0f
#define ViewOneOverPreExposure 1.0f

TEXTURE2D(_MultiScatteredLuminanceLutTexture);
SAMPLER(sampler_MultiScatteredLuminanceLutTexture);

// Returns the (right) direction of the current view in the world space.
float3 GetViewRightDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return viewMat[0].xyz;
}

// - RayOrigin: ray origin
// - RayDir: normalized ray direction
// - SphereCenter: sphere center
// - SphereRadius: sphere radius
// - Returns distance from RayOrigin to closest intersecion with sphere,
//   or -1.0 if no intersection.
float RaySphereIntersectNearest(float3 RayOrigin, float3 RayDir, float3 SphereCenter, float SphereRadius)
{
	float2 Sol = RayIntersectSphere(RayOrigin, RayDir, float4(SphereCenter, SphereRadius));
	float Sol0 = Sol.x;
	float Sol1 = Sol.y;

	if (Sol0 < 0.0f && Sol1 < 0.0f)
	{
		return -1.0f;
	}

	if (Sol0 < 0.0f)
	{
		return max(0.0f, Sol1);
	}
	else if (Sol1 < 0.0f)
	{
		return max(0.0f, Sol0);
	}

	return max(0.0f, min(Sol0, Sol1));
}

// Used for post process shaders which don't need to resolve the view
float3 SvPositionToTranslatedWorld(float4 SvPosition)
{
	//	new_xy = (xy - ViewRectMin.xy) * ViewSizeAndInvSize.zw * float2(2,-2) + float2(-1, 1);
	float2 PixelPos = (SvPosition.xy /* - _ViewRectMin.xy */) * _ScreenSize.zw * float2(2, -2) + float2(-1, 1);
	float4 HomWorldPos = mul(float4(PixelPos, SvPosition.z, 1), UNITY_MATRIX_I_VP);

	return HomWorldPos.xyz / HomWorldPos.w;
}

// investigate: doesn't work for usage with View.ScreenToWorld, see SvPositionToScreenPosition2()
float4 SvPositionToScreenPosition(float4 SvPosition)
{
	// todo: is already in .w or needs to be reconstructed like this:
//	SvPosition.w = ConvertFromDeviceZ(SvPosition.z);

	float2 PixelPos = SvPosition.xy - 0;//TODO: _ViewRectMin.xy;

	// NDC (NormalizedDeviceCoordinates, after the perspective divide)
	float3 NDCPos = float3( (PixelPos * _ScreenSize.zw - 0.5f) * float2(2, -2), SvPosition.z);

	// SvPosition.w: so .w has the SceneDepth, some mobile code and the DepthFade material expression wants that
	return float4(NDCPos.xyz, 1) * SvPosition.w;
}

float3 GetScreenWorldPos(float4 SVPos, float DeviceZ)
{
#if UNITY_REVERSED_Z
	DeviceZ = max(0.000000000001, DeviceZ);	// TODO: investigate why SvPositionToWorld returns bad values when DeviceZ is far=0 when using inverted z
#endif
    float2 positionNDC = SVPos.xy * _ScreenSize.zw + (0.5 * _ScreenSize.zw);
	return ComputeWorldSpacePosition(positionNDC, DeviceZ, UNITY_MATRIX_I_VP);
}

float3 GetScreenWorldDir(in float4 SVPos)
{
    float2 positionNDC = SVPos.xy * _ScreenSize.zw;
    float3 positionWS = ComputeWorldSpacePosition(positionNDC, UNITY_RAW_FAR_CLIP_VALUE, UNITY_MATRIX_I_VP); // Jittered
	return normalize(positionWS - _WorldSpaceCameraPos);
}

// This is the world position of the camera. It is also force to be at the top of the virutal planet surface.
// This is to always see the sky even when the camera is buried into the virtual planet.
float3 GetCameraWorldPos()
{
	return _SkyWorldCameraOrigin;
}

// This is the camera position relative to the virtual planet center.
// This is convenient because for all the math in this file using world position relative to the virtual planet center.
float3 GetCameraPlanetPos()
{
	return (GetCameraWorldPos() - _SkyPlanetCenterAndViewHeight.xyz) * M_TO_SKY_UNIT;
}

bool MoveToTopAtmosphere(inout float3 WorldPos, in float3 WorldDir, in float AtmosphereTopRadius)
{
	float ViewHeight = length(WorldPos);

	if (ViewHeight > AtmosphereTopRadius)
	{
		float TTop = RaySphereIntersectNearest(WorldPos, WorldDir, float3(0.0f, 0.0f, 0.0f), AtmosphereTopRadius);
		if (TTop >= 0.0f)
		{
			float3 UpVector = WorldPos / ViewHeight;
			float3 UpOffset = UpVector * -PLANET_RADIUS_OFFSET;
			WorldPos = WorldPos + WorldDir * TTop + UpOffset;
		}
		else
		{
			// Ray is not intersecting the atmosphere
			return false;
		}
	}

	return true; // ok to start tracing
}

////////////////////////////////////////////////////////////
// Participating medium properties
////////////////////////////////////////////////////////////

float RayleighPhase(float CosTheta)
{
	float Factor = 3.0f / (16.0f * PI);
	return Factor * (1.0f + CosTheta * CosTheta);
}

float HgPhase(float G, float CosTheta)
{
	// Reference implementation (i.e. not schlick approximation).
	// See http://www.pbr-book.org/3ed-2018/Volume_Scattering/Phase_Functions.html
	float Numer = 1.0f - G * G;
	float Denom = 1.0f + G * G + 2.0f * G * CosTheta;
	return Numer / (4.0f * PI * Denom * sqrt(Denom));
}

float3 GetAlbedo(float3 Scattering, float3 Extinction)
{
	return Scattering / max(0.001f, Extinction);
}

struct MediumSampleRGB
{
	float3 Scattering;
	float3 Absorption;
	float3 Extinction;

	float3 ScatteringMie;
	float3 AbsorptionMie;
	float3 ExtinctionMie;

	float3 ScatteringRay;
	float3 AbsorptionRay;
	float3 ExtinctionRay;

	float3 ScatteringOzo;
	float3 AbsorptionOzo;
	float3 ExtinctionOzo;

	float3 Albedo;
};

// If this is changed, please also update UAtmosphereComponent::GetTransmittance
MediumSampleRGB SampleMediumRGB(in float3 WorldPos)
{
	const float SampleHeight = max(0.0, (length(WorldPos) - _BottomRadiusKm));

	const float DensityMie = exp(_MieDensityExpScale * SampleHeight);

	const float DensityRay = exp(_RayleighDensityExpScale * SampleHeight);

	const float DensityOzo = SampleHeight < _AbsorptionDensity0LayerWidth ?
		saturate(_AbsorptionDensity0LinearTerm * SampleHeight + _AbsorptionDensity0ConstantTerm) :	// We use saturate to allow the user to create plateau, and it is free on GCN.
		saturate(_AbsorptionDensity1LinearTerm * SampleHeight + _AbsorptionDensity1ConstantTerm);

	MediumSampleRGB s;

	s.ScatteringMie = DensityMie * _MieScattering.rgb;
	s.AbsorptionMie = DensityMie * _MieAbsorption.rgb;
	s.ExtinctionMie = DensityMie * _MieExtinction.rgb;

	s.ScatteringRay = DensityRay * _RayleighScattering.rgb;
	s.AbsorptionRay = 0.0f;
	s.ExtinctionRay = s.ScatteringRay + s.AbsorptionRay;

	s.ScatteringOzo = 0.0f;
	s.AbsorptionOzo = DensityOzo * _AbsorptionExtinction.rgb;
	s.ExtinctionOzo = s.ScatteringOzo + s.AbsorptionOzo;

	s.Scattering = s.ScatteringMie + s.ScatteringRay + s.ScatteringOzo;
	s.Absorption = s.AbsorptionMie + s.AbsorptionRay + s.AbsorptionOzo;
	s.Extinction = s.ExtinctionMie + s.ExtinctionRay + s.ExtinctionOzo;
	s.Albedo = GetAlbedo(s.Scattering, s.Extinction);

	return s;
}

////////////////////////////////////////////////////////////
// LUT functions
////////////////////////////////////////////////////////////

// Transmittance LUT function parameterisation from Bruneton 2017 https://github.com/ebruneton/precomputed_atmospheric_scattering
// uv in [0,1]
// ViewZenithCosAngle in [-1,1]
// ViewHeight in [bottomRAdius, topRadius]

void UvToLutTransmittanceParams(out float ViewHeight, out float ViewZenithCosAngle, in float2 UV)
{
	//UV = FromSubUvsToUnit(UV, _SkyTransmittanceLutSizeAndInvSize); // No real impact so off
	float Xmu = UV.x;
	float Xr = UV.y;

	float H = sqrt(_TopRadiusKm * _TopRadiusKm - _BottomRadiusKm * _BottomRadiusKm);
	float Rho = H * Xr;
	ViewHeight = sqrt(Rho * Rho + _BottomRadiusKm * _BottomRadiusKm);

	float Dmin = _TopRadiusKm - ViewHeight;
	float Dmax = Rho + H;
	float D = Dmin + Xmu * (Dmax - Dmin);
	ViewZenithCosAngle = D == 0.0f ? 1.0f : (H * H - Rho * Rho - D * D) / (2.0f * ViewHeight * D);
	ViewZenithCosAngle = clamp(ViewZenithCosAngle, -1.0f, 1.0f);
}

void LutTransmittanceParamsToUv(in float ViewHeight, in float ViewZenithCosAngle, out float2 UV)
{
	getTransmittanceLutUvs(ViewHeight, ViewZenithCosAngle, _BottomRadiusKm, _TopRadiusKm, UV);
}

// SkyViewLut is a new texture used for fast sky rendering.
// It is low resolution of the sky rendering around the camera,
// basically a lat/long parameterisation with more texel close to the horizon for more accuracy during sun set.

void UvToSkyViewLutParams(out float3 ViewDir, in float ViewHeight, in float2 UV)
{
	// Constrain uvs to valid sub texel range (avoid zenith derivative issue making LUT usage visible)
	UV = FromSubUvsToUnit(UV, _SkyViewLutSizeAndInvSize);

	float Vhorizon = sqrt(ViewHeight * ViewHeight - _BottomRadiusKm * _BottomRadiusKm);
	float CosBeta = Vhorizon / ViewHeight;				// cos of zenith angle from horizon to zeniht
	float Beta = FastACos(CosBeta);
	float ZenithHorizonAngle = PI - Beta;

	float ViewZenithAngle;
	if (UV.y < 0.5f)
	{
		float Coord = 2.0f * UV.y;
		Coord = 1.0f - Coord;
		Coord *= Coord;
		Coord = 1.0f - Coord;
		ViewZenithAngle = ZenithHorizonAngle * Coord;
	}
	else
	{
		float Coord = UV.y * 2.0f - 1.0f;
		Coord *= Coord;
		ViewZenithAngle = ZenithHorizonAngle + Beta * Coord;
	}

	float CosViewZenithAngle = cos(ViewZenithAngle);
	float SinViewZenithAngle = sqrt(1.0 - CosViewZenithAngle * CosViewZenithAngle) * (ViewZenithAngle > 0.0f ? 1.0f : -1.0f); // Equivalent to sin(ViewZenithAngle)

	float LongitudeViewCosAngle = UV.x * 2.0f * PI;

	// Make sure those values are in range as it could disrupt other math done later such as sqrt(1.0-c*c)
	float CosLongitudeViewCosAngle = cos(LongitudeViewCosAngle);
	float SinLongitudeViewCosAngle = sqrt(1.0 - CosLongitudeViewCosAngle * CosLongitudeViewCosAngle) * (LongitudeViewCosAngle <= PI ? 1.0f : -1.0f); // Equivalent to sin(LongitudeViewCosAngle)

	ViewDir = float3(
		SinViewZenithAngle * CosLongitudeViewCosAngle,
		CosViewZenithAngle,
		SinViewZenithAngle * SinLongitudeViewCosAngle
		);
}

////////////////////////////////////////////////////////////
// Utilities
////////////////////////////////////////////////////////////

float3 GetLightDiskLuminance(float3 WorldPos, float3 WorldDir, uint LightIndex)
{
#if SOURCE_DISK_ENABLED
	float t = RaySphereIntersectNearest(WorldPos, WorldDir, float3(0.0f, 0.0f, 0.0f), _BottomRadiusKm);
	if (t < 0.0f										// No intersection with the planet
		// && _RenderingReflectionCaptureMask==0.0f	// Do not render light disk when in reflection capture in order to avoid double specular. The sun contribution is already computed analyticaly.
        )
	{
		float3 LightDiskLuminance = GetLightDiskLuminance(WorldPos, WorldDir, _AtmosphereLightDirection[LightIndex].xyz, _AtmosphereLightDiscCosHalfApexAngle[LightIndex].x, _AtmosphereLightDiscLuminance[LightIndex].xyz);

		// Clamp to avoid crazy high values (and exposed 64000.0f luminance is already crazy high, solar system sun is 1.6x10^9). Also this removes +inf float and helps TAA.
		const float3 MaxLightLuminance = 64000.0f;
		float3 ExposedLightLuminance = LightDiskLuminance * ViewPreExposure;
		ExposedLightLuminance = min(ExposedLightLuminance, MaxLightLuminance);

#if 1
		const float ViewDotLight = dot(WorldDir, _AtmosphereLightDirection[LightIndex].xyz);
		const float CosHalfApex = _AtmosphereLightDiscCosHalfApexAngle[LightIndex].x;
		const float HalfCosHalfApex = CosHalfApex + (1.0f - CosHalfApex) * 0.25; // Start fading when at 75% distance from light disk center (in cosine space)

		// Apply smooth fading at edge. This is currently an eye balled fade out that works well in many cases.
		const float Weight = 1.0-saturate((HalfCosHalfApex - ViewDotLight) / (HalfCosHalfApex - CosHalfApex));
		ExposedLightLuminance = ExposedLightLuminance * Weight;
#endif

		return ExposedLightLuminance;
	}
#endif
	return 0.0f;
}

float3 GetMultipleScattering(float3 WorlPos, float ViewZenithCosAngle)
{
	float2 UV = saturate(float2(ViewZenithCosAngle*0.5f + 0.5f, (length(WorlPos) - _BottomRadiusKm) / (_TopRadiusKm - _BottomRadiusKm)));
	// We do no apply UV transform to sub range here as it has minimal impact.
	float3 MultiScatteredLuminance = SAMPLE_TEXTURE2D_LOD(_MultiScatteredLuminanceLutTexture, sampler_MultiScatteredLuminanceLutTexture, UV, 0).rgb;
	return MultiScatteredLuminance;
}

float3 GetTransmittance(in float LightZenithCosAngle, in float PHeight)
{
	float2 UV;
	LutTransmittanceParamsToUv(PHeight, LightZenithCosAngle, UV);
#ifdef WHITE_TRANSMITTANCE
	float3 TransmittanceToLight = 1.0f;
#else
	float3 TransmittanceToLight = SAMPLE_TEXTURE2D_LOD(_TransmittanceLutTexture, sampler_TransmittanceLutTexture, UV, 0).rgb;
#endif
	return TransmittanceToLight;
}

#define DEFAULT_SAMPLE_OFFSET 0.3f
float AtmosphereNoise(float2 UV)
{
	//	return DEFAULT_SAMPLE_OFFSET;
	//	return float(Rand3DPCG32(int3(UV.x, UV.y, S)).x) / 4294967296.0f;
#if VIEWDATA_AVAILABLE && PER_PIXEL_NOISE
	return InterleavedGradientNoise(UV.xy, float(_StateFrameIndexMod8));
#else
	return DEFAULT_SAMPLE_OFFSET;
#endif
}

////////////////////////////////////////////////////////////
// Main scattering/transmitance integration function
////////////////////////////////////////////////////////////

struct SingleScatteringResult
{
	float3 L;						// Scattered light (luminance)
	float3 OpticalDepth;			// Optical depth (1/m)
	float3 Transmittance;			// Transmittance in [0,1] (unitless)
	float3 MultiScatAs1;
};

struct SamplingSetup
{
	bool VariableSampleCount;
	float SampleCountIni;			// Used when VariableSampleCount is false
	float MinSampleCount;
	float MaxSampleCount;
	float DistanceToSampleCountMaxInv;
};

SingleScatteringResult IntegrateSingleScatteredLuminance(
	in float4 SVPos, in float3 WorldPos, in float3 WorldDir,
	in bool Ground, in SamplingSetup Sampling, in float DeviceZ, in bool MieRayPhase,
	in float3 Light0Dir, in float3 Light1Dir, in float3 Light0Illuminance, in float3 Light1Illuminance,
	in float AerialPespectiveViewDistanceScale,
	in float tMaxMax = 9000000.0f)
{
	SingleScatteringResult Result;
	Result.L = 0;
	Result.OpticalDepth = 0;
	Result.Transmittance = 1.0f;
	Result.MultiScatAs1 = 0;

	if (dot(WorldPos, WorldPos) <= _BottomRadiusKm*_BottomRadiusKm)
	{
		return Result;	// Camera is inside the planet ground
	}

	float2 PixPos = SVPos.xy;

	// Compute next intersection with atmosphere or ground
	float3 PlanetO = float3(0.0f, 0.0f, 0.0f);
	float tBottom = RaySphereIntersectNearest(WorldPos, WorldDir, PlanetO, _BottomRadiusKm);
	float tTop = RaySphereIntersectNearest(WorldPos, WorldDir, PlanetO, _TopRadiusKm);
	float tMax = 0.0f;
	if (tBottom < 0.0f)
	{
		if (tTop < 0.0f)
		{
			tMax = 0.0f; // No intersection with planet nor its atmosphere: stop right away
			return Result;
		}
		else
		{
			tMax = tTop;
		}
	}
	else
	{
		if (tTop > 0.0f)
		{
			tMax = min(tTop, tBottom);
		}
	}

	float PlanetOnOpaque = 1.0f;	// This is used to hide opaque meshes under the planet ground
#if VIEWDATA_AVAILABLE
	if (DeviceZ != UNITY_RAW_FAR_CLIP_VALUE)
	{
		//const float3 DepthBufferWorldPosKm = GetScreenWorldPos(SVPos, DeviceZ).xyz * M_TO_SKY_UNIT;
		const float3 DepthBufferWorldPosKm = ComputeWorldSpacePosition(SVPos.xy * _ScreenSize.zw, DeviceZ, UNITY_MATRIX_I_VP).xyz * M_TO_SKY_UNIT;
		const float3 TraceStartWorldPosKm = WorldPos + _SkyPlanetCenterAndViewHeight.xyz * M_TO_SKY_UNIT; // apply planet offset to go back to world from planet local referencial.
		const float3 TraceStartToSurfaceWorldKm = DepthBufferWorldPosKm - TraceStartWorldPosKm;
		float tDepth = length(TraceStartToSurfaceWorldKm);
		if (tDepth < tMax)
		{
			tMax = tDepth;
		}
		else
		{
			// Artists did not like that we handle automatic hiding of opaque element behind the planet.
			// Now, pixel under the surface of earht will receive aerial perspective as if they were  on the ground.
			//PlanetOnOpaque = 0.0;
		}

		//if the ray intersects with the atmosphere boundary, make sure we do not apply atmosphere on surfaces are front of it.
		if (dot(WorldDir, TraceStartToSurfaceWorldKm) < 0.0)
		{
			return Result;
		}
	}
#endif
	tMax = min(tMax, tMaxMax);

	// Sample count
	float SampleCount = Sampling.SampleCountIni;
	float SampleCountFloor = Sampling.SampleCountIni;
	float tMaxFloor = tMax;
	if (Sampling.VariableSampleCount)
	{
		SampleCount = lerp(Sampling.MinSampleCount, Sampling.MaxSampleCount, saturate(tMax*Sampling.DistanceToSampleCountMaxInv));
		SampleCountFloor = floor(SampleCount);
		tMaxFloor = tMax * SampleCountFloor / SampleCount;	// rescale tMax to map to the last entire step segment.
	}
	float dt = tMax / SampleCount;

	// Phase functions
	const float uniformPhase = 1.0f / (4.0f * PI);
	const float3 wi = Light0Dir;
	const float3 wo = WorldDir;
	float cosTheta = dot(wi, wo);
	float MiePhaseValueLight0 = HgPhase(_MiePhaseG, -cosTheta);	// negate cosTheta because due to WorldDir being a "in" direction.
	float RayleighPhaseValueLight0 = RayleighPhase(cosTheta);
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
	cosTheta = dot(Light1Dir, wo);
	float MiePhaseValueLight1 = HgPhase(_MiePhaseG, -cosTheta);	// negate cosTheta because due to WorldDir being a "in" direction.
	float RayleighPhaseValueLight1 = RayleighPhase(cosTheta);
#endif

	// Ray march the atmosphere to integrate optical depth
	float3 L = 0.0f;
	float3 Throughput = 1.0f;
	float3 OpticalDepth = 0.0f;
	float t = 0.0f;
	float tPrev = 0.0f;

	float3 ExposedLight0Illuminance = Light0Illuminance * ViewPreExposure;
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
	float3 ExposedLight1Illuminance = Light1Illuminance * ViewPreExposure;
#endif

	float PixelNoise = PER_PIXEL_NOISE ? AtmosphereNoise(PixPos.xy) : DEFAULT_SAMPLE_OFFSET;
	for (float SampleI = 0.0f; SampleI < SampleCount; SampleI += 1.0f)
	{
		// Compute current ray t and sample point P
		if (Sampling.VariableSampleCount)
		{
			// More expenssive but artefact free
			float t0 = (SampleI) / SampleCountFloor;
			float t1 = (SampleI + 1.0f) / SampleCountFloor;;
			// Non linear distribution of samples within the range.
			t0 = t0 * t0;
			t1 = t1 * t1;
			// Make t0 and t1 world space distances.
			t0 = tMaxFloor * t0;
			if (t1 > 1.0f)
			{
				t1 = tMax;
				//t1 = tMaxFloor;	// this reveal depth slices
			}
			else
			{
				t1 = tMaxFloor * t1;
			}
			t = t0 + (t1 - t0) * PixelNoise;
			dt = t1 - t0;
		}
		else
		{
			t = tMax * (SampleI + PixelNoise) / SampleCount;
		}
		float3 P = WorldPos + t * WorldDir;
		float PHeight = length(P);

		// Sample the medium
		MediumSampleRGB Medium = SampleMediumRGB(P);
		const float3 SampleOpticalDepth = Medium.Extinction * dt * AerialPespectiveViewDistanceScale;
		const float3 SampleTransmittance = exp(-SampleOpticalDepth);
		OpticalDepth += SampleOpticalDepth;

		// Phase and transmittance for light 0
		const float3 UpVector = P / PHeight;
		float Light0ZenithCosAngle = dot(Light0Dir, UpVector);
		float3 TransmittanceToLight0 = GetTransmittance(Light0ZenithCosAngle, PHeight);
		float3 PhaseTimesScattering0;
		if (MieRayPhase)
		{
			PhaseTimesScattering0 = Medium.ScatteringMie * MiePhaseValueLight0 + Medium.ScatteringRay * RayleighPhaseValueLight0;
		}
		else
		{
			PhaseTimesScattering0 = Medium.Scattering * uniformPhase;
		}
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
		// Phase and transmittance for light 1
		float Light1ZenithCosAngle = dot(Light1Dir, UpVector);
		float3 TransmittanceToLight1 = GetTransmittance(Light1ZenithCosAngle, PHeight);
		float3 PhaseTimesScattering1;
		if (MieRayPhase)
		{
			PhaseTimesScattering1 = Medium.ScatteringMie * MiePhaseValueLight1 + Medium.ScatteringRay * RayleighPhaseValueLight1;
		}
		else
		{
			PhaseTimesScattering1 = Medium.Scattering * uniformPhase;
		}
#endif

		// Multiple scattering approximation
		float3 MultiScatteredLuminance0 = 0.0f;
#if MULTISCATTERING_APPROX_ENABLED
		MultiScatteredLuminance0 = GetMultipleScattering(P, Light0ZenithCosAngle);
#endif

		// Planet shadow
		float tPlanet0 = RaySphereIntersectNearest(P, Light0Dir, PlanetO + PLANET_RADIUS_OFFSET * UpVector, _BottomRadiusKm);
		float PlanetShadow0 = tPlanet0 >= 0.0f ? 0.0f : 1.0f;
		// MultiScatteredLuminance is already pre-exposed, atmospheric light contribution needs to be pre exposed
		// Multi-scattering is also not affected by PlanetShadow or TransmittanceToLight because it contains diffuse light after single scattering.
		float3 S = ExposedLight0Illuminance * (PlanetShadow0 * TransmittanceToLight0 * PhaseTimesScattering0 + MultiScatteredLuminance0 * Medium.Scattering);

#if SECOND_ATMOSPHERE_LIGHT_ENABLED
		float tPlanet1 = RaySphereIntersectNearest(P, Light1Dir, PlanetO + PLANET_RADIUS_OFFSET * UpVector, _BottomRadiusKm);
		float PlanetShadow1 = tPlanet1 >= 0.0f ? 0.0f : 1.0f;
		//  Multi-scattering can work for the second light but it is disabled for the sake of performance.
		S += ExposedLight1Illuminance * PlanetShadow1 * TransmittanceToLight1 * PhaseTimesScattering1;// +MultiScatteredLuminance * Medium.Scattering);
#endif

		// When using the power serie to accumulate all sattering order, serie r must be <1 for a serie to converge.
		// Under extreme coefficient, MultiScatAs1 can grow larger and thus results in broken visuals.
		// The way to fix that is to use a proper analytical integration as porposed in slide 28 of http://www.frostbite.com/2015/08/physically-based-unified-volumetric-rendering-in-frostbite/
		// However, it is possible to disable as it can also work using simple power serie sum unroll up to 5th order. The rest of the orders has a really low contribution.
#define MULTI_SCATTERING_POWER_SERIE 0
#if MULTI_SCATTERING_POWER_SERIE==0
		// 1 is the integration of luminance over the 4pi of a sphere, and assuming an isotropic phase function of 1.0/(4*PI)
		Result.MultiScatAs1 += Throughput * Medium.Scattering * 1.0f * dt;
#else
		float3 MS = Medium.Scattering * 1;
		float3 MSint = (MS - MS * SampleTransmittance) / Medium.Extinction;
		Result.MultiScatAs1 += Throughput * MSint;
#endif

#if 0
		L += Throughput * S * dt;
		Throughput *= SampleTransmittance;
#else
		// See slide 28 at http://www.frostbite.com/2015/08/physically-based-unified-volumetric-rendering-in-frostbite/
		float3 Sint = (S - S * SampleTransmittance) / Medium.Extinction;	// integrate along the current step segment
		L += Throughput * Sint;														// accumulate and also take into account the transmittance from previous steps
		Throughput *= SampleTransmittance;
#endif

		tPrev = t;
	}

	if (Ground && tMax == tBottom)
	{
		// Account for bounced light off the planet
		float3 P = WorldPos + tBottom * WorldDir;
		float PHeight = length(P);

		const float3 UpVector = P / PHeight;
		float Light0ZenithCosAngle = dot(Light0Dir, UpVector);
		float3 TransmittanceToLight0 = GetTransmittance(Light0ZenithCosAngle, PHeight);

		const float NdotL0 = saturate(dot(UpVector, Light0Dir));
		L += Light0Illuminance * TransmittanceToLight0 * Throughput * NdotL0 * _GroundAlbedo1.rgb * INV_PI;
#if SECOND_ATMOSPHERE_LIGHT_ENABLED
		{
			const float NdotL1 = saturate(dot(UpVector, Light1Dir));
			float Light1ZenithCosAngle = dot(UpVector, Light1Dir);
			float3 TransmittanceToLight1 = GetTransmittance(Light1ZenithCosAngle, PHeight);
			L += Light1Illuminance * TransmittanceToLight1 * Throughput * NdotL1 * _GroundAlbedo1.rgb * INV_PI;
		}
#endif
	}

	Result.L = L;
	Result.OpticalDepth = OpticalDepth;
	Result.Transmittance = Throughput * PlanetOnOpaque;

	return Result;
}

#endif
