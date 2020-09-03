using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [CreateAssetMenu(fileName = "AtmosphereSkySettingsAsset", menuName = "Rendering/Atmosphere Sky Settings")]
    public class AtmosphereSkySettingsAsset : ScriptableObject
    {
        [Tooltip("Atmosphere components are rendered when this is not 0, otherwise ignored.")]
        public bool enabled = true;

        [Tooltip("Enables Atmosphere rendering and shader code.")]
        public bool supportAtmosphere = true;

        [Tooltip("Enables Atmosphere affecting height fog. It requires r.SupportAtmosphere to be true.")]
        public bool supportAtmosphereAffectsHeightFog;

        // Regular sky

        [Tooltip("The minimum sample count used to compute sky/atmosphere scattering and transmittance.")]
        public float sampleCountMin = 2.0f;

        [Tooltip("The maximum sample count used to compute sky/atmosphere scattering and transmittance.")]
        public float sampleCountMax = 16.0f;

        [Tooltip("The distance in kilometer after which at which SampleCountMax samples will be used to ray march the atmosphere.")]
        public float distanceToSampleCountMax = 150.0f;

        // Fast sky

        [Tooltip("When enabled, a look up texture is used to render the sky. It is faster but can result in visual artefacts if there are some high frequency details " +
                 "in the sky such as earth shadow or scattering lob.")]
        public bool fastSkyLUT = false;

        [Tooltip("Fast sky minimum sample count used to compute sky/atmosphere scattering and transmittance. The minimal value will be clamped to 1.")]
        public float fastSkyLUTSampleCountMin = 4.0f;

        [Tooltip("Fast sky maximum sample count used to compute sky/atmosphere scattering and transmittance. The minimal value will be clamped to FastSkyLUTSampleCountMin + 1.")]
        public float fastSkyLUTSampleCountMax = 32.0f;

        [Tooltip("Fast sky distance in kilometer after which at which SampleCountMax samples will be used to ray march the atmosphere.")]
        public float fastSkyLUTDistanceToSampleCountMax = 150.0f;

        [Tooltip("")]
        public float fastSkyLUTWidth = 192.0f;

        [Tooltip("")]
        public float fastSkyLUTHeight = 104.0f;

        // Aerial perspective

        [Tooltip("The distance at which we start evaluate the aerial pespective in Kilometers. Default: 0.1 kilometers.")]
        public float aerialPerspectiveStartDepth = 0.5f;

        [Tooltip("When enabled, a depth test will be used to not write pixel closer to the camera than StartDepth, effectively improving performance.")]
        public bool aerialPerspectiveDepthTest = true;

        // Aerial perspective LUT

        [Tooltip("The number of depth slice to use for the aerial perspective volume texture.")]
        public float aerialPerspectiveLUTDepthResolution = 16.0f;

        [Tooltip("The length of the LUT in kilometers (default = 96km to get nice cloud/atmosphere interactions in the distance for default sky). Further than this distance, the last slice is used.")]
        public float aerialPerspectiveLUTDepth = 96.0f;

        [Tooltip("The sample count used per slice to evaluate aerial perspective scattering and transmittance in camera frustum space froxel.")]
        public float aerialPerspectiveLUTSampleCountPerSlice = 2.0f;

        [Tooltip("")]
        public float aerialPerspectiveLUTWidth = 32;

        [Tooltip("When enabled, the low resolution camera frustum/froxel volume containing atmospheric fog\n, " +
                 "usually used for fog on translucent surface, is used to render fog on opaque.\n" +
                 "It is faster but can result in visual artefacts if there are some high frequency details\n " +
                 "such as earth shadow or scattering lob.")]
        public bool aerialPerspectiveLUTFastApplyOnOpaque = false;

        // Transmittance LUT

        [Tooltip("The sample count used to evaluate transmittance.")]
        public float transmittanceLUTSampleCount = 32f;

        [Tooltip("If true, the transmittance LUT will use a small R8BG8B8A8 format to store data at lower quality.")]
        public bool transmittanceLUTUseSmallFormat = false;

        [Tooltip("")]
        public float transmittanceLUTWidth = 256;

        [Tooltip("")]
        public float transmittanceLUTHeight = 64;

        [Tooltip("Enables Atmosphere light per pixel transmittance. Only for opaque objects in the deferred renderer. It is more expensive but space/planetary views will be more accurate.")]
        public bool transmittanceLUTLightPerPixelTransmittance = false;

        // Multi-scattering LUT

        [Tooltip("The sample count used to evaluate multi-scattering.")]
        public float multiScatteringLUTSampleCount = 15.0f;

        [Tooltip("The when enabled, 64 samples are used instead of 2, resulting in a more accurate multi scattering approximation (but also more expenssive).")]
        public bool multiScatteringLUTHighQuality = false;

        [Tooltip("")]
        public float multiScatteringLUTWidth = 256;

        [Tooltip("")]
        public float multiScatteringLUTHeight = 256;

        // Distant Sky Light LUT

        [Tooltip("Enable the generation the sky ambient lighting value.")]
        public bool distantSkyLightLUT = true;

        [Tooltip("The altitude at which the sky samples are taken to integrate the sky lighting. Default to 6km, typicaly cirrus clouds altitude.")]
        public float distantSkyLightLUTAltitude = 6.0f;

        // Debug / Visualization

        [Tooltip("Use full 32bit per-channel precision for all sky LUTs.")]
        public bool useLUT32 = false;
    }
}
