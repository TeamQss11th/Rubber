using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Rubber.World
{
    // Explicit command-line benchmark only; never takes control during ordinary play.
    public sealed class VillaBenchmark : MonoBehaviour
    {
        private readonly List<float>[] samples = { new List<float>(), new List<float>(), new List<float>() };
        private Camera view;
        private VillaLightingPreview lighting;
        private float started;
        private int lastMode = -1;
        private string output;
        private const float Warmup = 12;
        private const float Duration = 180;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-villaBenchmark");
            if (index < 0 || index + 1 >= args.Length) return;
            var component = new GameObject("Benchmark - command line only").AddComponent<VillaBenchmark>();
            component.output = Path.GetFullPath(args[index + 1]);
        }

        private void Start()
        {
            view = Camera.main;
            lighting = FindAnyObjectByType<VillaLightingPreview>();
            if (view == null || lighting == null) { enabled = false; return; }
            FindAnyObjectByType<VillaWalkthrough>().enabled = false;
            lighting.animate = false;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            started = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            float elapsed = Time.realtimeSinceStartup - started;
            // Warm all three lighting modes, then repeat a fixed 60-second scenario three times.
            float routeTime = elapsed < Warmup ? elapsed * 5 : elapsed - Warmup;
            int mode = Mathf.FloorToInt(routeTime / 20) % 3;
            if (mode != lastMode) { lighting.Apply(mode * .5f); lastMode = mode; }
            float segment = routeTime % 20;
            Vector3 position;
            Vector3 target;
            if (segment < 7) { position = new Vector3(0, 1.65f, -5.8f); target = new Vector3(-1, 1.1f, 1); }
            else if (segment < 14) { position = new Vector3(.2f, 1.65f, -2.1f); target = new Vector3(-1, 1, 1.1f); }
            else { position = new Vector3(4.8f, 1.65f, -13.7f); target = new Vector3(0, .5f, -5); }
            view.transform.position = position;
            view.transform.LookAt(target);
            if (elapsed >= Warmup) samples[mode].Add(Time.unscaledDeltaTime * 1000);
            if (elapsed < Warmup + Duration) return;
            var lines = new List<string>
            {
                "Development Player baseline. Wall frame times, NOT isolated GPU timings.",
                $"GPU: {SystemInfo.graphicsDeviceName}; CPU: {SystemInfo.processorType}; RAM MB: {SystemInfo.systemMemorySize}",
                $"Resolution: {Screen.width}x{Screen.height}; VSync: {QualitySettings.vSyncCount}; cap: {Application.targetFrameRate}",
                "12s warmup; 3 x 60s scenario (20s each day/dusk/night); three fixed viewpoints each mode.",
                "mode,samples,mean_ms,p95_ms,p99_ms"
            };
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i].Sort();
                var values = samples[i];
                lines.Add(FormattableString.Invariant($"{new[] { "day", "dusk", "night" }[i]},{values.Count},{values.Average():F3},{values[Mathf.Min(values.Count - 1, Mathf.CeilToInt(values.Count * .95f) - 1)]:F3},{values[Mathf.Min(values.Count - 1, Mathf.CeilToInt(values.Count * .99f) - 1)]:F3}"));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllLines(output, lines);
            enabled = false;
            Application.Quit();
        }
    }
}
