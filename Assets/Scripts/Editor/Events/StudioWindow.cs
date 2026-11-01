using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boruhan.Audio;
using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Events;
using Boruhan.Gameplay;
using Boruhan.Map;
using Boruhan.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shape = Boruhan.EditorTools.PrologueBuilder.Shape;

namespace Boruhan.EditorTools
{
    /// <summary>Börühan > Stüdyo: one place to make and edit everything in the game without code.</summary>
    public class StudioWindow : EditorWindow
    {
        static readonly string[] Tabs = { "Olaylar", "Odalar", "Düşmanlar", "Diyaloglar", "Ortak Olaylar", "Oyuncu", "Ayarlar & Test" };
        const string CommonEventDir = "Assets/Events/OrtakOlaylar";
        const string SettingsPath = "Assets/Resources/OyunAyarlari.asset";

        int tab;
        Vector2 scroll;
        string search = "";
        Editor embedded;
        readonly List<Editor> playerEditors = new List<Editor>();

        [MenuItem("Börühan/Stüdyo", priority = -10)]
        public static void Open() => GetWindow<StudioWindow>("Börühan Stüdyo");

        void OnDisable()
        {
            if (embedded != null)
                DestroyImmediate(embedded);
            foreach (var e in playerEditors)
                DestroyImmediate(e);
            playerEditors.Clear();
        }

        void OnGUI()
        {
            tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(26));
            search = EditorGUILayout.TextField("Ara", search);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case 0: EventsTab(); break;
                case 1: RoomsTab(); break;
                case 2: AssetTab<EnemyDefinition>("Assets/Combat/Dusmanlar", "YeniDusman", d => $"{d.displayName}  [{d.difficulty}{(d.isBoss ? ", Boss" : "")}]",
                    d => { if (GUILayout.Button("Yerleştir", GUILayout.Width(80))) Place(CombatKit.PlaceEnemy(d, ViewCenter(), Parent("Dunya")), "Düşman"); }); break;
                case 3:
                    AssetTab<DialogueAsset>("Assets/Dialogue", "YeniDiyalog", d => $"{d.name}  ({d.lines.Count} satır)", null);
                    EditorGUILayout.Space(10);
                    AssetTab<DialogueSpeaker>("Assets/Dialogue/Konusmacilar", "YeniKonusmaci", s => s.displayName, null);
                    break;
                case 4: AssetTab<CommonEvent>(CommonEventDir, "YeniOrtakOlay", c => c.name, null); break;
                case 5: PlayerTab(); break;
                case 6: SettingsTab(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        // ================= Events =================

        void EventsTab()
        {
            EditorGUILayout.HelpBox("Şablondan olay ekle: Scene görünümünün ortasına konur ve seçilir. Ayrıntılar Inspector'da (sayfalar, koşullar, komutlar).", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("NPC")) Template("NPC");
                if (GUILayout.Button("Tabela")) Template("Tabela");
                if (GUILayout.Button("Sandık")) Template("Sandik");
                if (GUILayout.Button("Kapı / Geçiş")) Template("Kapi");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Tetikleyici Bölge")) Template("Tetik");
                if (GUILayout.Button("Otomatik Olay")) Template("Otomatik");
                if (GUILayout.Button("Tüccar (seçenekli)")) Template("Tuccar");
                if (GUILayout.Button("Boş Olay")) Template("Bos");
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Açık sahnedeki olaylar", EditorStyles.boldLabel);
            foreach (var ev in FindObjectsByType<GameEvent>(FindObjectsInactive.Include).OrderBy(e => e.name))
            {
                if (!Matches(ev.name))
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{ev.name}   ({ev.Pages.Count} sayfa, {ev.Pages.Sum(p => p.commands.Count)} komut)");
                    if (GUILayout.Button("Seç", GUILayout.Width(60)))
                        Select(ev.gameObject);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Diğer yerleştirilebilirler", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Varış Noktası")) Place(PrologueBuilder.Spawn(Parent("Dunya"), "yeni_varis", ViewCenter()), "Varış noktası");
                if (GUILayout.Button("Kut Ocağı (kayıt)")) MakeShrine();
                if (GUILayout.Button("Oda Oluşturucu…")) EditorApplication.ExecuteMenuItem("Börühan/Oda Oluşturucu");
            }
        }

