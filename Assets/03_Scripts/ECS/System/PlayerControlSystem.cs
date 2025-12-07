using System;

/// <summary>
/// 玩家控制系统：处理输入 → 更新位置 → 触发射击
/// 帧同步安全：所有逻辑在 FixedUpdate 中执行，输入来自 InputManager 的逻辑帧缓冲
/// </summary>
public class PlayerControlSystem : BaseSystem
{
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        var currentFrame = GameTimeManager.CurrentLogicFrame;

        Span<int> playerIndices = stackalloc int[4];
        int count = EntityManager.GetEntities<CPlayer>(playerIndices);

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var runtimes = EntityManager.GetComponentSpan<CPlayerRunTime>();

        for (int i = 0; i < count; i++)
        {
            int entityIndex = playerIndices[i];
            byte playerIndex = runtimes[entityIndex].playerIndex;

            if (!InputManager.Instance.TryGetInputForFrame(playerIndex, currentFrame, out var input))
            {
                input = FrameInput.Default;
            }

            ref var runtime = ref runtimes[entityIndex];
            ref var pos = ref positions[entityIndex];

            // 根据输入更新位置
            runtime.isSlowMode = input.SlowMode;
            float speed = input.SlowMode ? runtime.moveSlowSpeed : runtime.moveSpeed;

            pos.x += input.MoveHorizontal * speed * fixedDeltaTime;
            pos.y += input.MoveVertical * speed * fixedDeltaTime;

            // TODO: 触发射击系统（可发事件或写入 CShootRequest）
        }
    }
}