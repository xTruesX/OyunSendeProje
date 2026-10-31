using System.Collections.Generic;
using UnityEngine;

namespace Boruhan.Combat
{
    public enum Difficulty { Kolay, Orta, Zor }

    /// <summary>
    /// Everything about one kind of enemy: looks, stats, AI and its moveset (Hareket Seti).
    /// Create with Assets > Create > Börühan > Düşman, add abilities with the buttons in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Börühan/Düşman", fileName = "YeniDusman")]
    public class EnemyDefinition : ScriptableObject
    {
        public string displayName = "Düşman";
        [TextArea(2, 4)] public string description;
        public Difficulty difficulty;
        [Tooltip("Shows a big health bar with the name at the top of the screen.")]
        public bool isBoss;

        [Header("Görünüm")]
        public Sprite sprite;
        public Color tint = Color.white;
        [Min(0.1f)] public float scale = 1f;
        [Tooltip("Collision box at the feet, in sprite units (before scale).")]
        public Vector2 footSize = new Vector2(0.7f, 0.35f);

        [Header("Değerler")]
        [Min(1f)] public float maxHealth = 40f;
        [Min(0f)] public float moveSpeed = 2.5f;
        [Tooltip("Damage when the player touches it. 0 = harmless to touch.")]
        [Min(0f)] public float contactDamage;
        [Range(0f, 1f)] public float knockbackResistance;
        [Tooltip("Damage it can take before being staggered (interrupted). 0 = every hit staggers it.")]
        [Min(0f)] public float poise;
        [Min(0f)] public float staggerTime = 0.35f;

        [Header("Yapay Zeka")]
        [Tooltip("Starts fighting when the player comes this close (or hits it).")]
        [Min(0f)] public float aggroRange = 6f;
        [Tooltip("Distance it tries to keep from the player. Small = melee, large = ranged.")]
        [Min(0f)] public float preferredDistance = 1.5f;
        [Tooltip("Circle around the player while waiting for an ability.")]
        public bool strafe = true;

        [Header("Hareket Seti")]
        public List<EnemyAbility> abilities = new List<EnemyAbility>();
    }
}
