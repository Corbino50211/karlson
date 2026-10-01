using Momentum.Audio;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>Movement sounds: footsteps (distance based), jumps, landings, slides, wallruns, vaults.</summary>
    public class PlayerAudio : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;
        [SerializeField] float strideLength = 2.6f;
        [SerializeField] float wallRunStride = 1.8f;

        float distanceAccumulator;

        void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
        }

        void OnEnable()
        {
            if (movement == null) return;
            movement.Jumped += OnJumped;
            movement.Landed += OnLanded;
            movement.SlideStarted += OnSlide;
            if (movement.WallRun != null)
            {
                movement.WallRun.WallJumped += OnWallJump;
                movement.WallRun.WallKicked += OnWallJump;
            }
            if (movement.Ledge != null)
            {
                movement.Ledge.Vaulted += OnVault;
                movement.Ledge.Mantled += OnVault;
            }
        }

        void OnDisable()
        {
            if (movement == null) return;
            movement.Jumped -= OnJumped;
            movement.Landed -= OnLanded;
            movement.SlideStarted -= OnSlide;
            if (movement.WallRun != null)
            {
                movement.WallRun.WallJumped -= OnWallJump;
                movement.WallRun.WallKicked -= OnWallJump;
            }
            if (movement.Ledge != null)
            {
                movement.Ledge.Vaulted -= OnVault;
                movement.Ledge.Mantled -= OnVault;
            }
        }

        void Update()
        {
            if (movement == null || movement.IsFrozen) return;
            float speed = movement.HorizontalSpeed;
            if (movement.IsWallRunning)
            {
                distanceAccumulator += speed * Time.deltaTime;
                if (distanceAccumulator >= wallRunStride)
                {
                    distanceAccumulator = 0f;
                    AudioManager.Play(SoundId.WallRunStep, movement.FeetPosition, 0.5f);
                }
            }
            else if (movement.IsGrounded && !movement.IsSliding && speed > 1.5f)
            {
                distanceAccumulator += speed * Time.deltaTime;
                float stride = movement.IsCrouching ? strideLength * 0.7f : strideLength;
                if (distanceAccumulator >= stride)
                {
                    distanceAccumulator = 0f;
                    AudioManager.Play(SoundId.Footstep, movement.FeetPosition, movement.IsCrouching ? 0.3f : 0.55f);
                }
            }
        }

        void OnJumped() => AudioManager.Play(SoundId.Jump, movement.FeetPosition, 0.5f);

        void OnLanded(float impact)
        {
            if (impact < 3f) return;
            AudioManager.Play(SoundId.Land, movement.FeetPosition, Mathf.Clamp(impact / 20f, 0.3f, 1f));
            distanceAccumulator = 0f;
        }

        void OnSlide() => AudioManager.Play(SoundId.SlideStart, movement.FeetPosition, 0.6f);
        void OnWallJump() => AudioManager.Play(SoundId.WallJump, movement.CenterPosition, 0.6f);
        void OnVault() => AudioManager.Play(SoundId.Vault, movement.CenterPosition, 0.5f);
    }
}
