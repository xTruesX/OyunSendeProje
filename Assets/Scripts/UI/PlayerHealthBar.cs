using Boruhan.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Boruhan.UI
{
    /// <summary>Börühan's health bar on the HUD.</summary>
    public class PlayerHealthBar : MonoBehaviour
    {
        [SerializeField] RectTransform fill;
        [SerializeField] Text label;

        Health health;

        void Start()
        {
            var player = GameObject.FindWithTag("Player");
            health = player != null ? player.GetComponent<Health>() : null;
            if (health == null)
            {
                gameObject.SetActive(false);
                return;
            }
            health.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (health != null)
                health.Changed -= Refresh;
        }

        void Refresh()
        {
            fill.anchorMax = new Vector2(health.Ratio, 1f);
            label.text = $"Can  {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
        }
    }
}
