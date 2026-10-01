using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>A key/value property on a level object (stored as strings for a forgiving JSON format).</summary>
    [Serializable]
    public class LevelProperty
    {
        public string key;
        public string value;

        public LevelProperty() { }

        public LevelProperty(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>
    /// One placed object in a level: type, transform and type-specific properties.
    /// Enemies, pickups, checkpoints etc. are all LevelObjectData with different objectType values;
    /// see LevelObjectCatalog for the available types and their properties.
    /// </summary>
    [Serializable]
    public class LevelObjectData
    {
        public int id;
        public string objectType = "Cube";
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public List<LevelProperty> properties = new List<LevelProperty>();

        public LevelObjectData() { }

        public LevelObjectData(string type, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            objectType = type;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public bool Has(string key)
        {
            return FindProperty(key) != null;
        }

        LevelProperty FindProperty(string key)
        {
            if (properties == null) return null;
            foreach (var p in properties)
            {
                if (p != null && string.Equals(p.key, key, StringComparison.OrdinalIgnoreCase)) return p;
            }
            return null;
        }

        public string GetString(string key, string fallback = "")
        {
            var p = FindProperty(key);
            return p != null && p.value != null ? p.value : fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            var p = FindProperty(key);
            if (p != null && float.TryParse(p.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) return v;
            return fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            var p = FindProperty(key);
            if (p != null && int.TryParse(p.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) return v;
            if (p != null && float.TryParse(p.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return Mathf.RoundToInt(f);
            return fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            var p = FindProperty(key);
            if (p == null || string.IsNullOrEmpty(p.value)) return fallback;
            string v = p.value.Trim().ToLowerInvariant();
            if (v == "true" || v == "1" || v == "yes" || v == "on") return true;
            if (v == "false" || v == "0" || v == "no" || v == "off") return false;
            return fallback;
        }

        public Vector3 GetVector3(string key, Vector3 fallback)
        {
            var p = FindProperty(key);
            return p != null ? ParseVector3(p.value, fallback) : fallback;
        }

        public Color GetColor(string key, Color fallback)
        {
            var p = FindProperty(key);
            if (p == null || string.IsNullOrEmpty(p.value)) return fallback;
            if (ColorUtility.TryParseHtmlString(p.value, out Color c)) return c;
            var v = ParseVector3(p.value, new Vector3(fallback.r, fallback.g, fallback.b));
            return new Color(v.x, v.y, v.z, 1f);
        }

        public void Set(string key, string value)
        {
            var p = FindProperty(key);
            if (p != null) p.value = value;
            else
            {
                if (properties == null) properties = new List<LevelProperty>();
                properties.Add(new LevelProperty(key, value));
            }
        }

        public void Set(string key, float value) => Set(key, value.ToString("0.###", CultureInfo.InvariantCulture));
        public void Set(string key, int value) => Set(key, value.ToString(CultureInfo.InvariantCulture));
        public void Set(string key, bool value) => Set(key, value ? "true" : "false");
        public void Set(string key, Vector3 value) => Set(key, FormatVector3(value));
        public void Set(string key, Color value) => Set(key, "#" + ColorUtility.ToHtmlStringRGB(value));

        public void Remove(string key)
        {
            if (properties == null) return;
            properties.RemoveAll(p => p != null && string.Equals(p.key, key, StringComparison.OrdinalIgnoreCase));
        }

        public LevelObjectData Clone()
        {
            var c = new LevelObjectData(objectType, position, rotation, scale) { id = id };
            if (properties != null)
            {
                foreach (var p in properties)
                {
                    if (p != null) c.properties.Add(new LevelProperty(p.key, p.value));
                }
            }
            return c;
        }

        public static string FormatVector3(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.###},{1:0.###},{2:0.###}", v.x, v.y, v.z);
        }

        public static Vector3 ParseVector3(string s, Vector3 fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            var parts = s.Split(',');
            if (parts.Length < 3) return fallback;
            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return new Vector3(x, y, z);
            }
            return fallback;
        }
    }

    /// <summary>Lighting / sky / fog settings stored with a level.</summary>
    [Serializable]
    public class LevelEnvironmentSettings
    {
        public string preset = "Day";
        public string skybox = "Day";
        public Color ambientSky = new Color(0.72f, 0.78f, 0.88f);
        public Color ambientEquator = new Color(0.55f, 0.57f, 0.62f);
        public Color ambientGround = new Color(0.32f, 0.32f, 0.35f);
        public Color sunColor = new Color(1f, 0.96f, 0.9f);
        public float sunIntensity = 1.15f;
        public Vector3 sunRotation = new Vector3(50f, -35f, 0f);
        public bool fog = true;
        public Color fogColor = new Color(0.78f, 0.82f, 0.88f);
        public float fogDensity = 0.004f;

        public LevelEnvironmentSettings Clone()
        {
            return (LevelEnvironmentSettings)MemberwiseClone();
        }
    }

    /// <summary>
    /// Serializable level format used by custom maps (Application.persistentDataPath/Levels/*.json),
    /// the in-game level editor and the campaign generator.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public const int CurrentFormatVersion = 1;

        public int formatVersion = CurrentFormatVersion;
        public string levelId = "";
        public string levelName = "Untitled";
        public string author = "";
        public string description = "";
        public string createdUtc = "";
        public string modifiedUtc = "";
        public float killHeight = -40f;
        public RankThresholds rankTimes = RankThresholds.Default;
        public string music = "Level";
        public List<string> startingWeapons = new List<string>();
        public LevelEnvironmentSettings environment = new LevelEnvironmentSettings();
        public List<LevelObjectData> objects = new List<LevelObjectData>();

        public int NextId()
        {
            int max = 0;
            foreach (var o in objects)
            {
                if (o != null && o.id > max) max = o.id;
            }
            return max + 1;
        }

        public LevelObjectData Find(int id)
        {
            foreach (var o in objects)
            {
                if (o != null && o.id == id) return o;
            }
            return null;
        }

        public int Count(string objectType)
        {
            int n = 0;
            foreach (var o in objects)
            {
                if (o != null && string.Equals(o.objectType, objectType, StringComparison.OrdinalIgnoreCase)) n++;
            }
            return n;
        }

        public LevelObjectData FindFirst(string objectType)
        {
            foreach (var o in objects)
            {
                if (o != null && string.Equals(o.objectType, objectType, StringComparison.OrdinalIgnoreCase)) return o;
            }
            return null;
        }

        /// <summary>Assigns ids to objects missing one and fills nulls after loading.</summary>
        public void Sanitize()
        {
            if (objects == null) objects = new List<LevelObjectData>();
            if (startingWeapons == null) startingWeapons = new List<string>();
            if (environment == null) environment = new LevelEnvironmentSettings();
            objects.RemoveAll(o => o == null || string.IsNullOrEmpty(o.objectType));
            var used = new HashSet<int>();
            int next = NextId();
            foreach (var o in objects)
            {
                if (o.properties == null) o.properties = new List<LevelProperty>();
                if (o.scale == Vector3.zero) o.scale = Vector3.one;
                if (o.id <= 0 || used.Contains(o.id)) o.id = next++;
                used.Add(o.id);
            }
            if (!rankTimes.IsValid) rankTimes = RankThresholds.Default;
            if (string.IsNullOrEmpty(levelName)) levelName = "Untitled";
        }

        public string ToJson(bool pretty = true)
        {
            return JsonUtility.ToJson(this, pretty);
        }

        public static LevelData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var data = JsonUtility.FromJson<LevelData>(json);
                data?.Sanitize();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Momentum] Invalid level JSON: " + e.Message);
                return null;
            }
        }

        public LevelData DeepClone()
        {
            return FromJson(ToJson(false));
        }
    }

    /// <summary>Implemented by components that read their settings from LevelObjectData properties.</summary>
    public interface ILevelObjectConfigurable
    {
        void ApplyLevelProperties(LevelObjectData data);
    }
}
