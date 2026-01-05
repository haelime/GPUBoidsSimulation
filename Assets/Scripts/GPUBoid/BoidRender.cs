// ============================================================================
// BoidsRender.cs
// 
// GPU Instanced Rendering for Boid Simulation
// 
// Technical Highlights:
// - GPU Instancing with indirect draw calls (no CPU per-instance overhead)
// - Compute buffer integration with vertex shader for position/scale
// - Automatic bounds calculation for frustum culling optimization
// ============================================================================

using UnityEngine;

/// <summary>
/// GPU instanced mesh renderer for boid visualization.
/// Renders thousands of mesh instances efficiently using indirect draw calls.
/// </summary>
/// <remarks>
/// This component works in tandem with GPUBoids to render the simulation.
/// The boid data is read directly from GPU buffer in the vertex shader,
/// eliminating CPU-GPU data transfer overhead.
/// </remarks>
[RequireComponent(typeof(GPUBoids))]
public class BoidsRender : MonoBehaviour
{
    #region Rendering Parameters
    
    [Header("Instance Transform")]
    [Tooltip("Scale applied to each boid instance (X: width, Y: height, Z: length)")]
    public Vector3 ObjectScale = new Vector3(0.1f, 0.2f, 0.5f);
    
    #endregion
    
    #region Component References
    
    [Header("Dependencies")]
    [Tooltip("Reference to the GPUBoids simulation component")]
    public GPUBoids GPUBoidsScript;
    
    #endregion
    
    #region Rendering Resources
    
    [Header("Mesh & Material")]
    [Tooltip("Mesh to render for each boid instance")]
    public Mesh InstanceMesh;
    
    [Tooltip("Material with GPU instancing shader (must support StructuredBuffer)")]
    public Material InstanceRenderMaterial;
    
    #endregion
    
    #region Private GPU Resources
    
    /// <summary>
    /// Indirect arguments buffer for DrawMeshInstancedIndirect.
    /// Format: [indexCount, instanceCount, startIndex, baseVertex, startInstance]
    /// </summary>
    private uint[] _argsArray = new uint[5] { 0, 0, 0, 0, 0 };
    private ComputeBuffer _argsBuffer;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        InitializeArgsBuffer();
    }

    private void Update()
    {
        RenderInstancedMesh();
    }

    private void OnDisable()
    {
        ReleaseArgsBuffer();
    }
    
    #endregion
    
    #region GPU Buffer Management
    
    /// <summary>
    /// Initializes the indirect arguments buffer.
    /// </summary>
    private void InitializeArgsBuffer()
    {
        _argsBuffer = new ComputeBuffer(
            1, 
            _argsArray.Length * sizeof(uint),
            ComputeBufferType.IndirectArguments
        );
    }
    
    /// <summary>
    /// Releases the arguments buffer to prevent GPU memory leaks.
    /// </summary>
    private void ReleaseArgsBuffer()
    {
        if (_argsBuffer != null)
        {
            _argsBuffer.Release();
            _argsBuffer = null;
        }
    }
    
    #endregion
    
    #region Instanced Rendering
    
    /// <summary>
    /// Performs GPU instanced rendering of all boids in a single draw call.
    /// </summary>
    /// <remarks>
    /// This method:
    /// 1. Validates rendering prerequisites (material, mesh, GPU instancing support)
    /// 2. Updates indirect draw arguments with current instance count
    /// 3. Binds boid data buffer to material for vertex shader access
    /// 4. Issues a single indirect draw call for all instances
    /// 
    /// Performance: O(1) CPU cost regardless of instance count.
    /// All per-instance transforms are computed in the vertex shader.
    /// </remarks>
    private void RenderInstancedMesh()
    {
        // Validate rendering prerequisites
        if (InstanceRenderMaterial == null || 
            GPUBoidsScript == null || 
            !SystemInfo.supportsInstancing)
        {
            return;
        }
        
        // Configure indirect draw arguments
        uint indexCount = (InstanceMesh != null) ? (uint)InstanceMesh.GetIndexCount(0) : 0;
        _argsArray[0] = indexCount;                              // Index count per instance
        _argsArray[1] = (uint)GPUBoidsScript.GetMaxObjectNum();  // Instance count
        _argsBuffer.SetData(_argsArray);
        
        // Bind simulation data to shader
        InstanceRenderMaterial.SetBuffer("_BoidDataBuffer", GPUBoidsScript.GetBoidDataBuffer());
        InstanceRenderMaterial.SetVector("_ObjectScale", ObjectScale);
        
        // Calculate rendering bounds for frustum culling
        var renderBounds = new Bounds(
            GPUBoidsScript.GetSimulationAreaCenter(),
            GPUBoidsScript.GetSimulationAreaSize()
        );
        
        // Execute indirect instanced draw call
        Graphics.DrawMeshInstancedIndirect(
            InstanceMesh,
            0,                      // Submesh index
            InstanceRenderMaterial,
            renderBounds,
            _argsBuffer
        );
    }
    
    #endregion
}
