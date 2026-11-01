using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Yanılsama: makes see-through copies of itself that fight weakly and break in one hit, then may swap places with one.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Yanılsama", fileName = "Yanilsama")]
    public class SummonIllusionsAbility : EnemyAbility
    {
        [Header("Yanılsama")]
        [Min(1)] public int count = 2;
        public float radius = 2.5f;
        public float lifetime = 10f;
        [Tooltip("Trade places with one of the copies so the player loses track of the real one.")]
        public bool swapWithCopy = true;
        [Header("Şifa Küresi")]
        [Tooltip("Chance (0-1) that each copy drops a health orb when killed.")]
        [Range(0f, 1f)] public float healthOrbChance;
        public HealthOrb healthOrb;

        public override IEnumerator Perform(EnemyController enemy)
        {
            var warning = enemy.TelegraphCircle(enemy.Position, radius, telegraphColor);
            enemy.Flash(new Color(0.8f, 0.6f, 1f), windup);
            yield return Wait(windup);
            Object.Destroy(warning);

            EnemyController last = null;
            float start = Random.value * 360f;
            for (int i = 0; i < count; i++)
            {
                var offset = (Vector2)(Quaternion.Euler(0f, 0f, start + i * 360f / count) * Vector2.right) * radius;
                var spot = enemy.Position + offset;
                if (!enemy.IsFree(spot))
                    spot = enemy.Position + offset * 0.4f;

                var copy = Object.Instantiate(enemy.gameObject, spot, Quaternion.identity).GetComponent<EnemyController>();
                copy.name = enemy.name + " (Yanılsama)";
                copy.MakeIllusion(lifetime);
                copy.DropOnDeath = healthOrb;
                copy.DropChance = healthOrbChance;
                CombatFx.Burst(CombatFx.Circle, spot + Vector2.up * 0.5f, 0.3f, 1.5f, new Color(0.8f, 0.6f, 1f, 0.6f), 0.3f);
                last = copy;
            }

            if (swapWithCopy && last != null)
            {
                var here = enemy.Position;
                var there = last.Position;
                last.TryMoveTo(here);
                enemy.TryMoveTo(there);
            }
            yield return Wait(recovery);
        }
    }
}
