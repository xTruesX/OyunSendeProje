using System.Collections;
using System.Collections.Generic;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using Boruhan.Story;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boruhan.Combat
{
    /// <summary>
    /// Börühan's sword fighting, available once he has a sword (one of Combat Flags is set).
    /// Light attack: a 3-hit combo. Heavy attack: slow, breaks guards, Kut Yıkımı on staggered enemies.
    /// Dodge: a quick dash with invulnerability. Parry: block at the right moment to stagger the attacker.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health))]
    public class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Combat is enabled once any of these story flags is set.")]
        [SerializeField] string[] combatFlags = { StoryFlags.Sword, StoryFlags.KutSpark };

        [Header("Can ve Güçlenme")]
        [SerializeField] float baseMaxHealth = 100f;
        [Tooltip("Extra max health per Can Parçası found.")]
        [SerializeField] float healthPerShard = 20f;
        [Tooltip("Extra damage per Kut Parçası found (0.1 = +10%).")]
        [SerializeField] float damagePerKutShard = 0.1f;

        [Header("Hafif Saldırı (3'lü kombo)")]
        [SerializeField] float lightDamage = 12f;
        [SerializeField] float comboFinisherDamage = 20f;
        [SerializeField] float lightReach = 0.8f;
        [SerializeField] float lightRadius = 0.85f;
        [SerializeField] float lightDuration = 0.28f;
        [Tooltip("Time after a swing in which the next press continues the combo.")]
        [SerializeField] float comboWindow = 0.45f;

        [Header("Ağır Saldırı")]
        [SerializeField] float heavyDamage = 32f;
        [SerializeField] float heavyWindup = 0.35f;
        [SerializeField] float heavyReach = 1f;
        [SerializeField] float heavyRadius = 1.15f;
        [SerializeField] float heavyRecovery = 0.45f;
        [SerializeField] float heavyKnockback = 8f;
        [Tooltip("Damage multiplier for a heavy attack on a staggered enemy.")]
        [SerializeField] float kutYikimiMultiplier = 2.5f;

        [Header("Kaçış")]
        [SerializeField] float dodgeSpeed = 11f;
        [SerializeField] float dodgeDuration = 0.22f;
        [SerializeField] float dodgeCooldown = 0.45f;

        [Header("Savuşturma")]
        [SerializeField] float parryWindow = 0.22f;
        [SerializeField] float parryCooldown = 0.6f;
        [SerializeField] float parryStagger = 1.6f;

        [Header("Kut")]
        [Tooltip("Damage bonus once Kayra Han's spark is received.")]
        [SerializeField] float kutDamageMultiplier = 1.25f;

        [Header("Ayna Sıçraması (1. Kat ödülü)")]
        [Tooltip("With this ability the dodge becomes a short teleport that crosses pits (not walls).")]
        [SerializeField] float blinkDistance = 3.2f;

        [Header("Demir Eldiven (2. Kat ödülü)")]
        [Tooltip("A heavy attack with the gauntlet stuns enemies for this long and breaks cracked walls.")]
        [SerializeField] float gauntletStun = 2.2f;
        [Tooltip("The gauntlet is heavy: its stun can only be used this often.")]
        [SerializeField] float gauntletCooldown = 4f;

        [Header("Görsel")]
        [SerializeField] SpriteRenderer slashRenderer;
        [SerializeField] SpriteRenderer bodyRenderer;

        PlayerController controller;
        Health health;
        readonly List<Collider2D> hits = new List<Collider2D>();
        readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
        bool busy;
        int comboStep;
        float comboUntil;
        float nextDodge;
        float nextParry;
        float parryUntil;
        Color flashColor;
        float flashUntil;
        float bodyAlpha = 1f;
        float gauntletReadyAt;

        static readonly Vector2 BodyOffset = new Vector2(0f, 0.5f);

        public bool CanFight
        {
            get
            {
                foreach (var flag in combatFlags)
                {
                    if (GameSession.HasFlag(flag))
                        return true;
                }
                return false;
            }
        }

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            health.AddFilter(ParryFilter);
            health.Damaged += OnHurt;
            health.Died += OnDied;
            if (slashRenderer != null)
                slashRenderer.enabled = false;
        }

        void Start()
        {
            // Health carries over between scenes and saves.
            ApplyUpgrades(false);
            var data = GameSession.Data;
            health.SetCurrent(data != null && data.playerHealth > 0f ? data.playerHealth : health.Max);
            health.Changed += () =>
            {
                if (GameSession.Data != null && !health.IsDead)
                    GameSession.Data.playerHealth = health.Current;
            };
        }

        /// <summary>Recomputes max health from collected Can Parçası. Call after picking one up.</summary>
        public void ApplyUpgrades(bool healToFull)
        {
            float max = baseMaxHealth + GameSession.CountFlags("can_parcasi_") * healthPerShard;
            health.SetMax(max);
            if (healToFull)
                health.HealFull();
        }

        float DamageMultiplier =>
            (1f + GameSession.CountFlags("kut_parcasi_") * damagePerKutShard)
            * (GameSession.HasFlag(StoryFlags.KutSpark) ? kutDamageMultiplier : 1f);

        void Update()
        {
            UpdateBodyColor();
            if (health.IsDead || busy || !CanFight || !controller.InputEnabled
                || DialogueRunner.IsBusy || ScreenFader.IsBusy || Time.timeScale == 0f)
                return;

            if (GameInput.DodgePressed && Time.time >= nextDodge)
                StartCoroutine(Dodge());
            else if (GameInput.ParryPressed && Time.time >= nextParry)
                StartCoroutine(Parry());
            else if (GameInput.LightAttackPressed)
                StartCoroutine(LightAttack());
            else if (GameInput.HeavyAttackPressed)
                StartCoroutine(HeavyAttack());
        }

        Vector2 Aim => controller.LastMoveDirection;

        IEnumerator LightAttack()
        {
            busy = true;
            if (Time.time > comboUntil)
                comboStep = 0;
            comboStep = comboStep % 3 + 1;
            bool finisher = comboStep == 3;

            var aim = Aim;
            controller.SpeedMultiplier = 0.3f;
            controller.ForceVelocity(aim * 3f, 0.07f);
            Swing(aim, lightReach, lightRadius, finisher ? comboFinisherDamage : lightDamage, finisher ? 5f : 2.5f, false, finisher ? 1.25f : 1f);

            yield return new WaitForSeconds(lightDuration);
            controller.SpeedMultiplier = 1f;
            comboUntil = Time.time + comboWindow;
            busy = false;
        }

        IEnumerator HeavyAttack()
        {
            busy = true;
            comboStep = 0;
            var aim = Aim;
            controller.SpeedMultiplier = 0.15f;
            Flash(new Color(1f, 0.85f, 0.4f), heavyWindup);
            yield return new WaitForSeconds(heavyWindup);

            aim = Aim;
            controller.ForceVelocity(aim * 6f, 0.1f);
            Swing(aim, heavyReach, heavyRadius, heavyDamage, heavyKnockback, true, 1.6f);
            yield return new WaitForSeconds(heavyRecovery);
            controller.SpeedMultiplier = 1f;
            busy = false;
        }

        void Swing(Vector2 aim, float reach, float radius, float damage, float knockback, bool heavy, float effectScale)
        {
            var center = (Vector2)transform.position + BodyOffset + aim * reach;
            ShowSlash(center, aim, radius * effectScale);

            hitThisSwing.Clear();
            bool landed = false;
            Physics2D.OverlapCircle(center, radius, ContactFilter2D.noFilter, hits);
            foreach (var col in hits)
            {
                var projectile = col.GetComponent<Projectile>();
                if (projectile != null)
                {
                    // Shots can be cut out of the air.
                    projectile.Break();
                    continue;
                }

                var target = col.GetComponentInParent<Health>();
                if (target == null || target.Team == Team.Player || !hitThisSwing.Add(target))
                    continue;

                float amount = damage * DamageMultiplier;
                var enemy = target.GetComponent<EnemyController>();
                if (heavy && enemy != null && enemy.IsStaggered)
                {
                    amount *= kutYikimiMultiplier;
                    CombatFx.Burst(CombatFx.Circle, enemy.BodyCenter, 0.6f, 3f, new Color(1f, 0.55f, 0.15f, 0.8f), 0.35f, 70);
                }

                bool hit = target.Damage(new DamageInfo { amount = amount, source = transform.position, knockback = knockback, heavy = heavy });
                landed |= hit;

                // Demir Eldiven: the heavy blow stuns, but the gauntlet needs a moment between uses.
                if (hit && heavy && enemy != null && GameSession.HasFlag(StoryFlags.IronGauntlet) && Time.time >= gauntletReadyAt)
                {
                    gauntletReadyAt = Time.time + gauntletCooldown;
                    enemy.Stagger(gauntletStun);
                    CombatFx.Burst(CombatFx.Circle, enemy.BodyCenter, 0.4f, 2.4f, new Color(0.75f, 0.75f, 0.85f, 0.8f), 0.3f, 70);
                }
            }

            if (landed)
                CombatFx.HitStop(heavy ? 0.08f : 0.04f);
        }

        void ShowSlash(Vector2 center, Vector2 aim, float size)
        {
            if (slashRenderer == null)
                return;
            StopCoroutine(nameof(SlashRoutine));
            slashRenderer.transform.position = center;
            slashRenderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            var bounds = slashRenderer.sprite != null ? slashRenderer.sprite.bounds.size : Vector3.one;
            slashRenderer.transform.localScale = new Vector3(size * 2f / bounds.x, size * 2f / bounds.y, 1f);
            StartCoroutine(nameof(SlashRoutine));
        }

        IEnumerator SlashRoutine()
        {
            slashRenderer.enabled = true;
            var c = GameSession.HasFlag(StoryFlags.KutSpark) ? new Color(1f, 0.6f, 0.25f) : Color.white;
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                slashRenderer.color = new Color(c.r, c.g, c.b, 1f - t / 0.12f);
                yield return null;
            }
            slashRenderer.enabled = false;
        }

        IEnumerator Dodge()
        {
            if (GameSession.HasFlag(StoryFlags.MirrorLeap))
            {
                yield return Blink();
                yield break;
            }

            busy = true;
            nextDodge = Time.time + dodgeDuration + dodgeCooldown;
            var dir = controller.MoveInput.sqrMagnitude > 0.01f ? controller.MoveInput.normalized : Aim;
            health.SetInvulnerable(dodgeDuration + 0.05f);
            controller.ForceVelocity(dir * dodgeSpeed, dodgeDuration);
            bodyAlpha = 0.45f;
            yield return new WaitForSeconds(dodgeDuration);
            bodyAlpha = 1f;
            busy = false;
        }

        /// <summary>Ayna Sıçraması: vanish and reappear a short distance ahead, over pits but not through walls.</summary>
        IEnumerator Blink()
        {
            busy = true;
            nextDodge = Time.time + 0.15f + dodgeCooldown;
            var dir = controller.MoveInput.sqrMagnitude > 0.01f ? controller.MoveInput.normalized : Aim;
            Vector2 from = transform.position;
            var mirror = new Color(0.65f, 0.95f, 1f, 0.8f);

            for (float d = blinkDistance; d >= 0.8f; d -= 0.4f)
            {
                var to = from + dir * d;
                if (BlinkBlocked(from, to) || !IsFree(to))
                    continue;
                CombatFx.Burst(CombatFx.Circle, from + BodyOffset, 0.4f, 1.6f, mirror, 0.25f, 70);
                health.SetInvulnerable(0.2f);
                controller.Teleport(to);
                CombatFx.Burst(CombatFx.Circle, to + BodyOffset, 1.6f, 0.4f, mirror, 0.2f, 70);
                break;
            }
            yield return new WaitForSeconds(0.15f);
            busy = false;
        }

        bool BlinkBlocked(Vector2 from, Vector2 to)
        {
            foreach (var hit in Physics2D.LinecastAll(from + Vector2.up * 0.2f, to + Vector2.up * 0.2f))
            {
                if (hit.collider.isTrigger || hit.collider.attachedRigidbody == controller.GetComponent<Rigidbody2D>())
                    continue;
                if (hit.collider.GetComponentInParent<BlinkPassable>() == null && hit.collider.GetComponentInParent<Health>() == null)
                    return true;
            }
            return false;
        }

        bool IsFree(Vector2 position)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(position + Vector2.up * 0.18f, 0.3f))
            {
                if (!hit.isTrigger && hit.attachedRigidbody != controller.GetComponent<Rigidbody2D>())
                    return false;
            }
            return true;
        }

        IEnumerator Parry()
        {
            busy = true;
            nextParry = Time.time + parryCooldown;
            parryUntil = Time.time + parryWindow;
            controller.SpeedMultiplier = 0f;
            Flash(new Color(0.6f, 0.9f, 1f), parryWindow);
            yield return new WaitForSeconds(parryWindow);
            controller.SpeedMultiplier = 1f;
            busy = false;
        }

        bool ParryFilter(ref DamageInfo info)
        {
            if (Time.time >= parryUntil || !info.parriable)
                return true;

            // Savuşturma: the blow is turned aside and the attacker loses its balance.
            if (info.attacker != null)
                info.attacker.Stagger(parryStagger);
            Flash(Color.white, 0.15f);
            CombatFx.Burst(CombatFx.Circle, (Vector2)transform.position + BodyOffset, 0.5f, 2f, new Color(0.7f, 0.95f, 1f, 0.8f), 0.25f, 70);
            CombatFx.HitStop(0.1f);
            nextParry = Time.time;
            return false;
        }

        void OnHurt(DamageInfo info)
        {
            health.SetInvulnerable(0.6f);
            Flash(new Color(1f, 0.3f, 0.3f), 0.2f);
            var push = (Vector2)transform.position - info.source;
            if (push.sqrMagnitude > 0.0001f && info.knockback > 0f)
                controller.ForceVelocity(push.normalized * info.knockback, 0.15f);
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null && info.knockback > 0f)
                cam.Shake(0.15f);
        }

        void OnDied()
        {
            StopAllCoroutines();
            controller.InputEnabled = false;
            controller.SpeedMultiplier = 1f;
            StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            // Fall, then restart the room from where Börühan entered it (enemies come back too).
            Flash(new Color(0.3f, 0.1f, 0.1f), 10f);
            yield return new WaitForSecondsRealtime(1.2f);
            var spawn = GameSession.Data != null ? GameSession.Data.spawnId : null;
            if (GameSession.Data != null)
                GameSession.Data.playerHealth = -1f;
            GameSession.TravelTo(SceneManager.GetActiveScene().name, spawn);
        }

        void Flash(Color color, float seconds)
        {
            flashColor = color;
            flashUntil = Time.time + seconds;
        }

        void UpdateBodyColor()
        {
            if (bodyRenderer == null)
                return;
            var c = Color.white;
            if (Time.time < flashUntil)
                c = flashColor;
            else if (controller.IsSlowed)
                c = new Color(0.75f, 0.55f, 1f);
            else if (health.IsInvulnerable && !health.IsDead)
                c = Mathf.Repeat(Time.time * 12f, 1f) > 0.5f ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            c.a *= bodyAlpha;
            bodyRenderer.color = c;
        }
    }
}
