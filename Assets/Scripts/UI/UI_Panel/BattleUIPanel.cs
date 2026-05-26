using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD：将逻辑战斗区 <see cref="GlobalBattleData.AreaData"/> 渲染到 <see cref="RawImage"/>，
/// 并轮询 <see cref="BattleManager.TryGetBattleHudSnapshot"/> / <see cref="BattleManager.TryGetBossHudSnapshot"/> 更新 HUD。
/// 预制体 Addressables id 应为 <c>battlepanel</c>（即 prefab_battlepanel）。
/// </summary>
public class BattleUIPanel : UIPanel
{
    [Header("战斗画面（子物体默认名 BattleDisplay）")]
    [SerializeField] RectTransform battleDisplayRoot;

    [Header("HUD")]
    [SerializeField] TMP_Text battleInfoText;

    [Header("运行时数据")]
    [SerializeField] TMP_Text runtimeDataText;

    [Header("Boss 血条")]
    [Tooltip("Boss 入场后显示；无 Boss 或 Boss 退场/击败后隐藏。")]
    [SerializeField] GameObject bossHpBarRoot;
    [SerializeField] Image bossHpFillImage;
    [SerializeField] TMP_Text bossNameText;

    [Header("Boss 水平浮标")]
    [Tooltip("沿战斗区底边指示 Boss 水平位置；留空则不显示。")]
    [SerializeField] RectTransform bossMarkerRect;
    [Tooltip("浮标横向映射的参考宽度；默认使用 battleDisplayRoot。")]
    [SerializeField] RectTransform bossMarkerTrackRect;

    [Header("暂停")]
    [SerializeField] GameObject pauseOverlayRoot;
    [SerializeField] Button continueBattleButton;
    [SerializeField] Button restartBattleButton;
    [SerializeField] Button quitBattleButton;
    [SerializeField] TMP_Text pauseTitleText;

    const string BattleDisplayChildName = "BattleDisplay";
    const string BattleInfoChildName = "BattleInfo";
    const string PauseOverlayChildName = "Pause";
    const string RuntimeDataChildName = "RuntimeData";
    const string RestartBtnChildName = "RestartBtn";
    const int RenderTextureHeight = 720;

    RawImage _battleRaw;
    Camera _battlePresentationCamera;
    RenderTexture _battleRenderTexture;
    readonly List<Camera> _sceneCamerasDisabledForBattleView = new();
    float _bossMarkerTrackHalfWidth;

    public override void Initialize()
    {
        ResolveBattleDisplayViewport();
        ResolvePauseUiReferences();
        ResolveBattleInfoText();
        ResolveRuntimeDataText();
        HideBossHpBar();
        SetPauseOverlayVisible(false);
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        SetupBattleViewportFromAreaData();
        DisableCamerasRenderingToScreen();
        HideBossHpBar();
        SetPauseOverlayVisible(false);
        BindPauseButtons();
        RefreshHudFromBattleManager();
    }

    public override void OnHide()
    {
        UnbindPauseButtons();
        SetPauseOverlayVisible(false);
        base.OnHide();
        TeardownBattleViewport();
        RestoreDisabledCameras();
        HideBossHpBar();
    }

