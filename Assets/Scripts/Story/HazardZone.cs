using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Gameplay;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>An area that hurts and/or slows the player while inside: poison gas, bogs, lava heat.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HazardZone : MonoBehaviour
    {
        [SerializeField] float damagePerSecond = 6f;
        [Tooltip("1 = no slow.")]
        [Range(0.1f, 1f)] [SerializeField] float slowMultiplier = 1f;
        [Tooltip("With this story flag the damage is ignored (e.g. zehir_bagisikligi).")]
        [SerializeField] string immunityFlag;
        [Tooltip("Seconds until it disappears. 0 = permanent.")]
        [SerializeField] float lifetime;

        Health playerHealth;
        PlayerController player;
        float nextTick;

        public void Configure(float dps, float slow, string immunity, float seconds)
        {
            damagePerSecond = dps;
            slowMultiplier = slow;
            immunityFlag = immunity;
            lifetime = seconds;
        }

        void Start()
        {
            if (lifetime > 0f)
                Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;
            player = other.GetComponentInParent<PlayerController>();
            playerHealth = other.GetComponentInParent<Health>();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                player = null;
        }

        void Update()
        {
            if (player == null)
                return;
            if (slowMultiplier < 1f)
                player.ApplySlow(slowMultiplier, 0.15f);
            if (damagePerSecond <= 0f || GameSession.HasFlag(immunityFlag) || Time.time < nextTick)
                return;
            nextTick = Time.time + 0.5f;
            playerHealth.Damage(new DamageInfo { amount = damagePerSecond * 0.5f, source = player.transform.position });
        }
    }
}
