// ============================================================================
// CameraController.cs
// 
// Orbital camera controller for demo visualization
// ============================================================================

using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = Vector3.zero;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float defaultDistance = 30f;
    
    private float _currentDistance;
    private float _horizontalAngle;
    private float _verticalAngle = 30f;
    
    private void Start()
    {
        _currentDistance = defaultDistance;
        
        if (target == null)
        {
            var boids = FindFirstObjectByType<GPUBoids>();
            if (boids != null)
            {
                targetOffset = boids.WallCenter;
            }
        }
        
        UpdateCameraPosition();
    }
    
    private void LateUpdate()
    {
        HandleRotationInput();
        HandleZoomInput();
        UpdateCameraPosition();
    }
    
    private void HandleRotationInput()
    {
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            _horizontalAngle += mouseX * rotationSpeed;
            _verticalAngle -= mouseY * rotationSpeed;
            _verticalAngle = Mathf.Clamp(_verticalAngle, minVerticalAngle, maxVerticalAngle);
        }
    }
    
    private void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _currentDistance -= scroll * zoomSpeed;
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
        }
    }
    
    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = target != null ? target.position + targetOffset : targetOffset;
        
        Quaternion rotation = Quaternion.Euler(_verticalAngle, _horizontalAngle, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -_currentDistance);
        
        transform.position = targetPosition + offset;
        transform.LookAt(targetPosition);
    }
    
    public void SetTarget(Vector3 center)
    {
        targetOffset = center;
    }
}
