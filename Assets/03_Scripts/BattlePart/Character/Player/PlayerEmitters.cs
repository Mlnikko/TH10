using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEmitters : MonoBehaviour
{
    public List<DanmakuEmitter> emitters;

    public void EnableAllEmitters(bool fireable)
    {
        foreach (var emitter in emitters)
        {
            emitter.SetEmitterFireable(fireable);
        }
        GameLogger.Debug("½ÇÉ«µ¯Ä»·¢ÉäÆ÷ÆôÓÃ×´Ì¬" + fireable);
    }
}
