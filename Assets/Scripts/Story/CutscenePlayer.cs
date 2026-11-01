using System.Collections;
using System.IO;
using Boruhan.Core;
using Boruhan.Dialogue;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Boruhan.Story
{
    /// <summary>
    /// Plays a video (if there is one) with timed subtitles, optional big title cards, then moves on.
    /// Video comes from Video Clip if assigned, otherwise from StreamingAssets/Videolar/&lt;Video File Name&gt;.
    /// If the file is missing, empty or can't be played, the subtitles still run over a placeholder.
    /// The video's own sound is never played (music and voice lines are separate).
    /// </summary>
    public class CutscenePlayer : MonoBehaviour
    {
        [Header("Video")]
        [SerializeField] VideoPlayer videoPlayer;
        [Tooltip("Optional. Drag an imported video here, or leave empty and use the file name below.")]
        [SerializeField] VideoClip videoClip;
        [Tooltip("File inside Assets/StreamingAssets/Videolar/. Empty = no video.")]
        [SerializeField] string videoFileName;
        [Tooltip("Shown instead of the video when there's none.")]
        [SerializeField] GameObject placeholder;

        [Header("Altyazı ve Başlıklar")]
        [SerializeField] DialogueAsset subtitles;
        [SerializeField] Text titleText;
        [TextArea] [SerializeField] string openingTitle;
        [TextArea] [SerializeField] string closingTitle;
        [SerializeField] float titleDuration = 3f;

        [Header("Sonra")]
        [SerializeField] string nextScene;
        [SerializeField] string nextSpawn;

        [Header("Kayıt")]
        [SerializeField] string locationName = "Sinematik";
        [SerializeField] int floor;

        bool hasVideo;
        bool videoFinished;
        bool finished;

        IEnumerator Start()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (!GameSession.IsActive)
                GameSession.StartDebugSession(sceneName);

            // Saving here means quitting mid-cutscene and continuing replays it.
            var data = GameSession.Data;
            data.sceneName = sceneName;
            data.locationName = locationName;
            data.floor = floor;
            data.hasPosition = false;
            GameSession.Save();

            titleText.gameObject.SetActive(false);
            yield return PrepareVideo();
            if (placeholder != null)
                placeholder.SetActive(!hasVideo);

            if (!string.IsNullOrEmpty(openingTitle))
                yield return ShowTitle(openingTitle);

            if (hasVideo)
                videoPlayer.Play();

            bool subtitlesDone = false;
            DialogueRunner.PlayTimed(subtitles, () => subtitlesDone = true);
            while (!subtitlesDone || (hasVideo && !videoFinished))
                yield return null;

            if (!string.IsNullOrEmpty(closingTitle))
                yield return ShowTitle(closingTitle);

            Finish();
        }

        void Update()
        {
            // Short grace period so the key press that started the scene doesn't skip it immediately.
            if (!finished && Time.timeSinceLevelLoad > 0.5f && GameInput.SkipPressed)
                Finish();
        }

        IEnumerator PrepareVideo()
        {
            if (videoPlayer == null)
                yield break;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (videoClip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
            }
            else
            {
                if (string.IsNullOrEmpty(videoFileName))
                    yield break;

                var path = Path.Combine(Application.streamingAssetsPath, "Videolar", videoFileName);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    Debug.Log($"[Sinematik] Video yok ya da boş ({path}); yer tutucu gösteriliyor.");
                    yield break;
                }
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = path;
            }

            bool failed = false;
            videoPlayer.errorReceived += (_, message) =>
            {
                failed = true;
                Debug.LogWarning($"[Sinematik] Video oynatılamadı: {message}");
            };
            videoPlayer.loopPointReached += _ => videoFinished = true;
            videoPlayer.Prepare();

            for (float t = 0f; t < 10f && !videoPlayer.isPrepared && !failed; t += Time.unscaledDeltaTime)
                yield return null;

            hasVideo = videoPlayer.isPrepared && !failed;
        }

        IEnumerator ShowTitle(string title)
        {
            titleText.text = title;
            titleText.gameObject.SetActive(true);

            const float fade = 0.8f;
            for (float t = 0f; t < titleDuration; t += Time.unscaledDeltaTime)
            {
                float alpha = Mathf.Min(1f, t / fade, (titleDuration - t) / fade);
                var c = titleText.color;
                titleText.color = new Color(c.r, c.g, c.b, alpha);
                yield return null;
            }
            titleText.gameObject.SetActive(false);
        }

        void Finish()
        {
            if (finished)
                return;
            finished = true;

            StopAllCoroutines();
            if (DialogueRunner.Instance != null)
                DialogueRunner.Instance.Stop();
            if (videoPlayer != null)
                videoPlayer.Stop();

            if (string.IsNullOrEmpty(nextScene) || nextScene == SceneNames.MainMenu)
            {
                GameSession.End();
                ScreenFader.FadeToScene(SceneNames.MainMenu);
            }
            else
            {
                GameSession.TravelTo(nextScene, nextSpawn);
            }
        }
    }
}
