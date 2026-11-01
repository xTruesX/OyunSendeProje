using Boruhan.Core;
using Boruhan.Dialogue;
using UnityEngine;
using UnityEngine.Events;

namespace Boruhan.Story
{
    /// <summary>Something the player can face and press E on: a character, an item, a sign.</summary>
    public class Interactable : MonoBehaviour, IInteractable
    {
        [Tooltip("Shown in the prompt, e.g. [E] Konuş")]
        [SerializeField] string prompt = "Bak";
        [SerializeField] DialogueAsset dialogue;

        [Header("Kilit")]
        [Tooltip("If set, the main dialogue only plays once this flag is set.")]
        [SerializeField] string requiredFlag;
        [SerializeField] DialogueAsset lockedDialogue;

        [Header("Tekrar")]
        [Tooltip("Set after the main dialogue finishes.")]
        [SerializeField] string completedFlag;
        [Tooltip("Played instead of the main dialogue once Completed Flag is set. Empty = replay the main one.")]
        [SerializeField] DialogueAsset repeatDialogue;

        public UnityEvent onFinished;

        public string Prompt => prompt;
        public bool CanInteract => isActiveAndEnabled;

        public void Interact()
        {
            if (TryGetComponent<SavePoint>(out var savePoint) && savePoint.TryBlock())
                return;

            if (!string.IsNullOrEmpty(requiredFlag) && !GameSession.HasFlag(requiredFlag))
            {
                DialogueRunner.Play(lockedDialogue);
                return;
            }

            if (repeatDialogue != null && GameSession.HasFlag(completedFlag))
            {
                DialogueRunner.Play(repeatDialogue);
                return;
            }

            DialogueRunner.Play(dialogue, () =>
            {
                GameSession.SetFlag(completedFlag);
                onFinished.Invoke();
            });
        }
    }
}
