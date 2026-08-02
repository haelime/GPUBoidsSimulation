using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GPUBoids))]
public sealed class BoidBenchmarkRunner : MonoBehaviour
{
    private const int ThreadGroupSize = 256;
    private const int BoidDataBytes = 24;
    private const int ForceDataBytes = 12;

    [Header("Target")]
    [SerializeField] private GPUBoids simulation;

    [Header("Sweep")]
    [SerializeField] private int[] populationSizes =
        { 1024, 2048, 4096, 8192, 16384 };
    [SerializeField, Min(1)] private int warmupFrames = 120;
    [SerializeField, Min(30)] private int sampleFrames = 300;
    [SerializeField] private bool runOnStart = false;

    private readonly FrameTiming[] timingBuffer = new FrameTiming[1];
    private readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
    private bool isRunning;

    private IEnumerator Start()
    {
        if (!runOnStart)
            yield break;
        yield return null;
        yield return RunSweep();
    }

    [ContextMenu("Run Boid Benchmark")]
    public void RunBenchmark()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Run the benchmark in Play Mode.", this);
            return;
        }
        if (!isRunning)
            StartCoroutine(RunSweep());
    }

    private IEnumerator RunSweep()
    {
        isRunning = true;
        if (simulation == null)
            simulation = GetComponent<GPUBoids>();
        if (simulation == null)
        {
            Debug.LogError("GPUBoids component is missing.", this);
            isRunning = false;
            yield break;
        }

        int previousVSync = QualitySettings.vSyncCount;
        int previousTargetFrameRate = Application.targetFrameRate;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        var csv = new List<string>(populationSizes.Length + 1)
        {
            "boids,thread_groups,pair_checks,pair_growth,buffer_mib," +
            "median_frame_ms,p95_frame_ms,median_cpu_ms,median_gpu_ms," +
            "median_fps,frame_time_growth"
        };

        try
        {
            int previousPopulation = 0;
            double previousMedianFrame = 0.0;
            for (int index = 0; index < populationSizes.Length; index++)
            {
                int population = RoundToThreadGroup(populationSizes[index]);
                if (population == previousPopulation)
                    continue;

                simulation.ReinitializeWithCount(population);
                yield return null;
                for (int frame = 0; frame < warmupFrames; frame++)
                    yield return null;

                var frameTimes = new List<double>(sampleFrames);
                var cpuTimes = new List<double>(sampleFrames);
                var gpuTimes = new List<double>(sampleFrames);
                for (int frame = 0; frame < sampleFrames; frame++)
                {
                    FrameTimingManager.CaptureFrameTimings();
                    yield return endOfFrame;
                    frameTimes.Add(Time.unscaledDeltaTime * 1000.0);
                    uint count = FrameTimingManager.GetLatestTimings(1, timingBuffer);
                    if (count == 0)
                        continue;
                    if (timingBuffer[0].cpuFrameTime > 0.0)
                        cpuTimes.Add(timingBuffer[0].cpuFrameTime);
                    if (timingBuffer[0].gpuFrameTime > 0.0)
                        gpuTimes.Add(timingBuffer[0].gpuFrameTime);
                }

                double medianFrame = Percentile(frameTimes, 0.5);
                double pairGrowth = previousPopulation == 0
                    ? 1.0
                    : (double)population * population /
                      ((double)previousPopulation * previousPopulation);
                double frameGrowth = previousMedianFrame <= 0.0
                    ? 1.0
                    : medianFrame / previousMedianFrame;
                ulong pairChecks = (ulong)population * (ulong)population;
                double bufferMiB = (double)population *
                    (BoidDataBytes + ForceDataBytes) / (1024.0 * 1024.0);

                csv.Add(string.Join(",",
                    population.ToString(CultureInfo.InvariantCulture),
                    (population / ThreadGroupSize).ToString(CultureInfo.InvariantCulture),
                    pairChecks.ToString(CultureInfo.InvariantCulture),
                    Format(pairGrowth),
                    Format(bufferMiB),
                    Format(medianFrame),
                    Format(Percentile(frameTimes, 0.95)),
                    Format(Percentile(cpuTimes, 0.5)),
                    Format(Percentile(gpuTimes, 0.5)),
                    Format(medianFrame > 0.0 ? 1000.0 / medianFrame : 0.0),
                    Format(frameGrowth)));

                previousPopulation = population;
                previousMedianFrame = medianFrame;
            }

            string fileName = "boids-profile-" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) +
                ".csv";
            string outputPath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllLines(outputPath, csv);
            Debug.Log("Boid benchmark saved to " + outputPath, this);
        }
        finally
        {
            QualitySettings.vSyncCount = previousVSync;
            Application.targetFrameRate = previousTargetFrameRate;
            isRunning = false;
        }
    }

    private static int RoundToThreadGroup(int value)
    {
        int clamped = Mathf.Clamp(value, ThreadGroupSize, 65536);
        return Mathf.CeilToInt((float)clamped / ThreadGroupSize) * ThreadGroupSize;
    }

    private static double Percentile(List<double> values, double percentile)
    {
        if (values.Count == 0)
            return 0.0;
        values.Sort();
        int index = Mathf.Clamp(
            Mathf.CeilToInt((float)(values.Count * percentile)) - 1,
            0,
            values.Count - 1);
        return values[index];
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
