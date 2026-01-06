// ============================================================================
// FPSCounter.cs
// 
// Real-time FPS display for performance benchmarking
// ============================================================================

using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f;
    
    private float _deltaTime;
    private float _timer;
    
    private void Update()
    {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        _timer += Time.unscaledDeltaTime;
        
        if (_timer >= updateInterval)
        {
            float fps = 1.0f / _deltaTime;
            float ms = _deltaTime * 1000f;
            
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {fps:F1}\nMS: {ms:F2}";
            }
            
            _timer = 0f;
        }
    }
}
