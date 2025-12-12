using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteHelper
{
    const string SpriteRoot = "Sprites/";
    const string WeaponKey = SpriteRoot + "Weapon";
    const string CharacterSpriteRoot = SpriteRoot + "Character/";

    public static string GetSpriteKey(string spriteName)
    {
        return SpriteRoot + spriteName;
    }

    public static string GetWeaponSpriteKey(string weaponId)
    {
        return WeaponKey + $"[{weaponId}]";
    }

    public static string GetCharacterSpriteKey(string characterId)
    {
        return CharacterSpriteRoot + characterId;
    }
}

public static class SpriteManager
{
    /// <summary>
    /// 从 Addressables 加载独立 Sprite（如单张 PNG）
    /// </summary>
    public static Task<Sprite> LoadSpriteAsync(string spriteKey)
    {
        return ResManager.LoadAsync<Sprite>(spriteKey);
    }

    /// <summary>
    /// 从 Sprite Atlas 中获取指定名称的 Sprite
    /// </summary>
    /// <param name="atlasKey">Atlas 的 Addressable Key</param>
    /// <param name="spriteName">图集中定义的 Sprite 名称</param>
    public static async Task<Sprite> LoadSpriteFromAtlasAsync(string atlasKey, string spriteName)
    {
        if (string.IsNullOrEmpty(atlasKey) || string.IsNullOrEmpty(spriteName))
        {
            Logger.Error("Atlas key or sprite name is null/empty!", LogTag.Resource);
            return null;
        }

        // 委托给 ResManager 加载 Atlas（自动缓存）
        var atlas = await ResManager.LoadAsync<SpriteAtlas>(atlasKey);
        if (atlas == null)
        {
            Logger.Error($"Failed to load atlas: {atlasKey}", LogTag.Resource);
            return null;
        }

        // 从 Atlas 提取 Sprite（无需缓存，atlas 已被 ResManager 缓存）
        var sprite = atlas.GetSprite(spriteName);
        if (sprite == null)
        {
            Logger.Error($"[SpriteManager] Sprite '{spriteName}' not found in atlas '{atlasKey}'", LogTag.Resource);
        }

        return sprite;
    }

    /// <summary>
    /// 同步获取已加载的 Sprite（仅当确定已预加载时使用）
    /// </summary>
    public static Sprite GetSprite(string key)
    {
        return ResManager.Get<Sprite>(key);
    }

    /// <summary>
    /// 同步获取已加载的 Atlas
    /// </summary>
    public static SpriteAtlas GetAtlas(string key)
    {
        return ResManager.Get<SpriteAtlas>(key);
    }
}