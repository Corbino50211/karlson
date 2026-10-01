using Momentum.Audio;
using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Creates the persistent systems object before the first scene loads, so any scene (menu, level,
    /// editor) can be entered directly in Play Mode without a dedicated boot scene.
    /// </summary>
    public static class GameBootstrap
    {
        public static GameObject SystemsRoot { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var config = GameConfig.Instance;
            Time.fixedDeltaTime = Mathf.Clamp(config.fixedTimestep, 0.005f, 0.04f);
            Time.maximumDeltaTime = 0.1f;
            Physics.queriesHitTriggers = false;
            Physics.gravity = new Vector3(0f, -25f, 0f);
            Layers.ApplyCollisionMatrix();
            Application.runInBackground = true;

            if (SystemsRoot != null) return;

            SystemsRoot = new GameObject("[Momentum Systems]");
            Object.DontDestroyOnLoad(SystemsRoot);

            // Order matters: save & settings first, then systems that read them.
            SystemsRoot.AddComponent<SaveManager>();
            SystemsRoot.AddComponent<SettingsManager>();
            SystemsRoot.AddComponent<AudioManager>();
            SystemsRoot.AddComponent<PoolManager>();
            SystemsRoot.AddComponent<ScreenFader>();
            SystemsRoot.AddComponent<SceneLoader>();
            SystemsRoot.AddComponent<GameManager>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            SystemsRoot = null;
        }
    }
}
