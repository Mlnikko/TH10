/// <summary>
/// 表现 Updater 共用：经 <see cref="PresentationMotion"/> 采样插值/预测后的显示坐标。
/// </summary>
internal static class PresentationUpdaterHelper
{
    public static bool TryGetDisplayTransform(
        in EntityManager em,
        Entity entity,
        out float x,
        out float y,
        out float angleRad) =>
        PresentationMotion.TrySampleDisplayTransform(em, entity, out x, out y, out angleRad);
}
