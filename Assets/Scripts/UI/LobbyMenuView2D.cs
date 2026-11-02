using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Small shared construction helpers for the two station menus.
public static class LobbyMenuView2D
{
    public static GameObject Canvas(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        obj.transform.SetParent(parent, false); obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        obj.GetComponent<Canvas>().sortingOrder = 5;
        var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        return obj;
    }
    public static GameObject Modal(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; obj.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.065f, 1f);
        return obj;
    }
    public static RectTransform Panel(Transform parent)
    {
        var panel = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>(); panel.SetParent(parent, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one * 0.5f; panel.sizeDelta = new Vector2(1100f, 650f); return panel;
    }
    public static void Rect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
    public static Text Text(Transform parent, string value, float x, float y, float width, float height, int size = 22)
    {
        var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.text = value; text.color = Color.white; text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false;
        Rect(text.rectTransform, x, y, width, height); return text;
    }
    public static Button Button(Transform parent, string label, float x, float y, float width, float height, UnityAction action)
    {
        var obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
        Rect(obj.GetComponent<RectTransform>(), x, y, width, height);
        var image = obj.GetComponent<Image>(); image.color = new Color(0.15f, 0.35f, 0.6f);
        var button = obj.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        Text(obj.transform, label, 0, 0, width, height).alignment = TextAnchor.MiddleCenter; return button;
    }
    public static RectTransform Scroll(Transform parent, float x, float y, float width, float height, out ScrollRect scroll)
    {
        var viewport = new GameObject("Items", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.transform.SetParent(parent, false); Rect(viewport.GetComponent<RectTransform>(), x, y, width, height);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(viewport.transform, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        scroll = viewport.GetComponent<ScrollRect>(); scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35f; return content;
    }
}
