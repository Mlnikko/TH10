#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(CharacterConfigViewer))]
public class CharacterConfigViewerEditor : GameConfigViewerEditor<CharacterConfigViewer>
{
    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.CharacterConfig, "CharacterConfig"))
            return;

        DrawSyncHint("双击进入角色预制体后自动同步。武器发射请在 WeaponConfigViewer 预制体中编辑。");

        ConfigViewerEditorUI.DrawSeparator();
        DrawSave(Viewer.CharacterConfig, Viewer.SaveCharacterConfig, "CharacterConfig");
    }
}
#endif
