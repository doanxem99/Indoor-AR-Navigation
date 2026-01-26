using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    [Header("Settings")]
    public float lineWidth = 0.6f; // Độ rộng đường đi (0.6m là vừa đẹp cho AR)
    public float floorOffset = 0.01f; // Nhấc lên khỏi sàn 5cm để không bị z-fighting
    public float textureScrollSpeed = 2.0f; // Tốc độ chạy của mũi tên
    public float textureTiling = 1.0f; // Độ lặp lại của mũi tên
    
    [Header("Mobile Compatibility")]
    public bool useMobileOptimizedSettings = true; // Tự động phát hiện mobile và optimize
    public Color pathColor = new Color(0f, 0.8f, 1f, 1f); // Màu xanh dương phát sáng

    private LineRenderer lineRenderer;
    private Material pathMaterial;
    private bool isMobile;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        // Phát hiện platform
        isMobile = Application.platform == RuntimePlatform.Android || 
                   Application.platform == RuntimePlatform.IPhonePlayer;
        
        // Cấu hình LineRenderer tự động bằng code để đảm bảo đúng
        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        
        // QUAN TRỌNG: Để đường nằm bẹt xuống sàn như tấm thảm
        lineRenderer.alignment = LineAlignment.TransformZ;
        
        // Làm mịn các góc cua
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        
        // Mobile optimization: Giảm vertex count
        if (isMobile && useMobileOptimizedSettings)
        {
            lineRenderer.numCornerVertices = 2;
            lineRenderer.numCapVertices = 2;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
        
        // Setup material cho mobile
        SetupMaterial();
    }
    
    void SetupMaterial()
    {
        // Tạo material mới với shader phù hợp
        pathMaterial = new Material(Shader.Find("Sprites/Default"));
        
        // Nếu là mobile, dùng shader đơn giản hơn
        if (isMobile && useMobileOptimizedSettings)
        {
            // Dùng Unlit shader cho mobile (performance tốt nhất)
            pathMaterial = new Material(Shader.Find("Unlit/Color"));
            pathMaterial.color = pathColor;
            
            Debug.Log("[PathVisualizer] Using mobile-optimized Unlit shader");
        }
        else
        {
            // PC có thể dùng shader phức tạp hơn
            pathMaterial = new Material(Shader.Find("Sprites/Default"));
            pathMaterial.color = pathColor;
        }
        
        // Set render queue để render trên surface
        pathMaterial.renderQueue = 3000;
        
        // Apply material
        lineRenderer.material = pathMaterial;
        lineRenderer.startColor = pathColor;
        lineRenderer.endColor = pathColor;
        
        Debug.Log($"[PathVisualizer] Material setup complete. Mobile mode: {isMobile}");
    }

    void Update()
    {
        // Hiệu ứng: Làm texture trôi đi để tạo cảm giác dẫn đường
        // Chỉ chạy nếu không phải mobile hoặc đã tắt optimization
        if (lineRenderer.positionCount > 0 && pathMaterial != null && pathMaterial.HasProperty("_MainTex"))
        {
            if (!isMobile || !useMobileOptimizedSettings)
            {
                // Di chuyển texture theo thời gian (Offset)
                float offset = Time.time * textureScrollSpeed;
                pathMaterial.mainTextureOffset = new Vector2(-offset, 0);
            }
        }
    }

    // Hàm nhận dữ liệu từ NavigationController để vẽ
    public void DrawPath(Vector3[] pathCorners)
    {
        if (pathCorners == null || pathCorners.Length < 2)
        {
            Debug.LogWarning("[PathVisualizer] Path has less than 2 points, clearing...");
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = pathCorners.Length;

        for (int i = 0; i < pathCorners.Length; i++)
        {
            Vector3 point = pathCorners[i];
            // QUAN TRỌNG: Set Y về đúng độ cao sàn map
            // Lấy từ MapGenerator.mapContainer.position.y
            if (MapGenerator.Instance != null && MapGenerator.Instance.mapContainer != null)
            {
                point.y = MapGenerator.Instance.mapContainer.position.y + floorOffset;
            }
            else
            {
                // Fallback: Nâng nhẹ lên khỏi NavMesh position
                point.y += floorOffset;
            }
            
            lineRenderer.SetPosition(i, point);
            lineRenderer.SetPosition(i, point);
        }
        
        Debug.Log($"[PathVisualizer] Drew path with {pathCorners.Length} points");

        // Tự động tính toán Tiling để mũi tên không bị méo khi đường dài/ngắn
        if (pathMaterial != null && pathMaterial.HasProperty("_MainTex"))
        {
            pathMaterial.mainTextureScale = new Vector2(lineRenderer.positionCount * textureTiling, 1);
        }
    }

    public void ClearPath()
    {
        lineRenderer.positionCount = 0;
        Debug.Log("[PathVisualizer] Path cleared");
    }
    
    /// <summary>
    /// Force enable/disable LineRenderer visibility (for debugging)
    /// </summary>
    public void SetVisibility(bool visible)
    {
        lineRenderer.enabled = visible;
        Debug.Log($"[PathVisualizer] LineRenderer visibility: {visible}");
    }
}
