using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class Enemy : MonoBehaviour
{
    //public DanmakuSystem danmakuSystem;

    [SerializeField] protected EnemyConfig enemyConfig;

    [SerializeField] protected E_EnemyType enemyType;
    [SerializeField] protected E_EnemyName enemyName;

    [SerializeField] protected Vector2 colliderSize;

    public ICollider Collider;

    public Vector3 Postion
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    void Start()
    {
        Collider = new RectCollider(E_ColliderLayer.Enemy, transform.position, colliderSize);
        //danmakuSystem.AddEnemyCollider(Collider);
    }

    public void LoadEnemyConfig()
    {
        if (enemyConfig == null) return;

        enemyType = enemyConfig.EnemyType;
        enemyName = enemyConfig.EnemyName;
        colliderSize = enemyConfig.ColliderSize;

        OnEnemyConfigLoad();
    }

    protected virtual void OnEnemyConfigLoad()
    {

    }

    public void SaveEnemyConfig()
    {
        if (enemyConfig == null) return;

        enemyConfig.EnemyType = enemyType;
        enemyConfig.EnemyName = enemyName;
        enemyConfig.ColliderSize = colliderSize;

        OnEnemyConfigSave();

        UnityEditor.EditorUtility.SetDirty(enemyConfig);
        UnityEditor.AssetDatabase.SaveAssets();
    }

    protected virtual void OnEnemyConfigSave()
    {

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, colliderSize);
    }
}
