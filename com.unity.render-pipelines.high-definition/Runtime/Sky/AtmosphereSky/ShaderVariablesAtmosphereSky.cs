using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    public enum AtmosphereConfig
    {
        // Tiny
        MaxAtmosphereLights = 2, // <N, L>
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public unsafe struct ShaderVariablesAtmosphereSky
    {
        //Lights
#pragma warning disable 649
        [HLSLArray((int) AtmosphereConfig.MaxAtmosphereLights, typeof(Vector4))]
        public fixed float _AtmosphereLightDirection[(int) AtmosphereConfig.MaxAtmosphereLights * 4];
        [HLSLArray((int) AtmosphereConfig.MaxAtmosphereLights, typeof(Vector4))]
        public fixed float _AtmosphereLightColor[(int) AtmosphereConfig.MaxAtmosphereLights * 4];
        [HLSLArray((int) AtmosphereConfig.MaxAtmosphereLights, typeof(Vector4))]
        public fixed float _AtmosphereLightColorGlobalPostTransmittance[(int) AtmosphereConfig.MaxAtmosphereLights * 4];
        [HLSLArray((int) AtmosphereConfig.MaxAtmosphereLights, typeof(Vector4))]
        public fixed float _AtmosphereLightDiscLuminance[(int) AtmosphereConfig.MaxAtmosphereLights * 4];
        [HLSLArray((int) AtmosphereConfig.MaxAtmosphereLights, typeof(Vector4))]
        public fixed float _AtmosphereLightDiscCosHalfApexAngle[(int) AtmosphereConfig.MaxAtmosphereLights * 4];
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _SkyViewLutReferential[(int) 16 * 2];
#pragma warning restore 649

        //Sky
        public Vector4 _SkyViewLutSizeAndInvSize;
        public Vector3 _SkyWorldCameraOrigin;
        public float _ASPUnused0;

        public Vector4 _SkyPlanetCenterAndViewHeight;
        public Vector4 _AtmosphereSkyLuminanceFactor;

        public float _AtmosphereHeightFogContribution;
        public float _AtmosphereBottomRadiusKm;
        public float _AtmosphereTopRadiusKm;
        public float _AtmosphereAerialPerspectiveStartDepthKm;

        public float _AtmosphereCameraAerialPerspectiveVolumeDepthResolution;
        public float _AtmosphereCameraAerialPerspectiveVolumeDepthResolutionInv;
        public float _AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKm;
        public float _AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKmInv;
        public float _AtmosphereApplyCameraAerialPerspectiveVolume;
        public Vector3 _ASPUnused2;

        //Atmosphere
        public float _MultiScatteringFactor;
        public float _BottomRadiusKm;
        public float _TopRadiusKm;
        public float _ASPUnused3;

        public Vector3 _RayleighScattering;
        public float _RayleighDensityExpScale;

        public Vector3 _MieScattering;
        public float _MieDensityExpScale;

        public Vector3 _MieExtinction;
        public float _MiePhaseG;

        public Vector3 _MieAbsorption;
        public float _AbsorptionDensity0LayerWidth;

        public float _AbsorptionDensity0ConstantTerm;
        public float _AbsorptionDensity0LinearTerm;
        public float _AbsorptionDensity1ConstantTerm;
        public float _AbsorptionDensity1LinearTerm;

        public Vector3 _AbsorptionExtinction;
        public float _ASPUnused4;

        public Vector3 _GroundAlbedo1;
        public float _ASPUnused5;
    }
}
