#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CometaChampions.Core.Enums;
using CometaChampions.Core.Models;

namespace CometaChampions.Runtime.Services
{
    /// <summary>
    /// Centralized persistent audio service managing sound effects (SFX) and background music (BGM).
    /// Persists SfxEnabled and MusicEnabled preferences in PlayerProfile via SaveManager.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager? instance;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindAnyObjectByType<AudioManager>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("AudioManager");
                        instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private AudioSource? bgmSource;
        private AudioSource? sfxSource;
        private AudioListener? fallbackListener;

        private readonly Dictionary<SfxType, AudioClip?> sfxCache = new();
        private readonly Dictionary<BgmType, AudioClip?> bgmCache = new();

        private BgmType currentBgm = BgmType.None;
        private Coroutine? bgmFadeCoroutine;

        public bool IsSfxEnabled
        {
            get
            {
                var profile = SaveManager.Profile;
                return profile == null || profile.SfxEnabled;
            }
            set
            {
                var profile = SaveManager.Profile;
                if (profile != null)
                {
                    profile.SfxEnabled = value;
                }
                ApplySettings();
            }
        }

        public bool IsMusicEnabled
        {
            get
            {
                var profile = SaveManager.Profile;
                return profile == null || profile.MusicEnabled;
            }
            set
            {
                var profile = SaveManager.Profile;
                if (profile != null)
                {
                    profile.MusicEnabled = value;
                }
                ApplySettings();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAudioListener();
        }

        private void EnsureAudioListener()
        {
            var listeners = FindObjectsByType<AudioListener>();

            // Count active/enabled listeners excluding our own fallback
            int externalActiveCount = 0;
            if (listeners != null)
            {
                for (int i = 0; i < listeners.Length; i++)
                {
                    var l = listeners[i];
                    if (l != null && l != fallbackListener && l.enabled && l.gameObject.activeInHierarchy)
                    {
                        externalActiveCount++;
                    }
                }
            }

            if (externalActiveCount > 0)
            {
                if (fallbackListener != null)
                {
                    fallbackListener.enabled = false;
                }
                return;
            }

            // No external active AudioListener found: attach one to Camera.main if available, or enable fallback on AudioManager
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.activeInHierarchy)
            {
                var camListener = mainCam.gameObject.GetComponent<AudioListener>();
                if (camListener == null)
                {
                    camListener = mainCam.gameObject.AddComponent<AudioListener>();
                }
                camListener.enabled = true;

                if (fallbackListener != null)
                {
                    fallbackListener.enabled = false;
                }
                return;
            }

            if (fallbackListener == null)
            {
                fallbackListener = gameObject.GetComponent<AudioListener>();
                if (fallbackListener == null)
                {
                    fallbackListener = gameObject.AddComponent<AudioListener>();
                }
            }
            fallbackListener.enabled = true;
        }

        private void EnsureAudioSources()
        {
            EnsureAudioListener();

            if (bgmSource == null)
            {
                var bgmTransform = transform.Find("BgmSource");
                GameObject bgmObj;
                if (bgmTransform != null)
                {
                    bgmObj = bgmTransform.gameObject;
                }
                else
                {
                    bgmObj = new GameObject("BgmSource");
                    bgmObj.transform.SetParent(transform, false);
                }

                bgmSource = bgmObj.GetComponent<AudioSource>();
                if (bgmSource == null)
                {
                    bgmSource = bgmObj.AddComponent<AudioSource>();
                }
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = 0.65f;
            }

            if (sfxSource == null)
            {
                var sfxTransform = transform.Find("SfxSource");
                GameObject sfxObj;
                if (sfxTransform != null)
                {
                    sfxObj = sfxTransform.gameObject;
                }
                else
                {
                    sfxObj = new GameObject("SfxSource");
                    sfxObj.transform.SetParent(transform, false);
                }

                sfxSource = sfxObj.GetComponent<AudioSource>();
                if (sfxSource == null)
                {
                    sfxSource = sfxObj.AddComponent<AudioSource>();
                }
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = 1.0f;
            }
        }

        public void ApplySettings()
        {
            EnsureAudioSources();

            if (sfxSource != null)
            {
                sfxSource.mute = !IsSfxEnabled;
            }

            if (bgmSource != null)
            {
                bgmSource.mute = !IsMusicEnabled;
                if (IsMusicEnabled && currentBgm != BgmType.None && !bgmSource.isPlaying)
                {
                    var clip = GetBgmClip(currentBgm);
                    if (clip != null)
                    {
                        bgmSource.clip = clip;
                        bgmSource.Play();
                    }
                }
            }

            SaveManager.Save();
        }

