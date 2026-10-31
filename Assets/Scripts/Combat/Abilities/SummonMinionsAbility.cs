using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Çağırma: calls other enemies to its side (picked at random from the list), up to a limit alive at once.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Çağırma", fileName = "Cagirma")]
    public class SummonMinionsAbility : EnemyAbility
    {
        [Header("Çağırma")]
        public List<EnemyDefinition> minions = new List<EnemyDefinition>();
        [Min(1)] public int count = 2;
        [Tooltip("Never more than this many of its summons alive at once.")]
        [Min(1)] public int maxAlive = 4;
        public float radius = 2.5f;

        public override IEnumerator Perform(EnemyController enemy)
        {
            enemy.Minions.RemoveAll(m => m == null || m.Health.IsDead);
            int n = Mathf.Min(count, maxAlive - enemy.Minions.Count);
            if (minions.Count == 0 || n <= 0)
                yield break;

            var spots = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                var spot = enemy.Position + (Vector2)(Quaternion.Euler(0f, 0f, i * 360f / n + Random.value * 40f) * Vector2.right) * radius;
                spots.Add(enemy.IsFree(spot) ? spot : enemy.Position);
                enemy.TelegraphCircle(spot, 0.7f, telegraphColor);
            }
            enemy.Flash(new Color(0.7f, 0.4f, 0.9f), windup);
            yield return Wait(windup);

            foreach (var spot in spots)
            {
                var minion = EnemyController.Spawn(minions[Random.Range(0, minions.Count)], spot);
                minion.ForceAggro();
                enemy.Minions.Add(minion);
                CombatFx.Burst(CombatFx.Circle, spot + Vector2.up * 0.4f, 0.3f, 1.4f, new Color(0.7f, 0.4f, 0.9f, 0.6f), 0.3f);
            }
            yield return Wait(recovery);
        }
    }
}
