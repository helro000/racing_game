using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Shared, resolution-independent UI for the garage, race and result screens.
public static class RelayUI
{
    public static readonly Color Ink = new Color(.035f, .055f, .09f, .94f);
    public static readonly Color Cyan = new Color(.15f, .9f, .88f);
    public static Canvas Canvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        return canvas;
    }
    public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }
    public static GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rect = Rect(parent, name, anchor, pos, size);
        rect.gameObject.AddComponent<Image>().color = Ink;
        return rect.gameObject;
    }
    public static Text Label(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, int font = 22, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var rect = Rect(parent, text, anchor, pos, size);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = font;
        label.text = text;
        label.color = Color.white;
        label.alignment = align;
        label.raycastTarget = false;
        return label;
    }
    public static Button Button(Transform parent, string label, Vector2 pos, Vector2 size, UnityAction action)
    {
        var panel = Panel(parent, label, new Vector2(.5f,.5f), pos, size);
        panel.GetComponent<Image>().color = new Color(.08f,.35f,.4f);
        var button = panel.AddComponent<Button>();
        button.targetGraphic = panel.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = Cyan;
        colors.selectedColor = new Color(.5f,.9f,.9f);
        button.colors = colors;
        button.onClick.AddListener(action);
        Label(panel.transform, label, new Vector2(.5f,.5f), Vector2.zero, size, 22, TextAnchor.MiddleCenter);
        return button;
    }
}
