Shader "Hidden/HDRP/Sky/AtmosphereSky"
{
    HLSLINCLUDE

    #pragma vertex Vert

    //#pragma enable_d3d11_debug_symbols
    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #pragma multi_compile_local _ USE_CLOUD_MAP
    #pragma multi_compile_local _ USE_CLOUD_MOTION
    #pragma multi_compile_local _ RENDER_BAKING

    #define FASTSKY_ENABLED 1
    #define RENDERSKY_ENABLED 1

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/AtmosphereSky/AtmosphereSkyComputeCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/CloudLayer/CloudLayer.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/CookieSampling.hlsl"

    int _RenderSunDisk;             // bool...
    float _Intensity;

    // Sky framework does not set up global shader variables (even per-view ones),
    // so they can contain garbage. It's very difficult to not include them, however,
    // since the sky framework includes them internally in many header files.
    // Just don't use them. Ever.
    float3   _WorldSpaceCameraPos1;
    #undef _WorldSpaceCameraPos
    #define _WorldSpaceCameraPos _WorldSpaceCameraPos1
    float4x4 _ViewMatrix1;
    #undef UNITY_MATRIX_V
    #define UNITY_MATRIX_V _ViewMatrix1

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    const static float Max10BitsFloat = 64512.0f;

    float4 PrepareOutput(float3 Luminance, float3 Transmittance = float3(1.0f, 1.0f, 1.0f))
    {
        const float GreyScaleTransmittance = dot(Transmittance, float3(1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f));
        return float4(min(Luminance, Max10BitsFloat.xxx), GreyScaleTransmittance);
    }

    float3 GetCameraPlanetPos1()
    {
        return (_WorldSpaceCameraPos1 - _SkyPlanetCenterAndViewHeight.xyz) * M_TO_SKY_UNIT;
    }

    float4 RenderSky(Varyings input)
    {
        const float R = _PlanetaryRadius;

        // TODO: Not sure it's possible to precompute cam rel pos since variables
        // in the two constant buffers may be set at a different frequency?
        const float3 O = GetCameraPlanetPos();
        const float3 V = GetSkyViewDirWS(input.positionCS.xy);

        bool renderSunDisk = _RenderSunDisk != 0;

        float4 OutLuminance = 0;

        float2 PixPos = input.positionCS.xy;
        float2 UvBuffer = PixPos * _ScreenSize.zw;	// Uv for depth buffer read (size can be larger than viewport)

        float3 WorldPos = O;
        float3 WorldDir = V;

        // Get the light disk luminance to draw
        float3 PreExposedL = 0;
        float3 LuminanceScale = _SkyLuminanceFactor;
        //float DeviceZ = LookupDeviceZ(UvBuffer);
        if (renderSunDisk)
        {
    //         PreExposedL += GetLightDiskLuminance(WorldPos, WorldDir, 0);
    // #if SECOND_ATMOSPHERE_LIGHT_ENABLED
    //         PreExposedL += GetLightDiskLuminance(WorldPos, WorldDir, 1);
    // #endif

    // #if RENDERSKY_ENABLED == 0
    //         // We should not render the sky and the current pixels are at far depth, so simply early exit.
    //         // We enable depth bound when supported to not have to even process those pixels.
    //         OutLuminance = PrepareOutput(float3(0.0f, 0.0f, 0.0f), float3(1.0f, 1.0f, 1.0f));

    //         //Now the sky pass can ignore the pixel with depth == far but it will need to alpha clip because not all RHI backend support depthbound tests.
    //         // And the depthtest is already setup to avoid writing all the pixel closer than to the camera than the start distance (very good optimisation).
    //         // Since this shader does not write to depth or stencil it should still benefit from EArlyZ even with the clip (See AMD depth-in-depth documentation)
    //         clip(-1.0f);
    //         return OutLuminance;
    // #endif

            float tFrag = FLT_INF;

            // Intersect and shade emissive celestial bodies.
            // Unfortunately, they don't write depth.
            for (uint i = 0; i < _DirectionalLightCount; i++)
            {
                DirectionalLightData light = _DirectionalLightDatas[i];

                // Use scalar or integer cores (more efficient).
                bool interactsWithSky = asint(light.distanceFromCamera) >= 0;

                // Celestial body must be outside the atmosphere (request from Pierre D).
                float lightDist = light.distanceFromCamera;//max(light.distanceFromCamera, tExit);

                if (interactsWithSky && asint(light.angularDiameter) != 0 && lightDist < tFrag)
                {
                    // We may be able to see the celestial body.
                    float3 L = -light.forward.xyz;

                    float LdotV    = -dot(L, V);
                    float rad      = acos(LdotV);
                    float radInner = 0.5 * light.angularDiameter;
                    float cosInner = cos(radInner);
                    float cosOuter = cos(radInner + light.flareSize);

                    // float solidAngle = TWO_PI * (1 - cosInner);
                    float solidAngle = 1; // Don't scale...

                    if (LdotV >= cosOuter)
                    {
                        // Sun flare is visible. Sun disk may or may not be visible.
                        // Assume uniform emission.
                        float3 color = light.color.rgb;
                        float  scale = rcp(solidAngle);

                        if (LdotV >= cosInner) // Sun disk.
                        {
                            tFrag = lightDist;

                            if (light.surfaceTextureScaleOffset.x > 0)
                            {
                                // The cookie code de-normalizes the axes.
                                float2 proj   = float2(dot(-V, normalize(light.right)), dot(-V, normalize(light.up)));
                                float2 angles = HALF_PI - acos(proj);
                                float2 uv     = angles * rcp(radInner) * 0.5 + 0.5;

                                color *= SampleCookie2D(uv, light.surfaceTextureScaleOffset);
                                // color *= SAMPLE_TEXTURE2D_ARRAY(_CookieTextures, s_linear_clamp_sampler, uv, light.surfaceTextureIndex).rgb;
                            }

                            color *= light.surfaceTint;
                        }
                        else // Flare region.
                        {
                            float r = max(0, rad - radInner);
                            float w = saturate(1 - r * rcp(light.flareSize));

                            color *= light.flareTint;
                            scale *= pow(w, light.flareFalloff);
                        }

                        PreExposedL += color * scale;
                    }
                }
            }
        }

        float ViewHeight = length(WorldPos);

    #if FASTSKY_ENABLED && RENDERSKY_ENABLED
        //
        if (ViewHeight < _TopRadiusKm /*&& DeviceZ == FarDepthValue*/)
        {
            float2 UV;

            // The referencial used to build the Sky View lut
            float3 F = GetViewForwardDir();
            float3 R = GetViewRightDir();
            float3x3 LocalReferencial = GetSkyViewLutReferential(WorldPos, F, R);

            // Input vectors expressed in this referencial: Up is always Z. Also note that ViewHeight is unchanged in this referencial.
            float3 WorldPosLocal = float3(0.0, ViewHeight, 0.0);
            float3 UpVectorLocal = float3(0.0, 1.0, 0.0);
            float3 WorldDirLocal = mul(-V, LocalReferencial);

            // Now evaluate inputs in the referential.
            float ViewZenithCosAngle = dot(WorldDirLocal, UpVectorLocal);
            bool IntersectGround = RaySphereIntersectNearest(WorldPosLocal, WorldDirLocal, float3(0, 0, 0), _BottomRadiusKm) >= 0.0f;

            SkyViewLutParamsToUv(IntersectGround, ViewZenithCosAngle, WorldDirLocal, ViewHeight, _BottomRadiusKm, _SkyViewLutSizeAndInvSize, UV);
            float3 SkyLuminance = _SkyViewLutTexture.SampleLevel(sampler_SkyViewLutTexture, UV, 0).rgb;

            PreExposedL += SkyLuminance * LuminanceScale;
            OutLuminance = PrepareOutput(PreExposedL);
        }
        //
    #elif FASTAERIALPERSPECTIVE_ENABLED
        // const float OneOverPreExposure = 1.0f;

        // float3 DepthBufferWorldPos = GetScreenWorldPos(SVPos, DeviceZ).xyz;
        // float4 NDCPosition = mul(float4(DepthBufferWorldPos.xyz, 1), View.WorldToClip);

        // float4 AP = GetAerialPerspectiveLuminanceTransmittance(
        //     NDCPosition, DepthBufferWorldPos * M_TO_SKY_UNIT, GetCameraWorldPos() * M_TO_SKY_UNIT,
        //     CameraAerialPerspectiveVolumeTexture, CameraAerialPerspectiveVolumeTextureSampler,
        //     _AtmosphereCameraAerialPerspectiveVolumeDepthResolutionInv,
        //     _AtmosphereCameraAerialPerspectiveVolumeDepthResolution,
        //     AerialPerspectiveStartDepthKm,
        //     _AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKm,
        //     _AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKmInv,
        //     OneOverPreExposure);

        // PreExposedL += AP.rgb * LuminanceScale;
        // float Transmittance = AP.a;

        // OutLuminance = PrepareOutput(PreExposedL, float3(Transmittance, Transmittance, Transmittance));
        //
    #else // FASTAERIALPERSPECTIVE_ENABLED

        // // Move to top atmosphere as the starting point for ray marching.
        // // This is critical to be after the above to not disrupt above atmosphere tests and voxel selection.
        // if (!MoveToTopAtmosphere(WorldPos, WorldDir, _TopRadiusKm))
        // {
        //     // Ray is not intersecting the atmosphere
        //     OutLuminance = PrepareOutput(PreExposedL);
        // }
        // else
        // {
        //     // Apply the start depth offset after moving to the top of atmosphere for consistency (and to avoid wrong out-of-atmosphere test resulting in black pixels).
        //     WorldPos += WorldDir * _AtmosphereAerialPerspectiveStartDepthKm;

        //     SamplingSetup Sampling;
        //     {
        //         Sampling.VariableSampleCount = true;
        //         Sampling.MinSampleCount = _SampleCountMin;
        //         Sampling.MaxSampleCount = _SampleCountMax;
        //         Sampling.DistanceToSampleCountMaxInv = _DistanceToSampleCountMaxInv;
        //     }
        //     const bool Ground = false;
        //     const bool MieRayPhase = true;
        //     const float AerialPespectiveViewDistanceScale = 1.0f /* DeviceZ == FarDepthValue ? 1.0f : _AerialPespectiveViewDistanceScale*/;
        //     SingleScatteringResult ss = IntegrateSingleScatteredLuminance(
        //         input.positionCS, WorldPos, WorldDir,
        //         Ground, Sampling, /*DeviceZ*/FLT_INF, MieRayPhase,
        //         _AtmosphereLightDirection[0].xyz, _AtmosphereLightDirection[1].xyz, _AtmosphereLightColor[0].rgb, _AtmosphereLightColor[1].rgb,
        //         AerialPespectiveViewDistanceScale);

        //     PreExposedL += ss.L * LuminanceScale;

        //     OutLuminance = PrepareOutput(PreExposedL, ss.Transmittance);
        }
    #endif

        // Hacky way to boost the clouds for PBR sky
        OutLuminance.rgb += ApplyCloudLayer(-V, 0);
        OutLuminance *= _Intensity;

        #if SHADEROPTIONS_VERTEX_FOG == 1 && !defined(RENDER_BAKING)
            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
            posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;
            float3 color;
            float3 opacity;
            EvaluateAtmosphericScattering(posInput, V, color, opacity); // Premultiplied alpha
            // CompositeOver(color, opacity, skyColor, skyOpacity, skyColor, skyOpacity);
            OutLuminance.rgb = OutLuminance.rgb * (1 - opacity) + color;
        #endif

        return float4(OutLuminance.xyz, 1.0);
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderSky(input); // The cube map is not pre-exposed
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float4 value = RenderSky(input);
        value.rgb *= GetCurrentExposureMultiplier(); // Only the full-screen pass is pre-exposed
        return value;
    }

    float4 FragBlack(Varyings input) : SV_Target
    {
        return 0;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBlack
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBlack
            ENDHLSL
        }

    }
    Fallback Off
}
