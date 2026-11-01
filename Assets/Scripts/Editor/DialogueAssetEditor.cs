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
    /// Also makes the empty voice files (Seslendirme) that every line plays.
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
            int missing = asset.lines.Count(l => l.voice == null);
            if (missing > 0 && GUILayout.Button($"Seslendirme: {missing} satır için boş ses dosyası oluştur"))
            {
                CreateVoicePlaceholders(asset);
                WriteVoiceScript();
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Satırlar", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }

        void Apply(DialogueAsset asset)
        {
            var lines = Parse(script, out unknownSpeakers);
            KeepVoices(asset.lines, lines);
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

        /// <summary>Rewritten lines keep their recordings: same text first, otherwise the line at the same place.</summary>
        static void KeepVoices(List<DialogueLine> before, List<DialogueLine> after)
        {
            var unused = before.Where(l => l.voice != null).ToList();
            foreach (var line in after)
            {
                var same = unused.FirstOrDefault(l => l.text == line.text);
                if (same != null)
                {
                    line.voice = same.voice;
                    unused.Remove(same);
                }
            }
            for (int i = 0; i < after.Count && i < before.Count; i++)
            {
                if (after[i].voice == null && unused.Contains(before[i]))
                {
                    after[i].voice = before[i].voice;
                    unused.Remove(before[i]);
                }
            }
        }

        // ================= Seslendirme =================

        const string VoiceDir = "Assets/Audio/Seslendirme";

        /// <summary>
        /// Gives every dialogue line without a recording an empty (silent) WAV in
        /// Assets/Audio/Seslendirme/&lt;dialogue&gt;/&lt;no&gt;_&lt;speaker&gt;.wav. Record over the file (same name, .wav)
        /// and the line plays it. Seslendirme.txt next to them lists every file with its text.
        /// </summary>
        [MenuItem("Börühan/Bakım/Seslendirme Dosyalarını Oluştur")]
        public static void CreateAllVoicePlaceholders()
        {
            int made = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    made += CreateVoicePlaceholders(asset);
            }
            WriteVoiceScript();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Seslendirme] {made} boş ses dosyası oluşturuldu. Liste: {VoiceDir}/Seslendirme.txt");
        }

        public static int CreateVoicePlaceholders(DialogueAsset asset)
        {
            int made = 0;
            for (int i = 0; i < asset.lines.Count; i++)
            {
                var line = asset.lines[i];
                if (line.voice != null)
                    continue;
                var path = VoicePath(asset, i);
                if (!System.IO.File.Exists(path))
                {
                    WriteSilence(path);
                    AssetDatabase.ImportAsset(path);
                    made++;
                }
                line.voice = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                EditorUtility.SetDirty(asset);
            }
            return made;
        }

        static string VoicePath(DialogueAsset asset, int index)
        {
            var line = asset.lines[index];
            var who = line.speaker != null ? FileSafe(line.speaker.displayName) : "Anlatici";
            return $"{VoiceDir}/{asset.name}/{index + 1:00}_{who}.wav";
        }

        static string FileSafe(string s)
        {
            s = s.Replace('ı', 'i').Replace('İ', 'I').Replace('ö', 'o').Replace('Ö', 'O').Replace('ü', 'u').Replace('Ü', 'U')
                 .Replace('ş', 's').Replace('Ş', 'S').Replace('ğ', 'g').Replace('Ğ', 'G').Replace('ç', 'c').Replace('Ç', 'C');
            return new string(s.Where(char.IsLetterOrDigit).ToArray());
        }

        // A tenth of a second of silence, 16-bit mono.
        static void WriteSilence(string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            const int rate = 22050, count = rate / 10;
            using var w = new System.IO.BinaryWriter(System.IO.File.Create(path));
            w.Write("RIFF".ToCharArray()); w.Write(36 + count * 2); w.Write("WAVEfmt ".ToCharArray());
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(count * 2);
            w.Write(new byte[count * 2]);
        }

        /// <summary>Seslendirme.txt: every voice file with who says what, to read from while recording.</summary>
        static void WriteVoiceScript()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Seslendirme listesi. Her dosyanın üzerine aynı adla (.wav) kaydedin.").AppendLine();
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueAsset").OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || asset.lines.Count == 0)
                    continue;
                sb.AppendLine($"== {asset.name} ==");
                foreach (var line in asset.lines)
                {
                    var file = line.voice != null ? AssetDatabase.GetAssetPath(line.voice).Replace(VoiceDir + "/", "") : "(ses yok)";
                    sb.AppendLine($"{file}  |  {(line.speaker != null ? line.speaker.displayName : Narrator)}: {line.text}");
                }
                sb.AppendLine();
            }
            System.IO.Directory.CreateDirectory(VoiceDir);
            System.IO.File.WriteAllText($"{VoiceDir}/Seslendirme.txt", sb.ToString());
            AssetDatabase.ImportAsset($"{VoiceDir}/Seslendirme.txt");
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
