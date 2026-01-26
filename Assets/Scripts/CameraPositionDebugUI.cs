using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị thông tin camera position trên màn hình để debug
/// </summary>
public class CameraPositionDebugUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text positionText;
    public TMP_Text rotationText;
    public TMP_Text heightText;
    
    [Header("Camera Reference")]
    public Transform cameraTransform;
    
    [Header("Settings")]
    public bool showDebugInfo = true;
    public float updateInterval = 0.1f; // Update mỗi 0.1 giây
    
    private float timer = 0f;

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }
        
        if (cameraTransform == null)
        {
            Debug.LogError("[CameraPositionDebugUI] Không tìm thấy camera!");
        }
    }

    void Update()
    {
        if (!showDebugInfo || cameraTransform == null) return;
        
        timer += Time.deltaTime;
        
        if (timer >= updateInterval)
        {
            UpdateDebugInfo();
            timer = 0f;
        }
    }

    void UpdateDebugInfo()
    {
        Vector3 pos = cameraTransform.position;
        Vector3 rot = cameraTransform.eulerAngles;
        
        if (positionText != null)
        {
            positionText.text = $"Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})";
        }
        
        if (rotationText != null)
        {
            rotationText.text = $"Rotation: ({rot.x:F1}°, {rot.y:F1}°, {rot.z:F1}°)";
        }
        
        if (heightText != null)
        {
            heightText.text = $"Height: {pos.y:F2}m";
        }
    }

    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
        
        if (positionText != null) positionText.gameObject.SetActive(showDebugInfo);
        if (rotationText != null) rotationText.gameObject.SetActive(showDebugInfo);
        if (heightText != null) heightText.gameObject.SetActive(showDebugInfo);
    }
}