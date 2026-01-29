using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected EnemyConfig enemyConfig;  

    [Header("碰撞体设置")]
    [SerializeField] protected Vector2 colliderSize;

    [Header("敌人属性设置")]
    [SerializeField] protected EnemyType enemyType;
    [SerializeField] protected float maxHealth;

    [Header("音频资源设置")]
    [SerializeField] protected AudioName dieAudioName;

    //protected ColliderComponent Collider;

    public Vector3 Postion
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    void Awake()
    {
        LoadEnemyConfig();
        InitCollider();
    }

    void InitCollider()
    {
        //Collider = new RectCollider(this, E_ColliderLayer.Enemy, _transform.position, colliderSize);
        //Collider.OnCollide += OnHitted;
        //CollisionSystem.AddCollider(Collider);
    }

    public void LoadEnemyConfig()
    {
        if (enemyConfig == null) return;

        enemyType = enemyConfig.EnemyType;
        colliderSize = enemyConfig.ColliderSize;

        Logger.Debug("已加载敌人配置：" + enemyConfig.name);
        
    }

    //public virtual void OnHitted(ColliderComponent other)
    //{
    //    DanmakuPrefabTool danmaku = other.Owner as DanmakuPrefabTool;
    //    if (danmaku != null)
    //    {
    //        maxHealth -= danmaku.damage;
    //        if (maxHealth <= 0)
    //        {
    //            Die();
    //        }
    //    }
    //}

    public virtual void Die()
    {
        AudioManager.Instance.PlayAudio(dieAudioName);
        //CollisionSystem.RemoveCollider(Collider);
        EnemyManager.Instance.RemoveEnemy(this);        
    }
}
