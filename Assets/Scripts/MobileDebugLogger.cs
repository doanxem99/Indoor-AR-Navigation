using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// On-screen debug logger cho mobile testing
/// Hiển thị logs trực tiếp lên màn hình vì không có console trên điện thoại
/// </summary>
public class MobileDebugLogger : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text debugText;
    public ScrollRect scrollRect;
    public GameObject debugPanel;
    public Button toggleButton;
    
    [Header("Settings")]
    public int maxLines = 50;
    public bool showTimestamp = true;
    public bool autoScroll = true;
    
    private Queue<string> logQueue = new Queue<string>();
    private bool isPanelVisible = false;

    void Awake()
    {
        // Subscribe to Unity's log messages
        Application.logMessageReceived += HandleLog;
        
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }
        
        // Start with panel hidden
        if (debugPanel != null)
        {
            debugPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string prefix = "";
        switch (type)
        {
            case LogType.Error:
                prefix = "<color=red>[ERROR]</color> ";
                break;
            case LogType.Warning:
                prefix = "<color=yellow>[WARNING]</color> ";
                break;
            case LogType.Log:
                prefix = "<color=white>[LOG]</color> ";
                break;
        }

        string timestamp = showTimestamp ? $"[{System.DateTime.Now:HH:mm:ss}] " : "";
        string formattedLog = timestamp + prefix + logString;

        logQueue.Enqueue(formattedLog);

        // Keep only last N lines
        while (logQueue.Count > maxLines)
        {
            logQueue.Dequeue();
        }

        UpdateDebugText();
    }

    void UpdateDebugText()
    {
        if (debugText == null) return;

        debugText.text = string.Join("\n", logQueue);

        // Auto scroll to bottom
        if (autoScroll && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        if (debugPanel != null)
        {
            debugPanel.SetActive(isPanelVisible);
        }
    }

    public void ClearLogs()
    {
        logQueue.Clear();
        UpdateDebugText();
    }
}
