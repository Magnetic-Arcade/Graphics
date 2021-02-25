namespace UnityEngine.Rendering.HighDefinition
{
    struct AtmosphereSkyRenderData
    {
        public Vector3 planetCenterKm; // In sky unit (kilometers)
        public float bottomRadiusKm; // idem
        public float topRadiusKm; // idem

        public float multiScatteringFactor;

        public Vector3 rayleighScattering; // Unit is 1/km
        public float rayleighDensityExpScale;

        public Vector3 mieScattering; // Unit is 1/km
        public Vector3 mieExtinction; // idem
        public Vector3 mieAbsorption; // idem
        public float mieDensityExpScale;
        public float miePhaseG;

        public Vector3 absorptionExtinction;
        public float absorptionDensity0LayerWidth;
        public float absorptionDensity0ConstantTerm;
        public float absorptionDensity0LinearTerm;
        public float absorptionDensity1ConstantTerm;
        public float absorptionDensity1LinearTerm;

        public Vector3 groundAlbedo;
        public float transmittanceMinLightElevationAngle;

        public const float meterToSkyUnit = 0.001f; // Meters to Kilometers
        public const float skyUnitToMeter = 1.0f / meterToSkyUnit; // Kilometers to Meters

        public void Setup(AtmosphereSky atmosphereSky)
        {
            bottomRadiusKm = atmosphereSky.bottomRadius.value;
            topRadiusKm = atmosphereSky.bottomRadius.value + atmosphereSky.atmosphereHeight.value;
            groundAlbedo = ColorToVector(atmosphereSky.groundAlbedo.value);
            multiScatteringFactor = atmosphereSky.multiScatteringFactor.value;

            rayleighDensityExpScale = -1.0f / atmosphereSky.rayleighExponentialDistribution.value;
            rayleighScattering = Clamp(ColorToVector(atmosphereSky.rayleighScattering.value) * atmosphereSky.rayleighScatteringScale.value, 0.0f, 1e38f);

            mieScattering = Clamp(ColorToVector(atmosphereSky.mieScattering.value) * atmosphereSky.mieScatteringScale.value, 0.0f, 1e38f);
            mieAbsorption = Clamp(ColorToVector(atmosphereSky.mieAbsorption.value) * atmosphereSky.mieAbsorptionScale.value, 0.0f, 1e38f);
            mieExtinction = mieScattering + mieAbsorption;
            miePhaseG = atmosphereSky.mieAnisotropy.value;
            mieDensityExpScale = -1.0f / atmosphereSky.mieExponentialDistribution.value;

            absorptionExtinction = Clamp(ColorToVector(atmosphereSky.otherAbsorption.value) * atmosphereSky.otherAbsorptionScale.value, 0.0f, 1e38f);
            TentToCoefficients(atmosphereSky.otherTentDistribution.value,
                               out absorptionDensity0LayerWidth,
                               out absorptionDensity0LinearTerm,
                               out absorptionDensity1LinearTerm,
                               out absorptionDensity0ConstantTerm,
                               out absorptionDensity1ConstantTerm);

            transmittanceMinLightElevationAngle = atmosphereSky.transmittanceMinLightElevationAngle.value;
            planetCenterKm = new Vector3(0.0f, -bottomRadiusKm, 0.0f);
        }

        public void UpdateTransform(Vector3 position, AtmosphereTransformMode tranformMode)
        {
            switch (tranformMode)
            {
                case AtmosphereTransformMode.PlanetTopAtAbsoluteWorldOrigin:
                    planetCenterKm = new Vector3(0.0f, -bottomRadiusKm, 0.0f);
                    break;
                case AtmosphereTransformMode.PlanetTopAtComponentTransform:
                    planetCenterKm = new Vector3(0.0f, -bottomRadiusKm, 0.0f) + position * meterToSkyUnit;
                    break;
                case AtmosphereTransformMode.PlanetCenterAtComponentTransform:
                    planetCenterKm = position * meterToSkyUnit;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        public Vector3 GetTransmittanceAtGroundLevel(Vector3 sunDirection)
        {
            // Assuming camera is along Y on (0, earthRadius + 500m, 0)
            Vector3 worldPos = new Vector3(0.0f, bottomRadiusKm + 0.5f, 0.0f);
            Vector2 azimuthElevation = GetAzimuthAndElevation(sunDirection, Vector3.forward, Vector3.left, Vector3.up); // TODO: make it work over the entire virtual planet with a local basis
            azimuthElevation.y = Mathf.Max(transmittanceMinLightElevationAngle * Mathf.Deg2Rad, azimuthElevation.y);
            Vector3 worldDir = new Vector3(Mathf.Cos(azimuthElevation.y), Mathf.Sin(azimuthElevation.y), 0.0f); // no need to take azimuth into account as transmittance is symmetrical around zenith axis.
            Vector3 opticalDepthRGB = OpticalDepth(worldPos, worldDir);
            return new Vector3(Mathf.Exp(-opticalDepthRGB.x), Mathf.Exp(-opticalDepthRGB.y), Mathf.Exp(-opticalDepthRGB.z));
        }

        public void ComputeViewData(Vector3 worldCameraOrigin, 
                                    Vector3 viewForward, 
                                    Vector3 viewRight, 
                                    out Vector3 skyWorldCameraOrigin, 
                                    out Vector4 skyPlanetCenterAndViewHeight, 
                                    out Matrix4x4 skyViewLutReferential)
        {
	        // The constants below should match the one in SkyAtmosphereCommon.ush
	        // Always force to be 1 meters above the ground/sea level (to always see the sky and not be under the virtual planet occluding ray tracing) and lower for small planet radius
	        const float planetRadiusOffset = 0.001f;
            const float offset = planetRadiusOffset * skyUnitToMeter;
            float bottomRadiusWorld = bottomRadiusKm * skyUnitToMeter;
            Vector3 planetCenterWorld = planetCenterKm * skyUnitToMeter;
            Vector3 planetCenterToCameraWorld = worldCameraOrigin - planetCenterWorld;
            float distanceToPlanetCenterWorld = planetCenterToCameraWorld.magnitude;

	        // If the camera is below the planet surface, we snap it back onto the surface.
	        // This is to make sure the sky is always visible even if the camera is inside the virtual planet.
	        skyWorldCameraOrigin = distanceToPlanetCenterWorld < (bottomRadiusWorld + offset) ? planetCenterWorld + (bottomRadiusWorld + offset) * (planetCenterToCameraWorld / distanceToPlanetCenterWorld) : worldCameraOrigin;
	        skyPlanetCenterAndViewHeight = new Vector4(planetCenterWorld.x, planetCenterWorld.y, planetCenterWorld.z, (skyWorldCameraOrigin - planetCenterWorld).magnitude);

	        // Now compute the referential for the SkyView LUT
            Vector3 planetCenterToWorldCameraPos = (skyWorldCameraOrigin - planetCenterWorld) * meterToSkyUnit;
            Vector3 up = planetCenterToWorldCameraPos;
	        up.Normalize();
            Vector3	forward = viewForward;		// This can make texel visible when the camera is rotating. Use constant worl direction instead?
	        //FVector	Left = normalize(cross(Forward, Up)); 
            Vector3 left = Vector3.Cross(forward, up);
	        left.Normalize();
	        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f)
	        {
		        left = -viewRight;
	        }
	        forward = Vector3.Cross(up, left);
	        forward.Normalize();
            
            skyViewLutReferential = Matrix4x4.identity;
	        skyViewLutReferential.SetColumn(0, forward);
	        skyViewLutReferential.SetColumn(1, up);
	        skyViewLutReferential.SetColumn(2, left);
            skyViewLutReferential = skyViewLutReferential.transpose;
        }

        static Vector3 Clamp(Vector3 v, float min, float max)
        {
            return new Vector3(Mathf.Clamp(v.x, min, max),
                               Mathf.Clamp(v.y, min, max),
                               Mathf.Clamp(v.z, min, max));
        }

        static Vector3 ColorToVector(Color c)
        {
            return (Vector4) c;
        }

        // Convert Tent distribution to linear curve coefficients.
        static void TentToCoefficients(TentDistribution tent, out float layerWidth, out float linTerm0, out float linTerm1, out float constTerm0, out float constTerm1)
        {
            if (tent.width.value > 0.0f && tent.tipValue.value > 0.0f)
            {
                float px = tent.tipAltitude.value;
                float py = tent.tipValue.value;
                float slope = tent.tipValue.value / tent.width.value;
                layerWidth = px;
                linTerm0 = slope;
                linTerm1 = -slope;
                constTerm0 = py - px * linTerm0;
                constTerm1 = py - px * linTerm1;
            }
            else
            {
                layerWidth = 0.0f;
                linTerm0 = 0.0f;
                linTerm1 = 0.0f;
                constTerm0 = 0.0f;
                constTerm1 = 0.0f;
            }
        }

        static Vector2 GetAzimuthAndElevation(Vector3 direction, Vector3 axisX, Vector3 axisY, Vector3 axisZ)
        {
            Vector3 normalDir = direction.normalized;
            // Find projected point (on AxisX and AxisY, remove AxisZ component)
            Vector3 noZProjDir = (normalDir - Vector3.Dot(normalDir, axisZ) * axisZ).normalized;
            // Figure out if projection is on right or left.
            float azimuthSign = (Vector3.Dot(noZProjDir, axisY) < 0.0f) ? -1.0f : 1.0f;
            float elevationSin = Vector3.Dot(normalDir, axisZ);
            float azimuthCos = Vector3.Dot(noZProjDir, axisX);
            // Convert to Angles in Radian.
            return new Vector2(Mathf.Acos(azimuthCos) * azimuthSign, Mathf.Asin(elevationSin));
        }

        // The following code is from Atmosphere.hlsl
        // It compute transmittance from the origin towards a sun direction.
        static Vector2 RayIntersectSphere(Vector3 rayOrigin, Vector3 rayDirection, Vector3 sphereOrigin, float sphereRadius)
        {
            Vector3 localPosition = rayOrigin - sphereOrigin;
            float localPositionSqr = Vector3.Dot(localPosition, localPosition);

            Vector3 quadraticCoef;
            quadraticCoef.x = Vector3.Dot(rayDirection, rayDirection);
            quadraticCoef.y = 2.0f * Vector3.Dot(rayDirection, localPosition);
            quadraticCoef.z = localPositionSqr - sphereRadius * sphereRadius;

            float discriminant = quadraticCoef.y * quadraticCoef.y - 4.0f * quadraticCoef.x * quadraticCoef.z;

            // Only continue if the ray intersects the sphere
            Vector2 intersections = new Vector2 {x = -1.0f, y = -1.0f};
            if (discriminant >= 0)
            {
                float sqrtDiscriminant = Mathf.Sqrt(discriminant);
                intersections.x = (-quadraticCoef.y - 1.0f * sqrtDiscriminant) / (2 * quadraticCoef.x);
                intersections.y = (-quadraticCoef.y + 1.0f * sqrtDiscriminant) / (2 * quadraticCoef.x);
            }

            return intersections;
        }

        // Nearest intersection of ray r,mu with sphere boundary
        static float RaySphereIntersectNearest(Vector3 rayOrigin, Vector3 rayDirection, Vector3 sphereOrigin, float sphereRadius)
        {
            Vector2 sol = RayIntersectSphere(rayOrigin, rayDirection, sphereOrigin, sphereRadius);
            float sol0 = sol.x;
            float sol1 = sol.y;

            if (sol0 < 0.0f && sol1 < 0.0f)
            {
                return -1.0f;
            }

            if (sol0 < 0.0f)
            {
                return Mathf.Max(0.0f, sol1);
            }
            else if (sol1 < 0.0f)
            {
                return Mathf.Max(0.0f, sol0);
            }

            return Mathf.Max(0.0f, Mathf.Min(sol0, sol1));
        }

        Vector3 OpticalDepth(Vector3 rayOrigin, Vector3 rayDirection)
        {
            float max = RaySphereIntersectNearest(rayOrigin, rayDirection, new Vector3(0.0f, 0.0f, 0.0f), topRadiusKm);
            Vector3 opticalDepthRGB = Vector3.zero;

            if (max > 0.0f)
            {
                const float sampleCount = 15.0f;
                const float sampleStep = 1.0f / sampleCount;
                float sampleLength = sampleStep * max;

                for (float sampleT = 0.0f; sampleT < 1.0f; sampleT += sampleStep)
                {
                    Vector3 pos = rayOrigin + rayDirection * (max * sampleT);
                    float viewHeight = Vector3.Distance(pos, Vector3.zero) - bottomRadiusKm;
                    float densityMie = Mathf.Max(0.0f, Mathf.Exp(mieDensityExpScale * viewHeight));
                    float densityRay = Mathf.Max(0.0f, Mathf.Exp(rayleighDensityExpScale * viewHeight));
                    float densityOzo = Mathf.Clamp(viewHeight < absorptionDensity0LayerWidth
                                                       ? absorptionDensity0LinearTerm * viewHeight + absorptionDensity0ConstantTerm
                                                       : absorptionDensity1LinearTerm * viewHeight + absorptionDensity1ConstantTerm,
                                                   0.0f, 1.0f);

                    var sampleExtinction = densityMie * mieExtinction + densityRay * rayleighScattering + densityOzo * absorptionExtinction;
                    opticalDepthRGB += sampleLength * sampleExtinction;
                }
            }

            return opticalDepthRGB;
        }
    }
}
