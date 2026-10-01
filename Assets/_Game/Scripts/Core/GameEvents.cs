using System;
using Momentum.Bosses;
using Momentum.Enemies;
using Momentum.Levels;
using Momentum.PlayerSystems;
using Momentum.Weapons;
using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Lightweight global event hub. Systems raise events here and UI/audio/level logic listen,
    /// which keeps the gameplay classes decoupled from each other. Subscribers must unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // ---------------- Player ----------------
        public static event Action<PlayerController> PlayerSpawned;
        public static event Action<PlayerController, DamageInfo> PlayerDied;
        public static event Action<PlayerController> PlayerRespawned;
        public static event Action<DamageInfo> PlayerDamaged;
        public static event Action<float, float> PlayerHealthChanged;

        // ---------------- Combat ----------------
        public static event Action<WeaponBase> WeaponEquipped;
        public static event Action<WeaponBase> AmmoChanged;
        public static event Action<WeaponDefinition> WeaponPickedUp;
        public static event Action<bool, bool> HitConfirmed;
        public static event Action<EnemyBase> EnemyKilled;
        public static event Action<Vector3, float> Noise;

        // ---------------- Level ----------------
        public static event Action StartLineCrossed;
        public static event Action FinishLineCrossed;
        public static event Action RunStarted;
        public static event Action<LevelResult> RunFinished;
        public static event Action<Checkpoint, float, float> CheckpointReached;
        public static event Action<string> ArenaCleared;
        public static event Action<string, Color> Notification;
        public static event Action<string> InteractPromptChanged;
        public static event Action<int, int> SecretFound;

        // ---------------- Bosses ----------------
        public static event Action<BossBase> BossStarted;
        public static event Action<BossBase> BossHealthChanged;
        public static event Action<BossBase, int> BossPhaseChanged;
        public static event Action<BossBase> BossDefeated;
        public static event Action<BossBase> BossReset;

        // ---------------- Game ----------------
        public static event Action<bool> PauseChanged;
        public static event Action SettingsChanged;

        public static void RaisePlayerSpawned(PlayerController p) => PlayerSpawned?.Invoke(p);
        public static void RaisePlayerDied(PlayerController p, DamageInfo info) => PlayerDied?.Invoke(p, info);
        public static void RaisePlayerRespawned(PlayerController p) => PlayerRespawned?.Invoke(p);
        public static void RaisePlayerDamaged(DamageInfo info) => PlayerDamaged?.Invoke(info);
        public static void RaisePlayerHealthChanged(float current, float max) => PlayerHealthChanged?.Invoke(current, max);

        public static void RaiseWeaponEquipped(WeaponBase w) => WeaponEquipped?.Invoke(w);
        public static void RaiseAmmoChanged(WeaponBase w) => AmmoChanged?.Invoke(w);
        public static void RaiseWeaponPickedUp(WeaponDefinition d) => WeaponPickedUp?.Invoke(d);
        public static void RaiseHitConfirmed(bool killed, bool critical) => HitConfirmed?.Invoke(killed, critical);
        public static void RaiseEnemyKilled(EnemyBase e) => EnemyKilled?.Invoke(e);
        public static void RaiseNoise(Vector3 position, float radius) => Noise?.Invoke(position, radius);

        public static void RaiseStartLineCrossed() => StartLineCrossed?.Invoke();
        public static void RaiseFinishLineCrossed() => FinishLineCrossed?.Invoke();
        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunFinished(LevelResult result) => RunFinished?.Invoke(result);
        public static void RaiseCheckpointReached(Checkpoint cp, float time, float deltaToBest) => CheckpointReached?.Invoke(cp, time, deltaToBest);
        public static void RaiseArenaCleared(string arenaId) => ArenaCleared?.Invoke(arenaId);
        public static void RaiseNotification(string text, Color color) => Notification?.Invoke(text, color);
        public static void RaiseInteractPrompt(string prompt) => InteractPromptChanged?.Invoke(prompt);
        public static void RaiseSecretFound(int found, int total) => SecretFound?.Invoke(found, total);

        public static void RaiseBossStarted(BossBase b) => BossStarted?.Invoke(b);
        public static void RaiseBossHealthChanged(BossBase b) => BossHealthChanged?.Invoke(b);
        public static void RaiseBossPhaseChanged(BossBase b, int phase) => BossPhaseChanged?.Invoke(b, phase);
        public static void RaiseBossDefeated(BossBase b) => BossDefeated?.Invoke(b);
        public static void RaiseBossReset(BossBase b) => BossReset?.Invoke(b);

        public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);
        public static void RaiseSettingsChanged() => SettingsChanged?.Invoke();

        /// <summary>Clears all subscribers (supports "Enter Play Mode Options" with domain reload disabled).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PlayerSpawned = null; PlayerDied = null; PlayerRespawned = null; PlayerDamaged = null; PlayerHealthChanged = null;
            WeaponEquipped = null; AmmoChanged = null; WeaponPickedUp = null; HitConfirmed = null; EnemyKilled = null; Noise = null;
            StartLineCrossed = null; FinishLineCrossed = null; RunStarted = null; RunFinished = null; CheckpointReached = null;
            ArenaCleared = null; Notification = null; InteractPromptChanged = null; SecretFound = null;
            BossStarted = null; BossHealthChanged = null; BossPhaseChanged = null; BossDefeated = null; BossReset = null;
            PauseChanged = null; SettingsChanged = null;
        }
    }
}
