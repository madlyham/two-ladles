using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwoLadles.UI
{
    /// <summary>
    /// Player-chosen UI / HUD size. Bigger scale = smaller canvas reference resolution = bigger on-screen UI.
    /// Stored in PlayerPrefs; applied to every Scale-With-Screen-Size CanvasScaler.
    /// </summary>
    public static class UIScaleSettings
    {
        public const string PREF_KEY = "TwoLadles_UIScale";
        public const float MIN = 0.8f;
        public const float MAX = 1.6f;
        public const float STEP = 0.1f;

        // Each scaler's original reference resolution, so repeated Apply() calls never compound.
        private static readonly Dictionary<CanvasScaler, Vector2> baseRefs = new Dictionary<CanvasScaler, Vector2>();

        public static float Clamp(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale)) return 1.0f;
            return Mathf.Clamp(scale, MIN, MAX);
        }

        public static Vector2 ScaledReference(Vector2 baseRef, float scale) => baseRef / Clamp(scale);

        public static float Get() => Clamp(PlayerPrefs.GetFloat(PREF_KEY, 1.0f));

        public static void Set(float scale)
        {
            PlayerPrefs.SetFloat(PREF_KEY, Clamp(scale));
            PlayerPrefs.Save();
            Apply();
        }

        // Round to whole 10% steps so repeated taps never drift (e.g. 1.0999999).
        public static void Step(int direction) => Set(Mathf.Round((Get() + STEP * Mathf.Sign(direction)) * 10f) / 10f);

        public static void Apply()
        {
            float scale = Get();
            // Drop scalers whose canvases were destroyed (e.g. closed phone story) so the cache never grows.
            var dead = new List<CanvasScaler>();
            foreach (var k in baseRefs.Keys) if (k == null) dead.Add(k);
            foreach (var k in dead) baseRefs.Remove(k);
            foreach (var scaler in Object.FindObjectsOfType<CanvasScaler>(true))
            {
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                if (!baseRefs.TryGetValue(scaler, out var baseRef))
                {
                    baseRef = scaler.referenceResolution;
                    baseRefs[scaler] = baseRef;
                }
                scaler.referenceResolution = ScaledReference(baseRef, scale);
            }
        }
    }
}
