using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedAtmosphereSkySettings
    {
        public SerializedProperty root;

        public SerializedProperty enabled,
                                  sampleCountMin,
                                  sampleCountMax,
                                  distanceToSampleCountMax,
                                  fastSkyLUT,
                                  fastSkyLUTSampleCountMin,
                                  fastSkyLUTSampleCountMax,
                                  fastSkyLUTDistanceToSampleCountMax,
                                  fastSkyLUTWidth,
                                  fastSkyLUTHeight,
                                  aerialPerspectiveStartDepth,
                                  aerialPerspectiveDepthTest,
                                  aerialPerspectiveLUTDepthResolution,
                                  aerialPerspectiveLUTDepth,
                                  aerialPerspectiveLUTSampleCountPerSlice,
                                  aerialPerspectiveLUTWidth,
                                  aerialPerspectiveLUTFastApplyOnOpaque,
                                  transmittanceLUTSampleCount,
                                  transmittanceLUTUseSmallFormat,
                                  transmittanceLUTWidth,
                                  transmittanceLUTHeight,
                                  transmittanceLUTLightPerPixelTransmittance,
                                  multiScatteringLUTSampleCount,
                                  multiScatteringLUTHighQuality,
                                  multiScatteringLUTWidth,
                                  multiScatteringLUTHeight,
                                  distantSkyLightLUT,
                                  distantSkyLightLUTAltitude,
                                  useLUT32;

        public SerializedAtmosphereSkySettings(SerializedProperty root)
        {
            this.root = root;

            enabled = root.Find((GlobalAtmosphereSkySettings s) => s.enabled);
            sampleCountMin = root.Find((GlobalAtmosphereSkySettings s) => s.sampleCountMin);
            sampleCountMax = root.Find((GlobalAtmosphereSkySettings s) => s.sampleCountMax);
            distanceToSampleCountMax = root.Find((GlobalAtmosphereSkySettings s) => s.distanceToSampleCountMax);
            fastSkyLUT = root.Find((GlobalAtmosphereSkySettings s) => s.fastSkyLUT);
            fastSkyLUTSampleCountMin = root.Find((GlobalAtmosphereSkySettings s) => s.fastSkyLUTSampleCountMin);
            fastSkyLUTSampleCountMax = root.Find((GlobalAtmosphereSkySettings s) => s.fastSkyLUTSampleCountMax);
            fastSkyLUTDistanceToSampleCountMax = root.Find((GlobalAtmosphereSkySettings s) => s.fastSkyLUTDistanceToSampleCountMax);
            fastSkyLUTWidth = root.Find((GlobalAtmosphereSkySettings s) => s.fastSkyLUTWidth);
            fastSkyLUTHeight = root.Find((GlobalAtmosphereSkySettings s) => s.fastSkyLUTHeight);
            aerialPerspectiveStartDepth = root.Find((GlobalAtmosphereSkySettings s) => s.aerialPerspectiveStartDepth);
            aerialPerspectiveDepthTest = root.Find((GlobalAtmosphereSkySettings s) => s.aerialPerspectiveDepthTest);
            aerialPerspectiveLUTDepthResolution = root.Find((GlobalAtmosphereSkySettings s) => s.aerialPerspectiveLUTDepthResolution);
            aerialPerspectiveLUTDepth = root.Find((GlobalAtmosphereSkySettings s) => s.aerialPerspectiveLUTDepth);
            aerialPerspectiveLUTSampleCountPerSlice = root.Find((GlobalAtmosphereSkySettings s) => s.aerialPerspectiveLUTSampleCountPerSlice);
            aerialPerspectiveLUTWidth = root.Find((GlobalAtmosphereSkySettings s) => s.aerialPerspectiveLUTWidth);
            aerialPerspectiveLUTFastApplyOnOpaque = root.Find((GlobalAtmosphereSkySettings s) => s.aerialPerspectiveLUTFastApplyOnOpaque);
            transmittanceLUTSampleCount = root.Find((GlobalAtmosphereSkySettings s) => s.transmittanceLUTSampleCount);
            transmittanceLUTUseSmallFormat = root.Find((GlobalAtmosphereSkySettings s) => s.transmittanceLUTUseSmallFormat);
            transmittanceLUTWidth = root.Find((GlobalAtmosphereSkySettings s) => s.transmittanceLUTWidth);
            transmittanceLUTHeight = root.Find((GlobalAtmosphereSkySettings s) => s.transmittanceLUTHeight);
            transmittanceLUTLightPerPixelTransmittance = root.Find((GlobalAtmosphereSkySettings s) => s.transmittanceLUTLightPerPixelTransmittance);
            multiScatteringLUTSampleCount = root.Find((GlobalAtmosphereSkySettings s) => s.multiScatteringLUTSampleCount);
            multiScatteringLUTHighQuality = root.Find((GlobalAtmosphereSkySettings s) => s.multiScatteringLUTHighQuality);
            multiScatteringLUTWidth = root.Find((GlobalAtmosphereSkySettings s) => s.multiScatteringLUTWidth);
            multiScatteringLUTHeight = root.Find((GlobalAtmosphereSkySettings s) => s.multiScatteringLUTHeight);
            distantSkyLightLUT = root.Find((GlobalAtmosphereSkySettings s) => s.distantSkyLightLUT);
            distantSkyLightLUTAltitude = root.Find((GlobalAtmosphereSkySettings s) => s.distantSkyLightLUTAltitude);
            useLUT32 = root.Find((GlobalAtmosphereSkySettings s) => s.useLUT32);
        }
    }
}