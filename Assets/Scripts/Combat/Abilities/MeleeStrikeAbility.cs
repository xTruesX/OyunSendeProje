using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Yakın Saldırı: one or more strikes in front of the enemy, each with a step forward.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Yakın Saldırı", fileName = "YakinSaldiri")]
    public class MeleeStrikeAbility : EnemyAbility
    {
        [Header("Vuruş")]
        public float damage = 10f;
        [Min(1)] public int hits = 1;
        [Tooltip("How far in front of the enemy the hit lands.")]
        public float reach = 0.9f;
        public float radius = 0.8f;
        [Tooltip("Steps forward with each strike.")]
        public float stepDistance = 0.5f;
        [Tooltip("Warning time for the 2nd, 3rd… hit of a combo.")]
        public float timeBetweenHits = 0.35f;
        public float knockback = 4f;

        public override IEnumerator Perform(EnemyController enemy)
        {
            for (int i = 0; i < hits; i++)
            {
                var dir = enemy.DirectionToPlayer;
                var warning = enemy.TelegraphCircle(enemy.BodyCenter + dir * reach, radius, telegraphColor);
                enemy.Flash(new Color(1f, 0.5f, 0.4f), i == 0 ? windup : timeBetweenHits);
                yield return Wait(i == 0 ? windup : timeBetweenHits);
                Object.Destroy(warning);

                enemy.SetVelocity(dir * stepDistance / 0.1f);
                yield return Wait(0.1f);
                enemy.SetVelocity(Vector2.zero);

                var center = enemy.BodyCenter + dir * reach;
                enemy.HitPlayer(center, radius, damage, knockback);
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                CombatFx.Burst(CombatFx.Circle, center, radius, radius * 2.2f, new Color(1f, 0.85f, 0.7f, 0.6f), 0.15f, 60, angle);
            }
            yield return Wait(recovery);
        }
    }
}
