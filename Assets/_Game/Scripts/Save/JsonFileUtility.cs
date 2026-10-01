using System;
using System.IO;
using UnityEngine;

namespace Momentum.SaveSystem
{
    /// <summary>Readable JSON persistence helpers with atomic writes.</summary>
    public static class JsonFileUtility
    {
        public static bool TryRead<T>(string path, out T data) where T : class
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Momentum] Failed to read '{path}': {e.Message}");
                return false;
            }
        }

        public static bool Write(string path, object data, bool prettyPrint = true)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                string json = JsonUtility.ToJson(data, prettyPrint);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Momentum] Failed to write '{path}': {e.Message}");
                return false;
            }
        }

        public static bool Delete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Momentum] Failed to delete '{path}': {e.Message}");
            }
            return false;
        }
    }
}
