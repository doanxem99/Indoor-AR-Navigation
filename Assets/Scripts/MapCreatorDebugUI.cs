using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị debug info cho Map Creator scene
/// Bao gồm: Camera Position, Grid Position, Scan Stats
/// </summary>
public class MapCreatorDebugUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text cameraPositionText;
    public TMP_Text gridPositionText;
    public TMP_Text scanStatsText;
    public TMP_Text finalGridText; // Hiển thị final grid bounds
    
    [Header("References")]
    public Transform arCamera;
    public ScanVisualizer scanVisualizer;
    public MapRecorder mapRecorder;
    
    [Header("Settings")]
    public float updateInterval = 0.1f;
    public float gridCellSize = 0.25f; // Phải match với MapRecorder
    
    private float timer = 0f;
    private Vector3? initialCameraPosition = null;
    private Quaternion? initialCameraRotation = null;

    void Start()
    {
        // Auto-find components
        if (arCamera == null)
        {
            arCamera = Camera.main?.transform;
        }
        
        if (scanVisualizer == null)
        {
            scanVisualizer = FindFirstObjectByType<ScanVisualizer>();
        }
        
        if (mapRecorder == null)
        {
            mapRecorder = FindFirstObjectByType<MapRecorder>();
        }
        
        // Store initial camera position/rotation
        if (arCamera != null)
        {
            initialCameraPosition = arCamera.position;
            initialCameraRotation = arCamera.rotation;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= updateInterval)
        {
            UpdateDebugInfo();
            timer = 0f;
        }
    }

    void UpdateDebugInfo()
    {
        if (arCamera == null) return;
        
        // 1. Camera Position (World & Relative)
        UpdateCameraPosition();
        
        // 2. Grid Position
        UpdateGridPosition();
        
        // 3. Scan Stats
        UpdateScanStats();
        
        // 4. Final Grid Bounds
        UpdateFinalGrid();
    }

    void UpdateCameraPosition()
    {
        if (cameraPositionText == null || arCamera == null) return;
        
        Vector3 worldPos = arCamera.position;
        Vector3 relativePos = Vector3.zero;
        
        if (initialCameraPosition.HasValue)
        {
            relativePos = worldPos - initialCameraPosition.Value;
        }
        
        cameraPositionText.text = 
            $"<b>CAMERA</b>\n" +
            $"World: ({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})\n" +
            $"Relative: ({relativePos.x:F2}, {relativePos.y:F2}, {relativePos.z:F2})\n" +
            $"Rotation: {arCamera.eulerAngles.y:F0}°";
    }

    void UpdateGridPosition()
    {
        if (gridPositionText == null || arCamera == null) return;
        
        Vector3 worldPos = arCamera.position;
        Vector3 relativePos = worldPos;
        
        if (initialCameraPosition.HasValue && initialCameraRotation.HasValue)
        {
            Vector3 movement = worldPos - initialCameraPosition.Value;
            Quaternion inverseRotation = Quaternion.Inverse(initialCameraRotation.Value);
            relativePos = inverseRotation * movement;
        }
        
        // Convert to grid coordinates
        Vector2Int gridPos = WorldToGrid(relativePos);
        
        gridPositionText.text = 
            $"<b>GRID POSITION</b>\n" +
            $"Grid: ({gridPos.x}, {gridPos.y})\n" +
            $"World XZ: ({relativePos.x:F2}, {relativePos.y:F2}), {relativePos.z:F2})\n" +
            $"Cell: {gridCellSize}m";
    }

    void UpdateScanStats()
    {
        if (scanStatsText == null) return;
        
        if (scanVisualizer != null)
        {
            scanStatsText.text = $"<b>SCAN STATS</b>\n{scanVisualizer.GetScanStats()}";
        }
        else if (mapRecorder != null)
        {
            scanStatsText.text = 
                $"<b>SCAN STATS</b>\n" +
                $"Points: {mapRecorder.PointCount}\n" +
                $"Recording: {mapRecorder.IsRecording}";
        }
    }

    void UpdateFinalGrid()
    {
        if (finalGridText == null || mapRecorder == null) return;
        
        var allPoints = mapRecorder.GetAllPoints();
        if (allPoints == null || allPoints.Count == 0)
        {
            finalGridText.text = "<b>FINAL GRID</b>\nNo data";
            return;
        }
        
        // Calculate bounds
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (var point in allPoints)
        {
            Vector3 normalizedPoint = point;
            
            // Normalize to initial camera position if available
            if (initialCameraPosition.HasValue && initialCameraRotation.HasValue)
            {
                Vector3 movement = point - initialCameraPosition.Value;
                Quaternion inverseRotation = Quaternion.Inverse(initialCameraRotation.Value);
                normalizedPoint = inverseRotation * movement;
            }
            
            min.x = Mathf.Min(min.x, normalizedPoint.x);
            min.y = Mathf.Min(min.y, normalizedPoint.y);
            min.z = Mathf.Min(min.z, normalizedPoint.z);
            
            max.x = Mathf.Max(max.x, normalizedPoint.x);
            max.y = Mathf.Max(max.y, normalizedPoint.y);
            max.z = Mathf.Max(max.z, normalizedPoint.z);
        }
        
        // Convert to grid
        Vector2Int minGrid = WorldToGrid(min);
        Vector2Int maxGrid = WorldToGrid(max);
        
        int gridWidth = maxGrid.x - minGrid.x + 1;
        int gridHeight = maxGrid.y - minGrid.y + 1;
        
        finalGridText.text = 
            $"<b>FINAL GRID</b>\n" +
            $"Min: ({minGrid.x}, {minGrid.y})\n" +
            $"Max: ({maxGrid.x}, {maxGrid.y})\n" +
            $"Size: {gridWidth} x {gridHeight}\n" +
            $"Area: {gridWidth * gridHeight * gridCellSize * gridCellSize:F1}m²";
    }

    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / gridCellSize);
        int z = Mathf.FloorToInt(worldPos.z / gridCellSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Reset initial position (call khi bắt đầu scan mới)
    /// </summary>
    public void ResetInitialPosition()
    {
        if (arCamera != null)
        {
            initialCameraPosition = arCamera.position;
            initialCameraRotation = arCamera.rotation;
            Debug.Log($"[MapCreatorDebugUI] Reset initial position: {initialCameraPosition.Value}");
        }
    }
}
