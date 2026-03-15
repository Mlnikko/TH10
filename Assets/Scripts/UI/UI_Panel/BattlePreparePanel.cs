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
    [SerializeField] TMP_Text confirmCountdownText;

    [SerializeField] GameObject lastCountdown;
    [SerializeField] TMP_Text lastCountdownText;

    float confirmCountdownDuration = 10f;
    float lastCountdownDuration = 3f;
    const string ConfirmCountdownKey = "BattlePrepare_ConfirmCountdown";
    const string LastCountdownKey = "BattlePrepare_LastCountdown";

    // 数据缓存
    List<CharacterConfig> characterConfigs = new();
    Dictionary<E_Character, List<WeaponConfig>> characterWeaponsMap = new();

    E_Character selectedCharacterId;
    E_Weapon selectedWeaponId;

    // 当前 UI Item 缓存（用于高亮管理）
    List<CharacterSelectionUI> characterItems = new();
    List<WeaponSelectionUI> weaponItems = new();

    public override void Initialize()
    {
        ReadConfig();
    }

    void ReadConfig()
    {
        var allCharacterCfgIds = ResManager.Instance.Manifest.characterConfigIds;
        var allWeaponIds = ResManager.Instance.Manifest.weaponConfigIds;
        foreach (var cid in allCharacterCfgIds)
        {
            var charCfg = GameResDB.Instance.GetConfig<CharacterConfig>(cid);
            if (charCfg != null)
            {
                characterConfigs.Add(charCfg);
            }
        }

        characterWeaponsMap.Clear();

        foreach (var wid in allWeaponIds)
        {
            var weaponCfg = GameResDB.Instance.GetConfig<WeaponConfig>(wid);
            if (weaponCfg == null)
            {
                Logger.Warn("WeaponConfig not found for ID: " + wid);
                continue;
            }
            var charId = weaponCfg.characterID;

            if(charId == E_Character.None)
            {
                Logger.Warn($"WeaponConfig {weaponCfg.weaponID} has invalid characterID: {charId}");
                continue;
            }

            if (!characterWeaponsMap.ContainsKey(charId))
            {
                characterWeaponsMap[charId] = new List<WeaponConfig> { weaponCfg };
            }
            else
            {
                characterWeaponsMap[charId].Add(weaponCfg);
            }
        }
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        confirmBtn.onClick.AddListener(OnConfirmed);

        lastCountdown.SetActive(false);
        RefreshCharacterList();

        // 启动确认倒计时
        CoroutineManager.Instance.StartWithKey(ConfirmCountdownKey, ConfirmCountdownRoutine());
    }

    public override void OnHide()
    {
        base.OnHide();
        confirmBtn.onClick.RemoveListener(OnConfirmed);
        CoroutineManager.Instance.StopByKey(ConfirmCountdownKey);
        CoroutineManager.Instance.StopByKey(LastCountdownKey);
    }

    IEnumerator ConfirmCountdownRoutine()
    {
        float elapsed = 0f;
        while (elapsed < confirmCountdownDuration)
        {
            elapsed += Time.deltaTime;

            if (confirmCountdownText != null)
            {
                int remaining = Mathf.CeilToInt(confirmCountdownDuration - elapsed);
                confirmCountdownText.text = $"{remaining}s";
            }

            yield return null;
        }

        // 超时，自动确认
        OnConfirmed();
    }

    IEnumerator LastConfirmCountdownRoutine()
    {
        float elapsed = 0f;
        while (elapsed < lastCountdownDuration)
        {
            elapsed += Time.deltaTime;

            if (lastCountdownText != null)
            {
                int remaining = Mathf.CeilToInt(lastCountdownDuration - elapsed);
                lastCountdownText.text = $"战斗准备: {remaining}s!";
            }

            yield return null;
        }

        // 超时，自动确认
        OnLastCountdownEnd();
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

            if (!go.TryGetComponent<CharacterSelectionUI>(out var item))
            {
                Logger.Error($"CharacterSelectionUI component missing on prefab: {characterItemPrefab.name}");
                Destroy(go);
                continue;
            }

            item.Initialize(config, () => OnCharacterSelected(config.character));
            characterItems.Add(item);
        }

        // 如果尚未选择角色，默认选中第一个有效角色
        if (selectedCharacterId == E_Character.None && characterConfigs.Count > 0)
        {
            var firstValid = characterConfigs.FirstOrDefault(c => c != null && c.character != E_Character.None);
            if (firstValid != null)
            {
                OnCharacterSelected(firstValid.character);
            }
        }
    }

    void RefreshWeaponList()
    {
        // 清除旧的武器项
        foreach (var item in weaponItems)
            Destroy(item.gameObject);
        weaponItems.Clear();

        if (selectedCharacterId == E_Character.None || !characterWeaponsMap.TryGetValue(selectedCharacterId, out var weapons))
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

            item.Initialize(wcfg, () => OnWeaponSelected(wcfg.weaponID));
            weaponItems.Add(item);
        }

        // 默认选中第一个武器（如果当前未选或已失效）
        if (selectedWeaponId == E_Weapon.None || !weapons.Any(w => w.weaponID == selectedWeaponId))
        {
            if (weapons.Count > 0)
            {
                OnWeaponSelected(weapons[0].weaponID);
            }
            else
            {
                selectedWeaponId = E_Weapon.None; // 安全兜底
            }
        }
    }

    void ClearSelection()
    {
        selectedCharacterId = E_Character.None;
        selectedWeaponId = E_Weapon.None;

        // 取消所有高亮（假设 CharacterSelectionUI/WeaponSelectionUI 有 SetSelected(bool) 方法）
        foreach (var item in characterItems)
            item.SetSelected(false);
        foreach (var item in weaponItems)
            item.SetSelected(false);
    }

    void OnCharacterSelected(E_Character characterId)
    {
        // 避免无意义刷新
        if (selectedCharacterId == characterId) return;

        // 更新选中状态
        selectedCharacterId = characterId;

        // 高亮选中的角色
        foreach (var item in characterItems)
            item.SetSelected(item.characterName == characterId);

        // 刷新武器列表（因为不同角色可用武器不同）
        RefreshWeaponList();

        Logger.Info($"Selected Character: {selectedCharacterId}");
    }

    void OnWeaponSelected(E_Weapon weaponId)
    {
        if (selectedWeaponId == weaponId) return;

        selectedWeaponId = weaponId;

        // 高亮选中的武器
        foreach (var item in weaponItems)
            item.SetSelected(item.weaponId == weaponId);

        Logger.Info($"Selected Weapon: {selectedWeaponId}");
    }

    void OnConfirmed()
    {
        CoroutineManager.Instance.StopByKey(ConfirmCountdownKey);

        lastCountdown.SetActive(true);
        CoroutineManager.Instance.StartWithKey(LastCountdownKey, LastConfirmCountdownRoutine());
        

        var playerBattleData = new PlayerBattleData
        (
            RoomManager.LocalPlayerIndex,
            selectedCharacterId,
            selectedWeaponId
        );

        switch(NetworkManager.Instance.NetworkRole)
        {
            case NetworkRole.Host:
                BattleManager.Instance.AddPlayerData(playerBattleData);
                break;
            case NetworkRole.Client:
                NetworkManager.Instance.SendToHost(new BattleReadyMSG
                {
                    playerBattleData = playerBattleData,
                });
                break;
            case NetworkRole.None:
                BattleManager.Instance.AddPlayerData(playerBattleData);
                break;
        }
    }

    void OnLastCountdownEnd()
    {
        CoroutineManager.Instance.StopCoroutine(LastCountdownKey);

        UIManager.Instance.ClosePanel<BattlePreparePanel>();

        switch (NetworkManager.Instance.NetworkRole)
        {
            case NetworkRole.Host:       
                BattleManager.Instance.StartMutiPlayerBattleForHost();
                break;
            case NetworkRole.Client:
                
                break;
            case NetworkRole.None:
                BattleManager.Instance.StartSinglePlayerBattle();
                break;
        }
    }
}