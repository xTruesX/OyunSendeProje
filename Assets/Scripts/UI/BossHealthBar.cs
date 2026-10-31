using Boruhan.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Boruhan.UI
{
    /// <summary>Big named health bar at the top of the screen, shown when a boss starts fighting.</summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] RectTransform fill;
        [SerializeField] Text nameLabel;

        Health tracked;

        public static BossHealthBar Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            root.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Unhook();
        }

        public void Show(EnemyController boss)
        {
            Unhook();
            tracked = boss.Health;
            tracked.Changed += Refresh;
            tracked.Died += Hide;
            nameLabel.text = boss.Definition.displayName;
            root.SetActive(true);
            Refresh();
        }

        void Refresh() => fill.anchorMax = new Vector2(tracked.Ratio, 1f);

        void Hide()
        {
            Unhook();
            root.SetActive(false);
        }

        void Unhook()
        {
            if (tracked == null)
                return;
            tracked.Changed -= Refresh;
            tracked.Died -= Hide;
            tracked = null;
        }
    }
}
