using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// First-person camera: mouse look plus all camera feel effects (speed FOV, slide/wallrun tilt,
    /// landing dip, head bob, recoil, camera shake, zoom). Effects respect the comfort settings.
    /// Hierarchy: Player > CameraRig (yaw) > CameraPivot (pitch/roll/offsets) > Main Camera.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] CameraFeelSettings feel;
        [SerializeField] PlayerMovement movement;
        [SerializeField] Transform cameraRig;
        [SerializeField] Transform cameraPivot;
        [SerializeField] Camera mainCamera;
        [SerializeField] Camera viewModelCamera;
        [SerializeField] CameraShaker shaker;

        float yaw;
        float pitch;
        float roll;
        float eyeY;
        float landOffset;
        float landVelocity;
        float bobPhase;
        float bobIntensity;
        float currentFov;
        float zoomMultiplier = 1f;
        float zoomTarget = 1f;
        Vector2 recoilTarget;
        Vector2 recoilCurrent;

        public float Yaw => yaw;
        public float Pitch => pitch;
        public Camera Camera => mainCamera;
        public Camera ViewModelCamera => viewModelCamera;
        public Transform CameraTransform => mainCamera != null ? mainCamera.transform : transform;
        public bool LookEnabled { get; set; } = true;

        void Awake()
        {
            if (feel == null)
            {
                feel = GameConfig.Instance.defaultCameraSettings;
                if (feel == null) feel = ScriptableObject.CreateInstance<CameraFeelSettings>();
            }
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (mainCamera == null) mainCamera = GetComponentInChildren<Camera>();
            if (cameraRig == null) cameraRig = transform;
            if (cameraPivot == null && mainCamera != null) cameraPivot = mainCamera.transform.parent;
            if (shaker == null) shaker = GetComponentInParent<CameraShaker>();
            yaw = cameraRig.eulerAngles.y;
            currentFov = SettingsManager.Settings.fieldOfView;
        }

        void Start()
        {
            if (movement != null) eyeY = movement.EyeHeightLocal;
            ApplyTransforms();
        }

        void OnEnable()
        {
            if (movement != null) movement.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (movement != null) movement.Landed -= OnLanded;
        }

        void OnLanded(float impactSpeed)
        {
            if (!SettingsManager.Settings.motionEffects) return;
            if (impactSpeed < feel.minLandingSpeed) return;
            landVelocity -= Mathf.Min(impactSpeed * feel.landingImpulsePerSpeed, feel.maxLandingImpulse);
            if (shaker != null && impactSpeed > 18f) shaker.AddTrauma(Mathf.Clamp01((impactSpeed - 18f) / 30f) * 0.4f);
        }

        void Update()
        {
            var settings = SettingsManager.Settings;
            var input = movement != null ? movement.InputHandler : null;
            if (LookEnabled && input != null)
            {
                Vector2 delta = input.LookDelta * settings.mouseSensitivity * feel.sensitivityScale;
                yaw += delta.x;
                pitch += settings.invertY ? delta.y : -delta.y;
                pitch = Mathf.Clamp(pitch, -feel.pitchLimit, feel.pitchLimit);
                if (yaw > 360f || yaw < -360f) yaw = Mathf.Repeat(yaw, 360f);
            }
            if (cameraRig != null) cameraRig.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void LateUpdate()
        {
            ApplyTransforms();
        }

        void ApplyTransforms()
        {
            if (cameraPivot == null || movement == null) return;
            float dt = Time.deltaTime;
            var settings = SettingsManager.Settings;
            bool motion = settings.motionEffects;

            // Eye height (crouch/slide)
            eyeY = Mathf.Lerp(eyeY, movement.EyeHeightLocal, MathUtil.Damp(feel.eyeHeightSharpness, dt));

            // Landing spring
            float springAccel = -feel.landingSpringStiffness * landOffset - feel.landingSpringDamping * landVelocity;
            landVelocity += springAccel * dt;
            landOffset += landVelocity * dt;
            landOffset = Mathf.Clamp(landOffset, -0.6f, 0.3f);

            // Head bob
            float hSpeed = movement.HorizontalSpeed;
            bool bobbing = settings.headBob && movement.IsGrounded && !movement.IsSliding && hSpeed > 1f;
            float targetIntensity = bobbing ? Mathf.Clamp01(hSpeed / Mathf.Max(1f, movement.Settings.maxGroundSpeed)) : 0f;
            bobIntensity = Mathf.Lerp(bobIntensity, targetIntensity, MathUtil.Damp(10f, dt));
            if (bobbing) bobPhase += hSpeed * feel.bobFrequency * dt;
            Vector3 bob = new Vector3(
                Mathf.Cos(bobPhase * Mathf.PI) * feel.bobHorizontalAmplitude,
                Mathf.Sin(bobPhase * Mathf.PI * 2f) * feel.bobAmplitude,
                0f) * bobIntensity;

            // Tilt
            float targetRoll = 0f;
            if (motion)
            {
                var wall = movement.WallRun;
                if (wall != null && wall.IsWallRunning) targetRoll = wall.WallSide * feel.wallRunTilt;
                else if (movement.IsSliding) targetRoll = feel.slideTilt;
                else if (movement.InputHandler != null) targetRoll = -movement.InputHandler.Move.x * feel.strafeTilt;
            }
            roll = Mathf.Lerp(roll, targetRoll, MathUtil.Damp(feel.tiltSharpness, dt));

            // Recoil
            recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, MathUtil.Damp(feel.recoilRecovery, dt));
            recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, MathUtil.Damp(feel.recoilSnappiness, dt));

            Vector3 shakePos = shaker != null ? shaker.PositionOffset : Vector3.zero;
            Vector3 shakeRot = shaker != null ? shaker.RotationOffset : Vector3.zero;

            cameraPivot.localPosition = new Vector3(bob.x, eyeY + bob.y + (motion ? landOffset : 0f), 0f) + shakePos;
            cameraPivot.localRotation = Quaternion.Euler(pitch - recoilCurrent.x + shakeRot.x, recoilCurrent.y + shakeRot.y, roll + shakeRot.z);

            // FOV
            float baseFov = settings.fieldOfView;
            float extra = 0f;
            if (motion)
            {
                float speedK = Mathf.InverseLerp(feel.fovSpeedStart, feel.fovSpeedMax, movement.Speed);
                extra = feel.maxExtraFov * speedK + (movement.IsSliding ? feel.slideFovBoost : 0f);
            }
            zoomMultiplier = Mathf.Lerp(zoomMultiplier, zoomTarget, MathUtil.Damp(14f, dt));
            float targetFov = (baseFov + extra) * zoomMultiplier;
            currentFov = Mathf.Lerp(currentFov, targetFov, MathUtil.Damp(feel.fovLerpSpeed, dt));
            if (mainCamera != null) mainCamera.fieldOfView = currentFov;
            if (viewModelCamera != null) viewModelCamera.fieldOfView = feel.viewModelFov;
        }

        // ------------------------------------------------------------------ API

        public void SetRotation(float newYaw, float newPitch)
        {
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, -feel.pitchLimit, feel.pitchLimit);
            if (cameraRig != null) cameraRig.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Kicks the view up (pitch, degrees) and sideways (yaw, degrees).</summary>
        public void AddRecoil(float pitchKick, float yawKick)
        {
            recoilTarget += new Vector2(pitchKick, yawKick);
            recoilTarget.x = Mathf.Clamp(recoilTarget.x, -30f, 30f);
        }

        /// <summary>Zoom multiplier for the FOV (1 = none, 0.5 = 2x zoom).</summary>
        public void SetZoom(float multiplier)
        {
            zoomTarget = Mathf.Clamp(multiplier, 0.2f, 1f);
        }

        public void PunchLanding(float impulse)
        {
            landVelocity -= impulse;
        }

        public void ResetEffects()
        {
            roll = 0f;
            landOffset = 0f;
            landVelocity = 0f;
            recoilCurrent = Vector2.zero;
            recoilTarget = Vector2.zero;
            zoomTarget = 1f;
            zoomMultiplier = 1f;
            bobIntensity = 0f;
            if (shaker != null) shaker.Clear();
            if (movement != null) eyeY = movement.EyeHeightLocal;
        }
    }
}
