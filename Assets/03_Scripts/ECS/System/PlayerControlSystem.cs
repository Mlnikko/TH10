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
        int count = EntityManager.GetEntities<CPlayerRunTime>(playerIndices);

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var runtimes = EntityManager.GetComponentSpan<CPlayerRunTime>();

        for (int i = 0; i < count; i++)
        {
            int entityIndex = playerIndices[i];
            byte playerIndex = runtimes[entityIndex].playerIndex;

            // 从 InputManager 获取“当前逻辑帧”的输入
            if (!InputManager.Instance.TryGetInputForFrame(playerIndex, currentFrame, out var input))
            {
                input = FrameInput.Default;
                GameLogger.Debug($"Missing input for P{playerIndex} at frame {currentFrame}");
            }

            ref var runtime = ref runtimes[entityIndex];
            ref var pos = ref positions[entityIndex];

            runtime.isSlowMode = input.slowMode;
            float speed = input.slowMode ? runtime.moveSlowSpeed : runtime.moveSpeed;

            pos.x += input.moveHorizontal * speed * fixedDeltaTime;
            pos.y += input.moveVertical * speed * fixedDeltaTime;

            // TODO: 触发射击系统（可发事件或写入 CShootRequest）
        }
    }
}