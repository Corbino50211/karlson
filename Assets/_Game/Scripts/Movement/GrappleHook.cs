using System;
using Momentum.Audio;
using Momentum.Levels;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Grappling hook. Aim at a surface or grapple point within range and press the grapple key.
    /// While attached the player swings on an inelastic rope that slowly reels in and pulls toward the
    /// anchor, preserving momentum. Releasing gives a small boost; jumping while attached performs a
    /// grapple jump.
    /// </summary>
    public class GrappleHook : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;
        [SerializeField] Transform aimTransform;
        [SerializeField] Transform ropeOrigin;
        [SerializeField] GrappleRope rope;
        [SerializeField] Transform targetIndicator;

        Rigidbody rb;
        Transform attachedTo;
        Vector3 localAnchor;
        float ropeLength;
        float cooldownUntil;
        float attachTime;
        Collider targetCollider;
        GrapplePoint targetPoint;
        GrapplePoint highlightedPoint;

        public bool IsGrappling { get; private set; }
        public bool HasTarget { get; private set; }
        public Vector3 TargetPoint { get; private set; }
        public Vector3 AnchorPoint { get; private set; }
        public float RopeLength => ropeLength;
        public Transform RopeOrigin => ropeOrigin != null ? ropeOrigin : aimTransform;
        public bool IsReady => Time.time >= cooldownUntil;
        public float CooldownRemaining => Mathf.Max(0f, cooldownUntil - Time.time);

        public event Action Attached;
        public event Action Released;

        MovementSettings Settings => movement.Settings;

        void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            rb = GetComponent<Rigidbody>();
            if (aimTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) aimTransform = cam.transform;
            }
        }

        void OnDisable()
        {
            SetHighlighted(null);
            if (targetIndicator != null) targetIndicator.gameObject.SetActive(false);
        }

        void Update()
        {
            if (movement == null || aimTransform == null) return;
            if (movement.IsFrozen || (GameManager.Instance != null && GameManager.Instance.IsPaused))
            {
                HasTarget = false;
                UpdateIndicator();
                return;
            }

            if (!IsGrappling) UpdateTargeting();
            else HasTarget = false;

            var input = movement.InputHandler;
            if (input != null)
            {
                if (!IsGrappling)
                {
                    if (input.GrapplePressed && HasTarget && IsReady) StartGrapple();
                }
                else if (Time.time - attachTime > 0.05f)
                {
                    bool release = Settings.grappleHoldToAttach ? !input.GrappleHeld : input.GrapplePressed;
                    if (release) StopGrapple(true);
                }
            }
            UpdateIndicator();
        }

        void UpdateTargeting()
        {
            var s = Settings;
            Vector3 origin = aimTransform.position;
            Vector3 dir = aimTransform.forward;
            targetPoint = null;
            targetCollider = null;

            // 1) Dedicated grapple points inside the assist cone.
            float bestAngle = s.grapplePointAssistAngle;
            foreach (var gp in GrapplePoint.All)
            {
                if (gp == null) continue;
                Vector3 p = gp.AnchorPosition;
                Vector3 to = p - origin;
                float dist = to.magnitude;
                if (dist > s.grappleMaxDistance || dist < 0.5f) continue;
                float angle = Vector3.Angle(dir, to);
                if (angle > bestAngle) continue;
                if (Physics.Linecast(origin, p - to / dist * 0.6f, out RaycastHit block, Layers.SolidMask, QueryTriggerInteraction.Ignore) &&
                    !block.collider.transform.IsChildOf(gp.transform))
                {
                    continue;
                }
                bestAngle = angle;
                targetPoint = gp;
            }

            if (targetPoint != null)
            {
                HasTarget = true;
                TargetPoint = targetPoint.AnchorPosition;
                targetCollider = null;
                SetHighlighted(targetPoint);
                return;
            }
            SetHighlighted(null);

            // 2) Any grappleable surface.
            if (Physics.SphereCast(origin, s.grappleAimAssistRadius, dir, out RaycastHit hit, s.grappleMaxDistance, Layers.GrappleMask, QueryTriggerInteraction.Ignore) &&
                hit.distance > 1f)
            {
                // Prefer the exact point under the crosshair when the precise ray agrees.
                if (Physics.Raycast(origin, dir, out RaycastHit precise, s.grappleMaxDistance, Layers.GrappleMask, QueryTriggerInteraction.Ignore) &&
                    precise.collider == hit.collider)
                {
                    hit = precise;
                }
                HasTarget = true;
                TargetPoint = hit.point;
                targetCollider = hit.collider;
            }
            else
            {
                HasTarget = false;
            }
        }

        void SetHighlighted(GrapplePoint gp)
        {
            if (highlightedPoint == gp) return;
            if (highlightedPoint != null) highlightedPoint.SetTargeted(false);
            highlightedPoint = gp;
            if (highlightedPoint != null) highlightedPoint.SetTargeted(true);
        }

        void UpdateIndicator()
        {
            if (targetIndicator == null) return;
            bool show = HasTarget && !IsGrappling && IsReady && !movement.IsFrozen;
            if (targetIndicator.gameObject.activeSelf != show) targetIndicator.gameObject.SetActive(show);
            if (!show) return;
            targetIndicator.position = TargetPoint;
            float dist = Vector3.Distance(aimTransform.position, TargetPoint);
            targetIndicator.localScale = Vector3.one * Mathf.Clamp(dist * 0.025f, 0.15f, 1.2f);
            targetIndicator.rotation = Quaternion.LookRotation(aimTransform.position - TargetPoint) * Quaternion.Euler(0f, 0f, Time.time * 90f);
        }

        void StartGrapple()
        {
            var s = Settings;
            IsGrappling = true;
            attachTime = Time.time;
            if (targetPoint != null)
            {
                attachedTo = targetPoint.transform;
            }
            else if (targetCollider != null)
            {
                attachedTo = targetCollider.transform;
            }
            else
            {
                IsGrappling = false;
                return;
            }
            localAnchor = attachedTo.InverseTransformPoint(TargetPoint);
            AnchorPoint = TargetPoint;
            float dist = Vector3.Distance(rb.position, AnchorPoint);
            ropeLength = Mathf.Max(s.grappleMinRopeLength, dist * s.grappleInitialRopeFactor);

            if (movement.WallRun != null) movement.WallRun.StopWallRun();
            if (movement.IsGrounded)
            {
                movement.AddImpulse(Vector3.up * 4f);
                movement.ForceUnground(0.2f);
            }

            if (rope != null) rope.Shoot();
            AudioManager.Play(SoundId.GrappleFire, transform.position);
            AudioManager.Play(SoundId.GrappleAttach, AnchorPoint, 0.8f);
            SetHighlighted(null);
            Attached?.Invoke();
        }

        /// <summary>Applies rope physics. Called from PlayerMovement.FixedUpdate after movement.</summary>
        public void TickFixed(float dt)
        {
            if (!IsGrappling) return;
            var s = Settings;
            if (attachedTo == null || !attachedTo.gameObject.activeInHierarchy)
            {
                StopGrapple(false);
                return;
            }

            AnchorPoint = attachedTo.TransformPoint(localAnchor);
            Vector3 toAnchor = AnchorPoint - rb.position;
            float dist = toAnchor.magnitude;
            if (dist < s.grappleAutoReleaseDistance)
            {
                StopGrapple(true);
                return;
            }
            if (dist > s.grappleMaxDistance * 1.4f)
            {
                StopGrapple(false);
                return;
            }

            Vector3 dir = toAnchor / dist;
            ropeLength = Mathf.Max(s.grappleMinRopeLength, Mathf.Min(ropeLength, dist) - s.grappleReelSpeed * dt);

            Vector3 vel = rb.velocity;
            vel += dir * s.grappleForce * dt;

            Vector3 wish = movement.WishDirection;
            if (wish.sqrMagnitude > 0f) vel += Vector3.ProjectOnPlane(wish, dir) * s.grappleSwingAcceleration * dt;

            if (dist > ropeLength)
            {
                float radial = Vector3.Dot(vel, dir);
                if (radial < 0f) vel -= dir * radial;
                vel += dir * (dist - ropeLength) * s.grappleRopeSpring * dt;
            }

            rb.velocity = vel;
            if (movement.IsGrounded && Vector3.Dot(dir, Vector3.up) > 0.3f) movement.ForceUnground(0.1f);
        }

        void StopGrapple(bool boost)
        {
            if (!IsGrappling) return;
            IsGrappling = false;
            var s = Settings;
            if (boost)
            {
                Vector3 vel = rb.velocity;
                if (vel.sqrMagnitude > 1f) vel += vel.normalized * s.grappleReleaseBoost;
                vel.y += s.grappleReleaseUpBoost;
                rb.velocity = vel;
            }
            cooldownUntil = Time.time + s.grappleCooldown;
            attachedTo = null;
            if (rope != null) rope.Retract();
            AudioManager.Play(SoundId.GrappleRelease, transform.position, 0.6f);
            Released?.Invoke();
        }

        /// <summary>Jump while attached: detach with an upward and forward boost.</summary>
        public void GrappleJump()
        {
            var s = Settings;
            StopGrapple(false);
            Vector3 vel = rb.velocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            if (horizontal.sqrMagnitude > 0.01f) horizontal += horizontal.normalized * s.grappleReleaseBoost;
            vel = new Vector3(horizontal.x, Mathf.Max(vel.y, 0f) + s.grappleJumpForce, horizontal.z);
            rb.velocity = vel;
            movement.ForceUnground(0.12f);
            AudioManager.Play(SoundId.WallJump, transform.position, 0.8f);
        }

        public void ForceRelease(bool boost)
        {
            StopGrapple(boost);
            if (rope != null) rope.Hide();
        }
    }
}
