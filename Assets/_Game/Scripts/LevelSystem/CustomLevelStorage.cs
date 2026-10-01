using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Summary of a saved custom level file.</summary>
    public struct LevelFileInfo
    {
        public string path;
        public string fileName;
        public string levelName;
        public string author;
        public string levelId;
        public int objectCount;
        public DateTime modified;
    }

    /// <summary>
    /// Reads/writes custom maps as readable JSON in Application.persistentDataPath/Levels/.
    /// </summary>
    public static class CustomLevelStorage
    {
        public static string LevelsDirectory
        {
            get
            {
                string dir = SaveManager.CustomLevelsDirectory;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static List<LevelFileInfo> ListLevels()
        {
            var result = new List<LevelFileInfo>();
            string dir;
            try
            {
                dir = LevelsDirectory;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Momentum] Cannot access levels folder: " + e.Message);
                return result;
            }
            foreach (var path in Directory.GetFiles(dir, "*.json"))
            {
                var data = Load(path);
                if (data == null) continue;
                result.Add(new LevelFileInfo
                {
                    path = path,
                    fileName = Path.GetFileName(path),
                    levelName = data.levelName,
                    author = data.author,
                    levelId = data.levelId,
                    objectCount = data.objects.Count,
                    modified = File.GetLastWriteTime(path)
                });
            }
            result.Sort((a, b) => b.modified.CompareTo(a.modified));
            return result;
        }

        public static LevelData Load(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                return LevelData.FromJson(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Momentum] Failed to load level '{path}': {e.Message}");
                return null;
            }
        }

        /// <summary>Saves the level. Returns the file path, or null on failure.</summary>
        public static string Save(LevelData data, string existingPath = null)
        {
            if (data == null) return null;
            data.Sanitize();
            if (string.IsNullOrEmpty(data.levelId)) data.levelId = Guid.NewGuid().ToString("N");
            string now = DateTime.UtcNow.ToString("o");
            if (string.IsNullOrEmpty(data.createdUtc)) data.createdUtc = now;
            data.modifiedUtc = now;

            string path = existingPath;
            if (string.IsNullOrEmpty(path))
            {
                string baseName = MakeSafeFileName(data.levelName);
                path = Path.Combine(LevelsDirectory, baseName + ".json");
                int n = 2;
                while (File.Exists(path))
                {
                    var existing = Load(path);
                    if (existing != null && existing.levelId == data.levelId) break;
                    path = Path.Combine(LevelsDirectory, baseName + "_" + n + ".json");
                    n++;
                }
            }
            return JsonFileUtility.Write(path, data) ? path : null;
        }

        public static bool Delete(string path)
        {
            return JsonFileUtility.Delete(path);
        }

        public static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Untitled";
            var sb = new StringBuilder();
            foreach (char c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
                else if (c == ' ') sb.Append('_');
            }
            string s = sb.ToString();
            if (s.Length == 0) s = "Untitled";
            if (s.Length > 48) s = s.Substring(0, 48);
            return s;
        }

        /// <summary>Key used for best times of custom levels in the save file.</summary>
        public static string RecordKey(LevelData data)
        {
            return "custom:" + (data != null && !string.IsNullOrEmpty(data.levelId) ? data.levelId : "unknown");
        }
    }
}
