using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to automatically setup split-screen layout for scanner
/// Top half: AR Camera view
/// Bottom half: 2D map visualization
/// </summary>
[ExecuteInEditMode]
public class SplitScreenLayoutHelper : MonoBehaviour
{
    [Header("Layout Settings")]
    [Range(0.3f, 0.7f)]
    public float topViewHeight = 0.5f; // 50% màn hình phía trên
    
    [Header("References (Auto-find if empty)")]
    public Camera arCamera;
    public RectTransform mapContainer; // Container chứa map visualization
    public Canvas mainCanvas;
    
    [Header("Auto Setup")]
    public bool autoSetupOnStart = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupSplitScreen();
        }
    }
    
    [ContextMenu("Setup Split Screen Layout")]
    public void SetupSplitScreen()
    {
        Debug.Log("========================================");
        Debug.Log("SETTING UP SPLIT SCREEN LAYOUT");
        Debug.Log("========================================");
        
        // 1. Setup AR Camera viewport
        SetupARCameraViewport();
        
        // 2. Setup Map Container
        SetupMapContainer();
        
        Debug.Log("✓ Split screen layout setup complete!");
        Debug.Log($"  - Top {topViewHeight * 100}%: AR Camera");
        Debug.Log($"  - Bottom {(1 - topViewHeight) * 100}%: Map View");
        Debug.Log("========================================");
    }
    
    void SetupARCameraViewport()
    {
        // Tìm AR Camera nếu chưa có
        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
            {
                var arCamManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
                if (arCamManager != null)
                {
                    arCamera = arCamManager.GetComponent<Camera>();
                }
            }
        }
        
        if (arCamera != null)
        {
            // Set viewport cho AR Camera - phía trên màn hình
            Rect viewport = arCamera.rect;
            viewport.x = 0;
            viewport.width = 1; // Full width
            viewport.y = 1 - topViewHeight; // Bắt đầu từ vị trí phía trên
            viewport.height = topViewHeight; // Chiếm topViewHeight% màn hình
            
            arCamera.rect = viewport;
            
            Debug.Log($"✓ AR Camera viewport set: y={viewport.y:F2}, height={viewport.height:F2}");
        }
        else
        {
            Debug.LogWarning("⚠ AR Camera not found! Please assign manually.");
        }
    }
    
    void SetupMapContainer()
    {
        // Tìm Canvas nếu chưa có
        if (mainCanvas == null)
        {
            mainCanvas = FindFirstObjectByType<Canvas>();
        }
        
        if (mapContainer == null)
        {
            // Tìm hoặc tạo Map Container
            var existing = GameObject.Find("MapVisualizationContainer");
            if (existing != null)
            {
                mapContainer = existing.GetComponent<RectTransform>();
            }
            else if (mainCanvas != null)
            {
                // Tạo mới
                GameObject mapObj = new GameObject("MapVisualizationContainer");
                mapObj.transform.SetParent(mainCanvas.transform, false);
                mapContainer = mapObj.AddComponent<RectTransform>();
                
                Debug.Log("✓ Created new MapVisualizationContainer");
            }
        }
        
        if (mapContainer != null)
        {
            // Anchor ở bottom
            mapContainer.anchorMin = new Vector2(0, 0);
            mapContainer.anchorMax = new Vector2(1, 1 - topViewHeight);
            
            // Fill toàn bộ vùng bottom
            mapContainer.offsetMin = Vector2.zero;
            mapContainer.offsetMax = Vector2.zero;
            
            // Đảm bảo có Image component để hiển thị background
            var image = mapContainer.GetComponent<Image>();
            if (image == null)
            {
                image = mapContainer.gameObject.AddComponent<Image>();
            }
            
            // Màu nền tối cho map view
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            Debug.Log($"✓ Map Container setup: anchors (0, 0) to (1, {1 - topViewHeight:F2})");
        }
        else
        {
            Debug.LogWarning("⚠ Map Container not found! Please assign manually.");
        }
    }
    
    // Điều chỉnh tỷ lệ split
    [ContextMenu("Set 60-40 Split")]
    public void Set60_40()
    {
        topViewHeight = 0.6f;
        SetupSplitScreen();
    }
    
    [ContextMenu("Set 50-50 Split")]
    public void Set50_50()
    {
        topViewHeight = 0.5f;
        SetupSplitScreen();
    }
    
    [ContextMenu("Set 70-30 Split")]
    public void Set70_30()
    {
        topViewHeight = 0.7f;
        SetupSplitScreen();
    }
    
    // Hiển thị gizmo trong editor
    void OnDrawGizmos()
    {
        if (arCamera != null)
        {
            Gizmos.color = Color.green;
            // Vẽ wireframe cho AR camera viewport
        }
    }
    
#if UNITY_EDITOR
    void Update()
    {
        // Trong Editor mode, tự động cập nhật khi thay đổi slider
        if (!Application.isPlaying)
        {
            SetupSplitScreen();
        }
    }
#endif
}
