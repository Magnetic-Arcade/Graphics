namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDRenderPipelineUI
    {
        static void Drawer_SectionAtmosphereSky(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.enabled);
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.sampleCountMin);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.sampleCountMax);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.distanceToSampleCountMax);
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.fastSkyLUT);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.fastSkyLUTSampleCountMin);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.fastSkyLUTSampleCountMax);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.fastSkyLUTDistanceToSampleCountMax);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.fastSkyLUTWidth);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.fastSkyLUTHeight);
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.aerialPerspectiveStartDepth);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.aerialPerspectiveDepthTest);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.aerialPerspectiveLUTDepthResolution);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.aerialPerspectiveLUTDepth);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.aerialPerspectiveLUTSampleCountPerSlice);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.aerialPerspectiveLUTWidth);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.aerialPerspectiveLUTFastApplyOnOpaque);
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.transmittanceLUTSampleCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.transmittanceLUTUseSmallFormat);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.transmittanceLUTWidth);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.transmittanceLUTHeight);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.transmittanceLUTLightPerPixelTransmittance);
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.multiScatteringLUTSampleCount);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.multiScatteringLUTHighQuality);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.multiScatteringLUTWidth);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.multiScatteringLUTHeight);
            
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.distantSkyLightLUT);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.distantSkyLightLUTAltitude);
            EditorGUILayout.PropertyField(serialized.renderPipelineSettings.atmosphereSkySettings.useLUT32);
        }
    }
}