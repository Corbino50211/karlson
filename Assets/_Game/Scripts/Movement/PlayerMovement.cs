using System;
using Momentum.Levels;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    public enum MovementState
    {
        Grounded,
        Crouching,
        Sliding,
        Airborne,
        WallRunning,
        Grappling,
        Mantling,
        Frozen
    }

    /// <summary>
    /// Custom Rigidbody-based first-person motor (no CharacterController).
    ///
    /// Ground/air movement uses Quake/Source style acceleration so momentum is preserved, air strafing
    /// gains speed and perfectly timed jumps skip friction (bunny hopping). On top of that it handles
    /// coyote time, jump buffering, slopes, ground snapping, moving platforms, crouching, sliding and
    /// external impulses (rocket jumps, shotgun boosts, launch pads). Wallrunning, vaulting/mantling and
    /// the grappling hook are separate components ticked from here in a deterministic order.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] MovementSettings settings;
        [SerializeField] PlayerInputHandler inputHandler;
        [SerializeField] PlayerCamera playerCamera;
        [SerializeField] WallRunAbility wallRun;
        [SerializeField] LedgeAbility ledge;
        [SerializeField] GrappleHook grapple;
        [SerializeField] PhysicMaterial frictionlessMaterial;

        Rigidbody rb;
        CapsuleCollider capsule;

        // Ground state
        Collider groundCollider;
        MovingPlatform groundPlatform;
        Collider cachedPlatformCollider;
        bool wasGrounded;

        // Timers (Time.time based)
        float lastGroundedTime = -999f;
        float lastJumpTime = -999f;
        float landedTime = -999f;
        float ungroundedUntil = -999f;
        float lastSlideBoostTime = -999f;

        public MovementSettings Settings => settings;
        public PlayerInputHandler InputHandler => inputHandler;
        public PlayerCamera PlayerCamera => playerCamera;
        public WallRunAbility WallRun => wallRun;
        public LedgeAbility Ledge => ledge;
        public GrappleHook Grapple => grapple;
        public Rigidbody Body => rb;
        public CapsuleCollider Capsule => capsule;

        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsFrozen { get; private set; }
        public bool IsWallRunning => wallRun != null && wallRun.IsWallRunning;
        public bool IsGrappling => grapple != null && grapple.IsGrappling;
        public bool IsMantling => ledge != null && ledge.IsMantling;

        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public Vector3 GroundVelocity { get; private set; }
        public Collider GroundCollider => groundCollider;
        public MovementState State { get; private set; }
        public Vector3 WishDirection { get; private set; }
        public Vector3 PreviousVelocity { get; private set; }
        public float LastLandingSpeed { get; private set; }

        /// <summary>Multiplier applied to target speeds (heavy weapons slow the player slightly).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        public Vector3 Velocity
        {
            get => rb.velocity;
            set => rb.velocity = value;
        }

        public Vector3 HorizontalVelocity
        {
            get
            {
                var v = rb.velocity;
                return new Vector3(v.x, 0f, v.z);
            }
        }

        public float HorizontalSpeed => HorizontalVelocity.magnitude;
        public float Speed => rb.velocity.magnitude;

        public float Yaw => playerCamera != null ? playerCamera.Yaw : transform.eulerAngles.y;
        public Quaternion Orientation => Quaternion.Euler(0f, Yaw, 0f);
        public Vector3 Forward => Orientation * Vector3.forward;
        public Vector3 Right => Orientation * Vector3.right;

        /// <summary>Feet position (constant relative to the root, also while crouched).</summary>
        public Vector3 FeetPosition => rb.position + Vector3.down * (settings.standHeight * 0.5f);
        public Vector3 CenterPosition => rb.position + capsule.center;

        /// <summary>Local Y of the eye relative to the root, used by the camera.</summary>
        public float EyeHeightLocal =>
            (IsCrouching ? settings.eyeHeightCrouched : settings.eyeHeightStanding) - settings.standHeight * 0.5f;

        public bool JustJumped => Time.time - lastJumpTime < 0.15f;
        public float TimeSinceGrounded => Time.time - lastGroundedTime;

        public event Action Jumped;
        public event Action<float> Landed;
        public event Action SlideStarted;
        public event Action SlideEnded;

        // ------------------------------------------------------------------ Unity

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            if (settings == null)
            {
                settings = GameConfig.Instance.defaultMovementSettings;
                if (settings == null) settings = ScriptableObject.CreateInstance<MovementSettings>();
            }
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
            if (wallRun == null) wallRun = GetComponent<WallRunAbility>();
            if (ledge == null) ledge = GetComponent<LedgeAbility>();
            if (grapple == null) grapple = GetComponent<GrappleHook>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<PlayerCamera>();

            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.drag = 0f;
            rb.angularDrag = 0f;

            capsule.direction = 1;
            capsule.radius = settings.radius;
            capsule.height = settings.standHeight;
            capsule.center = Vector3.zero;
            if (capsule.sharedMaterial == null)
            {
                if (frictionlessMaterial == null)
                {
                    frictionlessMaterial = new PhysicMaterial("PlayerFrictionless")
                    {
                        dynamicFriction = 0f,
                        staticFriction = 0f,
                        bounciness = 0f,
                        frictionCombine = PhysicMaterialCombine.Minimum,
                        bounceCombine = PhysicMaterialCombine.Minimum
                    };
                }
                capsule.sharedMaterial = frictionlessMaterial;
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            PreviousVelocity = rb.velocity;

            if (IsFrozen)
            {
                FrozenTick(dt);
                return;
            }

            UpdateWishDirection();
            CheckGround();

            if (ledge != null && ledge.IsMantling)
            {
                ledge.TickMantle(dt);
                State = MovementState.Mantling;
                wasGrounded = IsGrounded;
                return;
            }

            UpdateCrouchAndSlide();
            if (wallRun != null) wallRun.TickFixed(dt);

            bool jumped = HandleJump();

            Vector3 vel = rb.velocity;
            if (IsWallRunning)
            {
                // Wallrun applies its own movement and gravity.
            }
            else if (IsGrounded && !jumped)
            {
                if (IsSliding) SlideMove(ref vel, dt);
                else GroundMove(ref vel, dt);
            }
            else
            {
                AirMove(ref vel, dt);
                ApplyGravity(ref vel, dt);
            }
            ClampVelocity(ref vel);
            rb.velocity = vel;

            if (grapple != null) grapple.TickFixed(dt);
            if (ledge != null) ledge.TickDetect(dt);

            UpdateState();
            wasGrounded = IsGrounded;
        }

        // ------------------------------------------------------------------ Input

        void UpdateWishDirection()
        {
            Vector2 m = inputHandler != null ? inputHandler.Move : Vector2.zero;
            Vector3 wish = Forward * m.y + Right * m.x;
            WishDirection = wish.sqrMagnitude > 0.0001f ? wish.normalized : Vector3.zero;
        }

        // ------------------------------------------------------------------ Ground detection

        void CheckGround()
        {
            bool canGround = Time.time >= ungroundedUntil;
            Vector3 center = CenterPosition;
            float castRadius = capsule.radius * 0.9f;
            float toFeet = capsule.height * 0.5f - castRadius;
            float castDistance = toFeet + settings.groundCheckDistance;

            IsGrounded = false;
            if (canGround && Physics.SphereCast(center, castRadius, Vector3.down, out RaycastHit hit, castDistance,
                    Layers.GroundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 normal = hit.normal;
                if (Physics.Raycast(hit.point + Vector3.up * 0.1f, Vector3.down, out RaycastHit refine, 0.25f,
                        Layers.GroundMask, QueryTriggerInteraction.Ignore) && refine.collider == hit.collider)
                {
                    if (Vector3.Angle(refine.normal, Vector3.up) <= settings.maxSlopeAngle) normal = refine.normal;
                }

                if (Vector3.Angle(normal, Vector3.up) <= settings.maxSlopeAngle)
                {
                    Vector3 surfaceVel = GetSurfaceVelocity(hit);
                    Vector3 rel = rb.velocity - surfaceVel;
                    // Moving away from the surface (jumping, launch pads) means we are not grounded.
                    if (Vector3.Dot(rel, normal) <= 1.5f || hit.distance < toFeet - 0.02f)
                    {
                        IsGrounded = true;
                        GroundNormal = normal;
                        GroundVelocity = surfaceVel;
                        groundCollider = hit.collider;
                    }
                }
            }

            if (!IsGrounded && wasGrounded && canGround && !JustJumped) TrySnapToGround(center, castRadius, toFeet);

            if (!IsGrounded)
            {
                GroundNormal = Vector3.up;
                GroundVelocity = Vector3.zero;
                groundCollider = null;
            }
            else
            {
                lastGroundedTime = Time.time;
                if (wallRun != null) wallRun.OnGrounded();
                if (!wasGrounded) OnLanded();
            }
        }

        void TrySnapToGround(Vector3 center, float castRadius, float toFeet)
        {
            if (HorizontalSpeed > settings.groundSnapMaxSpeed) return;
            if (rb.velocity.y > 0.5f) return;
            if (!Physics.SphereCast(center, castRadius, Vector3.down, out RaycastHit hit, toFeet + settings.groundSnapDistance,
                    Layers.GroundMask, QueryTriggerInteraction.Ignore)) return;
            if (Vector3.Angle(hit.normal, Vector3.up) > settings.maxSlopeAngle) return;

            float snap = hit.distance - toFeet - 0.01f;
            if (snap > 0f) rb.position += Vector3.down * snap;

            Vector3 vel = rb.velocity;
            float speed = vel.magnitude;
            vel = Vector3.ProjectOnPlane(vel, hit.normal);
            if (vel.sqrMagnitude > 0.0001f) vel = vel.normalized * speed;
            rb.velocity = vel;

            IsGrounded = true;
            GroundNormal = hit.normal;
            GroundVelocity = GetSurfaceVelocity(hit);
            groundCollider = hit.collider;
        }

        Vector3 GetSurfaceVelocity(RaycastHit hit)
        {
            if (hit.collider != cachedPlatformCollider)
            {
                cachedPlatformCollider = hit.collider;
                groundPlatform = hit.collider != null ? hit.collider.GetComponentInParent<MovingPlatform>() : null;
            }
            if (groundPlatform != null) return groundPlatform.Velocity;
            var body = hit.rigidbody;
            if (body != null && body.isKinematic) return body.GetPointVelocity(hit.point);
            return Vector3.zero;
        }

        void OnLanded()
        {
            LastLandingSpeed = Mathf.Max(0f, -PreviousVelocity.y);
            landedTime = Time.time;
            Landed?.Invoke(LastLandingSpeed);

            if (inputHandler != null && inputHandler.CrouchHeld && HorizontalSpeed >= settings.slideMinStartSpeed)
            {
                StartSlide();
            }
        }

        // ------------------------------------------------------------------ Jumping

        bool HandleJump()
        {
            if (inputHandler == null) return false;
            bool buffered = inputHandler.HasBufferedJump(settings.jumpBufferTime);
            bool autoHop = settings.autoBunnyHop && inputHandler.JumpHeld;
            if (!buffered && !autoHop) return false;

            if (IsWallRunning)
            {
                if (!buffered) return false;
                wallRun.WallJump();
                inputHandler.ConsumeJump();
                return true;
            }

            if (IsGrappling)
            {
                if (!buffered) return false;
                grapple.GrappleJump();
                inputHandler.ConsumeJump();
                return true;
            }

            bool coyote = !IsGrounded && Time.time - lastGroundedTime <= settings.coyoteTime &&
                          lastJumpTime < lastGroundedTime;
            if ((IsGrounded || coyote) && Time.time - lastJumpTime >= settings.jumpCooldown)
            {
                GroundJump();
                inputHandler.ConsumeJump();
                return true;
            }

            if (buffered && !IsGrounded && wallRun != null && wallRun.TryWallKick())
            {
                inputHandler.ConsumeJump();
                return true;
            }

            return false;
        }

        void GroundJump()
        {
            Vector3 vel = rb.velocity;
            bool perfectHop = Time.time - landedTime <= settings.bunnyHopWindow + Time.fixedDeltaTime;

            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            if (IsSliding)
            {
                if (horizontal.sqrMagnitude > 0.01f) horizontal += horizontal.normalized * settings.slideJumpBoost;
                EndSlide();
            }
            if (perfectHop && settings.bunnyHopSpeedGain > 0f && horizontal.magnitude > settings.maxGroundSpeed * 0.8f &&
                horizontal.magnitude < settings.maxBunnyHopSpeed)
            {
                horizontal += horizontal.normalized * settings.bunnyHopSpeedGain;
            }

            vel.x = horizontal.x;
            vel.z = horizontal.z;
            vel.y = settings.jumpForce + Mathf.Max(0f, GroundVelocity.y);
            rb.velocity = vel;

            IsGrounded = false;
            lastJumpTime = Time.time;
            ungroundedUntil = Time.time + 0.08f;
            Jumped?.Invoke();
        }

        /// <summary>Marks the player as airborne for a short time (used by abilities that launch the player).</summary>
        public void ForceUnground(float duration)
        {
            IsGrounded = false;
            ungroundedUntil = Mathf.Max(ungroundedUntil, Time.time + duration);
            lastJumpTime = Time.time;
        }

        // ------------------------------------------------------------------ Crouch / slide

        void UpdateCrouchAndSlide()
        {
            bool crouchHeld = inputHandler != null && inputHandler.CrouchHeld;
            bool crouchPressed = inputHandler != null && inputHandler.ConsumeCrouchPressed();

            if (crouchHeld)
            {
                SetCrouched(true);
                if (!IsSliding && IsGrounded && HorizontalSpeed >= settings.slideMinStartSpeed &&
                    (crouchPressed || Time.time - landedTime < 0.12f))
                {
                    StartSlide();
                }
            }
            else
            {
                if (IsSliding) EndSlide();
                if (IsCrouching && CanStandUp()) SetCrouched(false);
            }

            if (IsSliding)
            {
                if (!IsGrounded)
                {
                    // Leaving the ground pauses the slide; landing with crouch held resumes it.
                    EndSlide();
                }
                else
                {
                    float slopeAngle = Vector3.Angle(GroundNormal, Vector3.up);
                    if ((rb.velocity - GroundVelocity).magnitude < settings.slideStopSpeed && slopeAngle < 6f) EndSlide();
                }
            }
        }

        void StartSlide()
        {
            if (IsSliding) return;
            IsSliding = true;
            SetCrouched(true);

            Vector3 vel = rb.velocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            Vector3 dir = horizontal.sqrMagnitude > 0.1f ? horizontal.normalized : (WishDirection.sqrMagnitude > 0f ? WishDirection : Forward);
            if (Time.time - lastSlideBoostTime >= settings.slideBoostCooldown && horizontal.magnitude < settings.slideBoostMaxSpeed)
            {
                vel += dir * settings.slideForce;
                lastSlideBoostTime = Time.time;
                rb.velocity = vel;
            }
            SlideStarted?.Invoke();
        }

        void EndSlide()
        {
            if (!IsSliding) return;
            IsSliding = false;
            SlideEnded?.Invoke();
        }

        public void SetCrouched(bool crouched)
        {
            if (IsCrouching == crouched) return;
            IsCrouching = crouched;
            float h = crouched ? settings.crouchHeight : settings.standHeight;
            capsule.height = h;
            capsule.center = new Vector3(0f, (h - settings.standHeight) * 0.5f, 0f);
        }

        public bool CanStandUp()
        {
            float r = capsule.radius * 0.95f;
            Vector3 feet = FeetPosition;
            Vector3 bottom = feet + Vector3.up * (r + 0.05f);
            Vector3 top = feet + Vector3.up * (settings.standHeight - r - 0.02f);
            return !Physics.CheckCapsule(bottom, top, r, Layers.SolidMask, QueryTriggerInteraction.Ignore);
        }

        // ------------------------------------------------------------------ Movement

        float TargetSpeed()
        {
            float speed = settings.maxGroundSpeed;
            if (IsCrouching && !IsSliding) speed = settings.crouchSpeed;
            else if (settings.allowSprint && inputHandler != null && inputHandler.SprintHeld && inputHandler.Move.y > 0.1f &&
                     SaveSystem.SettingsManager.Settings.sprintEnabled)
            {
                speed = settings.sprintSpeed;
            }
            return speed * SpeedMultiplier;
        }

        void GroundMove(ref Vector3 vel, float dt)
        {
            Vector3 rel = vel - GroundVelocity;
            float relSpeed = new Vector3(rel.x, 0f, rel.z).magnitude;
            bool landingGrace = Time.time - landedTime < settings.bunnyHopWindow && relSpeed > settings.maxGroundSpeed;
            if (!landingGrace)
            {
                ApplyFriction(ref rel, settings.friction, dt);
                ApplyCounterMovement(ref rel, dt);
            }

            Vector3 wish = WishDirection;
            if (wish.sqrMagnitude > 0f) wish = Vector3.ProjectOnPlane(wish, GroundNormal).normalized;
            Accelerate(ref rel, wish, TargetSpeed(), settings.groundAcceleration, dt);

            float mag = rel.magnitude;
            rel = Vector3.ProjectOnPlane(rel, GroundNormal);
            if (rel.sqrMagnitude > 0.0001f) rel = rel.normalized * mag;
            vel = rel + GroundVelocity;
        }

        void SlideMove(ref Vector3 vel, float dt)
        {
            Vector3 rel = vel - GroundVelocity;

            // Gravity along the slope accelerates the slide downhill.
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, GroundNormal);
            rel += downhill * settings.gravity * settings.slideGravityMultiplier * dt;

            ApplyFriction(ref rel, settings.slideFriction, dt, 0.5f);

            if (WishDirection.sqrMagnitude > 0f && rel.sqrMagnitude > 0.01f)
            {
                Vector3 planarWish = Vector3.ProjectOnPlane(WishDirection, GroundNormal).normalized;
                float speed = rel.magnitude;
                Vector3 dir = Vector3.RotateTowards(rel / speed, planarWish, settings.slideSteerRate * Mathf.Deg2Rad * dt, 0f);
                rel = dir * speed;
            }

            float mag = rel.magnitude;
            rel = Vector3.ProjectOnPlane(rel, GroundNormal);
            if (rel.sqrMagnitude > 0.0001f) rel = rel.normalized * mag;
            vel = rel + GroundVelocity;
        }

        void AirMove(ref Vector3 vel, float dt)
        {
            Vector3 wish = WishDirection;
            if (wish.sqrMagnitude <= 0f) return;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            float wishSpeed = TargetSpeed();

            // Source-style air acceleration with a low wish-speed cap: strafing gains speed.
            float capped = Mathf.Min(wishSpeed, settings.maxAirSpeed);
            float current = Vector3.Dot(horizontal, wish);
            float add = capped - current;
            if (add > 0f)
            {
                float accel = Mathf.Min(settings.airAcceleration * wishSpeed * dt, add);
                horizontal += wish * accel;
            }

            // Basic directional control (lets players steer from low speeds).
            Accelerate(ref horizontal, wish, settings.airMoveSpeed * SpeedMultiplier, settings.airMoveAcceleration, dt);

            // CPMA-like air control while holding only forward.
            if (settings.airControl > 0f && inputHandler != null && inputHandler.Move.y > 0.1f && Mathf.Abs(inputHandler.Move.x) < 0.1f)
            {
                float speed = horizontal.magnitude;
                if (speed > 0.5f)
                {
                    Vector3 dir = horizontal / speed;
                    float dot = Vector3.Dot(dir, wish);
                    if (dot > 0f)
                    {
                        float k = 32f * settings.airControl * dot * dot * dt;
                        dir = (dir * speed + wish * k).normalized;
                        horizontal = dir * speed;
                    }
                }
            }

            vel.x = horizontal.x;
            vel.z = horizontal.z;
        }

        void ApplyGravity(ref Vector3 vel, float dt)
        {
            float g = settings.gravity;
            if (vel.y < 0f) g *= settings.fallGravityMultiplier;
            if (IsGrappling) g *= settings.grappleGravityMultiplier;
            vel.y -= g * dt;
            if (vel.y < -settings.maxFallSpeed) vel.y = -settings.maxFallSpeed;
        }

        void ClampVelocity(ref Vector3 vel)
        {
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            float max = settings.maxHorizontalSpeed;
            if (horizontal.sqrMagnitude > max * max)
            {
                horizontal = horizontal.normalized * max;
                vel.x = horizontal.x;
                vel.z = horizontal.z;
            }
        }

        static void Accelerate(ref Vector3 vel, Vector3 wishDir, float wishSpeed, float accel, float dt)
        {
            if (wishDir.sqrMagnitude < 0.0001f) return;
            float current = Vector3.Dot(vel, wishDir);
            float add = wishSpeed - current;
            if (add <= 0f) return;
            float accelSpeed = Mathf.Min(accel * wishSpeed * dt, add);
            vel += wishDir * accelSpeed;
        }

        void ApplyFriction(ref Vector3 vel, float friction, float dt, float stopSpeedScale = 1f)
        {
            Vector3 lateral = Vector3.ProjectOnPlane(vel, GroundNormal);
            float speed = lateral.magnitude;
            if (speed < 0.01f)
            {
                vel -= lateral;
                return;
            }
            float control = Mathf.Max(speed, settings.stopSpeed * stopSpeedScale);
            float drop = control * friction * dt;
            float newSpeed = Mathf.Max(0f, speed - drop);
            vel += lateral * (newSpeed / speed - 1f);
        }

        void ApplyCounterMovement(ref Vector3 vel, float dt)
        {
            if (settings.counterMovement <= 0f || inputHandler == null) return;
            Vector2 input = inputHandler.Move;
            Vector3 fwd = Forward;
            Vector3 right = Right;
            float vf = Vector3.Dot(vel, fwd);
            float vr = Vector3.Dot(vel, right);
            float limit = settings.maxGroundSpeed * 1.15f;
            float decel = settings.counterMovement * dt;

            if (Mathf.Abs(vf) < limit && (Mathf.Abs(input.y) < 0.01f || Mathf.Sign(input.y) != Mathf.Sign(vf)))
            {
                float newVf = Mathf.MoveTowards(vf, 0f, decel);
                vel += fwd * (newVf - vf);
            }
            if (Mathf.Abs(vr) < limit && (Mathf.Abs(input.x) < 0.01f || Mathf.Sign(input.x) != Mathf.Sign(vr)))
            {
                float newVr = Mathf.MoveTowards(vr, 0f, decel);
                vel += right * (newVr - vr);
            }
        }

        // ------------------------------------------------------------------ External forces

        /// <summary>
        /// Adds an instant velocity change (knockback, explosions, shotgun recoil). Momentum is preserved.
        /// </summary>
        public void AddImpulse(Vector3 velocityChange)
        {
            if (IsFrozen) return;
            if (ledge != null && ledge.IsMantling) ledge.Cancel();
            Vector3 vel = rb.velocity;
            if (settings.cancelFallOnUpwardImpulse && velocityChange.y > 1f && vel.y < 0f) vel.y = 0f;
            vel += velocityChange;
            rb.velocity = vel;
            if (velocityChange.y > settings.impulseUngroundThreshold)
            {
                ForceUnground(0.15f);
                if (IsSliding) EndSlide();
            }
        }

        /// <summary>
        /// Launch pads: sets/adds velocity. Vertical override makes pads consistent regardless of fall speed.
        /// </summary>
        public void Launch(Vector3 velocity, bool overrideVertical, bool overrideHorizontal)
        {
            if (IsFrozen) return;
            if (ledge != null && ledge.IsMantling) ledge.Cancel();
            if (wallRun != null) wallRun.StopWallRun();
            Vector3 vel = rb.velocity;
            if (overrideHorizontal)
            {
                vel.x = velocity.x;
                vel.z = velocity.z;
            }
            else
            {
                vel.x += velocity.x;
                vel.z += velocity.z;
            }
            vel.y = overrideVertical ? velocity.y : vel.y + velocity.y;
            rb.velocity = vel;
            ForceUnground(0.25f);
            if (IsSliding) EndSlide();
        }

        /// <summary>Restores the velocity from before the last physics step (used when smashing through glass).</summary>
        public void RestorePreviousVelocity(float factor = 1f)
        {
            rb.velocity = PreviousVelocity * factor;
        }

        // ------------------------------------------------------------------ State control

        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;
            if (frozen)
            {
                if (IsSliding) EndSlide();
                if (wallRun != null) wallRun.StopWallRun();
                if (grapple != null) grapple.ForceRelease(false);
                if (ledge != null) ledge.Cancel();
                State = MovementState.Frozen;
            }
        }

        void FrozenTick(float dt)
        {
            CheckGround();
            Vector3 vel = rb.velocity;
            if (IsGrounded)
            {
                Vector3 rel = vel - GroundVelocity;
                ApplyFriction(ref rel, settings.friction * 2f, dt);
                vel = rel + GroundVelocity;
            }
            else
            {
                ApplyGravity(ref vel, dt);
            }
            rb.velocity = vel;
            wasGrounded = IsGrounded;
        }

        /// <summary>Clears all transient movement state (used on respawn).</summary>
        public void ResetState()
        {
            if (IsSliding) EndSlide();
            if (wallRun != null) wallRun.StopWallRun();
            if (grapple != null) grapple.ForceRelease(false);
            if (ledge != null) ledge.Cancel();
            IsCrouching = true;
            SetCrouched(false);
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            IsGrounded = false;
            wasGrounded = false;
            lastJumpTime = -999f;
            landedTime = -999f;
            ungroundedUntil = -999f;
            lastGroundedTime = Time.time;
            SpeedMultiplier = 1f;
            if (inputHandler != null) inputHandler.ResetState();
        }

        /// <summary>Moves the player instantly (respawns, teleporters).</summary>
        public void Teleport(Vector3 rootPosition, bool resetVelocity)
        {
            rb.position = rootPosition;
            transform.position = rootPosition;
            if (resetVelocity)
            {
                rb.velocity = Vector3.zero;
                PreviousVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
        }

        void UpdateState()
        {
            if (IsFrozen) State = MovementState.Frozen;
            else if (IsMantling) State = MovementState.Mantling;
            else if (IsGrappling) State = MovementState.Grappling;
            else if (IsWallRunning) State = MovementState.WallRunning;
            else if (IsSliding) State = MovementState.Sliding;
            else if (IsGrounded) State = IsCrouching ? MovementState.Crouching : MovementState.Grounded;
            else State = MovementState.Airborne;
        }
    }
}
