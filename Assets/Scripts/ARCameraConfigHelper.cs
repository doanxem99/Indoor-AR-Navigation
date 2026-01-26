using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARCameraConfigHelper : MonoBehaviour
{
    void Start()
    {
        var arCameraManager = FindFirstObjectByType<ARCameraManager>();
        if (arCameraManager != null)
        {
            // Giảm resolution để tương thích với Mali GPU
            arCameraManager.requestedFacingDirection = UnityEngine.XR.ARFoundation.CameraFacingDirection.World;
            
            // Disable auto focus nếu gây vấn đề
            arCameraManager.autoFocusRequested = true;
        }
    }
}