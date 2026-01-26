using UnityEngine;
using UnityEngine.UI;
using TMPro; // Thư viện chữ đẹp
using UnityEngine.SceneManagement; // Để quay về menu

public class ScanUIController : MonoBehaviour
{
    [Header("Components")]
    public MapRecorder mapRecorder; // Kéo script MapRecorder vào đây
    public ScanVisualizer scanVisualizer; // Kéo script ScanVisualizer vào đây
    public TMP_InputField nameInput; // Kéo ô nhập tên
    public TMP_Text debugText;

    [Header("Buttons")]
    public Button btnStart;
    public Button btnSave;
    public Button btnBack; // (Tùy chọn) Nút quay về Main Menu

    void Start()
    {
        Application.logMessageReceived += HandleLog;
        
        // Kiểm tra MapRecorder
        if (mapRecorder == null)
        {
            mapRecorder = FindFirstObjectByType<MapRecorder>();
            if (mapRecorder == null)
            {
                Debug.LogError("KHÔNG TÌM THẤY MapRecorder! Hãy thêm MapRecorder vào Scene.");
            }
        }
        
        // Kiểm tra ScanVisualizer
        if (scanVisualizer == null)
        {
            scanVisualizer = FindFirstObjectByType<ScanVisualizer>();
            if (scanVisualizer != null)
            {
                Debug.Log("✓ ScanVisualizer tìm thấy - Split screen visualization enabled!");
            }
        }
        
        // 1. Gán sự kiện click
        if (btnStart != null)
            btnStart.onClick.AddListener(OnStartScan);
        if (btnSave != null)
            btnSave.onClick.AddListener(OnSaveMap);
        
        if (btnBack != null)
            btnBack.onClick.AddListener(OnBackToMenu);

        // 2. Trạng thái ban đầu
        if (btnStart != null)
            btnStart.gameObject.SetActive(true); // Hiện nút Start
        if (btnSave != null)
            btnSave.gameObject.SetActive(false); // Ẩn nút Save
        
        // Hiển thị thông tin debug
        if (debugText != null)
        {
            debugText.text = "Sẵn sàng quét map.\nBấm START để bắt đầu.";
        }
    }

    // Khi bấm BẮT ĐẦU
    void OnStartScan()
    {
        // Kiểm tra xem đã nhập tên chưa, nếu chưa thì đặt tên mặc định
        if (nameInput != null && string.IsNullOrEmpty(nameInput.text))
        {
            nameInput.text = "Map_" + System.DateTime.Now.ToString("MMdd_HHmm");
        }

        // Gọi lệnh bắt đầu ghi hình
        if (mapRecorder != null)
        {
            mapRecorder.StartRecording();
            
            // Reset visualization khi bắt đầu quét mới
            if (scanVisualizer != null)
            {
                scanVisualizer.ResetVisualization();
                Debug.Log("✓ Visualization reset - Ready to scan!");
            }
        }
        else
        {
            Debug.LogError("MapRecorder không tồn tại! Không thể bắt đầu quét.");
            return;
        }

        // Đổi trạng thái nút bấm
        if (btnStart != null)
            btnStart.gameObject.SetActive(false); // Ẩn Start đi
        if (btnSave != null)
            btnSave.gameObject.SetActive(true);   // Hiện Save lên
        if (nameInput != null)
            nameInput.interactable = false;       // Khóa ô nhập tên lại không cho sửa lúc đang quét
        
        // Cập nhật debug text
        if (debugText != null)
        {
            debugText.text = "ĐANG QUÉT...\nDi chuyển xung quanh để thu thập điểm.";
        }
    }

    // Khi bấm LƯU
    void OnSaveMap()
    {
        string mapName = nameInput != null ? nameInput.text : "Map_Default";
        
        if (string.IsNullOrEmpty(mapName))
        {
            mapName = "Map_" + System.DateTime.Now.ToString("MMdd_HHmm");
        }
        
        // Gọi lệnh lưu
        if (mapRecorder != null)
        {
            mapRecorder.StopRecordingAndSave(mapName);
            
            // Hiển thị thông báo thành công
            if (debugText != null)
            {
                string savePath = System.IO.Path.Combine(Application.persistentDataPath, mapName + ".txt");
                debugText.text = $"ĐÃ LƯU THÀNH CÔNG!\n{mapName}.txt\n\nĐường dẫn:\n{savePath}";
            }
        }
        else
        {
            Debug.LogError("MapRecorder không tồn tại! Không thể lưu map.");
            if (debugText != null)
            {
                debugText.text = "LỖI: Không thể lưu map!";
            }
        }

        // Reset trạng thái UI (hoặc hiện thông báo thành công)
        if (btnStart != null)
            btnStart.gameObject.SetActive(true);
        if (btnSave != null)
            btnSave.gameObject.SetActive(false);
        if (nameInput != null)
            nameInput.interactable = true;
        
        Debug.Log("Đã lưu map xong! Bạn có thể quay về menu hoặc quét map mới.");
    }

    void OnBackToMenu()
    {
        // Nhớ đổi "MainMenuScene" thành tên scene menu thật của bạn
        SceneManager.LoadScene("MainMenuScene"); 
    }

    void OnDestroy() {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        if (debugText == null) return;
        
        if (type == LogType.Error || type == LogType.Exception) {
            debugText.text = "LỖI: " + logString;
        }
        else if (type == LogType.Warning)
        {
            debugText.text = "CẢNH BÁO: " + logString;
        }
    }
}