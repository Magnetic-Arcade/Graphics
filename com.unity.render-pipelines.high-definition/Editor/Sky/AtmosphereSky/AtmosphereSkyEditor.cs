using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(AtmosphereSky))]
    class AtmosphereSkyEditor : SkySettingsEditor
    {
        SerializedDataParameter m_TransformMode;
        SerializedDataParameter m_SeaLevel;
        SerializedDataParameter m_BottomRadius;
        SerializedDataParameter m_GroundAlbedo;

        SerializedDataParameter m_AtmosphereHeight;
        SerializedDataParameter m_MultiScatteringFactor;

        SerializedDataParameter m_RayleighScatteringScale;
        SerializedDataParameter m_RayleighScattering;
        SerializedDataParameter m_RayleighExponentialDistribution;

        SerializedDataParameter m_MieScatteringScale;
        SerializedDataParameter m_MieScattering;
        SerializedDataParameter m_MieAbsorptionScale;
        SerializedDataParameter m_MieAbsorption;
        SerializedDataParameter m_MieAnisotropy;
        SerializedDataParameter m_MieExponentialDistribution;

        SerializedDataParameter m_OtherAbsorptionScale;
        SerializedDataParameter m_OtherAbsorption;
        SerializedDataParameter m_OtherTentDistribution;

        SerializedDataParameter m_SkyLuminanceFactor;
        SerializedDataParameter m_AerialPespectiveViewDistanceScale;
        SerializedDataParameter m_HeightFogContribution;
        SerializedDataParameter m_TransmittanceMinLightElevationAngle;

        static readonly GUIContent s_GroundRadius = new GUIContent("Ground Radius");
        static readonly GUIContent s_MultiScattering = new GUIContent("MultiScattering");
        static readonly GUIContent s_AbsorptionScale = new GUIContent("Absorption Scale");
        static readonly GUIContent s_Absorption = new GUIContent("Absorption");
        static readonly GUIContent s_TentDistribution = new GUIContent("Tent Distribution");

        public override void OnEnable()
        {
            base.OnEnable();

            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode
                                   | (uint)SkySettingsUIElement.SkyIntensity
                                   | (uint)SkySettingsUIElement.IncludeSunInBaking;

            var o = new PropertyFetcher<AtmosphereSky>(serializedObject);

            m_TransformMode = Unpack(o.Find(x => x.transformMode));
            m_SeaLevel = Unpack(o.Find(x => x.seaLevel));
            m_BottomRadius = Unpack(o.Find(x => x.bottomRadius));
            m_GroundAlbedo = Unpack(o.Find(x => x.groundAlbedo));

            m_AtmosphereHeight = Unpack(o.Find(x => x.atmosphereHeight));
            m_MultiScatteringFactor = Unpack(o.Find(x => x.multiScatteringFactor));

            m_RayleighScatteringScale = Unpack(o.Find(x => x.rayleighScatteringScale));
            m_RayleighScattering = Unpack(o.Find(x => x.rayleighScattering));
            m_RayleighExponentialDistribution = Unpack(o.Find(x => x.rayleighExponentialDistribution));

            m_MieScatteringScale = Unpack(o.Find(x => x.mieScatteringScale));
            m_MieScattering = Unpack(o.Find(x => x.mieScattering));
            m_MieAbsorptionScale = Unpack(o.Find(x => x.mieAbsorptionScale));
            m_MieAbsorption = Unpack(o.Find(x => x.mieAbsorption));
            m_MieAnisotropy = Unpack(o.Find(x => x.mieAnisotropy));
            m_MieExponentialDistribution = Unpack(o.Find(x => x.mieExponentialDistribution));

            m_OtherAbsorptionScale = Unpack(o.Find(x => x.otherAbsorptionScale));
            m_OtherAbsorption = Unpack(o.Find(x => x.otherAbsorption));
            m_OtherTentDistribution = Unpack(o.Find(x => x.otherTentDistribution));

            m_SkyLuminanceFactor = Unpack(o.Find(x => x.skyLuminanceFactor));
            m_AerialPespectiveViewDistanceScale = Unpack(o.Find(x => x.aerialPespectiveViewDistanceScale));
            m_HeightFogContribution = Unpack(o.Find(x => x.heightFogContribution));
            m_TransmittanceMinLightElevationAngle = Unpack(o.Find(x => x.transmittanceMinLightElevationAngle));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Planet");
            PropertyField(m_TransformMode);
            PropertyField(m_SeaLevel);
            PropertyField(m_BottomRadius, s_GroundRadius);
            PropertyField(m_GroundAlbedo);

            EditorGUILayout.LabelField("Atmosphere");
            PropertyField(m_AtmosphereHeight);
            PropertyField(m_MultiScatteringFactor, s_MultiScattering);

            EditorGUILayout.LabelField("Atmosphere - Rayleigh");
            PropertyField(m_RayleighScatteringScale);
            PropertyField(m_RayleighScattering);
            PropertyField(m_RayleighExponentialDistribution);

            EditorGUILayout.LabelField("Atmosphere - Mie");
            PropertyField(m_MieScatteringScale);
            PropertyField(m_MieScattering);
            PropertyField(m_MieAbsorptionScale);
            PropertyField(m_MieAbsorption);
            PropertyField(m_MieAnisotropy);
            PropertyField(m_MieExponentialDistribution);

            EditorGUILayout.LabelField("Atmosphere - Absorption");
            PropertyField(m_OtherAbsorptionScale, s_AbsorptionScale);
            PropertyField(m_OtherAbsorption, s_Absorption);
            EditorGUI.indentLevel++;
            {
                PropertyField(m_OtherTentDistribution, s_TentDistribution);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Art Direction");
            PropertyField(m_SkyLuminanceFactor);
            PropertyField(m_AerialPespectiveViewDistanceScale);
            PropertyField(m_HeightFogContribution);
            PropertyField(m_TransmittanceMinLightElevationAngle);

            EditorGUILayout.LabelField("Miscellaneous");
            base.CommonSkySettingsGUI();
        }
    }
}
