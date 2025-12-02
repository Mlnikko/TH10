using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
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

    // ECS 相关
    Entity playerEntity;
    Entity bodyColliderEntity;
    Entity grazeColliderEntity;

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
        characterConfig = GetComponent<CharacterConfiger>().CharacterConfig;
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
            case E_Character.Reimu:
                characterAnim = new ReimuAnim();
                break;
            case E_Character.Marisa:
                characterAnim = new MarisaAnim();
                break;
        }

        speed = characterConfig.Speed;
        slowSpeed = characterConfig.SlowSpeed;
    }
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
        
        
    }
    void HandleXInput()
    {
        ReleaseBoom();
    }

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

        //if (movedPosX + characterConfig.MoveBoxOffset.x - characterConfig.MoveBoxSize.x / 2 < BattleArea.battleRect.xMin) movedPosX = Position.x;
        //if (movedPosX + characterConfig.MoveBoxOffset.x + characterConfig.MoveBoxSize.x / 2 > BattleArea.battleRect.xMax) movedPosX = Position.x;
        //if (movedPosY + characterConfig.MoveBoxOffset.y + characterConfig.MoveBoxSize.y / 2 > BattleArea.battleRect.yMax) movedPosY = Position.y;
        //if (movedPosY + characterConfig.MoveBoxOffset.y - characterConfig.MoveBoxSize.y / 2 < BattleArea.battleRect.yMin) movedPosY = Position.y;

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
    
    void OnDestroy()
    {
        // 清理 ECS 实体
        if (BattleManager.Instance?.EntityManager != null)
        {
            var entityManager = BattleManager.Instance.EntityManager;
            
            if (!playerEntity.IsNull) entityManager.DestroyEntity(playerEntity);
            if (!bodyColliderEntity.IsNull) entityManager.DestroyEntity(bodyColliderEntity);
            if (!grazeColliderEntity.IsNull) entityManager.DestroyEntity(grazeColliderEntity);
        }
    }
}