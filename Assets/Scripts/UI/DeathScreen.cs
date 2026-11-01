using System.Collections;
using Boruhan.Audio;
using Boruhan.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Boruhan.UI
{
    /// <summary>
    /// Shown when Börühan dies: fades in over the scene, then Tekrar Dene reloads the last save
    /// (save point, pause-menu save or the last room change) and Ana Menü goes back to the menu.
    /// Lives on the HUD prefab.
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] Text messageText;
        [SerializeField] Button retryButton;
        [SerializeField] Button mainMenuButton;

        [Header("Zamanlama")]
        [Tooltip("Seconds between dying and the screen starting to appear (the death animation plays meanwhile).")]
        [SerializeField] float delay = 1.2f;
        [SerializeField] float fadeDuration = 1f;

        [Header("Yazı")]
        [Tooltip("One of these is shown under the title, picked at random.")]
        [TextArea] [SerializeField] string[] messages =
        {
            "Kurt düşer, ama yine kalkar.",
            "Ülgen Han hâlâ seni bekliyor.",
            "Erlik Han'ın gölgesi bu kez kazandı...",
            "Kut Ocağı'nın ateşi seni geri çağırıyor.",
        };

        [Header("Ses Efektleri")]
        [Tooltip("The moment Börühan falls.")]
        [SerializeField] AudioClip deathSound;
        [Tooltip("When the screen appears.")]
        [SerializeField] AudioClip screenSound;
        [Tooltip("Tekrar Dene pressed.")]
        [SerializeField] AudioClip retrySound;
        [Range(0f, 1f)] [SerializeField] float volume = 0.9f;

        public static DeathScreen Instance { get; private set; }

        /// <summary>True from the moment Börühan dies until the scene changes.</summary>
        public static bool IsOpen => Instance != null && Instance.open;

        bool open;
        bool leaving;
        AudioSource source;

        void Awake()
        {
            Instance = this;
            root.SetActive(false);
            retryButton.onClick.AddListener(Retry);
            mainMenuButton.onClick.AddListener(MainMenu);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Called by the player when Börühan dies.</summary>
        public void Show()
        {
            if (open)
                return;
            open = true;
            StartCoroutine(ShowRoutine());
        }

        IEnumerator ShowRoutine()
        {
            Play(deathSound);
            MusicManager.Stop(delay + fadeDuration);
            yield return new WaitForSecondsRealtime(delay);

            if (messageText != null && messages != null && messages.Length > 0)
                messageText.text = messages[Random.Range(0, messages.Length)];
            group.alpha = 0f;
            group.interactable = false;
            root.SetActive(true);
            Play(screenSound);
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / fadeDuration;
                yield return null;
            }
            group.alpha = 1f;
            group.interactable = true;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
        }

        void Retry()
        {
            if (leaving)
                return;
            leaving = true;
            group.interactable = false;
            PlayAcrossScenes(retrySound);
            GameSession.RetryFromLastSave();
        }

        void MainMenu()
        {
            if (leaving)
                return;
            leaving = true;
            group.interactable = false;
            GameSession.End();
            ScreenFader.FadeToScene(SceneNames.MainMenu);
        }

        void Play(AudioClip clip)
        {
            if (clip == null)
                return;
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(clip, volume);
        }

        // The scene starts fading out right away, so this sound gets its own object that outlives it.
        void PlayAcrossScenes(AudioClip clip)
        {
            if (clip == null)
                return;
            var go = new GameObject("TekrarDeneSesi");
            DontDestroyOnLoad(go);
            var s = go.AddComponent<AudioSource>();
            s.spatialBlend = 0f;
            s.PlayOneShot(clip, volume);
            Destroy(go, clip.length + 0.1f);
        }
    }
}
