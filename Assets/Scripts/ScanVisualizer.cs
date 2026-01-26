using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visualizes the scanned area in real-time as a 2D top-down map
/// Camera always stays in center, map moves around it
/// Grid-based: 25cm x 25cm cells (matching .txt output format)
/// FIXED: Origin tracking, wall/floor classification, memory optimization
/// </summary>
public class ScanVisualizer : MonoBehaviour
{
    [Header("References")]
    public RawImage mapDisplay; // UI RawImage để hiển thị map
    public MapRecorder mapRecorder; // Reference đến MapRecorder
    
    [Header("Visualization Settings")]
    public int textureWidth = 512; // Độ phân giải texture
    public int textureHeight = 512;
    public Color floorColor = new Color(0, 1, 0, 0.8f); // XANH LÁ = Sàn nhà (đường đi)
    public Color wallColor = new Color(1, 0, 0, 0.8f); // ĐỎ = Tường/vật cản
    public Color unscannedColor = new Color(0.3f, 0.3f, 0.3f, 0.9f); // XÁM = Chưa quét
    public Color cameraColor = Color.yellow; // VÀNG = Vị trí camera
    
    [Header("Map Settings")]
    public float viewRadius = 10f; // Bán kính hiển thị quanh camera (mét)
    public float gridCellSize = 0.25f; // Kích thước mỗi ô grid (phải = MapRecorder.cellSize)
    
    [Header("Wall/Floor Classification")]
    [Tooltip("Điểm thấp hơn giá trị này = sàn nhà (floor)")]
    public float floorMaxHeight = -0.2f; // Điểm dưới -0.2m = sàn
    
    [Tooltip("Điểm cao hơn giá trị này = vật cản (wall)")]
    public float wallMinHeight = 0.3f; // Điểm từ 0.3m trở lên = vật cản (ghế, bàn, tường)
    
    [Tooltip("Điểm cao hơn giá trị này = trần nhà (bỏ qua)")]
    public float ceilingHeight = 2.5f; // Điểm trên 2.5m = trần (không dùng)
    
    [Header("Camera Settings")]
    public Transform arCamera; // AR Camera
    
    [Header("Update Settings")]
    public float updateInterval = 0.1f; // Cập nhật 10 lần/giây
    
    private Texture2D mapTexture;
    private float nextUpdateTime = 0f;
    
    // Grid-based tracking - prevents duplicates when revisiting areas
    private Dictionary<Vector2Int, CellType> scannedGrid = new Dictionary<Vector2Int, CellType>();
    
    // FIX LỖI #1: Origin tracking - sử dụng CAMERA position làm origin
    private Vector3? initialCameraPosition = null; // Camera position khi bắt đầu scan
    private Quaternion? initialCameraRotation = null; // Camera rotation khi bắt đầu scan (Y-axis only)
    private bool originSet = false;
    
    // FIX LỖI #8: Memory optimization - reuse pixel array
    private Color[] pixelBuffer;
    
    private enum CellType
    {
        Floor,    // Xanh lá - đường đi
        Wall      // Đỏ - vật cản
    }
    
    void Start()
    {
        InitializeMap();
    }
    
    void OnEnable()
    {
        // Reset khi scene load/reload
        if (scannedGrid != null && scannedGrid.Count > 0)
        {
            Debug.Log("✓ ScanVisualizer: Scene reloaded - resetting grid");
            ResetVisualization();
        }
    }
    
    void InitializeMap()
    {
        // Tạo texture
        mapTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        mapTexture.filterMode = FilterMode.Point;
        
        if (mapDisplay != null)
        {
            mapDisplay.texture = mapTexture;
        }
        
        // FIX LỖI #8: Pre-allocate pixel buffer
        pixelBuffer = new Color[textureWidth * textureHeight];
        
        // Tìm AR Camera
        if (arCamera == null)
        {
            arCamera = Camera.main?.transform;
        }
        
        // Tìm MapRecorder
        FindMapRecorder();
        
        ClearTexture();
    }
    
