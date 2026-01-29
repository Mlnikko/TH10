using UnityEngine;

public enum EmitterCamp
{
    None,
    Player,
    Enemy
}

public class DanmakuEmitterPrefabTool : MonoBehaviour
{
    [Header("配置文件")]
    public DanmakuEmitterConfig emitterConfig;
    public DanmakuConfig danmakuConfig;

    [Header("发射器类型")]
    [SerializeField] EmitMode emitterType;

    [Header("发射器阵营")]
    [SerializeField] EmitterCamp emitterCamp;

    [Header("对象池配置")]
    [SerializeField] int poolMinSize;
    [SerializeField] int poolMaxSize;

    [Header("弹幕发射调整")]
    [SerializeField] Vector2 launchPosOffset;
    [SerializeField] Vector3 launchRotOffset;

    [Header("发射音效")]
    [SerializeField] AudioName launchAudio;

    [Header("发射间隔")]
    [SerializeField] float launchInterval;

    [Header("发射速度")]
    [SerializeField] float launchSpeed;

    public bool LoadEmitterConfig()
    {
        if (emitterConfig == null)
        {
            Logger.Warn("发射器配置为空，无法加载", LogTag.Config);
            return false;
        }

        emitterType = emitterConfig.emitMode;
        emitterCamp = emitterConfig.emitterCamp;
        launchPosOffset = emitterConfig.launchPosOffset;
        launchRotOffset = emitterConfig.launchRotOffset;
        launchInterval = emitterConfig.launchInterval;
        launchSpeed = emitterConfig.launchSpeed;
        launchAudio = emitterConfig.audio_Fire;
        

        Logger.Debug("已加载发射器配置" + emitterConfig.name, LogTag.Config);

        return true;
    }

    public void SaveEmitterConfig()
    {
        if (emitterConfig == null)
        {
            Logger.Warn("发射器配置为空，无法保存", LogTag.Config);
            return;
        }

        emitterConfig.emitMode = emitterType;
        emitterConfig.emitterCamp = emitterCamp;
        emitterConfig.launchPosOffset = launchPosOffset;
        emitterConfig.launchRotOffset = launchRotOffset;
        emitterConfig.launchInterval = launchInterval;
        emitterConfig.launchSpeed = launchSpeed;
        emitterConfig.audio_Fire = launchAudio;
      
        Logger.Debug("成功保存发射器配置" + emitterConfig.name, LogTag.Config);
    }

    public void PreviewEmitterEffect()
    {
        if (LoadEmitterConfig() == false) return;

        //// 临时生成若干轮弹幕用于预览（比如3轮）
        //for (int round = 0; round < 3; round++)
        //{
        //    float delay = round * launchInterval;
        //    UnityEditor.EditorApplication.delayCall += () =>
        //    {
        //        EmitPreviewDanmaku();
        //    };
        //}
    }

    void EmitPreviewDanmaku()
    {
        Vector3 emitterPos = transform.position + (Vector3)launchPosOffset;
        Quaternion emitterRot = Quaternion.Euler(launchRotOffset);

        //switch (emitterConfig.emitMode)
        //{
        //    case EmitMode.Line:
        //        EmitLinePreview(emitterPos, emitterRot);
        //        break;
        //    case EmitMode.Arc:
        //        EmitArcPreview(emifierPos, emitterRot);
        //        break;
        //    default:
        //        EmitSinglePreview(emitterPos, emitterRot);
        //        break;
        //}

        //// 播放音效（仅一次）
        //if (!string.IsNullOrEmpty(launchAudio.ToString()) && launchAudio != AudioName.None)
        //{
        //    AudioManager.Instance?.PlayOneShot(launchAudio);
        //}
    }

    void OnDrawGizmosSelected()
    {
        
    }
}
