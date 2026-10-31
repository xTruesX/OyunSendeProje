using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boruhan.Combat
{
    public enum Team { Player, Enemy }

    public struct DamageInfo
    {
        public float amount;
        /// <summary>Where the hit came from (used for knockback direction and guarding).</summary>
        public Vector2 source;
        public float knockback;
        /// <summary>Can the player parry this (savuşturma)?</summary>
        public bool parriable;
        /// <summary>A heavy attack: breaks guards and lands Kut Yıkımı on staggered enemies.</summary>
        public bool heavy;
        public EnemyController attacker;
    }

    /// <summary>Hit points for the player and for enemies.</summary>
    public class Health : MonoBehaviour
    {
        /// <summary>Can change the incoming damage, or return false to cancel it (parry, reflect, guard).</summary>
        public delegate bool DamageFilter(ref DamageInfo info);

        [SerializeField] Team team = Team.Enemy;
        [SerializeField] float maxHealth = 100f;

        readonly List<DamageFilter> filters = new List<DamageFilter>();
        float invulnerableUntil;

        public Team Team => team;
        public float Max => maxHealth;
        public float Current { get; private set; }
        public float Ratio => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsDead => Current <= 0f;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public event Action Changed;

        void Awake() => Current = maxHealth;

        public void Setup(Team newTeam, float max)
        {
            team = newTeam;
            maxHealth = max;
            Current = max;
            Changed?.Invoke();
        }

        public void AddFilter(DamageFilter filter) => filters.Add(filter);

        public void SetInvulnerable(float seconds) => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);

        /// <summary>Returns true if the damage landed.</summary>
        public bool Damage(DamageInfo info)
        {
            if (IsDead || IsInvulnerable)
                return false;
            foreach (var filter in filters)
            {
                if (!filter(ref info))
                    return false;
            }

            Current = Mathf.Max(0f, Current - info.amount);
            Damaged?.Invoke(info);
            Changed?.Invoke();
            if (Current <= 0f)
                Died?.Invoke();
            return true;
        }

        public void Heal(float amount)
        {
            if (IsDead)
                return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke();
        }

        public void HealFull() => Heal(maxHealth);

        /// <summary>Changes the maximum (e.g. a Can Parçası); current health is kept, capped to the new max.</summary>
        public void SetMax(float max)
        {
            maxHealth = max;
            Current = Mathf.Min(Current, maxHealth);
            Changed?.Invoke();
        }

        public void SetCurrent(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxHealth);
            Changed?.Invoke();
        }
    }
}
