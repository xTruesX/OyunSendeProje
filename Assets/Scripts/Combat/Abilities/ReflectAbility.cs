using System.Collections;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Ayna: for a moment every blow it takes is thrown back at the attacker, stronger. Don't hit it while it shimmers.</summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman Yeteneği/Ayna (Yansıtma)", fileName = "Ayna")]
    public class ReflectAbility : EnemyAbility
    {
        [Header("Ayna")]
        public float duration = 1.5f;
        public float reflectMultiplier = 1.5f;

        public override IEnumerator Perform(EnemyController enemy)
        {
            enemy.Flash(new Color(0.6f, 1f, 1f), windup);
            yield return Wait(windup);

            enemy.Reflecting = true;
            enemy.ReflectMultiplier = reflectMultiplier;
            var mirror = enemy.Temporary(CombatFx.Shape(CombatFx.Circle, enemy.BodyCenter, Vector2.one * 1.8f * enemy.Definition.scale, new Color(0.6f, 1f, 1f, 0.3f), 40));
            for (float t = 0f; t < duration && enemy.Reflecting; t += Time.deltaTime)
            {
                enemy.Flash(Color.Lerp(Color.white, new Color(0.5f, 1f, 1f), Mathf.PingPong(t * 6f, 1f)), 0.05f);
                mirror.transform.position = enemy.BodyCenter;
                yield return null;
            }
            enemy.Reflecting = false;
            Object.Destroy(mirror);
            yield return Wait(recovery);
        }
    }
}
