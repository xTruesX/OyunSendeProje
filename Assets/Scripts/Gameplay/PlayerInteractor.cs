using System.Collections.Generic;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Story;
using Boruhan.UI;
using UnityEngine;

namespace Boruhan.Gameplay
{
    /// <summary>Finds the Interactable in front of the player and triggers it on E / Enter / Space.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float reach = 0.6f;
        [SerializeField] float radius = 0.6f;
        [Tooltip("Roughly the middle of the character's body, relative to the feet.")]
        [SerializeField] Vector2 bodyOffset = new Vector2(0f, 0.5f);

        readonly List<Collider2D> hits = new List<Collider2D>();
        PlayerController player;

        void Awake() => player = GetComponent<PlayerController>();

        void Update()
        {
            var prompt = InteractPrompt.Instance;
            bool canInteract = player.InputEnabled && !DialogueRunner.IsBusy && !ScreenFader.IsBusy && Time.timeScale > 0f;
            var target = canInteract ? FindTarget() : null;

            if (prompt != null)
            {
                if (target != null)
                    prompt.Show(target.Prompt);
                else
                    prompt.Hide();
            }

            if (target != null && GameInput.SubmitPressed)
                target.Interact();
        }

        Interactable FindTarget()
        {
            Vector2 center = (Vector2)transform.position + bodyOffset + player.Facing * reach;
            int count = Physics2D.OverlapCircle(center, radius, ContactFilter2D.noFilter, hits);

            Interactable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var interactable = hits[i].GetComponentInParent<Interactable>();
                if (interactable == null || !interactable.isActiveAndEnabled)
                    continue;

                float distance = ((Vector2)hits[i].bounds.center - center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = interactable;
                    bestDistance = distance;
                }
            }
            return best;
        }

        void OnDrawGizmosSelected()
        {
            var facing = Application.isPlaying ? player.Facing : Vector2.down;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)transform.position + bodyOffset + facing * reach, radius);
        }
    }
}
