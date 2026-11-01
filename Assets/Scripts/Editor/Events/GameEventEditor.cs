using System.Linq;
using Boruhan.Events;
using UnityEditor;
using UnityEngine;

namespace Boruhan.EditorTools
{
    [CustomEditor(typeof(GameEvent))]
    public class GameEventEditor : Editor
    {
        int page;

        public override void OnInspectorGUI()
        {
            var ev = (GameEvent)target;
            serializedObject.Update();
            var pages = serializedObject.FindProperty("pages");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Olay ID", ev.Id, EditorStyles.miniLabel);
                // Duplicated objects share an id, which would make them share 'self:' flags.
                bool duplicate = FindObjectsByType<GameEvent>(FindObjectsInactive.Include).Count(e => e.Id == ev.Id) > 1;
                if (GUILayout.Button(duplicate ? "Yeni ID (kopya!)" : "Yeni ID", GUILayout.Width(duplicate ? 120 : 70)))
                {
                    Undo.RecordObject(ev, "Yeni ID");
                    ev.RegenerateId();
                    EditorUtility.SetDirty(ev);
                }
            }
            EditorGUILayout.HelpBox("Koşulları sağlanan EN SON sayfa çalışır. 'self:x' bayrakları sadece bu olaya aittir.", MessageType.None);

            page = Mathf.Clamp(page, 0, Mathf.Max(0, pages.arraySize - 1));
            using (new EditorGUILayout.HorizontalScope())
            {
                var names = Enumerable.Range(0, pages.arraySize).Select(i => $"Sayfa {i + 1}" + (Application.isPlaying && ev.ActivePageIndex == i ? " ●" : "")).ToArray();
                if (names.Length > 0)
                    page = GUILayout.Toolbar(page, names);
                if (GUILayout.Button("+", GUILayout.Width(24)))
                {
                    pages.arraySize++;
                    page = pages.arraySize - 1;
                    var added = pages.GetArrayElementAtIndex(page);
                    added.FindPropertyRelative("conditions").ClearArray();
                    added.FindPropertyRelative("commands").ClearArray();
                    added.FindPropertyRelative("note").stringValue = "";
                }
                using (new EditorGUI.DisabledScope(pages.arraySize <= 1))
                {
                    if (GUILayout.Button("−", GUILayout.Width(24)) && EditorUtility.DisplayDialog("Sayfayı sil", $"Sayfa {page + 1} silinsin mi?", "Sil", "Vazgeç"))
                    {
                        pages.DeleteArrayElementAtIndex(page);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                }
            }

            if (pages.arraySize == 0)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var p = pages.GetArrayElementAtIndex(page);
            EditorGUILayout.PropertyField(p.FindPropertyRelative("note"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Koşullar (hepsi sağlanmalı)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindPropertyRelative("conditions"), GUIContent.none, true);
            EditorGUILayout.PropertyField(p.FindPropertyRelative("trigger"), new GUIContent("Tetiklenme"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Görünüm", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindPropertyRelative("sprite"), new GUIContent("Görsel"));
            EditorGUILayout.PropertyField(p.FindPropertyRelative("tint"), new GUIContent("Renk"));
            EditorGUILayout.PropertyField(p.FindPropertyRelative("solid"), new GUIContent("Geçilmez"));
            EditorGUILayout.PropertyField(p.FindPropertyRelative("size"), new GUIContent("Boyut"));
            EditorGUILayout.PropertyField(p.FindPropertyRelative("prompt"), new GUIContent("Etkileşim Yazısı"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Hareket", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindPropertyRelative("movement"), new GUIContent("Tür"));
            EditorGUILayout.PropertyField(p.FindPropertyRelative("moveSpeed"), new GUIContent("Hız"));
            EditorGUILayout.PropertyField(p.FindPropertyRelative("wanderRadius"), new GUIContent("Dolaşma Yarıçapı"));
            EditorGUILayout.PropertyField(p.FindPropertyRelative("freezePlayer"), new GUIContent("Oyuncuyu Durdur"));

            EditorGUILayout.Space(8);
            CommandListDrawer.DrawList(p.FindPropertyRelative("commands"), "Komutlar");

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(CommonEvent))]
    public class CommonEventEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("note"), new GUIContent("Not"));
            EditorGUILayout.HelpBox("Herhangi bir olaydan 'Akış > Ortak Olay Çağır' ile çalıştırılır. 'self:' bayrakları onu çağıran olaya aittir.", MessageType.None);
            CommandListDrawer.DrawList(serializedObject.FindProperty("commands"), "Komutlar");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
