using System.Collections;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Floor panel that can be electrified. Either cycles on a timer (level hazard) or is triggered by
    /// bosses (The Sentinel). Warns (yellow) before becoming active (red) and damages anyone standing on it.
    /// The trigger volume sits on top of the solid panel collider.
    /// </summary>
    public class HazardPanel : MonoBehaviour, ILevelObjectConfigurable
    {
        public static readonly List<HazardPanel> All = new List<HazardPanel>();

        enum PanelState { Idle, Warning, Active }

        [SerializeField] Renderer panelRenderer;
        [SerializeField] BoxCollider damageTrigger;
        [SerializeField] float damagePerSecond = 35f;
        [SerializeField] float knockUp = 6f;
        [Tooltip("When > 0 the panel cycles automatically: off for this long, then warns and turns on.")]
        [SerializeField] float cycleOffTime;
        [SerializeField] float cycleOnTime = 2f;
        [SerializeField] float cycleWarnTime = 0.8f;
        [SerializeField] Color idleColor = new Color(0.25f, 0.25f, 0.3f);
        [SerializeField] Color warnColor = new Color(1f, 0.8f, 0.1f);
        [SerializeField] Color activeColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] float triggerHeight = 0.8f;

        PanelState state = PanelState.Idle;
        MaterialPropertyBlock block;
        Coroutine routine;
        float nextDamageTick;

        public bool IsActive => state == PanelState.Active;

        void FitTrigger()
        {
            if (damageTrigger == null) return;
            float sy = Mathf.Max(0.01f, transform.lossyScale.y);
            float h = triggerHeight / sy;
            damageTrigger.isTrigger = true;
            damageTrigger.center = new Vector3(0f, 0.5f + h * 0.5f, 0f);
            damageTrigger.size = new Vector3(1f, h, 1f);
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            SetState(PanelState.Idle);
        }

        void OnDisable()
        {
            All.Remove(this);
            routine = null;
        }

        void Start()
        {
            FitTrigger();
            if (cycleOffTime > 0f) routine = StartCoroutine(CycleRoutine());
        }

        /// <summary>Warns for 'warn' seconds then stays active for 'active' seconds.</summary>
        public void Trigger(float warn, float active)
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(TriggerRoutine(warn, active));
        }

        public void ForceIdle()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            SetState(PanelState.Idle);
            if (cycleOffTime > 0f && isActiveAndEnabled) routine = StartCoroutine(CycleRoutine());
        }

        IEnumerator TriggerRoutine(float warn, float active)
        {
            SetState(PanelState.Warning);
            yield return new WaitForSeconds(warn);
            SetState(PanelState.Active);
            AudioManager.Play(SoundId.LaserFire, transform.position, 0.35f, 0.7f);
            yield return new WaitForSeconds(active);
            SetState(PanelState.Idle);
            routine = null;
            if (cycleOffTime > 0f) routine = StartCoroutine(CycleRoutine());
        }

        IEnumerator CycleRoutine()
        {
            var wait = new WaitForSeconds(cycleOffTime);
            while (true)
            {
                SetState(PanelState.Idle);
                yield return wait;
                SetState(PanelState.Warning);
                yield return new WaitForSeconds(cycleWarnTime);
                SetState(PanelState.Active);
                yield return new WaitForSeconds(cycleOnTime);
            }
        }

        void SetState(PanelState s)
        {
            state = s;
            if (panelRenderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            Color c = s == PanelState.Active ? activeColor : s == PanelState.Warning ? warnColor : idleColor;
            panelRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", c);
            block.SetColor("_EmissionColor", s == PanelState.Idle ? Color.black : c * 2f);
            panelRenderer.SetPropertyBlock(block);
        }

        void OnTriggerStay(Collider other)
        {
            if (state != PanelState.Active || Time.time < nextDamageTick) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;
            nextDamageTick = Time.time + 0.25f;
            var info = new DamageInfo(damagePerSecond * 0.25f, DamageType.Hazard, gameObject, player.FeetPosition, Vector3.up);
            player.Health.TakeDamage(info);
            if (knockUp > 0f && player.Movement != null && player.Movement.IsGrounded) player.ApplyKnockback(Vector3.up * knockUp, 0f);
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            damagePerSecond = data.GetFloat("dps", damagePerSecond);
            cycleOffTime = data.GetFloat("cycleOff", cycleOffTime);
            cycleOnTime = data.GetFloat("cycleOn", cycleOnTime);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}
