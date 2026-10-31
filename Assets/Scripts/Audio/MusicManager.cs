using System.Collections;
using UnityEngine;

namespace Boruhan.Audio
{
    /// <summary>
    /// Plays one music track at a time and crossfades between them. Survives scene loads, so a track
    /// shared by two scenes keeps playing instead of restarting. Creates itself on first use.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        const string VolumeKey = "MuzikSesi";

        static MusicManager instance;

        AudioSource[] sources;
        int activeSource;
        MusicTrack currentTrack;
        Coroutine fade;

        /// <summary>Overall music volume (0-1), remembered between sessions. For a future settings menu.</summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                if (instance != null && instance.currentTrack != null && instance.fade == null)
                    instance.sources[instance.activeSource].volume = instance.TargetVolume(instance.currentTrack);
            }
        }

        public static MusicTrack Current => instance != null ? instance.currentTrack : null;

        public static void Play(MusicTrack track, float fadeSeconds = 1f)
        {
            if (track == null || track.clip == null)
            {
                Stop(fadeSeconds);
                return;
            }
            Instance.Crossfade(track, fadeSeconds);
        }

        public static void Stop(float fadeSeconds = 1f)
        {
            if (instance != null)
                instance.Crossfade(null, fadeSeconds);
        }

        static MusicManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("MuzikYoneticisi");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<MusicManager>();
                    instance.sources = new[] { CreateSource(go), CreateSource(go) };
                }
                return instance;
            }
        }

        static AudioSource CreateSource(GameObject go)
        {
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        float TargetVolume(MusicTrack track) => track.volume * MasterVolume;

        void Crossfade(MusicTrack track, float seconds)
        {
            if (track == currentTrack)
                return;

            currentTrack = track;
            if (fade != null)
                StopCoroutine(fade);
            fade = StartCoroutine(FadeRoutine(track, Mathf.Max(0.01f, seconds)));
        }

        IEnumerator FadeRoutine(MusicTrack track, float seconds)
        {
            var from = sources[activeSource];
            AudioSource to = null;

            if (track != null)
            {
                activeSource = 1 - activeSource;
                to = sources[activeSource];
                to.clip = track.clip;
                to.loop = track.loop;
                to.volume = 0f;
                to.Play();
            }

            float fromStart = from.volume;
            float target = track != null ? TargetVolume(track) : 0f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (to != null)
                    to.volume = Mathf.Lerp(0f, target, k);
                yield return null;
            }

            from.volume = 0f;
            from.Stop();
            if (to != null)
                to.volume = target;
            fade = null;
        }
    }
}
