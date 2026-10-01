using System.Collections.Generic;
using System.Text;
using Momentum.Bosses;
using Momentum.Levels;
using Momentum.PlayerSystems;
using Momentum.SaveSystem;
using Momentum.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>
    /// In-game HUD: timer, personal best, speed, health, ammo, weapon, crosshair with grapple indicator,
    /// hitmarkers, boss health bar, checkpoint splits, notifications, interact prompt, damage direction
    /// indicators, vignettes and the death overlay.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        [Header("Top")]
        [SerializeField] Text timerText;
        [SerializeField] Text pbText;
        [SerializeField] Text levelNameText;
        [SerializeField] Text startHintText;

        [Header("Speed")]
        [SerializeField] Text speedText;
        [SerializeField] Image speedFill;

        [Header("Health")]
        [SerializeField] Text healthText;
        [SerializeField] Image healthFill;

        [Header("Weapon")]
        [SerializeField] Text weaponText;
        [SerializeField] Text ammoText;
        [SerializeField] Text reserveText;
        [SerializeField] Image reloadFill;
        [SerializeField] Text slotsText;

        [Header("Crosshair")]
        [SerializeField] RectTransform crosshairRoot;
        [SerializeField] Image crosshairDot;
        [SerializeField] Image[] crosshairLines = new Image[0];
        [SerializeField] Image grappleRing;
        [SerializeField] CanvasGroup hitmarkerGroup;
        [SerializeField] Image[] hitmarkerLines = new Image[0];

        [Header("Boss")]
        [SerializeField] RectTransform bossRoot;
        [SerializeField] Text bossNameText;
        [SerializeField] Text bossPhaseText;
        [SerializeField] Image bossFill;
        [SerializeField] Image bossFillDelayed;

        [Header("Messages")]
        [SerializeField] Text notificationText;
        [SerializeField] CanvasGroup notificationGroup;
        [SerializeField] Text checkpointText;
        [SerializeField] CanvasGroup checkpointGroup;
        [SerializeField] Text interactText;

        [Header("Feedback")]
        [SerializeField] Image damageVignette;
        [SerializeField] Image lowHealthVignette;
        [SerializeField] RectTransform damageIndicatorRoot;
        [SerializeField] Image[] damageIndicators = new Image[0];
        [SerializeField] CanvasGroup deathGroup;
        [SerializeField] Text deathText;

        struct Indicator
        {
            public Vector3 source;
            public float time;
        }

        readonly Indicator[] indicatorState = new Indicator[8];
        readonly StringBuilder sb = new StringBuilder(64);
        float notificationTimer;
        float checkpointTimer;
        float hitmarkerTimer;
        float damageFlash;
        float displayedBoss = 1f;
        BossBase activeBoss;
        float smoothSpeed;
        float deathTimer;
        bool dead;

        void Awake()
        {
            if (crosshairDot != null) crosshairDot.sprite = UISprites.Circle;
            if (grappleRing != null) grappleRing.sprite = UISprites.Ring;
            foreach (var ind in damageIndicators)
            {
                if (ind != null) ind.sprite = UISprites.Triangle;
            }
            for (int i = 0; i < indicatorState.Length; i++) indicatorState[i].time = -999f;
            SetBossVisible(false);
            if (notificationGroup != null) notificationGroup.alpha = 0f;
            if (checkpointGroup != null) checkpointGroup.alpha = 0f;
            if (hitmarkerGroup != null) hitmarkerGroup.alpha = 0f;
            if (deathGroup != null) deathGroup.alpha = 0f;
            if (interactText != null) interactText.text = "";
        }

        void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.PlayerDamaged += OnPlayerDamaged;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.PlayerRespawned += OnPlayerRespawned;
            GameEvents.WeaponEquipped += OnWeaponChanged;
            GameEvents.AmmoChanged += OnWeaponChanged;
            GameEvents.WeaponPickedUp += OnWeaponPickedUp;
            GameEvents.HitConfirmed += OnHitConfirmed;
            GameEvents.Notification += OnNotification;
            GameEvents.CheckpointReached += OnCheckpoint;
            GameEvents.InteractPromptChanged += OnInteractPrompt;
            GameEvents.BossStarted += OnBossStarted;
            GameEvents.BossHealthChanged += OnBossHealth;
            GameEvents.BossPhaseChanged += OnBossPhase;
            GameEvents.BossDefeated += OnBossEnded;
            GameEvents.BossReset += OnBossEnded;
            GameEvents.RunFinished += OnRunFinished;
        }

        void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.PlayerDamaged -= OnPlayerDamaged;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.PlayerRespawned -= OnPlayerRespawned;
            GameEvents.WeaponEquipped -= OnWeaponChanged;
            GameEvents.AmmoChanged -= OnWeaponChanged;
            GameEvents.WeaponPickedUp -= OnWeaponPickedUp;
            GameEvents.HitConfirmed -= OnHitConfirmed;
            GameEvents.Notification -= OnNotification;
            GameEvents.CheckpointReached -= OnCheckpoint;
            GameEvents.InteractPromptChanged -= OnInteractPrompt;
            GameEvents.BossStarted -= OnBossStarted;
            GameEvents.BossHealthChanged -= OnBossHealth;
            GameEvents.BossPhaseChanged -= OnBossPhase;
            GameEvents.BossDefeated -= OnBossEnded;
            GameEvents.BossReset -= OnBossEnded;
            GameEvents.RunFinished -= OnRunFinished;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var settings = SettingsManager.Settings;
            var player = PlayerController.Current;
            var level = LevelManager.Instance;
            var timer = LevelTimer.Instance;

            // Timer & PB
            if (timerText != null)
            {
                timerText.enabled = settings.showTimer;
                timerText.text = TimeFormat.Format(timer != null ? timer.CurrentTime : 0f);
            }
            if (pbText != null)
            {
                float best = level != null ? level.BestTime : -1f;
                pbText.enabled = settings.showTimer;
                pbText.text = best > 0f ? "PB  " + TimeFormat.Format(best) : "PB  --:--.--";
            }
            if (levelNameText != null) levelNameText.text = level != null ? level.LevelName.ToUpperInvariant() : "";
            if (startHintText != null)
            {
                bool waiting = level != null && level.State == RunState.WaitingForStart;
                startHintText.enabled = waiting;
                if (waiting) startHintText.text = StartTrigger.All.Count > 0 ? "CROSS THE START GATE" : "MOVE TO START";
            }

            // Speed
            float speed = player != null && player.Movement != null ? player.Movement.HorizontalSpeed : 0f;
            smoothSpeed = Mathf.Lerp(smoothSpeed, speed, MathUtil.Damp(12f, dt));
            if (speedText != null)
            {
                speedText.enabled = settings.showSpeedometer;
                speedText.text = smoothSpeed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            if (speedFill != null)
            {
                speedFill.enabled = settings.showSpeedometer;
                UIFactory.SetFill(speedFill, Mathf.Clamp01(smoothSpeed / 40f));
                speedFill.color = Color.Lerp(UITheme.Secondary, UITheme.Accent, Mathf.Clamp01((smoothSpeed - 11f) / 20f));
            }

            // Crosshair / grapple indicator
            bool grappleTarget = player != null && player.Grapple != null && player.Grapple.HasTarget && player.Grapple.IsReady && !player.Grapple.IsGrappling;
            if (grappleRing != null)
            {
                grappleRing.enabled = grappleTarget || (player != null && player.Grapple != null && player.Grapple.IsGrappling);
                grappleRing.color = player != null && player.Grapple != null && player.Grapple.IsGrappling ? UITheme.Accent : UITheme.Secondary;
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.06f;
                grappleRing.rectTransform.localScale = Vector3.one * pulse;
            }
            float spread = 0f;
            var weapon = player != null && player.Weapons != null ? player.Weapons.Current : null;
            if (weapon != null && weapon.Definition != null) spread = weapon.Definition.spread * (player.Movement.IsGrounded ? 1f : weapon.Definition.airborneSpreadMultiplier) + speed * 0.05f;
            float gap = 7f + spread * 6f;
            for (int i = 0; i < crosshairLines.Length; i++)
            {
                var line = crosshairLines[i];
                if (line == null) continue;
                Vector2 dir = i == 0 ? Vector2.up : i == 1 ? Vector2.down : i == 2 ? Vector2.left : Vector2.right;
                line.rectTransform.anchoredPosition = dir * (gap + 5f);
            }
            if (weapon != null && weapon.IsAiming && crosshairRoot != null) crosshairRoot.localScale = Vector3.one * 0.7f;
            else if (crosshairRoot != null) crosshairRoot.localScale = Vector3.one;

            // Hitmarker
            if (hitmarkerGroup != null)
            {
                hitmarkerTimer -= dt;
                hitmarkerGroup.alpha = Mathf.Clamp01(hitmarkerTimer / 0.15f);
            }

            // Reload
            if (reloadFill != null)
            {
                bool reloading = weapon != null && weapon.IsReloading;
                reloadFill.enabled = reloading;
                if (reloading) UIFactory.SetFill(reloadFill, weapon.ReloadProgress);
            }

            // Notifications
            notificationTimer -= dt;
            if (notificationGroup != null) notificationGroup.alpha = Mathf.Clamp01(notificationTimer / 0.4f);
            checkpointTimer -= dt;
            if (checkpointGroup != null) checkpointGroup.alpha = Mathf.Clamp01(checkpointTimer / 0.4f);

            // Boss bar
            if (activeBoss != null && bossFill != null)
            {
                float target = activeBoss.HealthNormalized;
                UIFactory.SetFill(bossFill, target);
                displayedBoss = Mathf.MoveTowards(displayedBoss, target, dt * 0.35f);
                if (displayedBoss < target) displayedBoss = target;
                if (bossFillDelayed != null) UIFactory.SetFill(bossFillDelayed, displayedBoss);
            }

            // Damage feedback
            damageFlash = Mathf.Max(0f, damageFlash - dt * 2.5f);
            if (damageVignette != null) damageVignette.color = UITheme.WithAlpha(UITheme.Danger, damageFlash * 0.45f);
            if (lowHealthVignette != null && player != null && player.Health != null)
            {
                float low = Mathf.Clamp01(1f - player.Health.Normalized / 0.35f);
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                lowHealthVignette.color = UITheme.WithAlpha(UITheme.Danger, player.Health.IsAlive ? low * 0.35f * pulse : 0f);
            }
            UpdateDamageIndicators(player);

            // Death overlay
            if (deathGroup != null)
            {
                deathTimer += dt;
                float target = dead ? Mathf.Clamp01(deathTimer / 0.3f) : 0f;
                deathGroup.alpha = Mathf.MoveTowards(deathGroup.alpha, target, dt * 4f);
            }
        }

        void UpdateDamageIndicators(PlayerController player)
        {
            if (damageIndicators == null || player == null || player.PlayerCamera == null) return;
            Transform cam = player.PlayerCamera.CameraTransform;
            for (int i = 0; i < damageIndicators.Length && i < indicatorState.Length; i++)
            {
                var img = damageIndicators[i];
                if (img == null) continue;
                float age = Time.time - indicatorState[i].time;
                if (age > 1.2f)
                {
                    if (img.enabled) img.enabled = false;
                    continue;
                }
                img.enabled = true;
                Vector3 to = indicatorState[i].source - cam.position;
                Vector3 local = cam.InverseTransformDirection(to);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                var rt = img.rectTransform;
                rt.localRotation = Quaternion.Euler(0f, 0f, -angle);
                rt.anchoredPosition = Quaternion.Euler(0f, 0f, -angle) * Vector3.up * 150f;
                img.color = UITheme.WithAlpha(UITheme.Danger, Mathf.Clamp01(1.2f - age) * 0.9f);
            }
        }

        // ------------------------------------------------------------------ Event handlers

        void OnHealthChanged(float current, float max)
        {
            if (healthFill != null) UIFactory.SetFill(healthFill, max > 0f ? current / max : 0f);
            if (healthText != null) healthText.text = Mathf.CeilToInt(current).ToString();
            if (healthFill != null) healthFill.color = current / Mathf.Max(1f, max) < 0.35f ? UITheme.Danger : UITheme.Good;
        }

        void OnPlayerDamaged(DamageInfo info)
        {
            damageFlash = Mathf.Clamp01(damageFlash + info.amount / 40f + 0.2f);
            if (info.type == DamageType.Fall) return;
            Vector3 source = info.source != null ? info.source.transform.position : info.point - info.direction * 5f;
            int slot = 0;
            float oldest = float.MaxValue;
            for (int i = 0; i < indicatorState.Length; i++)
            {
                if (indicatorState[i].time < oldest)
                {
                    oldest = indicatorState[i].time;
                    slot = i;
                }
            }
            indicatorState[slot].source = source;
            indicatorState[slot].time = Time.time;
        }

        void OnPlayerDied(PlayerController player, DamageInfo info)
        {
            dead = true;
            deathTimer = 0f;
            if (deathText != null) deathText.text = info.type == DamageType.Fall ? "FELL" : "TERMINATED";
            if (interactText != null) interactText.text = "";
        }

        void OnPlayerRespawned(PlayerController player)
        {
            dead = false;
            damageFlash = 0f;
            for (int i = 0; i < indicatorState.Length; i++) indicatorState[i].time = -999f;
        }

        void OnWeaponChanged(WeaponBase weapon)
        {
            var player = PlayerController.Current;
            var current = player != null && player.Weapons != null ? player.Weapons.Current : null;
            if (current == null)
            {
                if (weaponText != null) weaponText.text = "UNARMED";
                if (ammoText != null) ammoText.text = "-";
                if (reserveText != null) reserveText.text = "";
            }
            else
            {
                if (weaponText != null)
                {
                    weaponText.text = current.Definition.displayName.ToUpperInvariant();
                    weaponText.color = current.Definition.accentColor;
                }
                if (ammoText != null)
                {
                    ammoText.text = current.AmmoInMagazine.ToString();
                    ammoText.color = current.AmmoInMagazine == 0 ? UITheme.Danger : UITheme.Text;
                }
                if (reserveText != null) reserveText.text = current.HasInfiniteReserve ? "/ --" : "/ " + current.ReserveAmmo;
            }
            UpdateSlots(player);
        }

        void OnWeaponPickedUp(WeaponDefinition def)
        {
            UpdateSlots(PlayerController.Current);
        }

        void UpdateSlots(PlayerController player)
        {
            if (slotsText == null) return;
            sb.Clear();
            var weapons = player != null ? player.Weapons : null;
            for (int i = 0; i < WeaponManager.SlotCount; i++)
            {
                var w = weapons != null ? weapons.GetSlot(i) : null;
                if (w == null) continue;
                bool current = weapons.CurrentSlot == i;
                string color = current ? ColorUtility.ToHtmlStringRGB(UITheme.Accent) : "8A8F99";
                sb.Append("<color=#").Append(color).Append('>').Append(i + 1).Append("</color>  ");
            }
            slotsText.text = sb.ToString();
        }

        void OnHitConfirmed(bool killed, bool critical)
        {
            hitmarkerTimer = killed ? 0.35f : 0.18f;
            Color c = killed ? UITheme.Danger : critical ? UITheme.Gold : Color.white;
            foreach (var line in hitmarkerLines)
            {
                if (line != null) line.color = c;
            }
            if (hitmarkerGroup != null) hitmarkerGroup.transform.localScale = Vector3.one * (killed ? 1.4f : 1f);
            Audio.AudioManager.PlayUI(Audio.SoundId.HitMarker, killed ? 0.9f : 0.5f);
        }

        void OnNotification(string text, Color color)
        {
            if (notificationText == null) return;
            notificationText.text = text;
            notificationText.color = color;
            notificationTimer = 2.4f;
        }

        void OnCheckpoint(Checkpoint cp, float time, float delta)
        {
            if (checkpointText == null) return;
            sb.Clear();
            sb.Append("CHECKPOINT  ").Append(TimeFormat.Format(time));
            if (!float.IsNaN(delta))
            {
                string color = delta <= 0f ? "5CFF7A" : "FF5050";
                sb.Append("   <color=#").Append(color).Append('>').Append(TimeFormat.FormatDelta(delta)).Append("</color>");
            }
            checkpointText.text = sb.ToString();
            checkpointTimer = 2.5f;
        }

        void OnInteractPrompt(string prompt)
        {
            if (interactText == null) return;
            interactText.text = string.IsNullOrEmpty(prompt) ? "" : "[E]  " + prompt.ToUpperInvariant();
        }

        void OnBossStarted(BossBase boss)
        {
            activeBoss = boss;
            displayedBoss = 1f;
            SetBossVisible(true);
            if (bossNameText != null) bossNameText.text = boss.BossName;
            OnBossPhase(boss, boss.Phase);
        }

        void OnBossHealth(BossBase boss)
        {
            if (boss != activeBoss) return;
            if (bossPhaseText != null) bossPhaseText.text = boss.PhaseLabel;
        }

        void OnBossPhase(BossBase boss, int phase)
        {
            if (boss != activeBoss || bossPhaseText == null) return;
            bossPhaseText.text = boss.PhaseLabel;
        }

        void OnBossEnded(BossBase boss)
        {
            if (boss != activeBoss) return;
            activeBoss = null;
            SetBossVisible(false);
        }

        void OnRunFinished(LevelResult result)
        {
            if (interactText != null) interactText.text = "";
        }

        void SetBossVisible(bool visible)
        {
            if (bossRoot != null) bossRoot.gameObject.SetActive(visible);
        }

        public void SetVisible(bool visible)
        {
            var group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = visible ? 1f : 0f;
            gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------ Builder

        /// <summary>Builds the complete HUD hierarchy and wires all references.</summary>
        public static HUDView Build(Transform parent)
        {
            var canvas = UIFactory.CreateCanvas("HUD", 10, parent);
            canvas.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var root = canvas.transform;
            var hud = canvas.gameObject.AddComponent<HUDView>();

            // Vignettes (full screen, behind everything)
            hud.lowHealthVignette = UIFactory.CreateImage("LowHealthVignette", root, Color.clear);
            UIFactory.Stretch(hud.lowHealthVignette.rectTransform);
            hud.damageVignette = UIFactory.CreateImage("DamageVignette", root, Color.clear);
            UIFactory.Stretch(hud.damageVignette.rectTransform);

            // Top center: timer + PB
            hud.timerText = UIFactory.CreateText("Timer", root, "00:00.00", 60, TextAnchor.UpperCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(hud.timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 70f));
            hud.pbText = UIFactory.CreateText("PB", root, "PB  --:--.--", 26, TextAnchor.UpperCenter, UITheme.TextDim);
            UIFactory.Place(hud.pbText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(500f, 34f));
            hud.startHintText = UIFactory.CreateText("StartHint", root, "MOVE TO START", 24, TextAnchor.UpperCenter, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(hud.startHintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(600f, 30f));

            // Top left: level name
            hud.levelNameText = UIFactory.CreateText("LevelName", root, "", 24, TextAnchor.UpperLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(hud.levelNameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -28f), new Vector2(700f, 34f));

            // Boss bar
            var bossRoot = UIFactory.CreateRect("BossBar", root);
            UIFactory.Place(bossRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 90f));
            hud.bossRoot = bossRoot;
            hud.bossNameText = UIFactory.CreateText("BossName", bossRoot, "BOSS", 30, TextAnchor.UpperCenter, UITheme.Boss, FontStyle.Bold);
            UIFactory.Place(hud.bossNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 36f));
            var bossBg = UIFactory.CreateImage("BossBarBg", bossRoot, new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Place(bossBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 22f));
            hud.bossFillDelayed = UIFactory.CreateImage("BossFillDelayed", bossBg.rectTransform, new Color(1f, 1f, 1f, 0.5f));
            UIFactory.Stretch(hud.bossFillDelayed.rectTransform, 3f, 3f, 3f, 3f);
            MakeFilled(hud.bossFillDelayed);
            hud.bossFill = UIFactory.CreateImage("BossFill", bossBg.rectTransform, UITheme.Boss);
            UIFactory.Stretch(hud.bossFill.rectTransform, 3f, 3f, 3f, 3f);
            MakeFilled(hud.bossFill);
            hud.bossPhaseText = UIFactory.CreateText("BossPhase", bossRoot, "PHASE 1", 20, TextAnchor.UpperCenter, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(hud.bossPhaseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(900f, 26f));

            // Notifications
            hud.notificationText = UIFactory.CreateText("Notification", root, "", 40, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(hud.notificationText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1400f, 60f));
            hud.notificationGroup = hud.notificationText.gameObject.AddComponent<CanvasGroup>();
            hud.checkpointText = UIFactory.CreateText("Checkpoint", root, "", 30, TextAnchor.MiddleCenter, UITheme.Good, FontStyle.Bold);
            UIFactory.Place(hud.checkpointText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1200f, 44f));
            hud.checkpointGroup = hud.checkpointText.gameObject.AddComponent<CanvasGroup>();
            hud.interactText = UIFactory.CreateText("Interact", root, "", 24, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(hud.interactText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(900f, 34f));

            // Crosshair
            var cross = UIFactory.CreateRect("Crosshair", root);
            UIFactory.Place(cross, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            hud.crosshairRoot = cross;
            hud.crosshairDot = UIFactory.CreateImage("Dot", cross, Color.white);
            UIFactory.Place(hud.crosshairDot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));
            hud.crosshairLines = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var line = UIFactory.CreateImage("Line" + i, cross, new Color(1f, 1f, 1f, 0.9f));
                bool vertical = i < 2;
                UIFactory.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, vertical ? new Vector2(2f, 10f) : new Vector2(10f, 2f));
                hud.crosshairLines[i] = line;
            }
            hud.grappleRing = UIFactory.CreateImage("GrappleRing", cross, UITheme.Secondary);
            UIFactory.Place(hud.grappleRing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            var hitRoot = UIFactory.CreateRect("Hitmarker", cross);
            UIFactory.Place(hitRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            hud.hitmarkerGroup = hitRoot.gameObject.AddComponent<CanvasGroup>();
            hud.hitmarkerLines = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var line = UIFactory.CreateImage("Hit" + i, hitRoot, Color.white);
                float angle = 45f + 90f * i;
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
                UIFactory.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dir * 13f, new Vector2(3f, 11f));
                line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                hud.hitmarkerLines[i] = line;
            }

            // Damage indicators
            var indicatorRoot = UIFactory.CreateRect("DamageIndicators", root);
            UIFactory.Place(indicatorRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            hud.damageIndicatorRoot = indicatorRoot;
            hud.damageIndicators = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var img = UIFactory.CreateImage("Indicator" + i, indicatorRoot, Color.clear);
                UIFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 26f));
                img.enabled = false;
                hud.damageIndicators[i] = img;
            }

            // Bottom left: health
            var healthLabel = UIFactory.CreateText("HealthLabel", root, "HEALTH", 18, TextAnchor.LowerLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(healthLabel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(40f, 74f), new Vector2(200f, 24f));
            var healthBg = UIFactory.CreateImage("HealthBg", root, new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(healthBg.rectTransform, Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(380f, 26f));
            hud.healthFill = UIFactory.CreateImage("HealthFill", healthBg.rectTransform, UITheme.Good);
            UIFactory.Stretch(hud.healthFill.rectTransform, 3f, 3f, 3f, 3f);
            MakeFilled(hud.healthFill);
            hud.healthText = UIFactory.CreateText("HealthValue", root, "100", 44, TextAnchor.LowerLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(hud.healthText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(432f, 34f), new Vector2(160f, 56f));

            // Bottom center: speed
            hud.speedText = UIFactory.CreateText("Speed", root, "0", 46, TextAnchor.LowerCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(hud.speedText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(300f, 60f));
            var speedUnit = UIFactory.CreateText("SpeedUnit", root, "M/S", 18, TextAnchor.LowerCenter, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(speedUnit.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(200f, 22f));
            var speedBg = UIFactory.CreateImage("SpeedBg", root, new Color(0f, 0f, 0f, 0.45f));
            UIFactory.Place(speedBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(320f, 8f));
            hud.speedFill = UIFactory.CreateImage("SpeedFill", speedBg.rectTransform, UITheme.Secondary);
            UIFactory.Stretch(hud.speedFill.rectTransform);
            MakeFilled(hud.speedFill);

            // Bottom right: weapon & ammo
            hud.slotsText = UIFactory.CreateText("Slots", root, "", 22, TextAnchor.LowerRight, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(hud.slotsText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 150f), new Vector2(500f, 28f));
            hud.weaponText = UIFactory.CreateText("Weapon", root, "UNARMED", 26, TextAnchor.LowerRight, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(hud.weaponText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 112f), new Vector2(500f, 34f));
            hud.reserveText = UIFactory.CreateText("Reserve", root, "", 28, TextAnchor.LowerLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(hud.reserveText.rectTransform, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(-150f, 40f), new Vector2(120f, 40f));
            hud.ammoText = UIFactory.CreateText("Ammo", root, "-", 64, TextAnchor.LowerRight, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(hud.ammoText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-158f, 32f), new Vector2(220f, 80f));
            var reloadBg = UIFactory.CreateImage("ReloadBg", root, new Color(0f, 0f, 0f, 0f));
            UIFactory.Place(reloadBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(120f, 5f));
            hud.reloadFill = UIFactory.CreateImage("ReloadFill", reloadBg.rectTransform, UITheme.Accent);
            UIFactory.Stretch(hud.reloadFill.rectTransform);
            MakeFilled(hud.reloadFill);

            // Death overlay
            var death = UIFactory.CreateImage("Death", root, new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Stretch(death.rectTransform);
            hud.deathGroup = death.gameObject.AddComponent<CanvasGroup>();
            hud.deathText = UIFactory.CreateText("DeathText", death.rectTransform, "TERMINATED", 90, TextAnchor.MiddleCenter, UITheme.Danger, FontStyle.Bold);
            UIFactory.Stretch(hud.deathText.rectTransform);

            return hud;
        }

        static void MakeFilled(Image img)
        {
            // Fill is done by moving the right anchor (works without sprites).
            UIFactory.SetFill(img, 1f);
        }
    }
}
