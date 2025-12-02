using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEmitters : MonoBehaviour
{
    List<DanmakuEmitter> allEmitters = new();

    void Awake()
    {
        // 获取所有子物体发射器组件
        allEmitters.Clear();
        allEmitters.AddRange(GetComponentsInChildren<DanmakuEmitter>());
    }

    public void EnableAllEmitters(bool fireable)
    {
        foreach (var emitter in allEmitters)
        {
            //emitter.SetFireable(fireable);
        }
    }

    public void EnableEmitter()
    {

    }
}
