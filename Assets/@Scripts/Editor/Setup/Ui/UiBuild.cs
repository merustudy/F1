using System;
using System.Collections.Generic;
using F1.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    /// <summary>A button made by <see cref="UiBuild"/>: the button, its background and its label.</summary>
    internal readonly struct ButtonParts
    {
        public ButtonParts(Button button, Image frame, TextMeshProUGUI label)
        {
            Button = button;
            Frame = frame;
            Label = label;
        }

        public Button Button { get; }
        public Image Frame { get; }
        public TextMeshProUGUI Label { get; }
        public RectTransform Rect => (RectTransform)Button.transform;
    }

    /// <summary>
    /// Small helpers for building uGUI hierarchies from Setup code. Screens are laid out in the
    /// 1920x1080 design space with <see cref="Box"/>: x and y are measured from the top-left
    /// corner of the parent.
    ///
    /// Every object of a prefab needs a name of its own: Unity keeps the ids of a rebuilt prefab
    /// stable by matching names, so a repeated name makes every rebuild rewrite the file.
    /// </summary>
    internal static class UiBuild
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        static Dictionary<string, string> _defaultTexts;

        public static Canvas Canvas(string name, Transform parent, int sortingOrder, bool raycaster)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            // Expand: the canvas is never smaller than the reference in either direction, so the
            // 1920x1080 frame of a screen always fits whatever the window's aspect ratio is.
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (raycaster)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Image(string name, Transform parent, Color color, bool raycastTarget = false)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>One of Unity's built-in UI sprites (a circle, a rounded frame), for placeholder shapes.</summary>
        public static Sprite BuiltinSprite(string path)
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Built-in sprite '{path}' was not found.");
            }

            return sprite;
        }

        public static TextMeshProUGUI Text(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string content,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        public static void Stretch(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors the rect to a point of its parent (0..1) and gives it a fixed size.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Puts the rect at (x, y) from the top-left corner of its parent, y growing downwards.</summary>
        public static T Box<T>(T component, float x, float y, float width, float height)
            where T : Component
        {
            Box((RectTransform)component.transform, x, y, width, height);
            return component;
        }

        public static RectTransform Box(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>
        /// Stretches the rect across its parent's width, at a fixed distance from the top and with a
        /// fixed height. For content of a card whose width is set at runtime.
        /// </summary>
        public static T Line<T>(T component, float top, float height, float left = 0f, float right = 0f)
            where T : Component
        {
            var rect = (RectTransform)component.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
            return component;
        }

        /// <summary>Gives a rect a fixed size inside a layout group.</summary>
        public static T Size<T>(T component, float width, float height)
            where T : Component
        {
            ((RectTransform)component.transform).sizeDelta = new Vector2(width, height);
            return component;
        }

        // ---- Screens -----------------------------------------------------------------------

        /// <summary>
        /// The root of a screen: it fills the UI root with the background color and holds a centered
        /// 1920x1080 frame. Everything else goes into the frame.
        /// </summary>
        public static RectTransform ScreenRoot(string name, Transform parent, out RectTransform frame)
        {
            Image background = Image(name, parent, UiPalette.Background, raycastTarget: true);
            Stretch(background.rectTransform);

            frame = Rect("Frame", background.transform);
            Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ReferenceResolution);
            return background.rectTransform;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            return Image(name, parent, color);
        }

        // ---- Text ----------------------------------------------------------------------------

        /// <summary>A text that code fills in at runtime.</summary>
        public static TextMeshProUGUI Label(
            string name,
            Transform parent,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            return Text(name, parent, FontSetup.LoadFontAsset(), string.Empty, fontSize, color, alignment);
        }

        /// <summary>
        /// A fixed label. It shows the entry of the UI string table and follows the locale by itself.
        /// The key must not have placeholders.
        /// </summary>
        public static TextMeshProUGUI LocalizedLabel(
            string name,
            Transform parent,
            string key,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            if (UiStringSource.IsSmart(DefaultText(key)))
            {
                throw new InvalidOperationException($"'{key}' has placeholders; build it at runtime with UiStrings.Get.");
            }

            // The prefab itself shows the key. The text comes from the string table when the label is enabled,
            // so editing the CSV never changes a prefab.
            TextMeshProUGUI text = Text(name, parent, FontSetup.LoadFontAsset(), key, fontSize, color, alignment);

            // The reference is by table name and key, so it survives a rebuild of the string table.
            var localize = text.gameObject.AddComponent<LocalizeStringEvent>();
            localize.StringReference = new LocalizedString(UiStrings.TableName, key);
            var setText = (UnityAction<string>)Delegate.CreateDelegate(
                typeof(UnityAction<string>),
                text,
                typeof(TMP_Text).GetProperty(nameof(TMP_Text.text)).GetSetMethod());
            UnityEventTools.AddPersistentListener(localize.OnUpdateString, setText);
            localize.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
            return text;
        }

        /// <summary>The default-locale text of a key. Throws when the key is not in the CSV.</summary>
        static string DefaultText(string key)
        {
            if (_defaultTexts == null)
            {
                _defaultTexts = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (UiStringRow row in LocalizationSetup.ReadSource().Rows)
                {
                    _defaultTexts.Add(row.Key, row.Values[F1.Data.LocalePolicy.DefaultCode]);
                }
            }

            if (!_defaultTexts.TryGetValue(key, out string text))
            {
                throw new InvalidOperationException($"UI string key '{key}' is not in {LocalizationSetup.SourcePath}.");
            }

            return text;
        }

        /// <summary>Forgets cached UI strings. Call before a build so an edited CSV is read again.</summary>
        public static void ResetCache()
        {
            _defaultTexts = null;
        }

        /// <summary>Keeps a text on one line; what does not fit ends with an ellipsis.</summary>
        public static TextMeshProUGUI SingleLine(TextMeshProUGUI text)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        // ---- Buttons and bars ----------------------------------------------------------------

        /// <summary>A button whose label code fills in at runtime.</summary>
        public static ButtonParts Button(string name, Transform parent, Color color, float fontSize)
        {
            Image frame = Image(name, parent, color, raycastTarget: true);
            Button button = MakeButton(frame);

            TextMeshProUGUI label = Label(name + "Label", frame.transform, fontSize, UiPalette.Text, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 8f, 4f, 8f, 4f);
            return new ButtonParts(button, frame, label);
        }

        /// <summary>A button with a fixed, localized label.</summary>
        public static ButtonParts LocalizedButton(string name, Transform parent, string key, Color color, float fontSize)
        {
            Image frame = Image(name, parent, color, raycastTarget: true);
            Button button = MakeButton(frame);

            TextMeshProUGUI label = LocalizedLabel(name + "Label", frame.transform, key, fontSize, UiPalette.Text, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 8f, 4f, 8f, 4f);
            return new ButtonParts(button, frame, label);
        }

        /// <summary>Makes an image clickable. Hover and press tint it; a disabled button is dimmed.</summary>
        public static Button MakeButton(Image frame)
        {
            frame.raycastTarget = true;
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            colors.fadeDuration = 0f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        /// <summary>A bar: a dark track with a fill whose width code sets at runtime.</summary>
        public static UiBar Bar(string name, Transform parent, Color fillColor)
        {
            Image track = Image(name, parent, UiPalette.Slot);
            Image fill = Image(name + "Fill", track.transform, fillColor);
            Stretch(fill.rectTransform);

            var bar = track.gameObject.AddComponent<UiBar>();
            SetReference(bar, "_fill", fill.rectTransform);
            SetReference(bar, "_fillImage", fill);
            return bar;
        }

        // ---- Layout groups -------------------------------------------------------------------

        /// <summary>Stacks children top to bottom at their own sizes.</summary>
        public static VerticalLayoutGroup Vertical(RectTransform rect, float spacing, int padding = 0, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childAlignment = alignment;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        /// <summary>Lines children up left to right at their own sizes.</summary>
        public static HorizontalLayoutGroup Horizontal(RectTransform rect, float spacing, int padding = 0, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childAlignment = alignment;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        /// <summary>Arranges children in rows of fixed-size cells.</summary>
        public static GridLayoutGroup Grid(RectTransform rect, Vector2 cellSize, float spacing, int columns)
        {
            var group = rect.gameObject.AddComponent<GridLayoutGroup>();
            group.cellSize = cellSize;
            group.spacing = new Vector2(spacing, spacing);
            group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            group.constraintCount = columns;
            group.childAlignment = TextAnchor.UpperLeft;
            return group;
        }

        // ---- Scrolling -----------------------------------------------------------------------

        /// <summary>
        /// A vertically scrolling area with a scrollbar on the right. The content stacks its children
        /// and grows with them; the mouse wheel over the area scrolls it.
        /// </summary>
        public static ScrollRect VerticalScroll(string name, Transform parent, float scrollbarWidth, out RectTransform content)
        {
            RectTransform root = Rect(name, parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();

            // The viewport is a raycast target so that the wheel reaches the scroll rect.
            Image viewport = Image(name + "Viewport", root, UiPalette.Slot, raycastTarget: true);
            Stretch(viewport.rectTransform, 0f, 0f, scrollbarWidth + 8f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Rect(name + "Content", viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup stack = Vertical(content, 0f, 12);
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            stack.childControlHeight = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image track = Image(name + "Scrollbar", root, UiPalette.Slot, raycastTarget: true);
            track.rectTransform.anchorMin = new Vector2(1f, 0f);
            track.rectTransform.anchorMax = Vector2.one;
            track.rectTransform.pivot = new Vector2(1f, 1f);
            track.rectTransform.offsetMin = new Vector2(-scrollbarWidth, 0f);
            track.rectTransform.offsetMax = Vector2.zero;
            RectTransform sliding = Rect(name + "SlidingArea", track.transform);
            Stretch(sliding, 2f, 2f, 2f, 2f);
            Image handle = Image(name + "Handle", sliding, UiPalette.ButtonQuiet, raycastTarget: true);
            Stretch(handle.rectTransform);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return scroll;
        }

        // ---- Serialized references -----------------------------------------------------------

        public static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = Find(serialized, target, propertyName);
            if (value == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} would be set to nothing.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetReferences(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = Find(serialized, target, propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    throw new InvalidOperationException($"{target.GetType().Name}.{propertyName}[{i}] would be set to nothing.");
                }

                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Find(SerializedObject serialized, UnityEngine.Object target, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{propertyName}'.");
            }

            return property;
        }
    }
}
