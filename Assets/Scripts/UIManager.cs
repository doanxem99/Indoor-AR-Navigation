using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Thư viện cho chữ đẹp
using System.IO;
using UnityEngine.SceneManagement; // Để quay về menu

// Class để đọc dữ liệu từ JSON
[System.Serializable]
public class LocationInfo
{
    public int id;
    public string info; // Tên phòng (VD: Phòng học nhóm)
    public string description;
}

[System.Serializable]
public class LocationDataList
{
    public LocationInfo[] destinations;
}

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject locationMenuPanel; // Cái Scroll View
    public Transform contentContainer;   // Cái object "Content" trong Scroll View
    public GameObject buttonPrefab;      // Cái Button Template
    public NavigationController navigationController;
    public TextAsset jsonFile;           // Kéo file GridMap_Info.json vào đây

    [Header("UI Elements")]
    public Button toggleMenuButton;      // Nút bật tắt menu
    public Button stopNavigationButton;  // Nút hủy dẫn đường
    public Button btnBack;

    private bool isMenuOpen = true;

    void Start()
    {
        LoadLocations();
        toggleMenuButton.onClick.AddListener(ToggleMenu);
        
        if(stopNavigationButton != null)
        {
            stopNavigationButton.onClick.AddListener(StopNav);
            // Mặc định ẩn nút Stop khi mới vào game
            stopNavigationButton.gameObject.SetActive(false); 
        }

        if (btnBack != null)
            btnBack.onClick.AddListener(OnBackToMenu);

        SetMenuState(false);
    }

    void LoadLocations()
    {
        if (jsonFile == null)
        {
            Debug.LogError("Chưa gán file JSON vào UIManager!");
            return;
        }

        // Parse JSON
        LocationDataList data = JsonUtility.FromJson<LocationDataList>(jsonFile.text);

        // Xóa các nút cũ nếu có
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // Tạo nút mới
        foreach (LocationInfo loc in data.destinations)
        {
            GameObject btnObj = Instantiate(buttonPrefab, contentContainer);
            
            // Tìm component TextMeshPro trong nút để đổi tên
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = loc.info; // Hiển thị tên phòng (VD: Phòng học nhóm)
            }

            // Gán sự kiện click
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnLocationSelected(loc.id));
        }
    }

    void OnLocationSelected(int id)
    {
        navigationController.StartNavigation(id);
        SetMenuState(false); // Ẩn menu
        
        // HIỆN nút Stop lên
        if(stopNavigationButton != null) 
            stopNavigationButton.gameObject.SetActive(true);
    }

    void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        SetMenuState(isMenuOpen);
    }

    void SetMenuState(bool isOpen)
    {
        locationMenuPanel.SetActive(isOpen);
        isMenuOpen = isOpen;
        
        // Đổi text nút toggle (tùy chọn)
        TMP_Text toggleText = toggleMenuButton.GetComponentInChildren<TMP_Text>();
        if(toggleText != null) toggleText.text = isOpen ? "ĐÓNG MENU" : "CHỌN ĐỊA ĐIỂM";
    }

    void StopNav()
    {
        navigationController.StopNavigation();
        
        // ẨN nút Stop đi sau khi hủy
        if(stopNavigationButton != null) 
            stopNavigationButton.gameObject.SetActive(false);
            
        // (Tùy chọn) Hiện lại menu để chọn cái khác
        SetMenuState(true); 
    }

    void OnBackToMenu()
    {
        // Nhớ đổi "MainMenuScene" thành tên scene menu thật của bạn
        SceneManager.LoadScene("MainMenuScene"); 
    }
}