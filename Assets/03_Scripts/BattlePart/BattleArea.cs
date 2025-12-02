using UnityEngine;

public class BattleArea : MonoBehaviour
{
    [Header("运行时生成的配置（只读）")]
    [SerializeField] private BattleAreaConfig config = new();

    [Header("编辑器参数")]
    [Tooltip("弹幕回收外扩距离（世界单位，建议 50~200）")]
    public Vector2 danmakuRecycleBounds = new(0.5f, 0.5f);

    [Tooltip("用于 DeterministicGrid 的单元格尺寸")]
    [Min(1)] public int gridCellSize = 64;

    [Header("可视化")]
    [SerializeField] private bool showGrid = true;

    private void Awake()
    {
        InitBattleArea();
    }

    /// <summary>
    /// 从当前 Transform 自动生成 BattleAreaConfig
    /// 调用后 config 即可用于其他系统
    /// </summary>
    public void InitBattleArea()
    {
        Vector3 size = transform.localScale;
        Vector3 center = transform.position;

        config.Width = size.x;
        config.Height = size.y;
        config.Center = new Vector2(center.x, center.y);
        config.DanmakuRecycleMargin = danmakuRecycleBounds;
        config.GridCellSize = gridCellSize;
    }

    /// <summary>
    /// 获取战斗配置（供 CollisionSystem 等使用）
    /// </summary>
    public BattleAreaConfig GetConfig() => config;

    void OnDrawGizmos()
    {
        // 临时计算当前配置（即使未 Awake 也能预览）
        Vector3 size = transform.localScale;
        Vector3 center = transform.position;
        Rect battleRect = new Rect(
            center.x - size.x * 0.5f,
            center.y - size.y * 0.5f,
            size.x,
            size.y
        );

        // 绘制弹幕回收边界（红色）
        Gizmos.color = Color.red;
        Vector3 recycleSize = new Vector3(
            size.x + danmakuRecycleBounds.x * 2f,
            size.y + danmakuRecycleBounds.y * 2f,
            0
        );
        Gizmos.DrawWireCube(center, recycleSize);

        // 绘制战斗区域（绿色）
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0));

        // 绘制网格（浅绿）
        if (showGrid && gridCellSize > 0)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
            float cellSize = gridCellSize;

            // 计算覆盖整个战斗区域的网格范围
            float left = battleRect.xMin;
            float right = battleRect.xMax;
            float bottom = battleRect.yMin;
            float top = battleRect.yMax;

            // 垂直线
            for (float x = left; x <= right + 0.1f; x += cellSize)
            {
                Gizmos.DrawLine(new Vector3(x, bottom), new Vector3(x, top));
            }
            // 水平线
            for (float y = bottom; y <= top + 0.1f; y += cellSize)
            {
                Gizmos.DrawLine(new Vector3(left, y), new Vector3(right, y));
            }
        }
    }
}
