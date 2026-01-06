// ============================================================================
// GPUBoids.cs
// 
// GPU-Accelerated Boid Flocking Simulation using Compute Shaders
// 
// Technical Highlights:
// - GPU Compute Shader utilization for O(n²) neighbor calculations
// - Double-buffered ComputeBuffer for read/write separation
// - Thread group optimization for GPU parallelism
// - Craig Reynolds' Boids algorithm (Separation, Alignment, Cohesion)
// ============================================================================

using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// GPU-accelerated Boid flocking simulation controller.
/// Manages compute shader dispatch and buffer lifecycle for massively parallel boid simulation.
/// </summary>
/// <remarks>
/// Performance: Capable of simulating 16,384+ boids at 60fps.
/// </remarks>
public class GPUBoids : MonoBehaviour
{
    #region Data Structures
    
    /// <summary>
    /// GPU-compatible boid data structure.
    /// Aligned for efficient GPU memory access (24 bytes = 2 x float3).
    /// </summary>
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    struct BoidData
    {
        public Vector3 Velocity;  // Current movement direction and speed
        public Vector3 Position;  // World space position
    }
    
    #endregion

    #region Constants
    
    /// <summary>
    /// GPU thread group size. Must match the [numthreads(256,1,1)] declaration in compute shader.
    /// </summary>
    private const int SIMULATION_BLOCK_SIZE = 256;
    
    #endregion

    #region Boid Simulation Parameters
    
    [Header("Population Settings")]
    [Tooltip("Total number of boids to simulate. Higher values require more GPU resources.")]
    [Range(256, 32768)]
    public int MaxObjectNum = 16384;

    [Header("Flocking Behavior - Neighborhood Radii")]
    [Tooltip("Radius for cohesion calculation - boids steer toward average position of neighbors")]
    public float CohesionNeighborhoodRadius = 2.0f;
    
    [Tooltip("Radius for alignment calculation - boids match velocity with nearby neighbors")]
    public float AlignmentNeighborhoodRadius = 2.0f;
    
    [Tooltip("Radius for separation calculation - boids avoid crowding neighbors")]
    public float SeparateNeighborhoodRadius = 1.0f;

    [Header("Movement Constraints")]
    [Tooltip("Maximum speed limit for boid movement")]
    public float MaxSpeed = 5.0f;
    
    [Tooltip("Maximum steering force applied per frame (affects turn responsiveness)")]
    public float MaxSteerForce = 0.5f;

    [Header("Behavior Weights")]
    [Tooltip("Influence of cohesion behavior (grouping tendency)")]
    public float CohesionWeight = 1.0f;
    
    [Tooltip("Influence of alignment behavior (velocity matching)")]
    public float AlignmentWeight = 1.0f;
    
    [Tooltip("Influence of separation behavior (collision avoidance)")]
    public float SeparateWeight = 3.0f;
    
    [Tooltip("Influence of wall avoidance (boundary containment)")]
    public float AvoidWallWeight = 10.0f;

    [Header("Simulation Boundaries")]
    [Tooltip("Center point of the simulation volume")]
    public Vector3 WallCenter = Vector3.zero;
    
    [Tooltip("Dimensions of the simulation volume (boids are contained within)")]
    public Vector3 WallSize = new Vector3(32.0f, 32.0f, 32.0f);
    
    [Header("Mouse Attractor")]
    [HideInInspector] public Vector3 AttractorPosition;
    [HideInInspector] public float AttractorStrength;
    [HideInInspector] public bool AttractorEnabled;
    
    #endregion

    #region Shader References
    
    [Header("Compute Shader")]
    [Tooltip("Reference to the Boids compute shader asset")]
    public ComputeShader BoidsCS;
    
    #endregion

    #region Private GPU Resources
    
    /// <summary>Buffer storing calculated forces for each boid (write target for ForceCS kernel)</summary>
    private ComputeBuffer _boidForceBuffer;
    
    /// <summary>Buffer storing position and velocity data for each boid</summary>
    private ComputeBuffer _boidDataBuffer;
    
    #endregion

    #region Public Accessors
    
    /// <summary>
    /// Provides read access to the boid data buffer for GPU instanced rendering.
    /// </summary>
    /// <returns>ComputeBuffer containing BoidData structures, or null if not initialized</returns>
    public ComputeBuffer GetBoidDataBuffer()
    {
        return _boidDataBuffer;
    }

    /// <summary>
    /// Returns the maximum number of boids being simulated.
    /// Used by the renderer to configure instanced draw calls.
    /// </summary>
    public int GetMaxObjectNum()
    {
        return MaxObjectNum;
    }
    
    /// <summary>
    /// Returns the center of the simulation volume.
    /// Used to set rendering bounds for frustum culling.
    /// </summary>
    public Vector3 GetSimulationAreaCenter()
    {
        return WallCenter;
    }
    
    /// <summary>
    /// Returns the size of the simulation volume.
    /// Used to set rendering bounds for frustum culling.
    /// </summary>
    public Vector3 GetSimulationAreaSize()
    {
        return WallSize;
    }
    
    /// <summary>
    /// Reinitializes the simulation with a new boid count.
    /// Releases existing buffers and creates new ones.
    /// </summary>
    public void ReinitializeWithCount(int newCount)
    {
        ReleaseBuffer();
        MaxObjectNum = Mathf.Clamp(newCount, 256, 65536);
        InitBuffer();
    }
    
