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
    readonly SpriteRenderer[] _bodyRenderers;
    readonly Transform _slowEffectRoot;
    readonly int _slowEffectLayerIndex;

    SpriteRenderer[] _weaponRenderers;

    GameObject _weaponGo;
    readonly WeaponRuntimeLayoutView _weaponLayout = new();

    public PlayerUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
        _animator = gameObject.GetComponent<Animator>();
        _slowEffectRoot = _transform.Find("Slow_Effect");
        _bodyRenderers = CollectBodyRenderersForBlink(gameObject.transform, _slowEffectRoot);
        _velocityAnimator = _animator != null
            ? new PresentationVelocityAnimatorSync.Driver(
                _animator,
                PresentationVelocityAnimatorSync.MotionProfile.HorizontalIdleLeftRight)
            : null;

        _slowEffectLayerIndex = _animator != null ? _animator.GetLayerIndex("Slow Effect") : -1;
        if (_slowEffectLayerIndex == -1 && _animator != null)
            Logger.Warn("Animator missing 'Slow Effect' layer!");

        ApplySlowModeVisual(false);
    }

    /// <summary>慢速特效子树由 Animator 写入 m_IsActive；权重归零不会自动还原，须显式关闭。</summary>
    static SpriteRenderer[] CollectBodyRenderersForBlink(Transform root, Transform slowEffectRoot)
    {
        var all = root.GetComponentsInChildren<SpriteRenderer>(true);
        if (slowEffectRoot == null || all.Length == 0)
            return all;

        int slowRootInstanceId = slowEffectRoot.gameObject.GetInstanceID();
        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (!IsUnderSlowEffect(all[i].transform, slowEffectRoot, slowRootInstanceId))
                count++;
        }

        if (count == all.Length)
            return all;

        var filtered = new SpriteRenderer[count];
        int w = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (!IsUnderSlowEffect(all[i].transform, slowEffectRoot, slowRootInstanceId))
                filtered[w++] = all[i];
        }

        return filtered;
    }

    static bool IsUnderSlowEffect(Transform t, Transform slowEffectRoot, int slowRootInstanceId)
    {
        for (var cur = t; cur != null; cur = cur.parent)
        {
            if (cur.gameObject.GetInstanceID() == slowRootInstanceId)
                return true;
        }

        return false;
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
        _weaponRenderers = _weaponGo.GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void ReleasePooledAttachments()
    {
        _weaponLayout.Clear();

        if (_weaponGo != null)
        {
            GameObjectPoolManager.Instance.Return(_weaponGo);
            _weaponGo = null;
            _weaponRenderers = null;
        }

        ApplySlowModeVisual(false);
    }

    void ApplySlowModeVisual(bool slowMode)
    {
        if (_slowEffectLayerIndex >= 0 && _animator != null)
            _animator.SetLayerWeight(_slowEffectLayerIndex, slowMode ? 1f : 0f);

        if (_slowEffectRoot != null)
            _slowEffectRoot.gameObject.SetActive(slowMode);
    }

    bool ResolveSlowModeForPresentation(in EntityManager em, Entity entity, in CPlayer player, uint logicFrame)
    {
        if (player.playerIndex == RoomManager.LocalPlayerIndex
            && BattleManager.Instance?.ActiveBattleWorld != null)
        {
            var input = InputManager.Instance.SampleLocalInput(
                player.playerIndex,
                logicFrame);
            return input.SlowMode;
        }

        return player.isSlowMode;
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        ref readonly var player = ref em.GetComponentSpan<CPlayer>()[entity.Index];
        uint logicFrame = BattleManager.Instance?.ActiveBattleWorld?.LogicFrameTimer.CurrentFrame ?? 0;
        bool slowMode = ResolveSlowModeForPresentation(em, entity, player, logicFrame);

        // === 位置更新（直接同步逻辑帧坐标）===
        if (_transform != null
            && PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out _))
        {
            _transform.position = new Vector3(x, y, 0);
        }

        if (_weaponGo != null)
        {
            var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
            if (weaponConfig != null)
            {
                _weaponLayout.Sync(
                    _weaponGo.transform,
                    weaponConfig,
                    player.powerOrbs,
                    player.secondarySlotConvergeT,
                    slowMode,
                    em,
                    entity);
            }
        }

        ApplySlowModeVisual(slowMode);

        PlayerInvincibilityPresentation.ApplyBlink(_bodyRenderers, logicFrame, player.invincibleFramesRemaining);
        PlayerInvincibilityPresentation.ApplyBlink(_weaponRenderers, logicFrame, player.invincibleFramesRemaining);

        // === 动画更新 ===
        if (_animator != null)
        {
            sbyte moveH;
            if (player.playerIndex == RoomManager.LocalPlayerIndex
                && BattleManager.Instance?.ActiveBattleWorld != null)
            {
                var input = InputManager.Instance.SampleLocalInput(
                    player.playerIndex,
                    logicFrame);
                moveH = input.MoveHorizontal;
            }
            else
            {
                ref readonly var velocity = ref em.GetComponentSpan<CVelocity>()[entity.Index];
                moveH = velocity.vx > 0 ? (sbyte)1 : (velocity.vx < 0 ? (sbyte)-1 : (sbyte)0);
            }

            _velocityAnimator?.TickHorizontal(moveH);
        }
    }
}
