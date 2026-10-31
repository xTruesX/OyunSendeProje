using System.Collections;
using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>
    /// When the boss dies: clears its summons, plays the victory dialogue (its lines can grant powers via
    /// flags), sets the defeated flag and optionally moves on. A defeated boss doesn't come back.
    /// </summary>
    public class BossEncounter : MonoBehaviour
    {
        [SerializeField] EnemyController boss;
        [SerializeField] string defeatedFlag;
        [SerializeField] DialogueAsset victoryDialogue;
        [Tooltip("Optional: go here straight after the victory dialogue.")]
        [SerializeField] string nextScene;
        [SerializeField] string nextSpawn;

        void Start()
        {
            if (GameSession.HasFlag(defeatedFlag))
            {
                if (boss != null)
                    Destroy(boss.gameObject);
                return;
            }
            if (boss != null)
                boss.Health.Died += () => StartCoroutine(Victory());
        }

        IEnumerator Victory()
        {
            yield return new WaitForSeconds(1.2f);
            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                if (!enemy.Health.IsDead)
                    enemy.Health.Damage(new DamageInfo { amount = 9999f, source = enemy.Position });
            }

            bool done = false;
            DialogueRunner.Play(victoryDialogue, () => done = true);
            while (!done)
                yield return null;

            GameSession.SetFlag(defeatedFlag);
            if (RoomManager.Instance != null)
                RoomManager.Instance.SaveGame();
            if (!string.IsNullOrEmpty(nextScene))
                GameSession.TravelTo(nextScene, nextSpawn);
        }
    }
}
