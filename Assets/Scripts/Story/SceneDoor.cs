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
        [Tooltip("Optional: while locked, lead here instead of showing the locked dialogue.")]
        [SerializeField] string lockedTargetScene;
        [SerializeField] string lockedTargetSpawn;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || DialogueRunner.IsBusy || ScreenFader.IsBusy)
                return;

            bool locked = !string.IsNullOrEmpty(requiredFlag) && !GameSession.HasFlag(requiredFlag);
            if (locked && string.IsNullOrEmpty(lockedTargetScene))
            {
                DialogueRunner.Play(lockedDialogue);
                return;
            }

            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
                player.InputEnabled = false;

            if (locked)
                GameSession.TravelTo(lockedTargetScene, lockedTargetSpawn);
            else
                GameSession.TravelTo(targetScene, targetSpawn);
        }

        // A door that opens under the player (e.g. a rift appearing while the reward text plays) works as
        // soon as nothing is in the way, without having to step off and back on.
        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || DialogueRunner.IsBusy || ScreenFader.IsBusy)
                return;
            if (!string.IsNullOrEmpty(requiredFlag) && !GameSession.HasFlag(requiredFlag))
                return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.InputEnabled)
                return;
            player.InputEnabled = false;
            GameSession.TravelTo(targetScene, targetSpawn);
        }

        void OnDrawGizmos() =>
            ZoneGizmo.Draw(this, new Color(0.3f, 1f, 0.45f), $"Kapı → {targetScene}" + (string.IsNullOrEmpty(targetSpawn) ? "" : $" ({targetSpawn})")
                + (string.IsNullOrEmpty(lockedTargetScene) ? "" : $"\nKilitliyken → {lockedTargetScene}"));
    }
}
