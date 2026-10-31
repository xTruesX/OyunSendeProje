using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>Walking into this trigger moves the player to another scene's SpawnPoint.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class SceneDoor : MonoBehaviour
    {
        [SerializeField] string targetScene;
        [Tooltip("Id of the SpawnPoint in the target scene.")]
        [SerializeField] string targetSpawn;

        [Header("Kilit")]
        [SerializeField] string requiredFlag;
        [Tooltip("Shown when the door is still locked.")]
        [SerializeField] DialogueAsset lockedDialogue;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || DialogueRunner.IsBusy || ScreenFader.IsBusy)
                return;

            if (!string.IsNullOrEmpty(requiredFlag) && !GameSession.HasFlag(requiredFlag))
            {
                DialogueRunner.Play(lockedDialogue);
                return;
            }

            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
                player.InputEnabled = false;

            GameSession.TravelTo(targetScene, targetSpawn);
        }
    }
}
