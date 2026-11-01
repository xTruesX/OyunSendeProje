using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boruhan.Animations
{
    public enum AnimState
    {
        [InspectorName("Bekle")] Idle,
        [InspectorName("Yürü")] Walk,
        [InspectorName("Saldırı")] Attack,
        [InspectorName("Ağır Saldırı")] HeavyAttack,
        [InspectorName("Kaçınma")] Dodge,
        [InspectorName("Savuşturma")] Parry,
        [InspectorName("Hasar")] Hurt,
        [InspectorName("Sersemleme")] Stagger,
        [InspectorName("Ölüm")] Death,
        [InspectorName("Özel")] Custom,
        [InspectorName("Dönüş")] Turn,
    }

    public enum AnimDirection
    {
        [InspectorName("Hepsi")] Any,
        [InspectorName("Aşağı")] Down,
        [InspectorName("Yukarı")] Up,
        [InspectorName("Yan")] Side,
        [InspectorName("Sol")] Left,
        [InspectorName("Sağ")] Right,
    }

    public static class AnimNames
    {
        public static string Turkish(this AnimState state) => state switch
        {
            AnimState.Idle => "Bekle",
            AnimState.Walk => "Yürü",
            AnimState.Attack => "Saldırı",
            AnimState.HeavyAttack => "Ağır Saldırı",
            AnimState.Dodge => "Kaçınma",
            AnimState.Parry => "Savuşturma",
            AnimState.Hurt => "Hasar",
            AnimState.Stagger => "Sersemleme",
            AnimState.Death => "Ölüm",
            AnimState.Turn => "Dönüş",
            _ => "Özel",
        };

        public static string Turkish(this AnimDirection direction) => direction switch
        {
            AnimDirection.Down => "Aşağı",
            AnimDirection.Up => "Yukarı",
            AnimDirection.Side => "Yan",
            AnimDirection.Left => "Sol",
            AnimDirection.Right => "Sağ",
            _ => "Hepsi",
        };
    }

    /// <summary>One animation: a row of frames for a state (and optionally one facing direction).</summary>
    [Serializable]
    public class AnimClip
    {
        public AnimState state;
        [Tooltip("Only for Özel: the name a move or the 'Animasyon Oynat' command asks for.")]
        public string customName = "";
        [Tooltip("Hepsi = used for every direction. Yan is mirrored automatically for left; Sol/Sağ are used as drawn.")]
        public AnimDirection direction;
        public List<Sprite> frames = new List<Sprite>();
        [Min(0.1f)] public float fps = 8f;
        [Tooltip("Off: plays once and stays on the last frame.")]
        public bool loop = true;

        public bool IsEmpty => frames == null || frames.Count == 0;
        public float Duration => IsEmpty ? 0f : frames.Count / fps;

        public Sprite FrameAt(float time)
        {
            if (IsEmpty)
                return null;
            int i = Mathf.FloorToInt(time * fps);
            i = loop ? (int)Mathf.Repeat(i, frames.Count) : Mathf.Clamp(i, 0, frames.Count - 1);
            return frames[i];
        }
    }

    /// <summary>
    /// All the animations of one character (Börühan, an enemy, an NPC). Create with
    /// Assets > Create > Börühan > Animasyon Seti and drop sprite strips onto its clips in the Inspector.
    /// Missing animations fall back sensibly (no Ağır Saldırı → Saldırı, no Yürü → Bekle, none → the plain sprite).
    /// Dönüş is special: its frames go round the character (front, right, back, left) and play between
    /// facings when the character turns.
    /// </summary>
    [CreateAssetMenu(menuName = "Börühan/Animasyon Seti", fileName = "YeniAnimasyonSeti")]
    public class AnimationSet : ScriptableObject
    {
        [Tooltip("Mirror the sprite when the character faces left. Draw side frames facing right.")]
        public bool mirrorForLeft = true;
        public List<AnimClip> clips = new List<AnimClip>();

        public AnimClip Find(AnimState state, string customName, AnimDirection direction)
        {
            var clip = FindExact(state, customName, direction);
            if (clip != null)
                return clip;
            switch (state)
            {
                case AnimState.HeavyAttack:
                case AnimState.Custom:
                    return FindExact(AnimState.Attack, null, direction);
                case AnimState.Walk:
                    return FindExact(AnimState.Idle, null, direction);
                default:
                    return null;
            }
        }

        AnimClip FindExact(AnimState state, string customName, AnimDirection direction)
        {
            AnimClip best = null;
            int bestScore = int.MaxValue;
            foreach (var c in clips)
            {
                if (c == null || c.state != state || c.IsEmpty)
                    continue;
                if (state == AnimState.Custom && !string.Equals(c.customName, customName, StringComparison.OrdinalIgnoreCase))
                    continue;
                int score = Score(c.direction, direction);
                if (score < bestScore)
                {
                    best = c;
                    bestScore = score;
                }
            }
            return best;
        }

        // Exact direction first, then a mirrored Yan for Sol/Sağ, then Hepsi, then anything.
        static int Score(AnimDirection clip, AnimDirection wanted)
        {
            if (clip == wanted)
                return 0;
            if (clip == AnimDirection.Side && (wanted == AnimDirection.Left || wanted == AnimDirection.Right))
                return 1;
            return clip == AnimDirection.Any ? 2 : 3;
        }
    }
}
