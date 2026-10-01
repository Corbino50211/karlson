using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>Camera feel tuning: FOV kick, tilts, landing dip, head bob and recoil recovery.</summary>
    [CreateAssetMenu(menuName = "Momentum/Camera Feel Settings", fileName = "CameraFeelSettings")]
    public class CameraFeelSettings : ScriptableObject
    {
        [Header("Look")]
        public float pitchLimit = 89f;
        [Tooltip("Degrees per mouse unit at sensitivity 1.")]
        public float sensitivityScale = 1f;

        [Header("Field of View")]
        public float viewModelFov = 62f;
        [Tooltip("Speed at which the FOV starts widening (m/s).")]
        public float fovSpeedStart = 12f;
        [Tooltip("Speed at which the FOV kick reaches its maximum (m/s).")]
        public float fovSpeedMax = 32f;
        public float maxExtraFov = 14f;
        public float slideFovBoost = 4f;
        public float fovLerpSpeed = 7f;

        [Header("Tilt")]
        public float slideTilt = 5f;
        public float wallRunTilt = 14f;
        public float strafeTilt = 1.2f;
        public float tiltSharpness = 9f;

        [Header("Landing")]
        public float landingImpulsePerSpeed = 0.11f;
        public float maxLandingImpulse = 3.5f;
        public float landingSpringStiffness = 140f;
        public float landingSpringDamping = 15f;
        public float minLandingSpeed = 4f;

        [Header("Head Bob")]
        [Tooltip("Bob cycles per meter travelled.")]
        public float bobFrequency = 0.32f;
        public float bobAmplitude = 0.045f;
        public float bobHorizontalAmplitude = 0.03f;

        [Header("Crouch")]
        public float eyeHeightSharpness = 14f;

        [Header("Recoil")]
        public float recoilSnappiness = 24f;
        public float recoilRecovery = 9f;
    }
}
