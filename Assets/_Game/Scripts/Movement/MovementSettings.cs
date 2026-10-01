using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Every tunable value of the player controller. Create variants (Assets > Create > Momentum >
    /// Movement Settings) to experiment with different movement feels without touching code.
    /// Units are meters and seconds.
    /// </summary>
    [CreateAssetMenu(menuName = "Momentum/Movement Settings", fileName = "MovementSettings")]
    public class MovementSettings : ScriptableObject
    {
        [Header("Body")]
        public float standHeight = 2f;
        public float crouchHeight = 1.1f;
        public float radius = 0.4f;
        [Tooltip("Eye height above the feet while standing.")]
        public float eyeHeightStanding = 1.7f;
        [Tooltip("Eye height above the feet while crouching/sliding.")]
        public float eyeHeightCrouched = 0.85f;

        [Header("Ground Movement")]
        [Tooltip("Top running speed (m/s).")]
        public float maxGroundSpeed = 11f;
        [Tooltip("Optional sprint speed (Left Shift).")]
        public float sprintSpeed = 14f;
        public bool allowSprint = true;
        public float crouchSpeed = 5f;
        [Tooltip("Quake-style acceleration factor on the ground. Higher = snappier.")]
        public float groundAcceleration = 14f;
        [Tooltip("Ground friction. Decelerates the player when not accelerating.")]
        public float friction = 6f;
        [Tooltip("Speed below which friction uses this value as a minimum (makes stopping crisp).")]
        public float stopSpeed = 3f;
        [Tooltip("Extra deceleration (m/s²) when input is released or opposes movement. Makes direction changes snappy.")]
        public float counterMovement = 40f;
        public float maxSlopeAngle = 46f;

        [Header("Air Movement")]
        [Tooltip("Air strafe acceleration (Source-style sv_airaccelerate).")]
        public float airAcceleration = 14f;
        [Tooltip("Wish-speed cap used for air strafing. Low values + high airAcceleration = classic strafe-jumping gains.")]
        public float maxAirSpeed = 1.6f;
        [Tooltip("Basic directional control in the air up to this speed.")]
        public float airMoveSpeed = 8f;
        public float airMoveAcceleration = 2.2f;
        [Tooltip("CPMA-style air control when holding only forward (0 = none).")]
        public float airControl = 0.6f;
        [Tooltip("Hard limit on horizontal speed (safety cap).")]
        public float maxHorizontalSpeed = 60f;
        public float maxFallSpeed = 60f;

        [Header("Gravity & Jumping")]
        public float gravity = 28f;
        [Tooltip("Gravity multiplier while falling (>1 = snappier arcs).")]
        public float fallGravityMultiplier = 1.1f;
        [Tooltip("Upward velocity applied by a jump (m/s).")]
        public float jumpForce = 9.5f;
        public float jumpCooldown = 0.12f;
        [Tooltip("Grace period after leaving a ledge during which a jump is still allowed.")]
        public float coyoteTime = 0.12f;
        [Tooltip("Jump presses this early before landing are remembered.")]
        public float jumpBufferTime = 0.14f;
        [Tooltip("Holding jump re-jumps automatically on landing.")]
        public bool autoBunnyHop = true;

        [Header("Bunny Hopping")]
        [Tooltip("Window after landing during which a jump counts as a perfect hop (no friction).")]
        public float bunnyHopWindow = 0.06f;
        [Tooltip("Speed added on each perfect hop.")]
        public float bunnyHopSpeedGain = 0.35f;
        [Tooltip("Perfect-hop gains stop above this horizontal speed.")]
        public float maxBunnyHopSpeed = 24f;

        [Header("Ground Snapping")]
        public float groundCheckDistance = 0.12f;
        public float groundSnapDistance = 0.55f;
        [Tooltip("Above this speed the player launches off ramp crests instead of snapping down.")]
        public float groundSnapMaxSpeed = 18f;
        public float stepHeight = 0.42f;

        [Header("Sliding")]
        [Tooltip("Impulse added when a slide starts.")]
        public float slideForce = 7f;
        [Tooltip("Friction while sliding (much lower than ground friction).")]
        public float slideFriction = 0.7f;
        public float slideMinStartSpeed = 5.5f;
        public float slideStopSpeed = 3.5f;
        public float slideBoostCooldown = 0.8f;
        [Tooltip("No slide boost above this speed (prevents infinite speed).")]
        public float slideBoostMaxSpeed = 20f;
        [Tooltip("How strongly slopes accelerate a slide downhill.")]
        public float slideGravityMultiplier = 1.4f;
        [Tooltip("Steering rate while sliding (degrees per second).")]
        public float slideSteerRate = 70f;
        [Tooltip("Horizontal boost when jumping out of a slide.")]
        public float slideJumpBoost = 2.2f;

        [Header("Wall Running")]
        public float wallRunSpeed = 13f;
        public float wallRunAcceleration = 18f;
        [Tooltip("Gravity while wall running (m/s²).")]
        public float wallRunGravity = 3f;
        public float wallRunMaxDuration = 1.9f;
        public float wallRunMinSpeed = 5f;
        [Tooltip("Minimum height above the ground to start a wallrun.")]
        public float wallRunMinHeight = 0.9f;
        public float wallCheckDistance = 0.75f;
        public float wallStickForce = 6f;
        public float wallRunEntryUpBoost = 1.5f;
        public bool wallRunRequiresForwardInput = true;
        [Tooltip("Outward force of a wall jump.")]
        public float wallJumpForce = 8.5f;
        public float wallJumpUpForce = 8.5f;
        public float wallJumpForwardBoost = 1.5f;
        [Tooltip("Seconds before the same wall can be run on again after leaving it.")]
        public float sameWallCooldown = 0.45f;

        [Header("Wall Kicks")]
        public float wallKickForce = 8f;
        public float wallKickUpForce = 8.5f;
        public int maxWallKicks = 2;
        public float wallKickDistance = 0.6f;

        [Header("Vaulting & Mantling")]
        public float vaultMaxHeight = 1.15f;
        public float vaultMinSpeed = 3f;
        public float vaultForwardBoost = 1f;
        public float minLedgeHeight = 0.45f;
        public float mantleMaxHeight = 2.3f;
        public float mantleDuration = 0.32f;
        public float mantleExitSpeed = 6f;
        public float ledgeCheckDistance = 0.7f;

        [Header("Grappling Hook")]
        public float grappleMaxDistance = 42f;
        [Tooltip("Acceleration pulling the player toward the anchor (m/s²).")]
        public float grappleForce = 28f;
        [Tooltip("Rope shortening speed while attached (m/s).")]
        public float grappleReelSpeed = 7f;
        public float grappleMinRopeLength = 3f;
        [Tooltip("Rope length when attaching, as a fraction of the distance.")]
        public float grappleInitialRopeFactor = 0.92f;
        [Tooltip("Steering acceleration while swinging (m/s²).")]
        public float grappleSwingAcceleration = 12f;
        public float grappleRopeSpring = 40f;
        [Tooltip("Gravity multiplier while grappling.")]
        public float grappleGravityMultiplier = 0.85f;
        [Tooltip("Speed boost along the velocity direction when releasing.")]
        public float grappleReleaseBoost = 3f;
        public float grappleReleaseUpBoost = 2.5f;
        public float grappleJumpForce = 8f;
        public float grappleCooldown = 0.25f;
        public float grappleAimAssistRadius = 0.5f;
        [Tooltip("Cone (degrees) in which dedicated grapple points are magnetically targeted.")]
        public float grapplePointAssistAngle = 7f;
        public float grappleAutoReleaseDistance = 1.8f;
        [Tooltip("Hold Q to stay attached (true) or press Q to toggle (false).")]
        public bool grappleHoldToAttach = true;

        [Header("External Forces")]
        [Tooltip("Upward impulses above this value detach the player from the ground.")]
        public float impulseUngroundThreshold = 2f;
        [Tooltip("Cancel downward velocity when receiving an upward impulse (rocket/shotgun jumps feel reliable).")]
        public bool cancelFallOnUpwardImpulse = true;
    }
}
