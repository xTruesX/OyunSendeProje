using System.Collections;
using Boruhan.Gameplay;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Yer Sarsma: slams the ground around itself, optionally in growing waves.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Yer Sarsma", fileName = "YerSarsma")]
    public class AreaSlamAbility : EnemyAbility
    {
        [Header("Sarsma")]
        public float damage = 16f;
        public float radius = 2f;
        [Min(1)] public int waves = 1;
        public float waveInterval = 0.45f;
        [Tooltip("Each wave is this much bigger than the last.")]
        public float waveGrowth = 1.4f;
        public float knockback = 7f;

        public override IEnumerator Perform(EnemyController enemy)
        {
            float r = radius;
            for (int w = 0; w < waves; w++)
            {
                var warning = enemy.TelegraphCircle(enemy.Position, r, telegraphColor);
                enemy.Flash(new Color(1f, 0.5f, 0.3f), w == 0 ? windup : waveInterval);
                yield return Wait(w == 0 ? windup : waveInterval);
                Object.Destroy(warning);

                enemy.HitPlayer(enemy.Position, r, damage, knockback);
                CombatFx.Burst(CombatFx.Circle, enemy.Position, r * 1.6f, r * 2.3f, new Color(1f, 0.8f, 0.5f, 0.55f), 0.3f, -40);
                var cam = Object.FindAnyObjectByType<CameraFollow>();
                if (cam != null)
                    cam.Shake(0.2f);
                r *= waveGrowth;
            }
            yield return Wait(recovery);
        }
    }
}
