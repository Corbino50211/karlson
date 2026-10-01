using Momentum.Weapons;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Root facade for the player. Owns references to every player subsystem, coordinates death,
    /// respawn and control locking, and exposes the active player through PlayerController.Current
    /// so enemies and UI never need manually assigned targets.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour, IKnockbackable
    {
        public static PlayerController Current { get; private set; }

        [SerializeField] PlayerInputHandler inputHandler;
        [SerializeField] PlayerMovement movement;
        [SerializeField] PlayerCamera playerCamera;
        [SerializeField] PlayerHealth health;
        [SerializeField] WeaponManager weapons;
        [SerializeField] PlayerCombat combat;
        [SerializeField] GrappleHook grapple;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] CameraShaker shaker;

        public PlayerInputHandler InputHandler => inputHandler;
        public PlayerMovement Movement => movement;
        public PlayerCamera PlayerCamera => playerCamera;
        public PlayerHealth Health => health;
        public WeaponManager Weapons => weapons;
        public PlayerCombat Combat => combat;
        public GrappleHook Grapple => grapple;
        public CameraShaker Shaker => shaker;

        public bool IsDead => health != null && health.IsDead;
        public bool ControlsEnabled { get; private set; } = true;

        /// <summary>Point enemies aim at (chest height).</summary>
        public Vector3 AimPoint => movement != null ? movement.CenterPosition + Vector3.up * 0.3f : transform.position;
        public Vector3 Velocity => movement != null ? movement.Velocity : Vector3.zero;
        public Vector3 FeetPosition => movement != null ? movement.FeetPosition : transform.position;

        void Awake()
        {
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<PlayerCamera>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (weapons == null) weapons = GetComponent<WeaponManager>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (grapple == null) grapple = GetComponent<GrappleHook>();
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (shaker == null) shaker = GetComponentInChildren<CameraShaker>();
        }

        void OnEnable()
        {
            Current = this;
            if (health != null) health.Died += OnDied;
        }

        void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            if (Current == this) Current = null;
        }

        void Start()
        {
            GameManager.SetCursorLocked(true);
            GameEvents.RaisePlayerSpawned(this);
            GameEvents.RaisePlayerHealthChanged(health != null ? health.Current : 0f, health != null ? health.Max : 0f);
        }

        public void SetControlsEnabled(bool enabled)
        {
            ControlsEnabled = enabled;
            if (inputHandler != null)
            {
                inputHandler.InputEnabled = enabled;
                if (!enabled) inputHandler.ResetState();
            }
            if (playerCamera != null) playerCamera.LookEnabled = enabled;
        }

        void OnDied(DamageInfo info)
        {
            if (movement != null) movement.SetFrozen(true);
            if (weapons != null) weapons.OnOwnerDied();
            SetControlsEnabled(false);
            GameEvents.RaisePlayerDied(this, info);
        }

        /// <summary>Kills the player instantly (kill zones, falling out of the level).</summary>
        public void Kill(DamageType type)
        {
            if (health == null || IsDead) return;
            health.Kill(new DamageInfo(9999f, type, null, transform.position, Vector3.down));
        }

        /// <summary>Respawns at a feet position facing the given yaw. Resets velocity, health and transient state.</summary>
        public void Respawn(Vector3 feetPosition, float yaw)
        {
            var settings = movement != null ? movement.Settings : null;
            float half = settings != null ? settings.standHeight * 0.5f : 1f;
            Vector3 root = feetPosition + Vector3.up * (half + 0.05f);

            if (movement != null)
            {
                movement.ResetState();
                movement.SetFrozen(false);
                movement.Teleport(root, true);
            }
            else
            {
                transform.position = root;
            }
            if (playerCamera != null)
            {
                playerCamera.SetRotation(yaw, 0f);
                playerCamera.ResetEffects();
            }
            if (health != null) health.OnRespawned();
            if (weapons != null) weapons.OnRespawn();
            SetControlsEnabled(true);
            GameEvents.RaisePlayerRespawned(this);
        }

        public void ApplyKnockback(Vector3 velocityChange, float stunDuration)
        {
            if (movement != null) movement.AddImpulse(velocityChange);
        }
    }
}
