using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Core;

namespace TwoLadles.Visuals
{
    /// <summary>
    /// Simulated day/night cycle. Each campaign wave moves the scene one lighting stage:
    /// stage 0 = slightly dim daylight, stage 1 = warm red noon, stage 2 = dark night (end of shift).
    /// Floor/grass tiles are unlit SpriteRenderers, so they are tinted directly (grass strongest,
    /// dining room medium, kitchen interior lightest). Directional light + ambient follow along.
    /// Self-bootstraps; purely visual, never touches gameplay state.
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; private set; }

        private struct Stage
        {
            public Color light; public float intensity; public Color tint; public Color ambient;
            public Stage(Color l, float i, Color t, Color a) { light = l; intensity = i; tint = t; ambient = a; }
        }

        // Neutral = outside a shift / tutorial.
        private static readonly Stage Neutral = new Stage(new Color(1.0f, 0.94f, 0.84f), 1.25f, Color.white, new Color(0.55f, 0.55f, 0.55f));
        private static readonly Stage[] Stages =
        {
            new Stage(new Color(0.95f, 0.90f, 0.82f), 1.10f, new Color(0.90f, 0.90f, 0.88f), new Color(0.50f, 0.50f, 0.48f)), // Wave 1: dimmer day
            new Stage(new Color(1.00f, 0.62f, 0.45f), 1.00f, new Color(1.00f, 0.74f, 0.62f), new Color(0.55f, 0.40f, 0.34f)), // Wave 2: red noon
            new Stage(new Color(0.45f, 0.50f, 0.80f), 0.55f, new Color(0.42f, 0.47f, 0.68f), new Color(0.22f, 0.24f, 0.36f)), // Wave 3: night
        };

        private const float BlendSeconds = 2.5f;

        private Light sun;
        private Transform cachedGrid;
        private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        private readonly List<Color> baseColors = new List<Color>();
        private readonly List<float> weights = new List<float>(); // 1 = grass, 0.6 dining, 0.3 kitchen

        private Stage current = Neutral, from = Neutral, target = Neutral;
        private float blendT = 1f;
        private int lastStageKey = int.MinValue;

        // Debug preview override (story/visual test tab). AutoStage = follow the shift waves.
        private const int AutoStage = int.MinValue + 1;
        private int previewStage = AutoStage;

        /// <summary>Debug only: force lighting stage (-1 neutral, 0 day, 1 red noon, 2 night).</summary>
        public void PreviewStage(int stage) { previewStage = Mathf.Clamp(stage, -1, 2); }

        /// <summary>Debug only: stop previewing and follow the shift waves again.</summary>
        public void ResumeAuto() { previewStage = AutoStage; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("DayNightCycleManager");
            DontDestroyOnLoad(go);
            go.AddComponent<DayNightCycleManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>Pure mapping used by tests: wave (1-based) of totalWaves to stage 0..2. -1 = neutral.</summary>
        public static int StageForWave(int waveNumber, int totalWaves)
        {
            if (waveNumber < 1) return -1;
            if (totalWaves <= 1) return 0;
            float t = Mathf.Clamp01((waveNumber - 1) / (float)(totalWaves - 1));
            return Mathf.Clamp(Mathf.RoundToInt(t * 2f), 0, 2);
        }

        private int ComputeStageKey()
        {
            var sm = ShiftManager.Instance;
            if (sm == null || !sm.shiftActive || sm.isTutorial || sm.currentMode != GameMode.CAMPAIGN) return -1;
            return StageForWave(sm.currentWaveNumber, sm.totalWaves);
        }

        private void Update()
        {
            int key = previewStage != AutoStage ? previewStage : ComputeStageKey();
            if (key != lastStageKey)
            {
                lastStageKey = key;
                from = current;
                target = key < 0 ? Neutral : Stages[key];
                blendT = 0f;
            }

            EnsureTileCache();

            if (blendT < 1f)
            {
                blendT = Mathf.Min(1f, blendT + Time.unscaledDeltaTime / BlendSeconds);
                float s = Mathf.SmoothStep(0f, 1f, blendT);
                current = new Stage(
                    Color.Lerp(from.light, target.light, s),
                    Mathf.Lerp(from.intensity, target.intensity, s),
                    Color.Lerp(from.tint, target.tint, s),
                    Color.Lerp(from.ambient, target.ambient, s));
                Apply();
            }
        }

        private void EnsureTileCache()
        {
            if (cachedGrid != null) return;
            GameObject grid = GameObject.Find("FloorTileGrid");
            if (grid == null) return;
            cachedGrid = grid.transform;
            tiles.Clear(); baseColors.Clear(); weights.Clear();
            foreach (var sr in grid.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!sr.name.StartsWith("Tile_")) continue;
                float w = sr.sortingOrder <= -55 ? 1f : (sr.sortingOrder <= -52 ? 0.6f : 0.3f);
                tiles.Add(sr); baseColors.Add(sr.color); weights.Add(w);
            }
            Apply(); // grid rebuilt: re-apply current look immediately
        }

        private void Apply()
        {
            if (sun == null)
            {
                foreach (var l in FindObjectsOfType<Light>())
                    if (l.type == LightType.Directional) { sun = l; break; }
            }
            if (sun != null) { sun.color = current.light; sun.intensity = current.intensity; }
            RenderSettings.ambientLight = current.ambient;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == null) continue;
                Color mul = Color.Lerp(Color.white, current.tint, weights[i]);
                Color b = baseColors[i];
                tiles[i].color = new Color(b.r * mul.r, b.g * mul.g, b.b * mul.b, b.a);
            }
        }
    }
}
