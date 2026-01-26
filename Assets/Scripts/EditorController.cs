using UnityEngine;
using UnityEngine.InputSystem;

// Tự động thêm CharacterController nếu chưa có
[RequireComponent(typeof(CharacterController))]
public class EditorController : MonoBehaviour
{
#if UNITY_EDITOR
    public float moveSpeed = 5.0f;
    public float mouseSensitivity = 2.0f;
    public float gravity = 9.81f;

    private float rotationX = 0;
    private float rotationY = 0;
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        // Đặt kích thước controller nhỏ gọn để dễ đi
        characterController.radius = 0.3f; 
        characterController.height = 1.8f;
        characterController.center = new Vector3(0, 0.8f, 0);
    }

    void Update()
    {
        // 1. Xoay Camera
        if (Mouse.current != null && Mouse.current.rightButton.isPressed) // Giữ chuột phải
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            rotationX += mouseDelta.x * mouseSensitivity * 0.1f;
            rotationY -= mouseDelta.y * mouseSensitivity * 0.1f;
            rotationY = Mathf.Clamp(rotationY, -90, 90);
            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0);
        }

        // 2. Di chuyển có va chạm (Collision)
        float moveX = 0f;
        float moveZ = 0f;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed) moveX = 1f;
            if (Keyboard.current.wKey.isPressed) moveZ = 1f;
            if (Keyboard.current.sKey.isPressed) moveZ = -1f;
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
        // Thêm trọng lực nhẹ để luôn bám sàn (nếu muốn)
        move.y -= gravity; 

        // Dùng Move() thay vì position += để tính toán va chạm
        characterController.Move(move * moveSpeed * Time.deltaTime);
    }
#endif
}