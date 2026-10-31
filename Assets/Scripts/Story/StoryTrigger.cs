using System.Collections;
using Boruhan.Core;
using Boruhan.Dialogue;
using UnityEngine;
using UnityEngine.Events;

namespace Boruhan.Story
{
    /// <summary>
    /// Plays a dialogue when the player walks into this trigger (or when the scene starts).
    /// Set Once Flag so it only ever plays one time per save.
    /// </summary>
    public class StoryTrigger : MonoBehaviour
    {
        [SerializeField] DialogueAsset dialogue;
        [Tooltip("Play as soon as the scene starts instead of waiting for the player to walk in.")]
        [SerializeField] bool playOnStart;
        [SerializeField] float startDelay = 0.6f;
        [Tooltip("Only plays if this flag is set. Empty = no requirement.")]
        [SerializeField] string requiredFlag;
        [Tooltip("Set when the dialogue finishes. Once set, this trigger never plays again. Empty = plays every time.")]
        [SerializeField] string onceFlag;

        public UnityEvent onStarted;
        public UnityEvent onFinished;

        bool playing;

        IEnumerator Start()
        {
            if (!playOnStart)
                yield break;
            yield return new WaitForSeconds(startDelay);
            yield return TryPlay();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                StartCoroutine(TryPlay());
        }

        IEnumerator TryPlay()
        {
            if (playing || !CanPlay())
                yield break;

            playing = true;
            while (DialogueRunner.IsBusy || ScreenFader.IsBusy)
                yield return null;

            onStarted.Invoke();
            DialogueRunner.Play(dialogue, () =>
            {
                GameSession.SetFlag(onceFlag);
                playing = false;
                onFinished.Invoke();
            });
        }

        bool CanPlay()
        {
            if (!string.IsNullOrEmpty(requiredFlag) && !GameSession.HasFlag(requiredFlag))
                return false;
            return !GameSession.HasFlag(onceFlag);
        }
    }
}
