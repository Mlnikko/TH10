using UnityEngine;

public class CharacterController : MonoBehaviour
{
    CharacterConfig characterConfig;

    [SerializeField] PlayerEmitters playerEmitters;
    [SerializeField] GameObject checkPoint;   
    
    Vector3 Position
    {
        get
        {
            return transform.position;
        }
        set
        {
            transform.position = value;
        }
    }

    Animator animator;
    IPlayerAnim characterAnim;

    CircleCollider bodyCollider;
    CircleCollider grazeCollider;

    Vector2 moveDir;
    bool leftInput;
    bool rightInput;
    bool upInput;
    bool downInput;

    bool isSlowMode;
    bool isShooting;

    float speed;
    float slowSpeed;

    void Awake()
    {
        characterConfig = GetComponent<CharacterPrefabTool>().CharacterConfig;
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        InputManager.Instance.OnKeyInput_Down += HandleDownInput;
        InputManager.Instance.OnKeyInput_Left += HandleLeftInput;
        InputManager.Instance.OnKeyInput_Right += HandleRightInput;
        InputManager.Instance.OnKeyInput_Up += HandleUpInput;
        InputManager.Instance.OnKeyInput_LS += HandleLSInput;
        InputManager.Instance.OnKeyInput_Z += HandleZInput;
        InputManager.Instance.OnKeyInput_X += HandleXInput;
    }

    void Start()
    {
        InitPlayer();
    }

    void Update()
    {
        UpdatePlayerMove();
    }

    void InitPlayer()
    {
        if (characterConfig == null) return;

        switch (characterConfig.CharacterName) 
        {
            case E_CharacterName.Reimu:
                characterAnim = new ReimuAnim();
                break;
            case E_CharacterName.Marisa:
                characterAnim = new MarisaAnim();
                break;
        }

        speed = characterConfig.Speed;
        slowSpeed = characterConfig.SlowSpeed;

        bodyCollider = new CircleCollider(E_ColliderLayer.Player, Position, characterConfig.HitRadius);
        grazeCollider = new CircleCollider(E_ColliderLayer.Player, Position, characterConfig.GrazeRadius);
    }

    #region  ‰»Î¬ﬂº≠¥¶¿Ì
    void HandleLSInput(bool isShift)
    {
        isSlowMode = isShift;
        checkPoint.SetActive(isSlowMode);
    }
    void HandleDownInput(bool isDown)
    {
        downInput = isDown;
        if (downInput) moveDir.y = -1;
        else if (upInput) moveDir.y = 1;
        else moveDir.y = 0;
    }
    void HandleUpInput(bool isUp)
    {
        upInput = isUp;
        if (upInput) moveDir.y = 1;
        else if (downInput) moveDir.y = -1;
        else moveDir.y = 0;
    }
    void HandleLeftInput(bool isLeft)
    {
        leftInput = isLeft;
        if (leftInput) moveDir.x = -1;
        else if (rightInput) moveDir.x = 1;
        else moveDir.x = 0;
        UpdatePlayerAnim();
    }
    void HandleRightInput(bool isRight)
    {
        rightInput = isRight;
        if (rightInput) moveDir.x = 1;
        else if (leftInput) moveDir.x = -1;
        else moveDir.x = 0;
        UpdatePlayerAnim();
    }
    void HandleZInput(bool isZ)
    {
        isShooting = isZ;

        if (playerEmitters == null) return;
        playerEmitters.EnableAllEmitters(isShooting);
    }
    void HandleXInput()
    {
        ReleaseBoom();
    }
    #endregion

    void UpdatePlayerMove()
    {
        if (moveDir == Vector2.zero) return;

        float moveX, moveY;

        if (isSlowMode)
        {
            moveX = moveDir.x * slowSpeed * Time.deltaTime;
            moveY = moveDir.y * slowSpeed * Time.deltaTime;
        }
        else
        {
            moveX = moveDir.x * speed * Time.deltaTime;
            moveY = moveDir.y * speed * Time.deltaTime;
        }

        float movedPosX = Position.x + moveX;
        float movedPosY = Position.y + moveY;

        if (movedPosX + characterConfig.MoveBoxOffset.x - characterConfig.MoveBoxSize.x / 2 < DanmakuArea.battleRect.xMin) movedPosX = Position.x;
        if (movedPosX + characterConfig.MoveBoxOffset.x + characterConfig.MoveBoxSize.x / 2 > DanmakuArea.battleRect.xMax) movedPosX = Position.x;
        if (movedPosY + characterConfig.MoveBoxOffset.y + characterConfig.MoveBoxSize.y / 2 > DanmakuArea.battleRect.yMax) movedPosY = Position.y;
        if (movedPosY + characterConfig.MoveBoxOffset.y - characterConfig.MoveBoxSize.y / 2 < DanmakuArea.battleRect.yMin) movedPosY = Position.y;

        Position = new(movedPosX, movedPosY, Position.z);
    }
    void UpdatePlayerAnim()
    {
        if (moveDir.x == 1)
        {
            animator.Play(characterAnim.GetRightMoveStartAnimName());
        }
        else if (moveDir.x == -1)
        {
            animator.Play(characterAnim.GetLeftMoveStartAnimName());
        }
        else
        {
            animator.Play(characterAnim.GetIdleAnimName());
        }
    }
    void ReleaseBoom()
    {

    }

    void OnDisable()
    {
        if(InputManager.Instance == null) return;
        InputManager.Instance.OnKeyInput_Down -= HandleDownInput;
        InputManager.Instance.OnKeyInput_Left -= HandleLeftInput;
        InputManager.Instance.OnKeyInput_Right -= HandleRightInput;
        InputManager.Instance.OnKeyInput_Up -= HandleUpInput;
        InputManager.Instance.OnKeyInput_LS -= HandleLSInput;
        InputManager.Instance.OnKeyInput_Z -= HandleZInput;
        InputManager.Instance.OnKeyInput_X -= HandleXInput;
    }
}