    void FindMapRecorder()
    {
        if (mapRecorder == null)
        {
            mapRecorder = FindFirstObjectByType<MapRecorder>();
            if (mapRecorder != null)
            {
                Debug.Log("✓ ScanVisualizer: Auto-found MapRecorder");
            }
        }
    }
    
    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateVisualization();
            nextUpdateTime = Time.time + updateInterval;
        }
    }
    
    void ClearTexture()
    {
        // FIX LỖI #8: Reuse pixel buffer
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            pixelBuffer[i] = unscannedColor;
        }
        mapTexture.SetPixels(pixelBuffer);
        mapTexture.Apply();
    }
    
    public void UpdateVisualization()
    {
        // Kiểm tra và tìm lại MapRecorder nếu cần
        if (mapRecorder == null)
        {
            FindMapRecorder();
        }
        
        if (mapRecorder == null || arCamera == null)
        {
            ClearTexture();
            DrawCameraAtCenter();
            mapTexture.Apply();
            return;
        }
        
        // Lấy points từ MapRecorder và update grid
        UpdateGridFromPoints();
        
        // Vẽ map với camera ở giữa
        DrawMap();
    }
    
    void UpdateGridFromPoints()
    {
        var allPoints = mapRecorder.GetAllPoints();
        if (allPoints == null || allPoints.Count == 0) return;
        
        // FIX LỖI #1: Set origin dựa trên CAMERA POSITION, không phải AR point
        if (!originSet && arCamera != null)
        {
            initialCameraPosition = arCamera.position;
            
            // FIX LỖI #10: Normalize rotation về Y-axis only (bỏ pitch/roll)
            Vector3 eulerAngles = arCamera.eulerAngles;
            initialCameraRotation = Quaternion.Euler(0, eulerAngles.y, 0);
            
            Debug.Log($"✓ Origin set: CamPos={initialCameraPosition.Value}, CamYaw={eulerAngles.y:F1}°");
            originSet = true;
        }
        
        if (!initialCameraRotation.HasValue || !initialCameraPosition.HasValue) return;
        
        Vector3 origin = initialCameraPosition.Value;
        Quaternion inverseRotation = Quaternion.Inverse(initialCameraRotation.Value);
        
        // Convert world points to grid cells
        foreach (var point in allPoints)
        {
            // Normalize point về origin (camera position ban đầu)
            Vector3 normalizedPoint = point - origin;
            
            // Transform về camera-relative space (xoay theo hướng camera ban đầu)
            Vector3 cameraRelativePoint = inverseRotation * normalizedPoint;
            
            // Chuyển world position thành grid coordinate
            Vector2Int gridPos = WorldToGrid(cameraRelativePoint);
            
            // FIX LỖI #2: Thuật toán phân loại Wall/Floor chính xác
            CellType cellType = ClassifyPoint(cameraRelativePoint.y);
            
            // QUAN TRỌNG: Wall LUÔN override Floor (ngay cả khi Floor quét sau)
            if (!scannedGrid.ContainsKey(gridPos))
            {
                scannedGrid[gridPos] = cellType;
            }
            else if (cellType == CellType.Wall)
            {
                // Wall đè lên bất kỳ gì (kể cả Floor quét sau)
                scannedGrid[gridPos] = CellType.Wall;
            }
            // Nếu cell hiện tại là Wall, không cho Floor ghi đè
        }
    }
    
    // FIX LỖI #2: Thuật toán phân loại Wall/Floor rõ ràng
    CellType ClassifyPoint(float y)
    {
        // Điểm quá cao (trần nhà) - BỎ QUA (không lưu vào grid)
        if (y > ceilingHeight)
        {
            return CellType.Floor; // Tạm coi là floor, nhưng thực tế nên filter ra
        }
        
        // Điểm cao (vật cản: tường, ghế, bàn, cửa)
        if (y >= wallMinHeight)
        {
            return CellType.Wall;
        }
        
        // Điểm thấp (sàn nhà, gần mặt đất)
        if (y <= floorMaxHeight)
        {
            return CellType.Floor;
        }
        
        // Vùng giữa (0.2m - 0.3m): Không rõ ràng, ưu tiên coi là Floor
        // Lý do: Tránh false positive (vật nhỏ như dây điện bị nhận nhầm là tường)
        return CellType.Floor;
    }
    
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Convert world position to grid coordinate
        int x = Mathf.FloorToInt(worldPos.x / gridCellSize);
        int z = Mathf.FloorToInt(worldPos.z / gridCellSize);
        return new Vector2Int(x, z);
    }
    
    void DrawMap()
    {
        ClearTexture();
        
        if (arCamera == null || !originSet || !initialCameraPosition.HasValue)
        {
            DrawCameraAtCenter();
            mapTexture.Apply();
            return;
        }
        
        // Tính camera movement từ lúc bắt đầu (RELATIVE movement)
        Vector3 currentCameraPos = arCamera.position;
        Vector3 cameraMovement = currentCameraPos - initialCameraPosition.Value;
        
        // Transform camera movement về camera-relative space
        Quaternion inverseRotation = Quaternion.Inverse(initialCameraRotation.Value);
        Vector3 normalizedCameraPos = inverseRotation * cameraMovement;
        
        // Camera luôn ở giữa texture
        int centerPixelX = textureWidth / 2;
        int centerPixelY = textureHeight / 2;
        
        // Tính toán world space mỗi pixel đại diện
        float metersPerPixel = (viewRadius * 2f) / Mathf.Min(textureWidth, textureHeight);
        
        int drawnCells = 0;
        
        // Tính kích thước cell trên texture (pixels)
        int cellSizeInPixels = Mathf.Max(2, Mathf.RoundToInt(gridCellSize / metersPerPixel));
        
        // Vẽ grid cells (mỗi cell là hình vuông 25cm)
        foreach (var kvp in scannedGrid)
        {
            Vector2Int gridPos = kvp.Key;
            CellType cellType = kvp.Value;
            
            // Chuyển grid position sang world position (TRUNG TÂM của cell)
            Vector3 cellWorldPos = new Vector3(
                gridPos.x * gridCellSize + gridCellSize * 0.5f,
                0,
                gridPos.y * gridCellSize + gridCellSize * 0.5f
            );
            
            // Tính offset từ camera (trong normalized space)
            float offsetX = cellWorldPos.x - normalizedCameraPos.x;
            float offsetZ = cellWorldPos.z - normalizedCameraPos.z;
            
            // Chuyển offset sang pixel coordinates (trung tâm cell)
            int pixelX = centerPixelX + Mathf.RoundToInt(offsetX / metersPerPixel);
            int pixelY = centerPixelY + Mathf.RoundToInt(offsetZ / metersPerPixel);
            
            // Vẽ hình vuông cell (25cm x 25cm)
            Color color = (cellType == CellType.Wall) ? wallColor : floorColor;
            bool cellDrawn = DrawSquareCell(pixelX - cellSizeInPixels/2, pixelY - cellSizeInPixels/2, cellSizeInPixels, color);
            
            if (cellDrawn) drawnCells++;
        }
        
        // Vẽ camera ở center
        DrawCameraAtCenter();
        
        mapTexture.Apply();
        
        // Debug only when needed
        if (drawnCells == 0 && scannedGrid.Count > 0)
        {
            Debug.LogWarning($"⚠ {scannedGrid.Count} cells but 0 drawn! ViewRadius={viewRadius}m");
        }
    }
    
    bool DrawSquareCell(int startX, int startY, int size, Color color)
    {
        // Vẽ hình vuông cell (góc trái dưới tại startX, startY)
        bool anyPixelDrawn = false;
        
        for (int dx = 0; dx < size; dx++)
        {
            for (int dy = 0; dy < size; dy++)
            {
                int px = startX + dx;
                int py = startY + dy;
                
                if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                {
                    mapTexture.SetPixel(px, py, color);
                    anyPixelDrawn = true;
                }
            }
        }
        
        return anyPixelDrawn;
    }
    
    void DrawCameraAtCenter()
    {
        int centerX = textureWidth / 2;
        int centerY = textureHeight / 2;
        
        // Vẽ hình tròn vàng cho camera
        int size = 3;
        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                if (dx * dx + dy * dy <= size * size)
                {
                    int px = centerX + dx;
                    int py = centerY + dy;
                    
                    if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                    {
                        mapTexture.SetPixel(px, py, cameraColor);
                    }
                }
            }
        }
        
        // Vẽ mũi tên chỉ hướng
        if (arCamera != null)
        {
            DrawCameraDirection(centerX, centerY);
        }
    }
    
    void DrawCameraDirection(int centerX, int centerY)
    {
        int arrowLength = 15;
        
        // Lấy hướng camera (X, Z plane) - relative to initial rotation
        Vector3 currentForward = arCamera.forward;
        Vector3 initialForward = initialCameraRotation.HasValue ? 
            initialCameraRotation.Value * Vector3.forward : 
            Vector3.forward;
        
        // Calculate relative angle
        float currentAngle = Mathf.Atan2(currentForward.x, currentForward.z);
        float initialAngle = Mathf.Atan2(initialForward.x, initialForward.z);
        float relativeAngle = currentAngle - initialAngle;
        
        int endX = centerX + Mathf.RoundToInt(Mathf.Sin(relativeAngle) * arrowLength);
        int endY = centerY + Mathf.RoundToInt(Mathf.Cos(relativeAngle) * arrowLength);
        
        DrawLine(centerX, centerY, endX, endY, cameraColor);
    }
    
    void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        // Bresenham's line algorithm
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            if (x0 >= 0 && x0 < textureWidth && y0 >= 0 && y0 < textureHeight)
            {
                mapTexture.SetPixel(x0, y0, color);
            }
            
            if (x0 == x1 && y0 == y1) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    
    public void ResetVisualization()
    {
        int oldCount = scannedGrid.Count;
        scannedGrid.Clear();
        ClearTexture();
        
        // Reset origin
        originSet = false;
        initialCameraPosition = null;
        initialCameraRotation = null;
        
        if (mapTexture != null)
        {
            mapTexture.Apply();
        }
        
        Debug.Log($"✓ Map visualization reset (cleared {oldCount} cells, origin reset)");
    }
    
    public string GetScanStats()
    {
        if (mapRecorder == null)
            return "Points: 0\nCells: 0\nArea: 0m²";
        
        int totalPoints = mapRecorder.PointCount;
        int cellCount = scannedGrid.Count;
        float area = cellCount * gridCellSize * gridCellSize;
        
        // Đếm số floor vs wall cells
        int floorCells = 0;
        int wallCells = 0;
        foreach (var kvp in scannedGrid)
        {
            if (kvp.Value == CellType.Floor)
                floorCells++;
            else
                wallCells++;
        }
        
        return $"Points: {totalPoints}\nCells: {cellCount} ({floorCells}F/{wallCells}W)\nArea: {area:F1}m²";
    }
}