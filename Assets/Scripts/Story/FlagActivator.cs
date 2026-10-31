using Boruhan.Core;
using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>Shows or hides objects depending on a story flag, and updates live when the flag gets set.</summary>
    public class FlagActivator : MonoBehaviour
    {
        [SerializeField] string flag;
        [Tooltip("On: targets appear once the flag is set. Off: targets disappear once the flag is set.")]
        [SerializeField] bool activeWhenSet = true;
        [SerializeField] GameObject[] targets;

        void OnEnable() => GameSession.FlagChanged += OnFlagChanged;
        void OnDisable() => GameSession.FlagChanged -= OnFlagChanged;

        void Start() => Apply();

        void OnFlagChanged(string changed)
        {
            if (changed == flag)
                Apply();
        }

        void Apply()
        {
            bool active = GameSession.HasFlag(flag) == activeWhenSet;
            foreach (var target in targets)
            {
                if (target != null)
                    target.SetActive(active);
            }
        }
    }
}
