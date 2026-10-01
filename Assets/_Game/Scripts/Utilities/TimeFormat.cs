using System.Globalization;
using UnityEngine;

namespace Momentum
{
    /// <summary>Formatting helpers for run times and splits.</summary>
    public static class TimeFormat
    {
        /// <summary>Formats seconds as mm:ss.ff (or "--:--.--" for negative / invalid times).</summary>
        public static string Format(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return "--:--.--";
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float rem = seconds - minutes * 60f;
            int wholeSeconds = Mathf.FloorToInt(rem);
            int hundredths = Mathf.FloorToInt((rem - wholeSeconds) * 100f);
            if (hundredths > 99) hundredths = 99;
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}.{2:00}", minutes, wholeSeconds, hundredths);
        }

        /// <summary>Formats a signed delta like "+1.23" / "-0.45".</summary>
        public static string FormatDelta(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta)) return string.Empty;
            string sign = delta >= 0f ? "+" : "-";
            return sign + Mathf.Abs(delta).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
