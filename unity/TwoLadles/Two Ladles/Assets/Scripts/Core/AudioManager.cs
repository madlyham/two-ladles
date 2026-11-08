using UnityEngine;
using UnityEngine.UI;

namespace TwoLadles.Core
{
    /// <summary>
    /// Upbeat bistro audio synthesizer & sound manager for Two Ladles.
    /// Plays an uplifting, cheerful major-key cafe marimba/ukulele melody at a gentle volume,
    /// plus crisp kitchen sound effects.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioSource sfxSource;
        public AudioSource bgmSource;

        public float masterBgmVolume = 0.045f; // Low, non-intrusive background volume
        public bool isMuted = false;

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
            sfxSource.volume = 0.35f;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0.0f;
            bgmSource.volume = masterBgmVolume;

            SynthesizeClips();
        }

        private void Start()
        {
            if (bgmLoopClip != null && bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.clip = bgmLoopClip;
                bgmSource.volume = isMuted ? 0f : masterBgmVolume;
                bgmSource.Play();
            }
            HookAllButtonsInScene();
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            if (bgmSource != null)
                bgmSource.volume = isMuted ? 0f : masterBgmVolume;
            if (sfxSource != null)
                sfxSource.mute = isMuted;
        }

        public void SetBgmVolume(float vol)
        {
            masterBgmVolume = Mathf.Clamp(vol, 0f, 0.25f);
            if (bgmSource != null && !isMuted)
                bgmSource.volume = masterBgmVolume;
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
            uiClickClip = CreateToneClip("UIClick", 0.05f, (t) =>
                Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 55f));

            pickupClip = CreateToneClip("Pickup", 0.08f, (t) =>
            {
                float freq = Mathf.Lerp(523.25f, 783.99f, t / 0.08f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 32f);
            });

            chopClip = CreateToneClip("Chop", 0.07f, (t) =>
            {
                float noise = (Mathf.PerlinNoise(t * 2000f, 0.5f) * 2f - 1f) * 0.5f;
                float thud = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.7f;
                return (noise + thud) * Mathf.Exp(-t * 42f);
            });

            sizzleClip = CreateToneClip("Sizzle", 0.22f, (t) =>
            {
                float noise = (Mathf.PerlinNoise(t * 3400f, 1.3f) * 2f - 1f);
                return noise * Mathf.Exp(-t * 10f) * 0.35f;
            });

            bellClip = CreateToneClip("ServiceBell", 0.38f, (t) =>
            {
                float f1 = Mathf.Sin(2f * Mathf.PI * 1046.5f * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.6f;
                float f3 = Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * 0.4f;
                return (f1 + f2 + f3) * 0.35f * Mathf.Exp(-t * 7.5f);
            });

            errorClip = CreateToneClip("ErrorBuzz", 0.14f, (t) =>
            {
                float wave = Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.35f;
                return wave * Mathf.Exp(-t * 18f);
            });

            // Check if custom audio clip exists in Resources/Audio/
            AudioClip customBgm = Resources.Load<AudioClip>("Audio/upbeat_bistro_loop");
            if (customBgm != null)
            {
                bgmLoopClip = customBgm;
                return;
            }

            // Uplifting, cheerful 135 BPM C-Major / G-Major Marimba & Ukulele Bistro Groove!
            // 16-beat loop (32 eighth-notes at 0.2222s per eighth-note = 7.111s seamless loop)
            float stepSec = 0.222222f; // 135 BPM eighth-note
            int totalSteps = 32;
            float loopDuration = stepSec * totalSteps;

            // Bright major pentatonic marimba melody notes (Hz)
            // C5=523.25, D5=587.33, E5=659.25, G5=783.99, A5=880.00, C6=1046.50, B5=987.77
            float[] melodyNotes = new float[]
            {
                // Bar 1 (C Major: C5 E5 G5 A5 G5 E5 C5 D5)
                523.25f, 659.25f, 783.99f, 880.00f, 783.99f, 659.25f, 523.25f, 587.33f,
                // Bar 2 (G Major: G5 A5 B5 D6 B5 G5 A5 G5)
                783.99f, 880.00f, 987.77f, 1174.66f, 987.77f, 783.99f, 880.00f, 783.99f,
                // Bar 3 (F Major -> C Major: A5 C6 A5 G5 E5 G5 A5 C6)
                880.00f, 1046.50f, 880.00f, 783.99f, 659.25f, 783.99f, 880.00f, 1046.50f,
                // Bar 4 (G7 -> C6 cheerful turnaround: B5 D6 C6 G5 E5 G5 C6 0)
                987.77f, 1174.66f, 1046.50f, 783.99f, 659.25f, 783.99f, 1046.50f, 783.99f
            };

            // Bouncy walking bassline (C3=130.81, E3=164.81, G3=196.00, F3=174.61, A3=220.00, D3=146.83)
            float[] bassNotes = new float[]
            {
                130.81f, 196.00f, 164.81f, 196.00f, 130.81f, 196.00f, 164.81f, 196.00f,
                196.00f, 246.94f, 293.66f, 246.94f, 196.00f, 246.94f, 293.66f, 246.94f,
                174.61f, 220.00f, 261.63f, 220.00f, 130.81f, 164.81f, 196.00f, 164.81f,
                196.00f, 246.94f, 293.66f, 196.00f, 130.81f, 164.81f, 196.00f, 261.63f
            };

            // Off-beat ukulele chord strums (bright major triads)
            float[][] ukeChords = new float[][]
            {
                new float[] { 523.25f, 659.25f, 783.99f }, // C Maj
                new float[] { 392.00f, 493.88f, 587.33f }, // G Maj
                new float[] { 349.23f, 440.00f, 523.25f }, // F Maj
                new float[] { 523.25f, 659.25f, 783.99f }  // C Maj
            };

            bgmLoopClip = CreateToneClip("UpbeatBistroBgm", loopDuration, (t) =>
            {
                int step = Mathf.Clamp((int)(t / stepSec), 0, totalSteps - 1);
                float localT = t - (step * stepSec);
                int bar = step / 8;

                // 1. Crisp wooden marimba pluck (fundamental + 4th harmonic overtone with fast decay)
                float mFreq = melodyNotes[step];
                float marimbaEnv = Mathf.Exp(-localT * 16f);
                float marimba = (Mathf.Sin(2f * Mathf.PI * mFreq * localT) +
                                 0.25f * Mathf.Sin(2f * Mathf.PI * (mFreq * 2f) * localT)) * marimbaEnv * 0.32f;

                // 2. Bouncy staccato acoustic bass
                float bFreq = bassNotes[step];
                float bassEnv = Mathf.Exp(-localT * 11f);
                float bass = (Mathf.Sin(2f * Mathf.PI * bFreq * localT) +
                              0.3f * Mathf.Sin(2f * Mathf.PI * (bFreq * 2f) * localT)) * bassEnv * 0.28f;

                // 3. Upbeat ukulele chord chop on off-beats (odd steps)
                float uke = 0f;
                if (step % 2 == 1)
                {
                    float ukeEnv = Mathf.Exp(-localT * 22f);
                    foreach (float uf in ukeChords[bar])
                    {
                        uke += Mathf.Sin(2f * Mathf.PI * uf * localT) * 0.12f;
                    }
                    uke *= ukeEnv;
                }

                // 4. Light shaker rhythm tick
                float shaker = (Mathf.PerlinNoise(t * 4200f, 0.3f) * 2f - 1f) * Mathf.Exp(-localT * 45f) * 0.08f;

                return marimba + bass + uke + shaker;
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

        public void PlayUIClick() => PlayClip(uiClickClip, 0.6f);
        public void PlayPickup() => PlayClip(pickupClip, 0.65f);
        public void PlayChop() => PlayClip(chopClip, 0.75f);
        public void PlaySizzle() => PlayClip(sizzleClip, 0.55f);
        public void PlayBell() => PlayClip(bellClip, 0.75f);
        public void PlayError() => PlayClip(errorClip, 0.6f);

        private void PlayClip(AudioClip clip, float volScale = 1.0f)
        {
            if (sfxSource != null && clip != null && !isMuted)
            {
                sfxSource.PlayOneShot(clip, volScale);
            }
        }
    }
}
