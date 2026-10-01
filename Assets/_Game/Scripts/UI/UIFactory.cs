using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>
    /// Code-driven uGUI construction helpers. Every screen in the game is built with these, either at
    /// runtime or by the editor UIGenerator (which saves the result as prefabs).
    /// </summary>
    public static class UIFactory
    {
        static Font font;

        /// <summary>Unity's built-in runtime font (LegacyRuntime.ttf in 2022.2+).</summary>
        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                return font;
            }
        }

        // ------------------------------------------------------------------ Core

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            go.layer = Layers.UI;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = Layers.UI;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Stretches to fill the parent with optional margins.</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Places a rect with a single anchor point, pivot, position and size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var img = CreateImage(name, parent, color);
            img.raycastTarget = true;
            return img;
        }

        public static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Color color, FontStyle style = FontStyle.Normal)
        {
            var rt = CreateRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        // ------------------------------------------------------------------ Controls

        public static ColorBlock ButtonColors(Color normal, Color highlight)
        {
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = normal;
            cb.highlightedColor = highlight;
            cb.pressedColor = Color.Lerp(highlight, Color.black, 0.25f);
            cb.selectedColor = normal;
            cb.disabledColor = UITheme.ButtonDisabled;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            return cb;
        }

        public static Button CreateButton(string name, Transform parent, string label, int fontSize = UITheme.ButtonSize, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.colors = ButtonColors(UITheme.Button, UITheme.Accent);
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            rt.gameObject.AddComponent<UIButtonSounds>();

            var text = CreateText("Label", rt, label, fontSize, align, UITheme.Text, FontStyle.Bold);
            Stretch(text.rectTransform, 18f, 18f, 0f, 0f);
            return button;
        }

        public static Text GetLabel(Button button)
        {
            return button != null ? button.GetComponentInChildren<Text>(true) : null;
        }

        public static Slider CreateSlider(string name, Transform parent, float min, float max, bool wholeNumbers)
        {
            var root = CreateRect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();

            var bg = CreateImage("Background", root, new Color(0.25f, 0.26f, 0.3f, 1f));
            Stretch(bg.rectTransform, 0f, 0f, 12f, 12f);

            var fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, 0f, 0f, 12f, 12f);
            var fill = CreateImage("Fill", fillArea, UITheme.Accent);
            Stretch(fill.rectTransform);

            var handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, 10f, 10f, 0f, 0f);
            var handle = CreateImage("Handle", handleArea, Color.white);
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(20f, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.colors = ButtonColors(Color.white, UITheme.Accent);
            var nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;
            return slider;
        }

        public static Toggle CreateToggle(string name, Transform parent)
        {
            var root = CreateRect(name, parent);
            var toggle = root.gameObject.AddComponent<Toggle>();
            var bg = CreateImage("Background", root, Color.white);
            bg.raycastTarget = true;
            Stretch(bg.rectTransform);
            var check = CreateImage("Checkmark", bg.rectTransform, UITheme.Accent);
            Stretch(check.rectTransform, 7f, 7f, 7f, 7f);
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.colors = ButtonColors(new Color(0.25f, 0.26f, 0.3f, 1f), new Color(0.35f, 0.36f, 0.42f, 1f));
            toggle.isOn = false;
            var nav = toggle.navigation;
            nav.mode = Navigation.Mode.None;
            toggle.navigation = nav;
            root.gameObject.AddComponent<UIButtonSounds>();
            return toggle;
        }

        public static InputField CreateInputField(string name, Transform parent, string placeholder, int fontSize = UITheme.BodySize)
        {
            var root = CreateRect(name, parent);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = Color.white;
            var input = root.gameObject.AddComponent<InputField>();
            input.targetGraphic = bg;
            input.colors = ButtonColors(new Color(0.16f, 0.17f, 0.2f, 1f), new Color(0.22f, 0.23f, 0.28f, 1f));

            var ph = CreateText("Placeholder", root, placeholder, fontSize, TextAnchor.MiddleLeft, UITheme.TextDim, FontStyle.Italic);
            Stretch(ph.rectTransform, 12f, 12f, 4f, 4f);
            ph.horizontalOverflow = HorizontalWrapMode.Wrap;
            var text = CreateText("Text", root, "", fontSize, TextAnchor.MiddleLeft, UITheme.Text);
            Stretch(text.rectTransform, 12f, 12f, 4f, 4f);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            input.textComponent = text;
            input.placeholder = ph;
            input.lineType = InputField.LineType.SingleLine;
            var nav = input.navigation;
            nav.mode = Navigation.Mode.None;
            input.navigation = nav;
            return input;
        }

        /// <summary>Vertical scroll view. Returns the ScrollRect; content receives a VerticalLayoutGroup.</summary>
        public static ScrollRect CreateScrollView(string name, Transform parent, out RectTransform content, float spacing = 8f)
        {
            var root = CreateRect(name, parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();

            var viewport = CreateRect("Viewport", root);
            Stretch(viewport, 0f, 16f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);

            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 4, 4, 4);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var barRoot = CreateRect("Scrollbar", root);
            barRoot.anchorMin = new Vector2(1f, 0f);
            barRoot.anchorMax = new Vector2(1f, 1f);
            barRoot.pivot = new Vector2(1f, 0.5f);
            barRoot.sizeDelta = new Vector2(10f, 0f);
            barRoot.anchoredPosition = Vector2.zero;
            var barBg = barRoot.gameObject.AddComponent<Image>();
            barBg.color = new Color(1f, 1f, 1f, 0.08f);
            var scrollbar = barRoot.gameObject.AddComponent<Scrollbar>();
            var slidingArea = CreateRect("Sliding Area", barRoot);
            Stretch(slidingArea);
            var handle = CreateImage("Handle", slidingArea, new Color(1f, 1f, 1f, 0.35f));
            handle.raycastTarget = true;
            Stretch(handle.rectTransform);
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return scroll;
        }

        // ------------------------------------------------------------------ Layout helpers

        public static VerticalLayoutGroup AddVertical(GameObject go, float spacing, RectOffset padding, TextAnchor align = TextAnchor.UpperCenter, bool controlHeight = true)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontal(GameObject go, float spacing, RectOffset padding, TextAnchor align = TextAnchor.MiddleLeft, bool expandWidth = false)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static LayoutElement Size(Component c, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f)
            {
                le.preferredWidth = preferredWidth;
                le.minWidth = preferredWidth;
            }
            if (preferredHeight >= 0f)
            {
                le.preferredHeight = preferredHeight;
                le.minHeight = preferredHeight;
            }
            if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) le.flexibleHeight = flexibleHeight;
            return le;
        }

        /// <summary>Makes sure there is an EventSystem with the legacy input module.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var existing = Object.FindObjectOfType<EventSystem>();
            if (existing != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(go);
        }

        /// <summary>Horizontal fill by anchors: the image spans 0..amount of its parent width.</summary>
        public static void SetFill(Image img, float amount)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            float a = Mathf.Clamp01(amount);
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(a, rt.anchorMax.y);
            // Hide when the fill would be narrower than its margins (avoids inverted quads).
            var parent = rt.parent as RectTransform;
            float parentWidth = parent != null ? parent.rect.width : 100f;
            float margins = rt.offsetMin.x - rt.offsetMax.x;
            img.canvasRenderer.cull = a * parentWidth <= margins + 0.5f;
        }

        public static void SetActive(Component c, bool active)
        {
            if (c != null && c.gameObject.activeSelf != active) c.gameObject.SetActive(active);
        }
    }
}
