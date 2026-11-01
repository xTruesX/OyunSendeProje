using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>A small health bar floating above a character.</summary>
    [RequireComponent(typeof(Health))]
    public class WorldHealthBar : MonoBehaviour
    {
        const string BarName = "CanBari";

        [SerializeField] float width = 1f;
        [SerializeField] float height = 0.12f;
        [SerializeField] Color fillColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField] Color backColor = new Color(0f, 0f, 0f, 0.7f);
        [Tooltip("Gap between the top of the sprite and the bar.")]
        [SerializeField] float gap = 0.25f;

        Health health;
        Transform root;
        Transform fill;

        void Awake()
        {
            health = GetComponent<Health>();

            // A copied character (e.g. an illusion) comes with the original's bar; replace it.
            var old = transform.Find(BarName);
            if (old != null)
                Destroy(old.gameObject);

            root = new GameObject(BarName).transform;
            root.SetParent(transform, false);
            Bar("Arka", backColor, 100);
            fill = Bar("Dolgu", fillColor, 101);

            health.Changed += Refresh;
            health.Died += () => root.gameObject.SetActive(false);
        }

        void Start()
        {
            // Sit just above the sprite, whatever its size, and ignore the character's scale.
            var sr = GetComponentInChildren<SpriteRenderer>();
            float top = sr != null && sr.sprite != null ? sr.bounds.max.y - transform.position.y : 1.2f;
            var scale = transform.lossyScale;
            root.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
            root.localPosition = new Vector3(0f, (top + gap) / scale.y, 0f);
            Refresh();
        }

        Transform Bar(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CombatFx.Square;
            sr.color = color;
            sr.sortingOrder = order;
            go.transform.localScale = new Vector3(width, height, 1f);
            return go.transform;
        }

        void Refresh()
        {
            float ratio = health.Ratio;
            fill.localScale = new Vector3(width * ratio, height, 1f);
            fill.localPosition = new Vector3(-width * (1f - ratio) / 2f, 0f, 0f);
        }
    }
}
