using UnityEngine;

public class UIPanel : MonoBehaviour
{
    public string PanelName => GetType().Name;

    // 初始化（首次激活时调用）
    public virtual void Initialize() { }

    // 显示时回调（每次 Show 时调用）
    public virtual void OnShow(object data = null) { }

    // 隐藏时回调
    public virtual void OnHide() { }
}
