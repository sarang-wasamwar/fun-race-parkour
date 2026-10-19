// RuntimeUiFactory.cs
// Builds TMP/uGUI trees at runtime so menu and HUD work without hand-wired scene refs.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FunRaceParkour
{
    public static class RuntimeUiFactory
    {
        private static TMP_FontAsset _configuredFont;
        public static void SetFont(TMP_FontAsset font) => _configuredFont = font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (_configuredFont != null) return _configuredFont;
                if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                    return TMP_Settings.defaultFontAsset;
                return null;
            }
        }

        public static Canvas CreateCanvas(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, int size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (Font != null) tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button Button(Transform parent, string name, string caption, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.18f, 0.32f, 0.92f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => AudioManager.Instance?.PlayButtonClick());
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = Label(go.transform, "Label", caption, 28, TextAlignmentOptions.Center, Color.white);
            Stretch(label.rectTransform);
            return button;
        }

        public static Slider Slider(Transform parent, string name, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 24f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), 8f, 4f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.25f, 0.75f, 1f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>(), 8f, 0f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 30f);
            handle.GetComponent<Image>().color = Color.white;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static void Stretch(RectTransform rt, float padX = 0f, float padY = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
