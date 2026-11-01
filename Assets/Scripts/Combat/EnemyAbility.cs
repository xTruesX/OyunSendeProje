using System.Collections;
using Boruhan.Animations;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>
    /// One move in an enemy's moveset. The AI picks a ready ability whose range fits the distance to
    /// the player (weighted at random), runs it, then waits for its cooldown.
    /// To make a new kind of move, inherit from this and write Perform().
    /// </summary>
    public abstract class EnemyAbility : ScriptableObject
    {
        public string displayName = "Yetenek";

        [Header("Ne Zaman")]
        [Min(0f)] public float cooldown = 2f;
        [Tooltip("Only used when the player is at least this far…")]
        [Min(0f)] public float minRange;
        [Tooltip("…and at most this far.")]
        [Min(0f)] public float maxRange = 2f;
        [Tooltip("How likely it is to be picked compared to the other ready abilities.")]
        [Min(0.01f)] public float weight = 1f;

        [Header("Zamanlama")]
        [Tooltip("Warning time before it hits. Longer = easier to dodge.")]
        [Min(0f)] public float windup = 0.4f;
        [Tooltip("Pause after it's done. The player's chance to hit back.")]
        [Min(0f)] public float recovery = 0.4f;
        public Color telegraphColor = new Color(1f, 0.25f, 0.2f, 0.35f);

        [Header("Animasyon")]
        [Tooltip("Played while this move runs. Özel = a clip of its own, named below.")]
        public AnimState animationState = AnimState.Attack;
        [Tooltip("Only for Özel: the clip's name in the enemy's Animasyon Seti.")]
        public string customAnimation = "";

        public abstract IEnumerator Perform(EnemyController enemy);

        protected static WaitForSeconds Wait(float seconds) => new WaitForSeconds(seconds);
    }
}
