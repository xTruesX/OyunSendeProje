using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boruhan.Story
{
    /// <summary>Small actions to hook up to UnityEvents (e.g. an Interactable's On Finished).</summary>
    public class RoomUtility : MonoBehaviour
    {
        /// <summary>Restarts the current room from the entrance: enemies return, health is full.</summary>
        public void ReloadRoom()
        {
            var spawn = GameSession.Data != null ? GameSession.Data.spawnId : null;
            GameSession.TravelTo(SceneManager.GetActiveScene().name, spawn);
        }

        /// <summary>Kut Ocağı: heal, make the nearest spawn point the respawn point, and save.</summary>
        public void RestAtShrine()
        {
            HealPlayer();
            var player = GameObject.FindWithTag("Player");
            if (player != null && GameSession.Data != null)
            {
                SpawnPoint nearest = null;
                float best = float.MaxValue;
                foreach (var spawn in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude))
                {
                    float d = Vector2.Distance(spawn.transform.position, player.transform.position);
                    if (d < best)
                    {
                        best = d;
                        nearest = spawn;
                    }
                }
                if (nearest != null)
                    GameSession.Data.spawnId = nearest.id;
            }
            if (RoomManager.Instance != null)
                RoomManager.Instance.SaveGame();
        }

        public void HealPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null && player.TryGetComponent<Health>(out var health))
            {
                health.HealFull();
                CombatFx.Burst(CombatFx.Circle, (Vector2)player.transform.position + Vector2.up * 0.5f, 0.5f, 2f, new Color(0.5f, 1f, 0.6f, 0.7f), 0.4f, 70);
            }
        }
    }
}
