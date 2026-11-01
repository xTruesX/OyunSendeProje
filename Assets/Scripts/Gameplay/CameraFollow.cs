using UnityEngine;

namespace Boruhan.Gameplay
{
    /// <summary>Follows the target and keeps the view inside the room bounds.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Rect bounds = new Rect(-9f, -6f, 18f, 12f);
        [SerializeField] float shakeStrength = 0.15f;

        Camera cam;
        float shakeTimeLeft;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    target = player.transform;
            }
        }

        /// <summary>Shakes the camera for the given number of seconds (e.g. the ground splitting open).</summary>
        public void Shake(float duration) => shakeTimeLeft = Mathf.Max(shakeTimeLeft, duration);

        void LateUpdate()
        {
            if (target == null)
                return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector2 p = target.position;

            // If the room is smaller than the view on an axis, center on that axis instead of clamping.
            p.x = bounds.width > halfWidth * 2f ? Mathf.Clamp(p.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth) : bounds.center.x;
            p.y = bounds.height > halfHeight * 2f ? Mathf.Clamp(p.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight) : bounds.center.y;

            if (shakeTimeLeft > 0f)
            {
                shakeTimeLeft -= Time.deltaTime;
                p += Random.insideUnitCircle * shakeStrength;
            }

            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
