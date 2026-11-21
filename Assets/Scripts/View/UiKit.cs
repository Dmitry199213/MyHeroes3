using System;
using UnityEngine;
using UnityEngine.UI;

namespace HeroesBattle
{
    public static class UiKit
    {
        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Segoe UI", 24);
                }

                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Place(RectTransform rt, float x, float topY, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -topY);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static RectTransform PlaceRight(RectTransform rt, float rightOffset, float topY, float width, float height)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-rightOffset, -topY);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static RectTransform PlaceBottom(RectTransform rt, float x, float bottomY, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, bottomY);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static RectTransform PlaceBottomRight(RectTransform rt, float rightOffset, float bottomY, float width, float height)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-rightOffset, bottomY);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static RectTransform PlaceCenter(RectTransform rt, float centerX, float centerY, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centerX, centerY);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Image Panel(Transform parent, string name, Color color, bool raycast = false)
        {
            var image = Img(parent, name, ProceduralArt.RoundedRect, color, raycast);
            image.type = Image.Type.Sliced;
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = true;
            return label;
        }

        public static Button TextButton(Transform parent, string name, string caption, int fontSize,
            Color background, Color foreground, Action onClick)
        {
            var image = Panel(parent, name, background, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = Label(image.transform, "Label", caption, fontSize, foreground, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform, 6f, 6f, 2f, 2f);

            button.onClick.AddListener(() =>
            {
                Sfx.Play(SfxId.Click, 0.5f);
                if (onClick != null) onClick();
            });

            return button;
        }

        public static Image CreateBar(Transform parent, string name, Color background, Color fillColor, out Image fill)
        {
            var bg = Panel(parent, name, background, false);
            fill = Img(bg.transform, "Fill", ProceduralArt.RoundedRect, fillColor, false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);
            return bg;
        }

        public static void SetBar(Image fill, float ratio)
        {
            if (fill == null) return;
            fill.fillAmount = Mathf.Clamp01(ratio);
        }

        public static GameObject UnitBadge(Transform parent, string name, UnitDef def, float size)
        {
            var container = Group(parent, name);
            container.sizeDelta = new Vector2(size, size);

            var bg = Img(container, "Bg", ProceduralArt.Circle, Darken(def.Color, 0.62f), false);
            Fill(bg.rectTransform);

            var icon = Img(container, "Icon", ProceduralArt.IconFor(def), new Color(1f, 1f, 1f, 0.92f), false);
            Fill(icon.rectTransform);
            icon.rectTransform.offsetMin = new Vector2(size * 0.22f, size * 0.22f);
            icon.rectTransform.offsetMax = new Vector2(-size * 0.22f, -size * 0.22f);

            return container.gameObject;
        }

        public static Color Darken(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
        }

        public static Color FactionColor(Side side)
        {
            return side == Side.Player
                ? new Color(0.25f, 0.46f, 0.72f)
                : new Color(0.70f, 0.26f, 0.22f);
        }

        public static Color PanelColor(float alpha = 0.92f)
        {
            return new Color(0.11f, 0.095f, 0.075f, alpha);
        }

        public static Color Gold
        {
            get { return new Color(0.95f, 0.78f, 0.42f); }
        }

        public static Color TextMain
        {
            get { return new Color(0.93f, 0.90f, 0.82f); }
        }

        public static Color TextDim
        {
            get { return new Color(0.70f, 0.67f, 0.60f); }
        }

        public static void SetText(Text label, string value)
        {
            if (label != null && label.text != value) label.text = value;
        }
    }
}