        /// <summary>Creates an event from a template at the centre of the Scene view.</summary>
        public static GameEvent Template(string kind)
        {
            var ev = NewEvent(kind);
            var page = ev.Pages[0];
            switch (kind)
            {
                case "NPC":
                    page.sprite = PrologueBuilder.Art("Olay_NPC", 12, 22, new Color32(110, 150, 190, 255), Shape.Figure);
                    page.animations = AnimationKit.CreatePlaceholderSet("Olay_NPC", page.sprite, AnimationKit.NpcStates);
                    page.prompt = "Konuş"; page.size = new Vector2(0.7f, 0.4f); page.movement = EventMovement.Wander;
                    page.commands.Add(new ShowTextCommand { text = "Merhaba, yolcu.\nBu topraklar tehlikelidir, dikkatli ol." });
                    break;
                case "Tabela":
                    page.sprite = PrologueBuilder.Art("Tabela", 12, 14, new Color32(150, 110, 70, 255), Shape.Box);
                    page.prompt = "Oku"; page.size = new Vector2(0.8f, 0.4f);
                    page.commands.Add(new ShowTextCommand { text = "Kuzey: Kristal Kanyon\nGüney: Giriş" });
                    break;
                case "Sandik":
                    page.note = "Kapalı sandık";
                    page.sprite = PrologueBuilder.Art("Sandik_Kapali", 16, 12, new Color32(140, 95, 45, 255), Shape.Box);
                    page.prompt = "Aç"; page.size = new Vector2(1f, 0.5f);
                    page.commands.Add(new GiveItemCommand { item = PickupKind.KutParcasi });
                    page.commands.Add(new SetFlagCommand { flag = "self:acildi", value = true });
                    ev.Pages.Add(new EventPage
                    {
                        note = "Açılmış sandık",
                        conditions = { new EventCondition { kind = ConditionKind.FlagSet, key = "self:acildi" } },
                        sprite = PrologueBuilder.Art("Sandik_Acik", 16, 12, new Color32(80, 55, 30, 255), Shape.Box),
                        prompt = "Bak", size = new Vector2(1f, 0.5f),
                        commands = { new ShowTextCommand { text = "Sandık boş." } },
                    });
                    break;
                case "Kapi":
                    page.trigger = EventTrigger.Touch; page.solid = false; page.size = new Vector2(1.5f, 1f);
                    page.commands.Add(new TeleportCommand { scene = "", spawnId = "giris" });
                    break;
                case "Tetik":
                    page.trigger = EventTrigger.Touch; page.solid = false; page.size = new Vector2(3f, 2f);
                    page.conditions.Add(new EventCondition { kind = ConditionKind.FlagNotSet, key = "self:bitti" });
                    page.commands.Add(new ShowTextCommand { text = "Rüzgâr bir an durdu..." });
                    page.commands.Add(new SetFlagCommand { flag = "self:bitti", value = true });
                    break;
                case "Otomatik":
                    page.trigger = EventTrigger.Autorun; page.solid = false;
                    page.conditions.Add(new EventCondition { kind = ConditionKind.FlagNotSet, key = "self:bitti" });
                    page.commands.Add(new WaitCommand { seconds = 0.5f });
                    page.commands.Add(new ShowTextCommand { text = "Sahne başladığında bir kez oynar." });
                    page.commands.Add(new SetFlagCommand { flag = "self:bitti", value = true });
                    break;
                case "Tuccar":
                    page.sprite = PrologueBuilder.Art("Olay_Tuccar", 12, 22, new Color32(190, 150, 80, 255), Shape.Figure);
                    page.animations = AnimationKit.CreatePlaceholderSet("Olay_Tuccar", page.sprite, AnimationKit.NpcStates);
                    page.prompt = "Konuş"; page.size = new Vector2(0.7f, 0.4f);
                    page.commands.Add(new ShowChoicesCommand
                    {
                        question = "Bir Şifa Otu ister misin? Karşılığında hiçbir şey istemem... bu sefer.",
                        options =
                        {
                            new ChoiceOption { text = "Evet", commands = { new GiveItemCommand { item = PickupKind.SifaOtu } } },
                            new ChoiceOption { text = "Hayır", commands = { new ShowTextCommand { text = "Nasıl istersen." } } },
                        },
                    });
                    break;
            }
            ev.Refresh();
            EditorUtility.SetDirty(ev);
            return ev;
        }

