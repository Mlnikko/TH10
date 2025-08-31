using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_ColliderType
{
    None,
    Rect,
    Circle,
}

public enum E_ColliderLayer
{
    Default = 0,
    Player = 1,
    Enemy = 2,
    PlayerDanmaku = 3,
    EnemyDanmaku = 4,
    Item = 5,
}

public class CollisionSystem
{
    Rect collisionRect;

    static Queue<ICollider> addColliderQueue = new();
    static Queue<ICollider> removeColliderQueue = new();

    List<ICollider> allActiveColliders = new();

    int gridRows;
    int gridColumns;

    // 用二维数组存储网格划分的矩形
    Rect[,] gridRects;

    // 管理网格中的游戏对象
    Dictionary<Vector2Int, List<ICollider>> gridColliders = new();


    public CollisionSystem(Rect collisionRect, int gridRows, int gridColumns)
    {
        this.collisionRect = collisionRect;
        this.gridRows = gridRows;
        this.gridColumns = gridColumns;
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        // 从左下角开始计算网格划分
        Vector2 origin = new(collisionRect.xMin, collisionRect.yMin);

        Vector2 gridSize = new(collisionRect.width / gridColumns, collisionRect.height / gridRows);

        gridRects = new Rect[gridColumns, gridRows];

        for (int i = 0; i < gridColumns; i++)
        {
            for (int j = 0; j < gridRows; j++)
            {
                Vector2 pos = new(origin.x + i * gridSize.x, origin.y + j * gridSize.y);
                gridRects[i, j] = new Rect(pos, gridSize);
            }
        }
    }

    public static void AddCollider(ICollider collider)
    {
        addColliderQueue.Enqueue(collider);                                                                                                                                                                                                                                                                         
    }

    public static void RemoveCollider(ICollider collider)
    {
        removeColliderQueue.Enqueue(collider);
    }

    public void Update()
    {
        HandleAddCollider();

        UpdateColliderData();

        CheckCollisions();

        HandleRemoveCollider();
    }

    void HandleAddCollider()
    {
        while (addColliderQueue.Count > 0)
        {
            ICollider collider = addColliderQueue.Dequeue();

            if (collider == null) return;

            allActiveColliders.Add(collider);

            AddColliderToGrid(collider);
        }
    }

    void AddColliderToGrid(ICollider collider)
    {
        Vector2Int gridPos = WorldToGrid(collider.GetBounds().center);
        if (gridPos.x == -1 || gridPos.y == -1) return;
        if (!gridColliders.ContainsKey(gridPos))
        {
            gridColliders[gridPos] = new List<ICollider>();
        }
        if (!gridColliders[gridPos].Contains(collider))
        {
            gridColliders[gridPos].Add(collider);
        }
    }

    void HandleRemoveCollider()
    {
        while (removeColliderQueue.Count > 0)
        {
            ICollider collider = removeColliderQueue.Dequeue();
            
            if (collider == null) return;

            allActiveColliders.Remove(collider);

            RemoveColliderFromGrid(collider);
        }
    }

    void RemoveColliderFromGrid(ICollider collider)
    {
        Vector2Int gridPos = WorldToGrid(collider.GetBounds().center);
        if (gridPos.x == -1 || gridPos.y == -1) return;
        if (gridColliders.ContainsKey(gridPos))
        {
            gridColliders[gridPos].Remove(collider);
        }
    }

