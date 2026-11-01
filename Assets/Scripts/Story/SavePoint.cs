using Boruhan.Combat;
using Boruhan.Dialogue;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>
    /// Marks a save point (Kut Ocağı). While a boss fight is in progress it can't be used:
    /// interacting only shows the message below.
    /// </summary>
    public class SavePoint : MonoBehaviour
    {
        [TextArea] [SerializeField] string blockedMessage = "Kut Ocağı'nın ateşi titriyor...\nSavaş sürerken burada dinlenemezsin.";

        /// <summary>Returns true (and shows the message) if the save point is blocked right now.</summary>
        public bool TryBlock()
        {
            if (!BossFight.InProgress)
                return false;
            DialogueRunner.PlayText(null, blockedMessage);
            return true;
        }
    }
}
