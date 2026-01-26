using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Text;

[RequireComponent(typeof(ARPointCloudManager))]
public class MapRecorder : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 0.25f; // Độ phân giải lưới (25cm)
    public float minHeight = -3.0f; // Bỏ qua điểm dưới -3m (quá thấp, có thể lỗi AR)
    public float maxHeight = 5.0f; // Bỏ qua điểm trên 5m (quá cao, có thể lỗi AR)
    
    [Header("Wall/Floor Classification")]
    [Tooltip("Điểm thấp hơn giá trị này = sàn nhà")]
    public float floorMaxHeight = -0.2f;
    
    [Tooltip("Điểm cao hơn giá trị này = vật cản")]
    public float wallMinHeight = 0.3f;
    
    [Tooltip("Điểm cao hơn giá trị này = trần nhà (bỏ qua)")]
    public float ceilingHeight = 2.5f;

    private ARPointCloudManager pointCloudManager;
    private ARSession arSession; // FIX LỖI #6
    
    // FIX LỖI #3: Sử dụng Dictionary với grid coordinate để lọc trùng lặp chính xác
    private Dictionary<Vector2Int, PointData> gridPoints = new Dictionary<Vector2Int, PointData>();
    
    // Lưu camera rotation ban đầu để normalize tọa độ
    private Quaternion? initialCameraRotation = null;
    private Vector3? initialCameraPosition = null;
    private Transform arCamera;
    
    private bool isRecording = false;
    
    // Struct để lưu thông tin điểm
    private struct PointData
    {
        public Vector3 position;
        public float height; // Y coordinate trong camera-relative space
        
        public PointData(Vector3 pos, float h)
        {
            position = pos;
            height = h;
        }
    }
    
    // PUBLIC API cho ScanVisualizer
    public bool IsRecording => isRecording;
    public int PointCount => gridPoints.Count;
    
    // FIX LỖI #8: Trả về copy của points để tránh allocation mỗi frame
    public HashSet<Vector3> GetAllPoints()
    {
        HashSet<Vector3> result = new HashSet<Vector3>(gridPoints.Count);
        foreach (var kvp in gridPoints)
        {
            result.Add(kvp.Value.position);
        }
        return result;
    }

    void Awake()
    {
        // Tự động tìm các components
        if (pointCloudManager == null)
            pointCloudManager = FindFirstObjectByType<ARPointCloudManager>();

        if (pointCloudManager == null)
        {
            Debug.LogError("❌ LỖI NGHIÊM TRỌNG: Không tìm thấy ARPointCloudManager trong Scene!");
            enabled = false;
            return;
        }
        
        // FIX LỖI #6: Kiểm tra ARSession
        arSession = FindFirstObjectByType<ARSession>();
        if (arSession == null)
        {
            Debug.LogError("❌ LỖI NGHIÊM TRỌNG: Không tìm thấy ARSession trong Scene!");
            Debug.LogError("Hãy thêm ARSession GameObject vào Scene!");
            enabled = false;
            return;
        }
        
        // Tìm AR Camera
        arCamera = Camera.main?.transform;
        if (arCamera == null)
        {
            Debug.LogWarning("⚠ Không tìm thấy AR Camera (Main Camera)");
        }
        
        // QUAN TRỌNG: Bật Point Cloud Manager
        pointCloudManager.enabled = true;
    }

    void OnEnable()
    {
        if (pointCloudManager != null)
            pointCloudManager.trackablesChanged.AddListener(OnPointCloudsChanged);
    }

    void OnDisable()
    {
        if (pointCloudManager != null)
            pointCloudManager.trackablesChanged.RemoveListener(OnPointCloudsChanged);
    }

    // FIX LỖI #11: Xử lý cả added, updated, removed
    void OnPointCloudsChanged(ARTrackablesChangedEventArgs<ARPointCloud> args)
    {
        if (!isRecording) return;
        
        // FIX LỖI #6: Kiểm tra ARSession state
        if (arSession != null && ARSession.state < ARSessionState.SessionTracking)
        {
            Debug.LogWarning("⚠ AR Session chưa sẵn sàng để tracking!");
            return;
        }

        // Set origin nếu chưa có
        if (!initialCameraRotation.HasValue && arCamera != null)
        {
            initialCameraPosition = arCamera.position;
            // Normalize về Y-axis only
            Vector3 eulerAngles = arCamera.eulerAngles;
            initialCameraRotation = Quaternion.Euler(0, eulerAngles.y, 0);
            Debug.Log($"✓ MapRecorder origin set: Pos={initialCameraPosition.Value}, Yaw={eulerAngles.y:F1}°");
        }
        
        if (!initialCameraRotation.HasValue || !initialCameraPosition.HasValue) return;
        
        Vector3 origin = initialCameraPosition.Value;
        Quaternion inverseRotation = Quaternion.Inverse(initialCameraRotation.Value);

        // Process added clouds
        foreach (var pointCloud in args.added)
        {
            ProcessPointCloud(pointCloud, origin, inverseRotation);
        }
        
        // Process updated clouds
        foreach (var pointCloud in args.updated)
        {
            ProcessPointCloud(pointCloud, origin, inverseRotation);
        }
    }
    
    void ProcessPointCloud(ARPointCloud pointCloud, Vector3 origin, Quaternion inverseRotation)
    {
        if (!pointCloud.positions.HasValue) return;
        
        foreach (var pos in pointCloud.positions.Value)
        {
            // Lọc theo chiều cao tuyệt đối
            if (pos.y < minHeight || pos.y > maxHeight)
                continue;
            
            // Normalize về camera-relative space
            Vector3 normalizedPoint = pos - origin;
            Vector3 cameraRelativePoint = inverseRotation * normalizedPoint;
            
            // FIX LỖI #3: Chuyển sang grid coordinate để lọc trùng lặp chính xác
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(cameraRelativePoint.x / cellSize),
                Mathf.FloorToInt(cameraRelativePoint.z / cellSize)
            );
            
            // Lưu vào dictionary (tự động override nếu trùng cell)
            // Ưu tiên giữ điểm cao nhất trong mỗi cell (thường là vật cản)
            if (!gridPoints.ContainsKey(gridPos) || 
                cameraRelativePoint.y > gridPoints[gridPos].height)
            {
                gridPoints[gridPos] = new PointData(pos, cameraRelativePoint.y);
            }
        }
    }

    public void StartRecording()
    {
        // FIX LỖI #6: Kiểm tra ARSession trước khi bắt đầu
        if (arSession != null && ARSession.state < ARSessionState.SessionTracking)
        {
            Debug.LogError("❌ Không thể bắt đầu quét: AR Session chưa sẵn sàng!");
            Debug.LogError($"AR Session State: {ARSession.state}");
            Debug.LogError("Hãy đợi AR Session khởi động hoàn toàn!");
            return;
        }
        
        isRecording = true;
        gridPoints.Clear();
        
        // Reset origin
        initialCameraPosition = null;
        initialCameraRotation = null;
        
        // Đảm bảo ARPointCloudManager đang hoạt động
        if (pointCloudManager != null)
        {
            pointCloudManager.enabled = true;
            Debug.Log("========================================");
            Debug.Log("✓ BẮT ĐẦU QUÉT MAP");
            Debug.Log("📱 Di chuyển thiết bị xung quanh để thu thập điểm.");
            Debug.Log("⚪ Các điểm trắng trên màn hình là Point Cloud.");
            Debug.Log("========================================");
        }
        else
        {
            Debug.LogError("❌ Không thể bắt đầu quét: ARPointCloudManager không tồn tại!");
        }
    }

    public void StopRecordingAndSave(string mapName)
    {
        isRecording = false;
        Debug.Log($"Kết thúc quét. Tổng số cells thu được: {gridPoints.Count}");
        
        // FIX LỖI #9: Validate map data
        if (gridPoints.Count == 0)
        {
            Debug.LogError("❌ CẢNH BÁO: Không thu thập được điểm nào!");
            Debug.LogError("Nguyên nhân có thể:");
            Debug.LogError("- ARPointCloudManager chưa hoạt động");
            Debug.LogError("- AR Session chưa tracking");
            Debug.LogError("- Môi trường thiếu ánh sáng hoặc texture");
            Debug.LogError("- Di chuyển thiết bị quá nhanh");
            return;
        }
        
        if (gridPoints.Count < 100)
        {
            Debug.LogWarning($"⚠ Map chỉ có {gridPoints.Count} cells - rất nhỏ!");
            Debug.LogWarning("Khuyến nghị: Di chuyển nhiều hơn để thu thập thêm điểm.");
        }

        // FIX LỖI #7: Kiểm tra FileSystemManager trước khi lưu
        if (FileSystemManager.Instance == null)
        {
            Debug.LogError("❌ LỖI NGHIÊM TRỌNG: FileSystemManager Instance is null!");
            Debug.LogError("GIẢI PHÁP:");
            Debug.LogError("1. Tạo Empty GameObject tên 'FileSystemManager'");
            Debug.LogError("2. Add Component → FileSystemManager script");
            Debug.LogError("3. Script FileSystemManager phải có DontDestroyOnLoad trong Awake()");
            Debug.LogError("4. Đảm bảo FileSystemManager GameObject được tạo TRƯỚC khi vào MapCreatorScene");
            return;
        }

        // FIX LỖI #4: Chuyển đổi và Lưu với coordinate normalization
        string mapData = ConvertToGridFormat();
        
        FileSystemManager.Instance.SaveMapData(mapName, mapData);
        
        string realPath = System.IO.Path.Combine(Application.persistentDataPath, mapName + ".txt");
        Debug.Log("========================================");
        Debug.Log($"✓ THÀNH CÔNG! Map đã được lưu tại:");
        Debug.Log($"📁 {realPath}");
        Debug.Log("========================================");
        
        // Tạo file JSON rỗng mặc định
        string defaultJson = "{ \"destinations\": [] }";
        FileSystemManager.Instance.SaveMapInfo(mapName, defaultJson);
        
        string jsonPath = System.IO.Path.Combine(Application.persistentDataPath, mapName + ".json");
        Debug.Log($"✓ Map Info JSON lưu tại: {jsonPath}");
    }

    // FIX LỖI #4: THUẬT TOÁN CHUYỂN ĐỔI với coordinate normalization
    string ConvertToGridFormat()
    {
        if (!initialCameraRotation.HasValue || !initialCameraPosition.HasValue)
        {
            Debug.LogError("❌ Lỗi: Origin chưa được set!");
            return "";
        }
        
        Vector3 origin = initialCameraPosition.Value;
        Quaternion inverseRotation = Quaternion.Inverse(initialCameraRotation.Value);
        
        // A. Tìm biên giới hạn trong CAMERA-RELATIVE SPACE
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        // Tạo dictionary để lưu grid với classification
        Dictionary<Vector2Int, int> finalGrid = new Dictionary<Vector2Int, int>();

        foreach (var kvp in gridPoints)
        {
            var pointData = kvp.Value;
            
            // Normalize về camera-relative space
            Vector3 normalizedPoint = pointData.position - origin;
            Vector3 cameraRelativePoint = inverseRotation * normalizedPoint;
            
            // Classify point
            int cellValue = ClassifyPointForMap(cameraRelativePoint.y);
            
            // Lưu vào final grid
            finalGrid[kvp.Key] = cellValue;
            
            // Update bounds
            if (cameraRelativePoint.x < minX) minX = cameraRelativePoint.x;
            if (cameraRelativePoint.x > maxX) maxX = cameraRelativePoint.x;
            if (cameraRelativePoint.z < minZ) minZ = cameraRelativePoint.z;
            if (cameraRelativePoint.z > maxZ) maxZ = cameraRelativePoint.z;
        }

        // B. Tính kích thước lưới
        int width = Mathf.CeilToInt((maxX - minX) / cellSize) + 5;
        int height = Mathf.CeilToInt((maxZ - minZ) / cellSize) + 5;
        
        // FIX LỖI #9: Validate grid size
        if (width > 1000 || height > 1000)
        {
            Debug.LogWarning($"⚠ Map rất lớn: {width}x{height}! Có thể bị lỗi khi load.");
        }

        // C. Khởi tạo ma trận (Mặc định 0 là đường đi)
        int[,] grid = new int[width, height];

        // D. Mapping điểm vào grid
        foreach (var kvp in finalGrid)
        {
            Vector2Int gridPos = kvp.Key;
            
            // Tính lại vị trí trong normalized space
            float worldX = gridPos.x * cellSize;
            float worldZ = gridPos.y * cellSize;
            
            int x = Mathf.FloorToInt((worldX - minX) / cellSize) + 2;
            int z = Mathf.FloorToInt((worldZ - minZ) / cellSize) + 2;

            if (x >= 0 && x < width && z >= 0 && z < height)
            {
                grid[x, z] = kvp.Value; // 0 = floor, 1 = wall
            }
        }

        // E. Xuất chuỗi string
        StringBuilder sb = new StringBuilder();
        
        // Dòng 1: Resolution (cm)
        sb.AppendLine(((int)(cellSize * 100)).ToString()); 
        
        // Dòng 2: Width Height
        sb.AppendLine($"{width} {height}");

        // Dòng 3 trở đi: Ma trận (Z ngược để hiển thị đúng)
        for (int z = height - 1; z >= 0; z--)
        {
            for (int x = 0; x < width; x++)
            {
                sb.Append(grid[x, z] + " ");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
    
    // Classify point cho map output (0 = floor/đường đi, 1 = wall/vật cản)
    int ClassifyPointForMap(float y)
    {
        // Trần nhà - bỏ qua (coi như floor)
        if (y > ceilingHeight)
            return 0;
        
        // Vật cản (tường, bàn, ghế)
        if (y >= wallMinHeight)
            return 1;
        
        // Sàn nhà
        return 0;
    }
}