    void UpdateColliderData()
    { 
        foreach (var collider in allActiveColliders)
        {
            UpdateColliderGrid(collider);
        }
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        if (gridRects == null) return new Vector2Int(-1, -1);
        for (int i = 0; i < gridRows; i++)
        {
            for (int j = 0; j < gridColumns; j++)
            {
                if (gridRects[i, j].Contains(worldPos))
                {
                    return new Vector2Int(i, j);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    List<ICollider> GetCollidersInGrid(Vector2Int gridPos)
    {
        if (gridColliders.ContainsKey(gridPos))
        {
            return gridColliders[gridPos];
        }
        return new List<ICollider>();
    }

    public void UpdateColliderGrid(ICollider collider)
    {
        RemoveColliderFromGrid(collider);
        AddColliderToGrid(collider);
    }

    void CheckCollisions()
    {
        foreach (var collider in allActiveColliders)
        {
            Rect bounds = collider.GetBounds();
            // 获取四个角所在的网格
            Vector2Int topLeftGrid = WorldToGrid(new Vector2(bounds.xMin, bounds.yMax));
            Vector2Int topRightGrid = WorldToGrid(new Vector2(bounds.xMax, bounds.yMax));
            Vector2Int bottomLeftGrid = WorldToGrid(new Vector2(bounds.xMin, bounds.yMin));
            Vector2Int bottomRightGrid = WorldToGrid(new Vector2(bounds.xMax, bounds.yMin));

            // 使用HashSet避免重复检测
            HashSet<ICollider> potentialColliders = new();
            potentialColliders.UnionWith(GetCollidersInGrid(topLeftGrid));
            potentialColliders.UnionWith(GetCollidersInGrid(topRightGrid));
            potentialColliders.UnionWith(GetCollidersInGrid(bottomLeftGrid));
            potentialColliders.UnionWith(GetCollidersInGrid(bottomRightGrid));

            // 检测碰撞
            foreach (var other in potentialColliders)
            {
                if (other != collider && CheckCollision(collider, other))
                {
                    collider.OnCollisionEnter(other);
                }
            }
        }
    }

    #region 碰撞检查处理

    /// <summary>
    /// 自定义碰撞层检测规则
    /// </summary>
    /// <param name="layerA"></param>
    /// <param name="layerB"></param>
    /// <returns></returns>
    static bool CheckLayer(E_ColliderLayer layerA, E_ColliderLayer layerB)
    {
        if(layerA == E_ColliderLayer.PlayerDanmaku && layerB == E_ColliderLayer.Enemy) return true;
        else if (layerA == E_ColliderLayer.EnemyDanmaku && layerB == E_ColliderLayer.Player) return true;
        else if (layerA == E_ColliderLayer.Player && (layerB == E_ColliderLayer.EnemyDanmaku || layerB == E_ColliderLayer.Enemy)) return true;
        else if (layerA == E_ColliderLayer.Enemy && (layerB == E_ColliderLayer.PlayerDanmaku || layerB == E_ColliderLayer.Player)) return true;
        return false;
    }

    static bool CheckCollision(ICollider a, ICollider b)
    {
        if(a == null || b == null) return false;

        if (!CheckLayer(a.ColliderLayer,b.ColliderLayer)) return false;

        if (a.ColliderType == E_ColliderType.Circle && b.ColliderType == E_ColliderType.Circle)
        {
            return CheckCircleCircle((CircleCollider)a, (CircleCollider)b);
        }
        else if (a.ColliderType == E_ColliderType.Circle && b.ColliderType == E_ColliderType.Rect)
        {
            return CheckCircleRect((CircleCollider)a, (RectCollider)b);
        }
        else if (a.ColliderType == E_ColliderType.Rect && b.ColliderType == E_ColliderType.Circle)
        {
            return CheckCircleRect((CircleCollider)b, (RectCollider)a);
        }
        else if (a.ColliderType == E_ColliderType.Rect && b.ColliderType == E_ColliderType.Rect)
        {
            return CheckRectRect((RectCollider)a, (RectCollider)b);
        }

        return false;
    }

    #endregion

    #region 碰撞检查算法
    /// <summary>
    /// 圆形圆形碰撞检测
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static bool CheckCircleCircle(CircleCollider a, CircleCollider b)
    {
        float distance = Vector2.Distance(a.CenterPos, b.CenterPos);
        return distance < (a.Radius + b.Radius);
    }

    /// <summary>
    /// 圆形矩形碰撞检测
    /// </summary>
    /// <param name="circle"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    static bool CheckCircleRect(CircleCollider circle, RectCollider rect)
    {
        Rect bounds = rect.GetBounds();
        float closestX = Mathf.Clamp(circle.CenterPos.x, bounds.x, bounds.x + bounds.width);
        float closestY = Mathf.Clamp(circle.CenterPos.y, bounds.y, bounds.y + bounds.height);

        float distanceX = circle.CenterPos.x - closestX;
        float distanceY = circle.CenterPos.y - closestY;
        float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);

        return distanceSquared < (circle.Radius * circle.Radius);
    }

    /// <summary>
    /// 矩形矩形碰撞检测
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static bool CheckRectRect(RectCollider a, RectCollider b)
    {
        return a.GetBounds().Overlaps(b.GetBounds());
    }
    #endregion
}
