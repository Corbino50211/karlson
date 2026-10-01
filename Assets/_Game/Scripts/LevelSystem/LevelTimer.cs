using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Run timer. Uses scaled time so it stops while paused.</summary>
    public class LevelTimer : MonoBehaviour
    {
        public static LevelTimer Instance { get; private set; }

        public bool Running { get; private set; }
        public bool Finished { get; private set; }
        public float CurrentTime { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Running) CurrentTime += Time.deltaTime;
        }

        public void ResetTimer()
        {
            Running = false;
            Finished = false;
            CurrentTime = 0f;
        }

        public void StartTimer()
        {
            if (Running || Finished) return;
            Running = true;
        }

        public void StopTimer()
        {
            Running = false;
            Finished = true;
        }
    }
}
