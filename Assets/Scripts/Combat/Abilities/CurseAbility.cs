using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Korku: a wave of dread that slows the player (and can hurt a little) if they're within range.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Korku (Yavaşlatma)", fileName = "Korku")]
    public class CurseAbility : EnemyAbility
    {
        [Header("Korku")]
        public float range = 6f;
        [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;
        public float slowDuration = 3f;
        public float damage;

        public override IEnumerator Perform(EnemyController enemy)
        {
            var warning = enemy.TelegraphCircle(enemy.Position, range, telegraphColor);
            enemy.Flash(new Color(0.6f, 0.3f, 0.8f), windup);
            yield return Wait(windup);
            Object.Destroy(warning);

            CombatFx.Burst(CombatFx.Circle, enemy.Position, 1f, range * 2f, new Color(0.5f, 0.2f, 0.7f, 0.5f), 0.4f, -40);
            if (enemy.DistanceToPlayer <= range)
            {
                enemy.Player.ApplySlow(slowMultiplier, slowDuration);
                if (damage > 0f)
                    enemy.HitPlayer(enemy.PlayerCenter, 1f, damage, 0f, false);
            }
            yield return Wait(recovery);
        }
    }
}
