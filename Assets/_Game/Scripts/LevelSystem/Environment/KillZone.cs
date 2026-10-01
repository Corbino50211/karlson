using Momentum.Enemies;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Instantly kills the player (and enemies) that enter it: pits, lava, coolant.</summary>
    public class KillZone : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] Renderer visual;
        [SerializeField] bool visibleInPlay = true;

        void Start()
        {
            if (visual != null) visual.enabled = visibleInPlay;
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.Kill(DamageType.Fall);
                return;
            }
            var enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDead && enemy.Health != null)
            {
                enemy.Health.Kill(new DamageInfo(9999f, DamageType.Fall, gameObject, other.transform.position, Vector3.down));
            }
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            visibleInPlay = data.GetBool("visible", visibleInPlay);
        }
    }
}
