using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

/// <summary>
/// Script để liệt kê và chọn file map đã lưu trong persistentDataPath
/// Dùng cho Navigation Scene (Scene 2)
/// </summary>
public class MapFileSelector : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mapSelectionPanel; // Panel chứa danh sách file map
    public Transform contentContainer;   // Content của ScrollView
    public GameObject mapButtonPrefab;   // Prefab nút để hiển thị mỗi map file
    public Button loadMapButton;         // Nút "Load Map" - chỉ hiện khi đã chọn map
    public TMP_Text selectedMapNameText; // Text hiển thị tên map đã chọn
    
    [Header("MapGenerator Reference")]
    public MapGenerator mapGenerator;    // Tham chiếu đến MapGenerator để load map
    public MapInitializer mapInitializer;  // Tham chiếu đến MapInitializer để initialize map sau khi load

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.cyan;

    private List<string> availableMaps = new List<string>();
    private string selectedMapPath = "";

    void Start()
    {
        LoadAvailableMaps();
    }

    /// <summary>
    /// Quét thư mục persistentDataPath để tìm các file map (.txt)
    /// </summary>
    void LoadAvailableMaps()
    {
        string mapFolder = Application.persistentDataPath;

        // Debug mapFolder
        Debug.Log($"Quét thư mục map: {mapFolder}");
        
        // Tìm tất cả file .txt bắt đầu bằng "Map_"
        if (Directory.Exists(mapFolder))
        {
            string[] files = Directory.GetFiles(mapFolder, "Map_*.txt");
            availableMaps = files.ToList();

            // Tạo UI buttons cho từng file
            foreach (string filePath in availableMaps)
            {
                CreateMapButton(filePath);
            }

            if (availableMaps.Count == 0)
            {
                Debug.LogWarning("Không tìm thấy file map nào! Vui lòng quét và tạo map trước.");
            }
            else
            {
                Debug.Log($"Tìm thấy {availableMaps.Count} file map.");
            }
        }
        else
        {
            Debug.LogError("Thư mục persistentDataPath không tồn tại!");
        }
    }

    /// <summary>
    /// Tạo nút button cho mỗi file map
    /// </summary>
    void CreateMapButton(string filePath)
    {
        GameObject btnObj = Instantiate(mapButtonPrefab, contentContainer);
        
        // Lấy tên file (không có đường dẫn)
        string fileName = Path.GetFileName(filePath);
        
        // Hiển thị tên file lên button
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.text = fileName.Replace("Map_", "").Replace(".txt", "");
        }

        // Gán sự kiện click
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() => OnMapSelected(filePath));
    }

    /// <summary>
    /// Khi user chọn một map file
    /// </summary>
    void OnMapSelected(string mapPath)
    {
        selectedMapPath = mapPath;
        Debug.Log($"Đã chọn map: {Path.GetFileName(mapPath)}");
        
        // Load map file đó vào MapGenerator
        LoadMapFromFile(mapPath);
        
        // Đóng panel chọn map
        if (mapSelectionPanel != null)
        {
            mapSelectionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Load map từ file thay vì từ TextAsset trong Resources
    /// </summary>
    void LoadMapFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File không tồn tại: {filePath}");
            return;
        }

        // Đọc nội dung file
        string mapContent = File.ReadAllText(filePath);
        
        // Tìm file JSON tương ứng (Map_Timestamp.json)
        string jsonPath = filePath.Replace(".txt", ".json");
        string jsonContent = "";
        
        if (File.Exists(jsonPath))
        {
            jsonContent = File.ReadAllText(jsonPath);
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy file JSON: {jsonPath}");
        }

        // Gọi hàm load map trong MapGenerator
        if (mapGenerator != null)
        {
            mapGenerator.LoadMapFromString(mapContent, jsonContent);
            // THÊM PHẦN NÀY - Sau khi load map xong, initialize position
            if (mapInitializer != null)
            {
                // Đợi 1 frame để map được generate xong
                StartCoroutine(InitializeMapAfterLoad());
            }
        }
        else
        {
            Debug.LogError("MapGenerator chưa được gán!");
        }
    }

    /// <summary>
    /// Hiển thị lại panel chọn map (để user có thể đổi map khác)
    /// </summary>
    public void ShowMapSelectionPanel()
    {
        if (mapSelectionPanel != null)
        {
            mapSelectionPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Refresh danh sách map (khi có map mới được tạo)
    /// </summary>
    public void RefreshMapList()
    {
        // Xóa các button cũ
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // Load lại
        availableMaps.Clear();
        LoadAvailableMaps();
    }

    /// <summary>
    /// Coroutine để đợi map load xong rồi mới initialize
    /// </summary>
    System.Collections.IEnumerator InitializeMapAfterLoad()
    {
        // Đợi 2 frames để đảm bảo NavMesh đã được bake
        yield return null;
        yield return null;
        
        Debug.Log("[MapFileSelector] Đang initialize map position...");
        mapInitializer.InitializeMapPosition();
    }
}
