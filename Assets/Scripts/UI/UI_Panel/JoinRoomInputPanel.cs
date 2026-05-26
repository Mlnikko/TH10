using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoomInputPanel : UIPanel
{
    [Header("UI References")]
    [SerializeField] TMP_InputField ipInput;
    [SerializeField] TMP_InputField portInput = null;
    [SerializeField] Button joinButton;
    [SerializeField] Button cancelButton;
    [SerializeField] TMP_Text statusText;

    const int DEFAULT_PORT = 7777;

    bool _isConnecting;

    public override void Initialize()
    {
        base.Initialize();
        if (portInput != null && string.IsNullOrEmpty(portInput.text))
            portInput.text = DEFAULT_PORT.ToString();
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);

        joinButton.onClick.AddListener(OnJoinClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        if (ipInput != null)
            ipInput.onEndEdit.AddListener(OnInputEndEdit);
        if (portInput != null)
            portInput.onEndEdit.AddListener(OnInputEndEdit);

        BindRoomEvents();
        SetConnecting(false);
        ClearStatus();
        ipInput?.Select();
    }

    public override void OnHide()
    {
        if (_isConnecting)
            RoomManager.Instance?.CancelJoinAttempt();

        UnbindRoomEvents();
        joinButton.onClick.RemoveListener(OnJoinClicked);
        cancelButton.onClick.RemoveListener(OnCancelClicked);
        if (ipInput != null)
            ipInput.onEndEdit.RemoveListener(OnInputEndEdit);
        if (portInput != null)
            portInput.onEndEdit.RemoveListener(OnInputEndEdit);

        base.OnHide();
    }

    void BindRoomEvents()
    {
        var rm = RoomManager.Instance;
        if (rm == null)
            return;

        rm.OnJoinStarted += HandleJoinStarted;
        rm.OnJoinFailed += HandleJoinFailed;
        rm.OnJoinSucceeded += HandleJoinSucceeded;
    }

    void UnbindRoomEvents()
    {
        var rm = RoomManager.Instance;
        if (rm == null)
            return;

        rm.OnJoinStarted -= HandleJoinStarted;
        rm.OnJoinFailed -= HandleJoinFailed;
        rm.OnJoinSucceeded -= HandleJoinSucceeded;
    }

    void HandleJoinStarted() => SetConnecting(true);

    void HandleJoinFailed(string message)
    {
        SetConnecting(false);
        ShowStatus(message, Color.red);
    }

    void HandleJoinSucceeded()
    {
        SetConnecting(false);
        ClearStatus();
    }

    void OnInputEndEdit(string _)
    {
        if (!_isConnecting)
            ClearStatus();
    }

    void OnJoinClicked()
    {
        if (_isConnecting)
            return;

        string ip = ipInput?.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            ShowStatus("请输入 IP 地址", Color.red);
            return;
        }

        if (!NetworkTool.IsValidHostAddress(ip))
        {
            ShowStatus("IP 地址格式无效", Color.red);
            return;
        }

        int port = DEFAULT_PORT;
        if (portInput != null && !string.IsNullOrWhiteSpace(portInput.text))
        {
            if (!int.TryParse(portInput.text.Trim(), out port) || port <= 0 || port > 65535)
            {
                ShowStatus("端口无效（1~65535）", Color.red);
                return;
            }
        }

        ShowStatus("正在连接...", new Color(1f, 0.85f, 0.2f));
        SetConnecting(true);

        if (!RoomManager.Instance.TryJoinRoom(ip, (ushort)port))
            SetConnecting(false);
    }

    void SetConnecting(bool connecting)
    {
        _isConnecting = connecting;

        if (joinButton != null)
            joinButton.interactable = !connecting;
        if (cancelButton != null)
            cancelButton.interactable = true;
        if (ipInput != null)
            ipInput.interactable = !connecting;
        if (portInput != null)
            portInput.interactable = !connecting;
    }

    void ClearStatus() => ShowStatus(string.Empty, Color.white);

    void ShowStatus(string message, Color color)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.color = color;
    }

    void OnCancelClicked()
    {
        if (_isConnecting)
            RoomManager.Instance?.CancelJoinAttempt();

        UIManager.Instance.GoBack();
    }
}
