using UnityEngine;

/// <summary>
/// 根据逻辑 <see cref="CVelocity"/>（每逻辑帧位移）驱动 Animator 待机/移动状态，供玩家与敌人表现 Updater 共用。
/// </summary>
public static class PresentationVelocityAnimatorSync
{
    public enum MotionProfile : byte
    {
        None = 0,
        /// <summary>Player_Idle / Player_Move_Left / Player_Move_Right</summary>
        HorizontalIdleLeftRight,
        /// <summary>仅 Idle + Move 两态（无 Move_Start）</summary>
        IdleMove,
        /// <summary>
        /// Idle → Move_Start → Move_Loop（Controller 内过渡）；动画仅朝右制作。
        /// 向左移动由 <see cref="PresentationHorizontalFlip"/> 翻转 Sprite，妖精与 Boss 共用。
        /// </summary>
        EnterMoveLoop,
    }

    public sealed class Driver
    {
        /// <summary>逻辑帧位移阈值；关底 Boss 慢速水平路径约 0.009/帧，需低于 0.01。</summary>
        const float DefaultMoveThreshold = 0.001f;

        static readonly string[] s_enterLoopIdleNames = { "Boss_Idle", "Fairy_Idle" };
        static readonly string[] s_enterLoopMoveStartNames = { "Boss_Move_Start", "Fairy_Move_Start" };
        static readonly string[] s_enterLoopMoveLoopNames = { "Boss_Move_Loop", "Fairy_Move_Loop" };
        static readonly string[] s_idleMoveIdleNames = { "Boss_Idle", "Idle" };
        static readonly string[] s_idleMoveMoveNames = { "Boss_Move", "Move" };

        readonly Animator _animator;
        readonly MotionProfile _profile;
        readonly int _idleHash;
        readonly int _moveHash;
        readonly int _moveStartHash;
        readonly int _moveLeftHash;
        readonly int _moveRightHash;

        int _lastMotionKind = int.MinValue;

        public Driver(Animator animator, MotionProfile profile = MotionProfile.None)
        {
            _animator = animator;
            _profile = profile == MotionProfile.None ? DetectProfile(animator) : profile;
            (_idleHash, _moveHash, _moveStartHash, _moveLeftHash, _moveRightHash) = ResolveStateHashes(animator, _profile);
        }

        public bool HasAnimator => _animator != null;
        public MotionProfile Profile => _profile;

        public void Tick(float vx, float vy, float moveThreshold = DefaultMoveThreshold)
        {
            if (_animator == null || _profile == MotionProfile.None)
                return;

            bool moving = vx * vx + vy * vy > moveThreshold * moveThreshold;
            switch (_profile)
            {
                case MotionProfile.IdleMove:
                    ApplyIdleMove(moving);
                    break;
                case MotionProfile.EnterMoveLoop:
                    ApplyEnterMoveLoop(moving);
                    break;
            }
        }

        /// <summary>水平输入：-1 左，0 待机，1 右。</summary>
        public void TickHorizontal(int direction)
        {
            if (_animator == null || _profile != MotionProfile.HorizontalIdleLeftRight)
                return;

            int kind = direction > 0 ? 1 : (direction < 0 ? -1 : 0);
            if (kind == _lastMotionKind)
                return;

            _lastMotionKind = kind;
            switch (kind)
            {
                case 1:
                    Play(_moveRightHash);
                    break;
                case -1:
                    Play(_moveLeftHash);
                    break;
                default:
                    Play(_idleHash);
                    break;
            }
        }

        void ApplyIdleMove(bool moving)
        {
            int kind = moving ? 1 : 0;
            if (kind == _lastMotionKind)
                return;

            _lastMotionKind = kind;
            Play(moving ? _moveHash : _idleHash);
        }

        void ApplyEnterMoveLoop(bool moving)
        {
            if (!moving)
            {
                if (_lastMotionKind == 0)
                    return;

                _lastMotionKind = 0;
                Play(_idleHash);
                return;
            }

            _lastMotionKind = 1;
            if (IsInMoveState())
                return;

            // hold 结束或慢速水平段：Animator 仍在 Idle 时补播移动（含向左）
            if (IsInIdleState() && _moveStartHash != 0)
                Play(_moveStartHash);
            else
                Play(_moveHash != 0 ? _moveHash : _moveStartHash);
        }

        bool IsInMoveState()
        {
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            int hash = state.shortNameHash;
            if (_moveHash != 0 && hash == _moveHash)
                return true;
            if (_moveStartHash != 0 && hash == _moveStartHash)
                return true;

            return false;
        }

        bool IsInIdleState()
        {
            if (_idleHash == 0)
                return false;

            return _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _idleHash;
        }

        void Play(int stateHash)
        {
            if (stateHash == 0)
                return;

            _animator.Play(stateHash, 0, 0f);
        }

        static MotionProfile DetectProfile(Animator animator)
        {
            if (animator == null)
                return MotionProfile.None;

            if (HasState(animator, "Player_Idle")
                && HasState(animator, "Player_Move_Left")
                && HasState(animator, "Player_Move_Right"))
                return MotionProfile.HorizontalIdleLeftRight;

            if (HasAnyState(animator, s_enterLoopIdleNames)
                && HasAnyState(animator, s_enterLoopMoveStartNames))
                return MotionProfile.EnterMoveLoop;

            if (HasAnyState(animator, s_idleMoveIdleNames)
                && HasAnyState(animator, s_idleMoveMoveNames))
                return MotionProfile.IdleMove;

            return MotionProfile.None;
        }

        static (int idle, int move, int moveStart, int moveLeft, int moveRight) ResolveStateHashes(
            Animator animator,
            MotionProfile profile)
        {
            if (animator == null || profile == MotionProfile.None)
                return (0, 0, 0, 0, 0);

            return profile switch
            {
                MotionProfile.HorizontalIdleLeftRight => (
                    Hash("Player_Idle"),
                    0,
                    0,
                    Hash("Player_Move_Left"),
                    Hash("Player_Move_Right")),
                MotionProfile.EnterMoveLoop => (
                    FirstHash(animator, s_enterLoopIdleNames),
                    FirstHash(animator, s_enterLoopMoveLoopNames),
                    FirstHash(animator, s_enterLoopMoveStartNames),
                    0,
                    0),
                MotionProfile.IdleMove => (
                    FirstHash(animator, s_idleMoveIdleNames),
                    FirstHash(animator, s_idleMoveMoveNames),
                    0,
                    0,
                    0),
                _ => (0, 0, 0, 0, 0),
            };

            static int Hash(string name) => string.IsNullOrEmpty(name) ? 0 : Animator.StringToHash(name);
        }

        static bool HasAnyState(Animator animator, string[] stateNames)
        {
            for (int i = 0; i < stateNames.Length; i++)
            {
                if (HasState(animator, stateNames[i]))
                    return true;
            }

            return false;
        }

        static int FirstHash(Animator animator, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (HasState(animator, names[i]))
                    return Animator.StringToHash(names[i]);
            }

            return 0;
        }

        static bool HasState(Animator animator, string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
                return false;

            return animator.HasState(0, Animator.StringToHash(stateName));
        }
    }
}
