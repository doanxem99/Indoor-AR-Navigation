using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;

/// <summary>
/// Script debug để kiểm tra trạng thái ARPointCloudManager và hiển thị thông tin point cloud
/// Giúp phát hiện vấn đề khi Point Cloud không hiển thị
/// </summary>
[RequireComponent(typeof(ARPointCloudManager))]
public class ARPointCloudDebugger : MonoBehaviour
{
    [Header("Debug Display")]
    public TMP_Text debugText; // Kéo Text UI vào đây để hiển thị debug info
    public bool showDebugInfo = true;
    
    [Header("Visualization")]
    public bool visualizePoints = true;
    public Color pointColor = Color.white;
    public float pointSize = 0.02f;

    private ARPointCloudManager pointCloudManager;
    private int totalPointsDetected = 0;
    private int activePointClouds = 0;

    void Awake()
    {
        pointCloudManager = GetComponent<ARPointCloudManager>();
        
        if (pointCloudManager == null)
        {
            Debug.LogError("ARPointCloudManager không tìm thấy!");
            return;
        }

        // Đảm bảo Point Cloud Manager được enable
        pointCloudManager.enabled = true;
    }

    void OnEnable()
    {
        if (pointCloudManager != null)
        {
            pointCloudManager.trackablesChanged.AddListener(OnPointCloudChanged);
        }
    }

    void OnDisable()
    {
        if (pointCloudManager != null)
        {
            pointCloudManager.trackablesChanged.RemoveListener(OnPointCloudChanged);
        }
    }

    void OnPointCloudChanged(ARTrackablesChangedEventArgs<ARPointCloud> eventArgs)
    {
        // Đếm point cloud
        activePointClouds = 0;
        totalPointsDetected = 0;

        foreach (var pointCloud in pointCloudManager.trackables)
        {
            if (pointCloud.positions.HasValue)
            {
                activePointClouds++;
                totalPointsDetected += pointCloud.positions.Value.Length;
            }
        }

        UpdateDebugDisplay();
    }

    void Update()
    {
        if (showDebugInfo && Time.frameCount % 30 == 0) // Cập nhật mỗi 30 frame
        {
            UpdateDebugDisplay();
        }
    }

    void UpdateDebugDisplay()
    {
        if (debugText == null || !showDebugInfo) return;

        string info = "=== AR POINT CLOUD DEBUG ===\n\n";
        
        // Kiểm tra trạng thái AR Session
        var arSession = FindFirstObjectByType<ARSession>();
        if (arSession != null)
        {
            info += $"AR Session: {(arSession.enabled ? "✓ Active" : "✗ Inactive")}\n";
        }
        else
        {
            info += "AR Session: ✗ Not Found\n";
        }

        // Kiểm tra Point Cloud Manager
        if (pointCloudManager != null)
        {
            info += $"Point Cloud Manager: {(pointCloudManager.enabled ? "✓ Enabled" : "✗ Disabled")}\n";
            info += $"Subsystem: {(pointCloudManager.subsystem != null ? "✓ Running" : "✗ Not Running")}\n\n";
            
            info += $"Active Point Clouds: {activePointClouds}\n";
            info += $"Total Points: {totalPointsDetected:N0}\n";
            
            // Kiểm tra trackables
            info += $"Trackables Count: {pointCloudManager.trackables.count}\n\n";
            
            if (totalPointsDetected == 0)
            {
                info += "⚠ CẢNH BÁO:\n";
                info += "Chưa phát hiện Point Cloud!\n";
                info += "• Di chuyển thiết bị xung quanh\n";
                info += "• Đảm bảo có ánh sáng đủ\n";
                info += "• Tránh bề mặt phản chiếu\n";
            }
            else
            {
                info += "✓ Point Cloud đang hoạt động\n";
            }
        }
        else
        {
            info += "✗ Point Cloud Manager: NULL\n";
        }

        debugText.text = info;
    }

    // Vẽ point cloud lên màn hình (chỉ trong Editor/Development Build)
    void OnDrawGizmos()
    {
        if (!visualizePoints || pointCloudManager == null) return;

        Gizmos.color = pointColor;
        
        foreach (var pointCloud in pointCloudManager.trackables)
        {
            if (pointCloud.positions.HasValue)
            {
                foreach (var position in pointCloud.positions.Value)
                {
                    Gizmos.DrawSphere(position, pointSize);
                }
            }
        }
    }

    // Hàm kiểm tra AR subsystem
    public void CheckARSubsystem()
    {
        var arSession = FindFirstObjectByType<ARSession>();
        if (arSession == null)
        {
            Debug.LogError("✗ Không tìm thấy ARSession trong Scene!");
            Debug.LogError("Hãy thêm ARSession GameObject vào Scene.");
            return;
        }

        if (pointCloudManager.subsystem == null)
        {
            Debug.LogError("✗ AR Point Cloud Subsystem chưa khởi động!");
            Debug.LogError("Có thể thiết bị không hỗ trợ AR hoặc AR Session chưa sẵn sàng.");
            return;
        }

        Debug.Log("✓ AR Subsystem đang hoạt động bình thường.");
    }
}
