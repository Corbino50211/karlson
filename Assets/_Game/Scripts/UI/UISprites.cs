using UnityEngine;

namespace Momentum.UI
{
    /// <summary>Procedurally generated UI sprites (circle, ring, triangle). Created once and cached.</summary>
    public static class UISprites
    {
        static Sprite circle;
        static Sprite ring;
        static Sprite triangle;

        public static Sprite Circle => circle != null ? circle : (circle = Make(64, (x, y, r) => Mathf.Clamp01(r - Dist(x, y, r) + 0.5f), "Circle"));

        public static Sprite Ring => ring != null ? ring : (ring = Make(128, (x, y, r) =>
        {
            float d = Dist(x, y, r);
            float outer = Mathf.Clamp01(r - d + 0.5f);
            float inner = Mathf.Clamp01(d - (r - 7f) + 0.5f);
            return outer * inner;
        }, "Ring"));

        public static Sprite Triangle => triangle != null ? triangle : (triangle = Make(64, (x, y, r) =>
        {
            // Upward pointing triangle.
            float size = r * 2f;
            float ny = y / size;
            float halfWidth = (1f - ny) * 0.5f;
            float nx = Mathf.Abs(x / size - 0.5f);
            return nx <= halfWidth ? 1f : 0f;
        }, "Triangle"));

        static float Dist(float x, float y, float r)
        {
            float dx = x - r + 0.5f;
            float dy = y - r + 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static Sprite Make(int size, System.Func<float, float, float, float> alpha, string spriteName)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UI_" + spriteName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            float r = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte a = (byte)(Mathf.Clamp01(alpha(x, y, r)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
