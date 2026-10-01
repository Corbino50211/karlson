using System;
using System.Collections.Generic;
using System.IO;
using Momentum.Levels;
using UnityEngine;

namespace Momentum.SaveSystem
{
    /// <summary>
    /// Owns the player's progress (completion, best times, ranks, splits, unlocks) and writes it as readable
    /// JSON to Application.persistentDataPath/save.json. Custom maps are stored separately in /Levels/.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public const string SaveFileName = "save.json";
        public const string SettingsFileName = "settings.json";

        public static SaveManager Instance { get; private set; }

        public SaveData Data { get; private set; } = new SaveData();

        public static string SaveDirectory => Application.persistentDataPath;
        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static string SettingsFilePath => Path.Combine(Application.persistentDataPath, SettingsFileName);

        public static string CustomLevelsDirectory
        {
            get
            {
                string folder = GameConfig.Instance != null && !string.IsNullOrEmpty(GameConfig.Instance.customLevelsFolder)
                    ? GameConfig.Instance.customLevelsFolder
                    : "Levels";
                return Path.Combine(Application.persistentDataPath, folder);
            }
        }

        bool dirty;
        float autosaveTimer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Load();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                if (dirty) Save();
                Instance = null;
            }
        }

        void Update()
        {
            Data.totalPlayTime += Time.unscaledDeltaTime;
            autosaveTimer += Time.unscaledDeltaTime;
            if (dirty && autosaveTimer > 5f)
            {
                Save();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit()
        {
            Save();
        }

        // ------------------------------------------------------------------ IO

        public void Load()
        {
            if (JsonFileUtility.TryRead(SaveFilePath, out SaveData loaded))
            {
                Data = loaded;
                if (Data.levels == null) Data.levels = new List<LevelRecord>();
                if (Data.secretsFound == null) Data.secretsFound = new List<string>();
                foreach (var r in Data.levels)
                {
                    if (r.bestSplits == null) r.bestSplits = new List<float>();
                    if (r.pbSplits == null) r.pbSplits = new List<float>();
                }
            }
            else
            {
                Data = new SaveData();
            }
            dirty = false;
        }

        public void Save()
        {
            autosaveTimer = 0f;
            if (JsonFileUtility.Write(SaveFilePath, Data)) dirty = false;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        /// <summary>Deletes the progress file (settings and custom maps are kept).</summary>
        public void ClearProgress()
        {
            Data = new SaveData();
            JsonFileUtility.Delete(SaveFilePath);
            dirty = false;
        }

        /// <summary>Static helper usable from editor menus without a running game.</summary>
        public static void DeleteSaveFiles(bool includeSettings)
        {
            JsonFileUtility.Delete(SaveFilePath);
            if (includeSettings) JsonFileUtility.Delete(SettingsFilePath);
            if (Instance != null) Instance.Data = new SaveData();
        }

        /// <summary>Static helper: writes unlockAll = true into the save file (works outside Play Mode).</summary>
        public static void WriteUnlockAll()
        {
            if (Instance != null)
            {
                Instance.UnlockAll();
                return;
            }
            if (!JsonFileUtility.TryRead(SaveFilePath, out SaveData data)) data = new SaveData();
            data.unlockAll = true;
            JsonFileUtility.Write(SaveFilePath, data);
        }

        // ------------------------------------------------------------------ Records

        public LevelRecord GetRecord(string levelId, bool create = false)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            foreach (var r in Data.levels)
            {
                if (r.levelId == levelId) return r;
            }
            if (!create) return null;
            var record = new LevelRecord { levelId = levelId };
            Data.levels.Add(record);
            MarkDirty();
            return record;
        }

        public float GetBestTime(string levelId)
        {
            var r = GetRecord(levelId);
            return r != null ? r.bestTime : -1f;
        }

        public bool IsCompleted(string levelId)
        {
            var r = GetRecord(levelId);
            return r != null && r.completed;
        }

        public bool IsUnlocked(LevelDefinition level)
        {
            if (level == null) return false;
            var config = GameConfig.Instance;
            if (config.unlockAllLevels || Data.unlockAll) return true;
            var record = GetRecord(level.id);
            if (record != null && (record.unlocked || record.completed)) return true;
            var registry = config.levelRegistry;
            if (registry == null) return true;
            int index = registry.IndexOf(level);
            if (index <= 0) return true;
            var previous = registry.GetByIndex(index - 1);
            return previous != null && IsCompleted(previous.id);
        }

        public void UnlockLevel(string levelId)
        {
            var r = GetRecord(levelId, true);
            if (!r.unlocked)
            {
                r.unlocked = true;
                MarkDirty();
            }
        }

        public void UnlockAll()
        {
            Data.unlockAll = true;
            Save();
        }

        public void SetLastPlayed(string levelId)
        {
            Data.lastPlayedLevelId = levelId ?? "";
            MarkDirty();
        }

        public void RecordAttempt(string levelId)
        {
            var r = GetRecord(levelId, true);
            r.attempts++;
            MarkDirty();
        }

        public void RecordDeath(string levelId)
        {
            Data.totalDeaths++;
            var r = GetRecord(levelId, true);
            if (r != null) r.deaths++;
            MarkDirty();
        }

        /// <summary>Records a finished run. Updates PB, rank, splits and unlocks the next campaign level.</summary>
        public RunRecordResult RecordRun(string levelId, float time, string rank, IList<float> splits)
        {
            var result = new RunRecordResult();
            var r = GetRecord(levelId, true);
            result.previousBest = r.bestTime;
            result.previousRank = r.bestRank;
            result.firstCompletion = !r.completed;

            r.completed = true;
            r.unlocked = true;
            r.completions++;
            r.lastCompletedUtc = DateTime.UtcNow.ToString("o");

            if (!r.HasBestTime || time < r.bestTime)
            {
                result.isNewBest = true;
                r.bestTime = time;
                r.bestRank = rank;
                r.pbSplits = splits != null ? new List<float>(splits) : new List<float>();
            }
            else if (RankCalculator.IsBetter(rank, r.bestRank))
            {
                r.bestRank = rank;
            }

            if (splits != null)
            {
                for (int i = 0; i < splits.Count; i++)
                {
                    TryUpdateBestSplit(levelId, i, splits[i], out _);
                }
            }

            // Unlock the next campaign level.
            var registry = GameConfig.Instance.levelRegistry;
            if (registry != null)
            {
                var def = registry.FindById(levelId);
                var next = def != null ? registry.GetNext(def) : null;
                if (next != null) UnlockLevel(next.id);
            }

            Save();
            return result;
        }

        /// <summary>Best time ever reached at the given checkpoint index, or -1.</summary>
        public float GetBestSplit(string levelId, int index)
        {
            var r = GetRecord(levelId);
            if (r == null || index < 0 || index >= r.bestSplits.Count) return -1f;
            return r.bestSplits[index];
        }

        public float GetPbSplit(string levelId, int index)
        {
            var r = GetRecord(levelId);
            if (r == null || index < 0 || index >= r.pbSplits.Count) return -1f;
            return r.pbSplits[index];
        }

        public bool TryUpdateBestSplit(string levelId, int index, float time, out float previous)
        {
            previous = -1f;
            if (index < 0) return false;
            var r = GetRecord(levelId, true);
            while (r.bestSplits.Count <= index) r.bestSplits.Add(-1f);
            previous = r.bestSplits[index];
            if (previous < 0f || time < previous)
            {
                r.bestSplits[index] = time;
                MarkDirty();
                return true;
            }
            return false;
        }

        public bool RegisterSecret(string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey) || Data.secretsFound.Contains(secretKey)) return false;
            Data.secretsFound.Add(secretKey);
            MarkDirty();
            return true;
        }
    }
}
