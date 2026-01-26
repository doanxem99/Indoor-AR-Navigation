using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation; // Cần thiết cho AR
using UnityEngine.XR.ARSubsystems;

public class ARLocationSync : MonoBehaviour
{
    [Header("Components")]
    public ARTrackedImageManager trackedImageManager;
    public MapGenerator mapGenerator;
    public Transform xrOrigin; // Kéo object XR Origin vào đây (Cha của Camera)

    [Header("Settings")]
    public bool syncRotation = true; // Có đồng bộ hướng xoay không? (Nên có)

    private bool isSynced = false;

    void OnEnable()
    {
        // Đăng ký sự kiện nhận diện ảnh
        trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // Chỉ cần lấy ảnh đầu tiên phát hiện được (added) hoặc vừa cập nhật (updated)
        foreach (var trackedImage in eventArgs.added)
        {
            SyncPosition(trackedImage);
        }
        
        // Nếu muốn liên tục cập nhật vị trí khi người dùng còn soi vào mã QR thì uncomment dòng dưới
        // foreach (var trackedImage in eventArgs.updated) { SyncPosition(trackedImage); }
    }

    void SyncPosition(ARTrackedImage trackedImage)
    {
        // Nếu đã sync rồi thì thôi (để tránh bị giật hình liên tục), trừ khi muốn nút Reset
        if (isSynced && trackedImage.trackingState != TrackingState.Tracking) return;

        // 1. Lấy tên của ảnh (Ví dụ tên ảnh là "100" tương ứng ID trong map)
        string imageName = trackedImage.referenceImage.name;
        
        if (int.TryParse(imageName, out int locationID))
        {
            // 2. Tìm tọa độ thật của ID này trong Map ảo
            if (mapGenerator.locationDatabase.ContainsKey(locationID))
            {
                Vector3 targetMapPosition = mapGenerator.locationDatabase[locationID];
                
                // 3. Tính toán độ lệch (Offset)
                // Logic: Vị trí Camera hiện tại + Offset = Vị trí Map
                // => Offset = Vị trí Map - Vị trí Camera nhìn thấy ảnh
                
                // Tuy nhiên, cách dễ nhất trong AR Foundation là dời XR Origin.
                // Ta muốn: trackedImage.transform.position (Thế giới thực) === targetMapPosition (Thế giới ảo)
                
                PerformSync(trackedImage.transform, targetMapPosition);
                
                isSynced = true;
                Debug.Log($"<color=green>Đã đồng bộ vị trí tại mốc ID: {locationID}</color>");
            }
        }
    }

    // Hàm giả lập Sync cho Editor debug
    public void DebugSync(int id)
    {
         if (mapGenerator.locationDatabase.ContainsKey(id))
         {
             // Giả vờ Camera đang đứng ngay tại mốc đó
             Vector3 targetPos = mapGenerator.locationDatabase[id];
             
             // Dời XR Origin sao cho Camera về đúng chỗ đó
             Vector3 currentCamPos = Camera.main.transform.position;
             Vector3 offset = targetPos - currentCamPos;
             
             // Dịch chuyển XR Origin (lưu ý giữ nguyên Y nếu muốn giữ độ cao sàn)
             xrOrigin.position += new Vector3(offset.x, 0, offset.z);
             
             Debug.Log($"Debug Sync thành công tới ID {id}");
         }
    }

    private void PerformSync(Transform imageMarkerTransform, Vector3 virtualTargetPos)
    {
        // Bước A: Xoay bản đồ cho đúng hướng (Heading)
        if (syncRotation)
        {
            // Giả sử mã QR dán trên tường, hướng ra ngoài.
            // Trong Map ảo, vị trí đó cũng phải có hướng tương ứng.
            // (Phần này nâng cao, tạm thời ta giả định mã QR dán SÀN và hướng Bắc QR trùng Bắc Bản đồ)
            
            // Lấy độ lệch góc xoay giữa Camera đang nhìn và hướng Bắc của Map
            float rotationOffset = virtualTargetPos.y - imageMarkerTransform.eulerAngles.y;
            // Xoay XR Origin (Cần tính toán kỹ hơn nếu dùng mã QR dán tường)
        }

        // Bước B: Dời vị trí (Position)
        // Đây là thuật toán "World Alignment" chuẩn:
        
        // 1. Làm cho XR Origin trở thành con của Marker tạm thời
        // (Mẹo tư duy: Coi Marker là gốc tọa độ)
        Vector3 cameraOffsetFromMarker = xrOrigin.position - imageMarkerTransform.position;
        
        // 2. Đặt XR Origin vào vị trí mới
        xrOrigin.position = virtualTargetPos + cameraOffsetFromMarker;

        // Lưu ý: Nếu map của bạn có Y=0 là sàn, mà AR nhận diện Y marker lung tung
        // Thì cần ép Y của XR Origin sao cho sàn AR trùng sàn ảo.
        float heightCorrection = 0.0f - xrOrigin.position.y; // 0.0f là độ cao sàn ảo
        xrOrigin.position += Vector3.up * heightCorrection;
    }
}