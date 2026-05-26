using UnityEngine;

/// <summary>表现层回收角色时，一并归还挂接的对象池实例（如武器预制体）。</summary>
public interface IPresentationPooledAttachments
{
    void ReleasePooledAttachments();
}

public class PlayerUpdater : IGameObjectUpdater, IPresentationPooledAttachments
{
    readonly Transform _transform;
    readonly Animator _animator;
    readonly PresentationVelocityAnimatorSync.Driver _velocityAnimator;

    GameObject _weaponGo;
    readonly WeaponRuntimeLayoutView _weaponLayout = new();

    bool _lastIsSlowMode = false;
    readonly int _slowEffectLayerIndex;

    public PlayerUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
        _animator = gameObject.GetComponent<Animator>();
        _velocityAnimator = _animator != null
            ? new PresentationVelocityAnimatorSync.Driver(
                _animator,
                PresentationVelocityAnimatorSync.MotionProfile.HorizontalIdleLeftRight)
            : null;

        // 缓存图层索引（避免每帧字符串查找）
        _slowEffectLayerIndex = _animator != null ? _animator.GetLayerIndex("Slow Effect") : -1;
        if (_slowEffectLayerIndex == -1 && _animator != null)
            Logger.Warn("Animator missing 'SlowEffect' layer!");
    }

    public void AttachWeapon(GameObject weaponGo)
    {
        _weaponLayout.Clear();
        if (_weaponGo != null)
        {
            GameObjectPoolManager.Instance.Return(_weaponGo);
            _weaponGo = null;
        }

        if (weaponGo == null)
            return;

        _weaponGo = weaponGo;
        _weaponGo.transform.SetParent(_transform, false);
        _weaponGo.transform.localPosition = Vector3.zero;
        _weaponGo.transform.localRotation = Quaternion.identity;
        _weaponGo.SetActive(true);
    }

    public void ReleasePooledAttachments()
    {
        _weaponLayout.Clear();

        if (_weaponGo == null)
            return;

        GameObjectPoolManager.Instance.Return(_weaponGo);
        _weaponGo = null;
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        // === 位置更新（逻辑帧插值 + 联机等待输入时本地预测）===
        if (_transform != null
            && PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out _))
        {
            _transform.position = new Vector3(x, y, 0);
        }

        if (_weaponGo != null)
        {
            ref readonly var player = ref em.GetComponentSpan<CPlayer>()[entity.Index];
            var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
            if (weaponConfig != null)
            {
                _weaponLayout.Sync(
                    _weaponGo.transform,
                    weaponConfig,
                    player.powerOrbs,
                    player.secondarySlotConvergeT,
                    player.isSlowMode);
            }
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

            _velocityAnimator?.TickHorizontal(moveH);

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