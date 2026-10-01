using System.Collections.Generic;
using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Launches the player (and enemies/props) upward and forward along the pad's facing direction.
    /// Vertical velocity is overridden for consistent launches; horizontal momentum is added on top.
    /// </summary>
    public class LaunchPad : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] float upwardVelocity = 22f;
        [SerializeField] float forwardVelocity = 8f;
        [SerializeField] float cooldown = 0.3f;
        [SerializeField] ParticleSystem launchEffect;

        readonly Dictionary<Rigidbody, float> lastLaunch = new Dictionary<Rigidbody, float>();

        public Vector3 LaunchVelocity => Vector3.up * upwardVelocity + transform.forward * forwardVelocity;

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            if (lastLaunch.TryGetValue(rb, out float t) && Time.time - t < cooldown) return;
            lastLaunch[rb] = Time.time;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.zero;
            Vector3 launch = Vector3.up * upwardVelocity + forward * forwardVelocity;

            var player = rb.GetComponent<PlayerController>();
            if (player != null)
            {
                if (player.IsDead) return;
                player.Movement.Launch(launch, true, false);
                CameraShaker.Shake(0.15f);
            }
            else if (!rb.isKinematic)
            {
                Vector3 v = rb.velocity;
                v.y = upwardVelocity;
                rb.velocity = v + forward * forwardVelocity;
            }
            else
            {
                return;
            }

            AudioManager.Play(SoundId.LaunchPad, transform.position, 0.9f);
            if (launchEffect != null)
            {
                launchEffect.Clear(true);
                launchEffect.Play(true);
            }
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            upwardVelocity = data.GetFloat("up", upwardVelocity);
            forwardVelocity = data.GetFloat("forward", forwardVelocity);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 v = Vector3.up * upwardVelocity + transform.forward * forwardVelocity;
            Vector3 p = transform.position;
            float g = 28f;
            for (int i = 0; i < 30; i++)
            {
                float t = i * 0.06f;
                Vector3 a = transform.position + v * t + Vector3.down * 0.5f * g * t * t;
                Gizmos.DrawLine(p, a);
                p = a;
            }
        }
    }
}
