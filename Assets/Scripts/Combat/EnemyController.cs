using System.Collections;
using System.Collections.Generic;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using Boruhan.UI;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>
    /// Runs an enemy from its EnemyDefinition: walks to its preferred distance, picks abilities from its
    /// moveset, gets staggered and knocked back, guards / reflects when an ability says so.
    /// Abilities use the helper methods here (telegraphs, hitting the player, moving).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] EnemyDefinition definition;
        [SerializeField] SpriteRenderer bodyRenderer;

        Rigidbody2D body;
        float[] readyAt;
        Coroutine action;
        readonly List<GameObject> temporary = new List<GameObject>();
        bool aggro;
        float staggerUntil;
        float poiseDamage;
        Vector2 velocity;
        Vector2 knockback;
        float knockbackUntil;
        float strafeSign = 1f;
        float nextStrafeFlip;
        float nextThink;
        Color tint = Color.white;
        Color flashColor;
        float flashUntil;
        float alpha = 1f;

        public EnemyDefinition Definition => definition;
        public Health Health { get; private set; }
        public Health PlayerHealth { get; private set; }
        public PlayerController Player { get; private set; }
        public bool IsIllusion { get; private set; }
        /// <summary>Scales all damage this enemy deals (illusions hit softer).</summary>
        public float DamageMultiplier { get; private set; } = 1f;
        public bool IsStaggered => Time.time < staggerUntil;
        /// <summary>Enemies this one has summoned (Çağırma).</summary>
        public List<EnemyController> Minions { get; } = new List<EnemyController>();

        /// <summary>Creates an enemy at runtime (used for summons).</summary>
        public static EnemyController Spawn(EnemyDefinition definition, Vector2 position)
        {
            var go = new GameObject(definition.displayName);
            go.SetActive(false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Health>();
            var enemy = go.AddComponent<EnemyController>();
            enemy.definition = definition;
            enemy.bodyRenderer = sr;
            go.AddComponent<WorldHealthBar>();
            go.SetActive(true);
            return enemy;
        }

        public void ForceAggro() => Aggro();

        // Set by abilities.
        public bool Guarding { get; set; }
        public float GuardDamageTaken { get; set; } = 0.15f;
        public bool Reflecting { get; set; }
        public float ReflectMultiplier { get; set; } = 1f;

        public Vector2 Position => body.position;
        public Vector2 BodyCenter => body.position + Vector2.up * 0.5f * (definition != null ? definition.scale : 1f);
        public Vector2 PlayerCenter => Player != null ? (Vector2)Player.transform.position + Vector2.up * 0.5f : Position;
        public float DistanceToPlayer => Vector2.Distance(Position, Player != null ? (Vector2)Player.transform.position : Position);
        public Vector2 DirectionToPlayer
        {
            get
            {
                if (Player == null)
                    return Vector2.down;
                var d = (Vector2)Player.transform.position - Position;
                return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.down;
            }
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Health = GetComponent<Health>();

            ApplyDefinition();
            if (definition == null)
            {
                Debug.LogError($"[Düşman] {name} için EnemyDefinition atanmamış.", this);
                enabled = false;
                return;
            }

            Health.Setup(Team.Enemy, definition.maxHealth);
            Health.AddFilter(FilterDamage);
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;

            // Stagger the first use of each ability so a group doesn't attack in sync.
            readyAt = new float[definition.abilities.Count];
            for (int i = 0; i < readyAt.Length; i++)
                readyAt[i] = Time.time + Random.Range(0.4f, 1.4f);
        }

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                Player = playerObject.GetComponent<PlayerController>();
                PlayerHealth = playerObject.GetComponent<Health>();
            }
        }

#if UNITY_EDITOR
        // Changing the sprite inside OnValidate itself makes Unity complain, so do it right after.
        void OnValidate() => UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
                ApplyDefinition();
        };
