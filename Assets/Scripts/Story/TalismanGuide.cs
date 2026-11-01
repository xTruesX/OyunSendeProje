using System;
using Boruhan.Core;
using Boruhan.Gameplay;
using Boruhan.UI;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>
    /// Ülgen Han's talisman: glows brighter the more the player is heading toward the current target.
    /// The first target whose flags match is the active one.
    /// </summary>
    public class TalismanGuide : MonoBehaviour
    {
        [Serializable]
        public class Target
        {
            public string name;
            public Transform point;
            [Tooltip("Target is active once this flag is set.")]
            public string activeFromFlag;
            [Tooltip("Target stops being active once this flag is set.")]
            public string activeUntilFlag;
        }

        [SerializeField] PlayerController player;
        [SerializeField] Target[] targets;
        [Tooltip("How fast the glow reacts to turning.")]
        [SerializeField] float smoothing = 6f;

        float glow;

        void Update()
        {
            var hud = TalismanHud.Instance;
            if (hud == null || player == null)
                return;

            var target = CurrentTarget();
            hud.SetVisible(target != null);
            if (target == null)
                return;

            Vector2 toTarget = ((Vector2)target.point.position - (Vector2)player.transform.position).normalized;
            float alignment = Vector2.Dot(player.LastMoveDirection, toTarget);

            // Bright only when roughly facing the target; fades out quickly when turning away.
            float desired = Mathf.InverseLerp(0.35f, 0.95f, alignment);
            glow = Mathf.Lerp(glow, desired, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            hud.SetGlow(glow);
        }

        Target CurrentTarget()
        {
            foreach (var target in targets)
            {
                bool started = string.IsNullOrEmpty(target.activeFromFlag) || GameSession.HasFlag(target.activeFromFlag);
                bool ended = GameSession.HasFlag(target.activeUntilFlag);
                if (started && !ended && target.point != null)
                    return target;
            }
            return null;
        }
    }
}
