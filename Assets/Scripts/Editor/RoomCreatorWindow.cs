using System.IO;
using System.Linq;
using Boruhan.Audio;
using Boruhan.Combat;
using Boruhan.Dialogue;
using Boruhan.Map;
using Boruhan.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shape = Boruhan.EditorTools.PrologueBuilder.Shape;

namespace Boruhan.EditorTools
{
    /// <summary>Börühan > Oda Oluşturucu: make new rooms and drop doors, triggers, NPCs and music into the open one.</summary>
    public class RoomCreatorWindow : EditorWindow
    {
        string sceneName = "YeniOda";
        string locationName = "Yeni Oda";
        int floor;
        Vector2Int size = new Vector2Int(24, 16);
        RoomTheme theme;

        DialogueAsset dialogue;
        string prompt = "Bak";
        string onceFlag = "";
        int doorTargetIndex;
        string doorSpawn = "giris";
        string spawnId = "yeni_varis";
        MusicTrack music;
        EnemyDefinition enemy;

        Vector2 scroll;

        [MenuItem("Börühan/Oda Oluşturucu", priority = 0)]
        static void Open() => GetWindow<RoomCreatorWindow>("Oda Oluşturucu");

        void OnEnable()
        {
            MapKit.EnsureAssets();
            if (theme == null)
                theme = MapKit.GetTheme("Bozkir");
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            Header("1. Yeni Oda");
            sceneName = EditorGUILayout.TextField(new GUIContent("Sahne Dosyası", "Türkçe karakter ve boşluk kullanmayın, örn. Kat1Col"), sceneName);
            locationName = EditorGUILayout.TextField(new GUIContent("Yer Adı", "Kayıt ekranında görünen ad"), locationName);
            floor = EditorGUILayout.IntField(new GUIContent("Kat", "0 = yeryüzü"), floor);
            size = EditorGUILayout.Vector2IntField("Boyut (karo)", size);
            theme = (RoomTheme)EditorGUILayout.ObjectField(new GUIContent("Tema", "Zemin, kenar, ışık ve müzik. Assets/Tiles/Temalar"), theme, typeof(RoomTheme), false);
            using (new EditorGUI.DisabledScope(theme == null || string.IsNullOrWhiteSpace(sceneName)))
            {
                if (GUILayout.Button("Odayı Oluştur ve Aç", GUILayout.Height(28)))
                    CreateRoom();
            }

            Header("2. Açık Sahneye Ekle");
            EditorGUILayout.HelpBox("Nesneler Scene görünümünün ortasına eklenir. Eklediğiniz nesne seçilir; sürükleyip yerleştirin.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                dialogue = (DialogueAsset)EditorGUILayout.ObjectField("Diyalog", dialogue, typeof(DialogueAsset), false);
                if (GUILayout.Button("Yeni", GUILayout.Width(50)))
                    dialogue = NewDialogue();
            }
            prompt = EditorGUILayout.TextField(new GUIContent("Etkileşim Yazısı", "[E] ... olarak görünür"), prompt);
            onceFlag = EditorGUILayout.TextField(new GUIContent("Tek Seferlik Bayrak", "Tetikleyici bu bayrak varsa bir daha çalışmaz. Boş = her seferinde"), onceFlag);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Hikaye Tetikleyici"))
                    AddTrigger();
                if (GUILayout.Button("Etkileşim Nesnesi"))
                    AddInteractable(false);
                if (GUILayout.Button("NPC"))
                    AddInteractable(true);
            }