#endif

        /// <summary>Copies looks and collision size from the definition (also used for the editor preview).</summary>
        public void ApplyDefinition()
        {
            if (definition == null)
                return;
            if (bodyRenderer == null)
                bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = definition.sprite;
                bodyRenderer.color = definition.tint;
            }
            tint = definition.tint;
            transform.localScale = Vector3.one * definition.scale;
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = definition.footSize;
                box.offset = new Vector2(0f, definition.footSize.y / 2f);
            }
        }

        // ---------------- Brain ----------------

        void Update()
        {
            if (Health == null || Health.IsDead)
                return;
            UpdateVisuals();
            if (Player == null || PlayerHealth == null || PlayerHealth.IsDead || DialogueRunner.IsBusy)
            {
                velocity = Vector2.zero;
                return;
            }

            if (!aggro)
            {
                if (DistanceToPlayer > definition.aggroRange)
                    return;
                Aggro();
            }

            if (action != null)
                return;
            if (IsStaggered)
            {
                velocity = Vector2.zero;
                return;
            }

            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.15f;
                int choice = ChooseAbility();
                if (choice >= 0)
                {
                    action = StartCoroutine(RunAbility(choice));
                    return;
                }
            }
            velocity = Positioning() * definition.moveSpeed;
        }

        void Aggro()
        {
            if (aggro)
                return;
            aggro = true;
            if (definition.isBoss && !IsIllusion && BossHealthBar.Instance != null)
                BossHealthBar.Instance.Show(this);
        }

        int ChooseAbility()
        {
            float distance = DistanceToPlayer;
            float total = 0f;
            var abilities = definition.abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (IsUsable(i, distance))
                    total += abilities[i].weight;
            }
            if (total <= 0f)
                return -1;

            float pick = Random.value * total;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (!IsUsable(i, distance))
                    continue;
                pick -= abilities[i].weight;
                if (pick <= 0f)
                    return i;
            }
            return -1;
        }

        bool IsUsable(int i, float distance)
        {
            var a = definition.abilities[i];
            if (a == null || Time.time < readyAt[i] || distance < a.minRange || distance > a.maxRange)
                return false;
            return !(IsIllusion && a is SummonIllusionsAbility);
        }

        IEnumerator RunAbility(int index)
        {
            var ability = definition.abilities[index];
            velocity = Vector2.zero;
            yield return ability.Perform(this);
            readyAt[index] = Time.time + ability.cooldown;
            EndAction();
        }

        /// <summary>Walk toward / away from the player to the preferred distance, circling when there.</summary>
        Vector2 Positioning()
        {
            float distance = DistanceToPlayer;
            var toPlayer = DirectionToPlayer;
            float preferred = definition.preferredDistance;

            if (distance > preferred + 0.4f)
                return toPlayer;
            if (distance < preferred - 0.4f)
                return -toPlayer;
            if (!definition.strafe)
                return Vector2.zero;

            if (Time.time > nextStrafeFlip)
            {
                strafeSign = Random.value < 0.5f ? -1f : 1f;
                nextStrafeFlip = Time.time + Random.Range(1f, 2.2f);
            }
            return new Vector2(-toPlayer.y, toPlayer.x) * strafeSign * 0.6f;
        }

        void FixedUpdate()
        {
            if (Health == null || Health.IsDead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
            if (Time.time < knockbackUntil)
                body.linearVelocity = knockback;
            else
                body.linearVelocity = IsStaggered ? Vector2.zero : velocity;
        }

        // ---------------- Being hit ----------------

        bool FilterDamage(ref DamageInfo info)
        {
            if (Reflecting && PlayerHealth != null)
            {
                // Ayna: the blow comes back to the attacker.
                PlayerHealth.Damage(new DamageInfo { amount = info.amount * ReflectMultiplier, source = Position, knockback = 5f, attacker = this });
                Flash(new Color(0.6f, 1f, 1f), 0.15f);
                CombatFx.Burst(CombatFx.Circle, BodyCenter, 0.6f, 2f, new Color(0.6f, 1f, 1f, 0.6f), 0.25f);
                return false;
            }

            if (Guarding)
            {
                if (info.heavy)
                {
                    // A heavy blow breaks the guard.
                    Stagger(definition.staggerTime * 2f);
                    CombatFx.Burst(CombatFx.Circle, BodyCenter, 0.5f, 2.5f, new Color(1f, 0.9f, 0.5f, 0.7f), 0.3f);
                }
                else
                {
                    info.amount *= GuardDamageTaken;
                    info.knockback *= 0.2f;
                    Flash(new Color(0.6f, 0.7f, 1f), 0.1f);
                }
            }
            return true;
        }

        void OnDamaged(DamageInfo info)
        {
            Aggro();
            Flash(Color.white, 0.08f);

            var push = (Position - info.source);
            push = push.sqrMagnitude > 0.0001f ? push.normalized : Vector2.up;
            float strength = info.knockback * (1f - definition.knockbackResistance);
            if (strength > 0.1f)
            {
                knockback = push * strength;
                knockbackUntil = Time.time + 0.12f;
            }

            if (definition.poise <= 0f)
            {
                Stagger(definition.staggerTime);
                return;
            }
            poiseDamage += info.heavy ? info.amount * 1.5f : info.amount;
            if (poiseDamage >= definition.poise)
            {
                poiseDamage = 0f;
                Stagger(definition.staggerTime * 1.5f);
            }
        }

        /// <summary>Interrupts whatever it's doing. Parries and broken guards call this.</summary>
        public void Stagger(float seconds)
        {
            if (Health.IsDead)
                return;
            Interrupt();
            staggerUntil = Mathf.Max(staggerUntil, Time.time + seconds);
        }

        void Interrupt()
        {
            if (action != null)
                StopCoroutine(action);
            EndAction();
        }

        void EndAction()
        {
            action = null;
            foreach (var go in temporary)
            {
                if (go != null)
                    Destroy(go);
            }
            temporary.Clear();
            Guarding = false;
            Reflecting = false;
            velocity = Vector2.zero;
            SetAlpha(1f);
        }

        void OnDied()
        {
            Interrupt();
            foreach (var col in GetComponents<Collider2D>())
                col.enabled = false;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            CombatFx.Burst(CombatFx.Circle, BodyCenter, 0.5f, 2.2f, new Color(tint.r, tint.g, tint.b, 0.8f), 0.4f);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                SetAlpha(1f - t / 0.4f);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if (definition.contactDamage <= 0f || IsStaggered || Health.IsDead || !collision.collider.CompareTag("Player"))
                return;
            PlayerHealth?.Damage(new DamageInfo { amount = definition.contactDamage * DamageMultiplier, source = Position, knockback = 4f, attacker = this });
        }

        // ---------------- Illusions ----------------

        /// <summary>Turns this copy into a fragile, see-through decoy that disappears after a while.</summary>
        public void MakeIllusion(float lifetime)
        {
            IsIllusion = true;
            DamageMultiplier = 0.3f;
            Health.Setup(Team.Enemy, 1f);
            alpha = 0.65f;
            aggro = true;
            StartCoroutine(Expire(lifetime));
        }

        IEnumerator Expire(float lifetime)
        {
            yield return Wait(lifetime);
            if (!Health.IsDead)
                Health.Damage(new DamageInfo { amount = 999f, source = Position });
        }

        // ---------------- Helpers for abilities ----------------

        public void SetVelocity(Vector2 v) => velocity = v;

        /// <summary>Moves instantly (teleport). Returns false if the spot is inside a wall.</summary>
        public bool TryMoveTo(Vector2 position)
        {
            if (!IsFree(position))
                return false;
            body.position = position;
            transform.position = position;
            return true;
        }

        public bool IsFree(Vector2 position)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(position + Vector2.up * 0.2f, 0.3f))
            {
                if (!hit.isTrigger && hit.GetComponentInParent<Health>() == null)
                    return false;
            }
            return true;
        }

        /// <summary>Hurts the player if they're within radius of center. Returns true on a hit.</summary>
        public bool HitPlayer(Vector2 center, float radius, float damage, float knockbackForce, bool parriable = true)
        {
            if (PlayerHealth == null || Vector2.Distance(center, PlayerCenter) > radius + 0.3f)
                return false;
            return PlayerHealth.Damage(new DamageInfo
            {
                amount = damage * DamageMultiplier,
                source = center,
                knockback = knockbackForce,
                parriable = parriable,
                attacker = this,
            });
        }

        /// <summary>A warning circle on the ground, removed automatically when the ability ends.</summary>
        public GameObject TelegraphCircle(Vector2 center, float radius, Color color) =>
            Temporary(CombatFx.Shape(CombatFx.Circle, center, Vector2.one * radius * 2f, color));

        /// <summary>A warning strip from 'from' along 'direction'.</summary>
        public GameObject TelegraphLine(Vector2 from, Vector2 direction, float length, float width, Color color)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Temporary(CombatFx.Shape(CombatFx.Square, from + direction * length / 2f, new Vector2(length, width), color, -50, angle));
        }

        public GameObject Temporary(GameObject go)
        {
            temporary.Add(go);
            return go;
        }

        public void Flash(Color color, float seconds)
        {
            flashColor = color;
            flashUntil = Time.time + seconds;
        }

        public void SetAlpha(float value) => alpha = IsIllusion ? Mathf.Min(value, 0.65f) : value;

        void UpdateVisuals()
        {
            if (bodyRenderer == null)
                return;
            var c = Time.time < flashUntil ? flashColor : tint;
            if (IsStaggered && Time.time >= flashUntil)
                c = Color.Lerp(tint, new Color(1f, 0.9f, 0.4f), 0.5f);
            c.a *= alpha;
            bodyRenderer.color = c;
            if (Player != null)
                bodyRenderer.flipX = Player.transform.position.x < transform.position.x;
        }

        static WaitForSeconds Wait(float seconds) => new WaitForSeconds(seconds);
    }
}