        static GameEvent NewEvent(string kind)
        {
            var go = new GameObject(kind == "Bos" ? "Olay" : $"Olay_{kind}");
            go.transform.SetParent(Parent("Olaylar"), false);
            go.transform.position = ViewCenter();
            var ev = go.AddComponent<GameEvent>();
            Place(go, "Olay");
            return ev;
        }

        static void MakeShrine()
        {
            var shrine = AssetDatabase.LoadAssetAtPath<CommonEvent>($"{CommonEventDir}/KutOcagi.asset");
            if (shrine == null)
            {
                Directory.CreateDirectory(CommonEventDir);
                shrine = CreateInstance<CommonEvent>();
                shrine.note = "Kut Ocağında dinlen: iyileş ve kaydet.";
                shrine.commands.Add(new ShowTextCommand { text = "Kut Ocağı'nın ateşi seni sardı. Yaraların kapandı." });
                shrine.commands.Add(new HealCommand());
                shrine.commands.Add(new SaveGameCommand());
                AssetDatabase.CreateAsset(shrine, $"{CommonEventDir}/KutOcagi.asset");
            }
            var ev = NewEvent("KutOcagi");
            var page = ev.Pages[0];
            page.sprite = PrologueBuilder.Art("KutOcagi", 16, 18, new Color32(170, 110, 60, 255), Shape.Box);
            page.prompt = "Dinlen"; page.size = new Vector2(1f, 0.6f);
            page.commands.Add(new CallCommonEventCommand { commonEvent = shrine });
            ev.gameObject.AddComponent<SavePoint>();
            ev.Refresh();
            EditorUtility.SetDirty(ev);
        }

        // ================= Rooms =================

