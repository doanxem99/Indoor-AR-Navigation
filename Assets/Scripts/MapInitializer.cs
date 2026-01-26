using UnityEngine;

/// <summary>
/// Auto teleport camera đến vị trí ID 101 khi load map xong
/// Và cố định map ở độ cao phù hợp (1.6m từ camera)
/// </summary>
public class MapInitializer : MonoBehaviour
{
    [Header("References")]
    public MapGenerator mapGenerator;
    public Transform xrOrigin; // XR Origin (parent của camera)
    public Transform mapContainer; // Container chứa toàn bộ map
    
    [Header("Settings")]
    public int defaultSpawnLocationID = 101; // ID vị trí spawn mặc định
    public float userHeight = 0.16f; // Độ cao người dùng cầm điện thoại (meter)
    public bool autoTeleportOnLoad = true;
    public bool fixMapHeight = true;
    
    private Camera arCamera;
    private bool hasInitialized = false;

    void Start()
    {
        arCamera = Camera.main;
        
        if (arCamera == null)
        {
            Debug.LogError("[MapInitializer] Không tìm thấy camera!");
        }
    }

    void Update()
    {
        // Đảm bảo map container luôn không xoay
        if (mapContainer != null)
        {
            // Lock rotation về (0, 0, 0) - map luôn thẳng
            mapContainer.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Gọi hàm này sau khi map được load xong
    /// </summary>
    public void InitializeMapPosition()
    {
        Debug.Log("========================================");
        Debug.Log("[MapInitializer] InitializeMapPosition() được gọi!");
        Debug.Log($"[MapInitializer] hasInitialized = {hasInitialized}");
        Debug.Log($"[MapInitializer] fixMapHeight = {fixMapHeight}");
        Debug.Log($"[MapInitializer] autoTeleportOnLoad = {autoTeleportOnLoad}");
        Debug.Log("========================================");
        if (hasInitialized)
        {
            Debug.LogWarning("[MapInitializer] Map đã được initialize rồi!");
            return;
        }
        
        // // Bước 1: Cố định map ở độ cao phù hợp
        // if (fixMapHeight && mapContainer != null)
        // {
        //     FixMapHeightToCamera();
        // }
        
        // Bước 2: Teleport camera đến vị trí spawn
        if (autoTeleportOnLoad)
        {
            TeleportToSpawnLocation();
        }
        
        hasInitialized = true;
    }

    /// <summary>
    /// Cố định map ở độ cao sao cho sàn map nằm dưới chân người dùng
    /// </summary>
    // void FixMapHeightToCamera()
    // {
    //     if (arCamera == null || mapContainer == null) return;
        
    //     // Tính toán: Camera hiện tại ở độ cao nào
    //     float currentCameraHeight = arCamera.transform.position.y;
        
    //     // Độ cao sàn map = Độ cao camera - Độ cao người dùng
    //     // Ví dụ: Camera ở y=0, user cao 1.6m -> Sàn phải ở y=-1.6
    //     float targetMapFloorY = currentCameraHeight - userHeight;
        
    //     // Di chuyển map container đến độ cao đó
    //     Vector3 newPos = mapContainer.position;
    //     newPos.y = targetMapFloorY;
    //     mapContainer.position = newPos;
        
        
    //     Debug.Log($"[MapInitializer] Map floor set to Y={targetMapFloorY:F2} (Camera at Y={currentCameraHeight:F2}, User height={userHeight}m)");
    // }

    /// <summary>
    /// Teleport camera (thông qua XR Origin) đến vị trí spawn
    /// </summary>
    void TeleportToSpawnLocation()
    {
        if (mapGenerator == null || xrOrigin == null || arCamera == null)
        {
            Debug.LogError("[MapInitializer] Thiếu references để teleport!");
            return;
        }
        
        // Kiểm tra vị trí spawn có tồn tại không
        if (!mapGenerator.locationDatabase.ContainsKey(defaultSpawnLocationID))
        {
            Debug.LogWarning($"[MapInitializer] Không tìm thấy location ID {defaultSpawnLocationID}! Available IDs: {string.Join(", ", mapGenerator.locationDatabase.Keys)}");
            return;
        }
        
        Vector3 spawnPosition = mapGenerator.locationDatabase[defaultSpawnLocationID];
        
        // Tính toán offset giữa camera và XR Origin
        Vector3 cameraOffset = arCamera.transform.position - xrOrigin.position;
        
        // Chỉ giữ offset theo X và Z, bỏ qua Y (để không ảnh hưởng độ cao)
        cameraOffset.y = 0;
        
        // Di chuyển XR Origin sao cho camera nằm tại spawn position
        Vector3 newOriginPosition = spawnPosition - cameraOffset;
        
        // Giữ nguyên độ cao Y của XR Origin (không thay đổi)
        newOriginPosition.y = xrOrigin.position.y;
        
        xrOrigin.position = newOriginPosition;
        
        Debug.Log($"[MapInitializer] Teleported to location ID {defaultSpawnLocationID} at {spawnPosition}");
        Debug.Log($"[MapInitializer] XR Origin moved to {newOriginPosition}");
        Debug.Log($"[MapInitializer] Camera now at {arCamera.transform.position}");
    }

    /// <summary>
    /// Public method để teleport thủ công đến một ID bất kỳ
    /// </summary>
    public void TeleportToLocation(int locationID)
    {
        if (mapGenerator == null || xrOrigin == null || arCamera == null)
        {
            Debug.LogError("[MapInitializer] Thiếu references!");
            return;
        }
        
        if (!mapGenerator.locationDatabase.ContainsKey(locationID))
        {
            Debug.LogError($"[MapInitializer] Location ID {locationID} không tồn tại!");
            return;
        }
        
        Vector3 targetPosition = mapGenerator.locationDatabase[locationID];
        Vector3 cameraOffset = arCamera.transform.position - xrOrigin.position;
        cameraOffset.y = 0;
        
        Vector3 newOriginPosition = targetPosition - cameraOffset;
        newOriginPosition.y = xrOrigin.position.y;
        
        xrOrigin.position = newOriginPosition;
        
        Debug.Log($"[MapInitializer] Teleported to location ID {locationID}");
    }

    /// <summary>
    /// Reset lại để có thể initialize lại
    /// </summary>
    public void ResetInitialization()
    {
        hasInitialized = false;
        Debug.Log("[MapInitializer] Đã reset initialization flag");
    }

    // Vẽ Gizmos để debug
    void OnDrawGizmos()
    {
        if (mapGenerator == null || !mapGenerator.locationDatabase.ContainsKey(defaultSpawnLocationID))
            return;
        
        Vector3 spawnPos = mapGenerator.locationDatabase[defaultSpawnLocationID];
        
        // Vẽ spawn point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPos, 0.5f);
        Gizmos.DrawLine(spawnPos, spawnPos + Vector3.up * 2f);
    }
}