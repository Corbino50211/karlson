using System.Collections.Generic;
using Momentum.Audio;
using Momentum.PlayerSystems;
using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Hidden area trigger. Finding it shows a notification and is remembered in the save file.</summary>
    public class SecretArea : MonoBehaviour, ILevelObjectConfigurable
    {
        public static readonly List<SecretArea> All = new List<SecretArea>();

        [SerializeField] string secretId = "";
        [SerializeField] GameObject editorVisual;

        bool found;

        public bool Found => found;

        public static int FoundCount
        {
            get
            {
                int n = 0;
                foreach (var s in All)
                {
                    if (s != null && s.found) n++;
                }
                return n;
            }
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        void Start()
        {
            if (editorVisual != null) editorVisual.SetActive(false);
            if (string.IsNullOrEmpty(secretId))
            {
                Vector3 p = transform.position;
                secretId = Mathf.RoundToInt(p.x) + "_" + Mathf.RoundToInt(p.y) + "_" + Mathf.RoundToInt(p.z);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (found) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;
            found = true;
            string levelKey = LevelManager.Instance != null ? LevelManager.Instance.LevelId : "level";
            if (SaveManager.Instance != null) SaveManager.Instance.RegisterSecret(levelKey + ":" + secretId);
            AudioManager.Play2D(SoundId.Secret, 0.9f);
            GameEvents.RaiseNotification("SECRET FOUND (" + FoundCount + "/" + All.Count + ")", new Color(0.85f, 0.5f, 1f));
            GameEvents.RaiseSecretFound(FoundCount, All.Count);
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            secretId = data.GetString("secret", data.id.ToString());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
        }
    }
}
