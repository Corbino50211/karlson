using System;
using UnityEngine;

namespace Momentum.PlayerSystems
{
    /// <summary>
    /// Ledge detection, vaulting (fast hop over low obstacles), mantling (climbing up chest-high ledges)
    /// and automatic step-up over small curbs. Ticked by PlayerMovement.
    /// </summary>
    public class LedgeAbility : MonoBehaviour
    {
        [SerializeField] PlayerMovement movement;

        Rigidbody rb;
        Vector3 mantleStart;
        Vector3 mantleMid;
        Vector3 mantleEnd;
        float mantleT;
        float mantleDuration;
        Vector3 mantleExitVelocity;
        float cooldownUntil;

        public bool IsMantling { get; private set; }

        public event Action Vaulted;
        public event Action Mantled;

        void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            rb = GetComponent<Rigidbody>();
        }

        public void TickDetect(float dt)
        {
            if (IsMantling || movement.IsFrozen || Time.time < cooldownUntil) return;
            if (movement.IsGrappling || movement.IsWallRunning) return;

            Vector3 moveDir = movement.WishDirection;
            if (moveDir.sqrMagnitude < 0.01f) return;
            if (Vector3.Dot(moveDir, movement.Forward) < 0.3f) return;

            var s = movement.Settings;
            Vector3 feet = movement.FeetPosition;
            float radius = movement.Capsule.radius;

            if (movement.IsGrounded && TryStepUp(feet, moveDir, radius)) return;

            float probe = radius + s.ledgeCheckDistance;
            RaycastHit wallHit;
            if (!Physics.Raycast(feet + Vector3.up * 0.55f, moveDir, out wallHit, probe, Layers.SolidMask, QueryTriggerInteraction.Ignore) &&
                !Physics.Raycast(feet + Vector3.up * 1.3f, moveDir, out wallHit, probe, Layers.SolidMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }
            if (Mathf.Abs(wallHit.normal.y) > 0.3f) return;
            if (wallHit.rigidbody != null && !wallHit.rigidbody.isKinematic) return;
            Vector3 into = -new Vector3(wallHit.normal.x, 0f, wallHit.normal.z).normalized;
            if (Vector3.Dot(into, moveDir) < 0.5f) return;

            // Find the top of the obstacle.
            float probeTopHeight = s.mantleMaxHeight + 0.3f;
            Vector3 topProbe = new Vector3(wallHit.point.x, feet.y + probeTopHeight, wallHit.point.z) + into * (radius + 0.1f);
            if (Physics.CheckSphere(topProbe, 0.05f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return; // wall continues upward
            if (!Physics.Raycast(topProbe, Vector3.down, out RaycastHit topHit, probeTopHeight - 0.2f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return;
            if (topHit.normal.y < 0.7f) return;

            float ledgeHeight = topHit.point.y - feet.y;
            if (ledgeHeight < s.minLedgeHeight || ledgeHeight > s.mantleMaxHeight) return;

            // Clearance on top of the ledge.
            Vector3 destFeet = topHit.point + into * (radius * 0.6f) + Vector3.up * 0.02f;
            bool fitsStanding = Fits(destFeet, s.standHeight, radius);
            bool fitsCrouched = fitsStanding || Fits(destFeet, s.crouchHeight, radius);
            if (!fitsCrouched) return;

            float horizontalSpeed = movement.HorizontalSpeed;
            if (ledgeHeight <= s.vaultMaxHeight && horizontalSpeed >= s.vaultMinSpeed)
            {
                Vault(ledgeHeight, into);
                return;
            }

            bool wantsMantle = !movement.IsGrounded || (movement.InputHandler != null && movement.InputHandler.JumpHeld);
            if (wantsMantle && rb.velocity.y < 6f)
            {
                StartMantle(destFeet, into, ledgeHeight, !fitsStanding, horizontalSpeed);
            }
        }

        bool Fits(Vector3 feet, float height, float radius)
        {
            float r = radius * 0.95f;
            Vector3 bottom = feet + Vector3.up * (r + 0.05f);
            Vector3 top = feet + Vector3.up * Mathf.Max(r + 0.06f, height - r - 0.02f);
            return !Physics.CheckCapsule(bottom, top, r, Layers.SolidMask, QueryTriggerInteraction.Ignore);
        }

        bool TryStepUp(Vector3 feet, Vector3 dir, float radius)
        {
            var s = movement.Settings;
            if (movement.HorizontalSpeed < 0.5f) return false;
            if (!Physics.Raycast(feet + Vector3.up * 0.06f, dir, out RaycastHit low, radius + 0.25f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
            if (Mathf.Abs(low.normal.y) > 0.3f) return false;
            if (low.rigidbody != null && !low.rigidbody.isKinematic) return false;
            if (Physics.Raycast(feet + Vector3.up * (s.stepHeight + 0.05f), dir, radius + 0.35f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;

            Vector3 probe = new Vector3(low.point.x, feet.y + s.stepHeight + 0.05f, low.point.z) + dir * 0.12f;
            if (!Physics.Raycast(probe, Vector3.down, out RaycastHit top, s.stepHeight + 0.05f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
            if (top.normal.y < 0.7f) return false;
            float h = top.point.y - feet.y;
            if (h <= 0.02f || h > s.stepHeight) return false;
            Vector3 newFeet = feet + Vector3.up * (h + 0.02f) + dir * 0.06f;
            if (!Fits(newFeet, movement.IsCrouching ? s.crouchHeight : s.standHeight, radius)) return false;
            rb.position += Vector3.up * (h + 0.02f) + dir * 0.06f;
            return true;
        }

        void Vault(float ledgeHeight, Vector3 into)
        {
            var s = movement.Settings;
            Vector3 vel = rb.velocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            if (Vector3.Dot(horizontal, into) < 0.5f) horizontal = into * Mathf.Max(horizontal.magnitude, s.vaultMinSpeed);
            horizontal += into * s.vaultForwardBoost;
            float up = Mathf.Sqrt(2f * s.gravity * (ledgeHeight + 0.3f));
            rb.velocity = new Vector3(horizontal.x, Mathf.Max(vel.y, up), horizontal.z);
            movement.ForceUnground(0.15f);
            cooldownUntil = Time.time + 0.35f;
            Vaulted?.Invoke();
        }

        void StartMantle(Vector3 destFeet, Vector3 into, float ledgeHeight, bool crouch, float horizontalSpeed)
        {
            var s = movement.Settings;
            IsMantling = true;
            mantleT = 0f;
            mantleDuration = s.mantleDuration * Mathf.Lerp(0.6f, 1f, ledgeHeight / Mathf.Max(0.01f, s.mantleMaxHeight));
            mantleStart = rb.position;
            float halfStand = s.standHeight * 0.5f;
            mantleEnd = destFeet + Vector3.up * halfStand;
            mantleMid = new Vector3(mantleStart.x, mantleEnd.y + 0.05f, mantleStart.z);
            mantleExitVelocity = into * Mathf.Max(horizontalSpeed * 0.8f, s.mantleExitSpeed);
            if (crouch) movement.SetCrouched(true);
            rb.velocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;
            Mantled?.Invoke();
        }

        public void TickMantle(float dt)
        {
            if (!IsMantling) return;
            mantleT += dt / Mathf.Max(0.01f, mantleDuration);
            Vector3 p;
            if (mantleT < 0.6f)
            {
                float k = mantleT / 0.6f;
                k = 1f - (1f - k) * (1f - k);
                p = Vector3.Lerp(mantleStart, mantleMid, k);
            }
            else
            {
                float k = Mathf.Clamp01((mantleT - 0.6f) / 0.4f);
                p = Vector3.Lerp(mantleMid, mantleEnd, k);
            }
            rb.MovePosition(p);
            if (mantleT >= 1f) FinishMantle();
        }

        void RestoreBody()
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        void FinishMantle()
        {
            IsMantling = false;
            RestoreBody();
            rb.velocity = mantleExitVelocity;
            cooldownUntil = Time.time + 0.2f;
        }

        public void Cancel()
        {
            if (!IsMantling) return;
            IsMantling = false;
            RestoreBody();
            cooldownUntil = Time.time + 0.2f;
        }
    }
}
