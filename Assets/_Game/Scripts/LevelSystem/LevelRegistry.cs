using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>Ordered list of campaign levels. Order defines progression / unlocks.</summary>
    [CreateAssetMenu(menuName = "Momentum/Level Registry", fileName = "LevelRegistry")]
    public class LevelRegistry : ScriptableObject
    {
        public List<LevelDefinition> campaign = new List<LevelDefinition>();

        public int Count => campaign.Count;

        public LevelDefinition GetByIndex(int index)
        {
            if (index < 0 || index >= campaign.Count) return null;
            return campaign[index];
        }

        public int IndexOf(LevelDefinition level)
        {
            return level == null ? -1 : campaign.IndexOf(level);
        }

        public LevelDefinition FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var l in campaign)
            {
                if (l != null && l.id == id) return l;
            }
            return null;
        }

        public LevelDefinition FindByScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            foreach (var l in campaign)
            {
                if (l != null && l.sceneName == sceneName) return l;
            }
            return null;
        }

        public LevelDefinition GetNext(LevelDefinition current)
        {
            int i = IndexOf(current);
            return i >= 0 ? GetByIndex(i + 1) : null;
        }
    }
}
