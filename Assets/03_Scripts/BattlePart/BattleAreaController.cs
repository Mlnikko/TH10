using UnityEngine;

public class BattleAreaController : MonoBehaviour
{
    [Header("边界设置")]
    public float padding; // 边界缓冲区域

    [Header("调试设置")]
    public bool showDebugGizmos = true;
    public Color debugColor = new (1, 0, 0, 0.3f);

    // 区域属性
    public Rect WorldRect { get; private set; }
    public Rect NormalizedRect { get; private set; }

    Camera _mainCamera;
    RectTransform _rectTransform;

    void Awake()
    {
        _mainCamera = Camera.main;
        _rectTransform = GetComponent<RectTransform>();
        UpdateBattleRect();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (showDebugGizmos)
            UpdateBattleRect();
#endif
    }

    public void UpdateBattleRect()
    {
        // 获取UI矩形在世界空间的实际边界
        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);

        // 计算世界空间矩形
        WorldRect = new Rect(
            corners[0].x,
            corners[0].y,
            corners[3].x - corners[0].x,
            corners[1].y - corners[0].y
        );


        // 计算标准化矩形（0-1范围）
        Vector2 minScreen = _mainCamera.WorldToScreenPoint(corners[0]);
        Vector2 maxScreen = _mainCamera.WorldToScreenPoint(corners[2]);

        NormalizedRect = new Rect(
            minScreen.x / Screen.width,
            minScreen.y / Screen.height,
            (maxScreen.x - minScreen.x) / Screen.width,
            (maxScreen.y - minScreen.y) / Screen.height
        );

        // 应用安全边距
        ApplyPadding();
    }

    void ApplyPadding()
    {
        WorldRect = new Rect(
            WorldRect.x + WorldRect.width * padding,
            WorldRect.y + WorldRect.height * padding,
            WorldRect.width * (1 - padding * 2),
            WorldRect.height * (1 - padding * 2)
        );
    }

    // 世界坐标转换
    public Vector2 WorldToBattlePosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - new Vector3(WorldRect.x, WorldRect.y, 0);
        return new Vector2(
            Mathf.Clamp01(localPos.x / WorldRect.width),
            Mathf.Clamp01(localPos.y / WorldRect.height)
        );
    }

    public Vector3 BattleToWorldPosition(Vector2 battlePos)
    {
        return new Vector3(
            WorldRect.x + battlePos.x * WorldRect.width,
            WorldRect.y + battlePos.y * WorldRect.height,
            0
        );
    }

    // 边界检查
    public bool IsInsideBattleArea(Vector3 worldPos)
    {
        return WorldRect.Contains(worldPos);
    }

    public Vector3 ClampToBattleArea(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.Clamp(worldPos.x, WorldRect.xMin, WorldRect.xMax),
            Mathf.Clamp(worldPos.y, WorldRect.yMin, WorldRect.yMax),
            worldPos.z
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        Gizmos.color = debugColor;

        // 绘制边界区域
        Vector3[] corners = {
            new Vector3(WorldRect.xMin, WorldRect.yMin, 0),
            new Vector3(WorldRect.xMax, WorldRect.yMin, 0),
            new Vector3(WorldRect.xMax, WorldRect.yMax, 0),
            new Vector3(WorldRect.xMin, WorldRect.yMax, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        // 绘制中心标记
        Vector3 center = new Vector3(
            WorldRect.x + WorldRect.width / 2,
            WorldRect.y + WorldRect.height / 2,
            0
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, 0.2f);

        // 坐标轴标识
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, center + Vector3.right);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(center, center + Vector3.up);
    }
#endif
}
