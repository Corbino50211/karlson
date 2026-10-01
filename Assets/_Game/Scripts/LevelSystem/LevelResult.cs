using System.Collections.Generic;

namespace Momentum.Levels
{
    /// <summary>Everything the results screen needs to know about a finished run.</summary>
    public class LevelResult
    {
        public string levelId;
        public string levelName;
        public float time;
        public Rank rank;
        public float previousBest = -1f;
        public bool isNewBest;
        public bool firstCompletion;
        public bool isCustomLevel;
        public bool isEditorTest;
        public bool hasNextLevel;
        public int deaths;
        public int kills;
        public int secretsFound;
        public int secretsTotal;
        public RankThresholds thresholds;
        public readonly List<float> splits = new List<float>();
        /// <summary>Delta of each split versus the personal-best run (NaN when unknown).</summary>
        public readonly List<float> splitDeltas = new List<float>();
    }
}
