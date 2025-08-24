using System.Collections.Generic;
using UnityEngine;
public class QuadtreeObject<T>
{
    public T obj;
    public Rect rect;

    public QuadtreeObject(T obj, Rect rect)
    {
        this.obj = obj;
        this.rect = rect;
    }
}
public class Quadtree<T> where T : class
{
    int maxObjects;
    int maxLevels;

    int level;
    Rect bounds;
    List<QuadtreeObject<T>> objects;
    Quadtree<T>[] nodes;

    public Quadtree(int level, int maxObjects, int maxLevels, Rect bounds)
    {
        this.level = level;
        this.maxObjects = maxObjects;
        this.maxLevels = maxLevels;
        this.bounds = bounds;
        objects = new List<QuadtreeObject<T>>();
        nodes = new Quadtree<T>[4];
    }

    // 清除四叉树
    public void Clear()
    {
        objects.Clear();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null)
            {
                nodes[i].Clear();
                nodes[i] = null;
            }
        }
    }

    // 分割为四个子节点
    void Split()
    {
        float subWidth = bounds.width / 2f;
        float subHeight = bounds.height / 2f;
        float x = bounds.x;
        float y = bounds.y;

        nodes[0] = new Quadtree<T>(level + 1, maxObjects, maxLevels,
                                  new Rect(x + subWidth, y, subWidth, subHeight));
        nodes[1] = new Quadtree<T>(level + 1, maxObjects, maxLevels,
                                  new Rect(x, y, subWidth, subHeight));
        nodes[2] = new Quadtree<T>(level + 1, maxObjects, maxLevels,
                                  new Rect(x, y + subHeight, subWidth, subHeight));
        nodes[3] = new Quadtree<T>(level + 1, maxObjects, maxLevels,
                                  new Rect(x + subWidth, y + subHeight, subWidth, subHeight));
    }

    // 确定对象属于哪个象限
    int GetIndex(Rect rect)
    {
        int index = -1;
        float verticalMidpoint = bounds.x + (bounds.width / 2f);
        float horizontalMidpoint = bounds.y + (bounds.height / 2f);

        bool topQuadrant = rect.y < horizontalMidpoint && rect.y + rect.height < horizontalMidpoint;
        bool bottomQuadrant = rect.y > horizontalMidpoint;

        if (rect.x < verticalMidpoint && rect.x + rect.width < verticalMidpoint)
        {
            if (topQuadrant) index = 1;
            else if (bottomQuadrant) index = 2;
        }
        else if (rect.x > verticalMidpoint)
        {
            if (topQuadrant) index = 0;
            else if (bottomQuadrant) index = 3;
        }

        return index;
    }

    // 插入对象
    public void Insert(QuadtreeObject<T> obj)
    {
        if (nodes[0] != null)
        {
            int index = GetIndex(obj.rect);
            if (index != -1)
            {
                nodes[index].Insert(obj);
                return;
            }
        }

        objects.Add(obj);

        if (objects.Count > maxObjects && level < maxLevels)
        {
            if (nodes[0] == null) Split();

            int i = 0;
            while (i < objects.Count)
            {
                int index = GetIndex(objects[i].rect);
                if (index != -1)
                {
                    nodes[index].Insert(objects[i]);
                    objects.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
    }

    // 检索可能碰撞的对象
    public List<T> Retrieve(Rect rect)
    {
        List<T> returnObjects = new List<T>();
        int index = GetIndex(rect);

        if (nodes[0] != null)
        {
            if (index != -1)
            {
                returnObjects.AddRange(nodes[index].Retrieve(rect));
            }
            else
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    returnObjects.AddRange(nodes[i].Retrieve(rect));
                }
            }
        }

        for (int i = 0; i < objects.Count; i++)
        {
            returnObjects.Add(objects[i].obj);
        }

        return returnObjects;
    }
}