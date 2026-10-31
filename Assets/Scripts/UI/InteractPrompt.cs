using UnityEngine;
using UnityEngine.UI;

namespace Boruhan.UI
{
    /// <summary>"[E] Konuş" hint shown when the player is facing something interactable.</summary>
    public class InteractPrompt : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text label;

        public static InteractPrompt Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(string verb)
        {
            label.text = $"[E] {verb}";
            if (!root.activeSelf)
                root.SetActive(true);
        }

        public void Hide()
        {
            if (root.activeSelf)
                root.SetActive(false);
        }
    }
}
