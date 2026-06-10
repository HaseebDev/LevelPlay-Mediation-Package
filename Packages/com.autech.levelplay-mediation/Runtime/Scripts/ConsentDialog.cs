using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM_PACKAGE
using UnityEngine.InputSystem.UI;
#endif

namespace Autech.LevelPlay
{
    /// <summary>
    /// Minimal, dependency-free GDPR consent dialog built from code (uGUI +
    /// built-in font) so the package needs no scene, prefab, or TMP assets.
    /// Accept = personalized ads consent granted; Decline = contextual ads only.
    /// </summary>
    internal static class ConsentDialog
    {
        private const int SortingOrder = 32600;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color AcceptColor = new Color(1f, 0.71f, 0f, 1f);
        private static readonly Color DeclineColor = new Color(0.28f, 0.28f, 0.32f, 1f);
        private static readonly Color TitleTextColor = Color.white;
        private static readonly Color BodyTextColor = new Color(0.85f, 0.85f, 0.87f, 1f);
        private static readonly Color LinkTextColor = new Color(1f, 0.71f, 0f, 1f);

        private const string Title = "We value your privacy";
        private const string Body =
            "This game shows ads from Unity Ads to keep it free. With your consent, " +
            "ads can be personalized using your device's advertising identifier. " +
            "If you decline, you will still see ads, but they will not be personalized. " +
            "You can change this choice anytime in Settings.";
        private const string AcceptLabel = "Accept";
        private const string DeclineLabel = "Decline";
        private const string PolicyLabel = "Privacy Policy";

        private static TaskCompletionSource<bool> activeRequest;

        /// <summary>
        /// Show the dialog and await the user's choice. Returns true when the
        /// user accepts personalized ads. Re-entrant calls share one dialog.
        /// </summary>
        public static Task<bool> ShowAsync(string privacyPolicyUrl, bool isPrivacyOptions = false)
        {
            if (activeRequest != null)
            {
                return activeRequest.Task;
            }

            activeRequest = new TaskCompletionSource<bool>();
            BuildDialog(privacyPolicyUrl);
            return activeRequest.Task;
        }

        private static void Complete(GameObject root, bool granted)
        {
            Object.Destroy(root);
            var request = activeRequest;
            activeRequest = null;
            request?.TrySetResult(granted);
        }

        private static void BuildDialog(string privacyPolicyUrl)
        {
            EnsureEventSystem();

            var root = new GameObject("AutechConsentDialog");
            Object.DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            // Dim overlay (blocks input behind the dialog)
            var overlay = CreateRect("Overlay", root.transform);
            Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color = OverlayColor;

            // Panel
            var panel = CreateRect("Panel", overlay);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(900f, 760f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            // Title
            var title = CreateText("Title", panel, Title, 52, FontStyle.Bold, TitleTextColor);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            title.rectTransform.sizeDelta = new Vector2(-80f, 80f);

            // Body
            var body = CreateText("Body", panel, Body, 36, FontStyle.Normal, BodyTextColor);
            body.rectTransform.anchorMin = new Vector2(0f, 1f);
            body.rectTransform.anchorMax = new Vector2(1f, 1f);
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(0f, -140f);
            body.rectTransform.sizeDelta = new Vector2(-80f, 360f);

            // Privacy policy link
            var linkButton = CreateButton("PolicyLink", panel, PolicyLabel, 34, Color.clear, LinkTextColor);
            var linkRect = linkButton.GetComponent<RectTransform>();
            linkRect.anchorMin = new Vector2(0.5f, 0f);
            linkRect.anchorMax = new Vector2(0.5f, 0f);
            linkRect.pivot = new Vector2(0.5f, 0f);
            linkRect.anchoredPosition = new Vector2(0f, 210f);
            linkRect.sizeDelta = new Vector2(420f, 64f);
            linkButton.onClick.AddListener(() => Application.OpenURL(privacyPolicyUrl));

            // Accept button
            var accept = CreateButton("Accept", panel, AcceptLabel, 40, AcceptColor, Color.black);
            var acceptRect = accept.GetComponent<RectTransform>();
            acceptRect.anchorMin = new Vector2(0.5f, 0f);
            acceptRect.anchorMax = new Vector2(0.5f, 0f);
            acceptRect.pivot = new Vector2(0.5f, 0f);
            acceptRect.anchoredPosition = new Vector2(0f, 120f);
            acceptRect.sizeDelta = new Vector2(740f, 84f);
            accept.onClick.AddListener(() => Complete(root, true));

            // Decline button
            var decline = CreateButton("Decline", panel, DeclineLabel, 40, DeclineColor, Color.white);
            var declineRect = decline.GetComponent<RectTransform>();
            declineRect.anchorMin = new Vector2(0.5f, 0f);
            declineRect.anchorMax = new Vector2(0.5f, 0f);
            declineRect.pivot = new Vector2(0.5f, 0f);
            declineRect.anchoredPosition = new Vector2(0f, 24f);
            declineRect.sizeDelta = new Vector2(740f, 84f);
            decline.onClick.AddListener(() => Complete(root, false));
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null) return;

            var eventSystem = new GameObject("AutechConsentEventSystem");
            Object.DontDestroyOnLoad(eventSystem);
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM_PACKAGE
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            Debug.Log("[Autech.LevelPlay] No EventSystem found; created one for the consent dialog.");
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, int fontSize, Color background, Color textColor)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = background;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText("Label", rect, label, fontSize, FontStyle.Bold, textColor);
            Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
