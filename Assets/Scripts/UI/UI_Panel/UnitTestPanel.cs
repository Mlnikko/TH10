using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitTestPanel : UIPanel
{
    [Header("Add Enemy Test")]
    [SerializeField] Button addEnemy_button;
    [SerializeField] TMP_InputField enemyId_inputField;

    [Header("Player Power Test")]
    [SerializeField] Button setPower_button;
    [SerializeField] TMP_InputField power_inputField;
    [SerializeField] TMP_InputField playerIndex_inputField;

    [Header("Player Invincible Test")]
    [SerializeField] Toggle playerInvincible_toggle;

    bool _suppressInvincibleToggleCallback;

    void Start()
    {
        addEnemy_button.onClick.AddListener(OnAddEnemy);
        EnsurePowerTestControls();
        EnsureInvincibleControls();
    }

    public override void OnShow(object data = null)
    {
        SyncInvincibleToggleFromBattle();
    }

    void EnsurePowerTestControls()
    {
        if (setPower_button == null || power_inputField == null)
            BuildPowerTestControlsFromTemplate();

        if (setPower_button == null || power_inputField == null)
        {
            Logger.Warn("[UnitTestPanel] 未配置 Power 测试控件。", LogTag.UnitTest);
            return;
        }

        setPower_button.onClick.RemoveListener(OnSetPlayerPower);
        setPower_button.onClick.AddListener(OnSetPlayerPower);

        if (string.IsNullOrEmpty(power_inputField.text))
            power_inputField.text = "0";

        if (playerIndex_inputField != null && string.IsNullOrEmpty(playerIndex_inputField.text))
            playerIndex_inputField.text = RoomManager.LocalPlayerIndex.ToString();
    }

    void EnsureInvincibleControls()
    {
        if (playerInvincible_toggle == null)
            BuildInvincibleToggleFromTemplate();

        if (playerInvincible_toggle == null)
        {
            Logger.Warn("[UnitTestPanel] 未配置玩家无敌 Toggle。", LogTag.UnitTest);
            return;
        }

        playerInvincible_toggle.onValueChanged.RemoveListener(OnPlayerInvincibleToggleChanged);
        playerInvincible_toggle.onValueChanged.AddListener(OnPlayerInvincibleToggleChanged);
        SyncInvincibleToggleFromBattle();
    }

    void BuildPowerTestControlsFromTemplate()
    {
        if (addEnemy_button == null)
            return;

        var templateRow = addEnemy_button.transform.parent;
        if (templateRow == null)
            return;

        Transform layoutParent = templateRow.parent;

        var playerIndexRow = Instantiate(templateRow.gameObject, layoutParent);
        playerIndexRow.name = "PlayerIndexRow";
        playerIndex_inputField = ConfigureInputOnlyRow(
            playerIndexRow,
            "玩家索引",
            RoomManager.LocalPlayerIndex.ToString());

        var powerRow = Instantiate(templateRow.gameObject, layoutParent);
        powerRow.name = "PlayerPowerRow";
        power_inputField = ConfigureInputOnlyRow(powerRow, "Power (火力)", "0");
        setPower_button = ConfigureButtonLabel(powerRow, "设置 Power");
    }

    void BuildInvincibleToggleFromTemplate()
    {
        Transform templateRow = setPower_button != null
            ? setPower_button.transform.parent
            : addEnemy_button != null ? addEnemy_button.transform.parent : null;
        if (templateRow == null)
            return;

        var row = Instantiate(templateRow.gameObject, templateRow.parent);
        row.name = "PlayerInvincibleRow";

        foreach (var input in row.GetComponentsInChildren<TMP_InputField>(true))
            input.gameObject.SetActive(false);

        var button = row.GetComponentInChildren<Button>(true);
        if (button == null)
            return;

        var buttonText = button.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
            buttonText.text = "玩家无敌";

        var graphic = button.GetComponent<Image>();
        var buttonGo = button.gameObject;
        Object.Destroy(button);

        playerInvincible_toggle = buttonGo.GetComponent<Toggle>();
        if (playerInvincible_toggle == null)
            playerInvincible_toggle = buttonGo.AddComponent<Toggle>();

        playerInvincible_toggle.targetGraphic = graphic;
        playerInvincible_toggle.isOn = false;
    }

    static TMP_InputField ConfigureInputOnlyRow(GameObject row, string placeholder, string defaultText)
    {
        var input = row.GetComponentInChildren<TMP_InputField>(true);
        if (input == null)
            return null;

        input.text = defaultText;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;

        if (input.placeholder is TMP_Text placeholderText)
            placeholderText.text = placeholder;

        var button = row.GetComponentInChildren<Button>(true);
        if (button != null)
            button.gameObject.SetActive(false);

        return input;
    }

    static Button ConfigureButtonLabel(GameObject row, string label)
    {
        var button = row.GetComponentInChildren<Button>(true);
        if (button == null)
            return null;

        button.gameObject.SetActive(true);
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;

        return button;
    }

    void SyncInvincibleToggleFromBattle()
    {
        if (playerInvincible_toggle == null)
            return;

        if (BattleManager.Instance == null
            || BattleManager.Instance.CurrentStatus != E_BattleStatus.InBattle)
        {
            SetInvincibleToggleSilently(false);
            return;
        }

        if (!TryParsePlayerIndex(out byte playerIndex))
            return;

        if (BattleManager.Instance.TryGetPlayerInvincible(playerIndex, out bool invincible))
            SetInvincibleToggleSilently(invincible);
    }

    void SetInvincibleToggleSilently(bool isOn)
    {
        if (playerInvincible_toggle == null)
            return;

        _suppressInvincibleToggleCallback = true;
        playerInvincible_toggle.isOn = isOn;
        _suppressInvincibleToggleCallback = false;
    }

    void OnPlayerInvincibleToggleChanged(bool isOn)
    {
        if (_suppressInvincibleToggleCallback)
            return;

        if (BattleManager.Instance == null
            || BattleManager.Instance.CurrentStatus != E_BattleStatus.InBattle)
        {
            Logger.Warn("[UnitTestPanel] 仅在战斗中可切换无敌。", LogTag.UnitTest);
            SetInvincibleToggleSilently(false);
            return;
        }

        if (!TryParsePlayerIndex(out byte playerIndex))
        {
            SetInvincibleToggleSilently(false);
            return;
        }

        if (!BattleManager.Instance.TrySetPlayerInvincible(playerIndex, isOn))
        {
            Logger.Warn(
                $"[UnitTestPanel] 设置无敌失败：未找到玩家 {playerIndex} 或当前不在战斗中。",
                LogTag.UnitTest);
            SyncInvincibleToggleFromBattle();
        }
    }

    bool TryParsePlayerIndex(out byte playerIndex)
    {
        playerIndex = RoomManager.LocalPlayerIndex;
        if (playerIndex_inputField == null)
            return true;

        string playerText = playerIndex_inputField.text.Trim();
        if (string.IsNullOrEmpty(playerText))
            return true;

        if (byte.TryParse(playerText, out playerIndex))
            return true;

        Logger.Warn("[UnitTestPanel] 玩家索引无效。", LogTag.UnitTest);
        return false;
    }

    void OnAddEnemy()
    {
        if (BattleManager.Instance.CurrentStatus != E_BattleStatus.InBattle)
        {
            Logger.Warn("Cannot add enemy when not in battle.");
            return;
        }

        string enemyId = enemyId_inputField.text.Trim();
        if (!string.IsNullOrEmpty(enemyId))
        {
            var enemyConfig = GameResDB.Instance.GetConfig<EnemyConfig>(enemyId);
            if (enemyConfig != null)
            {
                BattleManager.Instance.AddEnemyTest(enemyConfig, 0, 0);
            }
            else
            {
                Logger.Error($"EnemyConfig not found for ID: {enemyId}");
            }
        }
    }

    void OnSetPlayerPower()
    {
        if (BattleManager.Instance.CurrentStatus != E_BattleStatus.InBattle)
        {
            Logger.Warn("[UnitTestPanel] 仅在战斗中可设置 Power。", LogTag.UnitTest);
            return;
        }

        if (!int.TryParse(power_inputField.text.Trim(), out int powerOrbs) || powerOrbs < 0)
        {
            Logger.Warn("[UnitTestPanel] Power 须为非负整数。", LogTag.UnitTest);
            return;
        }

        if (!TryParsePlayerIndex(out byte playerIndex))
            return;

        if (!BattleManager.Instance.TrySetPlayerPowerOrbs(playerIndex, powerOrbs))
        {
            Logger.Warn(
                $"[UnitTestPanel] 设置失败：未找到玩家 {playerIndex} 或当前不在战斗中。",
                LogTag.UnitTest);
        }
    }
}
