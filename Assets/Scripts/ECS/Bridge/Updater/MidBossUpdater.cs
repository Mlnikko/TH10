using UnityEngine;

/// <summary>
/// 中场 Boss 表现：位置同步 + 按 <see cref="CMidBossEncounter"/> 阶段切换 Animator 状态。
/// </summary>
public class MidBossUpdater : IGameObjectUpdater
{
    readonly Transform _transform;
    readonly Animator _animator;
    readonly MidBossEncounterConfig _encounter;
    E_MidBossPhase _lastPhase = (E_MidBossPhase)255;

    public MidBossUpdater(GameObject gameObject, MidBossEncounterConfig encounter)
    {
        _transform = gameObject.transform;
        _animator = gameObject.GetComponent<Animator>();
        _encounter = encounter;
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        if (_transform != null
            && PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out _))
        {
            _transform.position = new Vector3(x, y, 0);
        }

        if (_animator == null || _encounter == null || !em.HasComponent<CMidBossEncounter>(entity))
            return;

        ref readonly var mid = ref em.GetComponent<CMidBossEncounter>(entity);
        if (mid.phase == _lastPhase)
            return;

        _lastPhase = mid.phase;
        string state = ResolveAnimatorState(mid.phase);
        if (!string.IsNullOrEmpty(state))
            _animator.Play(state);
    }

    string ResolveAnimatorState(E_MidBossPhase phase) => phase switch
    {
        E_MidBossPhase.Entry => _encounter.animatorStateEntry,
        E_MidBossPhase.OnField => _encounter.animatorStateLoop,
        E_MidBossPhase.Exit => _encounter.animatorStateExit,
        _ => _encounter.animatorStateIdle,
    };
}
