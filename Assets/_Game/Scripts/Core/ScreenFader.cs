using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum
{
    /// <summary>
    /// Full-screen black overlay used for scene transitions. Builds its own canvas at runtime.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        CanvasGroup group;
        float alpha;

        public float Alpha => alpha;

        void Awake()
        {
            EnsureBuilt();
        }

        void EnsureBuilt()
        {
            if (group != null) return;
            var canvasGo = new GameObject("ScreenFader", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            group = canvasGo.GetComponent<CanvasGroup>();

            var imageGo = new GameObject("Black", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)imageGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            imageGo.GetComponent<Image>().color = Color.black;
            SetAlpha(0f);
        }

        void SetAlpha(float a)
        {
            alpha = a;
            group.alpha = a;
            group.blocksRaycasts = a > 0.01f;
            group.interactable = false;
        }

        public IEnumerator FadeTo(float target, float duration)
        {
            EnsureBuilt();
            float start = alpha;
            if (duration <= 0f)
            {
                SetAlpha(target);
                yield break;
            }
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                SetAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(t)));
                yield return null;
            }
            SetAlpha(target);
        }
    }
}
