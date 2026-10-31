using System;
using System.Collections;
using Boruhan.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Boruhan.Dialogue
{
    /// <summary>
    /// Shows dialogue as subtitles at the bottom of the screen. Lives on the DiyalogKutusu prefab.
    /// Play() waits for the player to press E/Enter/Space/click; PlayTimed() advances by itself (cutscenes).
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] GameObject box;
        [SerializeField] Text speakerText;
        [SerializeField] Text bodyText;
        [SerializeField] Image portrait;
        [SerializeField] GameObject continueIndicator;

        [Header("Görünüm")]
        [SerializeField] float charactersPerSecond = 45f;
        [SerializeField] Color textColor = new Color32(235, 228, 212, 255);
        [SerializeField] Color narrationColor = new Color32(196, 186, 168, 255);
        [Tooltip("Body text left padding when a portrait is shown / hidden.")]
        [SerializeField] float paddingWithPortrait = 200f;
        [SerializeField] float paddingWithoutPortrait = 48f;

        public static DialogueRunner Instance { get; private set; }

        /// <summary>True while a dialogue is on screen; the player can't move or interact.</summary>
        public static bool IsBusy => Instance != null && Instance.running;

        bool running;
        Coroutine routine;
        Action onComplete;

        void Awake()
        {
            Instance = this;
            box.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Play(DialogueAsset dialogue, Action onComplete = null) => Begin(dialogue, false, onComplete);

        public static void PlayTimed(DialogueAsset dialogue, Action onComplete = null) => Begin(dialogue, true, onComplete);

        static void Begin(DialogueAsset dialogue, bool timed, Action onComplete)
        {
            if (Instance == null || dialogue == null || dialogue.lines.Count == 0)
            {
                if (Instance == null && dialogue != null)
                    Debug.LogWarning($"[Diyalog] Sahnede DiyalogKutusu yok, '{dialogue.name}' gösterilemedi.");
                onComplete?.Invoke();
                return;
            }

            Instance.Stop();
            Instance.onComplete = onComplete;
            Instance.routine = Instance.StartCoroutine(Instance.Run(dialogue, timed));
        }

        /// <summary>Hides the box immediately without calling the completion callback.</summary>
        public void Stop()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
            running = false;
            box.SetActive(false);
        }

        IEnumerator Run(DialogueAsset dialogue, bool timed)
        {
            running = true;
            box.SetActive(true);

            foreach (var line in dialogue.lines)
            {
                ShowLine(line);
                GameSession.SetFlag(line.setFlag);
                continueIndicator.SetActive(false);

                // Waiting one frame first means the key press that started the dialogue doesn't skip line one.
                yield return null;

                string text = line.text ?? "";
                float shown = 0f;
                while (shown < text.Length)
                {
                    if (!timed && AdvancePressed())
                        break;
                    if (!IsPaused)
                        shown += charactersPerSecond * Time.unscaledDeltaTime;
                    SetVisibleCharacters(text, Mathf.Min((int)shown, text.Length));
                    yield return null;
                }
                SetVisibleCharacters(text, text.Length);

                if (timed)
                {
                    float duration = line.duration > 0f ? line.duration : 2f + text.Length * 0.055f;
                    float remaining = Mathf.Max(0f, duration - text.Length / charactersPerSecond);
                    for (float t = 0f; t < remaining; t += IsPaused ? 0f : Time.unscaledDeltaTime)
                        yield return null;
                }
                else
                {
                    continueIndicator.SetActive(true);
                    yield return null;
                    while (!AdvancePressed())
                        yield return null;
                }
            }

            box.SetActive(false);
            running = false;
            routine = null;

            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

        void ShowLine(DialogueLine line)
        {
            var speaker = line.speaker;
            bool hasPortrait = speaker != null && speaker.portrait != null;

            speakerText.gameObject.SetActive(speaker != null);
            if (speaker != null)
            {
                speakerText.text = speaker.displayName;
                speakerText.color = speaker.nameColor;
            }

            portrait.gameObject.SetActive(hasPortrait);
            if (hasPortrait)
                portrait.sprite = speaker.portrait;

            float padding = hasPortrait ? paddingWithPortrait : paddingWithoutPortrait;
            SetLeftPadding(speakerText.rectTransform, padding);
            SetLeftPadding(bodyText.rectTransform, padding);

            bodyText.fontStyle = speaker == null ? FontStyle.Italic : FontStyle.Normal;
            bodyText.color = speaker == null ? narrationColor : textColor;
        }

        void SetVisibleCharacters(string text, int count)
        {
            // The hidden part is kept but transparent, so words don't jump to the next line mid-typing.
            bodyText.text = count >= text.Length
                ? text
                : text.Substring(0, count) + "<color=#00000000>" + text.Substring(count) + "</color>";
        }

        static void SetLeftPadding(RectTransform rect, float padding)
        {
            rect.offsetMin = new Vector2(padding, rect.offsetMin.y);
        }

        static bool IsPaused => Time.timeScale == 0f;

        static bool AdvancePressed()
        {
            if (IsPaused)
                return false;
            return GameInput.SubmitPressed || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        }
    }
}
