using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.AI.Navigation; // Cần cài package AI Navigation

// Class để đọc JSON
[System.Serializable]
public class GridInfoData
{
    public List<DestinationInfo> destinations;
}

[System.Serializable]
public class DestinationInfo
{
    public int id;
    public string info;
    public string description;
    // Các trường khác nếu cần
}

public class MapGenerator : MonoBehaviour
{
    [Header("Files")]
    public TextAsset outputTxtFile; // Kéo file output.txt vào đây
    public TextAsset gridInfoJsonFile; // Kéo file GridMap_Info.json vào đây

    [Header("Settings")]
    public GameObject wallPrefab;
    public Transform mapContainer; // Tạo một Empty Object để chứa toàn bộ map
    public NavMeshSurface navMeshSurface; // Kéo component NavMeshSurface vào
    public static MapGenerator Instance { get; private set; }

    // Dictionary để lưu vị trí các phòng: ID -> Tọa độ World
    public Dictionary<int, Vector3> locationDatabase = new Dictionary<int, Vector3>();
    public Dictionary<int, DestinationInfo> infoDatabase = new Dictionary<int, DestinationInfo>();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // Chỉ generate map nếu có file được gán sẵn (dùng cho test)
        // Khi dùng MapFileSelector, sẽ gọi LoadMapFromString() thay vì Start()
        if (outputTxtFile != null && gridInfoJsonFile != null)
        {
            // 1. Đọc thông tin phòng từ JSON trước
            LoadJsonInfo();

            // 2. Tạo Map từ file Text
            GenerateMap();

            // 3. Bake NavMesh tự động
            BakeNavMesh();
        }
        else
        {
            Debug.Log("Đang chờ MapFileSelector load map...");
        }
    }
    
    /// <summary>
    /// Load map từ string content (được gọi bởi MapFileSelector)
    /// </summary>
    public void LoadMapFromString(string mapContent, string jsonContent)
    {
        // 1. Load JSON info nếu có
        if (!string.IsNullOrEmpty(jsonContent))
        {
            LoadJsonInfoFromString(jsonContent);
        }
        
        // 2. Generate map từ string
        GenerateMapFromString(mapContent);
        
        // 3. Bake NavMesh
        BakeNavMesh();
        
        Debug.Log("Map loaded successfully from file!");
    }
    
    void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh Baked Successfully!");
        }
    }

    void LoadJsonInfo()
    {
        if (gridInfoJsonFile == null) return;
        LoadJsonInfoFromString(gridInfoJsonFile.text);
    }
    
    void LoadJsonInfoFromString(string jsonText)
    {
        GridInfoData data = JsonUtility.FromJson<GridInfoData>(jsonText);
        infoDatabase.Clear(); // Clear old data
        
        foreach (var dest in data.destinations)
        {
            if (!infoDatabase.ContainsKey(dest.id))
            {
                infoDatabase.Add(dest.id, dest);
            }
        }
    }

    void GenerateMap()
    {
        if (outputTxtFile == null) return;
        GenerateMapFromString(outputTxtFile.text);
    }
    
    void GenerateMapFromString(string mapText)
    {
        // Xóa map cũ nếu có (để tránh chồng chéo khi chạy lại)
        foreach (Transform child in mapContainer) {
            Destroy(child.gameObject);
        }
        
        // Clear database cũ
        locationDatabase.Clear();

        // ===== THÊM DÒNG NÀY ĐỂ FIX LỖI HEIGHT =====
        // Reset position của mapContainer về (0,0,0) trước khi generate map mới
        if (mapContainer != null)
        {
            mapContainer.position = Vector3.zero;
            Debug.Log("[MapGenerator] Reset map container position to (0,0,0)");
        }
        // ============================================

        string[] lines = mapText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Parse Header
        float cellSize = float.Parse(lines[0].Trim()) / 100f; // 25 -> 0.25m
        string[] dims = lines[1].Trim().Split(' ');
        int width = int.Parse(dims[0]);  // Số cột (X)
        int height = int.Parse(dims[1]); // Số hàng (Z)

        // Tạo một mặt sàn (Ground) duy nhất
        // Thay vì tạo hàng ngàn ô đất nhỏ, ta tạo 1 Plane lớn làm nền
        CreateFloorPlane(width, height, cellSize);

        // Duyệt dữ liệu map
        // Bắt đầu từ dòng thứ 2 (index 2) trong file text
        int fileRowIndex = 0; 
        for (int i = 2; i < lines.Length; i++)
        {
            string lineData = lines[i].Trim();
            if (string.IsNullOrEmpty(lineData)) continue;

            string[] cells = lineData.Split(' ');
            
            for (int col = 0; col < cells.Length; col++)
            {
                if (string.IsNullOrEmpty(cells[col])) continue;

                if (int.TryParse(cells[col], out int cellValue))
                {
                    // TÍNH TOÁN VỊ TRÍ (QUAN TRỌNG)
                    // fileRowIndex tương ứng với trục Z
                    // col tương ứng với trục X
                    
                    float xPos = col * cellSize;
                    
                    // Unity Z tăng dần về phía trước.
                    // Để map không bị ngược, ta nhân fileRowIndex.
                    float zPos = fileRowIndex * cellSize; 

                    // 1. Xử lý TƯỜNG (Obstacle)
                    if (cellValue == 1)
                    {
                        // Giả sử tường cao 2m
                        float wallHeight = 2.0f; 
                        
                        // Vị trí tâm của Cube: Y phải bằng một nửa chiều cao để đáy nằm trên mặt sàn (0)
                        Vector3 position = new Vector3(xPos, wallHeight / 2, zPos);
                        
                        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
                        
                        // Set kích thước tường khớp với ô lưới
                        wall.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
                        wall.transform.SetParent(mapContainer);
                        wall.name = $"Wall_{col}_{fileRowIndex}";
                    }
                    // 2. Xử lý ĐÍCH ĐẾN (Destination)
                    else if (cellValue > 1)
                    {
                        Vector3 destPos = new Vector3(xPos, 0, zPos);
                        if (!locationDatabase.ContainsKey(cellValue))
                        {
                            locationDatabase.Add(cellValue, destPos);
                            
                            // Tạo cột mốc ảo để debug
                            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            marker.transform.position = new Vector3(xPos, 0.5f, zPos); // Cao 0.5m
                            marker.transform.localScale = new Vector3(cellSize, 1, cellSize);
                            marker.name = $"Dest_{cellValue}";
                            marker.transform.SetParent(mapContainer);
                            
                            // Tô màu khác cho dễ nhìn
                            marker.GetComponent<Renderer>().material.color = Color.green;
                        }
                    }
                }
            }
            fileRowIndex++;
        }
    }

    // Hàm tạo mặt sàn lớn
    void CreateFloorPlane(int gridWidth, int gridHeight, float cellSize)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "FloorBase";
        floor.transform.SetParent(mapContainer);
        
        // Plane mặc định của Unity là 10x10 đơn vị.
        // Ta cần scale nó để bao phủ hết map.
        // Map width = gridWidth * cellSize.
        float mapRealWidth = gridWidth * cellSize;
        float mapRealHeight = gridHeight * cellSize;

        // Tính scale cho Plane (chia cho 10 vì size gốc là 10)
        float scaleX = mapRealWidth / 10f;
        float scaleZ = mapRealHeight / 10f;
        
        floor.transform.localScale = new Vector3(scaleX, 1, scaleZ);
        
        // Đặt vị trí Plane vào giữa map
        floor.transform.position = new Vector3(mapRealWidth / 2, 0, mapRealHeight / 2);
        
        // Gán layer hoặc material nếu cần (để NavMesh hiểu là Walkable)
    }
    void ProcessCell(int value, Vector3 pos)
    {
        // 0: Đường đi (Không làm gì)
        if (value == 0) return;

        // 1: Tường
        if (value == 1)
        {
            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
            wall.transform.SetParent(mapContainer);
            // Nâng tường lên một chút vì pivot cube thường ở giữa
            wall.transform.localPosition += Vector3.up * 1.0f; 
        }
        // > 1: Các địa điểm (101, 102...)
        else if (value > 1)
        {
            // Lưu tọa độ vào Database để sau này tìm đường
            if (!locationDatabase.ContainsKey(value))
            {
                locationDatabase.Add(value, pos);
                
                // Debug xem tìm thấy phòng nào
                string roomName = infoDatabase.ContainsKey(value) ? infoDatabase[value].info : "Unknown Room";
                Debug.Log($"Found {roomName} (ID: {value}) at {pos}");
                
                // (Tùy chọn) Đặt một cái mốc ảo ở đây để debug
                // GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // marker.transform.position = pos;
                // marker.name = roomName;
            }
        }
    }
    
    // Hàm public để gọi từ UI khi người dùng chọn phòng
    public Vector3 GetRoomPosition(int roomId)
    {
        if(locationDatabase.ContainsKey(roomId))
            return locationDatabase[roomId];
        return Vector3.zero;
    }
}