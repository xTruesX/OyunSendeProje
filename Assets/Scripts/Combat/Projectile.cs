using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>An enemy shot. Hurts the player, breaks on walls, and can be cut down with the sword.</summary>
    public class Projectile : MonoBehaviour
    {
        EnemyController owner;
        Rigidbody2D body;
        Vector2 velocity;
        float damage;
        float dieAt;

        public static Projectile Spawn(EnemyController owner, Vector2 position, Vector2 velocity, float damage, float size, Color color, Sprite sprite, float lifetime)
        {
            var go = new GameObject("Mermi");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : CombatFx.Circle;
            sr.color = color;
            sr.sortingOrder = 50;
            var bounds = sr.sprite.bounds.size;
            go.transform.localScale = new Vector3(size / bounds.x, size / bounds.y, 1f);

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = Mathf.Min(bounds.x, bounds.y) * 0.4f;

            var p = go.AddComponent<Projectile>();
            p.owner = owner;
            p.body = body;
            p.velocity = velocity;
            p.damage = damage;
            p.dieAt = Time.time + lifetime;
            return p;
        }

        void FixedUpdate()
        {
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
            if (Time.time > dieAt)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                if (health.Team != Team.Player)
                    return;
                health.Damage(new DamageInfo { amount = damage, source = body.position, knockback = 3f, parriable = true, attacker = owner });
                Break();
                return;
            }
            if (!other.isTrigger)
                Break();
        }

        public void Break()
        {
            var sr = GetComponent<SpriteRenderer>();
            CombatFx.Burst(CombatFx.Circle, transform.position, 0.3f, 0.8f, sr.color, 0.2f);
            Destroy(gameObject);
        }
    }
}
