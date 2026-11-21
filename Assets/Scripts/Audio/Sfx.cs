using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Менеджер звука: пул источников, музыка, отключение звука.</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance;
        public static event Action MuteChanged;

        static bool muted;

        AudioSource[] voices;
        int voiceIndex;
        AudioSource musicSource;
        readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();

        public static bool Muted
        {
            get { return muted; }
        }

        public static void Init()
        {
            if (Instance != null) return;

            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            go.AddComponent<Sfx>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            muted = PlayerPrefs.GetInt("hb_muted", 0) == 1;
            AudioListener.volume = muted ? 0f : 1f;

            BuildClips();
            BuildSources();

            if (musicSource != null)
            {
                musicSource.clip = SfxSynth.CreateMusic();
                musicSource.loop = true;
                musicSource.volume = 0.18f;
                musicSource.spatialBlend = 0f;
                musicSource.Play();
            }

            Debug.Log("[HeroesBattle] Аудио: " + AudioSettings.outputSampleRate + " Гц, " +
                      AudioSettings.speakerMode + ", музыка " +
                      (musicSource != null && musicSource.clip != null ? musicSource.clip.length.ToString("0.0") + " с" : "нет") +
                      ", mute=" + muted);
        }

        void BuildClips()
        {
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                clips[id] = SfxSynth.Create(id);
            }
        }

        void BuildSources()
        {
            voices = new AudioSource[10];
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 1f;
                source.ignoreListenerPause = true;
                voices[i] = source;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.ignoreListenerPause = true;
        }

        public static void Play(SfxId id, float volume = 1f)
        {
            if (Instance == null || muted || Instance.voices == null) return;

            AudioClip clip;
            if (!Instance.clips.TryGetValue(id, out clip) || clip == null) return;

            var source = Instance.voices[Instance.voiceIndex % Instance.voices.Length];
            Instance.voiceIndex++;

            source.pitch = 0.95f + UnityEngine.Random.value * 0.1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume) * 0.9f);
        }

        public static void ToggleMute()
        {
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;

            PlayerPrefs.SetInt("hb_muted", muted ? 1 : 0);
            PlayerPrefs.Save();

            if (MuteChanged != null) MuteChanged();
            if (!muted) Play(SfxId.Click, 0.7f);
        }

        public static string Label
        {
            get { return muted ? "Звук: выкл" : "Звук: вкл"; }
        }
    }
}
