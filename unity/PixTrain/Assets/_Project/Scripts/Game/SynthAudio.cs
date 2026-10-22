using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixTrain.Game
{
    public enum Sfx { Coin, Pickup, Cash, Full, Crash, Horn, Click, Clack, Fuel, Chuff, Star0, Star1, Star2, Turbo, Warn }

    /// <summary>
    /// Ses dosyası kullanmadan, açılışta küçük 8-bit efektler sentezler (prototipteki WebAudio sesleriyle aynı).
    /// </summary>
    public sealed class SynthAudio : MonoBehaviour
    {
        private const int Rate = 44100;

        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        private AudioSource _source;
        private System.Random _noise = new System.Random(3);

        public bool Muted { get; set; }

        private enum Wave { Square, Sawtooth, Triangle }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            Build(Sfx.Coin, 0.15f, b => { Tone(b, 988, 0.05f, Wave.Square, 0.04f); Tone(b, 1319, 0.08f, Wave.Square, 0.04f, 0, 0.05f); });
            Build(Sfx.Pickup, 0.25f, b => { Tone(b, 523, 0.07f); Tone(b, 659, 0.07f, Wave.Square, 0.06f, 0, 0.06f); Tone(b, 784, 0.1f, Wave.Square, 0.06f, 0, 0.12f); });
            Build(Sfx.Cash, 0.3f, b => { float[] f = { 784, 988, 1175, 1568 }; for (int i = 0; i < f.Length; i++) Tone(b, f[i], 0.09f, Wave.Square, 0.05f, 0, i * 0.06f); });
            Build(Sfx.Full, 0.2f, b => Tone(b, 150, 0.18f, Wave.Square, 0.06f, -40));
            Build(Sfx.Crash, 0.45f, b => { Noise(b, 0.4f, 0.22f); Tone(b, 90, 0.35f, Wave.Sawtooth, 0.08f, -50); });
            Build(Sfx.Horn, 0.6f, b => { foreach (float d in new[] { 0f, 0.3f }) { Tone(b, 311, 0.22f, Wave.Sawtooth, 0.05f, 0, d); Tone(b, 392, 0.22f, Wave.Sawtooth, 0.045f, 0, d); } });
            Build(Sfx.Click, 0.05f, b => Tone(b, 660, 0.03f, Wave.Square, 0.035f));
            Build(Sfx.Clack, 0.06f, b => Noise(b, 0.05f, 0.08f, true));
            Build(Sfx.Fuel, 0.15f, b => Tone(b, 330, 0.12f, Wave.Triangle, 0.08f, 220));
            Build(Sfx.Chuff, 0.08f, b => Noise(b, 0.07f, 0.025f, true));
            Build(Sfx.Star0, 0.2f, b => Tone(b, 880, 0.18f, Wave.Square, 0.05f));
            Build(Sfx.Star1, 0.2f, b => Tone(b, 1100, 0.18f, Wave.Square, 0.05f));
            Build(Sfx.Star2, 0.2f, b => Tone(b, 1320, 0.18f, Wave.Square, 0.05f));
            Build(Sfx.Turbo, 0.6f, b => { Tone(b, 160, 0.55f, Wave.Sawtooth, 0.05f, 440); Noise(b, 0.45f, 0.06f, true); });
            Build(Sfx.Warn, 0.35f, b => { Tone(b, 880, 0.09f, Wave.Square, 0.05f); Tone(b, 660, 0.09f, Wave.Square, 0.05f, 0, 0.12f); Tone(b, 880, 0.09f, Wave.Square, 0.05f, 0, 0.24f); });
        }

        public void Play(Sfx sfx, float delay = 0f)
        {
            if (Muted || !_clips.TryGetValue(sfx, out var clip)) return;
            if (delay <= 0f) _source.PlayOneShot(clip);
            else StartCoroutineSafe(delay, clip);
        }

        private void StartCoroutineSafe(float delay, AudioClip clip) => StartCoroutine(PlayLater(delay, clip));

        private System.Collections.IEnumerator PlayLater(float delay, AudioClip clip)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (!Muted) _source.PlayOneShot(clip);
        }

        private void Build(Sfx sfx, float seconds, Action<float[]> fill)
        {
            var buffer = new float[Mathf.CeilToInt(seconds * Rate)];
            fill(buffer);
            for (int i = 0; i < buffer.Length; i++) buffer[i] = Mathf.Clamp(buffer[i] * 2.5f, -1f, 1f);
            var clip = AudioClip.Create("sfx-" + sfx, buffer.Length, 1, Rate, false);
            clip.SetData(buffer, 0);
            _clips[sfx] = clip;
        }

        /// <summary>Üstel sönümlü ton; frekans süre boyunca <paramref name="slide"/> kadar kayar.</summary>
        private static void Tone(float[] b, float freq, float dur, Wave wave = Wave.Square, float vol = 0.06f, float slide = 0f, float delay = 0f)
        {
            int start = (int)(delay * Rate), n = (int)(dur * Rate);
            double phase = 0;
            for (int i = 0; i < n && start + i < b.Length; i++)
            {
                float t = (float)i / n;
                double f = freq + slide * t;
                phase += f / Rate;
                double p = phase - Math.Floor(phase);
                float s;
                switch (wave)
                {
                    case Wave.Sawtooth: s = (float)(2 * p - 1); break;
                    case Wave.Triangle: s = (float)(4 * Math.Abs(p - 0.5) - 1); break;
                    default: s = p < 0.5 ? 1f : -1f; break;
                }
                float env = vol * Mathf.Pow(0.0008f / Mathf.Max(vol, 0.0001f), t);
                b[start + i] += s * env;
            }
        }

        private void Noise(float[] b, float dur, float vol, bool highPass = false)
        {
            int n = Mathf.Min(b.Length, (int)(dur * Rate));
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float s = (float)(_noise.NextDouble() * 2 - 1) * (1f - (float)i / n);
                if (highPass) { float hp = s - prev; prev = s; s = hp; }
                b[i] += s * vol;
            }
        }
    }
}
