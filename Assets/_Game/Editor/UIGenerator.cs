using Momentum.LevelEditor;
using Momentum.UI;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Saves the procedurally built uGUI screens (HUD, pause menu, results, main menu, level editor UI) as
    /// prefabs so they can be restyled in the Inspector. The runtime instantiates these prefabs and falls
    /// back to building the same layout in code if a prefab is missing, so the game never shows no UI.
    /// </summary>
    public static class UIGenerator
    {
        public static void Generate(PrefabRegistry registry)
        {
            EditorUtil.EnsureFolder(GamePaths.UIPrefabs);
            registry.hud = Save(HUDView.Build(null).gameObject, "HUD");
            registry.pauseMenu = Save(PauseMenuView.Build(null).gameObject, "PauseMenu");
            registry.resultsScreen = Save(ResultsView.Build(null).gameObject, "ResultsScreen");
            registry.mainMenu = Save(MainMenuView.Build(null).gameObject, "MainMenu");
            registry.levelEditorUI = Save(LevelEditorUI.Build(null).gameObject, "LevelEditorUI");
        }

        static GameObject Save(GameObject root, string name)
        {
            // Builders return the component on the canvas; save the canvas root.
            var top = root.transform.root.gameObject;
            return EditorUtil.SavePrefab(top, GamePaths.UIPrefabs + "/" + name + ".prefab");
        }
    }
}
