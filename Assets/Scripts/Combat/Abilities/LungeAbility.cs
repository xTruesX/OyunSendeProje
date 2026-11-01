using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Atılma: aims, shows a warning strip, then dashes along it hurting the player on contact.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Atılma", fileName = "Atilma")]
    public class LungeAbility : EnemyAbility
    {
        [Header("Atılma")]
        public float damage = 12f;
        public float distance = 3.5f;
        public float speed = 12f;
        public float hitRadius = 0.6f;
        public float knockback = 6f;

        public override IEnumerator Perform(EnemyController enemy)
        {
            var dir = enemy.DirectionToPlayer;
            var warning = enemy.TelegraphLine(enemy.Position, dir, distance, 0.9f * enemy.Definition.scale, telegraphColor);
            enemy.Flash(new Color(1f, 0.5f, 0.4f), windup);
            yield return Wait(windup);
            Object.Destroy(warning);

            bool hit = false;
            for (float t = 0f; t < distance / speed; t += Time.deltaTime)
            {
                enemy.SetVelocity(dir * speed);
                if (!hit)
                    hit = enemy.HitPlayer(enemy.BodyCenter, hitRadius, damage, knockback);
                yield return null;
            }
            enemy.SetVelocity(Vector2.zero);
            yield return Wait(recovery);
        }
    }
}
