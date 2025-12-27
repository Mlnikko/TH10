using UnityEngine;

public class PlayerUpdater : IGameObjectUpdater
{
    readonly Transform _transform;
    readonly Animator _animator;
    public PlayerUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
        _animator = gameObject.GetComponent<Animator>();
    }
    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        if (!em.IsValid(entity)) return;

        // 更新位置
        if (_transform != null)
        {
            ref readonly var pos = ref em.GetComponentSpan<CPosition>()[entity.Index];
            _transform.position = new Vector3(pos.x, pos.y, 0);
        }

        // 更新动画

        //if (_animator != null)
        //{
        //    ref readonly var moveDir = ref em.GetComponentSpan<CPlayerMoveDir>()[entity.Index];
        //    if (moveDir.x == 1)
        //    {
        //        _animator.Play("Player_Move_Right");
        //    }
        //    else if (moveDir.x == -1)
        //    {
        //        _animator.Play("Player_Move_Left");
        //    }
        //    else
        //    {
        //        _animator.Play("Player_Idle");
        //    }
        //}
    }
}