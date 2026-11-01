using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using Boruhan.Story;
using Boruhan.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Generates the prologue: placeholder art, speakers, dialogues, prefabs and scenes.
    /// Only creates what's missing — delete a file to have it regenerated. Never touches AnaMenu.
    /// </summary>
    public static class PrologueBuilder
    {
        internal const string ArtDir = "Assets/Art/Placeholder";
        const string DialogueDir = "Assets/Dialogue";
        const string SpeakerDir = "Assets/Dialogue/Konusmacilar";
        const string PrefabDir = "Assets/Prefabs";
        internal const string SceneDir = "Assets/Scenes";
        const string VideoDir = "Assets/StreamingAssets/Videolar";
        const string IntroVideoFile = "giris_sinematigi.mp4";
        public const string SkipHintText = "Atlamak için Enter / Boşluk / Tıkla";
        const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        internal static readonly Color Gold = new Color32(212, 168, 75, 255);
        internal static readonly Color TextLight = new Color32(230, 222, 205, 255);

        static Font uiFont;
        internal static Font titleFont;
        static Scene previousActive;

        internal enum Shape { Box, Circle, Tile, Steps, Figure, Mountain, Crescent }

        [MenuItem("Börühan/Prolog Sahnelerini Oluştur")]
        public static void Build()
        {
            EnsureFonts();

            foreach (var dir in new[] { ArtDir, DialogueDir, SpeakerDir, PrefabDir, SceneDir, VideoDir })
                Directory.CreateDirectory(dir);

            ConfigureYSorting();
            CreateEmptyVideoFile();
            MapKit.EnsureAssets();

            var speakers = CreateSpeakers();
            var dialogues = CreateDialogues(speakers);
            var prefabs = CreatePrefabs();

            var created = new List<string>();
            BuildScene(SceneNames.IntroCutscene, created, () => BuildIntroCutscene(dialogues, prefabs));
            BuildScene(SceneNames.Yurt, created, () => BuildYurt(dialogues, prefabs));
            BuildScene(SceneNames.Overworld, created, () => BuildOverworld(dialogues, prefabs));
            BuildScene(SceneNames.SkyRealm, created, () => BuildSkyRealm(dialogues, prefabs));
            BuildScene(SceneNames.Cave, created, () => BuildCave(dialogues, prefabs));
            BuildScene(SceneNames.Floor1Intro, created, () => BuildFloor1Intro(dialogues, prefabs));

            UpdateBuildSettings();
            AssetDatabase.SaveAssets();

            Debug.Log(created.Count > 0
                ? $"[Börühan] Oluşturulan sahneler: {string.Join(", ", created)}"
                : "[Börühan] Tüm sahneler zaten var; yeniden oluşturmak için sahne dosyasını silin.");
        }

        internal static void EnsureFonts()
        {
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleFont == null)
                titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/CinzelDecorative-Bold.ttf") ?? uiFont;
        }

        // ================= Project setup =================

        static void ConfigureYSorting()
        {
            // Sprites lower on screen draw in front, so the player can walk behind tall objects.
            var renderer = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Renderer2D.asset");
            if (renderer == null)
                return;
            var so = new SerializedObject(renderer);
            var mode = so.FindProperty("m_TransparencySortMode");
            var axis = so.FindProperty("m_TransparencySortAxis");
            if (mode == null || axis == null)
            {
                Debug.LogWarning("[Börühan] Renderer2D sıralama ayarı bulunamadı; Y-sıralamasını elle açın.");
                return;
            }
            mode.intValue = (int)TransparencySortMode.CustomAxis;
            axis.vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateEmptyVideoFile()
        {
            var path = $"{VideoDir}/{IntroVideoFile}";
            if (!File.Exists(path))
                File.WriteAllBytes(path, new byte[0]);
        }

        static void UpdateBuildSettings()
        {
            var ours = new[]
            {
                SceneNames.MainMenu, SceneNames.IntroCutscene, SceneNames.Yurt, SceneNames.Overworld,
                SceneNames.SkyRealm, SceneNames.Cave, SceneNames.Floor1Intro,
            }.Select(n => $"{SceneDir}/{n}.unity").ToList();

            var scenes = ours.Where(File.Exists).Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            scenes.AddRange(EditorBuildSettings.scenes.Where(s => !ours.Contains(s.path) && !s.path.EndsWith("/DenemeOdasi.unity")));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ================= Placeholder art =================

        /// <summary>Creates a placeholder PNG if missing. Replace the PNG with real art and every use updates.</summary>
        internal static Sprite Art(string name, int width, int height, Color color, Shape shape, bool bottomPivot = true) =>
            Art(ArtDir, name, width, height, color, shape, bottomPivot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f));

        /// <summary>Same as above, in any folder and with an exact pivot (0-1). Tall tiles use this to stand on their cell.</summary>
        internal static Sprite Art(string folder, string name, int width, int height, Color color, Shape shape, Vector2 pivot)
        {
            Directory.CreateDirectory(folder);
            var path = $"{folder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels32(Paint(width, height, color, shape, name.GetHashCode()));
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.wrapMode = shape == Shape.Tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color32[] Paint(int w, int h, Color color, Shape shape, int seed)
        {
            var px = new Color32[w * h];
            var rng = new System.Random(seed);
            Color dark = Color.Lerp(color, Color.black, 0.45f);
            dark.a = color.a;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                Color c = Color.clear;
                switch (shape)
                {
                    case Shape.Box:
                        c = edge ? dark : color;
                        break;
                    case Shape.Tile:
                        float n = (float)rng.NextDouble();
                        c = n > 0.9f ? Color.Lerp(color, Color.white, 0.15f) : n < 0.08f ? Color.Lerp(color, Color.black, 0.15f) : color;
                        break;
                    case Shape.Circle:
                        float r = Mathf.Min(w, h) / 2f;
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w / 2f, h / 2f));
                        c = d <= r - 1.5f ? color : d <= r ? dark : Color.clear;
                        break;
                    case Shape.Steps:
                        bool side = x < 2 || x >= w - 2;
                        c = side ? dark : (y / 4) % 2 == 0 ? color : Color.Lerp(color, Color.white, 0.25f);
                        break;
                    case Shape.Crescent:
                        // A sword arc bulging to the right (the swing direction).
                        float cr = Mathf.Min(w, h) / 2f;
                        var cp = new Vector2(x + 0.5f - w / 2f, y + 0.5f - h / 2f);
                        if (cp.magnitude <= cr && (cp - new Vector2(-cr * 0.5f, 0f)).magnitude > cr * 0.9f)
                            c = color;
                        break;
                    case Shape.Mountain:
                        // Triangle peak with a snowy top third.
                        float half = (w / 2f) * (1f - (float)y / h);
                        float dx = Mathf.Abs(x + 0.5f - w / 2f);
                        if (dx <= half)
                            c = y > h * 0.66f ? new Color32(235, 238, 245, 255) : dx > half - 1f ? dark : color;
                        break;
                    case Shape.Figure:
                        // Body on the bottom 65%, a lighter head on top.
                        int neck = Mathf.RoundToInt(h * 0.65f);
                        int headInset = Mathf.Max(1, w / 5);
                        if (y < neck)
                            c = (x == 0 || x == w - 1 || y == 0 || y == neck - 1) ? dark : color;
                        else if (x >= headInset && x < w - headInset)
                            c = (x == headInset || x == w - headInset - 1 || y == h - 1) ? new Color32(120, 90, 70, 255) : new Color32(222, 190, 160, 255);
                        break;
                }
                px[y * w + x] = c;
            }
            return px;
        }

        // ================= Dialogue assets =================

        class Speakers
        {
            public DialogueSpeaker boruhan, kayra, ulgen, whispers;
        }

        static Speakers CreateSpeakers()
        {
            return new Speakers
            {
                boruhan = Speaker("Boruhan", "Börühan", new Color32(222, 120, 90, 255), Art("Portre_Boruhan", 48, 48, new Color32(170, 60, 50, 255), Shape.Box, false)),
                kayra = Speaker("KayraHan", "Kayra Han", new Color32(250, 230, 170, 255), Art("Portre_KayraHan", 48, 48, new Color32(240, 225, 180, 255), Shape.Box, false)),
                ulgen = Speaker("UlgenHan", "Ülgen Han", new Color32(150, 200, 255, 255), Art("Portre_UlgenHan", 48, 48, new Color32(110, 160, 220, 255), Shape.Box, false)),
                whispers = Speaker("Fisiltilar", "Fısıltılar", new Color32(170, 140, 200, 255), null),
            };
        }

        static DialogueSpeaker Speaker(string file, string displayName, Color color, Sprite portrait)
        {
            var path = $"{SpeakerDir}/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueSpeaker>(path);
            if (existing != null)
                return existing;

            var speaker = ScriptableObject.CreateInstance<DialogueSpeaker>();
            speaker.displayName = displayName;
            speaker.nameColor = color;
            speaker.portrait = portrait;
            AssetDatabase.CreateAsset(speaker, path);
            return speaker;
        }

        static DialogueAsset Dialogue(string file, params DialogueLine[] lines)
        {
            var path = $"{DialogueDir}/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
            if (existing != null)
                return existing;

            var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.lines.AddRange(lines);
            AssetDatabase.CreateAsset(dialogue, path);
            return dialogue;
        }

        static DialogueLine L(DialogueSpeaker speaker, string text, string flag = null) =>
            new DialogueLine { speaker = speaker, text = text, setFlag = flag };

        static DialogueLine N(string text, string flag = null) => L(null, text, flag);

        class Dialogues
        {
            public DialogueAsset intro, yurtIntro, takeTalisman, chestEmpty, hearth, yurtDoorLocked;
            public DialogueAsset talismanGlow, ruins, caveLocked, returnedToEarth;
            public DialogueAsset skyArrival, kayra, kayraRepeat, skyStairsLocked;
            public DialogueAsset rift, floor1;
        }

        static Dialogues CreateDialogues(Speakers s)
        {
            var d = new Dialogues();

            d.intro = Dialogue("00_GirisSinematigi",
                N("Evvel zaman içinde, kalbur saman içinde..."),
                N("Soğuk bir kış gecesinde Ülgen Han yeryüzünü seyrederken, yeni doğmuş ve donmak üzere olan bir insan bebeği gördü."),
                N("Ona kıyamadı ve onu kendi himayesi altına aldı. O bebeğin adı Börühan'dı."),
                N("Göklerin ocağında, yıldızların diliyle büyütüldü; demir tozuyla biledi bileklerini, rüzgârın fısıltısıyla kuşandı ruhunu."),
                N("Fakat yeraltının efendisi Erlik Han, kıskançlığı ve nefretiyle bir tuzak kurarak Ülgen Han'ı gafil avladı."),
                N("Göklerin nuru sönüp Ülgen Han yeraltının dokuzuncu katındaki paslı demir kazıklara mahkûm edildiğinde..."),
                N("...Börühan için artık çocukluk çok geride kalmıştı."));

            d.yurtIntro = Dialogue("01_Otag_Giris",
                N("Börühan yaşadığı otağda, Ülgen Han'ın ona verdiği tılsımı düşünüyordu."),
                N("İçinde bir hüzün ve bir nefret vardı. İntikam almak istiyordu, ama yeterince güçlü değildi."),
                L(s.boruhan, "Babam... Seni o karanlıkta nasıl bulacağım?"));

            d.takeTalisman = Dialogue("02_Otag_TilsimiAl",
                N("Sandığın içinde, Ülgen Han'ın ona bıraktığı tılsım duruyordu."),
                N("Bir anda Ülgen Han'ın söylediği bir söz aklına geldi."),
                L(s.ulgen, "Eğer bir gün çaresiz hissedersen, o tılsım sana yol gösterecek."),
                L(s.boruhan, "Yol göster öyleyse..."),
                N("Börühan tılsımı aldı ve yolculuğa hazırlandı.", StoryFlags.TalismanTaken));

            d.chestEmpty = Dialogue("02b_Otag_SandikBos", N("Sandık artık boş."));

            d.hearth = Dialogue("03_Otag_Ocak",
                N("Ocağın közleri hâlâ sıcaktı."),
                L(s.boruhan, "Ülgen Han'la bu ateşin başında geçen geceler... Hepsini geri alacağım."));

            d.yurtDoorLocked = Dialogue("04_Otag_KapiKilitli", L(s.boruhan, "Tılsımı almadan gidemem."));

            d.talismanGlow = Dialogue("10_Yeryuzu_TilsimParliyor",
                N("Börühan otağdan çıktığında tılsım birden parlamaya başladı."),
                N("Bir yöne döndüğünde parlıyor, başka yöne gittiğinde sönüyordu."),
                L(s.boruhan, "Bana yol gösteriyor... Işığını takip etmeliyim."));

            d.ruins = Dialogue("11_Yeryuzu_Harabe",
                N("Tılsımın ışığını takip eden Börühan, harabe bir yapıya ulaştı."),
                N("Birden önünde, gökyüzüne uzanan bir merdiven belirdi.", StoryFlags.StairwayAppeared),
                L(s.boruhan, "Göklere uzanan bir merdiven... Ülgen Han, bu senin işin mi?"));

            d.caveLocked = Dialogue("12_Yeryuzu_MagaraKapali",
                L(s.boruhan, "Zifiri karanlık bir mağara... İçeriden soğuk bir rüzgâr esiyor."),
                N("Şimdilik burada yapacak bir şeyi yoktu."));

            d.returnedToEarth = Dialogue("13_Yeryuzu_Donus",
                N("Börühan merdivenden yeryüzüne indi ve yola koyuldu."),
                L(s.boruhan, "Karanlıktan da karanlık bir mağara... Tılsım yine parlıyor. Yolu o gösterecek."));

            d.skyArrival = Dialogue("20_Gok_Varis",
                N("Börühan merdivenden çıktı; bulutların da ötesinde bir yere..."),
                N("Orada birisi oturuyordu..."),
                N("Kayra Han. Evrenin yaratıcısı."));

            d.kayra = Dialogue("21_Gok_KayraHan",
                N("Börühan ona her şeyi anlatmak istiyordu, ama Kayra Han zaten her şeyi biliyordu."),
                L(s.kayra, "Seni yukarıdan izledim, evlat. Çok zor zamanlar geçiriyorsun."),
                L(s.kayra, "Söyle bakalım, nedir benden istediğin?"),
                L(s.boruhan, "Ulu Kayra Han... Yeterince güçlü değilim. Ülgen Han'a nasıl ulaşacağımı da bilmiyorum."),
                L(s.kayra, "Öyleyse sana gücümden bir parça vereyim. Bir kıvılcım..."),
                N("Börühan'ın kılıcı alev aldı, gözleri kızıla döndü. İçinde bir güç kabardığını hissetti.", StoryFlags.KutSpark),
                L(s.kayra, "Bu kıvılcım seni yalnızca korur. Erlik'i yenmek istiyorsan, yeraltının katlarında yendiğin her dehşetin özünü kendi ellerinle söküp almalısın."),
                L(s.kayra, "Karanlıktan da karanlık bir mağarada, aradığın yeri bulacaksın."),
                L(s.boruhan, "Sağ ol, Ulu Han. Babamı geri getireceğim."));

            d.kayraRepeat = Dialogue("22_Gok_KayraHan_Tekrar",
                L(s.kayra, "Yolun açık olsun, evlat. Karanlıktan da karanlık mağara seni bekliyor."));

            d.skyStairsLocked = Dialogue("23_Gok_MerdivenKilitli",
                L(s.boruhan, "Buraya kadar geldim. Önce onunla konuşmalıyım."));

            d.rift = Dialogue("30_Magara_YerYarildi",
                N("Birden mağaranın içindeki yer yarıldı!", StoryFlags.RiftOpened),
                N("Yarıktan kırmızı, turuncu ışıklar ve çığlıklar yükseliyordu."),
                L(s.boruhan, "Bu ses... Bu ışık..."),
                N("Börühan korkuyordu. Ama Ülgen Han'ı kurtarma isteği ona cesaret verdi."),
                L(s.boruhan, "Babam beni bekliyor. Geri dönmek yok."));

            d.floor1 = Dialogue("40_Kat1_Giris",
                N("Yarık, Börühan'ın arkasından kapandı."),
                N("Cehennemin ilk katındaydı: Yalanlar ve Sırlar Çölü."),
                N("Burası, yeryüzündeyken birbirine ihanet etmiş, dostunu sırtından vurmuş ya da sır saklayıp güveni zedelemiş ruhların cezalandırıldığı yerdi."),
                N("Çölün rüzgârı, insanın kulağına en güvendiği kişilerin sesleriyle fısıldar; yalanları, iftiraları ve şüpheyi büyüterek kalbi kurutur."),
                L(s.whispers, "Ülgen Han seni gerçekten evladı olarak mı sevdi... yoksa seni kendi büyük planı için bir araç mı yaptı?"),
                L(s.whispers, "Ya bir gün seni yüzüstü bırakırsa?"),
                L(s.boruhan, "Susun... Onu tanıyorum. Sizi dinlemeyeceğim."));

            return d;
        }

        // ================= Prefabs =================

        internal class Prefabs
        {
            public GameObject player, hud, dialogueBox;
        }

        internal static Prefabs CreatePrefabs()
        {
            var prefabs = new Prefabs
            {
                player = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Boruhan.prefab"),
                hud = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/OyunArayuzu.prefab"),
                dialogueBox = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/DiyalogKutusu.prefab"),
            };
            if (prefabs.player != null && prefabs.hud != null && prefabs.dialogueBox != null)
                return prefabs;

            // Build in a throwaway scene so the user's open scenes are untouched.
            EnsureFonts();
            var temp = BeginScene();
            if (prefabs.player == null)
                prefabs.player = SavePrefab(BuildPlayer(), "Boruhan");
            if (prefabs.hud == null)
                prefabs.hud = SavePrefab(BuildHud(), "OyunArayuzu");
            if (prefabs.dialogueBox == null)
                prefabs.dialogueBox = SavePrefab(BuildDialogueBox(), "DiyalogKutusu");
            CloseScene(temp);
            return prefabs;
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildPlayer()
        {
            var go = new GameObject("Boruhan") { tag = "Player" };
            go.AddComponent<SortingGroup>();

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Feet-only collider: the body can overlap things above it, like in Undertale.
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.35f);
            col.offset = new Vector2(0f, 0.18f);

            SpriteObject(go.transform, "Govde", Art("Boruhan", 12, 22, new Color32(170, 60, 50, 255), Shape.Figure), Vector2.zero, 0);
            var facing = SpriteObject(go.transform, "YonGostergesi", Art("YonGostergesi", 3, 3, Gold, Shape.Box, false), new Vector2(0f, 0.1f), 3);

            var light = new GameObject("Isik").AddComponent<Light2D>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            PointLight(light, new Color(1f, 0.95f, 0.85f), 0.6f, 4.5f);

            // Kayra Han's spark: red eyes and the blade's glow. Hidden until the kut_kivilcimi flag.
            // (The sword itself is drawn in Börühan's sword animations, see AnimationKit.)
            var spark = new GameObject("KutKivilcimi");
            spark.transform.SetParent(go.transform, false);
            SpriteObject(spark.transform, "KizilGozler", Art("KizilGozler", 6, 1, new Color32(255, 40, 40, 255), Shape.Box, false), new Vector2(0f, 1.12f), 1);
            var flame = new GameObject("AlevIsigi").AddComponent<Light2D>();
            flame.transform.SetParent(spark.transform, false);
            flame.transform.localPosition = new Vector3(0.45f, 0.6f, 0f);
            PointLight(flame, new Color(1f, 0.5f, 0.15f), 1.1f, 3f);
            spark.SetActive(false);

            var controller = go.AddComponent<PlayerController>();
            Set(controller, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            Set(controller, "facingIndicator", facing.transform);
            go.AddComponent<PlayerInteractor>();

            var activator = go.AddComponent<FlagActivator>();
            Set(activator, "flag", StoryFlags.KutSpark);
            SetArray(activator, "targets", spark);
            return go;
        }

        static GameObject BuildHud()
        {
            var canvas = CreateCanvas("OyunArayuzu", 40);

            // Talisman, top-left
            var talisman = UIObject("Tilsim", canvas.transform);
            var tRect = (RectTransform)talisman.transform;
            tRect.anchorMin = tRect.anchorMax = tRect.pivot = new Vector2(0f, 1f);
            tRect.anchoredPosition = new Vector2(40f, -40f);
            tRect.sizeDelta = new Vector2(110f, 110f);
            var halo = UIImage(talisman.transform, "Hale", Art("Tilsim_Hale", 32, 32, Color.white, Shape.Circle, false), new Vector2(150f, 150f));
            var icon = UIImage(talisman.transform, "Ikon", Art("Tilsim_Ikon", 16, 16, new Color32(140, 220, 255, 255), Shape.Circle, false), new Vector2(84f, 84f));
            var hud = canvas.gameObject.AddComponent<TalismanHud>();
            Set(hud, "root", talisman);
            Set(hud, "icon", icon);
            Set(hud, "halo", halo);

            // Interact prompt, bottom-center
            var prompt = UIObject("EtkilesimIpucu", canvas.transform);
            var pRect = (RectTransform)prompt.transform;
            pRect.anchorMin = pRect.anchorMax = pRect.pivot = new Vector2(0.5f, 0f);
            pRect.anchoredPosition = new Vector2(0f, 300f);
            pRect.sizeDelta = new Vector2(260f, 56f);
            prompt.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            var promptText = Label(prompt.transform, "Yazi", "[E] Bak", 30, TextLight);
            Stretch(promptText.gameObject);
            var interactPrompt = canvas.gameObject.AddComponent<InteractPrompt>();
            Set(interactPrompt, "root", prompt);
            Set(interactPrompt, "label", promptText);

            // Controls hint, top-right
            var hint = Label(canvas.transform, "KontrolIpucu", "Hareket: WASD / Ok Tuşları  ·  Etkileşim: E  ·  Duraklat: Esc", 22, new Color32(200, 190, 170, 160));
            hint.alignment = TextAnchor.UpperRight;
            var hRect = hint.rectTransform;
            hRect.anchorMin = hRect.anchorMax = hRect.pivot = new Vector2(1f, 1f);
            hRect.anchoredPosition = new Vector2(-30f, -24f);
            hRect.sizeDelta = new Vector2(900f, 40f);

            // Pause menu
            var panel = UIObject("DuraklatmaPaneli", canvas.transform);
            Stretch(panel);
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Sized(Label(panel.transform, "Baslik", "Duraklatıldı", 72, Gold, titleFont), 1200, 100);
            var resume = MakeButton(panel.transform, "DevamButonu", "Devam Et");
            var save = MakeButton(panel.transform, "KaydetButonu", "Kaydet");
            var mainMenu = MakeButton(panel.transform, "AnaMenuButonu", "Kaydet ve Ana Menüye Dön");
            var status = Sized(Label(panel.transform, "Durum", "", 30, TextLight), 1200, 50);

            var pause = canvas.gameObject.AddComponent<PauseMenu>();
            Set(pause, "panel", panel);
            Set(pause, "resumeButton", resume);
            Set(pause, "saveButton", save);
            Set(pause, "mainMenuButton", mainMenu);
            Set(pause, "statusText", status);
            return canvas.gameObject;
        }

        static GameObject BuildDialogueBox()
        {
            var canvas = CreateCanvas("DiyalogKutusu", 50);

            var box = UIObject("Kutu", canvas.transform);
            var rect = (RectTransform)box.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(140f, 36f);
            rect.offsetMax = new Vector2(-140f, 36f + 230f);
            // Opaque on purpose: the Outline effect draws copies of the image, which would tint a see-through box.
            box.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.07f, 1f);
            var outline = box.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
            outline.effectDistance = new Vector2(2f, -2f);

            var portrait = UIImage(box.transform, "Portre", null, new Vector2(160f, 160f));
            portrait.preserveAspect = true;
            var pRect = portrait.rectTransform;
            pRect.anchorMin = pRect.anchorMax = pRect.pivot = new Vector2(0f, 0.5f);
            pRect.anchoredPosition = new Vector2(24f, 0f);

            var speaker = Label(box.transform, "Konusmaci", "İsim", 30, Gold, titleFont);
            speaker.alignment = TextAnchor.UpperLeft;
            var sRect = speaker.rectTransform;
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0f, 1f);
            sRect.offsetMin = new Vector2(48f, -62f);
            sRect.offsetMax = new Vector2(-48f, -18f);

            var body = Label(box.transform, "Metin", "", 32, TextLight);
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.supportRichText = true;
            body.lineSpacing = 1.1f;
            var bRect = body.rectTransform;
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = new Vector2(48f, 24f);
            bRect.offsetMax = new Vector2(-60f, -66f);

            var more = Label(box.transform, "DevamIsareti", "»", 40, Gold);
            var mRect = more.rectTransform;
            mRect.anchorMin = mRect.anchorMax = mRect.pivot = new Vector2(1f, 0f);
            mRect.anchoredPosition = new Vector2(-24f, 12f);
            mRect.sizeDelta = new Vector2(40f, 50f);

            var runner = canvas.gameObject.AddComponent<DialogueRunner>();
            Set(runner, "box", box);
            Set(runner, "speakerText", speaker);
            Set(runner, "bodyText", body);
            Set(runner, "portrait", portrait);
            Set(runner, "continueIndicator", more.gameObject);
            return canvas.gameObject;
        }

        // ================= Scenes =================

        static void BuildScene(string name, List<string> created, System.Action build)
        {
            var path = $"{SceneDir}/{name}.unity";
            if (File.Exists(path))
                return;

            var scene = BeginScene();
            build();
            EditorSceneManager.SaveScene(scene, path);
            CloseScene(scene);
            created.Add(name);
        }

        static void BuildIntroCutscene(Dialogues d, Prefabs p)
        {
            var cam = CreateCamera(5f, Color.black);
            var video = cam.gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = false;
            video.renderMode = VideoRenderMode.CameraNearPlane;
            video.targetCamera = cam;
            video.aspectRatio = VideoAspectRatio.FitInside;
            video.audioOutputMode = VideoAudioOutputMode.None; // the video's own sound is never played

            var canvas = CreateCanvas("Canvas", 10);
            var placeholder = UIObject("VideoYerTutucu", canvas.transform);
            Stretch(placeholder);
            placeholder.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.05f, 1f);
            var label = Label(placeholder.transform, "Yazi",
                $"[ Giriş sinematiği ]\nVideo dosyası: Assets/StreamingAssets/Videolar/{IntroVideoFile}", 28, new Color32(110, 100, 90, 255));
            Stretch(label.gameObject);
            label.rectTransform.offsetMax = new Vector2(0f, -300f);

            var title = CenterTitle(canvas.transform);
            SkipHint(canvas.transform);

            Instantiate(p.dialogueBox);
            MapKit.AddSceneMusic(MapKit.GetTrack("Sinematik"));

            var cutscene = new GameObject("Sinematik").AddComponent<CutscenePlayer>();
            Set(cutscene, "videoPlayer", video);
            Set(cutscene, "videoFileName", IntroVideoFile);
            Set(cutscene, "placeholder", placeholder);
            Set(cutscene, "subtitles", d.intro);
            Set(cutscene, "titleText", title);
            Set(cutscene, "nextScene", SceneNames.Yurt);
                        Set(cutscene, "locationName", "Giriş");
        }

        static void BuildYurt(Dialogues d, Prefabs p)
        {
            var area = new Rect(-6f, -4f, 12f, 8f);
            var ctx = GameplayScene(p, "Otağ", area, new Vector2(-3.5f, 0.5f), new Color(0.04f, 0.03f, 0.03f), 0.35f, new Color(1f, 0.85f, 0.7f), 5f);

            Floor(ctx.world, "Zemin", Art("Otag_Zemin", 16, 16, new Color32(110, 45, 40, 255), Shape.Tile), area);
            MapKit.AddSceneMusic(MapKit.GetTrack("Otag"));
            var wall = Floor(ctx.world, "UstDuvar", Art("Otag_Duvar", 16, 16, new Color32(120, 90, 60, 255), Shape.Tile), new Rect(area.xMin, area.yMax, area.width, 2.5f));
            wall.GetComponent<SpriteRenderer>().sortingOrder = -90;

            // Room edges, with a gap in the bottom wall for the door.
            Wall(ctx.world, "SolSinir", new Rect(area.xMin - 1f, area.yMin - 2f, 1f, area.height + 4f));
            Wall(ctx.world, "SagSinir", new Rect(area.xMax, area.yMin - 2f, 1f, area.height + 4f));
            Wall(ctx.world, "UstSinir", new Rect(area.xMin, area.yMax, area.width, 1f));
            Wall(ctx.world, "AltSinirSol", new Rect(area.xMin, area.yMin - 1f, area.width / 2f - 0.9f, 1f));
            Wall(ctx.world, "AltSinirSag", new Rect(0.9f, area.yMin - 1f, area.width / 2f - 0.9f, 1f));
            Wall(ctx.world, "KapiDisiSinir", new Rect(-0.9f, area.yMin - 2f, 1.8f, 0.6f));
            Floor(ctx.world, "KapiAcikligi", Art("Otag_Kapi", 16, 16, new Color32(30, 22, 18, 255), Shape.Tile), new Rect(-0.9f, area.yMin - 1.4f, 1.8f, 1.4f));

            Prop(ctx.world, "Yatak", Art("Otag_Yatak", 40, 24, new Color32(140, 60, 55, 255), Shape.Box), new Vector2(-4f, 1.2f), 1f);
            var hearth = Prop(ctx.world, "Ocak", Art("Otag_Ocak", 24, 20, new Color32(60, 50, 45, 255), Shape.Box), new Vector2(0f, 0.3f), 0.8f);
            var hearthLight = new GameObject("OcakIsigi").AddComponent<Light2D>();
            hearthLight.transform.SetParent(hearth.transform, false);
            hearthLight.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            PointLight(hearthLight, new Color(1f, 0.55f, 0.2f), 1.2f, 5f);
            Interact(hearth, "Bak", d.hearth);

            var chest = Prop(ctx.world, "Sandik", Art("Otag_Sandik", 24, 16, new Color32(120, 80, 40, 255), Shape.Box), new Vector2(3.5f, 1.6f), 0.8f);
            // The Tılsım shows on Börühan once taken (his Tılsım animations).
            Interact(chest, "Bak", d.takeTalisman, completedFlag: StoryFlags.TalismanTaken, repeat: d.chestEmpty);

            Spawn(ctx.world, "kapi", new Vector2(0f, -2.6f));
            Door(ctx.world, "Kapi", new Vector2(0f, area.yMin - 0.9f), new Vector2(1.8f, 0.8f), SceneNames.Overworld, "otag_kapisi", StoryFlags.TalismanTaken, d.yurtDoorLocked);
            StartTrigger(ctx.world, "GirisAnlatimi", d.yurtIntro, null, StoryFlags.YurtIntroSeen);
        }

        static void BuildOverworld(Dialogues d, Prefabs p)
        {
            var area = new Rect(-32f, -22f, 64f, 44f);
            var ctx = GameplayScene(p, "Bozkır", area, new Vector2(0f, -14f), new Color(0.02f, 0.03f, 0.07f), 0.5f, new Color(0.7f, 0.78f, 1f), 6f);

            MapKit.CreateRoomMap(area, MapKit.GetTheme("Bozkir"), ctx.camera);
            RoomBounds(ctx.world, area);
            MapKit.AddSceneMusic(MapKit.GetTrack("Bozkir"));

            // Yurt (exterior)
            Prop(ctx.world, "Otag", Art("Otag_Dis", 64, 48, new Color32(200, 180, 150, 255), Shape.Box), new Vector2(0f, -12f), 1.6f);
            Floor(ctx.world, "OtagKapisi", Art("Otag_Kapi", 16, 16, new Color32(30, 22, 18, 255), Shape.Tile), new Rect(-0.6f, -12f, 1.2f, 1.4f)).GetComponent<SpriteRenderer>().sortingOrder = 1;
            Door(ctx.world, "OtagKapiGecisi", new Vector2(0f, -12.25f), new Vector2(1.2f, 0.5f), SceneNames.Yurt, "kapi");
            Spawn(ctx.world, "otag_kapisi", new Vector2(0f, -13.8f));

            // Ruins and the stairway to the sky
            Prop(ctx.world, "Harabe", Art("Harabe", 96, 64, new Color32(110, 105, 100, 255), Shape.Box), new Vector2(20f, 12f), 1.6f);
            Trigger(ctx.world, "HarabeTetik", new Vector2(20f, 10.6f), new Vector2(6f, 2.2f), d.ruins, StoryFlags.TalismanTaken, StoryFlags.StairwayAppeared);

            var stairs = new GameObject("GokMerdiveni");
            stairs.transform.SetParent(ctx.world, false);
            Prop(stairs.transform, "Merdiven", Art("GokMerdiveni", 32, 112, new Color32(240, 210, 130, 220), Shape.Steps), new Vector2(24.5f, 11f), 0f);
            var stairLight = new GameObject("MerdivenIsigi").AddComponent<Light2D>();
            stairLight.transform.SetParent(stairs.transform, false);
            stairLight.transform.position = new Vector3(24.5f, 13f, 0f);
            PointLight(stairLight, new Color(1f, 0.85f, 0.5f), 1.2f, 6f);
            Door(stairs.transform, "MerdivenGecisi", new Vector2(24.5f, 11.3f), new Vector2(1.4f, 0.8f), SceneNames.SkyRealm, "merdiven_ust");
            Activator(ctx.world, "MerdivenGorunurlugu", StoryFlags.StairwayAppeared, true, stairs);
            Spawn(ctx.world, "merdiven_alti", new Vector2(24.5f, 9.6f));

            // The cave
            Prop(ctx.world, "Magara", Art("Magara_Agzi", 48, 40, new Color32(15, 12, 14, 255), Shape.Box), new Vector2(-24f, 8f), 1.2f);
            Door(ctx.world, "MagaraGirisi", new Vector2(-24f, 7.8f), new Vector2(1.4f, 0.6f), SceneNames.Cave, "giris", StoryFlags.KutSpark, d.caveLocked);

            Spawn(ctx.world, "magara_agzi", new Vector2(-24f, 6.2f));

            // Scenery
            var rock = Art("Kaya", 24, 16, new Color32(95, 95, 105, 255), Shape.Box);
            var tree = Art("KuruAgac", 16, 40, new Color32(45, 35, 35, 255), Shape.Box);
            var rocks = new[] { new Vector2(-8f, -6f), new Vector2(6f, -4f), new Vector2(-14f, -16f), new Vector2(12f, -18f), new Vector2(-4f, 6f), new Vector2(9f, 4f), new Vector2(-18f, 0f), new Vector2(28f, -6f), new Vector2(-28f, -12f), new Vector2(2f, 16f) };
            var trees = new[] { new Vector2(-6f, -10f), new Vector2(8f, -12f), new Vector2(-12f, 4f), new Vector2(14f, 0f), new Vector2(-20f, 14f), new Vector2(4f, 10f), new Vector2(16f, 18f), new Vector2(-26f, -4f), new Vector2(26f, 2f), new Vector2(-10f, 16f), new Vector2(22f, -14f), new Vector2(-16f, -8f) };
            for (int i = 0; i < rocks.Length; i++)
                Prop(ctx.world, $"Kaya{i + 1}", rock, rocks[i], 0.6f);
            for (int i = 0; i < trees.Length; i++)
                Prop(ctx.world, $"KuruAgac{i + 1}", tree, trees[i], 0.5f, 0.5f);

            StartTrigger(ctx.world, "TilsimParliyor", d.talismanGlow, StoryFlags.TalismanTaken, StoryFlags.TalismanGlowSeen);
            StartTrigger(ctx.world, "YeryuzuneDonus", d.returnedToEarth, StoryFlags.KutSpark, StoryFlags.ReturnedToEarth);

            var guide = new GameObject("TilsimRehberi").AddComponent<TalismanGuide>();
            guide.transform.SetParent(ctx.world, false);
            Set(guide, "player", ctx.player);
            var guideSo = new SerializedObject(guide);
            var targets = guideSo.FindProperty("targets");
            void AddTarget(string name, Vector2 point, string from, string until)
            {
                var marker = new GameObject($"Hedef_{name}").transform;
                marker.SetParent(guide.transform, false);
                marker.position = point;
                targets.arraySize++;
                var t = targets.GetArrayElementAtIndex(targets.arraySize - 1);
                t.FindPropertyRelative("name").stringValue = name;
                t.FindPropertyRelative("point").objectReferenceValue = marker;
                t.FindPropertyRelative("activeFromFlag").stringValue = from;
                t.FindPropertyRelative("activeUntilFlag").stringValue = until;
            }
            AddTarget("Harabe", new Vector2(20f, 10.5f), StoryFlags.TalismanTaken, StoryFlags.StairwayAppeared);
            AddTarget("Merdiven", new Vector2(24.5f, 11f), StoryFlags.StairwayAppeared, StoryFlags.KutSpark);
            AddTarget("Magara", new Vector2(-24f, 7.5f), StoryFlags.KutSpark, StoryFlags.RiftOpened);
            guideSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildSkyRealm(Dialogues d, Prefabs p)
        {
            var area = new Rect(-10f, -7f, 20f, 14f);
            var ctx = GameplayScene(p, "Gök Katı", area, new Vector2(0f, -4.6f), new Color(0.16f, 0.18f, 0.32f), 1f, new Color(1f, 0.97f, 0.9f), 5f);

            Floor(ctx.world, "Zemin", Art("Bulut_Zemin", 16, 16, new Color32(225, 225, 240, 255), Shape.Tile), area);
            RoomBounds(ctx.world, area);
            MapKit.AddSceneMusic(MapKit.GetTrack("Gok"));

            var cloud = Art("Bulut", 48, 24, new Color32(245, 245, 255, 255), Shape.Box);
            foreach (var pos in new[] { new Vector2(-7f, 3f), new Vector2(7f, 2f), new Vector2(-6f, -3f), new Vector2(6.5f, -4f) })
                Prop(ctx.world, "Bulut", cloud, pos, 0.8f);

            var throne = Prop(ctx.world, "Taht", Art("Taht", 48, 40, new Color32(230, 200, 120, 255), Shape.Box), new Vector2(0f, 2.5f), 1f);
            var kayra = SpriteObject(throne.transform, "KayraHan", Art("KayraHan", 20, 34, new Color32(245, 240, 225, 255), Shape.Figure), new Vector2(0f, 0.6f), 1);
            kayra.GetComponent<SpriteRenderer>().spriteSortPoint = SpriteSortPoint.Pivot;
            Interact(throne, "Konuş", d.kayra, completedFlag: StoryFlags.KutSpark, repeat: d.kayraRepeat);

            Prop(ctx.world, "AsagiMerdiven", Art("GokMerdiveni_Ust", 32, 24, new Color32(240, 210, 130, 220), Shape.Steps), new Vector2(0f, area.yMin), 0f);
            Door(ctx.world, "MerdivenGecisi", new Vector2(0f, area.yMin + 0.6f), new Vector2(1.4f, 0.8f), SceneNames.Overworld, "merdiven_alti", StoryFlags.KutSpark, d.skyStairsLocked);
            Spawn(ctx.world, "merdiven_ust", new Vector2(0f, -4.6f));

            StartTrigger(ctx.world, "VarisAnlatimi", d.skyArrival, null, StoryFlags.SkyArrivalSeen);
        }

        static void BuildCave(Dialogues d, Prefabs p)
        {
            var area = new Rect(-5f, -15f, 10f, 30f);
            var ctx = GameplayScene(p, "Karanlık Mağara", area, new Vector2(0f, -12.5f), Color.black, 0.08f, new Color(0.6f, 0.6f, 0.8f), 5f);

            MapKit.CreateRoomMap(area, MapKit.GetTheme("Magara"), ctx.camera);
            RoomBounds(ctx.world, area);
            MapKit.AddSceneMusic(MapKit.GetTrack("Magara"));

            var stalagmite = Art("Sarkit", 12, 20, new Color32(80, 70, 65, 255), Shape.Box);
            var positions = new[] { new Vector2(-4f, -9f), new Vector2(3.8f, -6f), new Vector2(-3.5f, -1f), new Vector2(4f, 3f), new Vector2(-4.2f, 7f), new Vector2(3.5f, 11f), new Vector2(-3f, 12.5f), new Vector2(2.5f, -11f) };
            for (int i = 0; i < positions.Length; i++)
                Prop(ctx.world, $"Sarkit{i + 1}", stalagmite, positions[i], 0.5f);

            Door(ctx.world, "Cikis", new Vector2(0f, area.yMin + 0.4f), new Vector2(2f, 0.8f), SceneNames.Overworld, "magara_agzi");
            Spawn(ctx.world, "giris", new Vector2(0f, -12.5f));

            var trigger = Trigger(ctx.world, "YerYarilmasi", new Vector2(0f, 2f), new Vector2(area.width, 1.2f), d.rift, StoryFlags.KutSpark, StoryFlags.RiftOpened);
            UnityEventTools.AddFloatPersistentListener(trigger.onStarted, ctx.camera.Shake, 1.2f);

            var rift = new GameObject("Yarik");
            rift.transform.SetParent(ctx.world, false);
            var riftSprite = SpriteObject(rift.transform, "YarikGorseli", Art("Yarik", 48, 24, new Color32(255, 90, 30, 255), Shape.Box, false), new Vector2(0f, 6.5f), -50);
            riftSprite.transform.localPosition = new Vector3(0f, 6.5f, 0f);
            var riftLight = new GameObject("YarikIsigi").AddComponent<Light2D>();
            riftLight.transform.SetParent(rift.transform, false);
            riftLight.transform.localPosition = new Vector3(0f, 6.5f, 0f);
            PointLight(riftLight, new Color(1f, 0.35f, 0.1f), 1.6f, 7f);
            Door(rift.transform, "YarikGecisi", new Vector2(0f, 6.5f), new Vector2(2.6f, 1f), SceneNames.Floor1Intro, "");
            Activator(ctx.world, "YarikGorunurlugu", StoryFlags.RiftOpened, true, rift);
        }

        static void BuildFloor1Intro(Dialogues d, Prefabs p)
        {
            CreateCamera(5f, Color.black);
            var canvas = CreateCanvas("Canvas", 10);
            var bg = UIImage(canvas.transform, "Arkaplan", Art("Kat1_Arkaplan", 320, 180, new Color32(120, 95, 70, 255), Shape.Tile, false), Vector2.zero);
            Stretch(bg.gameObject);
            bg.color = new Color(1f, 1f, 1f, 0.55f);
            var title = CenterTitle(canvas.transform);
            SkipHint(canvas.transform);

            Instantiate(p.dialogueBox);
            MapKit.AddSceneMusic(MapKit.GetTrack("Kat1"));

            var cutscene = new GameObject("Sinematik").AddComponent<CutscenePlayer>();
            Set(cutscene, "subtitles", d.floor1);
            Set(cutscene, "titleText", title);
            Set(cutscene, "openingTitle", "1. KAT\nYalanlar ve Sırlar Çölü");
            Set(cutscene, "closingTitle", "Devam edecek...");
            Set(cutscene, "nextScene", SceneNames.MainMenu);
            Set(cutscene, "locationName", "Yalanlar ve Sırlar Çölü");
            Set(cutscene, "floor", 1);
        }

        // ================= Scene building helpers =================

        internal class GameplayContext
        {
            public Transform world;
            public PlayerController player;
            public CameraFollow camera;
        }

        internal static GameplayContext GameplayScene(Prefabs p, string location, Rect area, Vector2 defaultSpawn, Color background, float lightIntensity, Color lightColor, float cameraSize)
        {
            var light = new GameObject("GlobalIsik").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = lightIntensity;
            light.color = lightColor;

            var world = new GameObject("Dunya").transform;

            var player = Instantiate(p.player).GetComponent<PlayerController>();
            player.transform.position = defaultSpawn;
            var spawn = new GameObject("VarsayilanBaslangic").transform;
            spawn.position = defaultSpawn;

            var cam = CreateCamera(cameraSize, background);
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            Set(follow, "target", player.transform);
            Set(follow, "bounds", new Rect(area.xMin - 1f, area.yMin - 1f, area.width + 2f, area.height + 3f));

            var manager = new GameObject("SahneYoneticisi").AddComponent<RoomManager>();
            Set(manager, "locationName", location);
            Set(manager, "player", player);
            Set(manager, "defaultSpawn", spawn);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            Instantiate(p.hud);
            Instantiate(p.dialogueBox);

            return new GameplayContext { world = world, player = player, camera = follow };
        }

        internal static GameObject Instantiate(GameObject prefab) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());

        internal static GameObject SpriteObject(Transform parent, string name, Sprite sprite, Vector2 localPosition, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go;
        }

        /// <summary>A standing object, y-sorted by its base, with a collider covering its footprint.</summary>
        internal static GameObject Prop(Transform parent, string name, Sprite sprite, Vector2 position, float footprintHeight, float footprintWidthScale = 0.9f)
        {
            var go = SpriteObject(parent, name, sprite, Vector2.zero, 0);
            go.transform.position = position;
            go.GetComponent<SpriteRenderer>().spriteSortPoint = SpriteSortPoint.Pivot;
            if (footprintHeight > 0f)
            {
                var size = sprite.bounds.size;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(size.x * footprintWidthScale, footprintHeight);
                col.offset = new Vector2(0f, footprintHeight / 2f);
            }
            return go;
        }

        static GameObject Floor(Transform parent, string name, Sprite tile, Rect area)
        {
            var go = SpriteObject(parent, name, tile, Vector2.zero, -100);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = area.size;
            // A tiled sprite is laid out around its pivot, so offset by the pivot to cover exactly the area.
            var pivot = tile.pivot / tile.rect.size;
            go.transform.position = area.min + Vector2.Scale(area.size, pivot);
            return go;
        }

        internal static void Wall(Transform parent, string name, Rect r)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = r.center;
            go.AddComponent<BoxCollider2D>().size = r.size;
        }

        internal static void RoomBounds(Transform parent, Rect a)
        {
            Wall(parent, "SinirSol", new Rect(a.xMin - 1f, a.yMin - 1f, 1f, a.height + 2f));
            Wall(parent, "SinirSag", new Rect(a.xMax, a.yMin - 1f, 1f, a.height + 2f));
            Wall(parent, "SinirAlt", new Rect(a.xMin, a.yMin - 1f, a.width, 1f));
            Wall(parent, "SinirUst", new Rect(a.xMin, a.yMax, a.width, 1f));
        }

        internal static GameObject Spawn(Transform parent, string id, Vector2 position)
        {
            var go = new GameObject($"Baslangic_{id}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<SpawnPoint>().id = id;
            return go;
        }

        internal static GameObject Door(Transform parent, string name, Vector2 position, Vector2 size, string scene, string spawn, string requiredFlag = null, DialogueAsset locked = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var door = go.AddComponent<SceneDoor>();
            Set(door, "targetScene", scene);
            Set(door, "targetSpawn", spawn);
            Set(door, "requiredFlag", requiredFlag ?? "");
            Set(door, "lockedDialogue", locked);
            return go;
        }

        internal static StoryTrigger Trigger(Transform parent, string name, Vector2 position, Vector2 size, DialogueAsset dialogue, string requiredFlag, string onceFlag)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var trigger = go.AddComponent<StoryTrigger>();
            Set(trigger, "dialogue", dialogue);
            Set(trigger, "requiredFlag", requiredFlag ?? "");
            Set(trigger, "onceFlag", onceFlag ?? "");
            return trigger;
        }

        internal static void StartTrigger(Transform parent, string name, DialogueAsset dialogue, string requiredFlag, string onceFlag)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var trigger = go.AddComponent<StoryTrigger>();
            Set(trigger, "dialogue", dialogue);
            Set(trigger, "playOnStart", true);
            Set(trigger, "requiredFlag", requiredFlag ?? "");
            Set(trigger, "onceFlag", onceFlag ?? "");
        }

        internal static void Interact(GameObject go, string prompt, DialogueAsset dialogue, string completedFlag = null, DialogueAsset repeat = null)
        {
            var interactable = go.AddComponent<Interactable>();
            Set(interactable, "prompt", prompt);
            Set(interactable, "dialogue", dialogue);
            Set(interactable, "completedFlag", completedFlag ?? "");
            Set(interactable, "repeatDialogue", repeat);
        }

        internal static void Activator(Transform parent, string name, string flag, bool activeWhenSet, params GameObject[] targets)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var activator = go.AddComponent<FlagActivator>();
            Set(activator, "flag", flag);
            Set(activator, "activeWhenSet", activeWhenSet);
            SetArray(activator, "targets", targets);
        }

        internal static void PointLight(Light2D light, Color color, float intensity, float radius)
        {
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.15f;
        }

        internal static Camera CreateCamera(float size, Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            return cam;
        }

        internal static Scene BeginScene()
        {
            previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene;
        }

        internal static void CloseScene(Scene scene)
        {
            if (previousActive.IsValid())
                SceneManager.SetActiveScene(previousActive);
            EditorSceneManager.CloseScene(scene, true);
        }

        // ================= UI helpers =================

        internal static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        internal static GameObject UIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static void Stretch(GameObject go)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal static Text Label(Transform parent, string name, string content, int size, Color color, Font font = null)
        {
            var text = UIObject(name, parent).AddComponent<Text>();
            text.font = font != null ? font : uiFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        internal static Text Sized(Text text, float width, float height)
        {
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            return text;
        }

        internal static Image UIImage(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var image = UIObject(name, parent).AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        internal static Text CenterTitle(Transform parent)
        {
            var title = Label(parent, "Baslik", "", 72, Gold, titleFont);
            Stretch(title.gameObject);
            title.lineSpacing = 1.2f;
            return title;
        }

        internal static void SkipHint(Transform parent)
        {
            var skip = Label(parent, "AtlaIpucu", SkipHintText, 22, new Color32(200, 190, 170, 140));
            skip.alignment = TextAnchor.UpperRight;
            var rect = skip.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -24f);
            rect.sizeDelta = new Vector2(700f, 40f);
        }

        internal static Button MakeButton(Transform parent, string name, string label)
        {
            var go = UIObject(name, parent);
            go.AddComponent<Image>().color = new Color32(40, 33, 42, 255);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = colors.selectedColor = new Color(1.6f, 1.4f, 1.1f);
            colors.colorMultiplier = 2f;
            button.colors = colors;
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 460;
            layout.preferredHeight = 80;
            Stretch(Label(go.transform, "Yazi", label, 34, TextLight).gameObject);
            return button;
        }

        // ================= Serialized field helpers =================

        internal static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Börühan] {target.GetType().Name}.{field} bulunamadı.");
                return;
            }
            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object o: prop.objectReferenceValue = o; break;
                case string s: prop.stringValue = s; break;
                case bool b: prop.boolValue = b; break;
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case Rect r: prop.rectValue = r; break;
                default: Debug.LogError($"[Börühan] Desteklenmeyen tür: {value.GetType()}"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(Object target, string field, params GameObject[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