        public void PlaySfx(SfxType sfx)
        {
            PlaySfx(sfx, 1.0f);
        }

        public void PlaySfx(SfxType sfx, float pitch)
        {
            if (!IsSfxEnabled) return;

            EnsureAudioSources();
            if (sfxSource == null) return;

            var clip = GetSfxClip(sfx);
            if (clip == null) return;

            sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 2.0f);
            sfxSource.PlayOneShot(clip);
        }

        public void PlayBgm(BgmType bgm)
        {
            EnsureAudioSources();
            if (bgmSource == null) return;

            if (bgm == BgmType.None)
            {
                StopBgm();
                return;
            }

            if (currentBgm == bgm && bgmSource.isPlaying)
            {
                bgmSource.mute = !IsMusicEnabled;
                return;
            }

            currentBgm = bgm;
            bgmSource.mute = !IsMusicEnabled;

            var nextClip = GetBgmClip(bgm);
            if (nextClip == null)
            {
                bgmSource.Stop();
                return;
            }

            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            if (gameObject.activeInHierarchy)
            {
                bgmFadeCoroutine = StartCoroutine(CrossfadeBgmCoroutine(nextClip, 0.8f));
            }
            else
            {
                bgmSource.clip = nextClip;
                bgmSource.volume = 0.65f;
                if (IsMusicEnabled) bgmSource.Play();
            }
        }

        public void StopBgm(float fadeOut = 0.8f)
        {
            currentBgm = BgmType.None;
            if (bgmSource == null || !bgmSource.isPlaying) return;

            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            if (fadeOut > 0f && gameObject.activeInHierarchy)
            {
                bgmFadeCoroutine = StartCoroutine(FadeOutBgmCoroutine(fadeOut));
            }
            else
            {
                bgmSource.Stop();
            }
        }

        public void PauseBgm()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Pause();
            }
        }

        public void ResumeBgm()
        {
            if (bgmSource != null && IsMusicEnabled && currentBgm != BgmType.None)
            {
                bgmSource.UnPause();
            }
        }

        public static BgmType GetBattleBgm(string? arenaBackgroundId)
        {
            if (string.IsNullOrWhiteSpace(arenaBackgroundId))
            {
                return BgmType.Battle_Pedestal3DActive;
            }

            return arenaBackgroundId.Trim().ToLowerInvariant() switch
            {
                "bg_arena_templo_milenario" => BgmType.Battle_TemploMilenario,
                "bg_arena_pista_draconia" => BgmType.Battle_PistaDraconia,
                "bg_arena_llanuras_tirano" => BgmType.Battle_LlanurasTirano,
                "bg_arena_nebulosa_cosmica" => BgmType.Battle_NebulosaCosmica,
                "bg_pedestal_3d_active" => BgmType.Battle_Pedestal3DActive,
                "bg_pedestal_3d_idle" => BgmType.Battle_Pedestal3DIdle,
                _ => BgmType.Battle_Pedestal3DActive
            };
        }

        private AudioClip? GetSfxClip(SfxType sfx)
        {
            if (sfxCache.TryGetValue(sfx, out var cached) && cached != null)
            {
                return cached;
            }

            var clip = Resources.Load<AudioClip>($"Audio/SFX/{sfx}");
            sfxCache[sfx] = clip;
            return clip;
        }

        private AudioClip? GetBgmClip(BgmType bgm)
        {
            if (bgm == BgmType.None) return null;

            if (bgmCache.TryGetValue(bgm, out var cached) && cached != null)
            {
                return cached;
            }

            var clip = Resources.Load<AudioClip>($"Audio/Music/{bgm}");
            bgmCache[bgm] = clip;
            return clip;
        }

        private IEnumerator CrossfadeBgmCoroutine(AudioClip nextClip, float duration)
        {
            if (bgmSource == null) yield break;

            float targetVolume = 0.65f;
            float halfDuration = Mathf.Max(0.05f, duration * 0.5f);

            if (bgmSource.isPlaying)
            {
                float startVol = bgmSource.volume;
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                    yield return null;
                }
                bgmSource.Stop();
            }

            bgmSource.clip = nextClip;
            bgmSource.volume = 0f;
            if (IsMusicEnabled)
            {
                bgmSource.Play();
            }

            float fadeInElapsed = 0f;
            while (fadeInElapsed < halfDuration)
            {
                fadeInElapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, fadeInElapsed / halfDuration);
                yield return null;
            }

            bgmSource.volume = targetVolume;
            bgmFadeCoroutine = null;
        }

        private IEnumerator FadeOutBgmCoroutine(float duration)
        {
            if (bgmSource == null) yield break;

            float startVol = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = 0.65f;
            bgmFadeCoroutine = null;
        }
    }
}
