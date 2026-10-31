using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Boruhan.UI
{
    /// <summary>
    /// Shows a small circle to the left of a button's text while it's selected.
    /// Hovering with the mouse selects the button, so mouse and keyboard share one marker.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class SelectionMarker : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [Tooltip("Custom marker image (e.g. a star). Leave empty for the default circle.")]
        [SerializeField] Sprite sprite;
        [SerializeField] float diameter = 18f;
        [SerializeField] float gap = 22f;
        [Tooltip("Leave transparent to use the button text's color (circle) or the image's own colors (custom sprite).")]
        [SerializeField] Color color = Color.clear;

        static Sprite circleSprite;

        Selectable selectable;
        Text label;
        Image marker;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            label = GetComponentInChildren<Text>(true);

            var go = new GameObject("SecimIsareti", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            marker = go.AddComponent<Image>();
            marker.raycastTarget = false;
            marker.preserveAspect = true;

            var rect = marker.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(diameter, diameter);

            ApplySprite();
            go.SetActive(false);
        }

        /// <summary>Swaps the marker image at runtime. Null goes back to the default circle.</summary>
        public void SetSprite(Sprite newSprite)
        {
            sprite = newSprite;
            ApplySprite();
        }

        void ApplySprite()
        {
            marker.sprite = sprite != null ? sprite : GetCircleSprite();

            if (color.a > 0f)
                marker.color = color;
            else if (sprite != null)
                marker.color = Color.white;
            else
                marker.color = label != null ? label.color : Color.white;
        }

        void OnDisable() => marker.gameObject.SetActive(false);

        public void OnSelect(BaseEventData eventData) => Show();

        public void OnDeselect(BaseEventData eventData) => marker.gameObject.SetActive(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable.IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        void Show()
        {
            // Position is recomputed each time because labels (e.g. save slots) can change.
            float x = -((RectTransform)transform).rect.width / 2f;
            if (label != null)
                x = TextLeftEdge();

            marker.rectTransform.anchoredPosition = new Vector2(x - gap - diameter / 2f, 0f);
            marker.gameObject.SetActive(true);
        }

        /// <summary>Left edge of the visible text, in the button's local space.</summary>
        float TextLeftEdge()
        {
            var textRect = label.rectTransform;
            var rect = textRect.rect;
            float textWidth = Mathf.Min(label.preferredWidth, rect.width);
            float offset = textRect.localPosition.x;

            switch (label.alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    return offset + rect.xMin;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return offset + rect.xMax - textWidth;
                default:
                    return offset + rect.center.x - textWidth / 2f;
            }
        }

        static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
                return circleSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                byte alpha = (byte)(Mathf.Clamp01(radius - dist) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circleSprite;
        }
    }
}
