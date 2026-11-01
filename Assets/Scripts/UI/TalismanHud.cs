using UnityEngine;
using UnityEngine.UI;

namespace Boruhan.UI
{
    /// <summary>The talisman icon in the corner of the screen. TalismanGuide drives how bright it is.</summary>
    public class TalismanHud : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image icon;
        [SerializeField] Image halo;
        [SerializeField] Color dimColor = new Color(0.35f, 0.4f, 0.5f, 0.6f);
        [SerializeField] Color litColor = new Color(0.55f, 0.9f, 1f, 1f);

        public static TalismanHud Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            SetVisible(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetVisible(bool visible)
        {
            if (root.activeSelf != visible)
                root.SetActive(visible);
        }

        public void SetGlow(float amount)
        {
            icon.color = Color.Lerp(dimColor, litColor, amount);
            // Gentle pulse when lit so it reads as "glowing".
            float pulse = 1f + amount * 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
            icon.rectTransform.localScale = Vector3.one * pulse;
            halo.color = new Color(litColor.r, litColor.g, litColor.b, amount * 0.6f);
            halo.rectTransform.localScale = Vector3.one * (0.8f + amount * 0.6f);
        }
    }
}
