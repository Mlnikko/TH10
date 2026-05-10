using System;

/// <summary>
/// 逻辑帧上根据 <see cref="CEnemyMovement"/> 写入 <see cref="CPosition"/>（东方系可配置轨迹）。
/// 需在 <see cref="StageTimelineSystem"/> 之后运行，以便当帧新生成的敌人也被推进。
/// </summary>
public class EnemyMovementSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CEnemyMovement>();
        if (indices.Length == 0)
            return;

        var motions = EntityManager.GetComponentSpan<CEnemyMovement>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            ref var m = ref motions[idx];
            ref var pos = ref positions[idx];
            float prevX = pos.x;
            float prevY = pos.y;
            EnemyMotionEvaluator.Evaluate(in m, frame, out float nx, out float ny);

            ref var vel = ref velocities[idx];
            vel.vx = nx - prevX;
            vel.vy = ny - prevY;
            pos.x = nx;
            pos.y = ny;

            TryRecycleEnemyOutOfRecycleArea(idx, nx, ny);
        }
    }

    /// <summary>与 <see cref="DanmakuSystem"/> 相同：超出 <see cref="GlobalBattleData.AreaData"/> 回收区则挂 <see cref="CPoolRecycleTag"/>，由 <see cref="PresentationSystem"/> 回收表现并销毁实体。</summary>
    void TryRecycleEnemyOutOfRecycleArea(int entityIndex, float x, float y)
    {
        if (!GlobalBattleData.IsInitialized)
            return;

        Entity entity = EntityManager.GetEntity(entityIndex);
        if (!EntityManager.IsValid(entity) || !EntityManager.HasComponent<CEnemy>(entity))
            return;
        if (EntityManager.HasComponent<CNoOffscreenRecycleTag>(entity))
            return;

        if (!GlobalBattleData.AreaData.IsPointInRecycleArea(x, y))
            EntityManager.AddComponent(entityIndex, new CPoolRecycleTag());
    }
}
