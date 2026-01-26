using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc để chuyển scene
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnScanMap;
    public Button btnNavigation;
    public Button btnExit;

    [Header("Scene Names")]
    // ĐIỀN ĐÚNG TÊN SCENE CỦA BẠN VÀO ĐÂY
    public string scanSceneName = "MapCreatorScene"; 
    public string navigationSceneName = "SampleScene"; // Hoặc tên Scene dẫn đường cũ của bạn

    void Start()
    {
        // Gán sự kiện cho các nút
        btnScanMap.onClick.AddListener(GoToScan);
        btnNavigation.onClick.AddListener(GoToNavigation);
        
        if(btnExit != null)
            btnExit.onClick.AddListener(ExitApp);
    }

    void GoToScan()
    {
        SceneManager.LoadScene(scanSceneName);
    }

    void GoToNavigation()
    {
        SceneManager.LoadScene(navigationSceneName);
    }

    void ExitApp()
    {
        Debug.Log("Thoát ứng dụng");
        Application.Quit();
    }
}