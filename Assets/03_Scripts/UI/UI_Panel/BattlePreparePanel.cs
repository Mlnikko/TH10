using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePreparePanel : UIPanel
{
    [Header("References")]
    [SerializeField] Transform characterListContent;
    [SerializeField] Transform weaponListContent;
    [SerializeField] GameObject characterItemPrefab;
    [SerializeField] GameObject weaponItemPrefab;
    [SerializeField] Button confirmBtn;
    [SerializeField] TMP_Text countdownText;

    float countdownDuration = 10f;
    const string COUNTDOWN_KEY = "BattlePrepare_Countdown";

    // 数据缓存
    CharacterConfig[] characterConfigs;
    Dictionary<string, List<WeaponConfig>> characterWeaponsMap = new();

    string selectedCharacterId;
    string selectedWeaponId;

    // 当前 UI Item 缓存（用于高亮管理）
    List<CharacterSelectionUI> characterItems = new();
    List<WeaponSelectionUI> weaponItems = new();

    public override void Initialize()
    {
        base.Initialize();
        ReadConfig();
    }

    void ReadConfig()
    {
        string[] ids = ConfigHelper.allCharCfgIds;

        characterConfigs = ConfigManager.GetConfig<CharacterConfig>(ids);

        characterWeaponsMap.Clear();

        foreach (var charCfg in characterConfigs)
        {
            if (charCfg == null || string.IsNullOrEmpty(charCfg.ConfigId))
                continue;

            var weaponIds = charCfg.AvailableWeapons;
            var weapons = new List<WeaponConfig>();

            foreach (var wid in weaponIds)
            {
                if (wid == E_Weapon.None) continue;

                var wcfg = ConfigManager.GetConfig<WeaponConfig>(wid.ToString());
                if (wcfg != null)
                    weapons.Add(wcfg);
                else
                    Logger.Warn($"Weapon '{wid}' not found for character '{charCfg.ConfigId}'");
            }

            characterWeaponsMap[charCfg.ConfigId] = weapons;
        }
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        confirmBtn.onClick.AddListener(OnConfirmed);
        RefreshCharacterList();

        // 启动倒计时
        StartCountdown();
    }

    public override void OnHide()
    {
        base.OnHide();
        confirmBtn.onClick.RemoveListener(OnConfirmed);
        ClearSelection(); // 可选：隐藏时清空选择
    }

    void StartCountdown()
    {
        CoroutineManager.Instance.StopCoroutineByKey(COUNTDOWN_KEY); // 安全兜底
        CoroutineManager.Instance.StartUniqueCoroutine(COUNTDOWN_KEY, CountdownRoutine());
    }

    void StopCountdown()
    {
        CoroutineManager.Instance.StopCoroutineByKey(COUNTDOWN_KEY);
    }

    IEnumerator CountdownRoutine()
    {
        float elapsed = 0f;
        while (elapsed < countdownDuration)
        {
            elapsed += Time.deltaTime;

            // 更新 UI（可选）
            if (countdownText != null)
            {
                int remaining = Mathf.CeilToInt(countdownDuration - elapsed);
                countdownText.text = $"自动开始: {remaining}s";
            }

            yield return null;
        }

        // 超时，自动确认
        OnConfirmed();
    }

    void RefreshCharacterList()
    {
        foreach (var item in characterItems)
            Destroy(item.gameObject);
        characterItems.Clear();

        // 创建新的角色项
        foreach (var config in characterConfigs)
        {
            if (config == null) continue;

            var go = Instantiate(characterItemPrefab, characterListContent);
            var item = go.GetComponent<CharacterSelectionUI>();
            if (item == null)
            {
                Logger.Error($"CharacterSelectionUI component missing on prefab: {characterItemPrefab.name}");
                Destroy(go);
                continue;
            }

            item.Initialize(config, () => OnCharacterSelected(config.ConfigId)).Forget();
            characterItems.Add(item);
        }

        // 如果尚未选择角色，默认选中第一个有效角色
        if (string.IsNullOrEmpty(selectedCharacterId) && characterConfigs.Length > 0)
        {
            var firstValid = characterConfigs.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.ConfigId));
            if (firstValid != null)
            {
                OnCharacterSelected(firstValid.ConfigId);
            }
        }
    }

    void RefreshWeaponList()
    {
        // 清除旧的武器项
        foreach (var item in weaponItems)
            Destroy(item.gameObject);
        weaponItems.Clear();

        if (string.IsNullOrEmpty(selectedCharacterId) || !characterWeaponsMap.TryGetValue(selectedCharacterId, out var weapons))
        {
            // 没有选中角色或该角色无武器，清空列表
            return;
        }

        // 创建武器项
        foreach (var wcfg in weapons)
        {
            var go = Instantiate(weaponItemPrefab, weaponListContent);
            var item = go.GetComponent<WeaponSelectionUI>();
            if (item == null)
            {
                Logger.Error($"WeaponSelectionUI component missing on prefab: {weaponItemPrefab.name}");
                Destroy(go);
                continue;
            }

            item.Initialize(wcfg, () => OnWeaponSelected(wcfg.ConfigId)).Forget();
            weaponItems.Add(item);
        }

        // 默认选中第一个武器（如果当前未选或已失效）
        if (string.IsNullOrEmpty(selectedWeaponId) || !weapons.Any(w => w.ConfigId == selectedWeaponId))
        {
            if (weapons.Count > 0)
            {
                OnWeaponSelected(weapons[0].ConfigId);
            }
            else
            {
                selectedWeaponId = null; // 安全兜底
            }
        }
    }

    void ClearSelection()
    {
        selectedCharacterId = null;
        selectedWeaponId = null;

        // 取消所有高亮（假设 CharacterSelectionUI/WeaponSelectionUI 有 SetSelected(bool) 方法）
        foreach (var item in characterItems)
            item.SetSelected(false);
        foreach (var item in weaponItems)
            item.SetSelected(false);
    }

    void OnCharacterSelected(string characterId)
    {
        // 避免无意义刷新
        if (selectedCharacterId == characterId) return;

        // 更新选中状态
        selectedCharacterId = characterId;

        // 高亮选中的角色
        foreach (var item in characterItems)
            item.SetSelected(item.ConfigId == characterId);

        // 刷新武器列表（因为不同角色可用武器不同）
        RefreshWeaponList();

        Logger.Info($"Selected Character: {selectedCharacterId}");
    }

    void OnWeaponSelected(string weaponId)
    {
        if (selectedWeaponId == weaponId) return;

        selectedWeaponId = weaponId;

        // 高亮选中的武器
        foreach (var item in weaponItems)
            item.SetSelected(item.ConfigId == weaponId);

        Logger.Info($"Selected Weapon: {selectedWeaponId}");
    }

    void OnConfirmed()
    {
        var playerBattleData = new PlayerBattleData
        (
            RoomManager.Instance.selfPlayerIndex,
            Enum.TryParse<E_Character>(selectedCharacterId, out var charId) ? charId : E_Character.None,
            Enum.TryParse<E_Weapon>(selectedWeaponId, out var weapId) ? weapId : E_Weapon.None
        );

        if (RoomManager.Instance.IsHost)
        {
            BattleManager.Instance.AddPlayer(playerBattleData);
        }
        else
        {
            NetworkManager.Instance.SendToHost(new PlayerBattleDataConfirmedMSG
            {
                playerBattleData = playerBattleData,
            });
        }

        UIManager.Instance.HidePanel<BattlePreparePanel>();
    }
}