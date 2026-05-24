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

    void Start()
    {
        addEnemy_button.onClick.AddListener(OnAddEnemy);
        EnsurePowerTestControls();
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

        byte playerIndex = RoomManager.LocalPlayerIndex;
        if (playerIndex_inputField != null)
        {
            string playerText = playerIndex_inputField.text.Trim();
            if (!string.IsNullOrEmpty(playerText))
            {
                if (!byte.TryParse(playerText, out playerIndex))
                {
                    Logger.Warn("[UnitTestPanel] 玩家索引无效。", LogTag.UnitTest);
                    return;
                }
            }
        }

        if (!BattleManager.Instance.TrySetPlayerPowerOrbs(playerIndex, powerOrbs))
        {
            Logger.Warn(
                $"[UnitTestPanel] 设置失败：未找到玩家 {playerIndex} 或当前不在战斗中。",
                LogTag.UnitTest);
        }
    }
}
