using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Boruhan.Core
{
    /// <summary>Fades to black, loads a scene, fades back in. Creates itself on first use.</summary>
    public class ScreenFader : MonoBehaviour
    {
        const float FadeDuration = 0.4f;

        static ScreenFader instance;

        Image overlay;
        bool busy;

        public static bool IsBusy => instance != null && instance.busy;

        public static void FadeToScene(string sceneName)
        {
            if (instance == null)
                Create();
            if (!instance.busy)
                instance.StartCoroutine(instance.Run(sceneName));
        }

        static void Create()
        {
            var go = new GameObject("EkranGecisi");
            DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            go.AddComponent<GraphicRaycaster>();

            var image = new GameObject("Siyah", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(go.transform, false);
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            instance = go.AddComponent<ScreenFader>();
            instance.overlay = image;
        }

        IEnumerator Run(string sceneName)
        {
            busy = true;
            yield return Fade(0f, 1f);

            var load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone)
                yield return null;

            yield return Fade(1f, 0f);
            busy = false;
        }

        IEnumerator Fade(float from, float to)
        {
            overlay.raycastTarget = true;
            for (float t = 0f; t < FadeDuration; t += Time.unscaledDeltaTime)
            {
                overlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / FadeDuration));
                yield return null;
            }
            overlay.color = new Color(0f, 0f, 0f, to);
            overlay.raycastTarget = to > 0f;
        }
    }
}
