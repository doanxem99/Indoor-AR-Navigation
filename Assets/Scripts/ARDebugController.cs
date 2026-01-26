using UnityEngine;
using UnityEngine.InputSystem.XR; // Dành cho Tracked Pose Driver mới
using UnityEngine.SpatialTracking; // Dành cho Tracked Pose Driver cũ (nếu có)

public class ARDebugController : MonoBehaviour
{
    // Kéo component Tracked Pose Driver vào đây trong Inspector
    public MonoBehaviour trackedPoseDriver; 
    public EditorController editorController; // Script di chuyển WASD của bạn

    void Awake()
    {
        // Kiểm tra xem đang chạy trên nền tảng nào
#if UNITY_EDITOR
        // Nếu là Editor (PC): Tắt AR Driver, Bật bàn phím
        if (trackedPoseDriver != null) trackedPoseDriver.enabled = false;
        if (editorController != null) editorController.enabled = true;
        Debug.Log("Mode: EDITOR - Keyboard Enabled, AR Disabled");
#else
        // Nếu là Điện thoại: Bật AR Driver, Tắt bàn phím
        if (trackedPoseDriver != null) trackedPoseDriver.enabled = true;
        if (editorController != null) editorController.enabled = false;
        Debug.Log("Mode: MOBILE - AR Enabled, Keyboard Disabled");
#endif
    }
}