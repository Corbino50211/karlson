using System.Globalization;
using System.Text;
using Momentum.Levels;
using Momentum.PlayerSystems;
using Momentum.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>F3 movement debug overlay: FPS, velocity, state flags, weapon and checkpoint.</summary>
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] Text text;
        [SerializeField] GameObject panel;

        readonly StringBuilder sb = new StringBuilder(512);
        float fps;
        float refresh;

        void Update()
        {
            var settings = SettingsManager.Settings;
            var player = PlayerController.Current;
            KeyCode toggleKey = player != null && player.InputHandler != null ? player.InputHandler.Bindings.debugOverlay : KeyCode.F3;
            if (Input.GetKeyDown(toggleKey))
            {
                settings.showDebugOverlay = !settings.showDebugOverlay;
                if (SettingsManager.Instance != null) SettingsManager.Instance.Save();
            }

            bool visible = settings.showDebugOverlay;
            if (panel != null && panel.activeSelf != visible) panel.SetActive(visible);
            if (!visible || text == null) return;

            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.08f);
            refresh -= dt;
            if (refresh > 0f) return;
            refresh = 0.05f;

            sb.Clear();
            sb.Append("FPS            ").Append(Mathf.RoundToInt(fps)).Append('\n');
            if (player == null || player.Movement == null)
            {
                sb.Append("No player");
                text.text = sb.ToString();
                return;
            }
            var m = player.Movement;
            Vector3 v = m.Velocity;
            sb.Append("Velocity       ").Append(F(v.x)).Append(", ").Append(F(v.y)).Append(", ").Append(F(v.z)).Append('\n');
            sb.Append("Speed          ").Append(F(m.Speed)).Append(" m/s\n");
            sb.Append("Horizontal     ").Append(F(m.HorizontalSpeed)).Append(" m/s\n");
            sb.Append("State          ").Append(m.State).Append('\n');
            sb.Append("Grounded       ").Append(m.IsGrounded).Append('\n');
            sb.Append("Crouching      ").Append(m.IsCrouching).Append('\n');
            sb.Append("Sliding        ").Append(m.IsSliding).Append('\n');
            sb.Append("Wallrunning    ").Append(m.IsWallRunning);
            if (m.IsWallRunning && m.WallRun != null) sb.Append(m.WallRun.WallSide < 0 ? " (left)" : " (right)");
            sb.Append('\n');
            sb.Append("Grappling      ").Append(m.IsGrappling);
            if (player.Grapple != null && player.Grapple.IsGrappling) sb.Append(" rope ").Append(F(player.Grapple.RopeLength)).Append('m');
            sb.Append('\n');
            sb.Append("Grapple target ").Append(player.Grapple != null && player.Grapple.HasTarget).Append('\n');
            sb.Append("Slope          ").Append(F(Vector3.Angle(m.GroundNormal, Vector3.up))).Append("°\n");
            var weapon = player.Weapons != null ? player.Weapons.Current : null;
            sb.Append("Weapon         ").Append(weapon != null ? weapon.Definition.displayName : "none");
            if (weapon != null) sb.Append(" (").Append(weapon.AmmoInMagazine).Append('/').Append(weapon.HasInfiniteReserve ? "inf" : weapon.ReserveAmmo.ToString()).Append(')');
            sb.Append('\n');
            var cps = CheckpointManager.Instance;
            sb.Append("Checkpoint     ");
            if (cps != null && cps.Current != null) sb.Append('#').Append(cps.IndexOf(cps.Current) + 1).Append(" (order ").Append(cps.Current.Order).Append(')');
            else sb.Append("start");
            if (cps != null) sb.Append("  ").Append(cps.ReachedCount).Append('/').Append(cps.CheckpointCount);
            sb.Append('\n');
            var level = LevelManager.Instance;
            if (level != null) sb.Append("Run            ").Append(level.State).Append("  deaths ").Append(level.Deaths);
            text.text = sb.ToString();
        }

        static string F(float v)
        {
            return v.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static DebugOverlay Build(Transform parent)
        {
            var canvas = UIFactory.CreateCanvas("DebugOverlay", 40, parent);
            var view = canvas.gameObject.AddComponent<DebugOverlay>();
            var panel = UIFactory.CreateImage("Panel", canvas.transform, new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(520f, 480f));
            view.panel = panel.gameObject;
            view.text = UIFactory.CreateText("Text", panel.rectTransform, "", 20, TextAnchor.UpperLeft, new Color(0.6f, 1f, 0.7f));
            UIFactory.Stretch(view.text.rectTransform, 16f, 16f, 14f, 14f);
            panel.gameObject.SetActive(false);
            return view;
        }
    }
}
