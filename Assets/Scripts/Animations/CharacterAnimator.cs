using System;
using System.Collections.Generic;
using Boruhan.Core;
using UnityEngine;

namespace Boruhan.Animations
{
    /// <summary>A different look once a story flag is set (e.g. Börühan wearing the Tılsım).</summary>
    [Serializable]
    public class AnimationVariant
    {
        [Tooltip("Used while this story flag is set (e.g. tilsim_alindi, kilic_alindi).")]
        public string flag;
        public AnimationSet animations;
    }

    /// <summary>
    /// Plays an AnimationSet on a SpriteRenderer. Walking and standing are detected from movement on its
    /// own; attacks, dodges, hits and death are started by the player / enemy scripts (or the
    /// "Animasyon Oynat" event command). Works on anything: add it next to a character's sprite.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] AnimationSet animations;
        [Tooltip("Other looks unlocked by story flags. The last one whose flag is set wins; animations it lacks come from the main set.")]
        [SerializeField] List<AnimationVariant> variants = new List<AnimationVariant>();
        [Tooltip("Empty = the SpriteRenderer on this object or its children.")]
        [SerializeField] SpriteRenderer target;
        [Tooltip("Moving faster than this (units/second) plays Yürü.")]
        [SerializeField] float walkThreshold = 0.2f;

        Sprite fallback;
        bool fallbackCaptured;
        Vector3 lastPosition;
        float speed;
        Vector2 moveDirection = Vector2.down;
        float baseTime;
        AnimState baseState;

        AnimClip action;
        float actionTime;
        float actionUntil;
        AnimClip hurt;
        float hurtTime;
        float hurtUntil;
        bool dead;
        float deadTime;
        bool facingLeft;
        AnimDirection lastDirection;
        bool hasLastDirection;
        readonly System.Collections.Generic.List<Sprite> turnFrames = new System.Collections.Generic.List<Sprite>();
        float turnTime;
        float turnFrameTime;

        public AnimationSet Animations
        {
            get => animations;
            set => animations = value;
        }

        /// <summary>Where the character looks. Leave unset to look where it walks.</summary>
        public Vector2? Facing { get; set; }

        /// <summary>Held while staggered (Sersemleme).</summary>
        public bool Staggered { get; set; }

        public bool IsPlayingAction => action != null && actionTime < actionUntil;

        void Awake()
        {
            if (target == null)
                target = GetComponentInChildren<SpriteRenderer>(true);
            lastPosition = transform.position;
        }

        /// <summary>Changes the set and the sprite shown when the set has nothing to play (GameEvent pages use this).</summary>
        public void Setup(AnimationSet set, Sprite plainSprite)
        {
            animations = set;
            fallback = plainSprite;
            fallbackCaptured = true;
        }

        /// <summary>
        /// Plays a one-off animation over walking/standing. It lasts at least 'minSeconds' and at least one
        /// full play of the clip; pass float.PositiveInfinity to keep it until StopAction(). Returns false
        /// if the set has no such animation.
        /// </summary>
        public bool PlayAction(AnimState state, float minSeconds = 0f, string customName = null)
        {
            var clip = FindClip(state, customName, Direction);
            if (clip == null)
                return false;
            action = clip;
            actionTime = 0f;
            turnFrames.Clear();
            actionUntil = Mathf.Max(minSeconds, clip.loop ? 0f : clip.Duration);
            return true;
        }

        public void StopAction() => action = null;

        /// <summary>A short flinch that doesn't cancel the current action.</summary>
        public void PlayHurt()
        {
            var clip = FindClip(AnimState.Hurt, null, Direction);
            if (clip == null)
                return;
            hurt = clip;
            hurtTime = 0f;
            hurtUntil = Mathf.Max(0.2f, clip.loop ? 0f : clip.Duration);
        }

        public void PlayDeath()
        {
            dead = true;
            deadTime = 0f;
        }

        /// <summary>Back to normal (after respawning).</summary>
        public void ResetState()
        {
            dead = false;
            action = null;
            hurt = null;
            turnFrames.Clear();
            Staggered = false;
        }

        /// <summary>How long the death animation takes (0 if there is none).</summary>
        public float DeathDuration
        {
            get
            {
                var clip = FindClip(AnimState.Death, null, Direction);
                return clip != null ? clip.Duration : 0f;
            }
        }

        AnimDirection Direction
        {
            get
            {
                var f = Facing ?? moveDirection;
                if (Mathf.Abs(f.x) >= Mathf.Abs(f.y))
                    return f.x < 0f ? AnimDirection.Left : AnimDirection.Right;
                return f.y > 0f ? AnimDirection.Up : AnimDirection.Down;
            }
        }

        void LateUpdate() => Tick(Time.deltaTime);

        public List<AnimationVariant> Variants => variants;

