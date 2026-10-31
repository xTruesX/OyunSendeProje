using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Kalkan: raises a guard and advances slowly. Light hits barely hurt; a heavy attack breaks it.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Kalkan", fileName = "Kalkan")]
    public class GuardAbility : EnemyAbility
    {
        [Header("Kalkan")]
        public float duration = 3f;
        [Range(0f, 1f)] public float damageTaken = 0.15f;
        [Range(0f, 1f)] public float moveSpeedMultiplier = 0.4f;

        public override IEnumerator Perform(EnemyController enemy)
        {
            enemy.Flash(new Color(0.6f, 0.7f, 1f), windup);
            yield return Wait(windup);

            enemy.Guarding = true;
            enemy.GuardDamageTaken = damageTaken;
            var shield = enemy.Temporary(CombatFx.Shape(CombatFx.Circle, enemy.BodyCenter, Vector2.one * 1.6f * enemy.Definition.scale, new Color(0.5f, 0.6f, 1f, 0.25f), 40));
            for (float t = 0f; t < duration && enemy.Guarding; t += Time.deltaTime)
            {
                shield.transform.position = enemy.BodyCenter;
                enemy.SetVelocity(enemy.DirectionToPlayer * enemy.Definition.moveSpeed * moveSpeedMultiplier);
                yield return null;
            }
            enemy.Guarding = false;
            Object.Destroy(shield);
            enemy.SetVelocity(Vector2.zero);
            yield return Wait(recovery);
        }
    }
}
