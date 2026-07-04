using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// Per-scene audio: one looping music source plus a shared source for one-shot SFX.
    /// Intentionally not a global singleton — each scene wires its own so audio scope is
    /// obvious and nothing leaks across loads.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField, Tooltip("Looping background-music source.")]
        private AudioSource musicSource;

        [SerializeField, Tooltip("Source used for PlayOneShot sound effects.")]
        private AudioSource sfxSource;

        [Header("Startup")]
        [SerializeField, Tooltip("Music played automatically when the scene starts (optional).")]
        private AudioClip sceneMusic;

        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

        private void Start()
        {
            if (sceneMusic != null)
            {
                PlayMusic(sceneMusic);
            }
        }

        /// <summary>Plays <paramref name="clip"/> as looping background music.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        /// <summary>Stops the background music.</summary>
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        /// <summary>Plays a one-shot sound effect at an optional relative volume.</summary>
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }
    }
}
