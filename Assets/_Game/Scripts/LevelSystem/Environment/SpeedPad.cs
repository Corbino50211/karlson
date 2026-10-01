using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Boost strip: while the player is on it, their speed along the pad's forward direction is raised to
    /// at least the boost speed. Faster players keep their speed.
    /// </summary>
    public class SpeedPad : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] float boostSpeed = 26f;
        [SerializeField] float sideDamping = 0.5f;

        float lastSound;

        void OnTriggerStay(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;
            var movement = player.Movement;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) return;
            forward.Normalize();

            Vector3 v = movement.Velocity;
            Vector3 horizontal = new Vector3(v.x, 0f, v.z);
            float along = Vector3.Dot(horizontal, forward);
            if (along >= boostSpeed) return;
            Vector3 side = horizontal - forward * along;
            horizontal = forward * boostSpeed + side * sideDamping;
            movement.Velocity = new Vector3(horizontal.x, v.y, horizontal.z);

            if (Time.time - lastSound > 0.6f)
            {
                lastSound = Time.time;
                AudioManager.Play(SoundId.SpeedPad, transform.position, 0.8f);
            }
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            boostSpeed = data.GetFloat("speed", boostSpeed);
        }
    }
}
