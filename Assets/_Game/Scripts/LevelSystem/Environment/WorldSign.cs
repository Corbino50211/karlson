using UnityEngine;

namespace Momentum.Levels
{
    /// <summary>3D text sign (tutorial hints, level labels).</summary>
    public class WorldSign : MonoBehaviour, ILevelObjectConfigurable
    {
        [SerializeField] TextMesh textMesh;
        [SerializeField] Transform backing;
        [SerializeField] string text = "SIGN";
        [SerializeField] Color color = Color.white;
        [SerializeField] float size = 1f;

        public string Text => text;

        void Start()
        {
            Apply();
        }

        void Apply()
        {
            if (textMesh == null) return;
            textMesh.text = text.Replace("\\n", "\n");
            textMesh.color = color;
            textMesh.transform.localScale = Vector3.one * 0.1f * size;
            if (backing != null)
            {
                var lines = textMesh.text.Split('\n');
                int longest = 1;
                foreach (var l in lines) longest = Mathf.Max(longest, l.Length);
                backing.localScale = new Vector3(Mathf.Max(1f, longest * 0.42f * size + 0.6f), lines.Length * 0.85f * size + 0.4f, 0.1f);
            }
        }

        public void ApplyLevelProperties(LevelObjectData data)
        {
            text = data.GetString("text", text);
            color = data.GetColor("color", color);
            size = Mathf.Clamp(data.GetFloat("size", size), 0.2f, 10f);
            Apply();
        }
    }
}
