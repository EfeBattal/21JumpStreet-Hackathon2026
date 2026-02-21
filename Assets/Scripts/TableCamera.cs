using UnityEngine;
using UnityEngine.InputSystem; 

public class TableCamera : MonoBehaviour
{
    [Header("Look Settings")]
    private float sensitivity = 0.4f; 

    [Header("Viewing Limits")]
    private float minXAngle = 0f;  
    private float maxXAngle = 90f;  
    private float minYAngle = 200f; 
    private float maxYAngle = 340f;  

    private float currentX = 0f;
    private float currentY = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        currentX = angles.x > 180 ? angles.x - 360 : angles.x;
        currentY = angles.y < 180 ? 360 - angles.y : angles.y;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float mouseX = mouseDelta.x * sensitivity;
            float mouseY = mouseDelta.y * sensitivity;

            currentY += mouseX;
            currentX -= mouseY;

            currentX = Mathf.Clamp(currentX, minXAngle, maxXAngle);
            currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

            transform.rotation = Quaternion.Euler(currentX, currentY, 0);
        }
    }
}