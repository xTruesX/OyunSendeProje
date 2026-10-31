using System;
using System.Collections.Generic;
using Boruhan.Combat;
using UnityEditor;
using UnityEngine;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Enemy definition inspector: stats on top, then the moveset (Hareket Seti) with every ability
    /// editable in place, buttons to add new abilities, and a button to place the enemy in the scene.
    /// Abilities added here live inside the enemy's own file.
    /// </summary>
    [CustomEditor(typeof(EnemyDefinition))]
    public class EnemyDefinitionEditor : Editor
    {
        static readonly (string label, Type type, string help)[] AbilityTypes =
        {
            ("Yakın Saldırı", typeof(MeleeStrikeAbility), "Önündeki alana bir ya da birkaç vuruş"),
            ("Atılma", typeof(LungeAbility), "Uyarı çizgisi, sonra hızla üzerine atılır"),
            ("Mermi", typeof(ProjectileAbility), "Tek, yelpaze ya da seri atış"),
            ("Işınlanma", typeof(TeleportAbility), "Arkana / yakınına / uzağa; ardından başka bir hamle"),
            ("Yanılsama", typeof(SummonIllusionsAbility), "Tek vuruşta kırılan kopyalar"),
            ("Yer Sarsma", typeof(AreaSlamAbility), "Etrafına dalga dalga alan saldırısı"),
            ("Kalkan", typeof(GuardAbility), "Hafif vuruşları emer; ağır saldırı kırar"),
            ("Korku (Yavaşlatma)", typeof(CurseAbility), "Menzildeysen seni yavaşlatır"),
            ("Ayna (Yansıtma)", typeof(ReflectAbility), "Kısa süre vuruşları sana geri yansıtır"),
        };

        readonly Dictionary<UnityEngine.Object, Editor> editors = new Dictionary<UnityEngine.Object, Editor>();
        readonly HashSet<UnityEngine.Object> expanded = new HashSet<UnityEngine.Object>();

        void OnDisable()
        {
            foreach (var e in editors.Values)
                DestroyImmediate(e);
            editors.Clear();
        }

        public override void OnInspectorGUI()
        {
            var def = (EnemyDefinition)target;
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "abilities");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Hareket Seti ({def.abilities.Count})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Yapay zeka, hazır olan ve oyuncuya olan mesafeye uyan yeteneklerden birini (ağırlığa göre) seçer. " +
                                    "Menzil, bekleme süresi ve uyarı süresi her yeteneğin üst kısmında.", MessageType.None);

            for (int i = 0; i < def.abilities.Count; i++)
            {
                if (DrawAbility(def, i))
                    break;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Yetenek Ekle", EditorStyles.miniBoldLabel);
            for (int i = 0; i < AbilityTypes.Length; i += 3)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = i; j < Mathf.Min(i + 3, AbilityTypes.Length); j++)
                    {
                        var (label, type, help) = AbilityTypes[j];
                        if (GUILayout.Button(new GUIContent(label, help)))
                        {
                            Undo.RegisterCompleteObjectUndo(def, "Yetenek ekle");
                            var ability = CombatKit.AddAbility(def, type, label);
                            expanded.Add(ability);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Sahneye Yerleştir", GUILayout.Height(26)))
                Place(def);
        }

        /// <summary>Returns true if the list changed (stop drawing this frame).</summary>
        bool DrawAbility(EnemyDefinition def, int index)
        {
            var ability = def.abilities[index];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool open = ability != null && expanded.Contains(ability);
                    string title = ability == null ? "(boş)" : $"{ability.displayName}   —   {TypeLabel(ability)}";
                    bool now = EditorGUILayout.Foldout(open, title, true);
                    if (ability != null && now != open)
                    {
                        if (now)
                            expanded.Add(ability);
                        else
                            expanded.Remove(ability);
                    }

                    using (new EditorGUI.DisabledScope(index == 0))
                    {
                        if (GUILayout.Button("▲", GUILayout.Width(24)))
                        {
                            Move(def, index, -1);
                            return true;
                        }
                    }
                    using (new EditorGUI.DisabledScope(index == def.abilities.Count - 1))
                    {
                        if (GUILayout.Button("▼", GUILayout.Width(24)))
                        {
                            Move(def, index, 1);
                            return true;
                        }
                    }
                    if (GUILayout.Button("✕", GUILayout.Width(24)) && EditorUtility.DisplayDialog("Yeteneği sil", $"'{(ability != null ? ability.displayName : "boş")}' silinsin mi?", "Sil", "Vazgeç"))
                    {
                        Remove(def, index);
                        return true;
                    }
                }

                if (ability != null && expanded.Contains(ability))
                {
                    if (!editors.TryGetValue(ability, out var editor) || editor == null)
                        editors[ability] = editor = CreateEditor(ability);

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel++;
                    editor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                    // Keep the sub-asset's name in the Project window matching its display name.
                    if (EditorGUI.EndChangeCheck() && ability.name != ability.displayName && AssetDatabase.IsSubAsset(ability))
                    {
                        ability.name = ability.displayName;
                        EditorUtility.SetDirty(def);
                    }
                }
            }
            return false;
        }

        static string TypeLabel(EnemyAbility ability)
        {
            foreach (var (label, type, _) in AbilityTypes)
            {
                if (type == ability.GetType())
                    return label;
            }
            return ability.GetType().Name;
        }

        static void Move(EnemyDefinition def, int index, int delta)
        {
            Undo.RecordObject(def, "Yetenek sırası");
            var a = def.abilities[index];
            def.abilities.RemoveAt(index);
            def.abilities.Insert(index + delta, a);
            EditorUtility.SetDirty(def);
        }

        static void Remove(EnemyDefinition def, int index)
        {
            var ability = def.abilities[index];
            def.abilities.RemoveAt(index);
            EditorUtility.SetDirty(def);
            if (ability != null && AssetDatabase.GetAssetPath(ability) == AssetDatabase.GetAssetPath(def))
            {
                AssetDatabase.RemoveObjectFromAsset(ability);
                DestroyImmediate(ability, true);
            }
            AssetDatabase.SaveAssets();
        }

        static void Place(EnemyDefinition def)
        {
            var view = SceneView.lastActiveSceneView;
            Vector2 position = view != null ? (Vector2)view.pivot : Vector2.zero;
            var go = CombatKit.PlaceEnemy(def, position, null);
            Undo.RegisterCreatedObjectUndo(go, "Düşman yerleştir");
            Selection.activeGameObject = go;
        }
    }
}
