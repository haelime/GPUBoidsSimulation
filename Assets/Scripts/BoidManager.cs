// ============================================================================
// BoidManager.cs
// 
// Boid Spawner and Lifetime Manager
// 
// Technical Highlights:
// - Prefab-based instantiation with configurable population size
// - Spherical spawn distribution for natural group formation
// - Hierarchical organization under manager transform
// ============================================================================

using UnityEngine;

/// <summary>
/// Factory component for spawning and managing a population of CPU-based boids.
/// Provides configurable spawn parameters for different use cases.
/// </summary>
/// <remarks>
/// For GPU-based simulation (16,000+ entities), use GPUBoids instead.
/// This manager is suited for physics-interactive flocks of 50-200 entities.
/// </remarks>
public class BoidManager : MonoBehaviour
{
    #region Spawn Configuration
    
    [Header("Boid Prefab")]
    [SerializeField]
    [Tooltip("Prefab containing BoidController component to instantiate")]
    private BoidController boidPrefab;
    
    [Header("Population Settings")]
    [SerializeField]
    [Tooltip("Number of boids to spawn on Start")]
    private int boidCount = 100;
    
    [SerializeField]
    [Tooltip("Radius of spherical spawn area centered on this transform")]
    private float spawnRadius = 10f;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        SpawnBoids();
    }
    
    #endregion
    
    #region Spawning Logic
    
    /// <summary>
    /// Instantiates all boids within a spherical volume.
    /// Boids are parented to this manager for scene organization.
    /// </summary>
    private void SpawnBoids()
    {
        if (boidPrefab == null)
        {
            Debug.LogError($"[{nameof(BoidManager)}] Boid prefab is not assigned!");
            return;
        }
        
        for (int i = 0; i < boidCount; i++)
        {
            Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
            Instantiate(boidPrefab, spawnPosition, Quaternion.identity, transform);
        }
    }
    
    #endregion
    
    #region Editor Visualization
    
    /// <summary>
    /// Draws the spawn radius in the Scene view for level design purposes.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
    
    #endregion
}
