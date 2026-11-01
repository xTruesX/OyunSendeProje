using System.Collections.Generic;
using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Knows whether a boss is currently fighting (from aggro until it dies). Save points check this.</summary>
    public static class BossFight
    {
        static readonly List<EnemyController> active = new List<EnemyController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        public static bool InProgress
        {
            get
            {
                active.RemoveAll(b => b == null || b.Health == null || b.Health.IsDead);
                return active.Count > 0;
            }
        }

        internal static void Begin(EnemyController boss)
        {
            if (!active.Contains(boss))
                active.Add(boss);
        }

        internal static void End(EnemyController boss) => active.Remove(boss);
    }
}
