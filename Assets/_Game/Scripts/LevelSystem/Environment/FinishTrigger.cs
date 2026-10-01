using Momentum.Bosses;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Finish line. Optionally locked (barrier visible) until every boss in the level is defeated.
    /// </summary>
    public class FinishTrigger : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] bool requireBossDefeat = true;
        [SerializeField] GameObject lockedBarrier;
        [SerializeField] GameObject openVisual;

        bool locked;
        float nextLockedMessage;

        public bool IsLocked => locked;

        void OnEnable()
        {
            GameEvents.BossDefeated += OnBossDefeated;
        }

        void OnDisable()
        {
            GameEvents.BossDefeated -= OnBossDefeated;
        }

        void Start()
        {
            RefreshLock();
        }

        void RefreshLock()
        {
            locked = false;
            if (requireBossDefeat)
            {
                foreach (var boss in BossBase.All)
                {
                    if (boss != null && !boss.IsDefeated)
                    {
                        locked = true;
                        break;
                    }
                }
            }
            if (lockedBarrier != null) lockedBarrier.SetActive(locked);
            if (openVisual != null) openVisual.SetActive(!locked);
        }

        void OnBossDefeated(BossBase boss)
        {
            bool wasLocked = locked;
            RefreshLock();
            if (wasLocked && !locked) GameEvents.RaiseNotification("EXIT UNLOCKED", new Color(0.3f, 1f, 0.5f));
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;
            if (locked)
            {
                if (Time.time >= nextLockedMessage)
                {
                    nextLockedMessage = Time.time + 2f;
                    GameEvents.RaiseNotification("DEFEAT THE BOSS TO FINISH", GameConfig.Instance.bossColor);
                }
                return;
            }
            GameEvents.RaiseFinishLineCrossed();
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            requireBossDefeat = data.GetBool("requireBoss", requireBossDefeat);
        }
    }
}
