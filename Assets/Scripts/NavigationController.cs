using UnityEngine;
using UnityEngine.AI;

public class NavigationController : MonoBehaviour
{
    [Header("References")]
    public Transform userTransform;
    public MapGenerator mapGenerator;
    public PathVisualizer pathVisualizer; // Tham chiếu đến script mới

    private NavMeshPath navMeshPath;
    private bool isNavigating = false;
    private Vector3 currentTarget;

    void Start()
    {
        navMeshPath = new NavMeshPath();
    }

    void Update()
    {
        if (isNavigating)
        {
            CalculateAndDrawPath();
        }

        // Test Input
        if (Input.GetKeyDown(KeyCode.T)) StartNavigation(102);
        if (Input.GetKeyDown(KeyCode.Y)) StartNavigation(101);
    }

    public void StartNavigation(int destinationID)
    {
        Debug.Log($"[NavigationController] StartNavigation called for ID: {destinationID}");
        
        if (mapGenerator.locationDatabase.ContainsKey(destinationID))
        {
            currentTarget = mapGenerator.locationDatabase[destinationID];
            isNavigating = true;
            Debug.Log($"[NavigationController] Target set to: {currentTarget}");
        }
        else
        {
            Debug.LogError($"[NavigationController] ID {destinationID} không tồn tại trong database!");
            Debug.LogError($"Available IDs: {string.Join(", ", mapGenerator.locationDatabase.Keys)}");
        }
    }

    public void StopNavigation()
    {
        Debug.Log("[NavigationController] Navigation stopped");
        isNavigating = false;
        pathVisualizer.ClearPath();
    }

    void CalculateAndDrawPath()
    {
        // 1. Xử lý vị trí người dùng (User Position)
        Vector3 rawStartPos = userTransform.position;
        NavMeshHit hitStart;
        Vector3 finalStartPos = rawStartPos;

        // Tìm điểm gần nhất trên NavMesh trong bán kính 2.0m quanh người dùng
        if (NavMesh.SamplePosition(rawStartPos, out hitStart, 2.0f, NavMesh.AllAreas))
        {
            finalStartPos = hitStart.position;
        }
        else
        {
            Debug.LogError("User đang đứng quá xa vùng NavMesh (màu xanh)!");
            return;
        }

        // 2. Xử lý vị trí đích (Target Position)
        NavMeshHit hitTarget;
        Vector3 finalTargetPos = currentTarget;

        // Tìm điểm gần nhất trên NavMesh trong bán kính 2.0m quanh đích đến
        if (NavMesh.SamplePosition(currentTarget, out hitTarget, 2.0f, NavMesh.AllAreas))
        {
            finalTargetPos = hitTarget.position;
        }
        else
        {
            Debug.LogError("Đích đến nằm ngoài vùng NavMesh hoặc bị cô lập!");
            return;
        }

        // Tính đường
        if (NavMesh.CalculatePath(finalStartPos, finalTargetPos, NavMesh.AllAreas, navMeshPath))
        {
            if (navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                Debug.Log($"[NavigationController] Path calculated with {navMeshPath.corners.Length} corners");
                // Gửi danh sách điểm sang cho Visualizer vẽ
                pathVisualizer.DrawPath(navMeshPath.corners);
            }
            else {
                Debug.LogWarning($"[NavigationController] Path status: {navMeshPath.status}");
            }
        }
        else
        {
            Debug.LogError("[NavigationController] NavMesh.CalculatePath failed!");
        }
    }
}