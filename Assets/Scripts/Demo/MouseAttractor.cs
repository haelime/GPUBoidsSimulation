// ============================================================================
// MouseAttractor.cs
// 
// Mouse-based boid attraction/repulsion for interactive demo
// ============================================================================

using UnityEngine;

public class MouseAttractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GPUBoids gpuBoids;
    [SerializeField] private Camera mainCamera;
    
    [Header("Attractor Settings")]
    [SerializeField] private float attractStrength = 5f;
    [SerializeField] private float repelStrength = -8f;
    [SerializeField] private float planeDistance = 30f;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject attractorIndicator;
    
    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (gpuBoids == null)
        {
            gpuBoids = FindFirstObjectByType<GPUBoids>();
        }
    }
    
    private void Update()
    {
        bool leftClick = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);
        
        if (leftClick || rightClick)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            
            gpuBoids.AttractorPosition = worldPos;
            gpuBoids.AttractorStrength = leftClick ? attractStrength : repelStrength;
            gpuBoids.AttractorEnabled = true;
            
            UpdateIndicator(worldPos, leftClick);
        }
        else
        {
            gpuBoids.AttractorEnabled = false;
            HideIndicator();
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint = gpuBoids.WallCenter;
        
        Plane plane = new Plane(-mainCamera.transform.forward, targetPoint);
        
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return ray.GetPoint(planeDistance);
    }
    
    private void UpdateIndicator(Vector3 position, bool isAttract)
    {
        if (attractorIndicator != null)
        {
            attractorIndicator.SetActive(true);
            attractorIndicator.transform.position = position;
            
            var renderer = attractorIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = isAttract ? Color.green : Color.red;
            }
        }
    }
    
    private void HideIndicator()
    {
        if (attractorIndicator != null)
        {
            attractorIndicator.SetActive(false);
        }
    }
}
