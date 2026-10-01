using System.Collections.Generic;
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
        public static void Generate(ScriptableObjectGenerator.Assets assets, List<LevelDefinition> definitions)
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
                var definition = definitions.Find(d => d != null && d.id == entry.id);
                SceneGenerator.GenerateCampaignScene(entry, data, definition, assets.prefabs);
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
