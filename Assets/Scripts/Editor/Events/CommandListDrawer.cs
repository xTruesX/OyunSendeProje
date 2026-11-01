using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Boruhan.Events;
using UnityEditor;
using UnityEngine;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Draws an editable command list (and the lists nested inside commands, like "O zaman / Değilse"
    /// or choice options) as a tree, with a categorized "+ Komut Ekle" menu.
    /// </summary>
    public static class CommandListDrawer
    {
        static readonly Dictionary<string, bool> expanded = new Dictionary<string, bool>();
        static List<(string path, Type type, string help)> commandTypes;

        static List<(string path, Type type, string help)> CommandTypes
        {
            get
            {
                if (commandTypes != null)
                    return commandTypes;
                commandTypes = TypeCache.GetTypesDerivedFrom<EventCommand>()
                    .Where(t => !t.IsAbstract)
                    .Select(t =>
                    {
                        var info = t.GetCustomAttribute<CommandInfoAttribute>();
                        return (info != null ? info.Path : "Diğer/" + t.Name, t, info != null ? info.Help : "");
                    })
                    .OrderBy(x => x.Item1)
                    .ToList();
                return commandTypes;
            }
        }

        public static void DrawList(SerializedProperty list, string label, int depth = 0)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 14);
                EditorGUILayout.LabelField($"{label} ({list.arraySize})", EditorStyles.boldLabel);
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                var command = element.managedReferenceValue as EventCommand;
                string key = element.propertyPath + "#" + element.managedReferenceId;
                expanded.TryGetValue(key, out bool open);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(depth * 14);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var title = $"{i + 1}. {(command != null ? command.Summary : "(boş)")}";
                            bool now = EditorGUILayout.Foldout(open, title, true);
                            if (now != open)
                                expanded[key] = now;

                            using (new EditorGUI.DisabledScope(i == 0))
                            {
                                if (GUILayout.Button("▲", GUILayout.Width(22)))
                                {
                                    list.MoveArrayElement(i, i - 1);
                                    return;
                                }
                            }
                            using (new EditorGUI.DisabledScope(i == list.arraySize - 1))
                            {
                                if (GUILayout.Button("▼", GUILayout.Width(22)))
                                {
                                    list.MoveArrayElement(i, i + 1);
                                    return;
                                }
                            }
                            if (GUILayout.Button("✕", GUILayout.Width(22)))
                            {
                                list.DeleteArrayElementAtIndex(i);
                                return;
                            }
                        }

                        if (open && command != null)
                            DrawCommandBody(element, command.GetType(), depth);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 14);
                if (GUILayout.Button("+ Komut Ekle", GUILayout.Width(120)))
                    ShowAddMenu(list);
            }
        }

        static void ShowAddMenu(SerializedProperty list)
        {
            var so = list.serializedObject;
            string path = list.propertyPath;
            var menu = new GenericMenu();
            foreach (var (menuPath, type, _) in CommandTypes)
            {
                menu.AddItem(new GUIContent(menuPath), false, () =>
                {
                    so.Update();
                    var l = so.FindProperty(path);
                    l.arraySize++;
                    var element = l.GetArrayElementAtIndex(l.arraySize - 1);
                    element.managedReferenceValue = Activator.CreateInstance(type);
                    expanded[element.propertyPath + "#" + element.managedReferenceId] = true;
                    so.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        static void DrawCommandBody(SerializedProperty element, Type type, int depth)
        {
            var info = type.GetCustomAttribute<CommandInfoAttribute>();
            if (info != null && !string.IsNullOrEmpty(info.Help))
                EditorGUILayout.LabelField(info.Help, EditorStyles.wordWrappedMiniLabel);

            var it = element.Copy();
            var end = element.GetEndProperty();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, end))
            {
                enterChildren = false;
                var field = type.GetField(it.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(List<EventCommand>))
                    DrawList(it.Copy(), it.displayName == "Then" ? "O zaman" : it.displayName == "Otherwise" ? "Değilse" : it.displayName, depth + 1);
                else if (field != null && field.FieldType == typeof(List<ChoiceOption>))
                    DrawChoices(it.Copy(), depth + 1);
                else
                    EditorGUILayout.PropertyField(it, true);
            }
        }

        static void DrawChoices(SerializedProperty options, int depth)
        {
            for (int i = 0; i < options.arraySize; i++)
            {
                var option = options.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(depth * 14);
                    EditorGUILayout.PropertyField(option.FindPropertyRelative("text"), new GUIContent($"Seçenek {i + 1}"));
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        options.DeleteArrayElementAtIndex(i);
                        return;
                    }
                }
                DrawList(option.FindPropertyRelative("commands"), $"“{option.FindPropertyRelative("text").stringValue}” seçilirse", depth + 1);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 14);
                if (GUILayout.Button("+ Seçenek", GUILayout.Width(120)))
                {
                    options.arraySize++;
                    var added = options.GetArrayElementAtIndex(options.arraySize - 1);
                    added.FindPropertyRelative("text").stringValue = "Yeni seçenek";
                    added.FindPropertyRelative("commands").ClearArray();
                }
            }
        }
    }

    /// <summary>Draws a condition on one line: [kind] [name] [value].</summary>
    [CustomPropertyDrawer(typeof(EventCondition))]
    public class EventConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var kind = property.FindPropertyRelative("kind");
            var key = property.FindPropertyRelative("key");
            var value = property.FindPropertyRelative("value");
            var k = (ConditionKind)kind.enumValueIndex;
            bool hasKey = k != ConditionKind.HealthShardsAtLeast && k != ConditionKind.KutShardsAtLeast && k != ConditionKind.BossFightActive;
            bool hasValue = k != ConditionKind.FlagSet && k != ConditionKind.FlagNotSet && k != ConditionKind.BossFightActive;

            float w = position.width;
            var r1 = new Rect(position.x, position.y, w * 0.38f, position.height);
            var r2 = new Rect(position.x + w * 0.4f, position.y, hasValue ? w * 0.38f : w * 0.6f, position.height);
            var r3 = new Rect(position.x + w * 0.8f, position.y, w * 0.2f, position.height);
            EditorGUI.PropertyField(r1, kind, GUIContent.none);
            if (hasKey)
                EditorGUI.PropertyField(r2, key, GUIContent.none);
            if (hasValue)
                EditorGUI.PropertyField(hasKey ? r3 : r2, value, GUIContent.none);
        }
    }
}
