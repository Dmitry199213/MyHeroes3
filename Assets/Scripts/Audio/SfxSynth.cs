using System;
using UnityEngine;

namespace HeroesBattle
{
    public enum SfxId
    {
        Click,
        Move,
        Melee,
        Shoot,
        MagicArrow,
        Buff,
        Death,
        Victory,
        Defeat,
        Turn,
        Error
    }

    /// <summary>Синтез всех звуков и фоновой музыки. Никаких внешних аудиофайлов.</summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;

        public static AudioClip Create(SfxId id)
        {
            float[] data;
            string name;

            switch (id)
            {
                case SfxId.Click: data = BuildClick(); name = "sfx_click"; break;
                case SfxId.Move: data = BuildMove(); name = "sfx_move"; break;
                case SfxId.Melee: data = BuildMelee(); name = "sfx_melee"; break;
                case SfxId.Shoot: data = BuildShoot(); name = "sfx_shoot"; break;
                case SfxId.MagicArrow: data = BuildMagicArrow(); name = "sfx_magic"; break;
                case SfxId.Buff: data = BuildBuff(); name = "sfx_buff"; break;
                case SfxId.Death: data = BuildDeath(); name = "sfx_death"; break;
                case SfxId.Victory: data = BuildVictory(); name = "sfx_victory"; break;
                case SfxId.Defeat: data = BuildDefeat(); name = "sfx_defeat"; break;
                case SfxId.Turn: data = BuildTurn(); name = "sfx_turn"; break;
                default: data = BuildError(); name = "sfx_error"; break;
            }

            Normalize(data, 0.85f);

            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateMusic()
        {
            const float length = 24f;
            int total = Mathf.CeilToInt(length * SampleRate);
            var buffer = new float[total];

            float[] dMinor = { 146.83f, 174.61f, 220.00f };
            float[] bFlat = { 116.54f, 146.83f, 174.61f };
            float[] fMajor = { 87.31f, 110.00f, 130.81f };
            float[] cMajor = { 130.81f, 164.81f, 196.00f };

            float[][] chords = { dMinor, bFlat, fMajor, cMajor };
            for (int c = 0; c < chords.Length; c++)
            {
                float start = c * 6f;
                for (int n = 0; n < chords[c].Length; n++)
                {
                    AddPad(buffer, start, 6.8f, chords[c][n], 0.048f, 0.9f, 0.055f);
                    AddPad(buffer, start, 6.8f, chords[c][n], 0.030f, 0.9f, 0.055f, 0.17f);
                }

                AddPad(buffer, start, 6.8f, chords[c][0] * 0.5f, 0.035f, 1.1f, 0.04f);
            }

            float[] melody = { 293.66f, 349.23f, 392.00f, 440.00f, 523.25f };
            var rng = new System.Random(20261122);
            for (float t = 1.5f; t < 22.5f; t += 1.5f)
            {
                if (rng.NextDouble() < 0.34) continue;
                float freq = melody[rng.Next(melody.Length)];
                float amp = 0.055f + (float)rng.NextDouble() * 0.035f;
                AddPluck(buffer, t, 1.1f, freq, amp, 3.4f);
                if (rng.NextDouble() < 0.25f) AddPluck(buffer, t + 0.2f, 0.8f, freq * 2f, amp * 0.28f, 4.5f);
            }

            AddEcho(buffer, 0.28f, 0.18f, 2);
            Normalize(buffer, 0.55f);
            Fade(buffer, 0.35f, 0.35f);

            var clip = AudioClip.Create("music_ambient", total, 1, SampleRate, false);
            clip.SetData(buffer, 0);
            return clip;
        }

        // ------------------------------------------------------------------ sfx recipes

        static float[] BuildClick()
        {
            var buffer = NewBuffer(0.09f);
            AddNoise(buffer, 0f, 0.035f, 0.30f, 90f, 0.55f);
            AddSweep(buffer, 0f, 0.07f, 1000f, 620f, 0.22f, 55f);
            AddTone(buffer, 0f, 0.05f, 1500f, 0.10f, 0.002f, 70f);
            return buffer;
        }

        static float[] BuildMove()
        {
            var buffer = NewBuffer(0.18f);
            AddSweep(buffer, 0f, 0.16f, 190f, 85f, 0.5f, 18f);
            AddNoise(buffer, 0f, 0.09f, 0.16f, 26f, 0.18f);
            return buffer;
        }

        static float[] BuildMelee()
        {
            var buffer = NewBuffer(0.38f);
            AddNoise(buffer, 0f, 0.14f, 0.55f, 26f, 0.42f);
            AddSweep(buffer, 0f, 0.24f, 130f, 55f, 0.65f, 13f);
            AddTone(buffer, 0.004f, 0.30f, 470f, 0.12f, 0.002f, 17f);
            AddTone(buffer, 0.006f, 0.26f, 790f, 0.08f, 0.002f, 20f);
            AddTone(buffer, 0.008f, 0.22f, 1180f, 0.05f, 0.002f, 24f);
            AddEcho(buffer, 0.09f, 0.18f, 1);
            return buffer;
        }

        static float[] BuildShoot()
        {
            var buffer = NewBuffer(0.26f);
            AddSweep(buffer, 0f, 0.16f, 340f, 150f, 0.32f, 26f);
            AddNoise(buffer, 0f, 0.20f, 0.20f, 20f, 0.10f);
            AddTone(buffer, 0f, 0.10f, 900f, 0.09f, 0.001f, 46f);
            return buffer;
        }

        static float[] BuildMagicArrow()
        {
            var buffer = NewBuffer(0.55f);
            AddSweep(buffer, 0f, 0.42f, 520f, 1500f, 0.24f, 5.5f);
            AddTone(buffer, 0.02f, 0.45f, 1046.5f, 0.08f, 0.01f, 6f, 0f, 7f, 12f);
            AddTone(buffer, 0.05f, 0.42f, 1568f, 0.055f, 0.01f, 7f, 0f, 9f, 16f);
            AddNoise(buffer, 0f, 0.38f, 0.08f, 8f, 0.6f);
            AddEcho(buffer, 0.12f, 0.2f, 2);
            return buffer;
        }

        static float[] BuildBuff()
        {
            var buffer = NewBuffer(0.75f);
            float[] notes = { 293.66f, 349.23f, 440.00f, 587.33f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddPluck(buffer, i * 0.11f, 0.55f, notes[i], 0.16f, 5.5f);
                AddPluck(buffer, i * 0.11f, 0.4f, notes[i] * 2f, 0.05f, 7f);
            }

            AddEcho(buffer, 0.16f, 0.22f, 2);
            return buffer;
        }

        static float[] BuildDeath()
        {
            var buffer = NewBuffer(0.7f);
            AddSweep(buffer, 0f, 0.6f, 320f, 70f, 0.38f, 4.5f);
            AddNoise(buffer, 0f, 0.45f, 0.16f, 6f, 0.09f);
            AddTone(buffer, 0f, 0.65f, 62f, 0.16f, 0.01f, 5f);
            return buffer;
        }

        static float[] BuildVictory()
        {
            var buffer = NewBuffer(2.0f);
            float[] notes = { 293.66f, 349.23f, 440.00f, 587.33f };
            float[] starts = { 0f, 0.16f, 0.32f, 0.52f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddPluck(buffer, starts[i], i == notes.Length - 1 ? 1.2f : 0.6f, notes[i], 0.22f, 2.6f);
            }

            float[] chord = { 146.83f, 174.61f, 220.00f, 293.66f };
            for (int i = 0; i < chord.Length; i++)
            {
                AddPad(buffer, 0f, 1.9f, chord[i], 0.06f, 0.05f, 0.8f);
            }

            AddSweep(buffer, 0f, 0.4f, 90f, 60f, 0.35f, 7f);
            AddEcho(buffer, 0.2f, 0.22f, 2);
            return buffer;
        }

        static float[] BuildDefeat()
        {
            var buffer = NewBuffer(1.8f);
            float[] notes = { 220.00f, 174.61f, 146.83f, 73.42f };
            float[] starts = { 0f, 0.34f, 0.68f, 1.05f };
            for (int i = 0; i < notes.Length; i++)
            {
                AddPluck(buffer, starts[i], i == notes.Length - 1 ? 0.8f : 0.55f, notes[i], 0.24f, 2f);
                AddPad(buffer, starts[i], 0.7f, notes[i], 0.05f, 0.04f, 1.4f);
            }

            AddPad(buffer, 0f, 1.75f, 73.42f, 0.10f, 0.05f, 0.7f);
            AddEcho(buffer, 0.25f, 0.2f, 2);
            return buffer;
        }

        static float[] BuildTurn()
        {
            var buffer = NewBuffer(0.3f);
            AddTone(buffer, 0f, 0.18f, 880f, 0.14f, 0.004f, 16f);
            AddTone(buffer, 0.09f, 0.18f, 1174.66f, 0.11f, 0.004f, 16f);
            return buffer;
        }

        static float[] BuildError()
        {
            var buffer = NewBuffer(0.24f);
            AddTone(buffer, 0f, 0.16f, 155f, 0.22f, 0.002f, 12f);
            AddTone(buffer, 0f, 0.16f, 116f, 0.14f, 0.002f, 10f);
            AddNoise(buffer, 0f, 0.05f, 0.12f, 45f, 0.4f);
            return buffer;
        }

        // ------------------------------------------------------------------ synthesis primitives

        static float[] NewBuffer(float seconds)
        {
            return new float[Mathf.Max(16, Mathf.CeilToInt(seconds * SampleRate))];
        }

        static float Envelope(float t, float duration, float attack, float decay)
        {
            float a = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            float d = Mathf.Exp(-t * decay);
            float tail = Mathf.Clamp01((duration - t) / 0.012f);
            return a * d * tail;
        }

        static void AddTone(float[] buffer, float start, float duration, float frequency, float amplitude,
            float attack, float decay, float detune = 0f, float vibratoHz = 0f, float vibratoDepth = 0f)
        {
            int startIndex = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            double phase = 0d;

            for (int i = 0; i < count; i++)
            {
                int index = startIndex + i;
                if (index < 0 || index >= buffer.Length) continue;

                float t = i / (float)SampleRate;
                float f = frequency + detune;
                if (vibratoDepth > 0f) f += Mathf.Sin(2f * Mathf.PI * vibratoHz * t) * vibratoDepth;
                phase += 2d * Math.PI * f / SampleRate;

                float env = Envelope(t, duration, attack, decay);
                buffer[index] += (float)Math.Sin(phase) * amplitude * env;
            }
        }

        static void AddPad(float[] buffer, float start, float duration, float frequency, float amplitude,
            float attack, float decay, float detune = 0f)
        {
            AddTone(buffer, start, duration, frequency, amplitude, attack, decay, detune);
            AddTone(buffer, start, duration, frequency * 2f, amplitude * 0.22f, attack * 1.2f, decay * 1.1f, detune);
        }

        static void AddPluck(float[] buffer, float start, float duration, float frequency, float amplitude, float decay)
        {
            AddTone(buffer, start, duration, frequency, amplitude, 0.003f, decay);
            AddTone(buffer, start, duration, frequency * 2.01f, amplitude * 0.3f, 0.002f, decay * 1.4f);
            AddTone(buffer, start, duration, frequency * 3.02f, amplitude * 0.12f, 0.002f, decay * 1.8f);
        }

        static void AddSweep(float[] buffer, float start, float duration, float from, float to,
            float amplitude, float decay)
        {
            int startIndex = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            double phase = 0d;

            for (int i = 0; i < count; i++)
            {
                int index = startIndex + i;
                if (index < 0 || index >= buffer.Length) continue;

                float t = i / (float)SampleRate;
                float k = count > 1 ? i / (float)(count - 1) : 0f;
                float f = Mathf.Lerp(from, to, k);
                phase += 2d * Math.PI * f / SampleRate;

                float env = Envelope(t, duration, 0.003f, decay);
                buffer[index] += (float)Math.Sin(phase) * amplitude * env;
            }
        }

        static void AddNoise(float[] buffer, float start, float duration, float amplitude, float decay, float smoothing)
        {
            int startIndex = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            var rng = new System.Random(7919 + Mathf.RoundToInt(start * 1000f));
            float low = 0f;

            for (int i = 0; i < count; i++)
            {
                int index = startIndex + i;
                if (index < 0 || index >= buffer.Length) continue;

                float white = (float)rng.NextDouble() * 2f - 1f;
                low = Mathf.Lerp(low, white, Mathf.Clamp01(smoothing));

                float t = i / (float)SampleRate;
                float env = Envelope(t, duration, 0.002f, decay);
                buffer[index] += low * amplitude * env;
            }
        }

        static void AddEcho(float[] buffer, float delaySeconds, float gain, int repeats)
        {
            int delay = Mathf.RoundToInt(delaySeconds * SampleRate);
            if (delay <= 0 || delay >= buffer.Length) return;

            for (int r = 0; r < repeats; r++)
            {
                for (int i = buffer.Length - 1; i >= delay; i--)
                {
                    buffer[i] += buffer[i - delay] * gain;
                }
            }
        }

        static void Normalize(float[] buffer, float target)
        {
            float peak = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                float value = Mathf.Abs(buffer[i]);
                if (value > peak) peak = value;
            }

            if (peak < 1e-5f) return;

            float gain = Mathf.Min(4f, target / peak);
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Mathf.Clamp(buffer[i] * gain, -1f, 1f);
            }
        }

        static void Fade(float[] buffer, float fadeInSeconds, float fadeOutSeconds)
        {
            int fadeIn = Mathf.RoundToInt(fadeInSeconds * SampleRate);
            int fadeOut = Mathf.RoundToInt(fadeOutSeconds * SampleRate);

            for (int i = 0; i < fadeIn && i < buffer.Length; i++)
            {
                buffer[i] *= i / (float)fadeIn;
            }

            for (int i = 0; i < fadeOut && i < buffer.Length; i++)
            {
                buffer[buffer.Length - 1 - i] *= i / (float)fadeOut;
            }
        }
    }
}
