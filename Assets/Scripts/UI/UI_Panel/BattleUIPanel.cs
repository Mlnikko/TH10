using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD：将逻辑战斗区 <see cref="GlobalBattleData.AreaData"/> 渲染到 <see cref="RawImage"/>，
/// 并轮询 <see cref="BattleManager.TryGetBattleHudSnapshot"/> 更新分数 / 体力 / 火力道具。
/// 预制体 Addressables id 应为 <c>battlepanel</c>（即 prefab_battlepanel）。
/// </summary>
public class BattleUIPanel : UIPanel
{
    [Header("战斗画面（子物体默认名 BattleDisplay）")]
    [SerializeField] RectTransform battleDisplayRoot;

    [Header("HUD")]
    [SerializeField] Image rankImage;
    [SerializeField] TMP_Text hiScoreValueText;
    [SerializeField] TMP_Text scoreValueText;
    [SerializeField] TMP_Text playerLifeValueText;
    [SerializeField] TMP_Text powerValueText;

    [SerializeField] Sprite[] rankSprite;

    const string BattleDisplayChildName = "BattleDisplay";
    const int RenderTextureHeight = 720;

    RawImage _battleRaw;
    Camera _battlePresentationCamera;
    RenderTexture _battleRenderTexture;
    readonly List<Camera> _sceneCamerasDisabledForBattleView = new();

    public void SetRank(E_Rank rank)
    {
        if (rankImage == null || rankSprite == null || rankSprite.Length == 0)
            return;

        int idx = rank switch
        {
            E_Rank.Eazy => 0,
            E_Rank.Normal => 1,
            E_Rank.Hard => 2,
            E_Rank.Lunatic => 3,
            E_Rank.Extra => 4,
            _ => 0
        };
        idx = Mathf.Clamp(idx, 0, rankSprite.Length - 1);
        rankImage.sprite = rankSprite[idx];
    }

    public override void Initialize()
    {
        ResolveBattleDisplayViewport();
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        SetupBattleViewportFromAreaData();
        DisableCamerasRenderingToScreen();
        RefreshHudFromBattleManager();
    }

    public override void OnHide()
    {
        base.OnHide();
        TeardownBattleViewport();
        RestoreDisabledCameras();
    }

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null || bm.CurrentStatus != E_BattleStatus.InBattle)
            return;
        RefreshHudFromBattleManager();
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
        if (bm == null || !bm.TryGetBattleHudSnapshot(out BattleHudSnapshot snap))
            return;

        CommitHiScoreIfNeeded(snap.Score);

        if (scoreValueText != null)
            scoreValueText.text = snap.Score.ToString();

        if (hiScoreValueText != null)
        {
            int hi = PlayerPrefs.GetInt("BattleHiScore", 0);
            hiScoreValueText.text = Mathf.Max(hi, snap.Score).ToString();
        }

        ApplyLifeDisplay(snap.HealthCurrent, snap.HealthMax);

        if (powerValueText != null)
            powerValueText.text = snap.PowerOrbs.ToString();
    }

    static void CommitHiScoreIfNeeded(int score)
    {
        int hi = PlayerPrefs.GetInt("BattleHiScore", 0);
        if (score > hi)
        {
            PlayerPrefs.SetInt("BattleHiScore", score);
            PlayerPrefs.Save();
        }
    }

    void ApplyLifeDisplay(int cur, int max)
    {
        if (playerLifeValueText == null)
            return;

        if (max <= 0)
        {
            playerLifeValueText.text = "";
            return;
        }

        if (max <= 10)
        {
            int hearts = Mathf.Clamp(cur, 0, max);
            playerLifeValueText.text = new string('\u2665', hearts);
        }
        else
            playerLifeValueText.text = $"{cur}/{max}";
    }
}
