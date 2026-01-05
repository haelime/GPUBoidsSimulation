// ============================================================================
// BoidsRender.shader
// 
// GPU Instanced Surface Shader for Boid Rendering
// 
// Technical Highlights:
// - Procedural instancing with StructuredBuffer for per-instance transforms
// - Velocity-based rotation using Euler angles (Heading-Bank-Attitude)
// - Compatible with Unity's Standard lighting model
// ============================================================================

Shader "Hidden/GPUBoids/BoidsRender"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow
        #pragma instancing_options procedural:setup

        // =====================================================================
        // Data Structures
        // =====================================================================
        
        struct Input
        {
            float2 uv_MainTex;
        };
        
        /// Must match BoidData struct in GPUBoids.cs (24 bytes)
        struct BoidData
        {
            float3 velocity;
            float3 position;
        };
        
        // =====================================================================
        // Shader Resources
        // =====================================================================
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<BoidData> _BoidDataBuffer;
        #endif

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float3 _ObjectScale;

        // =====================================================================
        // Utility Functions
        // =====================================================================
        
        /// Constructs a rotation matrix from Euler angles (Heading-Bank-Attitude order)
        /// Used to orient boids along their velocity direction
        float4x4 eulerAnglesToRotationMatrix(float3 angles)
        {
            float ch = cos(angles.y); float sh = sin(angles.y); // Heading (Y-axis)
            float ca = cos(angles.z); float sa = sin(angles.z); // Attitude (Z-axis)
            float cb = cos(angles.x); float sb = sin(angles.x); // Bank (X-axis)
            
            // Rotation order: Ry * Rx * Rz
            return float4x4(
                ch * ca + sh * sb * sa, -ch * sa + sh * sb * ca, sh * cb, 0,
                cb * sa,                 cb * ca,                -sb,     0,
                -sh * ca + ch * sb * sa, sh * sa + ch * sb * ca, ch * cb, 0,
                0,                       0,                       0,      1
            );
        }
        
        // =====================================================================
        // Vertex Shader
        // =====================================================================
        
        /// Transforms vertices using per-instance boid data from GPU buffer
        void vert(inout appdata_full v)
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            BoidData boidData = _BoidDataBuffer[unity_InstanceID];
            
            float3 pos = boidData.position.xyz;
            float3 scl = _ObjectScale;

            // Build object-to-world matrix with scale
            float4x4 object2world = (float4x4)0;
            object2world._11_22_33_44 = float4(scl.xyz, 1.0);
            
            // Calculate rotation from velocity direction
            float rotY = atan2(boidData.velocity.x, boidData.velocity.z);
            float rotX = -asin(boidData.velocity.y / (length(boidData.velocity.xyz) + 1e-8)); // Epsilon to prevent division by zero
            
            // Apply rotation and translation
            float4x4 rotMatrix = eulerAnglesToRotationMatrix(float3(rotX, rotY, 0));
            object2world = mul(rotMatrix, object2world);
            object2world._14_24_34 += pos.xyz;
            
            // Transform vertex position and normal
            v.vertex = mul(object2world, v.vertex);
            v.normal = normalize(mul(object2world, v.normal));
            #endif
        }
        
        /// Required setup function for procedural instancing (empty - all work done in vert)
        void setup() {}
        
        // =====================================================================
        // Surface Shader
        // =====================================================================
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        
        ENDCG
    }
    
    FallBack "Diffuse"
}