    void OnDestroy()
    {
        UnbindPauseButtons();
        TeardownBattleViewport();
        RestoreDisabledCameras();
    }

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null || bm.CurrentStatus != E_BattleStatus.InBattle)
            return;

        BattleRuntimeMetrics.RecordRenderFrame();

        if (bm.IsBattlePaused)
        {
            SetPauseOverlayVisible(true);
            RefreshPauseOverlayButtons(bm);

            if (bm.CanResumeBattle && WasPauseKeyPressed())
                ResumeBattle();

            RefreshHudFromBattleManager();
            RefreshRuntimeData();
            return;
        }

        // 联机：房主恢复后客户端仅同步了 IsBattlePaused，需主动关闭暂停层
        SetPauseOverlayVisible(false);

        if (bm.IsLocalSpectating)
        {
            SetPauseOverlayVisible(false);
            RefreshHudFromBattleManager();
            RefreshRuntimeData();
            return;
        }

        if (bm.CanPauseBattle && WasPauseKeyPressed())
            PauseBattle();

        RefreshHudFromBattleManager();
        RefreshRuntimeData();
    }

    void ResolveBattleInfoText()
    {
        if (battleInfoText != null)
            return;

        battleInfoText = transform.Find(BattleInfoChildName)?.GetComponent<TMP_Text>();
    }

    void ResolveRuntimeDataText()
    {
        if (runtimeDataText != null)
            return;

        runtimeDataText = transform.Find(RuntimeDataChildName)?.GetComponent<TMP_Text>();
    }

    void RefreshRuntimeData()
    {
        ResolveRuntimeDataText();
        if (runtimeDataText == null)
            return;

        var bm = BattleManager.Instance;
        if (bm == null || !bm.TryGetBattleRuntimeSnapshot(out BattleRuntimeSnapshot snap))
        {
            runtimeDataText.text = string.Empty;
            return;
        }

        runtimeDataText.text =
            $"渲染 FPS: {snap.RenderFps:0.#}\n" +
            $"逻辑 FPS: {snap.LogicFps:0.#}\n" +
            $"实体: {snap.ActiveEntityCount}\n" +
            $"GO: {snap.ActiveGameObjectCount}";
    }

    void ResolvePauseUiReferences()
    {
        if (pauseOverlayRoot == null)
        {
            Transform pause = transform.Find(PauseOverlayChildName);
            if (pause != null)
                pauseOverlayRoot = pause.gameObject;
        }

        if (pauseOverlayRoot == null)
            return;

        if (continueBattleButton == null)
            continueBattleButton = pauseOverlayRoot.transform.Find("ContinueBtn")?.GetComponent<Button>();

        if (restartBattleButton == null)
            restartBattleButton = pauseOverlayRoot.transform.Find(RestartBtnChildName)?.GetComponent<Button>();

        if (quitBattleButton == null)
            quitBattleButton = pauseOverlayRoot.transform.Find("QuitBtn")?.GetComponent<Button>();

        if (pauseTitleText == null)
            pauseTitleText = pauseOverlayRoot.transform.Find("PauseText")?.GetComponent<TMP_Text>();

        EnsureRestartButton();
    }

    void EnsureRestartButton()
    {
        if (restartBattleButton != null || pauseOverlayRoot == null || continueBattleButton == null)
            return;

        var clone = Instantiate(continueBattleButton.gameObject, pauseOverlayRoot.transform);
        clone.name = RestartBtnChildName;
        var rect = clone.GetComponent<RectTransform>();
        if (rect != null && continueBattleButton.TryGetComponent<RectTransform>(out var srcRect))
        {
            rect.anchorMin = srcRect.anchorMin;
            rect.anchorMax = srcRect.anchorMax;
            rect.pivot = srcRect.pivot;
            rect.anchoredPosition = srcRect.anchoredPosition + new Vector2(0f, -70f);
            rect.sizeDelta = srcRect.sizeDelta;
        }

        restartBattleButton = clone.GetComponent<Button>();
        var label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "重新开始";
    }

    void BindPauseButtons()
    {
        ResolvePauseUiReferences();

        if (continueBattleButton != null)
        {
            continueBattleButton.onClick.RemoveListener(OnContinueBattleClicked);
            continueBattleButton.onClick.AddListener(OnContinueBattleClicked);
        }

        if (restartBattleButton != null)
        {
            restartBattleButton.onClick.RemoveListener(OnRestartBattleClicked);
            restartBattleButton.onClick.AddListener(OnRestartBattleClicked);
        }

        if (quitBattleButton != null)
        {
            quitBattleButton.onClick.RemoveListener(OnQuitBattleClicked);
            quitBattleButton.onClick.AddListener(OnQuitBattleClicked);
        }
    }

    void UnbindPauseButtons()
    {
        if (continueBattleButton != null)
            continueBattleButton.onClick.RemoveListener(OnContinueBattleClicked);

        if (restartBattleButton != null)
            restartBattleButton.onClick.RemoveListener(OnRestartBattleClicked);

        if (quitBattleButton != null)
            quitBattleButton.onClick.RemoveListener(OnQuitBattleClicked);
    }

    static bool WasPauseKeyPressed()
    {
        var input = InputManager.Instance;
        if (input == null)
            return Input.GetKeyDown(KeyCode.Escape);

        return Input.GetKeyDown(input.KeyConfig.pause);
    }

    void PauseBattle()
    {
        var bm = BattleManager.Instance;
        if (bm == null || !bm.CanPauseBattle || bm.IsBattlePaused)
            return;

        bm.LocalRequestPause();
        SetPauseOverlayVisible(true);
    }

    void ResumeBattle()
    {
        var bm = BattleManager.Instance;
        if (bm == null || !bm.CanResumeBattle)
            return;

        bm.LocalRequestResumeBattle();
        SetPauseOverlayVisible(false);
    }

    void RefreshPauseOverlayButtons(BattleManager bm)
    {
        bool gameOverSingle = bm.PauseReason == E_BattlePauseReason.GameOverSingle;
        bool gameOverMulti = bm.PauseReason == E_BattlePauseReason.GameOverMulti;
        bool stageClearSingle = bm.PauseReason == E_BattlePauseReason.StageClearSingle;
        bool stageClearMulti = bm.PauseReason == E_BattlePauseReason.StageClearMulti;
        bool stageClear = stageClearSingle || stageClearMulti;
        bool manualPause = bm.PauseReason == E_BattlePauseReason.Manual;
        bool returnToRoom = gameOverMulti || stageClearMulti;
        bool clientWaitingHostResume = manualPause && !bm.isSinglePlayerMode && !bm.CanResumeBattle;
        bool hostManualPauseMulti = manualPause && !bm.isSinglePlayerMode && bm.CanResumeBattle;

        if (pauseTitleText != null)
        {
            if (stageClear)
                pauseTitleText.text = "成功通过关卡";
            else if (gameOverSingle || gameOverMulti)
                pauseTitleText.text = "游戏结束";
            else if (clientWaitingHostResume)
                pauseTitleText.text = "房主已暂停\n请等待房主继续游戏";
            else
                pauseTitleText.text = "暂停";
        }

        if (clientWaitingHostResume)
        {
            if (continueBattleButton != null)
                continueBattleButton.gameObject.SetActive(false);
            if (restartBattleButton != null)
                restartBattleButton.gameObject.SetActive(false);
            if (quitBattleButton != null)
                quitBattleButton.gameObject.SetActive(false);
            return;
        }

        if (continueBattleButton != null)
            continueBattleButton.gameObject.SetActive(manualPause && bm.CanResumeBattle);

        if (restartBattleButton != null)
        {
            bool showRestart = gameOverSingle
                || stageClearSingle
                || bm.CanHostRestartAfterStageClear;
            restartBattleButton.gameObject.SetActive(showRestart);
        }

        if (quitBattleButton != null)
        {
            bool showQuit = (manualPause && bm.isSinglePlayerMode)
                || hostManualPauseMulti
                || gameOverSingle
                || stageClearSingle
                || returnToRoom;
            quitBattleButton.gameObject.SetActive(showQuit);
            var quitLabel = quitBattleButton.GetComponentInChildren<TMP_Text>(true);
            if (quitLabel != null)
                quitLabel.text = returnToRoom || hostManualPauseMulti ? "返回房间" : "退出战斗";
        }
    }

    void OnContinueBattleClicked() => ResumeBattle();

    void OnRestartBattleClicked()
    {
        var bm = BattleManager.Instance;
        if (bm == null)
            return;

        SetPauseOverlayVisible(false);

        if (bm.PauseReason == E_BattlePauseReason.GameOverSingle
            || bm.PauseReason == E_BattlePauseReason.StageClearSingle)
        {
            bm.RestartSinglePlayerBattle();
            return;
        }

        if (bm.CanHostRestartAfterStageClear)
            bm.HostRequestRestartMultiplayerBattle();
    }

    void OnQuitBattleClicked()
    {
        var bm = BattleManager.Instance;
        if (bm == null)
            return;

        if (bm.PauseReason == E_BattlePauseReason.Manual
            && !bm.isSinglePlayerMode
            && bm.CanResumeBattle)
        {
            bm.HostRequestReturnToRoomFromPause();
            return;
        }

        if (bm.PauseReason == E_BattlePauseReason.GameOverMulti
            || bm.PauseReason == E_BattlePauseReason.StageClearMulti)
        {
            bm.QuitBattleToRoomAsync().Forget();
        }
        else
        {
            bm.QuitBattleToMenuAsync().Forget();
        }
    }

    void SetPauseOverlayVisible(bool visible)
    {
        if (pauseOverlayRoot != null)
            pauseOverlayRoot.SetActive(visible);
    }

    void ResolveBattleDisplayViewport()
    {
        if (_battleRaw != null)
            return;

        Transform root = battleDisplayRoot != null ? battleDisplayRoot : transform.Find(BattleDisplayChildName);
        if (root == null)
        {
            Logger.Warn("[BattleUIPanel] 未找到 BattleDisplay，战斗区域 RenderTexture 不可用。", LogTag.UI);
            return;
        }

        _battleRaw = root.GetComponentInChildren<RawImage>(true);
        if (_battleRaw != null)
            return;

        var viewGo = new GameObject("BattleViewRawImage");
        viewGo.transform.SetParent(root, false);
        var rtf = viewGo.AddComponent<RectTransform>();
        StretchRectToParent(rtf);
        _battleRaw = viewGo.AddComponent<RawImage>();
        _battleRaw.color = Color.white;
        _battleRaw.raycastTarget = false;
    }

    static void StretchRectToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    void SetupBattleViewportFromAreaData()
    {
        ResolveBattleDisplayViewport();
        if (_battleRaw == null || !GlobalBattleData.IsInitialized)
            return;

        TeardownBattleViewport(false);

        var area = GlobalBattleData.AreaData;
        float aspect = area.Width / Mathf.Max(0.001f, area.Height);
        int h = RenderTextureHeight;
        int w = Mathf.Clamp(Mathf.RoundToInt(h * aspect), 64, 4096);

        _battleRenderTexture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            name = "BattleArea_RT"
        };
        _battleRenderTexture.Create();

        var camGo = new GameObject("BattlePresentationCamera");
        camGo.transform.SetParent(transform, false);
        _battlePresentationCamera = camGo.AddComponent<Camera>();
        _battlePresentationCamera.targetTexture = _battleRenderTexture;
        _battlePresentationCamera.clearFlags = CameraClearFlags.SolidColor;
        _battlePresentationCamera.backgroundColor = new Color(0.02f, 0.02f, 0.06f, 1f);
        _battlePresentationCamera.orthographic = true;
        _battlePresentationCamera.orthographicSize = area.Height * 0.5f;
        _battlePresentationCamera.transform.position = new Vector3(area.Center.x, area.Center.y, -10f);
        _battlePresentationCamera.nearClipPlane = 0.01f;
        _battlePresentationCamera.farClipPlane = 100f;
        _battlePresentationCamera.depth = -50f;
        _battlePresentationCamera.cullingMask = ~0;
        _battlePresentationCamera.allowHDR = false;
        _battlePresentationCamera.allowMSAA = false;

        _battleRaw.texture = _battleRenderTexture;
        _battleRaw.enabled = true;
        CacheBossMarkerTrackWidth();
    }

    void CacheBossMarkerTrackWidth()
    {
        _bossMarkerTrackHalfWidth = 0f;
        RectTransform track = bossMarkerTrackRect != null ? bossMarkerTrackRect : battleDisplayRoot;
        if (track == null)
            return;

        _bossMarkerTrackHalfWidth = track.rect.width * 0.5f;
    }

    void TeardownBattleViewport(bool clearRawBinding = true)
    {
        if (_battlePresentationCamera != null)
        {
            Destroy(_battlePresentationCamera.gameObject);
            _battlePresentationCamera = null;
        }

        if (_battleRenderTexture != null)
        {
            _battleRenderTexture.Release();
            Destroy(_battleRenderTexture);
            _battleRenderTexture = null;
        }

        if (clearRawBinding && _battleRaw != null)
            _battleRaw.texture = null;
    }

    void DisableCamerasRenderingToScreen()
    {
        _sceneCamerasDisabledForBattleView.Clear();

#if UNITY_2023_1_OR_NEWER
        Camera[] all = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        Camera[] all = Object.FindObjectsOfType<Camera>();
#endif
        foreach (var cam in all)
        {
            if (cam == null || !cam.enabled || cam.targetTexture != null)
                continue;
            if (cam == _battlePresentationCamera)
                continue;

            _sceneCamerasDisabledForBattleView.Add(cam);
            cam.enabled = false;
        }
    }

    void RestoreDisabledCameras()
    {
        for (int i = 0; i < _sceneCamerasDisabledForBattleView.Count; i++)
        {
            var cam = _sceneCamerasDisabledForBattleView[i];
            if (cam != null)
                cam.enabled = true;
        }

        _sceneCamerasDisabledForBattleView.Clear();
    }

    void RefreshHudFromBattleManager()
    {
        var bm = BattleManager.Instance;
        if (bm == null)
            return;

        RefreshBossHpBar(bm);

        if (!bm.TryGetBattleHudSnapshot(out BattleHudSnapshot snap))
            return;

        ResolveBattleInfoText();
        if (battleInfoText != null)
        {
            battleInfoText.text =
                $"分数: {snap.Score}\n" +
                $"生命: {FormatLifeText(snap.HealthCurrent, snap.HealthMax)}\n" +
                $"Power: {snap.PowerOrbs}";
        }
    }

    void RefreshBossHpBar(BattleManager bm)
    {
        if (bossHpBarRoot == null && bossHpFillImage == null && bossNameText == null && bossMarkerRect == null)
            return;

        if (bm.TryGetBossHudSnapshot(out BossHudSnapshot bossSnap))
        {
            if (bossHpBarRoot != null && !bossHpBarRoot.activeSelf)
                bossHpBarRoot.SetActive(true);

            if (bossHpFillImage != null)
                bossHpFillImage.fillAmount = bossSnap.NormalizedHealth;

            ApplyBossNameDisplay(bossSnap.DisplayName);
            ApplyBossMarkerPosition(bossSnap.NormalizedHorizontal);
            return;
        }

        HideBossHpBar();
    }

    void ApplyBossNameDisplay(string displayName)
    {
        if (bossNameText == null)
            return;

        if (string.IsNullOrEmpty(displayName))
        {
            bossNameText.text = string.Empty;
            if (bossNameText.gameObject.activeSelf)
                bossNameText.gameObject.SetActive(false);
            return;
        }

        bossNameText.text = displayName;
        if (!bossNameText.gameObject.activeSelf)
            bossNameText.gameObject.SetActive(true);
    }

    void ApplyBossMarkerPosition(float normalizedHorizontal)
    {
        if (bossMarkerRect == null)
            return;

        if (!bossMarkerRect.gameObject.activeSelf)
            bossMarkerRect.gameObject.SetActive(true);

        RectTransform track = bossMarkerTrackRect != null ? bossMarkerTrackRect : battleDisplayRoot;
        if (track == null)
            return;

        if (_bossMarkerTrackHalfWidth <= 0.001f)
            CacheBossMarkerTrackWidth();
        if (_bossMarkerTrackHalfWidth <= 0.001f)
            return;

        Vector2 anchored = bossMarkerRect.anchoredPosition;
        anchored.x = Mathf.Lerp(-_bossMarkerTrackHalfWidth, _bossMarkerTrackHalfWidth, normalizedHorizontal);
        bossMarkerRect.anchoredPosition = anchored;
    }

    void HideBossHpBar()
    {
        if (bossHpBarRoot != null && bossHpBarRoot.activeSelf)
            bossHpBarRoot.SetActive(false);

        if (bossNameText != null)
        {
            bossNameText.text = string.Empty;
            if (bossNameText.gameObject.activeSelf)
                bossNameText.gameObject.SetActive(false);
        }

        if (bossMarkerRect != null && bossMarkerRect.gameObject.activeSelf)
            bossMarkerRect.gameObject.SetActive(false);
    }

    static string FormatLifeText(int cur, int max)
    {
        if (max <= 0)
            return string.Empty;

        if (max <= 10)
        {
            int hearts = Mathf.Clamp(cur, 0, max);
            return new string('\u2665', hearts);
        }

        return $"{cur}/{max}";
    }
}
