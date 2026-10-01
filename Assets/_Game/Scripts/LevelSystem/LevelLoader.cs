using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>
    /// Builds a level from LevelData at runtime (custom maps, editor test mode) by instantiating every
    /// object through LevelObjectFactory under a single root, and applies the level environment.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] Light sun;

        Transform root;

        public LevelData CurrentData { get; private set; }
        public Transform Root => root;

        public Transform Build(LevelData data, LevelBuildMode mode, string rootName = "Level")
        {
            Clear();
            CurrentData = data;
            root = new GameObject(rootName).transform;
            if (data == null) return root;
            data.Sanitize();
            foreach (var obj in data.objects)
            {
                LevelObjectFactory.Create(obj, root, mode);
            }
            EnvironmentApplier.Apply(data.environment, sun);
            return root;
        }

        public void Clear()
        {
            if (root != null)
            {
                // Deactivate first so static registries (spawn points, checkpoints...) update immediately.
                root.gameObject.SetActive(false);
                Destroy(root.gameObject);
                root = null;
            }
            CurrentData = null;
        }

        public static LevelData LoadFile(string path)
        {
            return CustomLevelStorage.Load(path);
        }
    }
}
