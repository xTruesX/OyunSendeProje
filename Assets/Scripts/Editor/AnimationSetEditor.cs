using System.Linq;
using Boruhan.Animations;
using UnityEditor;
using UnityEngine;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Animasyon Seti inspector: a live preview, and one box per animation where sprites or a whole
    /// sprite strip can be dragged in. A strip that isn't cut into frames yet is sliced automatically.
    /// </summary>
    [CustomEditor(typeof(AnimationSet))]
    public class AnimationSetEditor : Editor
    {
        int previewIndex;
        bool previewLeft;
        int stripFrames;
        double previewStart;

        // "Sprite Sayfası İçe Aktar"
        bool showSheet;
        Texture2D sheet;
        AnimState sheetState = AnimState.Walk;
        string sheetCustom = "";
        int sheetColumns = 4;
        int sheetRows = 4;
        float sheetFps = 8f;
        bool sheetLoop = true;
        AnimDirection[] sheetRowDirections = { AnimDirection.Down, AnimDirection.Up, AnimDirection.Left, AnimDirection.Right };

        public override bool RequiresConstantRepaint() => true;

        void OnEnable() => previewStart = EditorApplication.timeSinceStartup;

        public override void OnInspectorGUI()
        {
            var set = (AnimationSet)target;
            EditorGUILayout.HelpBox(
                "Her animasyona kareleri sürükleyip bırakın: tek tek sprite'lar ya da yan yana dizilmiş kareler içeren bir resim (şerit). " +
                "Şerit henüz kesilmemişse otomatik olarak eşit karelere bölünür.\n" +
                "Eksik animasyonlar yedeğe düşer: Ağır Saldırı yoksa Saldırı, Yürü yoksa Bekle, hiçbiri yoksa düz sprite.",
                MessageType.Info);

            DrawPreview(set);

            EditorGUI.BeginChangeCheck();
            bool mirror = EditorGUILayout.Toggle(new GUIContent("Sola Bakınca Aynala", "Yan kareleri sağa bakacak şekilde çizin."), set.mirrorForLeft);
            stripFrames = EditorGUILayout.IntField(new GUIContent("Şerit Kare Sayısı", "Kesilmemiş bir şerit bırakıldığında kaç kareye bölünsün. 0 = kare kareler (genişlik ÷ yükseklik)."), stripFrames);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(set, "Animasyon Seti");
                set.mirrorForLeft = mirror;
                EditorUtility.SetDirty(set);
            }

            DrawSheetImport(set);

            EditorGUILayout.Space(6);
            for (int i = 0; i < set.clips.Count; i++)
            {
                if (DrawClip(set, i))
                    break;
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ Animasyon Ekle", GUILayout.Width(140)))
            {
                var menu = new GenericMenu();
                foreach (AnimState state in System.Enum.GetValues(typeof(AnimState)))
                {
                    var s = state;
                    menu.AddItem(new GUIContent(s.Turkish()), false, () =>
                    {
                        Undo.RecordObject(set, "Animasyon Ekle");
                        set.clips.Add(new AnimClip { state = s, loop = s == AnimState.Idle || s == AnimState.Walk || s == AnimState.Stagger, customName = s == AnimState.Custom ? "yeni" : "" });
                        EditorUtility.SetDirty(set);
                    });
                }
                menu.ShowAsContext();
            }
        }

        /// <summary>A sheet with one row per facing (or a single row) becomes one animation per row.</summary>
        void DrawSheetImport(AnimationSet set)
        {
            showSheet = EditorGUILayout.Foldout(showSheet, "Sprite Sayfası İçe Aktar (satır başına bir yön)", true);
            if (!showSheet)
                return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Kareler eşit boyutta bir ızgarada olmalı. Her satır bir yön (ya da tek satır = Hepsi). " +
                                           "Bu durumun eski animasyonlarının yerine geçer.", EditorStyles.wordWrappedMiniLabel);
                sheet = (Texture2D)EditorGUILayout.ObjectField("Resim", sheet, typeof(Texture2D), false);
                sheetState = (AnimState)EditorGUILayout.EnumPopup("Durum", sheetState);
                if (sheetState == AnimState.Custom)
                    sheetCustom = EditorGUILayout.TextField("Özel Ad", sheetCustom);
                sheetColumns = Mathf.Max(1, EditorGUILayout.IntField("Sütun (kare sayısı)", sheetColumns));
                sheetRows = Mathf.Clamp(EditorGUILayout.IntField("Satır (yön sayısı)", sheetRows), 1, 8);
                if (sheetRowDirections.Length != sheetRows)
                {
                    var old = sheetRowDirections;
                    sheetRowDirections = new AnimDirection[sheetRows];
                    for (int i = 0; i < sheetRows; i++)
                        sheetRowDirections[i] = sheetRows == 1 ? AnimDirection.Any : i < old.Length ? old[i] : AnimDirection.Any;
                }
                for (int i = 0; i < sheetRows; i++)
                    sheetRowDirections[i] = (AnimDirection)EditorGUILayout.EnumPopup($"  {i + 1}. satır", sheetRowDirections[i]);
                sheetFps = Mathf.Max(0.1f, EditorGUILayout.FloatField(new GUIContent("Hız (kare/sn)", "1000 ÷ milisaniye. 120 ms ≈ 8.3"), sheetFps));
                sheetLoop = EditorGUILayout.Toggle("Döngü", sheetLoop);
                using (new EditorGUI.DisabledScope(sheet == null))
                {
                    if (GUILayout.Button("İçe Aktar"))
                    {
                        var other = set.clips.SelectMany(c => c.frames).FirstOrDefault(s => s != null);
                        AnimationKit.ImportSheet(set, AssetDatabase.GetAssetPath(sheet), sheetState, sheetColumns, sheetRowDirections,
                                                 sheetFps, sheetLoop, other != null ? other.pixelsPerUnit : 16f, sheetCustom);
                    }
                }
            }
        }

        void DrawPreview(AnimationSet set)
        {
            var usable = set.clips.Where(c => c != null && !c.IsEmpty).ToList();
            if (usable.Count == 0)
                return;
            previewIndex = Mathf.Clamp(previewIndex, 0, usable.Count - 1);
            using (new EditorGUILayout.HorizontalScope())
            {
                var names = usable.Select(Label).ToArray();
                int picked = EditorGUILayout.Popup("Önizleme", previewIndex, names);
                if (picked != previewIndex)
                {
                    previewIndex = picked;
                    previewStart = EditorApplication.timeSinceStartup;
                }
                previewLeft = GUILayout.Toggle(previewLeft, "Sola", "Button", GUILayout.Width(50));
            }
            var clip = usable[previewIndex];
            float t = (float)(EditorApplication.timeSinceStartup - previewStart);
            if (!clip.loop && t > clip.Duration + 0.6f)
                previewStart = EditorApplication.timeSinceStartup; // replay one-shots
            var r = GUILayoutUtility.GetRect(120, 120, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.17f));
            DrawSprite(r, clip.FrameAt(t), previewLeft && set.mirrorForLeft && (clip.direction == AnimDirection.Any || clip.direction == AnimDirection.Side));
        }

        static string Label(AnimClip c)
        {
            string name = c.state == AnimState.Custom ? $"Özel: {c.customName}" : c.state.Turkish();
            return c.direction == AnimDirection.Any ? name : $"{name} ({c.direction.Turkish()})";
        }

        /// <summary>Returns true if the list changed (stop drawing this frame).</summary>
        bool DrawClip(AnimationSet set, int index)
        {
            var clip = set.clips[index];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var thumb = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28));
                    DrawSprite(thumb, clip.FrameAt((float)EditorApplication.timeSinceStartup), false);
                    EditorGUILayout.LabelField($"{Label(clip)} · {clip.frames.Count} kare · {clip.fps:0.#} fps{(clip.loop ? " · döngü" : "")}", EditorStyles.boldLabel);
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        Undo.RecordObject(set, "Animasyon Sil");
                        set.clips.RemoveAt(index);
                        EditorUtility.SetDirty(set);
                        return true;
                    }
                }

                EditorGUI.BeginChangeCheck();
                var state = (AnimState)EditorGUILayout.EnumPopup("Durum", clip.state);
                string custom = clip.customName;
                if (state == AnimState.Custom)
                    custom = EditorGUILayout.TextField(new GUIContent("Özel Ad", "Yeteneklerde ya da 'Animasyon Oynat' komutunda yazılan ad"), clip.customName);
                var direction = (AnimDirection)EditorGUILayout.EnumPopup(new GUIContent("Yön", "Hepsi = her yöne. Yan, sola bakınca aynalanır."), clip.direction);
                float fps = EditorGUILayout.FloatField(new GUIContent("Hız (kare/sn)"), clip.fps);
                bool loop = EditorGUILayout.Toggle(new GUIContent("Döngü", "Kapalı: bir kez oynar ve son karede kalır"), clip.loop);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(set, "Animasyon");
                    clip.state = state;
                    clip.customName = custom;
                    clip.direction = direction;
                    clip.fps = Mathf.Max(0.1f, fps);
                    clip.loop = loop;
                    EditorUtility.SetDirty(set);
                }

                if (DrawFrames(set, clip))
                    return true;
                DropZone(set, clip);
            }
            return false;
        }

        bool DrawFrames(AnimationSet set, AnimClip clip)
        {
            const float size = 40f;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 50f) / (size + 4f)));
            for (int start = 0; start < clip.frames.Count; start += perRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = start; i < Mathf.Min(start + perRow, clip.frames.Count); i++)
                    {
                        var r = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size));
                        EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.2f));
                        DrawSprite(r, clip.frames[i], false);
                        var x = new Rect(r.xMax - 14, r.y, 14, 14);
                        if (GUI.Button(x, "×", EditorStyles.miniButton))
                        {
                            Undo.RecordObject(set, "Kare Sil");
                            clip.frames.RemoveAt(i);
                            EditorUtility.SetDirty(set);
                            return true;
                        }
                    }
                }
            }
            if (clip.frames.Count > 1)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ters Çevir", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        Undo.RecordObject(set, "Kareleri Ters Çevir");
                        clip.frames.Reverse();
                        EditorUtility.SetDirty(set);
                    }
                    if (GUILayout.Button("Temizle", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        Undo.RecordObject(set, "Kareleri Temizle");
                        clip.frames.Clear();
                        EditorUtility.SetDirty(set);
                        return true;
                    }
                }
            }
            return false;
        }

        void DropZone(AnimationSet set, AnimClip clip)
        {
            var r = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            GUI.Box(r, clip.IsEmpty ? "Kareleri buraya sürükleyin (sprite'lar ya da şerit resmi)" : "Kare eklemek için sürükleyin · yerine koymak için Shift'e basılı tutun", EditorStyles.helpBox);
            var e = Event.current;
            if (!r.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform))
                return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type != EventType.DragPerform)
                return;
            DragAndDrop.AcceptDrag();

            Undo.RecordObject(set, "Kare Ekle");
            if (e.shift)
                clip.frames.Clear();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Sprite sprite)
                    clip.frames.Add(sprite);
                else if (obj is Texture2D tex)
                    clip.frames.AddRange(FramesFrom(set, tex));
            }
            EditorUtility.SetDirty(set);
            e.Use();
        }

        /// <summary>The frames in an image; an uncut image is sliced into a strip first.</summary>
        System.Collections.Generic.List<Sprite> FramesFrom(AnimationSet set, Texture2D tex)
        {
            var path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var frames = AnimationKit.LoadFrames(path);
            bool sliced = importer != null && importer.spriteImportMode == SpriteImportMode.Multiple && frames.Count > 1;
            if (!sliced && importer != null)
            {
                // Same size per pixel as the other frames in this set, so the character doesn't change size.
                var other = set.clips.SelectMany(c => c.frames).FirstOrDefault(s => s != null);
                float ppu = other != null ? other.pixelsPerUnit
                    : importer.textureType == TextureImporterType.Sprite && !Mathf.Approximately(importer.spritePixelsPerUnit, 100f) ? importer.spritePixelsPerUnit : 16f;
                AnimationKit.SliceStrip(path, stripFrames, ppu, new Vector2(0.5f, 0f));
                frames = AnimationKit.LoadFrames(path);
            }
            return frames;
        }

        public static void DrawSprite(Rect area, Sprite sprite, bool flip)
        {
            if (sprite == null || sprite.texture == null)
                return;
            var tex = sprite.texture;
            var tr = sprite.textureRect;
            float scale = Mathf.Min(area.width / tr.width, area.height / tr.height);
            var size = new Vector2(tr.width * scale, tr.height * scale);
            var r = new Rect(area.center - size / 2f, size);
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            if (flip)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }
            GUI.DrawTextureWithTexCoords(r, tex, uv);
        }
    }

    /// <summary>Character Animator inspector: one click to make a placeholder set from the current sprite.</summary>
    [CustomEditor(typeof(CharacterAnimator))]
    public class CharacterAnimatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var animator = (CharacterAnimator)target;
            if (animator.Animations != null)
                return;
            var sr = animator.GetComponentInChildren<SpriteRenderer>(true);
            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(sr == null || sr.sprite == null))
            {
                if (GUILayout.Button("Bu sprite'tan yer tutucu animasyon seti oluştur"))
                {
                    Undo.RecordObject(animator, "Animasyon Seti");
                    animator.Animations = AnimationKit.CreatePlaceholderSet(animator.gameObject.name, sr.sprite, AnimationKit.NpcStates);
                    EditorUtility.SetDirty(animator);
                }
            }
        }
    }
}
