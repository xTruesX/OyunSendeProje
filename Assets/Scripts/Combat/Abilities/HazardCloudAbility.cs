using System.Collections;
using System.Collections.Generic;
using Boruhan.Story;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Zehir Bulutu: leaves lingering clouds around the player that hurt and slow while standing in them.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Zehir Bulutu", fileName = "ZehirBulutu")]
    public class HazardCloudAbility : EnemyAbility
    {
        [Header("Bulut")]
        [Min(1)] public int count = 2;
        public float radius = 1.6f;
        [Tooltip("How far from the player the extra clouds may land. The first lands on the player.")]
        public float spread = 3f;
        public float lifetime = 6f;
        public float damagePerSecond = 8f;
        [Range(0.1f, 1f)] public float slowMultiplier = 0.8f;
        [Tooltip("Players with this flag ignore the damage.")]
        public string immunityFlag = "zehir_bagisikligi";
        public Color cloudColor = new Color(0.45f, 0.8f, 0.3f, 0.4f);

        public override IEnumerator Perform(EnemyController enemy)
        {
            var spots = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                var spot = (Vector2)enemy.Player.transform.position + (i == 0 ? Vector2.zero : Random.insideUnitCircle * spread);
                spots.Add(spot);
                enemy.TelegraphCircle(spot, radius, telegraphColor);
            }
            enemy.Flash(cloudColor, windup);
            yield return Wait(windup);

            foreach (var spot in spots)
            {
                var cloud = CombatFx.Shape(CombatFx.Circle, spot, Vector2.one * radius * 2f, cloudColor, -45);
                cloud.name = "ZehirBulutu";
                var col = cloud.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                cloud.AddComponent<HazardZone>().Configure(damagePerSecond, slowMultiplier, immunityFlag, lifetime);
            }
            yield return Wait(recovery);
        }
    }
}
