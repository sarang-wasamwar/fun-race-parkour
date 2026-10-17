// AudioManager.cs
// Persistent audio manager singleton (DontDestroyOnLoad).
// Manages volume channels (Master, Music, SFX) backed by PlayerPrefs,
// provides music playback and SFX dispatching for race events and UI.

using UnityEngine;

namespace FunRaceParkour
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string KeyMasterVol = "FRP_MasterVol";
        private const string KeyMusicVol  = "FRP_MusicVol";
        private const string KeySFXVol    = "FRP_SFXVol";

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Default Sound Cues (Optional)")]
        [SerializeField] private AudioClip countdownTickClip;
        [SerializeField] private AudioClip countdownGoClip;
        [SerializeField] private AudioClip checkpointClip;
        [SerializeField] private AudioClip finishClip;
        [SerializeField] private AudioClip hazardHitClip;
        [SerializeField] private AudioClip respawnClip;
        [SerializeField] private AudioClip buttonClickClip;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume  { get; private set; } = 0.8f;
        public float SFXVolume    { get; private set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();
            LoadVolumeSettings();
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        public void LoadVolumeSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(KeyMasterVol, 1f);
            MusicVolume  = PlayerPrefs.GetFloat(KeyMusicVol, 0.8f);
            SFXVolume    = PlayerPrefs.GetFloat(KeySFXVol, 1f);

            ApplyVolumes();
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(KeyMasterVol, MasterVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(KeyMusicVol, MusicVolume);
            ApplyVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(KeySFXVol, SFXVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = MasterVolume * MusicVolume;
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = MasterVolume * MusicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null) return;
            float finalVolume = MasterVolume * SFXVolume * Mathf.Clamp01(volumeScale);
            sfxSource.PlayOneShot(clip, finalVolume);
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;
            float finalVolume = MasterVolume * SFXVolume * Mathf.Clamp01(volumeScale);
            AudioSource.PlayClipAtPoint(clip, position, finalVolume);
        }

        // ── Convenience event cues ──────────────────────────────────────────
        public void PlayCountdownTick() => PlaySFX(countdownTickClip);
        public void PlayCountdownGo()   => PlaySFX(countdownGoClip);
        public void PlayCheckpoint()    => PlaySFX(checkpointClip);
        public void PlayFinish()        => PlaySFX(finishClip);
        public void PlayHazardHit()     => PlaySFX(hazardHitClip);
        public void PlayRespawn()       => PlaySFX(respawnClip);
        public void PlayButtonClick()   => PlaySFX(buttonClickClip);
    }
}
