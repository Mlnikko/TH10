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
    [SerializeField] TMP_Text statusText;

    [Header("Phase Timing (seconds)")]
    [SerializeField] float selectionPhaseDuration = 15f;
    [SerializeField] float readyPhaseDuration = 5f;

    const string PrepareTimelineKey = "BattlePrepare_Timeline";

    readonly List<CharacterConfig> characterConfigs = new();
    readonly Dictionary<E_Character, List<WeaponConfig>> characterWeaponsMap = new();

    E_Character selectedCharacterId;
    E_Weapon selectedWeaponId;

    readonly List<CharacterSelectionUI> characterItems = new();
    readonly List<WeaponSelectionUI> weaponItems = new();

    bool _phaseSelectionLocked;
    bool _localReadyLocked;

    public override void Initialize()
    {
        ReadConfig();
    }

    void ReadConfig()
    {
        characterConfigs.Clear();
        characterWeaponsMap.Clear();

        var allCharacterCfgIds = ResManager.Instance.Manifest.characterConfigIds;
        var allWeaponIds = ResManager.Instance.Manifest.weaponConfigIds;
        foreach (var cid in allCharacterCfgIds)
        {
            var charCfg = GameResDB.Instance.GetConfig<CharacterConfig>(cid);
            if (charCfg != null)
                characterConfigs.Add(charCfg);
        }

        foreach (var wid in allWeaponIds)
        {
            var weaponCfg = GameResDB.Instance.GetConfig<WeaponConfig>(wid);
            if (weaponCfg == null)
            {
                Logger.Warn("WeaponConfig not found for ID: " + wid);
                continue;
            }
            var charId = weaponCfg.characterID;

            if (charId == E_Character.None)
            {
                Logger.Warn($"WeaponConfig {weaponCfg.weaponID} has invalid characterID: {charId}");
                continue;
            }

            if (!characterWeaponsMap.ContainsKey(charId))
                characterWeaponsMap[charId] = new List<WeaponConfig> { weaponCfg };
            else
                characterWeaponsMap[charId].Add(weaponCfg);
        }
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        confirmBtn.onClick.AddListener(OnConfirmClicked);
        var battle = BattleManager.Instance;
        battle.OnPrepareCharacterLocked += HandlePrepareCharacterLocked;
        battle.OnPrepareCharacterReleased += HandlePrepareCharacterReleased;

        selectedCharacterId = E_Character.None;
        selectedWeaponId = E_Weapon.None;
        _phaseSelectionLocked = false;
        _localReadyLocked = false;

        ClearWeaponListUi();
        UpdateConfirmButtonLabel();
        RefreshCharacterList();
        SetSelectionInteractable(true);
        SetStatusText($"请选择你的角色与符卡（{Mathf.CeilToInt(selectionPhaseDuration)}s）");

        CoroutineManager.Instance.StartWithKey(PrepareTimelineKey, PrepareTimelineRoutine());
    }

    public override void OnHide()
    {
        base.OnHide();
        confirmBtn.onClick.RemoveListener(OnConfirmClicked);
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnPrepareCharacterLocked -= HandlePrepareCharacterLocked;
            BattleManager.Instance.OnPrepareCharacterReleased -= HandlePrepareCharacterReleased;
        }
        CoroutineManager.Instance.StopByKey(PrepareTimelineKey);
    }

    void HandlePrepareCharacterLocked(byte playerIndex, E_Character characterId)
    {
        if (playerIndex == RoomManager.LocalPlayerIndex
            && NetworkManager.Instance.NetworkRole == NetworkRole.Client)
        {
            _localReadyLocked = true;
            ApplyLocalReadyUiState();
        }

        RefreshCharacterAvailability();
    }

    void HandlePrepareCharacterReleased(byte playerIndex)
    {
        if (playerIndex == RoomManager.LocalPlayerIndex)
        {
            _localReadyLocked = false;
            if (!_phaseSelectionLocked)
                ApplyLocalSelectionEditableState();
        }

        RefreshCharacterAvailability();
    }

    IEnumerator PrepareTimelineRoutine()
    {
        float elapsed = 0f;
        while (elapsed < selectionPhaseDuration)
        {
            elapsed += Time.deltaTime;
            int remaining = Mathf.CeilToInt(selectionPhaseDuration - elapsed);
            UpdateSelectionPhaseUi(remaining);
            yield return null;
        }

        EnterReadyPhase();

        elapsed = 0f;
        while (elapsed < readyPhaseDuration)
        {
            elapsed += Time.deltaTime;
            int remaining = Mathf.CeilToInt(readyPhaseDuration - elapsed);
            UpdateReadyPhaseUi(remaining);
            yield return null;
        }

        OnPrepareTimelineComplete();
    }

    void UpdateSelectionPhaseUi(int remainingSeconds)
    {
        SetStatusText($"请选择你的角色与符卡（{remainingSeconds}s）");
    }

    void UpdateReadyPhaseUi(int remainingSeconds)
    {
        SetStatusText($"即将进入战斗（{remainingSeconds}s）");
    }

    void SetStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    void EnterReadyPhase()
    {
        if (!_localReadyLocked)
            TryAssignRandomCharacterAndSubmit();

        _phaseSelectionLocked = true;
        SetSelectionInteractable(false);

        if (confirmBtn != null)
            confirmBtn.interactable = false;

        if (_localReadyLocked)
            SetStatusText("已确认，等待开战…");
        else
            SetStatusText("选择已锁定，等待开战…");

        Logger.Info("[BattlePrepare] Selection phase ended; entering ready phase.", LogTag.Battle);
    }

    void OnConfirmClicked()
    {
        if (_phaseSelectionLocked)
            return;

        if (_localReadyLocked)
        {
            CancelLocalPrepareReady();
            return;
        }

        if (selectedCharacterId == E_Character.None || selectedWeaponId == E_Weapon.None)
        {
            Logger.Warn("[BattlePrepare] 请先选择角色和武器。", LogTag.Battle);
            return;
        }

        if (!TrySubmitPrepareReady())
            return;

        if (NetworkManager.Instance.NetworkRole != NetworkRole.Client)
        {
            _localReadyLocked = true;
            ApplyLocalReadyUiState();
        }

        Logger.Info("[BattlePrepare] Prepare ready submitted.", LogTag.Battle);
    }

    void CancelLocalPrepareReady()
    {
        byte localIndex = RoomManager.LocalPlayerIndex;

        switch (NetworkManager.Instance.NetworkRole)
        {
            case NetworkRole.None:
                BattleManager.Instance.RemovePreparePlayerData(localIndex);
                break;
            case NetworkRole.Host:
                BattleManager.Instance.HostSubmitPrepareCancel(localIndex);
                break;
            case NetworkRole.Client:
                NetworkManager.Instance.SendToHost(new BattlePrepareCancelMSG { playerIndex = localIndex });
                Logger.Info("[BattlePrepare] Prepare cancel sent to host.", LogTag.Battle);
                return;
        }

        Logger.Info("[BattlePrepare] Prepare ready cancelled.", LogTag.Battle);
    }

    void ApplyLocalReadyUiState()
    {
        SetLocalSelectionLocked(true);
        UpdateConfirmButtonLabel();
        RefreshCharacterAvailability();
    }

    void ApplyLocalSelectionEditableState()
    {
        SetLocalSelectionLocked(false);
        UpdateConfirmButtonLabel();
        SetSelectionInteractable(true);
    }

    bool TrySubmitPrepareReady()
    {
        if (selectedCharacterId == E_Character.None || selectedWeaponId == E_Weapon.None)
            return false;

        var playerBattleData = new PlayerBattleData(
            RoomManager.LocalPlayerIndex,
            selectedCharacterId,
            selectedWeaponId);

        switch (NetworkManager.Instance.NetworkRole)
        {
            case NetworkRole.None:
                BattleManager.Instance.SetOrUpdatePlayerData(playerBattleData);
                return true;

            case NetworkRole.Host:
                return BattleManager.Instance.HostSubmitPrepareReady(playerBattleData);

            case NetworkRole.Client:
                NetworkManager.Instance.SendToHost(new BattleReadyMSG
                {
                    playerBattleData = playerBattleData,
                });
                return true;
        }

        return false;
    }

    void TryAssignRandomCharacterAndSubmit()
    {
        byte localIndex = RoomManager.LocalPlayerIndex;
        var battle = BattleManager.Instance;

        var available = characterConfigs
            .Where(c => c != null
                && c.character != E_Character.None
                && battle.IsPrepareCharacterAvailable(c.character, localIndex))
            .ToList();

        if (available.Count == 0)
        {
            Logger.Warn("[BattlePrepare] No available character for random assign.", LogTag.Battle);
            return;
        }

        var shuffled = available.OrderBy(_ => UnityEngine.Random.value).ToList();
        foreach (var cfg in shuffled)
        {
            ApplyCharacterSelectionLocal(cfg.character);
            if (selectedWeaponId == E_Weapon.None)
                continue;
            if (TrySubmitPrepareReady())
            {
                _localReadyLocked = true;
                Logger.Info($"[BattlePrepare] Random assigned character {cfg.character}.", LogTag.Battle);
                return;
            }
            selectedCharacterId = E_Character.None;
            selectedWeaponId = E_Weapon.None;
        }

        Logger.Warn("[BattlePrepare] Random assign failed after retries.", LogTag.Battle);
    }

    void UpdateConfirmButtonLabel()
    {
        if (confirmBtn == null) return;

        var label = confirmBtn.GetComponentInChildren<TMP_Text>();
        if (label == null) return;

        if (_phaseSelectionLocked)
            label.text = "已锁定";
        else if (_localReadyLocked)
            label.text = "取消准备";
        else
            label.text = "确认准备";
    }

    void SetSelectionInteractable(bool interactable)
    {
        bool canEdit = interactable && !_phaseSelectionLocked && !_localReadyLocked;

        if (canEdit)
            RefreshCharacterAvailability();
        else
        {
            foreach (var item in characterItems)
            {
                item.SetInteractable(false);
                item.SetTakenByOther(false);
            }
        }

        foreach (var item in weaponItems)
            item.SetInteractable(canEdit && selectedCharacterId != E_Character.None);

        if (confirmBtn != null)
            confirmBtn.interactable = !_phaseSelectionLocked;
    }

    void SetLocalSelectionLocked(bool locked)
    {
        foreach (var item in characterItems)
            item.SetInteractable(!locked && !_phaseSelectionLocked);
        foreach (var item in weaponItems)
            item.SetInteractable(!locked && !_phaseSelectionLocked && selectedCharacterId != E_Character.None);
    }

    void RefreshCharacterAvailability()
    {
        byte localIndex = RoomManager.LocalPlayerIndex;
        var battle = BattleManager.Instance;

        foreach (var item in characterItems)
        {
            bool isOwnSelection = item.characterName == selectedCharacterId;
            bool lockedByOtherReady = battle.IsMultiplayerPrepare
                && !isOwnSelection
                && !battle.IsPrepareCharacterAvailable(item.characterName, localIndex);
            bool canPick = !_phaseSelectionLocked
                && !_localReadyLocked
                && !lockedByOtherReady;

            item.SetInteractable(canPick);
            item.SetTakenByOther(lockedByOtherReady);
            item.SetSelected(isOwnSelection && selectedCharacterId != E_Character.None);

            byte? occupyingPlayer = null;
            if (battle.TryGetPrepareCharacterLocker(item.characterName, out byte lockerIndex))
                occupyingPlayer = lockerIndex;
            item.SetOccupyingPlayerId(occupyingPlayer);
        }
    }

    void OnPrepareTimelineComplete()
    {
        CoroutineManager.Instance.StopByKey(PrepareTimelineKey);

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

    void RefreshCharacterList()
    {
        foreach (var item in characterItems)
            Destroy(item.gameObject);

        characterItems.Clear();

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

        RefreshCharacterAvailability();
    }

    void ClearWeaponListUi()
    {
        foreach (var item in weaponItems)
            Destroy(item.gameObject);
        weaponItems.Clear();
    }

    void RefreshWeaponList()
    {
        ClearWeaponListUi();

        if (selectedCharacterId == E_Character.None
            || !characterWeaponsMap.TryGetValue(selectedCharacterId, out var weapons))
            return;

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
            item.SetInteractable(!_phaseSelectionLocked && !_localReadyLocked);
            weaponItems.Add(item);
        }

        if (weapons.Count > 0)
        {
            selectedWeaponId = weapons[0].weaponID;
            foreach (var item in weaponItems)
                item.SetSelected(item.weaponId == selectedWeaponId);
        }
        else
        {
            selectedWeaponId = E_Weapon.None;
        }
    }

    void OnCharacterSelected(E_Character characterId)
    {
        if (_phaseSelectionLocked || _localReadyLocked || selectedCharacterId == characterId)
            return;

        if (!BattleManager.Instance.IsPrepareCharacterAvailable(characterId, RoomManager.LocalPlayerIndex))
        {
            Logger.Warn($"[BattlePrepare] Character {characterId} is locked by another ready player.", LogTag.Battle);
            return;
        }

        ApplyCharacterSelectionLocal(characterId);
        Logger.Info($"Selected Character: {selectedCharacterId}");
    }

    void ApplyCharacterSelectionLocal(E_Character characterId)
    {
        selectedCharacterId = characterId;
        selectedWeaponId = E_Weapon.None;

        foreach (var item in characterItems)
            item.SetSelected(item.characterName == characterId);

        RefreshWeaponList();
        RefreshCharacterAvailability();
    }

    void OnWeaponSelected(E_Weapon weaponId)
    {
        if (_phaseSelectionLocked || _localReadyLocked || selectedWeaponId == weaponId)
            return;

        if (selectedCharacterId == E_Character.None)
            return;

        selectedWeaponId = weaponId;

        foreach (var item in weaponItems)
            item.SetSelected(item.weaponId == weaponId);

        Logger.Info($"Selected Weapon: {selectedWeaponId}");
    }
}
