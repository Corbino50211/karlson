using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Bosses
{
    /// <summary>
    /// Trigger volume around a boss arena. When the player enters, the boss activates (and arena doors
    /// listening for BossStarted close). Re-arms when the boss resets after a player death.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class BossArenaTrigger : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] BossBase boss;
        [Tooltip("Radius of the arena (used by the boss to stay inside).")]
        [SerializeField] float arenaRadius = 30f;
        [Tooltip("The fight starts when the player gets this close to the center.")]
        [SerializeField] float activationRadius = 22f;

        SphereCollider sphere;
        bool triggered;

        public BossBase Boss => boss;

        void Awake()
        {
            sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            ApplyRadius();
        }

        void OnEnable()
        {
            GameEvents.BossReset += OnBossReset;
        }

        void OnDisable()
        {
            GameEvents.BossReset -= OnBossReset;
        }

        void Start()
        {
            if (boss == null) boss = GetComponentInChildren<BossBase>();
            if (boss != null) boss.SetArena(transform.position, arenaRadius);
        }

        public void SetBoss(BossBase value)
        {
            boss = value;
        }

        void ApplyRadius()
        {
            if (sphere == null) sphere = GetComponent<SphereCollider>();
            float scale = Mathf.Max(0.01f, transform.lossyScale.x);
            sphere.radius = activationRadius / scale;
            sphere.center = Vector3.zero;
        }

        void OnTriggerEnter(Collider other)
        {
            if (triggered || boss == null || boss.IsDefeated) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;
            triggered = true;
            boss.Activate();
        }

        void OnBossReset(BossBase b)
        {
            if (b == boss) triggered = false;
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            arenaRadius = data.GetFloat("radius", arenaRadius);
            activationRadius = data.GetFloat("activation", Mathf.Min(activationRadius, arenaRadius * 0.8f));
            ApplyRadius();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, arenaRadius);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}
