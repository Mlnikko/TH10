using UnityEngine;

public class PlayerUpdater : IGameObjectUpdater
{
    readonly Transform _transform;
    readonly Animator _animator;

    int _lastDirection = 0;
    bool _lastIsSlowMode = false;
    readonly int _slowEffectLayerIndex;

    public PlayerUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
        _animator = gameObject.GetComponent<Animator>();

        // 缓存图层索引（避免每帧字符串查找）
        _slowEffectLayerIndex = _animator.GetLayerIndex("Slow Effect");
        if (_slowEffectLayerIndex == -1)
        {
            Logger.Warn("Animator missing 'SlowEffect' layer!");
        }
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        // === 位置更新（逻辑帧插值 + 联机等待输入时本地预测）===
        if (_transform != null
            && PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out _))
        {
            _transform.position = new Vector3(x, y, 0);
        }

        // === 动画更新 ===
        if (_animator != null)
        {
            ref readonly var player = ref em.GetComponentSpan<CPlayer>()[entity.Index];

            sbyte moveH;
            if (player.playerIndex == RoomManager.LocalPlayerIndex
                && BattleManager.Instance?.ActiveBattleWorld != null)
            {
                var input = InputManager.Instance.SampleLocalInput(
                    player.playerIndex,
                    BattleManager.Instance.ActiveBattleWorld.LogicFrameTimer.CurrentFrame);
                moveH = input.MoveHorizontal;
            }
            else
            {
                ref readonly var velocity = ref em.GetComponentSpan<CVelocity>()[entity.Index];
                moveH = velocity.vx > 0 ? (sbyte)1 : (velocity.vx < 0 ? (sbyte)-1 : (sbyte)0);
            }

            // --- 方向动画 ---
            int currentDirection = moveH > 0 ? 1 : (moveH < 0 ? -1 : 0);
            if (currentDirection != _lastDirection)
            {
                _lastDirection = currentDirection;
                switch (currentDirection)
                {
                    case 1:
                        _animator.Play("Player_Move_Right");
                        break;
                    case -1:
                        _animator.Play("Player_Move_Left");
                        break;
                    default:
                        _animator.Play("Player_Idle");
                        break;
                }
            }

            // --- 慢速模式特效图层 ---
            bool isSlowMode = player.isSlowMode;
            if (isSlowMode != _lastIsSlowMode)
            {
                _lastIsSlowMode = isSlowMode;
                float weight = isSlowMode ? 1 : 0;
                _animator.SetLayerWeight(_slowEffectLayerIndex, weight);
            }
        }
    }
}