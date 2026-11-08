using UnityEngine;
using UnityEngine.UI;

namespace TwoLadles.Core
{
    /// <summary>
    /// Procedural retro audio synthesizer for Two Ladles.
    /// Generates zero-dependency PCM AudioClips at runtime for UI clicks,
    /// knife chops, stove sizzles, pass counter handoffs, service bells,
    /// and a gentle warm background kitchen ambiance loop.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioSource bgmSource;

        private AudioClip uiClickClip;
        private AudioClip pickupClip;
        private AudioClip chopClip;
        private AudioClip sizzleClip;
        private AudioClip bellClip;
        private AudioClip errorClip;
        private AudioClip bgmLoopClip;

        private const int SAMPLE_RATE = 44100;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (FindObjectOfType<AudioListener>() == null && Camera.main != null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0.0f;
            sfxSource.volume = 0.65f;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0.0f;
            bgmSource.volume = 0.16f;

            SynthesizeClips();
        }

        private void Start()
        {
            if (bgmLoopClip != null && bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.clip = bgmLoopClip;
                bgmSource.Play();
            }
            HookAllButtonsInScene();
        }

        public void HookAllButtonsInScene()
        {
            Button[] buttons = FindObjectsOfType<Button>(true);
            foreach (var btn in buttons)
            {
                btn.onClick.RemoveListener(PlayUIClick);
                btn.onClick.AddListener(PlayUIClick);
            }
        }

        private void SynthesizeClips()
        {
            uiClickClip = CreateToneClip("UIClick", 0.06f, (t) =>
                Mathf.Sin(2f * Mathf.PI * 660f * t) * Mathf.Exp(-t * 45f));

            pickupClip = CreateToneClip("Pickup", 0.09f, (t) =>
            {
                float freq = Mathf.Lerp(380f, 580f, t / 0.09f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 28f);
            });

            chopClip = CreateToneClip("Chop", 0.08f, (t) =>
            {
                float noise = (Mathf.PerlinNoise(t * 1800f, 0.5f) * 2f - 1f) * 0.6f;
                float thud = Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.8f;
                return (noise + thud) * Mathf.Exp(-t * 38f);
            });

            sizzleClip = CreateToneClip("Sizzle", 0.28f, (t) =>
            {
                float noise = (Mathf.PerlinNoise(t * 3200f, 1.3f) * 2f - 1f);
                return noise * Mathf.Exp(-t * 8f) * 0.45f;
            });

            bellClip = CreateToneClip("ServiceBell", 0.45f, (t) =>
            {
                float f1 = Mathf.Sin(2f * Mathf.PI * 880f * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.6f;
                float f3 = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.35f;
                return (f1 + f2 + f3) * 0.4f * Mathf.Exp(-t * 6.5f);
            });

            errorClip = CreateToneClip("ErrorBuzz", 0.18f, (t) =>
            {
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 155f * t)) * 0.35f;
                return wave * Mathf.Exp(-t * 14f);
            });

            // Warm 4-bar cozy kitchen chord progression (Fmaj7 -> Em7 -> Dm7 -> G7)
            bgmLoopClip = CreateToneClip("KitchenBgm", 8.0f, (t) =>
            {
                float[][] chords = new float[][]
                {
                    new float[] { 349.23f, 440.00f, 523.25f, 659.25f }, // Fmaj7
                    new float[] { 329.63f, 392.00f, 493.88f, 587.33f }, // Em7
                    new float[] { 293.66f, 349.23f, 440.00f, 523.25f }, // Dm7
                    new float[] { 392.00f, 493.88f, 587.33f, 698.46f }  // G7
                };
                int chordIdx = Mathf.Clamp((int)(t / 2.0f), 0, 3);
                float localT = t % 2.0f;
                float env = Mathf.Sin(Mathf.Clamp01(localT / 2.0f) * Mathf.PI);
                float sum = 0f;
                foreach (float freq in chords[chordIdx])
                {
                    sum += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.18f;
                }
                return sum * env;
            });
        }

        private AudioClip CreateToneClip(string clipName, float durationSec, System.Func<float, float> generator)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SAMPLE_RATE * durationSec));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                samples[i] = Mathf.Clamp(generator(t), -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void PlayUIClick() => PlayClip(uiClickClip, 0.7f);
        public void PlayPickup() => PlayClip(pickupClip, 0.75f);
        public void PlayChop() => PlayClip(chopClip, 0.85f);
        public void PlaySizzle() => PlayClip(sizzleClip, 0.65f);
        public void PlayBell() => PlayClip(bellClip, 0.85f);
        public void PlayError() => PlayClip(errorClip, 0.7f);

        private void PlayClip(AudioClip clip, float volScale = 1.0f)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, volScale);
            }
        }
    }
}
