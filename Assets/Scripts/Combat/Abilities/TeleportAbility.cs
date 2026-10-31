using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Işınlanma: fades out and reappears behind / near / away from the player, optionally followed by another move.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Işınlanma", fileName = "Isinlanma")]
    public class TeleportAbility : EnemyAbility
    {
        public enum Destination { BehindPlayer, NearPlayer, AwayFromPlayer }

        [Header("Işınlanma")]
        public Destination destination = Destination.BehindPlayer;
        public float distance = 1.5f;
        [Tooltip("Done right after appearing (e.g. a strike for a backstab). Doesn't need to be in the moveset list.")]
        public EnemyAbility followUp;

        public override IEnumerator Perform(EnemyController enemy)
        {
            for (float t = 0f; t < windup; t += Time.deltaTime)
            {
                enemy.SetAlpha(1f - t / Mathf.Max(0.01f, windup));
                yield return null;
            }

            CombatFx.Burst(CombatFx.Circle, enemy.BodyCenter, 0.4f, 1.4f, new Color(0.5f, 0.3f, 0.7f, 0.6f), 0.25f);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                if (enemy.TryMoveTo(Target(enemy, attempt)))
                    break;
            }
            CombatFx.Burst(CombatFx.Circle, enemy.BodyCenter, 0.4f, 1.4f, new Color(0.5f, 0.3f, 0.7f, 0.6f), 0.25f);

            for (float t = 0f; t < 0.15f; t += Time.deltaTime)
            {
                enemy.SetAlpha(t / 0.15f);
                yield return null;
            }
            enemy.SetAlpha(1f);

            if (followUp != null && followUp != this)
                yield return followUp.Perform(enemy);
            yield return Wait(recovery);
        }

        Vector2 Target(EnemyController enemy, int attempt)
        {
            var player = (Vector2)enemy.Player.transform.position;
            // Later attempts try other directions in case the first spot is inside a wall.
            var jitter = Quaternion.Euler(0f, 0f, attempt * 45f);
            switch (destination)
            {
                case Destination.BehindPlayer:
                    return player + (Vector2)(jitter * -enemy.Player.Facing) * distance;
                case Destination.AwayFromPlayer:
                    return enemy.Position + (Vector2)(jitter * -enemy.DirectionToPlayer) * distance;
                default:
                    return player + Random.insideUnitCircle.normalized * distance;
            }
        }
    }
}
