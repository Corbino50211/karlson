using UnityEngine;

namespace Momentum.Enemies
{
    public enum EnemyType
    {
        Gunner = 0,
        Shotgunner = 1,
        Sniper = 2,
        Charger = 3,
        Drone = 4,
        Turret = 5
    }

    public enum EnemyState
    {
        Idle,
        Patrol,
        Alert,
        Chase,
        Attack,
        Search,
        Stunned,
        Dead
    }

    /// <summary>Stats for one enemy archetype. Behaviour comes from the EnemyBase subclass on the prefab.</summary>
    [CreateAssetMenu(menuName = "Momentum/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public EnemyType type = EnemyType.Gunner;
        public string displayName = "Gunner";
        public Color bodyColor = new Color(0.92f, 0.92f, 0.94f);
        public Color accentColor = new Color(1f, 0.15f, 0.2f);

        [Header("Health")]
        public float maxHealth = 60f;
        [Tooltip("How strongly knockback/explosions move this enemy (0 = immovable).")]
        public float knockbackMultiplier = 1f;
        [Range(0f, 1f)] public float healthDropChance = 0.2f;
        [Range(0f, 1f)] public float ammoDropChance = 0.25f;

        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float chaseSpeed = 6f;
        public float acceleration = 25f;
        public float turnSpeed = 360f;
        public float patrolRadius = 6f;

        [Header("Perception")]
        public float sightRange = 40f;
        [Tooltip("Field of view (degrees) while unaware.")]
        public float fieldOfView = 130f;
        [Tooltip("Within this distance the player is noticed regardless of facing.")]
        public float hearingRange = 7f;
        public float reactionTime = 0.45f;
        public float searchDuration = 6f;

        [Header("Combat")]
        public float attackRange = 25f;
        [Tooltip("Distance this enemy tries to keep from the player.")]
        public float preferredRange = 12f;
        public float attackCooldown = 1.6f;
        public float damage = 8f;
        public float projectileSpeed = 28f;
        [Tooltip("Shots per burst / pellets per blast.")]
        public int projectileCount = 3;
        public float burstInterval = 0.14f;
        [Tooltip("Aim error cone in degrees.")]
        public float inaccuracy = 2.5f;
        [Tooltip("0 = aim at current position, 1 = full lead on player velocity.")]
        [Range(0f, 1f)] public float leadFactor = 0.5f;
        [Tooltip("Velocity change applied to the player on hit.")]
        public float hitKnockback = 2f;
        public float windupTime = 0.4f;
    }
}
