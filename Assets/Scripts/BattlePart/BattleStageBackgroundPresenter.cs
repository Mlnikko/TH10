using UnityEngine;

/// <summary>
/// 战斗背景表现入口：正式战斗中创建/释放 <see cref="BattleStageBackgroundRuntime"/>。
/// Scene 预览由 <see cref="StageTimelineConfigViewer"/> 临时创建 Runtime。
/// </summary>
public static class BattleStageBackgroundPresenter
{
    const float DefaultPixelsPerUnit = 100f;

    static BattleStageBackgroundRuntime _runtime;

    public static void EnsureFromGlobalBattleData()
    {
        Release();

        if (!GlobalBattleData.IsInitialized || !GameResDB.IsInitialized)
            return;

        var data = GlobalBattleData.BackgroundData;
        if (data == null || !data.enabled)
            return;

        var go = new GameObject("BattleStageBackground");
        _runtime = go.AddComponent<BattleStageBackgroundRuntime>();
        _runtime.Apply(GlobalBattleData.AreaData, data, ResolveRuntimeSprite);
    }

    public static void TryShakeMidBossDefeated() => _runtime?.TryShakeMidBossDefeated();

    public static void TryShakeMainBossDefeated() => _runtime?.TryShakeMainBossDefeated();

    public static void Release()
    {
        if (_runtime == null)
            return;

        _runtime.DisposeInstance();
        _runtime = null;
    }

    static Sprite ResolveRuntimeSprite(string textureId)
    {
        if (string.IsNullOrEmpty(textureId) || !GameResDB.IsInitialized)
            return null;

        return GameResDB.Instance.GetSpriteFromTexture(textureId, DefaultPixelsPerUnit);
    }
}
