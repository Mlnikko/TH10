using System;
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

    public override void Initialize()
    {
        base.Initialize();
        if (portInput != null && string.IsNullOrEmpty(portInput.text))
        {
            portInput.text = DEFAULT_PORT.ToString();
        }
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);

        joinButton.onClick.AddListener(OnJoinClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        ipInput.onEndEdit.AddListener(OnInputEndEdit);
        portInput.onEndEdit.AddListener(OnInputEndEdit);

        ClearStatus();
        ipInput.Select();
    }

    void OnInputEndEdit(string _) => ClearStatus();

    void OnJoinClicked()
    {
        string ip = ipInput?.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            ShowStatus("请输入 IP 地址", Color.red);
            return;
        }

        int port = DEFAULT_PORT;
        if (portInput != null && !string.IsNullOrEmpty(portInput.text))
        {
            if (!int.TryParse(portInput.text, out port) || port <= 0 || port > 65535)
            {
                ShowStatus("端口无效（1~65535）", Color.red);
                return;
            }
        }

        ShowStatus("正在连接...", Color.yellow);

        try
        {
            RoomManager.Instance.JoinRoom(ip, (ushort)port);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            ShowStatus("连接失败: " + ex.Message, Color.red);
        }
    }

    void ClearStatus()
    {
        ShowStatus("", Color.white);
    }

    void ShowStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }

    void OnCancelClicked()
    {
        UIManager.Instance.GoBack();
    }

    public override void OnHide()
    {
        base.OnHide();

        joinButton.onClick.RemoveListener(OnJoinClicked);
        cancelButton.onClick.RemoveListener(OnCancelClicked);
        ipInput.onEndEdit.RemoveListener(OnInputEndEdit);
        portInput.onEndEdit.RemoveListener(OnInputEndEdit);
    }
}