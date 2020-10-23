using Unity.Collections;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;

#endif

namespace UnityEngine.Rendering.HighDefinition
{
    public class AtmosphereSkyRenderer : SkyRenderer
    {
        AtmosphereSkySettingsAsset m_Settings;
        ShaderVariablesAtmosphereSky m_ConstantBuffer = new ShaderVariablesAtmosphereSky();
        ShaderVariablesAtmosphereSkyCompute m_ComputeConstantBuffer = new ShaderVariablesAtmosphereSkyCompute();

        ComputeShader m_RenderTransmittanceLutCS;
        ComputeShader m_RenderMultiScatteredLuminanceLutCS;
        ComputeShader m_RenderDistantSkyLightLutCS;
        ComputeShader m_RenderSkyViewLutCS;
        ComputeShader m_RenderCameraAerialPerspectiveVolumeCS;

        // ComputeShader m_RenderDebugAtmospherePs;
        // Shader m_RenderAtmosphereEditorHudPs;

        RTHandle m_TransmittanceLutTexture;
        RTHandle m_MultiScatteredLuminanceLutTexture;
        RTHandle m_DistantSkyLightLutTexture;
        RTHandle m_AtmosphereViewLutTexture;
        RTHandle m_AtmosphereCameraAerialPerspectiveVolume;
        ComputeBuffer m_UniformSphereSamplesBuffer;

        int m_TransmittanceLutWidth;
        int m_TransmittanceLutHeight;
        int m_MultiScatteredLuminanceLutWidth;
        int m_MultiScatteredLuminanceLutHeight;
        int m_SkyViewLutWidth;
        int m_SkyViewLutHeight;
        int m_CameraAerialPerspectiveVolumeScreenResolution;
        int m_CameraAerialPerspectiveVolumeDepthResolution;
        float m_CameraAerialPerspectiveVolumeDepthKm;
        float m_CameraAerialPerspectiveVolumeDepthSliceLengthKm;

        Material m_AtmosphereSkyMaterial;
        static MaterialPropertyBlock s_AtmosphereSkyMaterialProperties;

        ProfilingSampler m_GlobalLutSampler;
        ProfilingSampler m_ViewLutSampler;

        int m_LastParamHash;

        const float k_KmToM = 1000.0f;
        const float k_MToKm = (1.0f / k_KmToM);

        public AtmosphereSkyRenderer()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            SceneManager.activeSceneChanged += SceneManagerOnactiveSceneChanged;
#endif
        }

        ~AtmosphereSkyRenderer()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            SceneManager.activeSceneChanged -= SceneManagerOnactiveSceneChanged;
#endif
        }

#if UNITY_EDITOR
        void OnBeforeAssemblyReload()
        {
            m_LastParamHash = 0;
        }

        void SceneManagerOnactiveSceneChanged(Scene a, Scene b)
        {
            m_LastParamHash = 0;
        }
