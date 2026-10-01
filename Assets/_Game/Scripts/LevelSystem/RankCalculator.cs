using System;
using UnityEngine;

namespace Momentum.Levels
{
    public enum Rank
    {
        S = 0,
        A = 1,
        B = 2,
        C = 3,
        D = 4
    }

    /// <summary>Time limits (seconds) for each rank. A time at or below the limit earns the rank.</summary>
    [Serializable]
    public struct RankThresholds
    {
        public float s;
        public float a;
        public float b;
        public float c;

        public RankThresholds(float s, float a, float b, float c)
        {
            this.s = s;
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public static RankThresholds Default => new RankThresholds(45f, 60f, 90f, 150f);

        public bool IsValid => s > 0f && a >= s && b >= a && c >= b;

        public float ForRank(Rank rank)
        {
            switch (rank)
            {
                case Rank.S: return s;
                case Rank.A: return a;
                case Rank.B: return b;
                case Rank.C: return c;
                default: return float.PositiveInfinity;
            }
        }
    }

    /// <summary>Computes completion ranks from run times.</summary>
    public static class RankCalculator
    {
        public static Rank Calculate(float time, RankThresholds t)
        {
            if (!t.IsValid) t = RankThresholds.Default;
            if (time <= t.s) return Rank.S;
            if (time <= t.a) return Rank.A;
            if (time <= t.b) return Rank.B;
            if (time <= t.c) return Rank.C;
            return Rank.D;
        }

        public static string ToLetter(Rank rank) => rank.ToString();

        public static bool TryParse(string letter, out Rank rank)
        {
            rank = Rank.D;
            if (string.IsNullOrEmpty(letter)) return false;
            return Enum.TryParse(letter.Trim(), true, out rank);
        }

        /// <summary>True when newRank is strictly better than oldRank (empty counts as worst).</summary>
        public static bool IsBetter(string newRank, string oldRank)
        {
            if (!TryParse(newRank, out var n)) return false;
            if (!TryParse(oldRank, out var o)) return true;
            return (int)n < (int)o;
        }

        public static Color GetColor(Rank rank)
        {
            switch (rank)
            {
                case Rank.S: return new Color(1f, 0.82f, 0.2f);
                case Rank.A: return new Color(0.35f, 1f, 0.5f);
                case Rank.B: return new Color(0.3f, 0.75f, 1f);
                case Rank.C: return new Color(0.8f, 0.6f, 1f);
                default: return new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public static Color GetColor(string letter)
        {
            return TryParse(letter, out var r) ? GetColor(r) : new Color(0.5f, 0.5f, 0.5f);
        }
    }
}
