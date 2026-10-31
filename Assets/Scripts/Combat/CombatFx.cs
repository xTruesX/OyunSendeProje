using UnityEngine;

namespace Boruhan.Combat
{
    /// <summary>Runtime shapes and small effects for combat: telegraphs, rings, flashes, hit-stop.</summary>
    public static class CombatFx
    {
        static Sprite circle;
        static Sprite square;

        public static Sprite Circle => circle != null ? circle : circle = MakeCircle(64);
        public static Sprite Square => square != null ? square : square = MakeSquare();

        /// <summary>A flat colored shape on the ground (telegraphs, bars). Drawn under characters.</summary>
        public static GameObject Shape(Sprite sprite, Vector2 position, Vector2 size, Color color, int order = -50, float angle = 0f)
        {
            var go = new GameObject("Efekt");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            var bounds = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1f);
            return go;
        }

        /// <summary>A shape that grows from one size to another while fading out, then removes itself.</summary>
        public static void Burst(Sprite sprite, Vector2 position, float fromSize, float toSize, Color color, float duration, int order = 5, float angle = 0f)
        {
            var go = Shape(sprite, position, Vector2.one * fromSize, color, order, angle);
            go.AddComponent<FadeEffect>().Init(duration, toSize / Mathf.Max(0.01f, fromSize));
        }

        /// <summary>Freezes the game for a moment so hits feel heavy.</summary>
        public static void HitStop(float seconds)
        {
            if (Time.timeScale < 1f)
                return;
            new GameObject("HitStop").AddComponent<HitStopEffect>().Init(seconds);
        }

        static Sprite MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeSquare()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        class FadeEffect : MonoBehaviour
        {
            float duration;
            float elapsed;
            Vector3 startScale;
            Vector3 endScale;
            SpriteRenderer sr;
            Color color;

            public void Init(float seconds, float growth)
            {
                duration = Mathf.Max(0.01f, seconds);
                sr = GetComponent<SpriteRenderer>();
                color = sr.color;
                startScale = transform.localScale;
                endScale = startScale * growth;
            }

            void Update()
            {
                elapsed += Time.deltaTime;
                float k = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, endScale, k);
                sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
                if (k >= 1f)
                    Destroy(gameObject);
            }
        }

        class HitStopEffect : MonoBehaviour
        {
            const float FrozenScale = 0.05f;
            float remaining;

            public void Init(float seconds)
            {
                remaining = seconds;
                Time.timeScale = FrozenScale;
            }

            void Update()
            {
                remaining -= Time.unscaledDeltaTime;
                if (remaining > 0f)
                    return;
                // Only undo our own slow-down: if the game was paused meanwhile, leave it paused.
                if (Mathf.Approximately(Time.timeScale, FrozenScale))
                    Time.timeScale = 1f;
                Destroy(gameObject);
            }
        }
    }
}
