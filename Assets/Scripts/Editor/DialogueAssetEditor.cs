using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Boruhan.Dialogue;
using UnityEditor;
using UnityEngine;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Adds "Hızlı Yazım" to dialogue assets: write the whole conversation as plain text, one line per row.
    ///   Börühan: Babam beni bekliyor.
    ///   Anlatıcı: Yer yarıldı! [bayrak: yarik_acildi]
    ///   Kayra Han: Yolun açık olsun. [süre: 4]
    /// </summary>
    [CustomEditor(typeof(DialogueAsset))]
    public class DialogueAssetEditor : Editor
    {
        const string Narrator = "Anlatıcı";
        static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
        static readonly Regex TagPattern = new Regex(@"\[\s*(bayrak|süre|sure)\s*:\s*([^\]]*)\]", RegexOptions.IgnoreCase);

        static bool showQuickWrite = true;
        string script;
        List<string> unknownSpeakers = new List<string>();

        void OnEnable() => script = ToScript((DialogueAsset)target);

        public override void OnInspectorGUI()
        {
            var asset = (DialogueAsset)target;

            showQuickWrite = EditorGUILayout.Foldout(showQuickWrite, "Hızlı Yazım (metin olarak düzenle)", true, EditorStyles.foldoutHeader);
            if (showQuickWrite)
            {
                EditorGUILayout.HelpBox(
                    "Her satır bir konuşma. \"İsim: metin\" yazın; isim bir Konuşmacı'nın adı olmalı. " +
                    "Anlatım için \"Anlatıcı: metin\" ya da sadece metin.\n" +
                    "İsteğe bağlı: [bayrak: bayrak_adi]  [süre: 4]", MessageType.None);

                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                script = EditorGUILayout.TextArea(script, style, GUILayout.MinHeight(180));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Metni Uygula", GUILayout.Height(24)))
                        Apply(asset);
                    if (GUILayout.Button("Satırlardan Yenile", GUILayout.Height(24)))
                    {
                        script = ToScript(asset);
                        unknownSpeakers.Clear();
                        GUI.FocusControl(null);
                    }
                }

                if (unknownSpeakers.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Bilinmeyen konuşmacılar (anlatım olarak eklendi): {string.Join(", ", unknownSpeakers)}", MessageType.Warning);
                    if (GUILayout.Button("Bunları Konuşmacı Olarak Oluştur ve Tekrar Uygula"))
                    {
                        foreach (var name in unknownSpeakers.ToList())
                            CreateSpeaker(name);
                        Apply(asset);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Satırlar", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }

        void Apply(DialogueAsset asset)
        {
            var lines = Parse(script, out unknownSpeakers);
            Undo.RecordObject(asset, "Diyalog metni uygula");
            asset.lines = lines;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            GUI.FocusControl(null);
        }

        /// <summary>Turns the quick-write text into dialogue lines. Names that aren't speakers end up in unknown.</summary>
        public static List<DialogueLine> Parse(string text, out List<string> unknown)
        {
            var speakers = AllSpeakers();
            unknown = new List<string>();
            var lines = new List<DialogueLine>();

            foreach (var raw in text.Split('\n'))
            {
                var row = raw.Trim();
                if (row.Length == 0)
                    continue;

                var line = new DialogueLine();
                foreach (Match m in TagPattern.Matches(row))
                {
                    var key = m.Groups[1].Value.ToLower(Turkish);
                    var value = m.Groups[2].Value.Trim();
                    if (key == "bayrak")
                        line.setFlag = value;
                    else if (float.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                        line.duration = seconds;
                }
                row = TagPattern.Replace(row, "").Trim();

                int colon = row.IndexOf(':');
                if (colon > 0)
                {
                    var name = row.Substring(0, colon).Trim();
                    var body = row.Substring(colon + 1).Trim();
                    if (Same(name, Narrator))
                    {
                        row = body;
                    }
                    else if (speakers.TryGetValue(name.ToLower(Turkish), out var speaker))
                    {
                        line.speaker = speaker;
                        row = body;
                    }
                    else if (LooksLikeName(name) && !unknown.Contains(name))
                    {
                        unknown.Add(name);
                    }
                }

                line.text = row;
                lines.Add(line);
            }
            return lines;
        }

        public static string ToScript(DialogueAsset asset)
        {
            var sb = new StringBuilder();
            foreach (var line in asset.lines)
            {
                sb.Append(line.speaker != null ? line.speaker.displayName : Narrator).Append(": ").Append(line.text);
                if (!string.IsNullOrEmpty(line.setFlag))
                    sb.Append($" [bayrak: {line.setFlag}]");
                if (line.duration > 0f)
                    sb.Append($" [süre: {line.duration.ToString(CultureInfo.InvariantCulture)}]");
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static Dictionary<string, DialogueSpeaker> AllSpeakers()
        {
            var result = new Dictionary<string, DialogueSpeaker>();
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueSpeaker"))
            {
                var speaker = AssetDatabase.LoadAssetAtPath<DialogueSpeaker>(AssetDatabase.GUIDToAssetPath(guid));
                if (speaker == null)
                    continue;
                result[speaker.displayName.ToLower(Turkish)] = speaker;
                result.TryAdd(speaker.name.ToLower(Turkish), speaker);
            }
            return result;
        }

        static void CreateSpeaker(string displayName)
        {
            const string dir = "Assets/Dialogue/Konusmacilar";
            System.IO.Directory.CreateDirectory(dir);
            var file = new string(displayName.Where(char.IsLetterOrDigit).ToArray());
            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{(file.Length > 0 ? file : "Konusmaci")}.asset");
            var speaker = CreateInstance<DialogueSpeaker>();
            speaker.displayName = displayName;
            AssetDatabase.CreateAsset(speaker, path);
        }

        // "Börühan" or "Kayra Han" – not a sentence like "Sonra dedi ki".
        static bool LooksLikeName(string s) => s.Length <= 24 && s.Split(' ').Length <= 3 && !s.Any(c => ".,!?\"".Contains(c));

        static bool Same(string a, string b) => string.Compare(a, b, Turkish, CompareOptions.IgnoreCase) == 0;
    }
}
