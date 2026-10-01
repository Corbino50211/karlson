using System;
using System.Collections.Generic;

namespace Momentum.SaveSystem
{
    /// <summary>Per-level progress record. Serialized to save.json with JsonUtility.</summary>
    [Serializable]
    public class LevelRecord
    {
        public string levelId = "";
        public bool unlocked;
        public bool completed;
        public float bestTime = -1f;
        public string bestRank = "";
        /// <summary>Best time ever recorded at each checkpoint index (independent of the PB run).</summary>
        public List<float> bestSplits = new List<float>();
        /// <summary>Checkpoint times of the personal-best run.</summary>
        public List<float> pbSplits = new List<float>();
        public int attempts;
        public int completions;
        public int deaths;
        public string lastCompletedUtc = "";

        public bool HasBestTime => bestTime > 0f;
    }

    /// <summary>Root object written to Application.persistentDataPath/save.json.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public bool unlockAll;
        public string lastPlayedLevelId = "";
        public float totalPlayTime;
        public int totalDeaths;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public List<string> secretsFound = new List<string>();
    }

    /// <summary>Result of recording a completed run.</summary>
    public struct RunRecordResult
    {
        public bool isNewBest;
        public float previousBest;
        public string previousRank;
        public bool firstCompletion;
    }
}
