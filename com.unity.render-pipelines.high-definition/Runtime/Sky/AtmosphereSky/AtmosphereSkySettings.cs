using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public struct GlobalAtmosphereSkySettings
    {
        /// <summary>Default GlobalDynamicResolutionSettings</summary>
        /// <returns></returns>
        public static GlobalAtmosphereSkySettings NewDefault() => new GlobalAtmosphereSkySettings()
        {
            enabled = true,
            sampleCountMin = 2,
            sampleCountMax = 16,
            distanceToSampleCountMax = 150.0f,
            fastSkyLUT = true,
            fastSkyLUTSampleCountMin = 4,
            fastSkyLUTSampleCountMax = 32,
            fastSkyLUTDistanceToSampleCountMax = 150.0f,
            fastSkyLUTWidth = 192,
            fastSkyLUTHeight = 104,
            aerialPerspectiveStartDepth = 0.1f,
            aerialPerspectiveDepthTest = true,
            aerialPerspectiveLUTDepthResolution = 16,
            aerialPerspectiveLUTDepth = 96.0f,
            aerialPerspectiveLUTSampleCountPerSlice = 2,
            aerialPerspectiveLUTWidth = 32,
            aerialPerspectiveLUTFastApplyOnOpaque = false,
            transmittanceLUTSampleCount = 32,
            transmittanceLUTUseSmallFormat = false,
            transmittanceLUTWidth = 256,
            transmittanceLUTHeight = 64,
            transmittanceLUTLightPerPixelTransmittance = false,
            multiScatteringLUTSampleCount = 15,
            multiScatteringLUTHighQuality = false,
            multiScatteringLUTWidth = 256,
            multiScatteringLUTHeight = 256,
            distantSkyLightLUT = true,
            distantSkyLightLUTAltitude = 6.0f,
            useLUT32 = false,
        };
        
        [Tooltip("Atmosphere components are rendered when this is not 0, otherwise ignored.")]
        public bool enabled;
        
        // [Tooltip("Enables Atmosphere rendering and shader code.")]
        // public bool supportAtmosphere;
        //
        // [Tooltip("Enables Atmosphere affecting height fog. It requires r.SupportAtmosphere to be true.")]
        // public bool supportAtmosphereAffectsHeightFog;

        // Regular sky

        [Tooltip("The minimum sample count used to compute sky/atmosphere scattering and transmittance.")]
        [Range(1, 64)]
        public float sampleCountMin;

        [Tooltip("The maximum sample count used to compute sky/atmosphere scattering and transmittance.")]
        [Range(1, 64)]
        public float sampleCountMax;

        [Tooltip("The distance in kilometer after which at which SampleCountMax samples will be used to ray march the atmosphere.")]
        [Min(0)]
        public float distanceToSampleCountMax;

        // Fast sky

        [Tooltip("When enabled, a look up texture is used to render the sky. It is faster but can result in visual artefacts if there are some high frequency details " +
                 "in the sky such as earth shadow or scattering lob.")]
        public bool fastSkyLUT;

        [Tooltip("Fast sky minimum sample count used to compute sky/atmosphere scattering and transmittance. The minimal value will be clamped to 1.")]
        [Range(1, 64)]
        public int fastSkyLUTSampleCountMin;

        [Tooltip("Fast sky maximum sample count used to compute sky/atmosphere scattering and transmittance. The minimal value will be clamped to FastSkyLUTSampleCountMin + 1.")]
        [Range(1, 64)]
        public int fastSkyLUTSampleCountMax;

        [Tooltip("Fast sky distance in kilometer after which at which SampleCountMax samples will be used to ray march the atmosphere.")]
        [Min(0)]
        public float fastSkyLUTDistanceToSampleCountMax;

        [Tooltip("")]
        [Range(1, 512)]
        public int fastSkyLUTWidth;

        [Tooltip("")]
        [Range(1, 512)]
        public int fastSkyLUTHeight;

        // Aerial perspective

        [Tooltip("The distance at which we start evaluate the aerial pespective in Kilometers. Default: 0.1 kilometers.")]
        [Min(0)]
        public float aerialPerspectiveStartDepth;

        [Tooltip("When enabled, a depth test will be used to not write pixel closer to the camera than StartDepth, effectively improving performance.")]
        public bool aerialPerspectiveDepthTest;

        // Aerial perspective LUT

        [Tooltip("The number of depth slice to use for the aerial perspective volume texture.")]
        [Range(1, 64)]
        public int aerialPerspectiveLUTDepthResolution;

        [Tooltip("The length of the LUT in kilometers (default = 96km to get nice cloud/atmosphere interactions in the distance for default sky). Further than this distance, the last slice is used.")]
        [Min(0)]
        public float aerialPerspectiveLUTDepth;

        [Tooltip("The sample count used per slice to evaluate aerial perspective scattering and transmittance in camera frustum space froxel.")]
        [Range(1, 64)]
        public int aerialPerspectiveLUTSampleCountPerSlice;

        [Tooltip("")]
        [Range(1, 512)]
        public int aerialPerspectiveLUTWidth;

        [Tooltip("When enabled, the low resolution camera frustum/froxel volume containing atmospheric fog\n, " +
                 "usually used for fog on translucent surface, is used to render fog on opaque.\n" +
                 "It is faster but can result in visual artefacts if there are some high frequency details\n " +
                 "such as earth shadow or scattering lob.")]
        public bool aerialPerspectiveLUTFastApplyOnOpaque;

        // Transmittance LUT

        [Tooltip("The sample count used to evaluate transmittance.")]
        [Range(1, 64)]
        public int transmittanceLUTSampleCount;

        [Tooltip("If true, the transmittance LUT will use a small R8BG8B8A8 format to store data at lower quality.")]
        public bool transmittanceLUTUseSmallFormat;

        [Tooltip("")]
        [Range(1, 512)]
        public int transmittanceLUTWidth;

        [Tooltip("")]
        [Range(1, 512)]
        public int transmittanceLUTHeight;

        [Tooltip("Enables Atmosphere light per pixel transmittance. Only for opaque objects in the deferred renderer. It is more expensive but space/planetary views will be more accurate.")]
        public bool transmittanceLUTLightPerPixelTransmittance;

        // Multi-scattering LUT

        [Tooltip("The sample count used to evaluate multi-scattering.")]
        [Range(1, 64)]
        public int multiScatteringLUTSampleCount;

        [Tooltip("The when enabled, 64 samples are used instead of 2, resulting in a more accurate multi scattering approximation (but also more expenssive).")]
        public bool multiScatteringLUTHighQuality;

        [Tooltip("")]
        [Range(1, 512)]
        public int multiScatteringLUTWidth;

        [Tooltip("")]
        [Range(1, 512)]
        public int multiScatteringLUTHeight;

        // Distant Sky Light LUT

        [Tooltip("Enable the generation the sky ambient lighting value.")]
        public bool distantSkyLightLUT;

        [Tooltip("The altitude at which the sky samples are taken to integrate the sky lighting. Default to 6km, typicaly cirrus clouds altitude.")]
        [Min(0)]
        public float distantSkyLightLUTAltitude;

        // Debug / Visualization

        [Tooltip("Use full 32bit per-channel precision for all sky LUTs.")]
        public bool useLUT32;
    }
}
