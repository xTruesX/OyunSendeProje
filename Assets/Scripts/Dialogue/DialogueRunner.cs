using System;
using System.Collections;
using System.Collections.Generic;
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

        [Header("Seslendirme")]
        [Range(0f, 1f)] [SerializeField] float voiceVolume = 1f;

        public static DialogueRunner Instance { get; private set; }

        /// <summary>True while a dialogue is on screen; the player can't move or interact.</summary>
        public static bool IsBusy => Instance != null && Instance.running;

        bool running;
        AudioSource voiceSource;
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

        /// <summary>Plays text written directly in an event: one dialogue line per text line.</summary>
        /// Voices, if given, go to the lines in order.
        public static void PlayText(DialogueSpeaker speaker, string text, Action onComplete = null, IList<AudioClip> voices = null)
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            foreach (var row in (text ?? "").Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(row))
                    continue;
                int i = asset.lines.Count;
                asset.lines.Add(new DialogueLine { speaker = speaker, text = row.Trim(), voice = voices != null && i < voices.Count ? voices[i] : null });
            }
            Play(asset, onComplete);
        }

        /// <summary>Shows a question with options (W/S or arrows to move, E/Enter/Space or 1-9 to pick).</summary>
        public static void Choose(string question, IList<string> options, Action<int> onChosen)
        {
            if (Instance == null || options == null || options.Count == 0)
            {
                onChosen?.Invoke(0);
                return;
            }
            Instance.Stop();
            Instance.routine = Instance.StartCoroutine(Instance.RunChoice(question, options, onChosen));
        }

        IEnumerator RunChoice(string question, IList<string> options, Action<int> onChosen)
        {
            running = true;
            box.SetActive(true);
            speakerText.gameObject.SetActive(false);
            portrait.gameObject.SetActive(false);
            continueIndicator.SetActive(false);
            SetLeftPadding(bodyText.rectTransform, paddingWithoutPortrait);
            bodyText.fontStyle = FontStyle.Normal;
            bodyText.color = textColor;

            int index = 0;
            yield return null;
            while (true)
            {
                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(question))
                    sb.Append(question).Append('\n');
                for (int i = 0; i < options.Count; i++)
                    sb.Append(i == index ? $"<color=#D4A84B>» {options[i]}</color>" : $"   {options[i]}").Append('\n');
                bodyText.text = sb.ToString();

                if (!IsPaused)
                {
                    var kb = Keyboard.current;
                    var pad = Gamepad.current;
                    if ((kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) || (pad != null && pad.dpad.up.wasPressedThisFrame))
                        index = (index + options.Count - 1) % options.Count;
                    if ((kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) || (pad != null && pad.dpad.down.wasPressedThisFrame))
                        index = (index + 1) % options.Count;
                    if (kb != null)
                    {
                        for (int i = 0; i < Mathf.Min(9, options.Count); i++)
                        {
                            if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                            {
                                index = i;
                                goto chosen;
                            }
                        }
                    }
                    if (GameInput.SubmitPressed)
                        break;
                }
                yield return null;
            }
            chosen:
            box.SetActive(false);
            running = false;
            routine = null;
            onChosen?.Invoke(index);
        }

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
            StopVoice();
        }

        /// <summary>Plays a line's recording, cutting off the previous one.</summary>
        void PlayVoice(AudioClip clip)
        {
            StopVoice();
            if (clip == null)
                return;
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
                voiceSource.spatialBlend = 0f;
            }
            voiceSource.clip = clip;
            voiceSource.volume = voiceVolume;
            voiceSource.Play();
        }

        void StopVoice()
        {
            if (voiceSource != null)
                voiceSource.Stop();
            voicePaused = false;
        }

        bool voicePaused;

        // The recording waits with the subtitles while the game is paused.
        void Update()
        {
            if (voiceSource == null)
                return;
            if (IsPaused && voiceSource.isPlaying)
            {
                voiceSource.Pause();
                voicePaused = true;
            }
            else if (!IsPaused && voicePaused)
            {
                voiceSource.UnPause();
                voicePaused = false;
            }
        }

        IEnumerator Run(DialogueAsset dialogue, bool timed)
        {
            running = true;
            box.SetActive(true);

            foreach (var line in dialogue.lines)
            {
                ShowLine(line);
                PlayVoice(line.voice);
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
                    // A recording longer than the line's time keeps the subtitle up until it ends.
                    if (line.voice != null)
                        duration = Mathf.Max(duration, line.voice.length + 0.3f);
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
            StopVoice();

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
