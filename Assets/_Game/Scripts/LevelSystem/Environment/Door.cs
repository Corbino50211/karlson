using System;
using Momentum.Audio;
using Momentum.Bosses;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    public enum DoorMode
    {
        /// <summary>Opens when the player is near.</summary>
        Proximity,
        /// <summary>Opens when every enemy of its arena id is dead.</summary>
        ArenaClear,
        /// <summary>Open until a boss fight starts; reopens when the boss is defeated or resets.</summary>
        BossFight,
        /// <summary>Opens with the interact key.</summary>
        Interact,
        /// <summary>Never opens.</summary>
        Locked
    }

    /// <summary>Sliding door with several opening rules (proximity, arena cleared, boss fight, interact).</summary>
    public class Door : MonoBehaviour, IInteractable, ILevelObjectConfigurable
    {
        [SerializeField] DoorMode mode = DoorMode.Proximity;
        [SerializeField] string arenaId = "";
        [SerializeField] Transform panel;
        [SerializeField] Vector3 openOffset = new Vector3(0f, 4.8f, 0f);
        [SerializeField] float speed = 6f;
        [SerializeField] float proximityRadius = 6f;
        [SerializeField] Renderer statusLight;

        Vector3 closedLocal;
        bool open;
        bool initialized;
        MaterialPropertyBlock block;

        public bool IsOpen => open;

        void Start()
        {
            if (panel != null) closedLocal = panel.localPosition;
            initialized = true;
            switch (mode)
            {
                case DoorMode.ArenaClear:
                    SetOpen(ArenaRegistry.IsCleared(arenaId), true);
                    break;
                case DoorMode.BossFight:
                    SetOpen(true, true);
                    break;
                default:
                    SetOpen(false, true);
                    break;
            }
        }

        void OnEnable()
        {
            GameEvents.ArenaCleared += OnArenaCleared;
            GameEvents.BossStarted += OnBossStarted;
            GameEvents.BossDefeated += OnBossEnded;
            GameEvents.BossReset += OnBossEnded;
        }

        void OnDisable()
        {
            GameEvents.ArenaCleared -= OnArenaCleared;
            GameEvents.BossStarted -= OnBossStarted;
            GameEvents.BossDefeated -= OnBossEnded;
            GameEvents.BossReset -= OnBossEnded;
        }

        void Update()
        {
            if (!initialized || panel == null) return;
            if (mode == DoorMode.Proximity)
            {
                var player = PlayerController.Current;
                bool near = player != null && (player.transform.position - transform.position).sqrMagnitude < proximityRadius * proximityRadius;
                if (near != open) SetOpen(near, false);
            }
            // Panels are scaled with the door, so the open offset is expressed in local units.
            Vector3 target = closedLocal + (open ? LocalOpenOffset() : Vector3.zero);
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, speed / Mathf.Max(0.01f, transform.lossyScale.y) * Time.deltaTime);
        }

        Vector3 LocalOpenOffset()
        {
            Vector3 s = transform.lossyScale;
            return new Vector3(openOffset.x / Mathf.Max(0.01f, s.x), openOffset.y / Mathf.Max(0.01f, s.y), openOffset.z / Mathf.Max(0.01f, s.z));
        }

        public void SetOpen(bool value, bool instant)
        {
            if (open != value && !instant) AudioManager.Play(SoundId.DoorOpen, transform.position, 0.7f, value ? 1f : 0.8f);
            open = value;
            if (instant && panel != null && initialized) panel.localPosition = closedLocal + (open ? LocalOpenOffset() : Vector3.zero);
            UpdateLight();
        }

        void UpdateLight()
        {
            if (statusLight == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            Color c = open ? new Color(0.3f, 1f, 0.4f) : (mode == DoorMode.Locked || mode == DoorMode.ArenaClear ? new Color(1f, 0.2f, 0.15f) : new Color(1f, 0.75f, 0.2f));
            statusLight.GetPropertyBlock(block);
            block.SetColor("_Color", c);
            block.SetColor("_EmissionColor", c * 2f);
            statusLight.SetPropertyBlock(block);
        }

        void OnArenaCleared(string id)
        {
            if (mode == DoorMode.ArenaClear && string.Equals(id, arenaId, StringComparison.OrdinalIgnoreCase)) SetOpen(true, false);
        }

        void OnBossStarted(BossBase boss)
        {
            if (mode == DoorMode.BossFight) SetOpen(false, false);
        }

        void OnBossEnded(BossBase boss)
        {
            if (mode == DoorMode.BossFight) SetOpen(true, false);
        }

        public string GetInteractPrompt(PlayerController player) => open ? "Close door" : "Open door";
        public bool CanInteract(PlayerController player) => mode == DoorMode.Interact;
        public void Interact(PlayerController player) => SetOpen(!open, false);

        public void ApplyLevelProperties(LevelObjectData data)
        {
            if (Enum.TryParse(data.GetString("mode", mode.ToString()), true, out DoorMode parsed)) mode = parsed;
            arenaId = data.GetString("arena", arenaId);
            float h = data.scale.y > 0f ? data.scale.y : 5f;
            openOffset = new Vector3(0f, h - 0.3f, 0f);
        }
    }
}