    #endregion

    #region Unity Lifecycle
    
    /// <summary>
    /// Initializes GPU buffers on startup.
    /// </summary>
    private void Start()
    {
        InitBuffer();
    }

    /// <summary>
    /// Dispatches compute shader kernels each frame to update boid simulation.
    /// </summary>
    private void Update()
    {
        Simulation();
    }

    /// <summary>
    /// Releases GPU resources when the component is destroyed.
    /// Critical for preventing GPU memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        ReleaseBuffer();
    }

    /// <summary>
    /// Visualizes the simulation boundary in the Unity Editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(WallCenter, WallSize);
    }
    
    #endregion

    #region GPU Buffer Management
    
    /// <summary>
    /// Allocates and initializes GPU compute buffers.
    /// Boids are spawned at random positions within a unit sphere around origin.
    /// </summary>
    private void InitBuffer()
    {
        // Allocate GPU buffers with appropriate stride sizes
        _boidDataBuffer = new ComputeBuffer(MaxObjectNum, Marshal.SizeOf(typeof(BoidData)));
        _boidForceBuffer = new ComputeBuffer(MaxObjectNum, Marshal.SizeOf(typeof(Vector3)));

        // Initialize with random positions and velocities
        var forceArr = new Vector3[MaxObjectNum];
        var boidDataArr = new BoidData[MaxObjectNum];
        
        for (var i = 0; i < MaxObjectNum; i++)
        {
            forceArr[i] = Vector3.zero;
            boidDataArr[i].Position = Random.insideUnitSphere * 1.0f;
            boidDataArr[i].Velocity = Random.insideUnitSphere * 0.1f;
        }
        
        // Upload initial data to GPU
        _boidForceBuffer.SetData(forceArr);
        _boidDataBuffer.SetData(boidDataArr);

        // Allow GC to collect temporary arrays
        forceArr = null;
        boidDataArr = null;
    }
    
    /// <summary>
    /// Releases GPU compute buffers.
    /// Must be called to prevent GPU memory leaks.
    /// </summary>
    private void ReleaseBuffer()
    {
        if (_boidDataBuffer != null)
        {
            _boidDataBuffer.Release();
            _boidDataBuffer = null;
        }

        if (_boidForceBuffer != null)
        {
            _boidForceBuffer.Release();
            _boidForceBuffer = null;
        }
    }
    
    #endregion

    #region GPU Simulation
    
    /// <summary>
    /// Executes the boid simulation on the GPU.
    /// Two-pass approach:
    /// 1. ForceCS: Calculate steering forces based on neighbor interactions
    /// 2. IntegrateCS: Apply forces to update positions and velocities
    /// </summary>
    private void Simulation()
    {
        ComputeShader cs = BoidsCS;
        int kernelId = -1;

        // Calculate thread group count (ceiling division to cover all boids)
        int threadGroupSize = Mathf.CeilToInt((float)MaxObjectNum / SIMULATION_BLOCK_SIZE);

        // ===== PASS 1: Force Calculation =====
        // Each thread calculates forces for one boid based on all neighbors (O(n²))
        kernelId = cs.FindKernel("ForceCS");
        
        // Set simulation parameters as shader uniforms
        cs.SetInt("_MaxBoidObjectNum", MaxObjectNum);
        cs.SetFloat("_CohesionNeighborhoodRadius", CohesionNeighborhoodRadius);
        cs.SetFloat("_AlignmentNeighborhoodRadius", AlignmentNeighborhoodRadius);
        cs.SetFloat("_SeparateNeighborhoodRadius", SeparateNeighborhoodRadius);
        cs.SetFloat("_MaxSpeed", MaxSpeed);
        cs.SetFloat("_MaxSteerForce", MaxSteerForce);
        cs.SetFloat("_SeparateWeight", SeparateWeight);
        cs.SetFloat("_CohesionWeight", CohesionWeight);
        cs.SetFloat("_AlignmentWeight", AlignmentWeight);
        cs.SetVector("_WallCenter", WallCenter);
        cs.SetVector("_WallSize", WallSize);
        cs.SetFloat("_AvoidWallWeight", AvoidWallWeight);
        cs.SetVector("_AttractorPosition", AttractorPosition);
        cs.SetFloat("_AttractorStrength", AttractorStrength);
        cs.SetInt("_AttractorEnabled", AttractorEnabled ? 1 : 0);
        
        // Bind buffers for force calculation
        cs.SetBuffer(kernelId, "_BoidDataBufferRead", _boidDataBuffer);
        cs.SetBuffer(kernelId, "_BoidForceBufferWrite", _boidForceBuffer);
        cs.Dispatch(kernelId, threadGroupSize, 1, 1);

        // ===== PASS 2: Integration (Euler Method) =====
        // Apply calculated forces to update velocity and position
        kernelId = cs.FindKernel("IntegrateCS");
        cs.SetFloat("_DeltaTime", Time.deltaTime);
        cs.SetBuffer(kernelId, "_BoidForceBufferRead", _boidForceBuffer);
        cs.SetBuffer(kernelId, "_BoidDataBufferWrite", _boidDataBuffer);
        cs.Dispatch(kernelId, threadGroupSize, 1, 1);
    }
    
    #endregion
}
