using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>
    /// Şifa Küresi: a green orb some summoned minions drop. Heals on touch, drifts to the player when
    /// close and fades away after a while. Left alone while the player is at full health.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HealthOrb : MonoBehaviour
    {
        [SerializeField] float healAmount = 15f;
        [Tooltip("Seconds before it disappears (it blinks for the last 3).")]
        [SerializeField] float lifetime = 12f;
        [Tooltip("Within this distance it flies to the player.")]
        [SerializeField] float magnetRange = 1.8f;
        [SerializeField] float magnetSpeed = 6f;
        [SerializeField] AudioClip pickupSound;
        [Range(0f, 1f)] [SerializeField] float volume = 0.8f;

        SpriteRenderer sprite;
        Vector3 baseScale;
        float age;
        bool collected;

        /// <summary>Drops a copy of this orb at a position.</summary>
        public HealthOrb Drop(Vector2 position)
        {
            var orb = Instantiate(this, position, Quaternion.identity);
            orb.name = name;
            return orb;
        }

        void Awake()
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
            baseScale = transform.localScale;
            CombatFx.Burst(CombatFx.Circle, transform.position, 0.2f, 1f, new Color(0.4f, 1f, 0.5f, 0.6f), 0.25f, 70);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
            transform.localScale = baseScale * (1f + 0.1f * Mathf.Sin(age * 5f));
            if (sprite != null)
                sprite.enabled = lifetime - age > 3f || Mathf.Repeat(age, 0.25f) < 0.17f;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;
            var health = player.GetComponentInParent<Health>();
            if (health == null || health.IsDead || health.Current >= health.Max)
                return;
            var target = player.transform.position + Vector3.up * 0.5f;
            if (Vector2.Distance(transform.position, target) < magnetRange)
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            if (collected || !other.CompareTag("Player"))
                return;
            var health = other.GetComponentInParent<Health>();
            if (health == null || health.IsDead || health.Current >= health.Max)
                return;

            collected = true;
            health.Heal(healAmount);
            CombatFx.Burst(CombatFx.Circle, transform.position, 0.3f, 1.8f, new Color(0.4f, 1f, 0.5f, 0.8f), 0.35f, 70);
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, Camera.main != null ? Camera.main.transform.position : transform.position, volume);
            Destroy(gameObject);
        }
    }
}
