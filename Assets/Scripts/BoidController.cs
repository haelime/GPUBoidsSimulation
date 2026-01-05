// ============================================================================
// BoidController.cs
// 
// CPU-Based Boid Flocking Behavior for Individual Agents
// 
// Technical Highlights:
// - Physics-based neighbor detection using OverlapSphere
// - Three classic boid behaviors: Separation, Alignment, Cohesion
// - Rigidbody velocity integration for smooth movement
// - Configurable weights for behavior tuning
// ============================================================================

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CPU-based boid controller implementing flocking behavior via physics simulation.
/// Each boid detects neighbors and calculates steering forces for natural group movement.
/// </summary>
/// <remarks>
/// Complexity: O(n) per boid for neighbor detection using Physics.OverlapSphere.
/// Recommended for groups of 50-100 boids with physics interactions.
/// For larger crowds without physics needs, use GPUBoids instead.
/// </remarks>
[RequireComponent(typeof(Rigidbody))]
public class BoidController : MonoBehaviour
{
    #region Movement Parameters
    
    [Header("Speed Settings")]
    [SerializeField] 
    [Tooltip("Maximum velocity magnitude")]
    private float maxSpeed = 5.0f;
    
    [SerializeField] 
    [Tooltip("Initial velocity applied on spawn")]
    private float startSpeed = 5.0f;
    
    #endregion
    
    #region Flocking Parameters
    
    [Header("Neighbor Detection")]
    [SerializeField] 
    [Tooltip("Radius for detecting neighboring boids")]
    private float neighborRadius = 3.0f;
    
    [Header("Behavior Weights")]
    [SerializeField] 
    [Tooltip("Separation: Avoidance force to prevent crowding")]
    private float separationWeight = 1.5f;
    
    [SerializeField] 
    [Tooltip("Alignment: Tendency to match neighbors' velocity direction")]
    private float alignmentWeight = 1.0f;
    
    [SerializeField] 
    [Tooltip("Cohesion: Attraction toward the center of nearby boids")]
    private float cohesionWeight = 1.0f;
    
    #endregion
    
    #region Cached Components
    
    private Rigidbody _rigidbody;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        ApplyInitialVelocity();
    }

    private void FixedUpdate()
    {
        var neighbors = FindNeighbors();
        var steeringForce = CalculateSteeringForce(neighbors);
        ApplySteeringForce(steeringForce);
        AlignRotationToVelocity();
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Applies initial velocity to start the boid moving.
    /// </summary>
    private void ApplyInitialVelocity()
    {
        _rigidbody.AddForce(new Vector3(startSpeed, startSpeed, 0f));
    }
    
    #endregion
    
    #region Neighbor Detection
    
    /// <summary>
    /// Finds all neighboring boids within detection radius using physics overlap.
    /// </summary>
    /// <returns>List of nearby BoidController instances (excluding self)</returns>
    private List<BoidController> FindNeighbors()
    {
        var neighbors = new List<BoidController>();
        Collider[] hits = Physics.OverlapSphere(transform.position, neighborRadius);

        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject) 
                continue;
                
            var otherBoid = hit.GetComponent<BoidController>();
            if (otherBoid != null)
            {
                neighbors.Add(otherBoid);
            }
        }

        return neighbors;
    }
    
    #endregion
    
    #region Steering Calculation
    
    /// <summary>
    /// Combines all three flocking behaviors into a single steering force.
    /// </summary>
    private Vector3 CalculateSteeringForce(List<BoidController> neighbors)
    {
        Vector3 separation = ComputeSeparation(neighbors) * separationWeight;
        Vector3 alignment = ComputeAlignment(neighbors) * alignmentWeight;
        Vector3 cohesion = ComputeCohesion(neighbors) * cohesionWeight;

        return separation + alignment + cohesion;
    }
    
    /// <summary>
    /// Applies steering force to velocity with speed clamping.
    /// </summary>
    private void ApplySteeringForce(Vector3 acceleration)
    {
        Vector3 newVelocity = _rigidbody.linearVelocity + acceleration * Time.fixedDeltaTime;
        _rigidbody.linearVelocity = Vector3.ClampMagnitude(newVelocity, maxSpeed);
    }
    
    /// <summary>
    /// Rotates the boid to face its movement direction.
    /// </summary>
    private void AlignRotationToVelocity()
    {
        const float minVelocityThreshold = 0.01f;
        
        if (_rigidbody.linearVelocity.sqrMagnitude > minVelocityThreshold)
        {
            transform.rotation = Quaternion.LookRotation(_rigidbody.linearVelocity);
        }
    }
    
    #endregion
    
    #region Boid Behaviors
    
    /// <summary>
    /// Separation: Steers away from nearby neighbors to avoid crowding.
    /// Force magnitude is inversely proportional to distance (closer = stronger repulsion).
    /// </summary>
    /// <param name="neighbors">List of neighboring boids</param>
    /// <returns>Separation steering vector</returns>
    private Vector3 ComputeSeparation(List<BoidController> neighbors)
    {
        Vector3 force = Vector3.zero;
        
        foreach (var neighbor in neighbors)
        {
            Vector3 awayFromNeighbor = transform.position - neighbor.transform.position;
            float distance = awayFromNeighbor.magnitude;
            
            if (distance > 0)
            {
                // Inverse distance weighting: closer neighbors exert stronger repulsion
                force += awayFromNeighbor.normalized / distance;
            }
        }
        
        return force;
    }

    /// <summary>
    /// Alignment: Steers toward the average heading of nearby neighbors.
    /// Helps maintain cohesive group movement direction.
    /// </summary>
    /// <param name="neighbors">List of neighboring boids</param>
    /// <returns>Alignment steering vector</returns>
    private Vector3 ComputeAlignment(List<BoidController> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector3.zero;
        
        Vector3 averageVelocity = Vector3.zero;
        
        foreach (var neighbor in neighbors)
        {
            averageVelocity += neighbor._rigidbody.linearVelocity;
        }
        
        averageVelocity /= neighbors.Count;
        
        // Steer toward average heading
        return averageVelocity.normalized - _rigidbody.linearVelocity.normalized;
    }

    /// <summary>
    /// Cohesion: Steers toward the center of mass of nearby neighbors.
    /// Creates the grouping/flocking tendency.
    /// </summary>
    /// <param name="neighbors">List of neighboring boids</param>
    /// <returns>Cohesion steering vector</returns>
    private Vector3 ComputeCohesion(List<BoidController> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        
        foreach (var neighbor in neighbors)
        {
            centerOfMass += neighbor.transform.position;
        }
        
        centerOfMass /= neighbors.Count;
        
        // Direction toward center of mass
        return (centerOfMass - transform.position).normalized;
    }
    
    #endregion
}
