using Momentum.PlayerSystems;
using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum.Weapons
{
    /// <summary>
    /// Procedural view-model animation on the weapon holder: mouse sway, movement bob, recoil kick,
    /// equip/reload poses, slide/wallrun tilt and aim-down-sights offset.
    /// </summary>
    public class WeaponSway : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;

        [Header("Sway")]
        [SerializeField] float swayPosition = 0.012f;
        [SerializeField] float swayPositionMax = 0.05f;
        [SerializeField] float swayRotation = 2.2f;
        [SerializeField] float swayRotationMax = 7f;
        [SerializeField] float swaySharpness = 10f;

        [Header("Bob")]
        [SerializeField] float bobAmount = 0.022f;
        [SerializeField] float bobFrequency = 0.33f;

        [Header("Kick")]
        [SerializeField] float kickRecovery = 12f;

        [Header("Poses")]
        [SerializeField] Vector3 aimOffset = new Vector3(-0.22f, 0.06f, -0.05f);
        [SerializeField] Vector3 reloadOffset = new Vector3(0f, -0.08f, 0f);
        [SerializeField] Vector3 reloadRotation = new Vector3(25f, 0f, -18f);
        [SerializeField] float equipDuration = 0.25f;

        Vector3 basePosition;
        Vector3 swayPos;
        Vector3 swayRot;
        Vector3 kickPos;
        Vector3 kickRot;
        Vector3 posePos;
        Vector3 poseRot;
        float bobPhase;
        float bobWeight;
        float equipTimer = 1f;
        bool reloading;
        bool aiming;
        float aimWeight;

        void Awake()
        {
            basePosition = transform.localPosition;
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
        }

        public void AddKick(Vector3 position, Vector3 rotation)
        {
            kickPos += position;
            kickRot += rotation;
            kickPos = Vector3.ClampMagnitude(kickPos, 0.25f);
            kickRot = Vector3.ClampMagnitude(kickRot, 35f);
        }

        public void PlayEquip()
        {
            equipTimer = 0f;
        }

        public void SetReloading(bool value)
        {
            reloading = value;
        }

        public void SetAiming(bool value)
        {
            aiming = value;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            var settings = SettingsManager.Settings;
            float motionScale = settings.motionEffects ? 1f : 0.35f;

            // Mouse sway
            Vector2 look = movement != null && movement.InputHandler != null ? movement.InputHandler.LookDelta : Vector2.zero;
            float aimScale = aiming ? 0.3f : 1f;
            Vector3 targetSwayPos = new Vector3(
                Mathf.Clamp(-look.x * swayPosition, -swayPositionMax, swayPositionMax),
                Mathf.Clamp(-look.y * swayPosition, -swayPositionMax, swayPositionMax), 0f) * aimScale * motionScale;
            Vector3 targetSwayRot = new Vector3(
                Mathf.Clamp(look.y * swayRotation, -swayRotationMax, swayRotationMax),
                Mathf.Clamp(-look.x * swayRotation, -swayRotationMax, swayRotationMax),
                Mathf.Clamp(-look.x * swayRotation, -swayRotationMax, swayRotationMax)) * aimScale * motionScale;
            float s = MathUtil.Damp(swaySharpness, dt);
            swayPos = Vector3.Lerp(swayPos, targetSwayPos, s);
            swayRot = Vector3.Lerp(swayRot, targetSwayRot, s);

            // Bob
            Vector3 bob = Vector3.zero;
            if (movement != null)
            {
                float speed = movement.HorizontalSpeed;
                bool walking = movement.IsGrounded && !movement.IsSliding && speed > 1f;
                bool wallrun = movement.IsWallRunning;
                float targetWeight = settings.weaponBob && (walking || wallrun) ? Mathf.Clamp01(speed / 11f) : 0f;
                bobWeight = Mathf.Lerp(bobWeight, targetWeight * (aiming ? 0.25f : 1f), MathUtil.Damp(8f, dt));
                if (walking || wallrun) bobPhase += speed * bobFrequency * dt;
                bob = new Vector3(Mathf.Cos(bobPhase * Mathf.PI) * bobAmount, -Mathf.Abs(Mathf.Sin(bobPhase * Mathf.PI)) * bobAmount, 0f) * bobWeight;
            }

            // Movement poses
            Vector3 targetPosePos = Vector3.zero;
            Vector3 targetPoseRot = Vector3.zero;
            if (movement != null && settings.motionEffects)
            {
                if (movement.IsSliding)
                {
                    targetPosePos += new Vector3(-0.03f, -0.04f, 0f);
                    targetPoseRot += new Vector3(0f, 0f, 12f);
                }
                if (movement.IsWallRunning && movement.WallRun != null)
                {
                    targetPoseRot += new Vector3(0f, 0f, -movement.WallRun.WallSide * 10f);
                }
                if (!movement.IsGrounded)
                {
                    float vy = Mathf.Clamp(movement.Velocity.y * -0.004f, -0.04f, 0.04f);
                    targetPosePos += new Vector3(0f, vy, 0f);
                }
            }
            if (reloading)
            {
                targetPosePos += reloadOffset;
                targetPoseRot += reloadRotation;
            }
            posePos = Vector3.Lerp(posePos, targetPosePos, MathUtil.Damp(10f, dt));
            poseRot = Vector3.Lerp(poseRot, targetPoseRot, MathUtil.Damp(10f, dt));

            // Aim
            aimWeight = Mathf.MoveTowards(aimWeight, aiming ? 1f : 0f, dt * 6f);

            // Kick recovery
            kickPos = Vector3.Lerp(kickPos, Vector3.zero, MathUtil.Damp(kickRecovery, dt));
            kickRot = Vector3.Lerp(kickRot, Vector3.zero, MathUtil.Damp(kickRecovery, dt));

            // Equip
            equipTimer = Mathf.Min(1f, equipTimer + dt / Mathf.Max(0.01f, equipDuration));
            float e = 1f - (1f - equipTimer) * (1f - equipTimer);
            Vector3 equipPos = new Vector3(0f, -0.3f, 0f) * (1f - e);
            Vector3 equipRot = new Vector3(35f, 0f, 0f) * (1f - e);

            transform.localPosition = basePosition + swayPos + bob + posePos + kickPos + equipPos + aimOffset * aimWeight;
            transform.localRotation = Quaternion.Euler(swayRot + poseRot + kickRot + equipRot);
        }
    }
}
