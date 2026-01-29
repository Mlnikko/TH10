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
        // === 位置更新 ===
        if (_transform != null)
        {
            var pos = em.GetComponentSpan<CPosition>()[entity.Index];
            _transform.position = new Vector3(pos.x, pos.y, 0);
        }

        // === 动画更新 ===
        if (_animator != null)
        {
            ref readonly var velocity = ref em.GetComponentSpan<CVelocity>()[entity.Index];
            ref readonly var playerRuntime = ref em.GetComponentSpan<CPlayerRunTime>()[entity.Index];

            // --- 方向动画 ---
            int currentDirection = velocity.vx > 0 ? 1 : (velocity.vx < 0 ? -1 : 0);
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
            bool isSlowMode = playerRuntime.isSlowMode;
            if (isSlowMode != _lastIsSlowMode)
            {
                _lastIsSlowMode = isSlowMode;
                float weight = isSlowMode ? 1 : 0;
                _animator.SetLayerWeight(_slowEffectLayerIndex, weight);
            }
        }
    }
}