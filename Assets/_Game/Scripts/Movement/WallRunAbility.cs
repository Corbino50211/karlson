using System;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Wallrunning, wall jumping and wall kicks. Ticked by PlayerMovement.FixedUpdate.
    /// </summary>
    public class WallRunAbility : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;

        Rigidbody rb;
        float wallRunTimer;
        Collider lastWallCollider;
        Vector3 lastWallNormal;
        float lastWallLeaveTime = -999f;
        int wallKicksUsed;

        public bool IsWallRunning { get; private set; }
        public Vector3 WallNormal { get; private set; }
        /// <summary>-1 = wall on the left, +1 = wall on the right, 0 = none.</summary>
        public int WallSide { get; private set; }
        public float WallRunProgress => movement != null && movement.Settings.wallRunMaxDuration > 0f
            ? wallRunTimer / movement.Settings.wallRunMaxDuration
            : 0f;

        public event Action WallRunStarted;
        public event Action WallRunEnded;
        public event Action WallJumped;
        public event Action WallKicked;

        static readonly Vector3[] probeDirs = new Vector3[6];

        void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            rb = GetComponent<Rigidbody>();
        }

        public void OnGrounded()
        {
            wallKicksUsed = 0;
            if (IsWallRunning) StopWallRun();
        }

        public void TickFixed(float dt)
        {
            var s = movement.Settings;
            if (movement.IsGrounded || movement.IsGrappling || movement.IsMantling || movement.IsFrozen)
            {
                if (IsWallRunning) StopWallRun();
                return;
            }

            bool hasWall = FindWall(s.wallCheckDistance, true, out RaycastHit hit);
            Vector2 input = movement.InputHandler != null ? movement.InputHandler.Move : Vector2.zero;
            bool forward = !s.wallRunRequiresForwardInput || input.y > 0.1f;
            float speed = movement.HorizontalSpeed;

            if (!IsWallRunning)
            {
                if (!hasWall || !forward || speed < s.wallRunMinSpeed) return;
                if (!IsHighEnough(s.wallRunMinHeight)) return;
                if (IsOnCooldown(hit)) return;
                if (rb.velocity.y < -18f) return;
                StartWallRun(hit);
            }
            else
            {
                if (hasWall) UpdateWall(hit);
                bool pushingAway = Vector3.Dot(movement.WishDirection, WallNormal) > 0.5f;
                if (!hasWall || !forward || pushingAway || wallRunTimer > s.wallRunMaxDuration || speed < s.wallRunMinSpeed * 0.5f)
                {
                    StopWallRun();
                    return;
                }
            }

            WallRunMove(dt);
        }

        void StartWallRun(RaycastHit hit)
        {
            var s = movement.Settings;
            IsWallRunning = true;
            wallRunTimer = 0f;
            UpdateWall(hit);
            Vector3 vel = rb.velocity;
            vel.y = Mathf.Max(vel.y * 0.4f, s.wallRunEntryUpBoost);
            rb.velocity = vel;
            if (movement.IsSliding || movement.IsCrouching)
            {
                // Stand up for wallruns if there is room.
                if (movement.CanStandUp()) movement.SetCrouched(false);
            }
            WallRunStarted?.Invoke();
        }

        void UpdateWall(RaycastHit hit)
        {
            WallNormal = hit.normal;
            WallNormal = new Vector3(WallNormal.x, 0f, WallNormal.z).normalized;
            lastWallCollider = hit.collider;
            WallSide = Vector3.Dot(WallNormal, movement.Right) > 0f ? -1 : 1;
        }

        public void StopWallRun()
        {
            if (!IsWallRunning) return;
            IsWallRunning = false;
            lastWallLeaveTime = Time.time;
            lastWallNormal = WallNormal;
            WallSide = 0;
            WallRunEnded?.Invoke();
        }

        void WallRunMove(float dt)
        {
            var s = movement.Settings;
            wallRunTimer += dt;

            Vector3 wallForward = Vector3.Cross(WallNormal, Vector3.up);
            if (Vector3.Dot(wallForward, movement.Forward) < 0f) wallForward = -wallForward;

            Vector3 vel = rb.velocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            float along = Vector3.Dot(horizontal, wallForward);
            if (along < s.wallRunSpeed) along = Mathf.MoveTowards(along, s.wallRunSpeed, s.wallRunAcceleration * dt);
            horizontal = wallForward * along - WallNormal * s.wallStickForce * dt * 10f;

            // Gravity ramps up towards the end of the wallrun.
            float t = wallRunTimer / Mathf.Max(0.01f, s.wallRunMaxDuration);
            float gravity = s.wallRunGravity * (t > 0.6f ? Mathf.Lerp(1f, 4f, (t - 0.6f) / 0.4f) : 1f);
            vel.y -= gravity * dt;
            vel.y = Mathf.Max(vel.y, -6f);
            rb.velocity = new Vector3(horizontal.x, vel.y, horizontal.z);
        }

        public void WallJump()
        {
            var s = movement.Settings;
            Vector3 normal = WallNormal;
            Vector3 vel = rb.velocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            Vector3 look = movement.Forward;
            // Looking away from the wall adds forward boost in the look direction.
            float lookAway = Mathf.Clamp01(Vector3.Dot(look, normal));
            horizontal += normal * s.wallJumpForce + look * s.wallJumpForwardBoost * (1f + lookAway);
            vel = horizontal;
            vel.y = Mathf.Max(rb.velocity.y, 0f) + s.wallJumpUpForce;
            StopWallRun();
            rb.velocity = vel;
            movement.ForceUnground(0.1f);
            WallJumped?.Invoke();
        }

        /// <summary>Kick off a nearby wall while airborne (limited uses before landing).</summary>
        public bool TryWallKick()
        {
            var s = movement.Settings;
            if (wallKicksUsed >= s.maxWallKicks) return false;
            if (!FindWall(s.wallKickDistance, false, out RaycastHit hit)) return false;
            Vector3 normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            Vector3 vel = rb.velocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            float into = Vector3.Dot(horizontal, -normal);
            if (into > 0f) horizontal += normal * into;
            horizontal += normal * s.wallKickForce;
            vel = horizontal;
            vel.y = Mathf.Max(rb.velocity.y, 0f) * 0.3f + s.wallKickUpForce;
            rb.velocity = vel;
            wallKicksUsed++;
            lastWallCollider = hit.collider;
            lastWallNormal = normal;
            lastWallLeaveTime = Time.time;
            movement.ForceUnground(0.1f);
            WallKicked?.Invoke();
            return true;
        }

        bool FindWall(float distance, bool sidesOnly, out RaycastHit best)
        {
            best = default;
            Vector3 origin = movement.CenterPosition;
            Vector3 f = movement.Forward;
            Vector3 r = movement.Right;
            int count;
            if (sidesOnly)
            {
                probeDirs[0] = r;
                probeDirs[1] = -r;
                probeDirs[2] = (r + f).normalized;
                probeDirs[3] = (-r + f).normalized;
                probeDirs[4] = (r - f * 0.5f).normalized;
                probeDirs[5] = (-r - f * 0.5f).normalized;
                count = 6;
            }
            else
            {
                probeDirs[0] = f;
                probeDirs[1] = -f;
                probeDirs[2] = r;
                probeDirs[3] = -r;
                probeDirs[4] = (f + r).normalized;
                probeDirs[5] = (f - r).normalized;
                count = 6;
            }

            float range = movement.Capsule.radius + distance;
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (!Physics.Raycast(origin, probeDirs[i], out RaycastHit hit, range, Layers.WallRunMask, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.35f) continue;
                if (hit.collider.CompareTag(Tags.NoWallRun)) continue;
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic) continue;
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = hit;
                    found = true;
                }
            }
            return found;
        }

        bool IsHighEnough(float minHeight)
        {
            Vector3 feet = movement.FeetPosition;
            return !Physics.Raycast(feet + Vector3.up * 0.1f, Vector3.down, minHeight + 0.1f, Layers.GroundMask, QueryTriggerInteraction.Ignore);
        }

        bool IsOnCooldown(RaycastHit hit)
        {
            if (Time.time - lastWallLeaveTime > movement.Settings.sameWallCooldown) return false;
            Vector3 n = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            return hit.collider == lastWallCollider && Vector3.Dot(n, lastWallNormal) > 0.9f;
        }
    }
}
