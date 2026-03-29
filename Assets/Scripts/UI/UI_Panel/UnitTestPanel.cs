using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitTestPanel : UIPanel
{
    [Header("Add Enemy Test")]
    [SerializeField] Button addEnemy_button;
    [SerializeField] TMP_InputField enemyId_inputField;

    void Start()
    {
        addEnemy_button.onClick.AddListener(OnAddEnemy);
    }

    void OnAddEnemy()
    {
        if(BattleManager.Instance.CurrentStatus != E_BattleStatus.InBattle)
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
}