#endif

        static AtmosphereSkySettingsAsset s_DefaultSettings;

        public override void Build()
        {
            var hdrpAsset = HDRenderPipeline.currentAsset;
            var hdrpResources = HDRenderPipeline.defaultAsset.renderPipelineResources;

            m_Settings = ScriptableObject.CreateInstance<AtmosphereSkySettingsAsset>();
            m_LastParamHash = 0;

            m_GlobalLutSampler = new ProfilingSampler("Sky Atmosphere (Global LUTs)");
            m_ViewLutSampler = new ProfilingSampler("Sky Atmosphere (View LUTs)");

            m_RenderTransmittanceLutCS = hdrpResources.shaders.atomosphereSkyRenderTransmittanceLutCS;
            m_RenderMultiScatteredLuminanceLutCS = hdrpResources.shaders.atomosphereSkyRenderMultiScatteredLuminanceLutCS;
            m_RenderDistantSkyLightLutCS = hdrpResources.shaders.atomosphereSkyRenderDistantLightLutCS;
            m_RenderSkyViewLutCS = hdrpResources.shaders.atomosphereSkyRenderViewLutCS;
            m_RenderCameraAerialPerspectiveVolumeCS = hdrpResources.shaders.atomosphereSkyRenderAerialVolumeCS;

            Debug.Assert(m_RenderTransmittanceLutCS != null);
            Debug.Assert(m_RenderMultiScatteredLuminanceLutCS != null);
            Debug.Assert(m_RenderDistantSkyLightLutCS != null);
            Debug.Assert(m_RenderSkyViewLutCS != null);
            Debug.Assert(m_RenderCameraAerialPerspectiveVolumeCS != null);

            s_AtmosphereSkyMaterialProperties = new MaterialPropertyBlock();
            m_AtmosphereSkyMaterial = CoreUtils.CreateEngineMaterial(hdrpResources.shaders.atmosphereSkyPS);

            m_TransmittanceLutWidth = ValidateLutResolution(m_Settings.transmittanceLUTWidth);
            m_TransmittanceLutHeight = ValidateLutResolution(m_Settings.transmittanceLUTHeight);
            m_MultiScatteredLuminanceLutWidth = ValidateLutResolution(m_Settings.multiScatteringLUTWidth);
            m_MultiScatteredLuminanceLutHeight = ValidateLutResolution(m_Settings.multiScatteringLUTHeight);
            m_SkyViewLutWidth = ValidateLutResolution(m_Settings.fastSkyLUTWidth);
            m_SkyViewLutHeight = ValidateLutResolution(m_Settings.fastSkyLUTHeight);
            m_CameraAerialPerspectiveVolumeScreenResolution = ValidateLutResolution(m_Settings.aerialPerspectiveLUTWidth);
            m_CameraAerialPerspectiveVolumeDepthResolution = ValidateLutResolution(m_Settings.aerialPerspectiveLUTDepthResolution);
            m_CameraAerialPerspectiveVolumeDepthKm = m_Settings.aerialPerspectiveLUTDepth;
            m_CameraAerialPerspectiveVolumeDepthKm = m_CameraAerialPerspectiveVolumeDepthKm < 1.0f ? 1.0f : m_CameraAerialPerspectiveVolumeDepthKm; /* 1 kilometer minimum */
            m_CameraAerialPerspectiveVolumeDepthSliceLengthKm = m_CameraAerialPerspectiveVolumeDepthKm / m_CameraAerialPerspectiveVolumeDepthResolution;

            var textureLutFormat = GetSkyLutTextureFormat(SystemInfo.graphicsDeviceType);
            var textureLutSmallFormat = GetSkyLutSmallTextureFormat();
            var transtmittanceLutUseSmallFormat = m_Settings.transmittanceLUTUseSmallFormat;

            m_TransmittanceLutTexture = RTHandles.Alloc(m_TransmittanceLutWidth, m_TransmittanceLutHeight,
                                                        colorFormat: transtmittanceLutUseSmallFormat ? textureLutSmallFormat : textureLutFormat,
                                                        filterMode: FilterMode.Trilinear,
                                                        wrapMode: TextureWrapMode.Clamp,
                                                        enableRandomWrite: true,
                                                        name: "AtmosphereSky_Transmittance");
            Debug.Assert(m_TransmittanceLutTexture != null);

            m_MultiScatteredLuminanceLutTexture = RTHandles.Alloc(m_MultiScatteredLuminanceLutWidth, m_MultiScatteredLuminanceLutHeight,
                                                                  colorFormat: textureLutFormat,
                                                                  filterMode: FilterMode.Trilinear,
                                                                  wrapMode: TextureWrapMode.Clamp,
                                                                  enableRandomWrite: true,
                                                                  name: "AtmosphereSky_MultiScatteredLuminance");
            Debug.Assert(m_MultiScatteredLuminanceLutTexture != null);

            m_DistantSkyLightLutTexture = RTHandles.Alloc(1, 1,
                                                          colorFormat: textureLutFormat,
                                                          filterMode: FilterMode.Trilinear,
                                                          wrapMode: TextureWrapMode.Clamp,
                                                          enableRandomWrite: true,
                                                          name: "AtmosphereSky_DistantLight");
            Debug.Assert(m_DistantSkyLightLutTexture != null);

            m_AtmosphereViewLutTexture = RTHandles.Alloc(m_SkyViewLutWidth, m_SkyViewLutHeight,
                                                         colorFormat: textureLutFormat,
                                                         filterMode: FilterMode.Trilinear,
                                                         wrapMode: TextureWrapMode.Clamp,
                                                         enableRandomWrite: true,
                                                         name: "AtmosphereSky_View");
            Debug.Assert(m_AtmosphereViewLutTexture != null);

            var volumeFormat = m_Settings.useLUT32 ? GraphicsFormat.R32G32B32A32_SFloat : GraphicsFormat.R16G16B16A16_SFloat;
            m_AtmosphereCameraAerialPerspectiveVolume = RTHandles.Alloc(m_CameraAerialPerspectiveVolumeScreenResolution, m_CameraAerialPerspectiveVolumeScreenResolution,
                                                                        m_CameraAerialPerspectiveVolumeDepthResolution,
                                                                        dimension: TextureDimension.Tex3D,
                                                                        colorFormat: volumeFormat,
                                                                        filterMode: FilterMode.Trilinear,
                                                                        wrapMode: TextureWrapMode.Clamp,
                                                                        enableRandomWrite: true,
                                                                        name: "AtmosphereSky_AerialPerspective");
            Debug.Assert(m_AtmosphereCameraAerialPerspectiveVolume != null);

            SetupUniformSphereBuffer();
        }

        public override void Cleanup()
        {
            m_LastParamHash = 0;

            RTHandles.Release(m_TransmittanceLutTexture);
            m_TransmittanceLutTexture = null;
            RTHandles.Release(m_MultiScatteredLuminanceLutTexture);
            m_MultiScatteredLuminanceLutTexture = null;
            RTHandles.Release(m_DistantSkyLightLutTexture);
            m_DistantSkyLightLutTexture = null;
            RTHandles.Release(m_AtmosphereViewLutTexture);
            m_AtmosphereViewLutTexture = null;
            RTHandles.Release(m_AtmosphereCameraAerialPerspectiveVolume);
            m_AtmosphereCameraAerialPerspectiveVolume = null;
            CoreUtils.SafeRelease(m_UniformSphereSamplesBuffer);
            m_UniformSphereSamplesBuffer = null;
            CoreUtils.Destroy(m_AtmosphereSkyMaterial);
            m_AtmosphereSkyMaterial = null;
        }

        protected override bool Update(BuiltinSkyParameters builtinParams)
        {
            var atmosphereSky = builtinParams.skySettings as AtmosphereSky;

            // CoreUtils.SetKeyword(builtinParams.commandBuffer, "_SUPPORT_ATMOSPHERE", m_Settings.supportAtmosphere);
            // CoreUtils.SetKeyword(builtinParams.commandBuffer, "_SUPPORT_ATMOSPHERE_AFFECTS_HEIGHFOG", m_Settings.supportAtmosphereAffectsHeightFog);

            var hashCode = atmosphereSky.GetHashCode();
            if (hashCode != m_LastParamHash)
            {
                m_LastParamHash = hashCode;
                atmosphereSky.UpdateInternalParams(new Vector3(0, atmosphereSky.seaLevel.value, 0));

                UpdateGlobalConstantBuffer(builtinParams.commandBuffer, builtinParams);
                UpdateInternalConstantBuffer(builtinParams.commandBuffer, builtinParams);

                using (new ProfilingScope(builtinParams.commandBuffer, m_GlobalLutSampler))
                {
                    RenderAtmosphereLookUpTables(builtinParams);
                }

                // If the sky is realtime, an upcoming update will update the sky lighting. Otherwise we need to force an update.
                return builtinParams.skySettings.updateMode != EnvironmentUpdateMode.Realtime;
            }
            else
            {
                UpdateGlobalConstantBuffer(builtinParams.commandBuffer, builtinParams);
                UpdateInternalConstantBuffer(builtinParams.commandBuffer, builtinParams);

                builtinParams.commandBuffer.SetGlobalTexture(s_TransmittanceLutTexture, m_TransmittanceLutTexture);
                builtinParams.commandBuffer.SetGlobalTexture(s_MultiScatteredLuminanceLutTexture, m_MultiScatteredLuminanceLutTexture);
                builtinParams.commandBuffer.SetGlobalTexture(s_DistantSkyLightLutTexture, m_DistantSkyLightLutTexture);

                return false;
            }
        }

        public override void PreRenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            using (new ProfilingScope(builtinParams.commandBuffer, m_ViewLutSampler))
            {
                RenderAtmosphereViewDependentLookUpTables(builtinParams, false, renderSunDisk);
            }

            builtinParams.commandBuffer.SetGlobalTexture(s_SkyViewLutTexture, m_AtmosphereViewLutTexture);
            builtinParams.commandBuffer.SetGlobalTexture(s_CameraAerialPerspectiveVolume, m_AtmosphereCameraAerialPerspectiveVolume);
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            if (renderForCubemap)
            {
                RenderAtmosphereViewDependentLookUpTables(builtinParams, renderForCubemap, renderSunDisk);

                builtinParams.commandBuffer.SetGlobalTexture(s_SkyViewLutTexture, m_AtmosphereViewLutTexture);
                builtinParams.commandBuffer.SetGlobalTexture(s_CameraAerialPerspectiveVolume, m_AtmosphereCameraAerialPerspectiveVolume);
            }

            float iMul = GetSkyIntensity(builtinParams.skySettings, builtinParams.debugSettings);
            s_AtmosphereSkyMaterialProperties.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            s_AtmosphereSkyMaterialProperties.SetVector(HDShaderIDs._WorldSpaceCameraPos1, builtinParams.worldSpaceCameraPos);
            s_AtmosphereSkyMaterialProperties.SetMatrix(HDShaderIDs._ViewMatrix1, builtinParams.viewMatrix);
            s_AtmosphereSkyMaterialProperties.SetInt(HDShaderIDs._RenderSunDisk, renderSunDisk ? 1 : 0);
            s_AtmosphereSkyMaterialProperties.SetFloat("_Intensity", iMul);

            int pass = (renderForCubemap ? 0 : 2);
            CoreUtils.SetKeyword(m_AtmosphereSkyMaterial, "RENDER_BAKING", renderForCubemap);

            CloudLayer.Apply(builtinParams.cloudLayer, m_AtmosphereSkyMaterial);

            ConstantBuffer.Push(builtinParams.commandBuffer, m_ComputeConstantBuffer, m_AtmosphereSkyMaterial, s_ShaderVariablesAtmosphereSkyCompute);
            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_AtmosphereSkyMaterial, s_AtmosphereSkyMaterialProperties, pass);
        }

        void SetupUniformSphereBuffer()
        {
            const int groupSize = 8;
            const float groupSizeInv = 1.0f / (float) (groupSize);

            m_UniformSphereSamplesBuffer = new ComputeBuffer(groupSize * groupSize, sizeof(float) * 4, ComputeBufferType.Default);
            var dest = new NativeArray<Vector4>(groupSize * groupSize, Allocator.Temp);

            var prevRndState = Random.state;
            {
                Random.InitState(unchecked((int) 0xDE4DC0DE));
                for (int i = 0; i < groupSize; ++i)
                {
                    for (int j = 0; j < groupSize; ++j)
                    {
                        float u0 = (i + Random.value) * groupSizeInv;
                        float u1 = (j + Random.value) * groupSizeInv;

                        float a = 1.0f - 2.0f * u0;
                        float b = Mathf.Sqrt(1.0f - a * a);
                        float phi = 2 * Mathf.PI * u1;

                        int idx = j * groupSize + i;
                        dest[idx] = new Vector4
                        {
                            x = b * Mathf.Cos(phi),
                            y = a,
                            z = b * Mathf.Sin(phi),
                            w = 0.0f
                        };
                    }
                }
            }
            Random.state = prevRndState;

            m_UniformSphereSamplesBuffer.SetData(dest);
            dest.Dispose();
        }

        static readonly int s_TransmittanceLutTexture = Shader.PropertyToID("_TransmittanceLutTexture");
        static readonly int s_MultiScatteredLuminanceLutTexture = Shader.PropertyToID("_MultiScatteredLuminanceLutTexture");
        static readonly int s_DistantSkyLightLutTexture = Shader.PropertyToID("_DistantSkyLightLutTexture");
        static readonly int s_SkyViewLutTexture = Shader.PropertyToID("_SkyViewLutTexture");
        static readonly int s_CameraAerialPerspectiveVolume = Shader.PropertyToID("_CameraAerialPerspectiveVolume");

        static readonly int s_ShaderVariablesAtmosphereSky = Shader.PropertyToID("ShaderVariablesAtmosphereSky");
        static readonly int s_ShaderVariablesAtmosphereSkyCompute = Shader.PropertyToID("ShaderVariablesAtmosphereSkyCompute");

        static readonly int s_UniformSphereSamplesBuffer = Shader.PropertyToID("UniformSphereSamplesBuffer");
        static readonly int s_UniformSphereSamplesBufferSampleCount = Shader.PropertyToID("UniformSphereSamplesBufferSampleCount");

        //RenderTransmittanceLut
        static readonly int s_TransmittanceLutUav = Shader.PropertyToID("TransmittanceLutUAV");

        //RenderMultiScatteredLuminanceLut
        static readonly int s_MultiScatteredLuminanceLutUav = Shader.PropertyToID("MultiScatteredLuminanceLutUAV");

        //RenderDistanceSkyLut
        static readonly int s_DistantSkyLightLutUav = Shader.PropertyToID("DistantSkyLightLutUAV");

        static readonly int s_DistantSkyLightSampleAltitude = Shader.PropertyToID("DistantSkyLightSampleAltitude");

        //RenderSkyViewLut
        static readonly int s_SkyViewLutUav = Shader.PropertyToID("SkyViewLutUAV");

        //RenderCameraAerialPerspective
        static readonly int s_AerialPerspectiveStartDepthKm = Shader.PropertyToID("AerialPerspectiveStartDepthKm");
        static readonly int s_CameraAerialPerspectiveVolumeUav = Shader.PropertyToID("CameraAerialPerspectiveVolumeUAV");

        void RenderAtmosphereLookUpTables(BuiltinSkyParameters builtinParams)
        {
            var cmd = builtinParams.commandBuffer;
            var atmosphereSky = builtinParams.skySettings as AtmosphereSky;
            var useMultiScattering = atmosphereSky.multiScatteringFactor.value > 0f;
            var useHighQualityMultiScattering = m_Settings.multiScatteringLUTHighQuality;
            var secondAtmosphereLightEnabled = false; //lights.Count > 1;

            //RenderTransmittanceLut
            {
                const int renderTransmittanceLutGroupSize = 8;

                //constants
                ConstantBuffer.Push(cmd, m_ComputeConstantBuffer, m_RenderTransmittanceLutCS, s_ShaderVariablesAtmosphereSkyCompute);
                //uavs
                cmd.SetComputeTextureParam(m_RenderTransmittanceLutCS, 0, s_TransmittanceLutUav, m_TransmittanceLutTexture);

                cmd.DispatchCompute(m_RenderTransmittanceLutCS, 0,
                                    HDUtils.DivRoundUp(m_TransmittanceLutTexture.rt.width, renderTransmittanceLutGroupSize),
                                    HDUtils.DivRoundUp(m_TransmittanceLutTexture.rt.height, renderTransmittanceLutGroupSize),
                                    1);
            }
            //MultiScatteredLuminanceLutUAV
            {
                const int multiscatteredGs = 8;

                //keywords
                CoreUtils.SetKeyword(m_RenderMultiScatteredLuminanceLutCS, "HIGHQUALITY_MULTISCATTERING_APPROX_ENABLED", useHighQualityMultiScattering);
                //constants
                ConstantBuffer.Push(cmd, m_ComputeConstantBuffer, m_RenderMultiScatteredLuminanceLutCS, s_ShaderVariablesAtmosphereSkyCompute);
                cmd.SetComputeIntParam(m_RenderMultiScatteredLuminanceLutCS, m_UniformSphereSamplesBuffer.count, s_UniformSphereSamplesBufferSampleCount);
                //textures
                cmd.SetComputeTextureParam(m_RenderMultiScatteredLuminanceLutCS, 0, s_TransmittanceLutTexture, m_TransmittanceLutTexture);
                //buffers
                cmd.SetComputeBufferParam(m_RenderMultiScatteredLuminanceLutCS, 0, s_UniformSphereSamplesBuffer, m_UniformSphereSamplesBuffer);
                //uavs
                cmd.SetComputeTextureParam(m_RenderMultiScatteredLuminanceLutCS, 0, s_MultiScatteredLuminanceLutUav, m_MultiScatteredLuminanceLutTexture);
                //dispatch
                cmd.DispatchCompute(m_RenderMultiScatteredLuminanceLutCS, 0,
                                    HDUtils.DivRoundUp(m_MultiScatteredLuminanceLutTexture.rt.width, multiscatteredGs),
                                    HDUtils.DivRoundUp(m_MultiScatteredLuminanceLutTexture.rt.height, multiscatteredGs),
                                    1);
            }
            //RenderDistanceSkyLut
            if (m_Settings.distantSkyLightLUT)
            {
                const int distantSkyLightGs = 8;

                //keywords
                CoreUtils.SetKeyword(m_RenderDistantSkyLightLutCS, "MULTISCATTERING_APPROX_ENABLED", useMultiScattering);
                CoreUtils.SetKeyword(m_RenderDistantSkyLightLutCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", secondAtmosphereLightEnabled);
                //constants
                ConstantBuffer.Push(cmd, m_ComputeConstantBuffer, m_RenderDistantSkyLightLutCS, s_ShaderVariablesAtmosphereSkyCompute);
                cmd.SetComputeFloatParam(m_RenderDistantSkyLightLutCS, s_DistantSkyLightSampleAltitude, m_Settings.distantSkyLightLUTAltitude);
                //textures
                cmd.SetComputeTextureParam(m_RenderDistantSkyLightLutCS, 0, s_TransmittanceLutTexture, m_TransmittanceLutTexture);
                cmd.SetComputeTextureParam(m_RenderDistantSkyLightLutCS, 0, s_MultiScatteredLuminanceLutTexture, m_MultiScatteredLuminanceLutTexture);
                //buffers
                cmd.SetComputeBufferParam(m_RenderDistantSkyLightLutCS, 0, s_UniformSphereSamplesBuffer, m_UniformSphereSamplesBuffer);
                //uavs
                cmd.SetComputeTextureParam(m_RenderDistantSkyLightLutCS, 0, s_DistantSkyLightLutUav, m_DistantSkyLightLutTexture);
                //dispatch
                cmd.DispatchCompute(m_RenderDistantSkyLightLutCS, 0,
                                    HDUtils.DivRoundUp(1, distantSkyLightGs),
                                    HDUtils.DivRoundUp(1, distantSkyLightGs),
                                    HDUtils.DivRoundUp(1, distantSkyLightGs));
            }
        }

        void RenderAtmosphereViewDependentLookUpTables(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            var cmd = builtinParams.commandBuffer;
            var atmosphereSky = builtinParams.skySettings as AtmosphereSky;
            var camera = builtinParams.hdCamera.camera;
            var useMultiScattering = atmosphereSky.multiScatteringFactor.value > 0f;
            var secondAtmosphereLightEnabled = false; //lights.Count > 1;

            float aerialPerspectiveStartDepthInM = GetValidAerialPerspectiveStartDepthInM(camera);
            // bool bLightDiskEnabled = hdCamera.camera.cameraType != CameraType.Reflection;

            // Sky View
            {
                const int skyViewLutGs = 8;

                //keywords
                CoreUtils.SetKeyword(m_RenderSkyViewLutCS, "MULTISCATTERING_APPROX_ENABLED", useMultiScattering);
                CoreUtils.SetKeyword(m_RenderSkyViewLutCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", secondAtmosphereLightEnabled);
                CoreUtils.SetKeyword(m_RenderSkyViewLutCS, "SOURCE_DISK_ENABLED", renderSunDisk);
                //constants
                ConstantBuffer.Push(cmd, m_ComputeConstantBuffer, m_RenderSkyViewLutCS, s_ShaderVariablesAtmosphereSkyCompute);
                cmd.SetComputeMatrixParam(m_RenderSkyViewLutCS, HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
                cmd.SetComputeVectorParam(m_RenderSkyViewLutCS, HDShaderIDs._WorldSpaceCameraPos1, builtinParams.worldSpaceCameraPos);
                cmd.SetComputeMatrixParam(m_RenderSkyViewLutCS, HDShaderIDs._ViewMatrix1, builtinParams.viewMatrix);
                //textures
                cmd.SetComputeTextureParam(m_RenderSkyViewLutCS, 0, s_TransmittanceLutTexture, m_TransmittanceLutTexture);
                cmd.SetComputeTextureParam(m_RenderSkyViewLutCS, 0, s_MultiScatteredLuminanceLutTexture, m_MultiScatteredLuminanceLutTexture);
                //uavs
                cmd.SetComputeTextureParam(m_RenderSkyViewLutCS, 0, s_SkyViewLutUav, m_AtmosphereViewLutTexture);
                //dispatch
                cmd.DispatchCompute(m_RenderSkyViewLutCS, 0,
                                    HDUtils.DivRoundUp(m_AtmosphereViewLutTexture.rt.width, skyViewLutGs),
                                    HDUtils.DivRoundUp(m_AtmosphereViewLutTexture.rt.height, skyViewLutGs),
                                    1);
            }
            // Camera Atmosphere Volume
            {
                const int renderTransmittanceLutGroupSize = 8;

                //keywords
                CoreUtils.SetKeyword(m_RenderCameraAerialPerspectiveVolumeCS, "SOURCE_DISK_ENABLED", renderSunDisk);
                CoreUtils.SetKeyword(m_RenderCameraAerialPerspectiveVolumeCS, "SECOND_ATMOSPHERE_LIGHT_ENABLED", secondAtmosphereLightEnabled);
                //constants
                ConstantBuffer.Push(cmd, m_ComputeConstantBuffer, m_RenderCameraAerialPerspectiveVolumeCS, s_ShaderVariablesAtmosphereSkyCompute);
                cmd.SetComputeMatrixParam(m_RenderCameraAerialPerspectiveVolumeCS, HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
                cmd.SetComputeVectorParam(m_RenderCameraAerialPerspectiveVolumeCS, HDShaderIDs._WorldSpaceCameraPos1, builtinParams.worldSpaceCameraPos);
                cmd.SetComputeMatrixParam(m_RenderCameraAerialPerspectiveVolumeCS, HDShaderIDs._ViewMatrix1, builtinParams.viewMatrix);
                cmd.SetComputeFloatParam(m_RenderCameraAerialPerspectiveVolumeCS, s_AerialPerspectiveStartDepthKm, aerialPerspectiveStartDepthInM * k_MToKm);
                //textures
                cmd.SetComputeTextureParam(m_RenderCameraAerialPerspectiveVolumeCS, 0, s_TransmittanceLutTexture, m_TransmittanceLutTexture);
                cmd.SetComputeTextureParam(m_RenderCameraAerialPerspectiveVolumeCS, 0, s_MultiScatteredLuminanceLutTexture, m_MultiScatteredLuminanceLutTexture);
                //uavs
                cmd.SetComputeTextureParam(m_RenderCameraAerialPerspectiveVolumeCS, 0, s_CameraAerialPerspectiveVolumeUav, m_AtmosphereCameraAerialPerspectiveVolume);
                //dispatch
                cmd.DispatchCompute(m_RenderCameraAerialPerspectiveVolumeCS, 0,
                                    HDUtils.DivRoundUp(m_AtmosphereCameraAerialPerspectiveVolume.rt.width, renderTransmittanceLutGroupSize),
                                    HDUtils.DivRoundUp(m_AtmosphereCameraAerialPerspectiveVolume.rt.height, renderTransmittanceLutGroupSize),
                                    HDUtils.DivRoundUp(m_AtmosphereCameraAerialPerspectiveVolume.rt.volumeDepth, renderTransmittanceLutGroupSize));
            }
        }

        static readonly HDAdditionalLightData[] s_Lights = new HDAdditionalLightData[2];

        void UpdateGlobalConstantBuffer(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            var atmosphereSky = builtinParams.skySettings as AtmosphereSky;
            var camera = builtinParams.hdCamera.camera;
            var parameters = atmosphereSky.GetInternalParams();

            if (builtinParams.sunLight != null)
                builtinParams.sunLight.TryGetComponent(out s_Lights[0]);
            else
                s_Lights[0] = null;

            unsafe
            {
                for (int i = 0; i < s_Lights.Length; ++i)
                {
                    if (s_Lights[i] == null)
                        continue;

                    PrepareSunLight(s_Lights[i], ref parameters, out Vector3 transmittanceFactor, out Vector3 sunDiskOuterSpaceLuminance);

                    Vector4 direction = -s_Lights[i].transform.forward;
                    Vector4 color = s_Lights[i].color * s_Lights[i].intensity;
                    
                    if (s_Lights[i].useColorTemperature)
                    {
                        var cct = Mathf.CorrelatedColorTemperatureToRGB(s_Lights[i].legacyLight.colorTemperature);
                        color *= cct;
                    }
                    
                    color.w = 1.0f;
                    Vector4 colorGlobalPostTransmittance = Vector4.Scale(color, transmittanceFactor);
                    colorGlobalPostTransmittance.w = 1.0f;
                    Vector4 discLuminance = sunDiskOuterSpaceLuminance;
                    // float diskHalfApexAngleRadian = lights[i].GetSunLightHalfApexAngleRadian();
                    // Vector4 discCosHalfApexAngle = Vector4.one * Mathf.Cos(lights[i].sunDiskScale * diskHalfApexAngleRadian);
                    Vector4 discCosHalfApexAngle = Vector4.one * Mathf.Cos(s_Lights[i].sunLightConeAngle * Mathf.Deg2Rad);

                    for (int j = 0; j < 4; j++)
                    {
                        m_ConstantBuffer._AtmosphereLightDirection[i * 4 + j] = direction[j];
                        m_ConstantBuffer._AtmosphereLightColor[i * 4 + j] = color[j];
                        m_ConstantBuffer._AtmosphereLightColorGlobalPostTransmittance[i * 4 + j] = colorGlobalPostTransmittance[j];
                        m_ConstantBuffer._AtmosphereLightDiscLuminance[i * 4 + j] = discLuminance[j];
                        m_ConstantBuffer._AtmosphereLightDiscCosHalfApexAngle[i * 4 + j] = discCosHalfApexAngle[j];
                    }
                }
            }

            //Sky
            m_ConstantBuffer._SkyViewLutSizeAndInvSize = GetSizeAndInvSize(m_SkyViewLutWidth, m_SkyViewLutHeight);

            // The constants below should match the one in AtmosphereCommon.ush
            const float planetRadiusOffset = 0.01f; // Always force to be 10 meters above the ground/sea level (to always see the sky and not be under the virtual planet occluding ray tracing)
            const float offset = planetRadiusOffset * AtmosphereParameters.skyUnitToM;
            float bottomRadiusWorld = parameters.bottomRadiusKm * AtmosphereParameters.skyUnitToM;
            Vector3 planetCenterWorld = parameters.planetCenterKm * AtmosphereParameters.skyUnitToM;
            Vector3 worldCameraOrigin = camera.transform.position;
            Vector3 planetCenterToCameraWorld = worldCameraOrigin - planetCenterWorld;
            float distanceToPlanetCenterWorld = planetCenterToCameraWorld.magnitude;

            // If the camera is below the planet surface, we snap it back onto the surface.
            // This is to make sure the sky is always visible even if the camera is inside the virtual planet.
            var skyWorldOrigin = distanceToPlanetCenterWorld < (bottomRadiusWorld + offset)
                ? planetCenterWorld + (bottomRadiusWorld + offset) * (planetCenterToCameraWorld / distanceToPlanetCenterWorld)
                : worldCameraOrigin;
            m_ConstantBuffer._SkyWorldCameraOrigin = skyWorldOrigin;
            m_ConstantBuffer._SkyPlanetCenterAndViewHeight = planetCenterWorld;
            m_ConstantBuffer._SkyPlanetCenterAndViewHeight.w = (skyWorldOrigin - planetCenterWorld).magnitude;

            m_ConstantBuffer._AtmosphereSkyLuminanceFactor = atmosphereSky.skyLuminanceFactor.value;
            m_ConstantBuffer._AtmosphereHeightFogContribution = atmosphereSky.heightFogContribution.value;
            m_ConstantBuffer._AtmosphereBottomRadiusKm = parameters.bottomRadiusKm;
            m_ConstantBuffer._AtmosphereTopRadiusKm = parameters.topRadiusKm;

            float aerialPerspectiveStartDepthInM = GetValidAerialPerspectiveStartDepthInM(camera);
            m_ConstantBuffer._AtmosphereAerialPerspectiveStartDepthKm = aerialPerspectiveStartDepthInM * k_MToKm;
            m_ConstantBuffer._AtmosphereCameraAerialPerspectiveVolumeDepthResolution = m_CameraAerialPerspectiveVolumeDepthResolution;
            m_ConstantBuffer._AtmosphereCameraAerialPerspectiveVolumeDepthResolutionInv = 1.0f / m_CameraAerialPerspectiveVolumeDepthResolution;
            m_ConstantBuffer._AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKm = m_CameraAerialPerspectiveVolumeDepthSliceLengthKm;
            m_ConstantBuffer._AtmosphereCameraAerialPerspectiveVolumeDepthSliceLengthKmInv = 1.0f / m_CameraAerialPerspectiveVolumeDepthResolution;
            m_ConstantBuffer._AtmosphereApplyCameraAerialPerspectiveVolume = m_AtmosphereCameraAerialPerspectiveVolume.rt == null ? 0.0f : 1.0f;

            m_ConstantBuffer._MultiScatteringFactor = parameters.multiScatteringFactor;
            m_ConstantBuffer._TopRadiusKm = parameters.topRadiusKm;
            m_ConstantBuffer._BottomRadiusKm = parameters.bottomRadiusKm;
            m_ConstantBuffer._RayleighScattering = parameters.rayleighScattering;
            m_ConstantBuffer._RayleighDensityExpScale = parameters.rayleighDensityExpScale;
            m_ConstantBuffer._MieScattering = parameters.mieScattering;
            m_ConstantBuffer._MieDensityExpScale = parameters.mieDensityExpScale;
            m_ConstantBuffer._MieExtinction = parameters.mieExtinction;
            m_ConstantBuffer._MiePhaseG = parameters.miePhaseG;
            m_ConstantBuffer._MieAbsorption = parameters.mieAbsorption;
            m_ConstantBuffer._AbsorptionDensity0ConstantTerm = parameters.absorptionDensity0ConstantTerm;
            m_ConstantBuffer._AbsorptionDensity0LayerWidth = parameters.absorptionDensity0LayerWidth;
            m_ConstantBuffer._AbsorptionDensity0LinearTerm = parameters.absorptionDensity0LinearTerm;
            m_ConstantBuffer._AbsorptionDensity1ConstantTerm = parameters.absorptionDensity1ConstantTerm;
            m_ConstantBuffer._AbsorptionDensity1LinearTerm = parameters.absorptionDensity1LinearTerm;
            m_ConstantBuffer._AbsorptionExtinction = parameters.absorptionExtinction;
            m_ConstantBuffer._GroundAlbedo1 = parameters.groundAlbedo;

            ConstantBuffer.PushGlobal(cmd, m_ConstantBuffer, s_ShaderVariablesAtmosphereSky);
        }

        void UpdateInternalConstantBuffer(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            var atmosphereSky = builtinParams.skySettings as AtmosphereSky;
            Debug.Assert(atmosphereSky);

            m_ComputeConstantBuffer._TransmittanceLutSizeAndInvSize = GetSizeAndInvSize(m_TransmittanceLutWidth, m_TransmittanceLutHeight);
            m_ComputeConstantBuffer._MultiScatteredLuminanceLutSizeAndInvSize = GetSizeAndInvSize(m_MultiScatteredLuminanceLutWidth, m_MultiScatteredLuminanceLutHeight);

            m_ComputeConstantBuffer._SampleCountMin = m_Settings.sampleCountMin;
            m_ComputeConstantBuffer._SampleCountMax = m_Settings.sampleCountMax;
            float distanceToSampleCountMaxInv = m_Settings.distanceToSampleCountMax;

            m_ComputeConstantBuffer._FastSkySampleCountMin = m_Settings.fastSkyLUTSampleCountMin;
            m_ComputeConstantBuffer._FastSkySampleCountMax = m_Settings.fastSkyLUTSampleCountMax;
            float fastSkyDistanceToSampleCountMaxInv = m_Settings.fastSkyLUTDistanceToSampleCountMax;

            m_ComputeConstantBuffer._CameraAerialPerspectiveSampleCountPerSlice = m_Settings.aerialPerspectiveLUTSampleCountPerSlice;

            m_ComputeConstantBuffer._TransmittanceSampleCount = m_Settings.transmittanceLUTSampleCount;
            m_ComputeConstantBuffer._MultiScatteringSampleCount = m_Settings.multiScatteringLUTSampleCount;

            m_ComputeConstantBuffer._SkyLuminanceFactor = (Vector4) atmosphereSky.skyLuminanceFactor.value;
            m_ComputeConstantBuffer._AerialPespectiveViewDistanceScale = atmosphereSky.aerialPespectiveViewDistanceScale.value;

            ValidateSampleCountValue(ref m_ComputeConstantBuffer._SampleCountMin);
            ValidateMaxSampleCountValue(ref m_ComputeConstantBuffer._SampleCountMax, m_ComputeConstantBuffer._SampleCountMin);
            ValidateSampleCountValue(ref m_ComputeConstantBuffer._FastSkySampleCountMin);
            ValidateMaxSampleCountValue(ref m_ComputeConstantBuffer._FastSkySampleCountMax, m_ComputeConstantBuffer._FastSkySampleCountMin);
            ValidateSampleCountValue(ref m_ComputeConstantBuffer._CameraAerialPerspectiveSampleCountPerSlice);
            ValidateSampleCountValue(ref m_ComputeConstantBuffer._TransmittanceSampleCount);
            ValidateSampleCountValue(ref m_ComputeConstantBuffer._MultiScatteringSampleCount);
            ValidateDistanceValue(ref distanceToSampleCountMaxInv);
            ValidateDistanceValue(ref fastSkyDistanceToSampleCountMaxInv);

            // Derived values post validation
            m_ComputeConstantBuffer._DistanceToSampleCountMaxInv = 1.0f / distanceToSampleCountMaxInv;
            m_ComputeConstantBuffer._FastSkyDistanceToSampleCountMaxInv = 1.0f / fastSkyDistanceToSampleCountMaxInv;
            m_ComputeConstantBuffer._CameraAerialPerspectiveVolumeSizeAndInvSize = GetSizeAndInvSize(m_CameraAerialPerspectiveVolumeScreenResolution, m_CameraAerialPerspectiveVolumeScreenResolution);

            //Other
            m_ComputeConstantBuffer._StateFrameIndexMod8 = builtinParams.frameIndex % 8;
            // m_ComputeConstantBuffer._ViewSizeAndInvSize = builtinParams.screenSize;
        }

        float GetValidAerialPerspectiveStartDepthInM(Camera camera)
        {
            float aerialPerspectiveStartDepthKm = m_Settings.aerialPerspectiveStartDepth;
            aerialPerspectiveStartDepthKm = aerialPerspectiveStartDepthKm < 0.0f ? 0.0f : aerialPerspectiveStartDepthKm;
            // For sky reflection capture, the start depth can be super large. So we max it to make sure the triangle is never in front the NearClippingDistance.
            float startDepthInM = Mathf.Max(aerialPerspectiveStartDepthKm * k_KmToM, camera.nearClipPlane);
            return startDepthInM;
        }

        GraphicsFormat GetSkyLutTextureFormat(GraphicsDeviceType featureLevel)
        {
            GraphicsFormat textureLutFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            if (!SystemInfo.IsFormatSupported(textureLutFormat, FormatUsage.Render))
            {
                // OpenGL ES3.1 does not support storing into 3-component images
                // TODO: check if need this for Metal, Vulkan
                textureLutFormat = GraphicsFormat.R16G16B16A16_SFloat;
            }

            if (m_Settings.useLUT32)
            {
                textureLutFormat = GraphicsFormat.R32G32B32A32_SFloat;
            }

            return textureLutFormat;
        }

        GraphicsFormat GetSkyLutSmallTextureFormat()
        {
            if (m_Settings.useLUT32)
            {
                return GraphicsFormat.R32G32B32A32_SFloat;
            }

            GraphicsFormat textureLutFormat = GraphicsFormat.R8G8B8_UNorm;
            return GraphicsFormat.R8G8B8_UNorm;
        }

        void PrepareSunLight(HDAdditionalLightData light, ref AtmosphereParameters parameters, out Vector3 transmittanceFactor, out Vector3 sunDiskOuterSpaceLuminance)
        {
            // See explanation in https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/s2016-pbs-frostbite-sky-clouds-new.pdf page 26
            Vector3 atmosphereLightDirection = -light.transform.forward;
            Vector3 transmittanceTowardSun = parameters.GetTransmittanceAtGroundLevel(atmosphereLightDirection);
            Vector3 transmittanceAtZenithFinal = parameters.GetTransmittanceAtGroundLevel(new Vector3(0.0f, 1.0f, 0.0f));
            transmittanceFactor = m_Settings.transmittanceLUTLightPerPixelTransmittance ? Vector3.one : VectorDivide(transmittanceTowardSun, transmittanceAtZenithFinal);

            Vector3 sunZenithIlluminance = (Vector4) light.color;
            Vector3 sunOuterSpaceIlluminance = VectorDivide(sunZenithIlluminance, transmittanceAtZenithFinal);
            sunDiskOuterSpaceLuminance = GetLightDiskLuminance(light, sunOuterSpaceIlluminance);
        }

        static Vector3 VectorDivide(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        static Vector3 GetLightDiskLuminance(HDAdditionalLightData lightData, Vector3 lightIlluminance)
        {
            float sunSolidAngle = 2.0f * Mathf.PI * (1.0f - Mathf.Cos(GetSunLightHalfApexAngleRadian(lightData))); // Solid angle from aperture https://en.wikipedia.org/wiki/Solid_angle
            return lightIlluminance / sunSolidAngle;                                                               // approximation
        }

        static float GetSunLightHalfApexAngleRadian(HDAdditionalLightData lightData)
        {
            return 0.5f * lightData.sunLightConeAngle * Mathf.Deg2Rad; // LightSourceAngle is apex angle (angular diameter) in degree
        }

        static float GetSunOnEarthHalfApexAngleRadian()
        {
            const float sunOnEarthApexAngleDegree = 0.545f; // Apex angle == angular diameter
            return 0.5f * sunOnEarthApexAngleDegree * Mathf.PI / 180.0f;
        }

        static int ValidateLutResolution(float value)
        {
            return (int) (value < 4 ? 4 : value);
        }

        static Vector4 GetSizeAndInvSize(int width, int height)
        {
            float fWidth = width;
            float fHeight = height;
            return new Vector4(fWidth, fHeight, 1.0f / fWidth, 1.0f / fHeight);
        }

        static void ValidateDistanceValue(ref float value)
        {
            const float kindaSmallNumber = 1e-4f;
            value = value < kindaSmallNumber ? kindaSmallNumber : value;
        }

        static void ValidateSampleCountValue(ref float value)
        {
            value = value < 1.0f ? 1.0f : value;
        }

        static void ValidateMaxSampleCountValue(ref float value, float minValue)
        {
            value = value < minValue ? minValue : value;
        }
    }
}
