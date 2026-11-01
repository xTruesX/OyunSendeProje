using Boruhan.Combat;
using Boruhan.Core;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>A cracked wall that only a heavy attack with the right power (Demir Eldiven) can break.</summary>
    [RequireComponent(typeof(Health))]
    public class BreakableWall : MonoBehaviour
    {
        [SerializeField] string requiredFlag = StoryFlags.IronGauntlet;
        [Tooltip("Unique within the game, so it stays broken.")]
        [SerializeField] string id;

        string BrokenFlag => "kirik_duvar_" + id;

        void Start()
        {
            if (GameSession.HasFlag(BrokenFlag))
            {
                Destroy(gameObject);
                return;
            }

            var health = GetComponent<Health>();
            health.Setup(Team.Enemy, 1f);
            health.AddFilter(Filter);
            health.Died += Break;
        }

        bool Filter(ref DamageInfo info)
        {
            if (info.heavy && GameSession.HasFlag(requiredFlag))
                return true;
            CombatFx.Burst(CombatFx.Circle, transform.position + Vector3.up * 0.5f, 0.3f, 0.9f, new Color(1f, 1f, 1f, 0.5f), 0.15f, 70);
            return false;
        }

        void Break()
        {
            GameSession.SetFlag(BrokenFlag);
            CombatFx.Burst(CombatFx.Circle, transform.position + Vector3.up * 0.5f, 0.5f, 3f, new Color(0.7f, 0.65f, 0.6f, 0.8f), 0.4f, 70);
            Destroy(gameObject);
        }
    }
}
