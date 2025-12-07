using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkTestUI : MonoBehaviour
{
    // UI References
    public Button startHostButton;
    public Button startClientButton;
    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;

    void Start()
    {
        if (startHostButton) startHostButton.onClick.AddListener(OnStartHost);
        if (startClientButton) startClientButton.onClick.AddListener(OnStartClient);

        ipInputField.text = "127.0.0.1";
        portInputField.text = "7777";      
    }

    void OnStartHost()
    {
        try
        {
            NetworkManager.Instance.StartHost();
            Logger.Debug("Started as Host (Player 0)");
        }
        catch (Exception e)
        {
            Logger.Debug("Failed to start host: " + e.Message);
        }
    }

    void OnStartClient()
    {
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            Logger.Debug("IP cannot be empty");
            return;
        }

        if (!ushort.TryParse(portInputField.text, out ushort port) || port == 0)
        {
            Logger.Debug("Invalid port");
            return;
        }

        try
        {
            NetworkManager.Instance.StartClient(ip, port);
            Logger.Debug($"Connecting as Client to {ip}:{port}");
        }
        catch (Exception e)
        {
            Logger.Debug("Failed to connect: " + e.Message);
        }
    }
}