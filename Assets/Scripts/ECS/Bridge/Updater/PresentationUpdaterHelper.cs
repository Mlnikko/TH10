/// <summary>
/// 表现 Updater 共用的插值/预测采样。
/// </summary>
internal static class PresentationUpdaterHelper
{
    public static bool TryGetDisplayTransform(
        in EntityManager em,
        Entity entity,
        out float x,
        out float y,
        out float angleRad)
    {
        var battle = BattleManager.Instance;
        var world = battle != null ? battle.ActiveBattleWorld : null;
        if (world == null)
        {
            ref readonly var pos = ref em.GetComponent<CPosition>(entity);
            x = pos.x;
            y = pos.y;
            angleRad = em.HasComponent<CRotation>(entity)
                ? em.GetComponent<CRotation>(entity).angleRad
                : 0f;
            return true;
        }

        return PresentationMotion.TrySampleDisplayTransform(
            em,
            entity,
            world.LogicFrameTimer,
            world.IsPresentationLogicStalled,
            RoomManager.LocalPlayerIndex,
            out x,
            out y,
            out angleRad);
    }
}
