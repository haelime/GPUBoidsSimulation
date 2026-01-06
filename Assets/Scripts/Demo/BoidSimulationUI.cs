// ============================================================================
// BoidSimulationUI.cs
// 
// UI controller for interactive boid simulation demo
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoidSimulationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GPUBoids gpuBoids;
    [SerializeField] private BoidsRender boidsRender;
    
    [Header("Boid Count")]
    [SerializeField] private Slider boidCountSlider;
    [SerializeField] private TextMeshProUGUI boidCountText;
    [SerializeField] private int minBoidCount = 1000;
    [SerializeField] private int maxBoidCount = 65536;
    
    [Header("Speed")]
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;
    
    [Header("Behavior Weights")]
    [SerializeField] private Slider separationSlider;
    [SerializeField] private TextMeshProUGUI separationText;
    [SerializeField] private Slider alignmentSlider;
    [SerializeField] private TextMeshProUGUI alignmentText;
    [SerializeField] private Slider cohesionSlider;
    [SerializeField] private TextMeshProUGUI cohesionText;
    
    [Header("Simulation Control")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI pauseButtonText;
    [SerializeField] private Button resetButton;
    
    private bool _isPaused;
    private float _cachedTimeScale = 1f;
    
    private void Start()
    {
        InitializeSliders();
        InitializeButtons();
    }
    
    private void InitializeSliders()
    {
        if (gpuBoids == null) return;
        
        // Boid Count
        if (boidCountSlider != null)
        {
            boidCountSlider.minValue = minBoidCount;
            boidCountSlider.maxValue = maxBoidCount;
            boidCountSlider.value = gpuBoids.MaxObjectNum;
            boidCountSlider.onValueChanged.AddListener(OnBoidCountChanged);
            UpdateBoidCountText(gpuBoids.MaxObjectNum);
        }
        
        // Speed
        if (speedSlider != null)
        {
            speedSlider.minValue = 1f;
            speedSlider.maxValue = 20f;
            speedSlider.value = gpuBoids.MaxSpeed;
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            UpdateSpeedText(gpuBoids.MaxSpeed);
        }
        
        // Separation
        if (separationSlider != null)
        {
            separationSlider.minValue = 0f;
            separationSlider.maxValue = 10f;
            separationSlider.value = gpuBoids.SeparateWeight;
            separationSlider.onValueChanged.AddListener(OnSeparationChanged);
            UpdateSeparationText(gpuBoids.SeparateWeight);
        }
        
        // Alignment
        if (alignmentSlider != null)
        {
            alignmentSlider.minValue = 0f;
            alignmentSlider.maxValue = 10f;
            alignmentSlider.value = gpuBoids.AlignmentWeight;
            alignmentSlider.onValueChanged.AddListener(OnAlignmentChanged);
            UpdateAlignmentText(gpuBoids.AlignmentWeight);
        }
        
        // Cohesion
        if (cohesionSlider != null)
        {
            cohesionSlider.minValue = 0f;
            cohesionSlider.maxValue = 10f;
            cohesionSlider.value = gpuBoids.CohesionWeight;
            cohesionSlider.onValueChanged.AddListener(OnCohesionChanged);
            UpdateCohesionText(gpuBoids.CohesionWeight);
        }
    }
    
    private void InitializeButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
            UpdatePauseButtonText();
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
    }
    
    private void OnBoidCountChanged(float value)
    {
        int count = Mathf.RoundToInt(value / 256) * 256; // Round to multiple of 256
        count = Mathf.Max(256, count);
        UpdateBoidCountText(count);
    }
    
    private void OnSpeedChanged(float value)
    {
        if (gpuBoids != null)
        {
            gpuBoids.MaxSpeed = value;
        }
        UpdateSpeedText(value);
    }
    
    private void OnSeparationChanged(float value)
    {
        if (gpuBoids != null)
        {
            gpuBoids.SeparateWeight = value;
        }
        UpdateSeparationText(value);
    }
    
    private void OnAlignmentChanged(float value)
    {
        if (gpuBoids != null)
        {
            gpuBoids.AlignmentWeight = value;
        }
        UpdateAlignmentText(value);
    }
    
    private void OnCohesionChanged(float value)
    {
        if (gpuBoids != null)
        {
            gpuBoids.CohesionWeight = value;
        }
        UpdateCohesionText(value);
    }
    
    private void OnPauseClicked()
    {
        _isPaused = !_isPaused;
        
        if (_isPaused)
        {
            _cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _cachedTimeScale;
        }
        
        UpdatePauseButtonText();
    }
    
    private void OnResetClicked()
    {
        if (gpuBoids != null)
        {
            int newCount = Mathf.RoundToInt(boidCountSlider.value / 256) * 256;
            newCount = Mathf.Max(256, newCount);
            gpuBoids.ReinitializeWithCount(newCount);
        }
    }
    
    private void UpdateBoidCountText(int count)
    {
        if (boidCountText != null)
        {
            boidCountText.text = $"Boids: {count:N0}";
        }
    }
    
    private void UpdateSpeedText(float value)
    {
        if (speedText != null)
        {
            speedText.text = $"Speed: {value:F1}";
        }
    }
    
    private void UpdateSeparationText(float value)
    {
        if (separationText != null)
        {
            separationText.text = $"Separation: {value:F1}";
        }
    }
    
    private void UpdateAlignmentText(float value)
    {
        if (alignmentText != null)
        {
            alignmentText.text = $"Alignment: {value:F1}";
        }
    }
    
    private void UpdateCohesionText(float value)
    {
        if (cohesionText != null)
        {
            cohesionText.text = $"Cohesion: {value:F1}";
        }
    }
    
    private void UpdatePauseButtonText()
    {
        if (pauseButtonText != null)
        {
            pauseButtonText.text = _isPaused ? "Resume" : "Pause";
        }
    }
}
