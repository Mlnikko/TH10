/// <summary>
/// 表现 Updater 共用：直接读取当前逻辑帧的 <see cref="CPosition"/> / <see cref="CRotation"/>。
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
        ref readonly var pos = ref em.GetComponent<CPosition>(entity);
        x = pos.x;
        y = pos.y;
        angleRad = em.HasComponent<CRotation>(entity)
            ? em.GetComponent<CRotation>(entity).angleRad
            : 0f;
        return true;
    }
}
