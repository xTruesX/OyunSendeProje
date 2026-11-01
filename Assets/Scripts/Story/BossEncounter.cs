using System.Collections;
using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>
    /// When the boss dies: clears its summons, plays the victory dialogue, sets the defeated flag and drops
    /// its reward item where it fell. The player has to pick the item up (that grants the power and, in the
    /// arenas, opens the way down). A defeated boss doesn't come back; an uncollected reward waits for you.
    /// </summary>
    public class BossEncounter : MonoBehaviour
    {
        [SerializeField] EnemyController boss;
        [SerializeField] string defeatedFlag;
        [SerializeField] DialogueAsset victoryDialogue;

        [Header("Ödül")]
        [Tooltip("An inactive item in the scene. It appears where the boss falls and must be picked up.")]
        [SerializeField] Pickup rewardItem;

        [Header("Sonra")]
        [Tooltip("Optional: go here after the victory (and after the reward is picked up, if there is one).")]
        [SerializeField] string nextScene;
        [SerializeField] string nextSpawn;

        Vector2 fallenAt;

        void Start()
        {
            if (GameSession.HasFlag(defeatedFlag))
            {
                if (boss != null)
                    Destroy(boss.gameObject);
                // Left before picking the reward up: it is still lying there.
                if (rewardItem != null && !rewardItem.IsCollected)
                    rewardItem.gameObject.SetActive(true);
                return;
            }
            if (boss != null)
            {
                boss.Health.Died += () =>
                {
                    fallenAt = boss.Position;
                    StartCoroutine(Victory());
                };
            }
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
            if (rewardItem != null)
                Drop();
            if (RoomManager.Instance != null)
                RoomManager.Instance.SaveGame();

            if (string.IsNullOrEmpty(nextScene))
                yield break;
            while (rewardItem != null && !rewardItem.IsCollected)
                yield return null;
            while (DialogueRunner.IsBusy)
                yield return null;
            GameSession.TravelTo(nextScene, nextSpawn);
        }

        void Drop()
        {
            // Where the boss fell, unless that spot is inside a wall.
            bool free = true;
            foreach (var hit in Physics2D.OverlapCircleAll(fallenAt, 0.3f))
            {
                if (!hit.isTrigger && hit.GetComponentInParent<Health>() == null)
                    free = false;
            }
            if (free)
                rewardItem.transform.position = fallenAt;
            rewardItem.gameObject.SetActive(true);
            CombatFx.Burst(CombatFx.Circle, rewardItem.transform.position, 0.5f, 3f, new Color(1f, 0.85f, 0.35f, 0.8f), 0.5f, 70);
        }
    }
}
