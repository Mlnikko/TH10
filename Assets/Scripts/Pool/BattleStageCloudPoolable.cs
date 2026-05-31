using UnityEngine;

/// <summary>
/// 战斗背景云雾池化根节点（<c>cloud</c>，<see cref="Assets/Prefabs/Stage/Cloud.prefab"/>）。
/// 由 <see cref="BattleStageBackgroundRuntime"/> 从池取出后动态替换 Sprite。
/// </summary>
[DisallowMultipleComponent]
public class BattleStageCloudPoolable : MonoBehaviour, IPoolable
{
    public const string DefaultPrefabId = "cloud";

    SpriteRenderer _spriteRenderer;

    void Awake() => ResolveRenderer();

    public void OnGet()
    {
        ResolveRenderer();
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;
    }

    public void OnReturn()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = null;
            var color = _spriteRenderer.color;
            color.a = 1f;
            _spriteRenderer.color = color;
            _spriteRenderer.enabled = false;
        }

        transform.localScale = Vector3.one;
    }

    void ResolveRenderer()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
