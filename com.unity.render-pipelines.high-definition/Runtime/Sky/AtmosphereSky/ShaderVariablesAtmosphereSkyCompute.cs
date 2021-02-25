using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering.HighDefinition
{
    // Extra internal constants shared between all passes.
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public unsafe struct ShaderVariablesAtmosphereSkyCompute
    {
        public float _SampleCountMin;
        public float _SampleCountMax;
        public float _DistanceToSampleCountMaxInv;
        public float _SVAIPad0;

        public float _FastSkySampleCountMin;
        public float _FastSkySampleCountMax;
        public float _FastSkyDistanceToSampleCountMaxInv;
        public float _SVAIPad1;

        public Vector4 _CameraAerialPerspectiveVolumeSizeAndInvSize;
        public float _CameraAerialPerspectiveSampleCountPerSlice;
        public Vector3 _SVAIPad3;

        public Vector4 _TransmittanceLutSizeAndInvSize;
        public Vector4 _MultiScatteredLuminanceLutSizeAndInvSize;

        public float _TransmittanceSampleCount;
        public float _MultiScatteringSampleCount;
        public float _AerialPespectiveViewDistanceScale;
        public float _SVAIPad4;

        public Vector3 _SkyLuminanceFactor;
        public float _SVAIPad5;

        public int _StateFrameIndexMod8;
        public Vector3 _SVAIPad6;
    }
}
