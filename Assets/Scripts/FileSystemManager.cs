using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class FileSystemManager : MonoBehaviour
{
    // Singleton để dễ gọi từ bất kỳ đâu
    public static FileSystemManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ script này sống qua các Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Hàm lấy danh sách tất cả file Map đã lưu
    public List<string> GetAllMapNames()
    {
        List<string> mapNames = new List<string>();
        string path = Application.persistentDataPath;
        
        // Lấy tất cả file .txt
        DirectoryInfo d = new DirectoryInfo(path);
        foreach (var file in d.GetFiles("*.txt"))
        {
            // Tên map là tên file bỏ đuôi .txt
            mapNames.Add(Path.GetFileNameWithoutExtension(file.Name));
        }
        return mapNames;
    }

    // 1. LƯU MAP (Grid Matrix)
    public void SaveMapData(string mapName, string content)
    {
        string path = Path.Combine(Application.persistentDataPath, mapName + ".txt");
        
        try
        {
            File.WriteAllText(path, content);
            Debug.Log("========================================");
            Debug.Log($"✓ ĐÃ LƯU MAP DATA THÀNH CÔNG");
            Debug.Log($"Tên file: {mapName}.txt");
            Debug.Log($"Đường dẫn: {path}");
            Debug.Log($"Kích thước: {content.Length} bytes");
            Debug.Log("========================================");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LỖI khi lưu Map Data: {e.Message}");
        }
    }

    // 2. LƯU INFO (Json)
    public void SaveMapInfo(string mapName, string jsonContent)
    {
        string path = Path.Combine(Application.persistentDataPath, mapName + ".json");
        
        try
        {
            File.WriteAllText(path, jsonContent);
            Debug.Log($"✓ Đã lưu Map Info tại: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LỖI khi lưu Map Info: {e.Message}");
        }
    }

    // 3. ĐỌC MAP
    public string LoadMapData(string mapName)
    {
        string path = Path.Combine(Application.persistentDataPath, mapName + ".txt");
        if (File.Exists(path))
        {
            string content = File.ReadAllText(path);
            Debug.Log($"✓ Đã load Map Data: {mapName}.txt ({content.Length} bytes)");
            return content;
        }
        Debug.LogError($"✗ Không tìm thấy file Map: {path}");
        return null;
    }

    // 4. ĐỌC INFO
    public string LoadMapInfo(string mapName)
    {
        string path = Path.Combine(Application.persistentDataPath, mapName + ".json");
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        return null; // Có thể map này chưa có info
    }
}