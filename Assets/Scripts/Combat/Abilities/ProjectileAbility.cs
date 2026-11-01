using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Mermi: fires shots at the player – single, fan-shaped spreads, or several bursts.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Mermi", fileName = "Mermi")]
    public class ProjectileAbility : EnemyAbility
    {
        [Header("Mermi")]
        public float damage = 8f;
        [Min(1)] public int count = 1;
        [Tooltip("Total angle the shots of one burst are spread over.")]
        public float spreadAngle;
        [Min(1)] public int bursts = 1;
        public float burstInterval = 0.3f;
        public float speed = 7f;
        public float size = 0.35f;
        public float lifetime = 4f;
        public Color color = new Color(0.6f, 0.95f, 1f);
        [Tooltip("Optional image for the shot. Empty = a glowing ball.")]
        public Sprite sprite;

        public override IEnumerator Perform(EnemyController enemy)
        {
            var glow = enemy.TelegraphCircle(enemy.BodyCenter, 0.6f, new Color(color.r, color.g, color.b, 0.35f));
            enemy.Flash(color, windup);
            yield return Wait(windup);
            Object.Destroy(glow);

            for (int b = 0; b < bursts; b++)
            {
                var dir = (enemy.PlayerCenter - enemy.BodyCenter).normalized;
                for (int i = 0; i < count; i++)
                {
                    float angle = count > 1 ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, i / (float)(count - 1)) : 0f;
                    var shotDir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * dir);
                    Projectile.Spawn(enemy, enemy.BodyCenter + shotDir * 0.4f, shotDir * speed, damage * enemy.DamageMultiplier, size, color, sprite, lifetime);
                }
                if (b < bursts - 1)
                    yield return Wait(burstInterval);
            }
            yield return Wait(recovery);
        }
    }
}