        /// <summary>The set in use: the last variant whose flag is set, else the main set.</summary>
        public AnimationSet Current
        {
            get
            {
                for (int i = variants.Count - 1; i >= 0; i--)
                {
                    var v = variants[i];
                    if (v != null && v.animations != null && GameSession.HasFlag(v.flag))
                        return v.animations;
                }
                return animations;
            }
        }

        AnimClip FindClip(AnimState state, string customName, AnimDirection direction)
        {
            var current = Current;
            var clip = current != null ? current.Find(state, customName, direction) : null;
            if (clip == null && current != animations && animations != null)
                clip = animations.Find(state, customName, direction);
            return clip;
        }

        /// <summary>Advances the animation by dt seconds (LateUpdate does this every frame).</summary>
        public void Tick(float dt)
        {
            if (target == null)
                target = GetComponentInChildren<SpriteRenderer>(true);
            if (target == null)
                return;
            if (!fallbackCaptured)
            {
                // Captured late so scripts that set the sprite in Awake/Start (enemies) are already done.
                fallback = target.sprite;
                fallbackCaptured = true;
            }

            if (dt > 0f)
            {
                var delta = transform.position - lastPosition;
                speed = Mathf.Lerp(speed, delta.magnitude / dt, 0.5f);
                if (delta.sqrMagnitude > 0.000001f)
                    moveDirection = delta.normalized;
            }
            lastPosition = transform.position;

            if (Current == null)
                return;

            var direction = Direction;
            var f = Facing ?? moveDirection;
            if (Mathf.Abs(f.x) > 0.01f)
                facingLeft = f.x < 0f;
            if (hasLastDirection && direction != lastDirection && !dead)
                StartTurn(lastDirection, direction);
            lastDirection = direction;
            hasLastDirection = true;

            AnimClip clip;
            float time;
            if (dead)
            {
                deadTime += dt;
                clip = FindClip(AnimState.Death, null, direction);
                time = deadTime;
            }
            else if (hurt != null && hurtTime < hurtUntil)
            {
                hurtTime += dt;
                clip = hurt;
                time = hurtTime;
            }
            else if (action != null && actionTime < actionUntil)
            {
                actionTime += dt;
                clip = action;
                time = actionTime;
            }
            else if (turnFrames.Count > 0)
            {
                // Dönüş: the in-between facings, drawn as they are (never mirrored).
                turnTime += dt;
                int i = Mathf.FloorToInt(turnTime / turnFrameTime);
                if (i < turnFrames.Count)
                {
                    target.sprite = turnFrames[i];
                    target.flipX = false;
                    return;
                }
                turnFrames.Clear();
                return; // next frame continues with walking / standing
            }
            else
            {
                action = null;
                var state = Staggered ? AnimState.Stagger : speed > walkThreshold ? AnimState.Walk : AnimState.Idle;
                if (state != baseState)
                {
                    baseState = state;
                    baseTime = 0f;
                }
                baseTime += dt;
                clip = FindClip(state, null, direction);
                if (clip == null && state == AnimState.Stagger)
                    clip = FindClip(AnimState.Idle, null, direction);
                time = baseTime;
            }

            var sprite = clip != null ? clip.FrameAt(time) : null;
            target.sprite = sprite != null ? sprite : fallback;
            // Sol/Sağ/Aşağı/Yukarı frames are drawn the way they face; Yan and Hepsi are mirrored for left.
            bool mirrorable = clip == null || clip.direction == AnimDirection.Side || clip.direction == AnimDirection.Any;
            target.flipX = Current.mirrorForLeft && mirrorable && facingLeft;
        }

        /// <summary>
        /// Queues the Dönüş frames between two facings. Its frames go round the character starting from
        /// the front: front, front-right, right, back-right, back, back-left, left, front-left (any count
        /// divisible by 4 works; 4 frames = no in-betweens).
        /// </summary>
        void StartTurn(AnimDirection from, AnimDirection to)
        {
            turnFrames.Clear();
            if (action != null || hurt != null && hurtTime < hurtUntil)
                return;
            var clip = FindClip(AnimState.Turn, null, AnimDirection.Any);
            if (clip == null || clip.frames.Count < 8 || clip.frames.Count % 4 != 0)
                return;
            int n = clip.frames.Count;
            int a = Ring(from) * n / 4, b = Ring(to) * n / 4;
            int delta = ((b - a) % n + n) % n;
            int step = delta <= n / 2 ? 1 : -1;
            for (int i = (a + step + n) % n; i != b; i = (i + step + n) % n)
                turnFrames.Add(clip.frames[i]);
            turnTime = 0f;
            turnFrameTime = 1f / clip.fps;
        }

        static int Ring(AnimDirection d) => d switch
        {
            AnimDirection.Right => 1,
            AnimDirection.Up => 2,
            AnimDirection.Left => 3,
            _ => 0,
        };
    }
}