        void RoomsTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Yeni Oda…")) EditorApplication.ExecuteMenuItem("Börühan/Oda Oluşturucu");
                if (GUILayout.Button("Tile Palette")) EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            }
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Oyundaki sahneler (Build sırası)", EditorStyles.boldLabel);
            foreach (var s in EditorBuildSettings.scenes)
            {
                var name = Path.GetFileNameWithoutExtension(s.path);
                if (!Matches(name))
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(name);
                    if (GUILayout.Button("Aç", GUILayout.Width(50)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(s.path);
                }
            }
            EditorGUILayout.Space(6);
            AssetTab<RoomTheme>("Assets/Tiles/Temalar", "YeniTema", t => t.name, null);
            EditorGUILayout.Space(6);
            AssetTab<MusicTrack>("Assets/Audio/Parcalar", "YeniMuzik", t => $"{t.name}  ({(t.clip != null ? t.clip.name : "klip yok")})", null);
        }

        // ================= Player =================

        void PlayerTab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boruhan.prefab");
            if (prefab == null)
            {
                EditorGUILayout.HelpBox("Assets/Prefabs/Boruhan.prefab bulunamadı.", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox("Börühan'ın ayarları (prefab). Değişiklikler tüm sahnelerde geçerlidir.", MessageType.Info);
            if (playerEditors.Count == 0)
            {
                foreach (var c in new Component[] { prefab.GetComponent<PlayerController>(), prefab.GetComponent<PlayerCombat>(), prefab.GetComponent<Health>() })
                {
                    if (c != null)
                        playerEditors.Add(Editor.CreateEditor(c));
                }
            }
            foreach (var e in playerEditors)
            {
                EditorGUILayout.LabelField(e.target.GetType().Name, EditorStyles.boldLabel);
                e.OnInspectorGUI();
                EditorGUILayout.Space(8);
            }
        }

        // ================= Settings & test =================

        void SettingsTab()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory("Assets/Resources");
                settings = CreateInstance<GameSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            EditorGUILayout.LabelField("Oyun Ayarları", EditorStyles.boldLabel);
            Editor.CreateCachedEditor(settings, null, ref embedded);
            embedded.OnInspectorGUI();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Açık sahneyi doğrudan oynatır. Aşağıdaki bayraklar test için açık başlar (virgülle ayırın). Test modunda kayıt yapılmaz.", MessageType.None);
            var flags = EditorPrefs.GetString(GameSession.TestFlagsKey, "");
            var newFlags = EditorGUILayout.TextField("Test Bayrakları", flags);
            if (newFlags != flags)
                EditorPrefs.SetString(GameSession.TestFlagsKey, newFlags);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Kılıçla başla")) AddTestFlag(StoryFlags.Sword);
                if (GUILayout.Button("Kut Kıvılcımı")) AddTestFlag(StoryFlags.KutSpark);
                if (GUILayout.Button("Tüm güçler"))
                {
                    foreach (Power p in Enum.GetValues(typeof(Power)))
                        AddTestFlag(GrantPowerCommand.FlagOf(p));
                }
                if (GUILayout.Button("Temizle")) EditorPrefs.SetString(GameSession.TestFlagsKey, "");
            }
            if (GUILayout.Button("▶  Açık Sahneyi Test Et", GUILayout.Height(30)))
                EditorApplication.EnterPlaymode();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Kullanılan bayraklar (açık sahnedeki olaylardan)", EditorStyles.boldLabel);
            var used = new SortedSet<string>();
            foreach (var ev in FindObjectsByType<GameEvent>(FindObjectsInactive.Include))
            {
                foreach (var page in ev.Pages)
                {
                    foreach (var c in page.conditions)
                        used.Add(c.key);
                    CollectFlags(page.commands, used);
                }
            }
            used.Remove(null);
            used.Remove("");
            EditorGUILayout.LabelField(used.Count == 0 ? "(yok)" : string.Join(", ", used), EditorStyles.wordWrappedLabel);
        }

        static void CollectFlags(List<EventCommand> commands, SortedSet<string> into)
        {
            foreach (var c in commands)
            {
                switch (c)
                {
                    case SetFlagCommand f: into.Add(f.flag); break;
                    case VariableCommand v: into.Add(v.variable); break;
                    case IfCommand i:
                        foreach (var cond in i.conditions) into.Add(cond.key);
                        CollectFlags(i.then, into);
                        CollectFlags(i.otherwise, into);
                        break;
                    case ShowChoicesCommand s:
                        foreach (var o in s.options) CollectFlags(o.commands, into);
                        break;
                }
            }
        }

        static void AddTestFlag(string flag)
        {
            var list = EditorPrefs.GetString(GameSession.TestFlagsKey, "").Split(',').Select(f => f.Trim()).Where(f => f.Length > 0).ToList();
            if (!list.Contains(flag))
                list.Add(flag);
            EditorPrefs.SetString(GameSession.TestFlagsKey, string.Join(", ", list));
        }

        // ================= Helpers =================

        void AssetTab<T>(string folder, string newName, Func<T, string> label, Action<T> extra) where T : ScriptableObject
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(typeof(T).Name, EditorStyles.boldLabel);
                if (GUILayout.Button("+ Yeni", GUILayout.Width(70)))
                {
                    Directory.CreateDirectory(folder);
                    var asset = CreateInstance<T>();
                    AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{newName}.asset"));
                    Select(asset);
                }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || !AssetDatabase.IsMainAsset(asset))
                    continue;
                var text = label(asset);
                if (!Matches(text))
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(text);
                    extra?.Invoke(asset);
                    if (GUILayout.Button("Düzenle", GUILayout.Width(70)))
                        Select(asset);
                }
            }
        }

        bool Matches(string text) => string.IsNullOrEmpty(search) || (text ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        static Vector2 ViewCenter()
        {
            var view = SceneView.lastActiveSceneView;
            Vector2 p = view != null ? (Vector2)view.pivot : Vector2.zero;
            return new Vector2(Mathf.Round(p.x * 2f) / 2f, Mathf.Round(p.y * 2f) / 2f);
        }

        static Transform Parent(string name)
        {
            var scene = SceneManager.GetActiveScene();
            var root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == name);
            if (root == null)
            {
                root = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(root, "Klasör");
            }
            return root.transform;
        }

        static void Place(GameObject go, string what)
        {
            Undo.RegisterCreatedObjectUndo(go, $"{what} ekle");
            Select(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        static void Select(UnityEngine.Object o)
        {
            Selection.activeObject = o;
            EditorGUIUtility.PingObject(o);
        }
    }
}
