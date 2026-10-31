using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>Draws invisible trigger zones (doors, story triggers) in the Scene view with a label.</summary>
    static class ZoneGizmo
    {
        public static void Draw(Component owner, Color color, string label)
        {
#if UNITY_EDITOR
            var col = owner.GetComponent<BoxCollider2D>();
            Vector3 center = owner.transform.position;
            Vector3 size = Vector3.one * 0.5f;
            if (col != null)
            {
                center = owner.transform.TransformPoint(col.offset);
                size = Vector3.Scale(col.size, owner.transform.lossyScale);
            }

            Gizmos.color = new Color(color.r, color.g, color.b, 0.25f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);

            var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { normal = { textColor = color } };
            UnityEditor.Handles.Label(center + Vector3.up * (size.y / 2f + 0.4f), label, style);
#endif
        }
    }
}
