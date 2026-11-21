using System;
using UnityEngine;

namespace HeroesBattle.EditorTools
{
    /// <summary>Проверка синтезированных звуков: не тишина, без NaN, пик в пределах нормы.</summary>
    public static class AudioSelfTest
    {
        [UnityEditor.MenuItem("HeroesBattle/Test/Validate Audio")]
        public static void ValidateMenu()
        {
            RunBatch();
        }

        public static void RunBatch()
        {
            int failures = 0;

            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                try
                {
                    var clip = SfxSynth.Create(id);
                    Validate(clip, 0.05f, 3.0f, 0.05f, 0.0005f);
                    Debug.Log("[AudioTest] " + id + ": " + clip.length.ToString("0.00") + " c, ok");
                }
                catch (Exception e)
                {
                    failures++;
                    Debug.LogError("[AudioTest] " + id + " провален: " + e.Message);
                }
            }

            try
            {
                var music = SfxSynth.CreateMusic();
                Validate(music, 20f, 30f, 0.05f, 0.005f);
                Debug.Log("[AudioTest] Music: " + music.length.ToString("0.00") + " c, ok");
            }
            catch (Exception e)
            {
                failures++;
                Debug.LogError("[AudioTest] Музыка провалена: " + e.Message);
            }

            if (failures > 0)
            {
                throw new Exception("[AudioTest] Провалов: " + failures);
            }

            Debug.Log("[AudioTest] Все звуки синтезированы корректно.");
        }

        static void Validate(AudioClip clip, float minLength, float maxLength, float minPeak, float minRms)
        {
            if (clip == null) throw new Exception("клип не создан");
            if (clip.channels != 1) throw new Exception("ожидался моно-клип");
            if (clip.samples < 1000) throw new Exception("слишком мало сэмплов");
            if (clip.length < minLength || clip.length > maxLength)
            {
                throw new Exception("некорректная длина: " + clip.length);
            }

            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0)) throw new Exception("не удалось прочитать сэмплы");

            float peak = 0f;
            double sumSquares = 0d;
            for (int i = 0; i < data.Length; i++)
            {
                float value = data[i];
                if (float.IsNaN(value) || float.IsInfinity(value)) throw new Exception("NaN/Infinity в сэмплах");

                float abs = Mathf.Abs(value);
                if (abs > peak) peak = abs;
                sumSquares += value * value;
            }

            double rms = Math.Sqrt(sumSquares / data.Length);

            if (peak > 1.0001f) throw new Exception("клиппинг: пик " + peak);
            if (peak < minPeak) throw new Exception("слишком тихо: пик " + peak);
            if (rms < minRms) throw new Exception("почти тишина: RMS " + rms);
        }
    }
}
