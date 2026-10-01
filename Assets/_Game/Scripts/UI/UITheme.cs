using UnityEngine;

namespace Momentum.UI
{
    /// <summary>Shared UI colors and sizes. Accent colors come from GameConfig so they can be re-themed.</summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.06f, 0.065f, 0.08f, 0.94f);
        public static readonly Color Panel = new Color(0.11f, 0.115f, 0.14f, 0.96f);
        public static readonly Color PanelLight = new Color(0.17f, 0.18f, 0.21f, 0.98f);
        public static readonly Color Button = new Color(0.2f, 0.21f, 0.25f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.14f, 0.14f, 0.16f, 0.7f);
        public static readonly Color Text = new Color(0.95f, 0.95f, 0.97f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.64f, 0.7f, 1f);
        public static readonly Color Danger = new Color(1f, 0.25f, 0.25f, 1f);
        public static readonly Color Good = new Color(0.35f, 1f, 0.5f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.6f);

        public static Color Accent => GameConfig.Instance != null ? GameConfig.Instance.accentColor : new Color(1f, 0.45f, 0.1f);
        public static Color Secondary => GameConfig.Instance != null ? GameConfig.Instance.secondaryColor : new Color(0.15f, 0.85f, 1f);
        public static Color Boss => GameConfig.Instance != null ? GameConfig.Instance.bossColor : new Color(0.9f, 0.12f, 0.2f);

        public const int TitleSize = 96;
        public const int HeaderSize = 44;
        public const int ButtonSize = 30;
        public const int BodySize = 26;
        public const int SmallSize = 20;

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
