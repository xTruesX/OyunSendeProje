using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boruhan.Animations;
using Boruhan.Combat;
using Boruhan.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Animation tools: slicing a sprite strip into frames, and placeholder animations made from a
    /// character's own sprite (bobbing walk, lunge, flinch, collapse…) so everything moves until real
    /// frames are drawn. Strips are only created when missing: replace a PNG and the set uses it.
    /// </summary>
    public static class AnimationKit
    {
        public const string AnimDir = "Assets/Art/Animasyonlar";
        const string PlayerPrefab = "Assets/Prefabs/Boruhan.prefab";
        const string EnemyDir = "Assets/Combat/Dusmanlar";

        public static readonly AnimState[] PlayerStates =
            { AnimState.Idle, AnimState.Walk, AnimState.Attack, AnimState.HeavyAttack, AnimState.Dodge, AnimState.Parry, AnimState.Hurt, AnimState.Death };
        public static readonly AnimState[] EnemyStates =
            { AnimState.Idle, AnimState.Walk, AnimState.Attack, AnimState.Parry, AnimState.Dodge, AnimState.Hurt, AnimState.Stagger, AnimState.Death };
        public static readonly AnimState[] NpcStates = { AnimState.Idle, AnimState.Walk };

        // NPC sprites (by file name) that get an idle/walk set wherever they appear.
        static readonly string[] NpcSprites = { "KayraHan", "Ruh", "UlgenHan_Zincirli", "Olay_NPC", "Olay_Tuccar", "YeniNPC" };

        public static string FileName(AnimState state) => state switch
        {
            AnimState.Idle => "Bekle",
            AnimState.Walk => "Yuru",
            AnimState.Attack => "Saldiri",
            AnimState.HeavyAttack => "AgirSaldiri",
            AnimState.Dodge => "Kacinma",
            AnimState.Parry => "Savusturma",
            AnimState.Hurt => "Hasar",
            AnimState.Stagger => "Sersemleme",
            AnimState.Death => "Olum",
            AnimState.Turn => "Donus",
            _ => "Ozel",
        };

        // ================= Setup for the whole game =================

        [MenuItem("Börühan/Animasyonları Kur")]
        public static void SetupAll()
        {
            int sets = 0;

            // Börühan
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            var body = root.transform.Find("Govde");
            if (body != null)
            {
                var sr = body.GetComponent<SpriteRenderer>();
                var animator = body.GetComponent<CharacterAnimator>();
                if (animator == null)
                    animator = body.gameObject.AddComponent<CharacterAnimator>();
                if (animator.Animations == null && sr.sprite != null)
                {
                    animator.Animations = CreatePlaceholderSet("Boruhan", sr.sprite, PlayerStates);
                    sets++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            PrefabUtility.UnloadPrefabContents(root);

            // Enemies
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyDir }))
            {
                var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null || def.animations != null || def.sprite == null)
                    continue;
                CreateForEnemy(def);
                sets++;
            }

            // NPCs, in every scene
            var npcSets = new Dictionary<Sprite, AnimationSet>();
            foreach (var name in NpcSprites)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PrologueBuilder.ArtDir}/{name}.png");
                if (sprite != null)
                    npcSets[sprite] = CreatePlaceholderSet(name, sprite, NpcStates);
            }
            int npcs = AttachToNpcsInScenes(npcSets, out var skipped);

            AssetDatabase.SaveAssets();
            string msg = $"[Animasyon] {sets} yeni animasyon seti, sahnelerde {npcs} NPC'ye animasyon eklendi. Kareler: {AnimDir}";
            if (skipped.Count > 0)
                msg += $"\nKaydedilmemiş değişiklikleri olduğu için kaydedilmedi (siz kaydedin): {string.Join(", ", skipped)}";
            Debug.Log(msg);
        }

        // Börühan's looks: the plain one, then variants that take over once a story flag is set.
        // Later entries win, so with both the Tılsım and the sword he shows the sword look.
        static readonly (string folder, string flag)[] BoruhanLooks =
        {
            ("Boruhan", null),
            ("Boruhan_Tilsim", Core.StoryFlags.TalismanTaken),
            ("Boruhan_Kilic", Core.StoryFlags.Sword),
        };

        /// <summary>
        /// Imports every sheet in Börühan's animation folders (see SheetLayout for the file names) and
        /// hooks the Tılsım and sword folders up as variants. Re-run after adding or redrawing sheets.
        /// </summary>
        [MenuItem("Börühan/Bakım/Börühan Animasyonlarını İçe Aktar")]
        public static void ImportBoruhanSheets()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            var body = root.transform.Find("Govde");
            var animator = body != null ? body.GetComponent<CharacterAnimator>() : null;
            if (animator == null || animator.Animations == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("[Animasyon] Önce Börühan → Animasyonları Kur çalıştırılmalı.");
                return;
            }

            const float ppu = 32f; // same as Boruhan.png
            var report = new List<string>();
            animator.Variants.Clear();
            foreach (var (folder, flag) in BoruhanLooks)
            {
                var dir = $"{AnimDir}/{folder}";
                Directory.CreateDirectory(dir);
                var setPath = $"{dir}/{folder}_Animasyon.asset";
                var set = AssetDatabase.LoadAssetAtPath<AnimationSet>(setPath);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<AnimationSet>();
                    AssetDatabase.CreateAsset(set, setPath);
                }
                // The old one-direction placeholders made from the front sprite.
                foreach (var old in new[] { "Saldiri", "AgirSaldiri", "Kacinma", "Savusturma", "Hasar", "Olum" })
                    AssetDatabase.DeleteAsset($"{dir}/{old}.png");

                int count = ImportFolder(set, dir, ppu);
                int made = GenerateMissingSheets(set, dir);
                if (made > 0)
                    count = ImportFolder(set, dir, ppu);
                if (flag != null)
                    animator.Variants.Add(new AnimationVariant { flag = flag, animations = set });
                report.Add($"{folder}: {count} sayfa ({made} yer tutucu oluşturuldu)");
            }
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Animasyon] Börühan içe aktarıldı — " + string.Join(", ", report));
        }

        /// <summary>
        /// Imports each sheet in a folder whose name tells its layout (others, like generated
        /// placeholders, are left alone). Returns how many sheets were imported.
        /// </summary>
        public static int ImportFolder(AnimationSet set, string folder, float pixelsPerUnit)
        {
            int count = 0;
            foreach (var path in Directory.GetFiles(folder, "*.png").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
            {
                var layout = SheetLayout.FromFileName(Path.GetFileNameWithoutExtension(path));
                if (layout == null)
                    continue;
                AssetDatabase.ImportAsset(path);
                ImportSheet(set, path, layout.state, layout.columns, layout.rows, layout.fps, layout.loop, pixelsPerUnit);
                count++;
            }
            set.clips.RemoveAll(c => c.IsEmpty || c.frames.Any(f => f == null)); // clips whose image was deleted
            EditorUtility.SetDirty(set);
            return count;
        }

        public static AnimationSet CreateForEnemy(EnemyDefinition def)
        {
            var set = CreatePlaceholderSet(def.name, def.sprite, EnemyStates);
            def.animations = set;
            // Moves that aren't swings look better with their own animation.
            foreach (var ability in def.abilities)
            {
                if (ability == null || ability.animationState != AnimState.Attack)
                    continue;
                if (ability is GuardAbility || ability is ReflectAbility)
                    ability.animationState = AnimState.Parry;
                else if (ability is TeleportAbility)
                    ability.animationState = AnimState.Dodge;
                else
                    continue;
                EditorUtility.SetDirty(ability);
            }
            EditorUtility.SetDirty(def);
            return set;
        }

        static int AttachToNpcsInScenes(Dictionary<Sprite, AnimationSet> npcSets, out List<string> skipped)
        {
            skipped = new List<string>();
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                bool wasDirty = wasOpen && scene.isDirty;
                if (!wasOpen)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                int changed = 0;
                foreach (var go in scene.GetRootGameObjects())
                {
                    foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (sr.sprite == null || !npcSets.TryGetValue(sr.sprite, out var set))
                            continue;
                        if (sr.GetComponentInParent<GameEvent>() != null)
                            continue; // events get theirs per page below
                        var animator = sr.GetComponent<CharacterAnimator>();
                        if (animator != null)
                            continue;
                        animator = sr.gameObject.AddComponent<CharacterAnimator>();
                        animator.Animations = set;
                        changed++;
                    }
                    foreach (var ev in go.GetComponentsInChildren<GameEvent>(true))
                    {
                        bool touched = false;
                        foreach (var page in ev.Pages)
                        {
                            if (page.sprite != null && page.animations == null && npcSets.TryGetValue(page.sprite, out var set))
                            {
                                page.animations = set;
                                touched = true;
                            }
                        }
                        if (touched)
                        {
                            EditorUtility.SetDirty(ev);
                            changed++;
                        }
                    }
                }

                count += changed;
                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (wasDirty)
                        skipped.Add(scene.name);
                    else
                        EditorSceneManager.SaveScene(scene);
                }
                if (!wasOpen)
                    EditorSceneManager.CloseScene(scene, true);
            }
            return count;
        }

        // ================= Placeholder sets =================

        /// <summary>
        /// Makes (or loads) Assets/Art/Animasyonlar/&lt;name&gt;/&lt;name&gt;_Animasyon.asset with one
        /// placeholder strip per state, generated from the sprite. Existing strips and sets are kept.
        /// </summary>
        public static AnimationSet CreatePlaceholderSet(string name, Sprite source, IEnumerable<AnimState> states)
        {
            var folder = $"{AnimDir}/{name}";
            Directory.CreateDirectory(folder);
            var setPath = $"{folder}/{name}_Animasyon.asset";
            var set = AssetDatabase.LoadAssetAtPath<AnimationSet>(setPath);
            if (set != null)
                return set;

            set = ScriptableObject.CreateInstance<AnimationSet>();
            foreach (var state in states)
            {
                var recipe = Recipe(state);
                var stripPath = $"{folder}/{FileName(state)}.png";
                if (!File.Exists(stripPath))
                    WriteStrip(source, recipe.frames, stripPath);
                set.clips.Add(new AnimClip
                {
                    state = state,
                    frames = LoadFrames(stripPath),
                    fps = recipe.fps,
                    loop = recipe.loop,
                });
            }
            AssetDatabase.CreateAsset(set, setPath);
            return set;
        }

        /// <summary>One frame of a placeholder: the sprite squashed, leaned, moved and tinted.</summary>
        struct Pose
        {
            public float scaleX, scaleY, lean, dx, dy, tintAmount, alpha;
            public Color tint;

            public static Pose Of(float scaleX = 1f, float scaleY = 1f, float lean = 0f, float dx = 0f, float dy = 0f,
                                  Color? tint = null, float tintAmount = 0f, float alpha = 1f) =>
                new Pose { scaleX = scaleX, scaleY = scaleY, lean = lean, dx = dx, dy = dy, tint = tint ?? Color.white, tintAmount = tintAmount, alpha = alpha };
        }

        // lean, dx, dy are fractions of the sprite's height.
        static (Pose[] frames, float fps, bool loop) Recipe(AnimState state)
        {
            var white = Color.white;
            var red = new Color(1f, 0.3f, 0.3f);
            var blue = new Color(0.6f, 0.9f, 1f);
            switch (state)
            {
                case AnimState.Walk:
                    return (new[] { Pose.Of(lean: 0.06f), Pose.Of(dy: 0.05f), Pose.Of(lean: -0.06f), Pose.Of(dy: 0.05f) }, 8f, true);
                case AnimState.Attack:
                    return (new[] { Pose.Of(lean: -0.12f, scaleX: 0.95f), Pose.Of(lean: 0.18f, scaleX: 1.12f, dx: 0.06f), Pose.Of(lean: 0.08f, dx: 0.03f) }, 14f, false);
                case AnimState.HeavyAttack:
                    return (new[]
                    {
                        Pose.Of(scaleX: 1.1f, scaleY: 0.88f, lean: -0.1f),
                        Pose.Of(scaleX: 1.15f, scaleY: 0.8f, lean: -0.14f, tint: white, tintAmount: 0.3f),
                        Pose.Of(scaleX: 0.92f, scaleY: 1.12f, lean: 0.22f, dx: 0.08f),
                        Pose.Of(lean: 0.1f, dx: 0.04f),
                    }, 6f, false);
                case AnimState.Dodge:
                    return (new[] { Pose.Of(scaleX: 1.25f, scaleY: 0.7f), Pose.Of(scaleX: 1.1f, scaleY: 0.85f), Pose.Of() }, 12f, false);
                case AnimState.Parry:
                    return (new[] { Pose.Of(lean: -0.1f, tint: blue, tintAmount: 0.4f), Pose.Of(lean: -0.06f, tint: blue, tintAmount: 0.2f) }, 10f, false);
                case AnimState.Hurt:
                    return (new[] { Pose.Of(dx: -0.05f, lean: -0.08f, tint: white, tintAmount: 0.6f), Pose.Of(dx: 0.03f, tint: red, tintAmount: 0.3f) }, 12f, false);
                case AnimState.Stagger:
                    return (new[] { Pose.Of(lean: 0.08f), Pose.Of(), Pose.Of(lean: -0.08f), Pose.Of() }, 7f, true);
                case AnimState.Death:
                    return (new[]
                    {
                        Pose.Of(lean: -0.1f, tint: white, tintAmount: 0.5f),
                        Pose.Of(scaleX: 1.1f, scaleY: 0.75f, tint: Color.black, tintAmount: 0.2f),
                        Pose.Of(scaleX: 1.2f, scaleY: 0.5f, tint: Color.black, tintAmount: 0.35f, alpha: 0.85f),
                        Pose.Of(scaleX: 1.3f, scaleY: 0.28f, tint: Color.black, tintAmount: 0.5f, alpha: 0.65f),
                        Pose.Of(scaleX: 1.35f, scaleY: 0.12f, tint: Color.black, tintAmount: 0.6f, alpha: 0.45f),
                    }, 10f, false);
                default: // Idle: a slow breath
                    return (new[] { Pose.Of(), Pose.Of(scaleX: 1.03f, scaleY: 0.96f) }, 2f, true);
            }
        }

        static void WriteStrip(Sprite source, Pose[] poses, string path)
        {
            var src = ReadPixels(source, out int w, out int h);
            int pad = Mathf.Max(2, Mathf.CeilToInt(Mathf.Max(w, h) * 0.3f));
            int fw = w + pad * 2, fh = h + pad;
            var strip = new Color32[fw * poses.Length * fh];
            float pivotX = source.pivot.x;

            for (int f = 0; f < poses.Length; f++)
            {
                for (int y = 0; y < fh; y++)
                for (int x = 0; x < fw; x++)
                {
                    if (Sample(src, w, h, pivotX, poses[f], 1f, x - pad - pivotX, y, out var c))
                        strip[y * fw * poses.Length + f * fw + x] = c;
                }
            }

            var tex = new Texture2D(fw * poses.Length, fh, TextureFormat.RGBA32, false);
            tex.SetPixels32(strip);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var pivot = new Vector2((pad + pivotX) / fw, source.pivot.y / fh);
            SliceStrip(path, poses.Length, source.pixelsPerUnit, pivot);
        }

        /// <summary>
        /// The colour a posed sprite has at (x, y), measured from its pivot column and its feet. Works
        /// backwards to the source pixel; scaling and leaning keep the feet in place. 'forward' is +1 when
        /// leaning means right, -1 for left, 0 for no leaning (facing the camera or away).
        /// </summary>
        static bool Sample(Color32[] src, int w, int h, float pivotX, Pose p, float forward, float x, float y, out Color32 result)
        {
            result = default;
            float u = y - p.dy * h;
            float sy = u / p.scaleY;
            float lean = p.lean * forward * h * (u / h);
            float sx = (x - p.dx * forward * h - lean) / p.scaleX + pivotX;
            int ix = Mathf.FloorToInt(sx), iy = Mathf.FloorToInt(sy);
            if (ix < 0 || iy < 0 || ix >= w || iy >= h)
                return false;
            Color c = src[iy * w + ix];
            if (c.a <= 0f)
                return false;
            var tinted = Color.Lerp(c, p.tint, p.tintAmount);
            tinted.a = c.a * p.alpha;
            result = tinted;
            return true;
        }

        // ================= Directional placeholders =================

        static readonly AnimState[] CombatStates =
            { AnimState.Attack, AnimState.HeavyAttack, AnimState.Dodge, AnimState.Parry, AnimState.Hurt, AnimState.Death };

        /// <summary>
        /// For each combat animation the folder has no sheet for, draws a 4-direction placeholder sheet
        /// from the set's own Bekle frames (so a look with the sword attacks holding the sword), named so
        /// ImportFolder picks it up. Replace a generated sheet with a drawn one of any layout.
        /// Returns how many were made.
        /// </summary>
        public static int GenerateMissingSheets(AnimationSet set, string folder)
        {
            var have = new HashSet<AnimState>(Directory.GetFiles(folder, "*.png")
                .Select(f => SheetLayout.FromFileName(Path.GetFileNameWithoutExtension(f)))
                .Where(l => l != null).Select(l => l.state));
            var rows = SheetLayout.FourWay;
            var sources = rows.Select(d => set.Find(AnimState.Idle, null, d)).Select(c => c != null && !c.IsEmpty ? c.frames[0] : null).ToArray();
            if (sources.Any(s => s == null))
                return 0;

            int made = 0;
            foreach (var state in CombatStates)
            {
                if (have.Contains(state))
                    continue;
                var (poses, fps, _) = Recipe(state);
                int ms = Mathf.RoundToInt(1000f / fps);
                var path = $"{folder}/{FileName(state)}_4yon_{poses.Length}kare_{ms}ms.png";
                WriteDirectionalSheet(sources, rows, poses, path);
                made++;
            }
            return made;
        }

        static void WriteDirectionalSheet(Sprite[] sources, AnimDirection[] rows, Pose[] poses, string path)
        {
            var first = ReadPixels(sources[0], out int cw, out int ch);
            int width = cw * poses.Length, height = ch * rows.Length;
            var sheet = new Color32[width * height];
            for (int r = 0; r < rows.Length; r++)
            {
                var src = r == 0 ? first : ReadPixels(sources[r], out _, out _);
                float pivotX = sources[r].pivot.x;
                float forward = rows[r] == AnimDirection.Right ? 1f : rows[r] == AnimDirection.Left ? -1f : 0f;
                int top = height - (r + 1) * ch; // row 0 is the top row
                for (int f = 0; f < poses.Length; f++)
                {
                    // Frames stay the size of the drawn ones, so lunges are kept small enough to fit.
                    var p = poses[f];
                    p.lean *= 0.5f;
                    p.dx *= 0.5f;
                    for (int y = 0; y < ch; y++)
                    for (int x = 0; x < cw; x++)
                    {
                        if (Sample(src, cw, ch, pivotX, p, forward, x - pivotX, y, out var c))
                            sheet[(top + y) * width + f * cw + x] = c;
                    }
                }
            }
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(sheet);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        /// <summary>The sprite's pixels (works even if its texture isn't marked readable).</summary>
        static Color32[] ReadPixels(Sprite sprite, out int w, out int h)
        {
            var path = AssetDatabase.GetAssetPath(sprite.texture);
            var full = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            full.LoadImage(File.ReadAllBytes(path));
            var r = sprite.rect;
            w = Mathf.RoundToInt(r.width);
            h = Mathf.RoundToInt(r.height);
            var px = full.GetPixels32();
            var result = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                result[y * w + x] = px[(y + (int)r.y) * full.width + x + (int)r.x];
            Object.DestroyImmediate(full);
            return result;
        }

        // ================= Slicing =================

        /// <summary>
        /// Cuts a horizontal strip into equal frames (left to right) as point-filtered sprites.
        /// frameCount 0 = square frames (width / height).
        /// </summary>
        public static void SliceStrip(string path, int frameCount, float pixelsPerUnit, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            int width, height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);
            if (frameCount <= 0)
                frameCount = Mathf.Max(1, Mathf.RoundToInt((float)width / height));
            int fw = width / frameCount;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var name = Path.GetFileNameWithoutExtension(path);
            var rects = new SpriteRect[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"{name}_{i}",
                    rect = new Rect(i * fw, 0, fw, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = GUID.Generate(),
                };
            }
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>Cuts a sheet into a grid of equal frames, named &lt;file&gt;_&lt;row&gt;_&lt;column&gt; (row 0 = top).</summary>
        public static void SliceGrid(string path, int columns, int rows, float pixelsPerUnit, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int cw = width / columns, ch = height / rows;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var name = Path.GetFileNameWithoutExtension(path);
            var rects = new List<SpriteRect>();
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                rects.Add(new SpriteRect
                {
                    name = $"{name}_{r}_{c}",
                    rect = new Rect(c * cw, height - (r + 1) * ch, cw, ch),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = GUID.Generate(),
                });
            }
            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Imports a sprite sheet with one row per facing (e.g. Aşağı, Yukarı, Sol, Sağ) as clips of one
        /// state, replacing that state's old clips. One row with direction Hepsi works for plain strips.
        /// </summary>
        public static void ImportSheet(AnimationSet set, string path, AnimState state, int columns, AnimDirection[] rowDirections,
                                       float fps, bool loop, float pixelsPerUnit, string customName = "")
        {
            SliceGrid(path, columns, rowDirections.Length, pixelsPerUnit, new Vector2(0.5f, 0f));
            var frames = LoadFrames(path);
            Undo.RecordObject(set, "Sprite Sayfası");
            set.clips.RemoveAll(c => c.state == state && (state != AnimState.Custom || c.customName == customName));
            for (int r = 0; r < rowDirections.Length; r++)
            {
                set.clips.Add(new AnimClip
                {
                    state = state,
                    customName = customName,
                    direction = rowDirections[r],
                    frames = frames.Skip(r * columns).Take(columns).ToList(),
                    fps = fps,
                    loop = loop,
                });
            }
            EditorUtility.SetDirty(set);
        }

        /// <summary>All sprites in an image, left to right then top to bottom.</summary>
        public static List<Sprite> LoadFrames(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderByDescending(s => Mathf.RoundToInt(s.rect.y))
                .ThenBy(s => s.rect.x)
                .ToList();
    }
}
