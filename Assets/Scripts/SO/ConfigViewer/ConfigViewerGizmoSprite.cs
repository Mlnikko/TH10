#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene 视图 Gizmo 中绘制 Sprite（配置预览用，不参与战斗逻辑）。
/// </summary>
public static class ConfigViewerGizmoSprite
{
    public static void DrawAt(Vector3 worldPosition, Sprite sprite, Color tint, float uniformScale = 1f)
    {
        if (sprite == null || sprite.texture == null)
            return;

        float guiScale = HandleUtility.GetHandleSize(worldPosition) * 48f * uniformScale;
        float aspect = sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
        float w = guiScale;
        float h = guiScale * aspect;

        Vector2 gui = HandleUtility.WorldToGUIPoint(worldPosition);
        var screenRect = new Rect(gui.x - w * 0.5f, gui.y - h * 0.5f, w, h);

        var tex = sprite.texture;
        var tr = sprite.textureRect;
        var uv = new Rect(
            tr.x / tex.width,
            tr.y / tex.height,
            tr.width / tex.width,
            tr.height / tex.height);

        Handles.BeginGUI();
        var prev = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(screenRect, tex, uv);
        GUI.color = prev;
        Handles.EndGUI();
    }
}
#endif
