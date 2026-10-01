using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Momentum
{
    /// <summary>
    /// Loads scenes asynchronously behind a fade. Lives on the persistent systems object.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }
        public static bool IsLoading { get; private set; }

        [SerializeField] float fadeOutTime = 0.2f;
        [SerializeField] float fadeInTime = 0.3f;

        ScreenFader fader;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            fader = GetComponent<ScreenFader>();
            if (fader == null) fader = gameObject.AddComponent<ScreenFader>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Loads a scene by name with a fade. Logs a helpful error if the scene is not in Build Settings.</summary>
        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[Momentum] SceneLoader.Load called with an empty scene name.");
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Momentum] Scene '{sceneName}' is not in Build Settings. Run Tools > Parkour FPS > Setup Complete Game.");
                GameEvents.RaiseNotification($"Missing scene: {sceneName}", Color.red);
                return;
            }
            if (Instance == null)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(sceneName);
                return;
            }
            if (IsLoading) return;
            Instance.StartCoroutine(Instance.LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            yield return fader.FadeTo(1f, fadeOutTime);
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            IsLoading = false;
            yield return fader.FadeTo(0f, fadeInTime);
        }
    }
}
