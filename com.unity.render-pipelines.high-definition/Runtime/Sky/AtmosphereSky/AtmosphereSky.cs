using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum AtmosphereTransformMode
    {
        PlanetTopAtAbsoluteWorldOrigin,
        PlanetTopAtComponentTransform,
        PlanetCenterAtComponentTransform,
    }

    [Serializable]
    public sealed class TentDistribution
    {
        public ClampedFloatParameter tipAltitude = new ClampedFloatParameter(25.0f, 0.0f, 60.0f);
        public ClampedFloatParameter tipValue = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter width = new ClampedFloatParameter(15.0f, 0.01f, 20.0f);
    }

    /// <summary>
    /// Environment Update volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class TransformModeParameter : VolumeParameter<AtmosphereTransformMode>
    {
        /// <summary>
        /// Environment Update parameter constructor.
        /// </summary>
        /// <param name="value">Environment Update Mode parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public TransformModeParameter(AtmosphereTransformMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [VolumeComponentMenu("Sky/Atmosphere Sky")]
    [SkyUniqueID((int)SkyType.Atmosphere)]
    public class AtmosphereSky : SkySettings
    {
        // All distance here are in kilometer and scattering/absorptions coefficient in 1/kilometers.
        const float k_EarthBottomRadius = 6360.0f;
        const float k_EarthTopRadius = 6420.0f;
        const float k_EarthRayleighScaleHeight = 8.0f;
        const float k_EarthMieScaleHeight = 1.2f;

        static readonly Vector4 s_GroundAlbedoRaw = new Vector4(0.4f, 0.4f, 0.4f);
        static readonly Vector4 s_RayleighScatteringRaw = new Vector4(0.005802f, 0.013558f, 0.033100f);
        static readonly Vector4 s_OtherAbsorptionRaw = new Vector4(0.000650f, 0.001881f, 0.000085f);

        //Planet

        [Tooltip("")]
        public TransformModeParameter transformMode = new TransformModeParameter(AtmosphereTransformMode.PlanetTopAtAbsoluteWorldOrigin);

        [Tooltip("Sets the world-space y coordinate of the planet's sea level in meters.")]
        public FloatParameter seaLevel = new FloatParameter(0);

        [Tooltip("The planet radius. (kilometers from the center to the ground level).")]
        public MinFloatParameter bottomRadius = new MinFloatParameter(k_EarthBottomRadius, 100.0f);

        // [Tooltip("The ground albedo that will tint the astmophere when the sun light will bounce on it. Only taken into account when MultiScattering > 0")]
        public ColorParameter groundAlbedo = new ColorParameter(s_GroundAlbedoRaw, hdr: true, showAlpha: false, showEyeDropper: true);

        //Atmosphere

        [Tooltip("The planet radius. (kilometers from the center to the ground level).")]
        public ClampedFloatParameter atmosphereHeight = new ClampedFloatParameter(k_EarthTopRadius - k_EarthBottomRadius, 10.0f, 200.0f);

        [Tooltip("Render multi scattering as if sun light would bounce around in the atmosphere. This is achieved using a dual scattering approach.")]
        public ClampedFloatParameter multiScatteringFactor = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);

        //Atmosphere - Rayleigh

        [Tooltip("Rayleigh scattering coefficient scale.")]
        public MinFloatParameter rayleighScatteringScale = new MinFloatParameter(s_RayleighScatteringRaw.z, 0.0f);

        [Tooltip("The Rayleigh scattering coefficients resulting from molecules in the air at an altitude of 0 kilometer.")]
        public ColorParameter rayleighScattering = new ColorParameter(GetRayleighScattering(), hdr: true, showAlpha: false, showEyeDropper: true);

        [Tooltip("The altitude in kilometer at which Rayleigh scattering effect is reduced to 40%.")]
        public MinFloatParameter rayleighExponentialDistribution = new MinFloatParameter(k_EarthRayleighScaleHeight, 0.1f);

        //Atmosphere - Mie

        [Tooltip("Mie scattering coefficient scale.")]
        public MinFloatParameter mieScatteringScale = new MinFloatParameter(0.003996f, 0.0f);

        [Tooltip("The Mie scattering coefficients resulting from particles in the air at an altitude of 0 kilometer. As it becomes higher, light will be scattered more.")]
        public ColorParameter mieScattering = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);

        [Tooltip("Mie absorption coefficient scale.")]
        public MinFloatParameter mieAbsorptionScale = new MinFloatParameter(0.000444f, 0.0f);

        [Tooltip("The Mie absorption coefficients resulting from particles in the air at an altitude of 0 kilometer. As it becomes higher, light will be absorbed more.")]
        public ColorParameter mieAbsorption = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);

        [Tooltip("A value of 0 mean light is uniformly scattered. A value closer to 1 means lights will scatter more forward, resulting in halos around light sources.")]
        public ClampedFloatParameter mieAnisotropy = new ClampedFloatParameter(0.8f, 0.0f, 0.999f);

        [Tooltip("The altitude in kilometer at which Mie effects are reduced to 40%.")]
        public ClampedFloatParameter mieExponentialDistribution = new ClampedFloatParameter(k_EarthMieScaleHeight, 0.01f, 10.0f);

        //Atmosphere - Absorption

        [Tooltip("Absorption coefficients for another atmosphere layer. Density increase from 0 to 1 between 10 to 25km and decreases from 1 to 0 between 25 to 40km. This approximates ozone molecules distribution in the Earth atmosphere.")]
        public MinFloatParameter otherAbsorptionScale = new MinFloatParameter(s_OtherAbsorptionRaw.y, 0.0f);

        [Tooltip("Absorption coefficients for another atmosphere layer. Density increase from 0 to 1 between 10 to 25km and decreases from 1 to 0 between 25 to 40km. The default values represents ozone molecules absorption in the Earth atmosphere.")]
        public ColorParameter otherAbsorption = new ColorParameter(GetOtherAbsorption(), hdr: true, showAlpha: false, showEyeDropper: true);

        [Tooltip("Represents the altitude based tent distribution of absorption particles in the atmosphere.")]
        public ObjectParameter<TentDistribution> otherTentDistribution = new ObjectParameter<TentDistribution>(new TentDistribution());

        //Art direction

        [Tooltip("Scales the luminance of pixels representing the sky, i.e. not belonging to any surface.")]
        public ColorParameter skyLuminanceFactor = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);

        [Tooltip("Makes the aerial perspective look thicker by scaling distances from view to surfaces (opaque and translucent).")]
        public MinFloatParameter aerialPespectiveViewDistanceScale = new MinFloatParameter(1.0f, 0.0f);

        [Tooltip("Scale the sky and atmosphere lights contribution to the height fog when SupportAtmosphereAffectsHeightFog project setting is true.")]
        public MinFloatParameter heightFogContribution = new MinFloatParameter(1.0f, 0.0f);

        [Tooltip("The minimum elevation angle in degree that should be used to evaluate the sun transmittance to the ground. Useful to maintain a visible sun light and shadow on meshes even when the sun has started going below the horizon. This does not affect the aerial perspective.")]
        public ClampedFloatParameter transmittanceMinLightElevationAngle = new ClampedFloatParameter(-90.0f, -90f, 90f);

        //
        AtmosphereSkyRenderData m_RenderData = new AtmosphereSkyRenderData();

        AtmosphereSky()
        {
            displayName = "Atmosphere Sky";
        }

        /// <summary> Returns the hash code of the parameters of the sky. </summary>
        /// <returns> The hash code of the parameters of the sky. </returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = hash * 23 + transformMode.GetHashCode();
                hash = hash * 23 + seaLevel.GetHashCode();
                hash = hash * 23 + bottomRadius.GetHashCode();
                hash = hash * 23 + groundAlbedo.GetHashCode();
                hash = hash * 23 + atmosphereHeight.GetHashCode();
                hash = hash * 23 + multiScatteringFactor.GetHashCode();
                hash = hash * 23 + rayleighScatteringScale.GetHashCode();
                hash = hash * 23 + rayleighScattering.GetHashCode();
                hash = hash * 23 + rayleighExponentialDistribution.GetHashCode();
                hash = hash * 23 + mieScatteringScale.GetHashCode();
                hash = hash * 23 + mieScattering.GetHashCode();
                hash = hash * 23 + mieAbsorptionScale.GetHashCode();
                hash = hash * 23 + mieAbsorption.GetHashCode();
                hash = hash * 23 + mieAnisotropy.GetHashCode();
                hash = hash * 23 + mieExponentialDistribution.GetHashCode();
                hash = hash * 23 + otherAbsorptionScale.GetHashCode();
                hash = hash * 23 + otherAbsorption.GetHashCode();
                hash = hash * 23 + otherTentDistribution.GetHashCode();
                hash = hash * 23 + skyLuminanceFactor.GetHashCode();
                hash = hash * 23 + aerialPespectiveViewDistanceScale.GetHashCode();
                hash = hash * 23 + heightFogContribution.GetHashCode();
                hash = hash * 23 + transmittanceMinLightElevationAngle.GetHashCode();
            }

            return hash;
        }

        /// <summary> Returns the type of the sky renderer. </summary>
        /// <returns> AtmosphereSkyRenderer type. </returns>
        public override Type GetSkyRendererType() { return typeof(AtmosphereSkyRenderer); }

        internal void UpdateRenderData(Vector3 position)
        {
            m_RenderData.Setup(this);
            m_RenderData.UpdateTransform(position, transformMode.value);
        }

        internal AtmosphereSkyRenderData GetRenderData() => m_RenderData;

        internal static Color GetRayleighScattering()
        {
            return s_RayleighScatteringRaw * (1.0f / s_RayleighScatteringRaw.z);
        }

        internal static Color GetOtherAbsorption()
        {
            return s_OtherAbsorptionRaw * (1.0f / s_OtherAbsorptionRaw.y);
        }
    }
}
