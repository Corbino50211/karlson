using System.IO;
using Momentum.Levels;
using UnityEditor;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Builds the ten campaign scenes from CampaignLevels and exports each layout as JSON
    /// (Assets/_Game/Levels/Campaign) so it can be loaded, studied or remixed in the in-game editor.
    /// </summary>
    public static class DemoLevelGenerator
    {
        /// <summary>
        /// Builds every campaign scene. Assets are looked up again for each scene (through GameConfig) because
        /// opening a scene can unload assets that are only referenced from editor code.
        /// </summary>
        public static void Generate()
        {
            EditorUtil.EnsureFolder(GamePaths.LevelScenes);
            EditorUtil.EnsureFolder(GamePaths.CampaignJson);
            int index = 0;
            foreach (var entry in CampaignLevels.All)
            {
                index++;
                EditorUtility.DisplayProgressBar("MOMENTUM Setup", "Building level " + entry.number + ": " + entry.name, index / (float)CampaignLevels.All.Length);
                var data = entry.build();
                ExportJson(entry, data);
                SceneGenerator.GenerateCampaignScene(entry, data);
                Debug.Log($"[Momentum Setup] Built {entry.scene} ({data.objects.Count} objects).");
            }
            EditorUtility.ClearProgressBar();
        }

        static void ExportJson(CampaignLevels.Entry entry, LevelData data)
        {
            data.createdUtc = "2024-01-01T00:00:00Z";
            data.modifiedUtc = data.createdUtc;
            data.Sanitize();
            string path = GamePaths.CampaignJson + "/" + entry.scene + ".json";
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset(path);
        }
    }
}