            EditorGUILayout.Space();
            var scenes = EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            doorTargetIndex = EditorGUILayout.Popup("Kapı Hedefi", Mathf.Clamp(doorTargetIndex, 0, Mathf.Max(0, scenes.Length - 1)), scenes);
            doorSpawn = EditorGUILayout.TextField(new GUIContent("Hedefteki Varış Noktası", "Hedef sahnedeki SpawnPoint id'si"), doorSpawn);
            spawnId = EditorGUILayout.TextField(new GUIContent("Yeni Varış Noktası id", "Başka sahnelerin kapıları bu id ile buraya gelir"), spawnId);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(scenes.Length == 0))
                {
                    if (GUILayout.Button("Kapı"))
                        AddDoor(scenes[doorTargetIndex]);
                }
                if (GUILayout.Button("Varış Noktası"))
                    AddSpawn();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                music = (MusicTrack)EditorGUILayout.ObjectField("Müzik", music, typeof(MusicTrack), false);
                if (GUILayout.Button("Sahne Müziği Ayarla", GUILayout.Width(140)))
                    SetMusic();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                enemy = (EnemyDefinition)EditorGUILayout.ObjectField(new GUIContent("Düşman", "Assets/Combat/Dusmanlar"), enemy, typeof(EnemyDefinition), false);
                using (new EditorGUI.DisabledScope(enemy == null))
                {
                    if (GUILayout.Button("Düşman Ekle", GUILayout.Width(140)))
                        Created(CombatKit.PlaceEnemy(enemy, PlacePoint(), Parent()), "Düşman");
                }
            }

            Header("3. Haritayı Boya");
            EditorGUILayout.HelpBox(
                "Tile Palette penceresinde bir palet seçin (Bozkir, Magara, Gok, IcMekan) ve Harita altındaki katmana boyayın:\n" +
                "• Zemin – yer döşemesi\n• Dekor – yol, çimen, detaylar (çarpışma yok)\n• Engeller – dağ, kaya, duvar (geçilmez, oyuncuyla sıralanır)\n\n" +
                "Görselleri değiştirmek için Assets/Art/Tiles içindeki PNG'lerin üzerine yazın.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Tile Palette'i Aç"))
                    EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
                if (GUILayout.Button(new GUIContent("Çarpışmaları Yenile", "Boyadığınız engeller oyunda geçilebiliyorsa buna basın")))
                    RebuildCollision();
            }

            EditorGUILayout.EndScrollView();
        }

        static void Header(string text)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        // ---------- Rooms ----------

        void CreateRoom()
        {
            var path = $"{PrologueBuilder.SceneDir}/{sceneName}.unity";
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Oda Oluşturucu", $"{path} zaten var. Başka bir ad seçin.", "Tamam");
                return;
            }
            if (size.x < 4 || size.y < 4)
            {
                EditorUtility.DisplayDialog("Oda Oluşturucu", "Oda en az 4x4 karo olmalı.", "Tamam");
                return;
            }

            MapKit.CreateRoomScene(sceneName, locationName, floor, size, theme);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }

        // ---------- Objects in the open scene ----------

        static Transform Parent()
        {
            var world = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(r => r.name == "Dunya");
            return world != null ? world.transform : null;
        }

        static Vector2 PlacePoint()
        {
            var view = SceneView.lastActiveSceneView;
            Vector2 p = view != null ? (Vector2)view.pivot : Vector2.zero;
            return new Vector2(Mathf.Round(p.x * 2f) / 2f, Mathf.Round(p.y * 2f) / 2f);
        }

        static void Created(GameObject go, string what)
        {
            Undo.RegisterCreatedObjectUndo(go, $"{what} ekle");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        void AddTrigger()
        {
            var trigger = PrologueBuilder.Trigger(Parent(), "HikayeTetikleyici", PlacePoint(), new Vector2(3f, 2f), dialogue, "", onceFlag);
            Created(trigger.gameObject, "Hikaye tetikleyici");
        }

        void AddInteractable(bool npc)
        {
            var sprite = npc
                ? PrologueBuilder.Art("YeniNPC", 12, 22, new Color32(90, 130, 170, 255), Shape.Figure)
                : PrologueBuilder.Art("YeniNesne", 16, 16, new Color32(150, 120, 80, 255), Shape.Box);
            var go = PrologueBuilder.Prop(Parent(), npc ? "NPC" : "Nesne", sprite, PlacePoint(), 0.5f);
            PrologueBuilder.Interact(go, npc && prompt == "Bak" ? "Konuş" : prompt, dialogue);
            Created(go, npc ? "NPC" : "Etkileşim nesnesi");
        }

        void AddDoor(string targetScene)
        {
            var go = PrologueBuilder.Door(Parent(), $"Kapi_{targetScene}", PlacePoint(), new Vector2(1.5f, 1f), targetScene, doorSpawn);
            Created(go, "Kapı");
        }

        void AddSpawn()
        {
            var go = PrologueBuilder.Spawn(Parent(), spawnId, PlacePoint());
            Created(go, "Varış noktası");
        }

        void SetMusic()
        {
            var existing = Object.FindAnyObjectByType<SceneMusic>();
            if (existing == null)
            {
                var go = new GameObject("SahneMuzigi");
                Undo.RegisterCreatedObjectUndo(go, "Sahne müziği ekle");
                existing = go.AddComponent<SceneMusic>();
            }
            Undo.RecordObject(existing, "Sahne müziği");
            PrologueBuilder.Set(existing, "track", music);
            Selection.activeGameObject = existing.gameObject;
        }

        static void RebuildCollision()
        {
            foreach (var tilemap in FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsInactive.Exclude))
            {
                if (tilemap.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() == null)
                    continue;
                MapKit.RebuildCollision(tilemap);
                EditorSceneManager.MarkSceneDirty(tilemap.gameObject.scene);
            }
        }

        static DialogueAsset NewDialogue()
        {
            Directory.CreateDirectory("Assets/Dialogue");
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Dialogue/YeniDiyalog.asset");
            var asset = CreateInstance<DialogueAsset>();
            AssetDatabase.CreateAsset(asset, path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return asset;
        }
    }
}
