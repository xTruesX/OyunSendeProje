using System.Collections;
using System.Collections.Generic;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
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
        [SerializeField] string[] combatFlags = { "kilic_alindi", StoryFlags.KutSpark };

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

                float amount = damage * (GameSession.HasFlag(StoryFlags.KutSpark) ? kutDamageMultiplier : 1f);
                var enemy = target.GetComponent<EnemyController>();
                if (heavy && enemy != null && enemy.IsStaggered)
                {
                    amount *= kutYikimiMultiplier;
                    CombatFx.Burst(CombatFx.Circle, enemy.BodyCenter, 0.6f, 3f, new Color(1f, 0.55f, 0.15f, 0.8f), 0.35f, 70);
                }

                landed |= target.Damage(new DamageInfo { amount = amount, source = transform.position, knockback = knockback, heavy = heavy });
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
            if (cam != null)
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
