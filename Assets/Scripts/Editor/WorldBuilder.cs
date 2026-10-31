using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boruhan.Audio;
using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using Boruhan.Map;
using Boruhan.Story;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Shape = Boruhan.EditorTools.PrologueBuilder.Shape;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Builds the underworld: Kat 1 (desert) through Kat 9 (Erlik's throne) and the ending.
    /// Maps are laid out as rooms and paths on a grid (see the Build… methods), then painted
    /// into the Harita tilemaps, so everything can be repainted by hand afterwards.
    /// Only scenes/assets that don't exist yet are created.
    /// </summary>
    public static class WorldBuilder
    {
        const string DialogueDir = "Assets/Dialogue/Dunya";

        // Scene names
        public const string Kat1 = "Kat1_Col";
        public const string Kat1Boss = "Kat1_Ayna";
        public const string Kat2Intro = "Kat2Giris";
        public const string Kat2 = "Kat2_Demirhane";
        public const string Kat2Boss = "Kat2_Temir";
        public const string Kat3Intro = "Kat3Giris";
        public const string Kat3 = "Kat3_Bataklik";
        public const string Kat3Boss = "Kat3_Calis";
        public const string Kat9Intro = "Kat9Giris";
        public const string Kat9 = "Kat9_Taht";
        public const string Ending = "Son";

        static Dictionary<string, EnemyDefinition> enemies;
        static Dictionary<string, DialogueSpeaker> speakers;

        [MenuItem("Börühan/Yeraltını Oluştur")]
        public static void Build()
        {
            MapKit.EnsureAssets();
            EnsureFloorAssets();
            CombatKit.Setup();
            enemies = CombatKit.EnsureEnemies();
            EnsureFloorEnemies();
            EnsureSpeakers();

            BuildScene(Kat1, BuildKat1);
            BuildScene(Kat1Boss, () => BossArena(Kat1Boss, "Aynanın Sonu", 1, "Col", enemies["AynaKopya"], StoryFlags.MirrorBossDefeated,
                Dialogue("kat1_ayna_giris",
                    "Anlatıcı: Çölün sonunda aşırı tanıdık bir yüz vardı.\n" +
                    "Anlatıcı: Kendisi.\n" +
                    "Ayna Kopya: Ülgen Han seni hiç sevmedi. Ben biliyorum... çünkü ben senim.\n" +
                    "Anlatıcı: Bu sahte kopya kendini çoğaltabiliyor, Börühan'ın korkularına bürünüp onu yavaşlatabiliyor ve vuruşlarını daha güçlü yansıtabiliyordu."),
                Dialogue("kat1_ayna_zafer",
                    "Anlatıcı: Börühan korkularının üstesinden geldi. Kopya bir ayna gibi paramparça oldu ve kristal tozlara dönüştü.\n" +
                    $"Anlatıcı: Börühan kılıcını tozlara saplayarak kalıntının özünü içine aldı. [bayrak: {StoryFlags.MirrorLeap}]\n" +
                    "Anlatıcı: AYNA SIÇRAMASI — Kaçış tuşu (Shift) artık seni kısa bir mesafe ileri ışınlar. Daha uzağa gitmez, ama çukurların üzerinden geçebilirsin.\n" +
                    "Anlatıcı: Kumların ortasında, aşağıya inen bir yarık açıldı."),
                Kat2Intro, null));

            BuildScene(Kat2Intro, () => FloorIntro("2. KAT\nZırhlılar Geçidi", Dialogue("kat2_giris",
                "Anlatıcı: Börühan çölü geçti ve cehennemin ikinci katına indi: Zırhlılar Geçidi.\n" +
                "Anlatıcı: Burası Erlik Han'ın yeraltındaki silahhanesi ve devasa demir atölyesiydi.\n" +
                "Anlatıcı: Havada kömür karası tozlar, dev körüklerin sesi ve kızgın demir kokusu vardı.\n" +
                "Anlatıcı: Zeminde yer yer dipsiz çukurlar açılıyordu. Ayna Sıçraması burada işine yarayacaktı."),
                Kat2, 2, "Zırhlılar Geçidi", "Kat2"));
            BuildScene(Kat2, BuildKat2);
            BuildScene(Kat2Boss, () => BossArena(Kat2Boss, "Temir Han'ın Örsü", 2, "Demirhane", enemies["TemirHan"], StoryFlags.TemirHanDefeated,
                Dialogue("kat2_temir_giris",
                    "Anlatıcı: Sona geldiğinde, önünde kanlarında lav akan, taştan bir dev belirdi. Elinde devasa bir metal balyoz vardı.\n" +
                    "Temir Han: Erlik'in örsüne bir demir daha... Gel, seni de döveyim!\n" +
                    "Anlatıcı: Temir Han yavaştı ama sert vuruyordu. Tek bir darbesi bile neredeyse ölümcüldü."),
                Dialogue("kat2_temir_zafer",
                    "Anlatıcı: Dev parçalandı; içindeki lavlar etrafa aktı.\n" +
                    $"Anlatıcı: Devin ellerinden biri metal bir eldivene dönüştü. Börühan eldiveni aldı. [bayrak: {StoryFlags.IronGauntlet}]\n" +
                    "Anlatıcı: DEMİR ELDİVEN — Ağır saldırın artık düşmanı sersemletir ve çatlak duvarları kırar. Eldiven ağırdır; sersemletme her an kullanılamaz.\n" +
                    "Anlatıcı: Börühan daha da güçlendiğini hissetti, ama hâlâ hazır değildi."),
                Kat3Intro, null));

            BuildScene(Kat3Intro, () => FloorIntro("3. KAT\nHastalık ve Çürüme Diyarı", Dialogue("kat3_giris",
                "Anlatıcı: Börühan yoluna devam etti ve üçüncü kata ulaştı: Hastalık ve Çürüme Diyarı.\n" +
                "Anlatıcı: Zemininde zehirli yeşil sisler, bataklıklar ve havada uçuşan salgın tozları vardı. Nefes almak bile can yakıyordu.\n" +
                "Anlatıcı: Burası, yeryüzüne salgınları ve acıyı yayan Çalış Han'ın diyarıydı.\n" +
                "Anlatıcı: Bataklıklar insanı içine çeker, hava zehirler. Yeşil sislerden uzak dur."),
                Kat3, 3, "Hastalık ve Çürüme Diyarı", "Kat3"));
            BuildScene(Kat3, BuildKat3);
            BuildScene(Kat3Boss, () => BossArena(Kat3Boss, "Çalış Han'ın Çukuru", 3, "Bataklik", enemies["CalisHan"], StoryFlags.CalisHanDefeated,
                Dialogue("kat3_calis_giris",
                    "Anlatıcı: Börühan bataklığın sonunda Çalış Han ile yüzleşti.\n" +
                    "Çalış Han: Nefes al, yiğit... Her nefes seni bana biraz daha yaklaştırır.\n" +
                    "Anlatıcı: Etrafa zehirli gaz bulutları yayıyor, bataklığın derinliklerinden yeni yaratıklar çağırıyordu."),
                Dialogue("kat3_calis_zafer",
                    "Anlatıcı: Ağır darbeler ve uzun bir savaşın sonunda Çalış Han yenildi.\n" +
                    "Anlatıcı: Etraftaki zehirli gaz dağıldı.\n" +
                    $"Anlatıcı: Börühan Çalış Han'ın özünden zehir ve veba bağışıklığı elde etti. Artık zehirler onu etkileyemezdi. [bayrak: {StoryFlags.PoisonImmunity}]\n" +
                    "Anlatıcı: Ve yerin en dibine, son kata inen yol açıldı."),
                Kat9Intro, null));

            BuildScene(Kat9Intro, () => FloorIntro("9. KAT\nErlik'in Tahtı", Dialogue("kat9_giris",
                "Anlatıcı: Ve sonunda Börühan son kata ulaştı.\n" +
                "Anlatıcı: Erlik Han'ın tahtı ve Ülgen Han'ın tutulduğu yer.\n" +
                "Anlatıcı: Yerler simsiyah, soğuk obsidyen taşlarıyla kaplıydı. Çevrede göğü ve yeri tuttuğu söylenen, ama artık paslanmış devasa demir sütunlar uzanıyordu."),
                Kat9, 9, "Erlik'in Tahtı", "Kat9"));
            BuildScene(Kat9, BuildKat9);
            BuildScene(Ending, BuildEnding);

            PatchPrologue();
            RemoveTestRoom();
            AssetDatabase.SaveAssets();
            Debug.Log("[Börühan] Yeraltı oluşturuldu.");
        }

        // ================= Assets =================

        static void EnsureFloorAssets()
        {
            MapKit.CreateSet("Col",
                ("SahteKumTepesi", 16, 16, new Color32(150, 120, 82, 255), Shape.Tile, false, false));
            MapKit.CreateSet("Demirhane",
                ("DemirZemin", 16, 16, new Color32(72, 64, 62, 255), Shape.Tile, false, false),
                ("KomurZemin", 16, 16, new Color32(46, 41, 41, 255), Shape.Tile, false, false),
                ("Kivilcim", 16, 16, new Color32(255, 140, 50, 255), Shape.Circle, false, false),
                ("DemirDuvar", 16, 16, new Color32(40, 36, 38, 255), Shape.Tile, true, false),
                ("DemirDuvarZirve", 16, 32, new Color32(86, 76, 72, 255), Shape.Box, true, true),
                ("Ors", 16, 20, new Color32(62, 62, 72, 255), Shape.Box, true, true),
                ("Cukur", 16, 16, new Color32(8, 6, 6, 255), Shape.Tile, true, false),
                ("LavKanali", 16, 16, new Color32(230, 100, 30, 255), Shape.Tile, true, false));
            MapKit.CreateSet("Bataklik",
                ("BatakZemin", 16, 16, new Color32(62, 74, 50, 255), Shape.Tile, false, false),
                ("CamurZemin", 16, 16, new Color32(78, 70, 52, 255), Shape.Tile, false, false),
                ("Bataklik", 16, 16, new Color32(40, 60, 46, 255), Shape.Tile, false, false),
                ("ZehirliSis", 16, 16, new Color32(120, 175, 70, 255), Shape.Tile, false, false),
                ("CurukDuvar", 16, 16, new Color32(36, 44, 32, 255), Shape.Tile, true, false),
                ("CurukAgac", 16, 40, new Color32(55, 50, 36, 255), Shape.Box, true, true));
            MapKit.CreateSet("Obsidyen",
                ("Obsidyen", 16, 16, new Color32(26, 23, 31, 255), Shape.Tile, false, false),
                ("Obsidyen2", 16, 16, new Color32(34, 29, 40, 255), Shape.Tile, false, false),
                ("Zincir", 16, 16, new Color32(95, 92, 98, 255), Shape.Box, false, false),
                ("ObsidyenDuvar", 16, 16, new Color32(14, 12, 18, 255), Shape.Tile, true, false),
                ("PasliSutun", 24, 56, new Color32(115, 62, 40, 255), Shape.Box, true, true));

            MapKit.Track("Kat2", 110, 147);
            MapKit.Track("Kat3", 92, 139);
            MapKit.Track("Kat9", 73, 110);
            MapKit.Track("Boss", 98, 156);
            MapKit.Track("Son", 131, 175);

            MapKit.Theme("Demirhane", MapKit.GetTile("Demirhane", "DemirZemin"), MapKit.GetTile("Demirhane", "DemirDuvar"),
                new TileBase[] { MapKit.GetTile("Demirhane", "DemirDuvarZirve") }, 5,
                new Color(0.05f, 0.03f, 0.03f), new Color(1f, 0.62f, 0.42f), 0.45f, 6f, MapKit.GetTrack("Kat2"));
            MapKit.Theme("Bataklik", MapKit.GetTile("Bataklik", "BatakZemin"), MapKit.GetTile("Bataklik", "CurukDuvar"),
                new TileBase[] { MapKit.GetTile("Bataklik", "CurukAgac") }, 5,
                new Color(0.03f, 0.05f, 0.03f), new Color(0.65f, 0.9f, 0.55f), 0.5f, 6f, MapKit.GetTrack("Kat3"));
            MapKit.Theme("Obsidyen", MapKit.GetTile("Obsidyen", "Obsidyen"), MapKit.GetTile("Obsidyen", "ObsidyenDuvar"),
                new TileBase[] { MapKit.GetTile("Obsidyen", "PasliSutun") }, 5,
                new Color(0.02f, 0.01f, 0.03f), new Color(0.65f, 0.6f, 0.95f), 0.3f, 6f, MapKit.GetTrack("Kat9"));
        }

        static void EnsureSpeakers()
        {
            speakers = new Dictionary<string, DialogueSpeaker>();
            Speaker("AynaKopya", "Ayna Kopya", new Color32(200, 225, 245, 255));
            Speaker("TemirHan", "Temir Han", new Color32(240, 120, 60, 255));
            Speaker("CalisHan", "Çalış Han", new Color32(140, 200, 90, 255));
            Speaker("ErlikHan", "Erlik Han", new Color32(200, 60, 70, 255));
            Speaker("PismanRuh", "Pişman Ruh", new Color32(150, 190, 230, 255));
            Speaker("YorgunKole", "Yorgun Köle", new Color32(190, 160, 130, 255));
        }

        static void Speaker(string file, string display, Color color)
        {
            var path = $"Assets/Dialogue/Konusmacilar/{file}.asset";
            var speaker = AssetDatabase.LoadAssetAtPath<DialogueSpeaker>(path);
            if (speaker == null)
            {
                speaker = ScriptableObject.CreateInstance<DialogueSpeaker>();
                speaker.displayName = display;
                speaker.nameColor = color;
                AssetDatabase.CreateAsset(speaker, path);
            }
            speakers[file] = speaker;
        }

        static DialogueAsset Dialogue(string file, string script)
        {
            Directory.CreateDirectory(DialogueDir);
            var path = $"{DialogueDir}/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
            if (existing != null)
                return existing;
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            asset.lines = DialogueAssetEditor.Parse(script, out var unknown);
            if (unknown.Count > 0)
                Debug.LogWarning($"[Börühan] {file}: bilinmeyen konuşmacı {string.Join(", ", unknown)}");
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ================= Enemies of the lower floors =================

        static Sprite Art(string name, int w, int h, Color color, Shape shape) => CombatKit.EnemyArt(name, w, h, color, shape);

        static void EnsureFloorEnemies()
        {
            var orange = new Color(1f, 0.55f, 0.2f);
            var poison = new Color(0.55f, 0.85f, 0.3f);

            enemies["KorCini"] = CombatKit.Enemy("KorCini", d =>
            {
                d.displayName = "Kor Cini"; d.difficulty = Difficulty.Kolay;
                d.description = "Körüklerin ateşinden doğan küçük bir cin. Uzaktan kor fırlatır.";
                d.sprite = Art("KorCini", 12, 14, new Color32(255, 130, 50, 255), Shape.Circle);
                d.maxHealth = 40f; d.moveSpeed = 2.4f; d.aggroRange = 7f; d.preferredDistance = 4.5f;
            }, d =>
            {
                CombatKit.Add<ProjectileAbility>(d, "Kor", a => { a.damage = 9f; a.count = 2; a.spreadAngle = 20f; a.speed = 7f; a.color = orange; a.windup = 0.5f; a.cooldown = 2f; a.minRange = 2f; a.maxRange = 8f; });
                CombatKit.Add<TeleportAbility>(d, "Kıvılcım Kaçışı", a => { a.destination = TeleportAbility.Destination.AwayFromPlayer; a.distance = 3f; a.windup = 0.2f; a.cooldown = 4f; a.maxRange = 2.5f; });
            });

            enemies["DemirMuhafiz"] = CombatKit.Enemy("DemirMuhafiz", d =>
            {
                d.displayName = "Demir Muhafız"; d.difficulty = Difficulty.Orta;
                d.description = "Erlik'in silahhanesini koruyan zırhlı asker. Kalkanını ağır saldırıyla kır.";
                d.sprite = Art("DemirMuhafiz", 14, 24, new Color32(110, 110, 125, 255), Shape.Figure);
                d.maxHealth = 95f; d.moveSpeed = 2.2f; d.poise = 30f; d.knockbackResistance = 0.5f;
                d.aggroRange = 6f; d.preferredDistance = 1.5f;
            }, d =>
            {
                CombatKit.Add<MeleeStrikeAbility>(d, "Mızrak Darbesi", a => { a.damage = 12f; a.hits = 2; a.reach = 1f; a.radius = 0.8f; a.windup = 0.5f; a.cooldown = 2f; a.maxRange = 2f; });
                CombatKit.Add<GuardAbility>(d, "Demir Kalkan", a => { a.duration = 2.5f; a.damageTaken = 0.2f; a.windup = 0.25f; a.cooldown = 6f; a.maxRange = 5f; });
                CombatKit.Add<LungeAbility>(d, "Hamle", a => { a.damage = 14f; a.distance = 4f; a.speed = 11f; a.windup = 0.6f; a.cooldown = 4.5f; a.minRange = 2.5f; a.maxRange = 5.5f; });
            });

            enemies["ZincirliMahkum"] = CombatKit.Enemy("ZincirliMahkum", d =>
            {
                d.displayName = "Zincirli Mahkûm"; d.difficulty = Difficulty.Orta;
                d.description = "Örslere zincirlenmiş bir ruh. Zincirlerini savurarak çevresini döver.";
                d.sprite = Art("ZincirliMahkum", 12, 22, new Color32(140, 120, 100, 255), Shape.Figure);
                d.maxHealth = 80f; d.moveSpeed = 2.6f; d.poise = 20f; d.contactDamage = 4f;
                d.aggroRange = 6f; d.preferredDistance = 2f;
            }, d =>
            {
                CombatKit.Add<AreaSlamAbility>(d, "Zincir Savurma", a => { a.damage = 13f; a.radius = 2.2f; a.windup = 0.6f; a.recovery = 0.6f; a.cooldown = 3.5f; a.maxRange = 2.5f; });
                CombatKit.Add<LungeAbility>(d, "Zincir Atılışı", a => { a.damage = 12f; a.distance = 5f; a.speed = 12f; a.windup = 0.6f; a.cooldown = 3.5f; a.minRange = 2.5f; a.maxRange = 6f; });
            });

            enemies["OrsBekcisi"] = CombatKit.Enemy("OrsBekcisi", d =>
            {
                d.displayName = "Örs Bekçisi"; d.difficulty = Difficulty.Zor;
                d.description = "Dev bir çekiçle örsleri bekleyen demir yığını. Yere vuruşu dalga dalga yayılır.";
                d.sprite = Art("OrsBekcisi", 24, 30, new Color32(80, 78, 90, 255), Shape.Box);
                d.scale = 1.35f; d.footSize = new Vector2(1.2f, 0.5f);
                d.maxHealth = 210f; d.moveSpeed = 1.4f; d.contactDamage = 8f; d.poise = 55f; d.knockbackResistance = 0.85f;
                d.aggroRange = 6f; d.preferredDistance = 1.8f; d.strafe = false;
            }, d =>
            {
                CombatKit.Add<AreaSlamAbility>(d, "Örs Dalgası", a => { a.damage = 20f; a.radius = 1.8f; a.waves = 3; a.waveGrowth = 1.45f; a.windup = 0.9f; a.recovery = 0.8f; a.cooldown = 5f; a.maxRange = 2.8f; });
                CombatKit.Add<MeleeStrikeAbility>(d, "Çekiç", a => { a.damage = 26f; a.reach = 1.3f; a.radius = 1.2f; a.windup = 1f; a.recovery = 0.9f; a.cooldown = 3f; a.maxRange = 2.2f; });
                CombatKit.Add<GuardAbility>(d, "Zırh", a => { a.duration = 3f; a.damageTaken = 0.15f; a.cooldown = 8f; a.maxRange = 6f; });
            });

            enemies["TemirHan"] = CombatKit.Enemy("TemirHan", d =>
            {
                d.displayName = "Temir Han"; d.difficulty = Difficulty.Zor; d.isBoss = true;
                d.description = "Kanlarında lav akan taştan dev. Yavaştır ama sert vurur; tek darbesi neredeyse ölümcüldür.";
                d.sprite = Art("TemirHan", 28, 36, new Color32(95, 70, 60, 255), Shape.Figure);
                d.scale = 1.8f; d.footSize = new Vector2(1.1f, 0.45f);
                d.maxHealth = 520f; d.moveSpeed = 1.3f; d.contactDamage = 10f; d.poise = 120f; d.knockbackResistance = 0.95f;
                d.aggroRange = 6.5f; d.preferredDistance = 2.2f; d.strafe = false;
            }, d =>
            {
                CombatKit.Add<MeleeStrikeAbility>(d, "Balyoz", a => { a.damage = 32f; a.reach = 1.6f; a.radius = 1.5f; a.windup = 1.05f; a.recovery = 1f; a.cooldown = 2.5f; a.maxRange = 3f; a.weight = 1.5f; });
                CombatKit.Add<MeleeStrikeAbility>(d, "Balyoz Kombosu", a => { a.damage = 22f; a.hits = 2; a.reach = 1.5f; a.radius = 1.4f; a.windup = 0.9f; a.timeBetweenHits = 0.75f; a.recovery = 1f; a.cooldown = 5f; a.maxRange = 3f; });
                CombatKit.Add<AreaSlamAbility>(d, "Lav Dalgası", a => { a.damage = 22f; a.radius = 2.2f; a.waves = 3; a.waveGrowth = 1.5f; a.windup = 1f; a.recovery = 1f; a.cooldown = 6f; a.maxRange = 4f; a.telegraphColor = new Color(1f, 0.45f, 0.1f, 0.35f); });
                CombatKit.Add<LungeAbility>(d, "Dev Hücumu", a => { a.damage = 26f; a.distance = 6f; a.speed = 9f; a.hitRadius = 1.2f; a.windup = 1.1f; a.recovery = 1.1f; a.cooldown = 6f; a.minRange = 3.5f; a.maxRange = 9f; });
                CombatKit.Add<ProjectileAbility>(d, "Kor Yağmuru", a => { a.damage = 10f; a.count = 7; a.spreadAngle = 120f; a.speed = 5.5f; a.size = 0.45f; a.color = orange; a.windup = 0.8f; a.cooldown = 7f; a.minRange = 3f; a.maxRange = 10f; });
                CombatKit.Add<SummonMinionsAbility>(d, "Körük Çağrısı", a => { a.minions.Add(enemies["KorCini"]); a.count = 2; a.maxAlive = 3; a.windup = 0.8f; a.cooldown = 15f; a.maxRange = 12f; });
            });

            enemies["VebaSinegi"] = CombatKit.Enemy("VebaSinegi", d =>
            {
                d.displayName = "Veba Sineği"; d.difficulty = Difficulty.Kolay;
                d.description = "Salgın taşıyan iri bir sinek. Hızlıdır, sık sık üzerine atılır.";
                d.sprite = Art("VebaSinegi", 10, 10, new Color32(110, 140, 60, 255), Shape.Circle);
                d.maxHealth = 22f; d.moveSpeed = 3.6f; d.contactDamage = 4f; d.aggroRange = 7f; d.preferredDistance = 2.2f;
            }, d =>
            {
                CombatKit.Add<LungeAbility>(d, "Vızıltı", a => { a.damage = 7f; a.distance = 3f; a.speed = 13f; a.windup = 0.4f; a.recovery = 0.4f; a.cooldown = 1.4f; a.minRange = 1f; a.maxRange = 4f; });
            });

            enemies["BatakSulugu"] = CombatKit.Enemy("BatakSulugu", d =>
            {
                d.displayName = "Batak Sülüğü"; d.difficulty = Difficulty.Kolay;
                d.description = "Bataklığın dibinde bekler; yanında belirip ısırır.";
                d.sprite = Art("BatakSulugu", 14, 10, new Color32(60, 50, 40, 255), Shape.Box);
                d.maxHealth = 45f; d.moveSpeed = 1.8f; d.aggroRange = 6f; d.preferredDistance = 1.2f;
            }, d =>
            {
                var bite = CombatKit.Add<MeleeStrikeAbility>(d, "Isırık", a => { a.damage = 11f; a.reach = 0.7f; a.radius = 0.7f; a.windup = 0.4f; a.recovery = 0.6f; a.cooldown = 1.8f; a.maxRange = 1.6f; });
                CombatKit.Add<TeleportAbility>(d, "Dipten Çıkış", a => { a.destination = TeleportAbility.Destination.NearPlayer; a.distance = 1.3f; a.followUp = bite; a.windup = 0.5f; a.cooldown = 5f; a.minRange = 2.5f; a.maxRange = 8f; });
            });

            enemies["CurukYaratik"] = CombatKit.Enemy("CurukYaratik", d =>
            {
                d.displayName = "Çürük Yaratık"; d.difficulty = Difficulty.Orta;
                d.description = "Çürümüş etten bir yığın. Vurdukça etrafına zehirli sis bırakır.";
                d.sprite = Art("CurukYaratik", 18, 20, new Color32(90, 110, 60, 255), Shape.Box);
                d.scale = 1.15f; d.maxHealth = 100f; d.moveSpeed = 1.8f; d.contactDamage = 6f; d.poise = 25f; d.knockbackResistance = 0.4f;
                d.aggroRange = 6f; d.preferredDistance = 1.6f;
            }, d =>
            {
                CombatKit.Add<MeleeStrikeAbility>(d, "Çürük Pençe", a => { a.damage = 13f; a.hits = 2; a.reach = 0.9f; a.radius = 0.9f; a.windup = 0.55f; a.cooldown = 2.2f; a.maxRange = 2f; });
                CombatKit.Add<HazardCloudAbility>(d, "Zehirli Sis", a => { a.count = 1; a.radius = 1.6f; a.lifetime = 5f; a.damagePerSecond = 8f; a.windup = 0.6f; a.cooldown = 6f; a.maxRange = 6f; a.cloudColor = new Color(poison.r, poison.g, poison.b, 0.4f); });
            });

            enemies["VebaKami"] = CombatKit.Enemy("VebaKami", d =>
            {
                d.displayName = "Veba Kamı"; d.difficulty = Difficulty.Zor;
                d.description = "Çalış Han'a tapan şaman. Sinekleri çağırır, zehir fırlatır, korku salar.";
                d.sprite = Art("VebaKami", 12, 24, new Color32(80, 100, 50, 255), Shape.Figure);
                d.maxHealth = 150f; d.moveSpeed = 2.2f; d.poise = 35f; d.aggroRange = 7.5f; d.preferredDistance = 5f;
            }, d =>
            {
                CombatKit.Add<ProjectileAbility>(d, "Zehir Yelpazesi", a => { a.damage = 9f; a.count = 4; a.spreadAngle = 50f; a.speed = 6.5f; a.color = poison; a.windup = 0.6f; a.cooldown = 2.4f; a.minRange = 2f; a.maxRange = 9f; });
                CombatKit.Add<SummonMinionsAbility>(d, "Sürü", a => { a.minions.Add(enemies["VebaSinegi"]); a.count = 3; a.maxAlive = 4; a.windup = 0.8f; a.cooldown = 10f; a.maxRange = 10f; });
                CombatKit.Add<CurseAbility>(d, "Veba Laneti", a => { a.range = 6f; a.slowMultiplier = 0.55f; a.slowDuration = 2.5f; a.damage = 6f; a.windup = 0.8f; a.cooldown = 9f; a.maxRange = 6f; a.telegraphColor = new Color(0.5f, 0.8f, 0.2f, 0.2f); });
                CombatKit.Add<HazardCloudAbility>(d, "Salgın", a => { a.count = 2; a.windup = 0.7f; a.cooldown = 7f; a.maxRange = 9f; a.cloudColor = new Color(poison.r, poison.g, poison.b, 0.4f); });
            });

            enemies["CalisHan"] = CombatKit.Enemy("CalisHan", d =>
            {
                d.displayName = "Çalış Han"; d.difficulty = Difficulty.Zor; d.isBoss = true;
                d.description = "Yeryüzüne salgın ve acı yayan hastalığın efendisi.";
                d.sprite = Art("CalisHan", 22, 32, new Color32(110, 140, 70, 255), Shape.Figure);
                d.scale = 1.6f; d.footSize = new Vector2(1f, 0.45f);
                d.maxHealth = 560f; d.moveSpeed = 2f; d.poise = 90f; d.knockbackResistance = 0.9f;
                d.aggroRange = 6.5f; d.preferredDistance = 4f;
            }, d =>
            {
                CombatKit.Add<HazardCloudAbility>(d, "Zehirli Gaz", a => { a.count = 3; a.radius = 1.8f; a.spread = 4f; a.lifetime = 7f; a.damagePerSecond = 10f; a.windup = 0.7f; a.cooldown = 5f; a.maxRange = 12f; a.weight = 1.5f; a.cloudColor = new Color(poison.r, poison.g, poison.b, 0.4f); });
                CombatKit.Add<SummonMinionsAbility>(d, "Bataklığın Çocukları", a => { a.minions.Add(enemies["VebaSinegi"]); a.minions.Add(enemies["BatakSulugu"]); a.count = 3; a.maxAlive = 4; a.windup = 0.9f; a.cooldown = 12f; a.maxRange = 12f; });
                CombatKit.Add<ProjectileAbility>(d, "Veba Yağmuru", a => { a.damage = 10f; a.count = 5; a.spreadAngle = 70f; a.bursts = 2; a.burstInterval = 0.4f; a.speed = 6.5f; a.color = poison; a.windup = 0.7f; a.cooldown = 4f; a.minRange = 2f; a.maxRange = 11f; });
                CombatKit.Add<AreaSlamAbility>(d, "Çürüme Dalgası", a => { a.damage = 18f; a.radius = 2.4f; a.waves = 2; a.windup = 0.8f; a.cooldown = 5f; a.maxRange = 3f; a.telegraphColor = new Color(0.5f, 0.8f, 0.2f, 0.35f); });
                CombatKit.Add<CurseAbility>(d, "Ağır Hava", a => { a.range = 8f; a.slowMultiplier = 0.5f; a.slowDuration = 3f; a.damage = 8f; a.windup = 0.8f; a.cooldown = 11f; a.maxRange = 8f; a.telegraphColor = new Color(0.5f, 0.8f, 0.2f, 0.2f); });
            });

            enemies["ErlikHan"] = CombatKit.Enemy("ErlikHan", d =>
            {
                d.displayName = "Erlik Han"; d.difficulty = Difficulty.Zor; d.isBoss = true;
                d.description = "Yeraltının, ölümün ve karanlığın efendisi. Salgın yayar, illüzyonlarla zihni bunaltır, yeni yaratıklar yapar.";
                d.sprite = Art("ErlikHan", 22, 34, new Color32(120, 30, 45, 255), Shape.Figure);
                d.scale = 1.7f; d.footSize = new Vector2(1f, 0.45f);
                d.maxHealth = 950f; d.moveSpeed = 2.6f; d.poise = 140f; d.knockbackResistance = 0.95f;
                d.aggroRange = 8f; d.preferredDistance = 2.8f;
            }, d =>
            {
                CombatKit.Add<MeleeStrikeAbility>(d, "Karanlık Kılıç", a => { a.damage = 18f; a.hits = 3; a.reach = 1.3f; a.radius = 1.2f; a.timeBetweenHits = 0.35f; a.windup = 0.55f; a.recovery = 0.7f; a.cooldown = 2.6f; a.maxRange = 2.6f; a.weight = 1.5f; });
                CombatKit.Add<ProjectileAbility>(d, "Karanlık Küreler", a => { a.damage = 12f; a.count = 6; a.spreadAngle = 90f; a.bursts = 3; a.burstInterval = 0.35f; a.speed = 6f; a.size = 0.45f; a.color = new Color(0.7f, 0.2f, 0.5f); a.windup = 0.7f; a.cooldown = 4.5f; a.minRange = 3f; a.maxRange = 12f; });
                CombatKit.Add<HazardCloudAbility>(d, "Salgın", a => { a.count = 3; a.radius = 1.8f; a.spread = 4f; a.lifetime = 7f; a.damagePerSecond = 10f; a.windup = 0.7f; a.cooldown = 8f; a.maxRange = 12f; a.cloudColor = new Color(0.55f, 0.85f, 0.3f, 0.4f); });
                CombatKit.Add<SummonIllusionsAbility>(d, "Yanılsama", a => { a.count = 3; a.radius = 3.5f; a.lifetime = 9f; a.windup = 0.8f; a.cooldown = 16f; a.maxRange = 12f; });
                CombatKit.Add<SummonMinionsAbility>(d, "Yeni Yaratıklar", a => { a.minions.Add(enemies["HainGolge"]); a.minions.Add(enemies["KorCini"]); a.minions.Add(enemies["VebaSinegi"]); a.count = 2; a.maxAlive = 3; a.windup = 0.9f; a.cooldown = 14f; a.maxRange = 12f; });
                CombatKit.Add<CurseAbility>(d, "Dehşet", a => { a.range = 9f; a.slowMultiplier = 0.5f; a.slowDuration = 3f; a.damage = 10f; a.windup = 0.9f; a.cooldown = 12f; a.maxRange = 9f; a.telegraphColor = new Color(0.4f, 0.1f, 0.3f, 0.25f); });
                CombatKit.Add<AreaSlamAbility>(d, "Taht Sarsıntısı", a => { a.damage = 22f; a.radius = 2.5f; a.waves = 3; a.waveGrowth = 1.4f; a.windup = 0.9f; a.cooldown = 6f; a.maxRange = 3f; });
                var cut = CombatKit.Add<MeleeStrikeAbility>(d, "Gölgeden Kesik", a => { a.damage = 20f; a.reach = 1.2f; a.radius = 1.1f; a.windup = 0.4f; a.recovery = 0.6f; }, inMoveset: false);
                CombatKit.Add<TeleportAbility>(d, "Gölge Adımı", a => { a.destination = TeleportAbility.Destination.BehindPlayer; a.distance = 1.6f; a.followUp = cut; a.windup = 0.35f; a.cooldown = 7f; a.minRange = 4f; a.maxRange = 14f; });
            });
            AssetDatabase.SaveAssets();
        }

        // ================= Map layout =================

        /// <summary>A grid of map cells, all wall to begin with; rooms and paths are carved into it.</summary>
        class MapGrid
        {
            public const char Wall = '#', Floor = '.', Pit = 'O', Fake = 'F', Bog = 'B', Poison = 'P', Pillar = 'C';
            public readonly int Width, Height;
            readonly char[,] cells;

            public MapGrid(int width, int height)
            {
                Width = width;
                Height = height;
                cells = new char[width, height];
                for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = Wall;
            }

            public Rect WorldRect => new Rect(-Width / 2, -Height / 2, Width, Height);
            public char this[int x, int y] => x < 0 || y < 0 || x >= Width || y >= Height ? Wall : cells[x, y];
            public Vector3Int ToTile(int x, int y) => new Vector3Int(x - Width / 2, y - Height / 2, 0);

            /// <summary>Fills a rectangle given by its world-space center and size.</summary>
            public void Rect(float cx, float cy, float w, float h, char c = Floor)
            {
                int x0 = Mathf.FloorToInt(cx - w / 2f) + Width / 2, y0 = Mathf.FloorToInt(cy - h / 2f) + Height / 2;
                for (int x = x0; x < x0 + Mathf.RoundToInt(w); x++)
                for (int y = y0; y < y0 + Mathf.RoundToInt(h); y++)
                {
                    if (x > 0 && y > 0 && x < Width - 1 && y < Height - 1)
                        cells[x, y] = c;
                }
            }

            /// <summary>An L-shaped corridor: across from a, then up/down to b.</summary>
            public void Path(Vector2 a, Vector2 b, float width = 4f)
            {
                Rect((a.x + b.x) / 2f, a.y, Mathf.Abs(b.x - a.x) + width, width);
                Rect(b.x, (a.y + b.y) / 2f, width, Mathf.Abs(b.y - a.y) + width);
            }
        }

        class TileSet
        {
            public TileBase floor, floorAlt, decor, wallFill, wallEdge, pillar, pit, fake, bog, poison;
        }

        /// <summary>Paints the grid into Harita/Zemin, Dekor, Engeller (+ Cukurlar for pits) and adds bog/poison zones.</summary>
        static void PaintMap(MapGrid grid, TileSet t, int seed)
        {
            var root = new GameObject("Harita", typeof(Grid)).GetComponent<Grid>();
            var floor = Layer(root, MapKit.LayerFloor, -100, false, false);
            var decor = Layer(root, MapKit.LayerDecor, -90, false, false);
            var solid = Layer(root, MapKit.LayerSolid, 0, true, false);
            Tilemap pits = null;

            var rng = new System.Random(seed);
            const int ring = 7;
            for (int x = -ring; x < grid.Width + ring; x++)
            for (int y = -ring; y < grid.Height + ring; y++)
            {
                var cell = grid.ToTile(x, y);
                char c = grid[x, y];
                floor.SetTile(cell, rng.NextDouble() < 0.15 ? t.floorAlt : t.floor);

                switch (c)
                {
                    case MapGrid.Wall:
                        solid.SetTile(cell, NextToOpen(grid, x, y) && t.wallEdge != null ? t.wallEdge : t.wallFill);
                        break;
                    case MapGrid.Pillar:
                        solid.SetTile(cell, t.pillar);
                        break;
                    case MapGrid.Pit:
                        if (pits == null)
                        {
                            pits = Layer(root, "Cukurlar", -95, true, false);
                            pits.gameObject.AddComponent<BlinkPassable>();
                        }
                        pits.SetTile(cell, t.pit);
                        break;
                    case MapGrid.Fake:
                        // Looks like a wall, but isn't one: the desert lies.
                        decor.SetTile(cell, t.fake);
                        break;
                    case MapGrid.Bog:
                        decor.SetTile(cell, t.bog);
                        break;
                    case MapGrid.Poison:
                        decor.SetTile(cell, t.poison);
                        break;
                    default:
                        if (t.decor != null && rng.NextDouble() < 0.04)
                            decor.SetTile(cell, t.decor);
                        break;
                }
            }

            MapKit.RebuildCollision(solid);
            if (pits != null)
                MapKit.RebuildCollision(pits);

            Zones(grid, MapGrid.Bog, "Bataklik", 0f, 0.5f, null, new Color(0.25f, 0.4f, 0.25f, 0f));
            Zones(grid, MapGrid.Poison, "ZehirliSis", 8f, 0.85f, StoryFlags.PoisonImmunity, new Color(0.5f, 0.85f, 0.3f, 0.18f));
        }

        static bool NextToOpen(MapGrid grid, int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                char n = grid[x + dx, y + dy];
                if (n != MapGrid.Wall && n != MapGrid.Fake)
                    return true;
            }
            return false;
        }

        static Tilemap Layer(Grid root, string name, int order, bool solid, bool unused)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(root.transform, false);
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = solid && name == MapKit.LayerSolid ? TilemapRenderer.Mode.Individual : TilemapRenderer.Mode.Chunk;
            if (solid)
            {
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                go.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
                go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            }
            return go.GetComponent<Tilemap>();
        }

        /// <summary>Joins horizontal runs of a hazard cell into trigger zones.</summary>
        static void Zones(MapGrid grid, char kind, string name, float dps, float slow, string immunity, Color tint)
        {
            Transform parent = null;
            for (int y = 0; y < grid.Height; y++)
            {
                int x = 0;
                while (x < grid.Width)
                {
                    if (grid[x, y] != kind)
                    {
                        x++;
                        continue;
                    }
                    int start = x;
                    while (x < grid.Width && grid[x, y] == kind)
                        x++;

                    if (parent == null)
                        parent = new GameObject(name + "Alanlari").transform;
                    var cell = grid.ToTile(start, y);
                    var go = new GameObject(name);
                    go.transform.SetParent(parent, false);
                    go.transform.position = new Vector3(cell.x + (x - start) / 2f, cell.y + 0.5f, 0f);
                    var box = go.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.size = new Vector2(x - start, 1f);
                    go.AddComponent<HazardZone>().Configure(dps, slow, immunity, 0f);
                }
            }
        }

        // ================= Placing things =================

        class Floor
        {
            public Transform world, enemyRoot;
            public RoomUtility utility;
            public string scene;
        }

        static Floor StartFloor(string scene, string location, int floorNumber, MapGrid grid, RoomTheme theme, Vector2 spawn)
        {
            var prefabs = PrologueBuilder.CreatePrefabs();
            var ctx = PrologueBuilder.GameplayScene(prefabs, location, grid.WorldRect, spawn, theme.background, theme.lightIntensity, theme.lightColor, theme.cameraSize);
            var manager = SceneManager.GetActiveScene().GetRootGameObjects().Select(r => r.GetComponent<RoomManager>()).First(m => m != null);
            PrologueBuilder.Set(manager, "floor", floorNumber);
            PrologueBuilder.Set(ctx.camera, "bounds", grid.WorldRect);
            PrologueBuilder.Spawn(ctx.world, "giris", spawn);
            MapKit.AddSceneMusic(theme.music);
            return new Floor
            {
                world = ctx.world,
                enemyRoot = new GameObject("Dusmanlar").transform,
                utility = new GameObject("OdaAraclari").AddComponent<RoomUtility>(),
                scene = scene,
            };
        }

        static void Enemy(Floor f, string kind, float x, float y) => CombatKit.PlaceEnemy(enemies[kind], new Vector2(x, y), f.enemyRoot);

        static GameObject Prop(Floor f, string name, Sprite sprite, float x, float y, float footprint = 0.5f) =>
            PrologueBuilder.Prop(f.world, name, sprite, new Vector2(x, y), footprint);

        static void Glow(GameObject go, Color color, float intensity, float radius)
        {
            var light = new GameObject("Isik").AddComponent<Light2D>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            PrologueBuilder.PointLight(light, color, intensity, radius);
        }

        /// <summary>A lore tablet. Reading it counts toward "Yazıt" in the pause menu.</summary>
        static void Lore(Floor f, string id, float x, float y, string script)
        {
            var go = Prop(f, "Yazit", PrologueBuilder.Art("Yazit", 12, 16, new Color32(125, 118, 108, 255), Shape.Box), x, y, 0.5f);
            Glow(go, new Color(1f, 0.85f, 0.5f), 0.5f, 2f);
            PrologueBuilder.Interact(go, "Oku", Dialogue($"{f.scene}_yazit_{id}", script), completedFlag: "yazit_" + f.scene + "_" + id);
        }

        /// <summary>A lost soul who talks. Also counts as a Yazıt.</summary>
        static void Soul(Floor f, string id, float x, float y, string script)
        {
            var go = Prop(f, "Ruh", PrologueBuilder.Art("Ruh", 12, 22, new Color32(150, 190, 230, 150), Shape.Figure), x, y, 0.4f);
            Glow(go, new Color(0.6f, 0.8f, 1f), 0.6f, 2.5f);
            PrologueBuilder.Interact(go, "Konuş", Dialogue($"{f.scene}_ruh_{id}", script), completedFlag: "yazit_" + f.scene + "_" + id);
        }

        /// <summary>Kut Ocağı: rest to heal, save and set the respawn point.</summary>
        static void Shrine(Floor f, string id, float x, float y)
        {
            var go = Prop(f, "KutOcagi", PrologueBuilder.Art("KutOcagi", 16, 18, new Color32(170, 110, 60, 255), Shape.Box), x, y, 0.6f);
            Glow(go, new Color(1f, 0.6f, 0.25f), 1.1f, 4.5f);
            PrologueBuilder.Interact(go, "Dinlen", Dialogue("kut_ocagi", "Anlatıcı: Kut Ocağı'nın ateşi Börühan'ı sardı. Yaraları kapandı ve yolu kaydedildi."));
            UnityEventTools.AddPersistentListener(go.GetComponent<Interactable>().onFinished, f.utility.RestAtShrine);
            PrologueBuilder.Spawn(f.world, $"ocak_{id}", new Vector2(x, y - 1.3f));
        }

        static void Item(Floor f, PickupKind kind, string id, float x, float y)
        {
            var (name, color, size) = kind switch
            {
                PickupKind.CanParcasi => ("CanParcasi", new Color32(230, 60, 70, 255), 10),
                PickupKind.KutParcasi => ("KutParcasi", new Color32(255, 160, 50, 255), 10),
                _ => ("SifaOtu", new Color32(110, 210, 110, 255), 8),
            };
            var go = new GameObject(name);
            go.transform.SetParent(f.world, false);
            go.transform.position = new Vector2(x, y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PrologueBuilder.Art(PrologueBuilder.ArtDir, name, size, size, color, Shape.Circle, new Vector2(0.5f, 0.5f));
            sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            var pickup = go.AddComponent<Pickup>();
            PrologueBuilder.Set(pickup, "kind", (int)kind);
            PrologueBuilder.Set(pickup, "id", $"{f.scene}_{id}");
            Glow(go, color, 0.8f, 2.2f);
        }

        static void Crack(Floor f, string id, float x, float y)
        {
            // 2x2 so it fully plugs a 2-wide passage.
            var go = Prop(f, "CatlakDuvar", PrologueBuilder.Art("CatlakDuvar", 32, 32, new Color32(95, 88, 80, 255), Shape.Steps), x, y, 2f);
            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 2f);
            box.offset = new Vector2(0f, 1f);
            go.AddComponent<Health>();
            var wall = go.AddComponent<BreakableWall>();
            PrologueBuilder.Set(wall, "id", $"{f.scene}_{id}");
        }

        /// <summary>The way down: a glowing gate (or rift) that leads to the next scene.</summary>
        static GameObject Gate(Floor f, string name, float x, float y, string scene, string spawn)
        {
            var go = Prop(f, name, PrologueBuilder.Art("KatKapisi", 32, 40, new Color32(90, 30, 25, 255), Shape.Box), x, y, 0f);
            Glow(go, new Color(1f, 0.35f, 0.15f), 1.4f, 5f);
            PrologueBuilder.Door(go.transform, "Gecit", new Vector2(x, y - 0.2f), new Vector2(2f, 0.8f), scene, spawn);
            return go;
        }

        static void Trigger(Floor f, string id, float x, float y, float w, float h, string script) =>
            PrologueBuilder.Trigger(f.world, "HikayeTetik", new Vector2(x, y), new Vector2(w, h), Dialogue($"{f.scene}_{id}", script), null, $"{f.scene}_{id}");

        // ================= Kat 1: Yalanlar ve Sırlar Çölü =================

        static void BuildKat1()
        {
            var g = new MapGrid(96, 78);
            g.Rect(0, -30, 18, 12);      // Giriş
            g.Rect(0, -12, 30, 16);      // Fısıltı Tepeleri
            g.Rect(-32, -20, 20, 14);    // Batı Vaha
            g.Rect(32, -18, 16, 22);     // Kristal Kanyon
            g.Rect(-32, 8, 22, 14);      // Kervan Yolu
            g.Rect(30, 10, 24, 18);      // Serap Labirenti
            g.Rect(0, 8, 12, 8);         // Mühürlü Mahzen
            g.Rect(0, 28, 22, 12);       // Kuzey Kapısı

            g.Path(new Vector2(0, -30), new Vector2(0, -12));
            g.Path(new Vector2(0, -12), new Vector2(-32, -20));
            g.Path(new Vector2(0, -14), new Vector2(32, -18));
            g.Path(new Vector2(-32, -20), new Vector2(-32, 8));
            g.Path(new Vector2(32, -18), new Vector2(32, 10));
            g.Path(new Vector2(-32, 17), new Vector2(-8, 28));
            g.Path(new Vector2(30, 21), new Vector2(8, 28));
            g.Path(new Vector2(0, -4), new Vector2(0, 8), 2f);              // narrow way to the vault

            // Secret shortcut from the entrance to the oasis, hidden behind walls that lie.
            g.Path(new Vector2(-9, -32), new Vector2(-26, -24), 2f);
            g.Rect(-11, -32, 3, 2, MapGrid.Fake);
            // Hidden chamber inside the mirage labyrinth.
            g.Rect(39, 15, 8, 8, MapGrid.Wall);
            g.Rect(39, 15, 6, 6);
            g.Rect(35.5f, 15, 1, 2, MapGrid.Fake);
            // Crystal pillars to fight around.
            foreach (var p in new[] { new Vector2(-6, -10), new Vector2(6, -14), new Vector2(-12, -8), new Vector2(12, -9), new Vector2(29, -22), new Vector2(35, -14),
                         new Vector2(29, -10), new Vector2(-38, 10), new Vector2(-26, 5), new Vector2(24, 4), new Vector2(26, 14), new Vector2(-8, 30), new Vector2(8, 30) })
                g.Rect(p.x, p.y, 1, 1, MapGrid.Pillar);

            var theme = MapKit.GetTheme("Col");
            var f = StartFloor(Kat1, "Yalanlar ve Sırlar Çölü", 1, g, theme, new Vector2(0, -33));
            PaintMap(g, new TileSet
            {
                floor = MapKit.GetTile("Col", "KristalKum"), floorAlt = MapKit.GetTile("Col", "KristalKum2"), decor = MapKit.GetTile("Col", "KirikKristal"),
                wallFill = MapKit.GetTile("Col", "KumTepesi"), wallEdge = MapKit.GetTile("Col", "KumTepesiZirve"), pillar = MapKit.GetTile("Col", "KristalSutun"),
                fake = MapKit.GetTile("Col", "SahteKumTepesi"),
            }, 1);

            // Giriş — safe, with a shrine and the first hints.
            Shrine(f, "giris", 5, -29);
            Lore(f, "1", -5, -27,
                "Anlatıcı: Taşa kazınmış eski bir yazı:\n" +
                "Anlatıcı: \"Yeraltı dokuz kattır. Her kat, yeryüzünde işlenen bir günahın ağırlığıyla kazılmıştır. İlki yalanınkidir.\"\n" +
                "Anlatıcı: Altına sonradan biri eklemiş: \"Bazı duvarlar yalan söyler. İnanma, yürü.\"");

            // Fısıltı Tepeleri — easy enemies, the whispers.
            Trigger(f, "fisiltilar", 0, -18, 6, 2,
                "Fısıltılar: Ülgen Han seni gerçekten evladı olarak mı sevdi... yoksa seni kendi büyük planı için bir araç mı yaptı?\n" +
                "Anlatıcı: Fısıltılar Börühan'ın güvenini içeriden kemirmeye, zehirli bir sarmaşık gibi ruhunu sarmaya çalıştı.\n" +
                "Anlatıcı: Ama Börühan'ın Ülgen Han'a sevgisi ve onunla edindiği anılar, bu fısıltıların üstesinden geldi.\n" +
                "Börühan: Yalan söylüyorsunuz. Onu tanıyorum.");
            Enemy(f, "Fisilti", -8, -10); Enemy(f, "Fisilti", 7, -16); Enemy(f, "Fisilti", 10, -7); Enemy(f, "KirikYalan", -13, -15);
            Lore(f, "2", 13, -14,
                "Anlatıcı: \"Fısıltılara kulak veren kendi sesini unutur. Fısıltıları susturan tek şey, gerçek bir anıdır.\"");
            Item(f, PickupKind.SifaOtu, "tepe", -14, -6);

            // Batı Vaha — herbs, a remorseful soul.
            Enemy(f, "Fisilti", -37, -15); Enemy(f, "Fisilti", -27, -24);
            Item(f, PickupKind.SifaOtu, "vaha1", -39, -23); Item(f, PickupKind.SifaOtu, "vaha2", -25, -15);
            Soul(f, "3", -34, -21,
                "Pişman Ruh: Ben kardeşimi bir avuç altın için sattım...\n" +
                "Pişman Ruh: Şimdi rüzgâr onun sesiyle bana fısıldıyor. Her gün, her gece.\n" +
                "Pişman Ruh: Sen kimseyi satma, yiğit. Kuzeydeki kervan yolunda, ihanetin gölgeleri pusuya yatar.");
            Shrine(f, "vaha", -27, -18);

            // Kristal Kanyon — Kırık Yalan nest, a Kut Parçası at the far end.
            Enemy(f, "KirikYalan", 28, -24); Enemy(f, "KirikYalan", 36, -21); Enemy(f, "KirikYalan", 30, -12); Enemy(f, "Fisilti", 34, -9);
            Item(f, PickupKind.KutParcasi, "kanyon", 37, -27);

            // Kervan Yolu — Hain Gölge ambush, Can Parçası, a diary.
            Enemy(f, "HainGolge", -28, 10); Enemy(f, "HainGolge", -37, 5); Enemy(f, "Fisilti", -30, 3); Enemy(f, "KirikYalan", -40, 12);
            Item(f, PickupKind.CanParcasi, "kervan", -41, 13);
            Lore(f, "4", -24, 12,
                "Anlatıcı: Yarı gömülü bir kervancı günlüğü:\n" +
                "Anlatıcı: \"Erlik'in elçileri yeryüzünde altın dağıttı; karşılığında yalnızca küçük bir yalan istediler.\"\n" +
                "Anlatıcı: \"Kimse bedelin kendi ruhları olduğunu bilmiyordu.\"");

            // Serap Labirenti — illusionists; a hidden chamber.
            Enemy(f, "Serap", 24, 10); Enemy(f, "Serap", 33, 5); Enemy(f, "KirikYalan", 26, 17); Enemy(f, "HainGolge", 30, 2);
            Item(f, PickupKind.CanParcasi, "serap", 39, 15);
            Lore(f, "5", 40, 17,
                "Anlatıcı: \"Ayna her şeyi gösterir, ama hiçbir şeyi bilmez.\"\n" +
                "Anlatıcı: \"Kendi yansımanla karşılaştığında korkularını değil, sevdiklerini hatırla. Ve o parladığında ona vurma.\"");

            // Mühürlü Mahzen — guarded by a Sır Bekçisi.
            Enemy(f, "SirBekcisi", 0, 6);
            Item(f, PickupKind.KutParcasi, "mahzen", -4, 10);
            Lore(f, "6", 4, 10,
                "Anlatıcı: \"Kut, Kayra Han'ın evrene üflediği candır.\"\n" +
                "Anlatıcı: \"Yenilen her dehşet özünü geride bırakır. Onu söküp alan, kendi Kut'unu büyütür.\"\n" +
                "Anlatıcı: Börühan, Kayra Han'ın sözlerini hatırladı.");

            // Kuzey Kapısı — the hardest fight before the boss.
            Enemy(f, "SirBekcisi", -6, 27); Enemy(f, "HainGolge", 7, 25); Enemy(f, "KirikYalan", 9, 31);
            Shrine(f, "kuzey", -8, 24);
            Lore(f, "7", 9, 24,
                "Anlatıcı: \"Ülgen'in ışığını çalan dokuzuncu katta oturur.\"\n" +
                "Anlatıcı: \"Tahtı paslı demir kazıklardan, kazıkları ise tanrıların zincirlerinden dövülmüştür.\"");
            Gate(f, "AynaKapisi", 0, 32.5f, Kat1Boss, "giris");
        }

        // ================= Kat 2: Zırhlılar Geçidi =================

        static void BuildKat2()
        {
            var g = new MapGrid(84, 68);
            g.Rect(0, -26, 16, 10);      // Giriş
            g.Rect(0, -8, 28, 14);       // Ocaklar Salonu
            g.Rect(-27, -8, 16, 14);     // Silahhane
            g.Rect(27, -6, 18, 16);      // Körükler
            g.Rect(-25, 16, 18, 12);     // Maden
            g.Rect(25, 18, 18, 12);      // Kalıp Atölyesi
            g.Rect(0, 24, 20, 10);       // Temir Kapısı

            g.Path(new Vector2(0, -26), new Vector2(0, -8));
            g.Path(new Vector2(-12, -8), new Vector2(-27, -8));
            g.Path(new Vector2(12, -6), new Vector2(27, -6));
            g.Path(new Vector2(0, -2), new Vector2(0, 24));                 // main way north…
            g.Rect(0, 8.5f, 4, 2, MapGrid.Pit);                              // …across a chasm (Ayna Sıçraması)
            g.Path(new Vector2(-27, -2), new Vector2(-25, 16));
            g.Rect(-25, 4.5f, 4, 2, MapGrid.Pit);
            g.Path(new Vector2(27, 2), new Vector2(25, 18));
            g.Path(new Vector2(-16, 18), new Vector2(-6, 24));
            g.Path(new Vector2(16, 18), new Vector2(6, 24));

            // Armory treasure on an island in a pit.
            g.Rect(-27, -10, 8, 6, MapGrid.Pit);
            g.Rect(-27, -10, 4, 2);
            // Lava channels in the forge hall and the bellows.
            g.Rect(-6, -8, 2, 10, MapGrid.Pit);
            g.Rect(6, -8, 2, 10, MapGrid.Pit);
            g.Rect(-6, -8, 2, 2);                                            // gaps to walk through
            g.Rect(6, -12, 2, 2);
            g.Rect(32, -6, 2, 16, MapGrid.Pit);
            foreach (var p in new[] { new Vector2(-10, -4), new Vector2(10, -11), new Vector2(-2, -12), new Vector2(22, -1), new Vector2(-30, 18), new Vector2(-20, 14), new Vector2(20, 20), new Vector2(30, 16) })
                g.Rect(p.x, p.y, 1, 1, MapGrid.Pillar);

            var theme = MapKit.GetTheme("Demirhane");
            var f = StartFloor(Kat2, "Zırhlılar Geçidi", 2, g, theme, new Vector2(0, -29));
            PaintMap(g, new TileSet
            {
                floor = MapKit.GetTile("Demirhane", "DemirZemin"), floorAlt = MapKit.GetTile("Demirhane", "KomurZemin"), decor = MapKit.GetTile("Demirhane", "Kivilcim"),
                wallFill = MapKit.GetTile("Demirhane", "DemirDuvar"), wallEdge = MapKit.GetTile("Demirhane", "DemirDuvarZirve"), pillar = MapKit.GetTile("Demirhane", "Ors"),
                pit = MapKit.GetTile("Demirhane", "Cukur"),
            }, 2);
            foreach (var lava in new[] { new Vector2(-6, -8), new Vector2(6, -8), new Vector2(32, -6) })
                Glow(new GameObject("LavIsigi") { transform = { position = lava } }, new Color(1f, 0.45f, 0.1f), 1f, 5f);

            Shrine(f, "giris", 5, -25);
            Lore(f, "1", -5, -24,
                "Anlatıcı: \"Erlik'in silahhanesi hiç soğumaz. Burada dövülen her kılıç, yeryüzünde bir savaşı başlatmak içindir.\"\n" +
                "Anlatıcı: Altında kömürle yazılmış bir not: \"Çukurların üzerinden ancak ayna gibi geçilir.\"");

            Enemy(f, "KorCini", -10, -12); Enemy(f, "KorCini", 10, -4); Enemy(f, "DemirMuhafiz", 0, -6); Enemy(f, "HainGolge", -3, -2);
            Item(f, PickupKind.SifaOtu, "salon", 12, -13);

            Enemy(f, "DemirMuhafiz", -24, -4); Enemy(f, "KorCini", -32, -3); Enemy(f, "KirikYalan", -22, -12);
            Item(f, PickupKind.CanParcasi, "silahhane", -27, -10);
            Lore(f, "2", -32, -12,
                "Anlatıcı: \"Tunç Ordu da bu örslerde dövüldü.\"\n" +
                "Anlatıcı: \"Yeraltının başka bir katında, atlarıyla birlikte uyandırılmayı bekliyorlar.\"");

            Enemy(f, "ZincirliMahkum", 22, -8); Enemy(f, "KorCini", 28, 0); Enemy(f, "DemirMuhafiz", 26, -11); Enemy(f, "OrsBekcisi", 24, -3);
            Item(f, PickupKind.KutParcasi, "korukler", 35, -9);
            Shrine(f, "korukler", 20, -12);

            Enemy(f, "ZincirliMahkum", -25, 13); Enemy(f, "KorCini", -19, 19); Enemy(f, "HainGolge", -30, 14);
            Soul(f, "3", -28, 19,
                "Yorgun Köle: Temir Han'ın damarlarında kan değil, lav akar.\n" +
                "Yorgun Köle: Bir darbesi dağları yarar... Ama yavaştır, çok yavaş. Vuruşunu bekle, sonra yanına kaç.\n" +
                "Yorgun Köle: Dengesini bozarsan, Kut'unla yık onu.");
            Item(f, PickupKind.SifaOtu, "maden", -21, 13);

            Enemy(f, "OrsBekcisi", 25, 18); Enemy(f, "DemirMuhafiz", 20, 15); Enemy(f, "KorCini", 30, 21);
            Lore(f, "4", 31, 14,
                "Anlatıcı: \"Gök Dalı, Yer Dalı ve Ulu Öz... Ata Ağacı'nın üç dalı, bir kahramanın yolunu anlatır.\"\n" +
                "Anlatıcı: \"Ülgen'in mirası hız ve yıldırımdır; yeraltının özü dayanıklılık; Kayra'nın kıvılcımı ise hepsini büyütür.\"");
            Item(f, PickupKind.CanParcasi, "kalip", 32, 22);

            Enemy(f, "DemirMuhafiz", -5, 23); Enemy(f, "ZincirliMahkum", 6, 22);
            Shrine(f, "kapi", -8, 21);
            Gate(f, "TemirKapisi", 0, 27.5f, Kat2Boss, "giris");
        }

        // ================= Kat 3: Hastalık ve Çürüme Diyarı =================

        static void BuildKat3()
        {
            var g = new MapGrid(88, 70);
            g.Rect(0, -28, 16, 10);      // Giriş
            g.Rect(0, -9, 30, 16);       // Sisli Bataklık
            g.Rect(-29, -10, 18, 14);    // Veba Koyu
            g.Rect(29, -12, 18, 18);     // Çürük Orman
            g.Rect(-27, 15, 18, 12);     // Ölü Kervan
            g.Rect(27, 15, 18, 12);      // Kemik Tapınağı
            g.Rect(0, 25, 20, 10);       // Çalış Kapısı

            g.Path(new Vector2(0, -28), new Vector2(0, -9));
            g.Path(new Vector2(-13, -9), new Vector2(-29, -10));
            g.Path(new Vector2(13, -10), new Vector2(29, -12));
            g.Path(new Vector2(-29, -3), new Vector2(-27, 15));
            g.Path(new Vector2(29, -3), new Vector2(27, 15));
            g.Path(new Vector2(-18, 17), new Vector2(-6, 25));
            g.Path(new Vector2(18, 17), new Vector2(6, 25));
            // A short but poisonous road straight north.
            g.Path(new Vector2(0, -1), new Vector2(0, 25), 3f);
            g.Rect(0, 10, 3, 14, MapGrid.Poison);

            // Bogs and gas in the open marsh.
            g.Rect(-7, -6, 8, 5, MapGrid.Bog); g.Rect(8, -12, 9, 4, MapGrid.Bog); g.Rect(3, -4, 4, 3, MapGrid.Poison);
            g.Rect(-28, -12, 10, 6, MapGrid.Poison); g.Rect(27, 13, 6, 4, MapGrid.Bog); g.Rect(-25, 12, 6, 3, MapGrid.Bog);
            // Rooms sealed by cracked walls (Demir Eldiven).
            g.Rect(-42, -10, 6, 6); g.Rect(-39, -10, 2, 2);
            g.Rect(42, -16, 6, 6); g.Rect(39, -16, 2, 2);
            foreach (var p in new[] { new Vector2(24, -16), new Vector2(33, -8), new Vector2(26, -6), new Vector2(34, -18), new Vector2(-12, -12), new Vector2(12, -5), new Vector2(-32, 18), new Vector2(31, 18) })
                g.Rect(p.x, p.y, 1, 1, MapGrid.Pillar);

            var theme = MapKit.GetTheme("Bataklik");
            var f = StartFloor(Kat3, "Hastalık ve Çürüme Diyarı", 3, g, theme, new Vector2(0, -31));
            PaintMap(g, new TileSet
            {
                floor = MapKit.GetTile("Bataklik", "BatakZemin"), floorAlt = MapKit.GetTile("Bataklik", "CamurZemin"),
                wallFill = MapKit.GetTile("Bataklik", "CurukDuvar"), wallEdge = MapKit.GetTile("Bataklik", "CurukAgac"), pillar = MapKit.GetTile("Bataklik", "CurukAgac"),
                bog = MapKit.GetTile("Bataklik", "Bataklik"), poison = MapKit.GetTile("Bataklik", "ZehirliSis"),
            }, 3);

            Shrine(f, "giris", 5, -27);
            Lore(f, "1", -5, -26,
                "Anlatıcı: \"Çalış Han, yeryüzüne salgın ve acı gönderen hastalığın efendisidir. Burada nefesin bile ona aittir.\"\n" +
                "Anlatıcı: Altına kazınmış: \"Çatlak duvarlar demirden bir yumruğu bekler.\"");

            Enemy(f, "VebaSinegi", -8, -12); Enemy(f, "VebaSinegi", 9, -6); Enemy(f, "VebaSinegi", 5, -14); Enemy(f, "BatakSulugu", -4, -4); Enemy(f, "CurukYaratik", 12, -9);
            Item(f, PickupKind.SifaOtu, "bataklik", -13, -15);

            Enemy(f, "CurukYaratik", -26, -6); Enemy(f, "BatakSulugu", -33, -13); Enemy(f, "VebaSinegi", -23, -14);
            Crack(f, "koy", -39, -11);
            Item(f, PickupKind.CanParcasi, "koy", -42, -10);
            Lore(f, "2", -43, -9,
                "Anlatıcı: \"Ülgen Han bir kış gecesi yeryüzüne baktığında yalnızca donmak üzere bir bebek görmedi.\"\n" +
                "Anlatıcı: \"Erlik'i yenecek olanı gördü.\"");

            Enemy(f, "CurukYaratik", 26, -16); Enemy(f, "DemirMuhafiz", 31, -6); Enemy(f, "BatakSulugu", 24, -9); Enemy(f, "VebaSinegi", 34, -13);
            Crack(f, "orman", 39, -17);
            Item(f, PickupKind.KutParcasi, "orman", 42, -16);
            Shrine(f, "orman", 22, -19);

            Enemy(f, "VebaKami", -27, 17); Enemy(f, "BatakSulugu", -21, 13); Enemy(f, "CurukYaratik", -32, 13);
            Soul(f, "3", -22, 19,
                "Pişman Ruh: Ben bir şamandım. Çalış Han'a tapanlara katıldım, salgını kutsal sandım...\n" +
                "Pişman Ruh: Bataklığın dibinden yeni yaratıklar doğuyor. O onları çağırdıkça ölüler bile rahat edemiyor.\n" +
                "Pişman Ruh: Onu yenersen, zehrin sana dokunamaz olur. Bu bataklığın tek iyiliği budur.");

            Enemy(f, "VebaKami", 27, 17); Enemy(f, "HainGolge", 22, 12); Enemy(f, "VebaSinegi", 32, 14);
            Lore(f, "4", 32, 19,
                "Anlatıcı: \"Dördüncü kattan sekizinciye uzanan derinliklerde Buz Devi uyur, Kam-Su'nun kızgın bakır nehirleri akar ve Kut Çeşmesi kadim sırlarını saklar.\"\n" +
                "Anlatıcı: \"Ama Erlik'e giden en kısa yol, Çalış Han'ın çukurundan geçer.\"");
            Item(f, PickupKind.CanParcasi, "tapinak", 22, 19);

            Enemy(f, "CurukYaratik", -5, 24); Enemy(f, "VebaKami", 6, 26);
            Shrine(f, "kapi", -8, 22);
            Gate(f, "CalisKapisi", 0, 28.5f, Kat3Boss, "giris");
        }

        // ================= Kat 9: Erlik'in Tahtı =================

        static void BuildKat9()
        {
            var g = new MapGrid(44, 64);
            g.Rect(0, -22, 12, 8);       // Giriş
            g.Rect(0, -6, 14, 24);       // Sütunlar Salonu
            g.Rect(0, 18, 30, 18);       // Taht Odası
            g.Path(new Vector2(0, -22), new Vector2(0, -6));
            g.Path(new Vector2(0, 5), new Vector2(0, 12), 6f);
            for (int i = 0; i < 5; i++)
            {
                g.Rect(-5, -14 + i * 4, 1, 1, MapGrid.Pillar);
                g.Rect(5, -14 + i * 4, 1, 1, MapGrid.Pillar);
            }
            foreach (var p in new[] { new Vector2(-12, 12), new Vector2(12, 12), new Vector2(-12, 22), new Vector2(12, 22) })
                g.Rect(p.x, p.y, 1, 1, MapGrid.Pillar);

            var theme = MapKit.GetTheme("Obsidyen");
            var f = StartFloor(Kat9, "Erlik'in Tahtı", 9, g, theme, new Vector2(0, -24));
            PaintMap(g, new TileSet
            {
                floor = MapKit.GetTile("Obsidyen", "Obsidyen"), floorAlt = MapKit.GetTile("Obsidyen", "Obsidyen2"), decor = MapKit.GetTile("Obsidyen", "Zincir"),
                wallFill = MapKit.GetTile("Obsidyen", "ObsidyenDuvar"), wallEdge = MapKit.GetTile("Obsidyen", "PasliSutun"), pillar = MapKit.GetTile("Obsidyen", "PasliSutun"),
            }, 9);

            Shrine(f, "giris", 4, -21);
            Lore(f, "1", -4, -20,
                "Anlatıcı: \"Göğü ve yeri tuttuğu söylenen sütunlar şimdi paslanıyor.\"\n" +
                "Anlatıcı: \"Denge bozuldukça yeryüzünde kışlar uzar, geceler kararır.\"");
            Enemy(f, "DemirMuhafiz", -2, -12); Enemy(f, "HainGolge", 3, -6); Enemy(f, "VebaKami", 0, 0);
            Soul(f, "ulgen", 0, -15,
                "Ülgen Han: Evlat... Buraya kadar geldin.\n" +
                "Ülgen Han: Sesimi duyuyor musun? Korkma. Ben buradayım. Seni hep izledim.\n" +
                "Ülgen Han: Erlik illüzyonlarla zihnini bunaltmaya çalışacak. Gerçeği hatırla: sen benim oğlumsun.");
            Item(f, PickupKind.SifaOtu, "salon1", -4, -4); Item(f, PickupKind.SifaOtu, "salon2", 4, 2);

            var throne = Prop(f, "Taht", PrologueBuilder.Art("ErlikTahti", 40, 36, new Color32(60, 20, 30, 255), Shape.Box), 0, 21, 1.2f);
            Glow(throne, new Color(0.9f, 0.2f, 0.25f), 1.2f, 6f);
            var ulgen = Prop(f, "UlganHan", PrologueBuilder.Art("UlgenHan_Zincirli", 14, 24, new Color32(150, 180, 220, 255), Shape.Figure), 0, 25.5f, 0.4f);
            Glow(ulgen, new Color(0.6f, 0.75f, 1f), 0.5f, 3f);

            Trigger(f, "taht", 0, 9, 6, 2,
                "Anlatıcı: Burası sıradan bir taht odası değildi.\n" +
                "Anlatıcı: Tahtın arkasında, Börühan'ın bütün bu yolu gelmesinin sebebi vardı: Ülgen Han.\n" +
                "Ülgen Han: Börühan... Evlat...\n" +
                "Anlatıcı: Tahtta oturan kişi Erlik Han'dı. Ayağa kalktı ve konuşmaya başladı.\n" +
                "Erlik Han: Demek bütün katları aşıp buraya kadar geldin ha?\n" +
                "Erlik Han: Üzgünüm ama burası senin ulaşacağın son yer.");

            var erlik = CombatKit.PlaceEnemy(enemies["ErlikHan"], new Vector2(0, 17), f.enemyRoot).GetComponent<EnemyController>();
            var encounter = new GameObject("ErlikKarsilasmasi").AddComponent<BossEncounter>();
            PrologueBuilder.Set(encounter, "boss", erlik);
            PrologueBuilder.Set(encounter, "defeatedFlag", StoryFlags.ErlikDefeated);
            PrologueBuilder.Set(encounter, "victoryDialogue", Dialogue("kat9_zafer",
                "Anlatıcı: Zorlu savaşın sonunda Erlik Han yere yığıldı.\n" +
                "Anlatıcı: Börühan paslı zincirleri kılıcıyla parçaladı.\n" +
                "Ülgen Han: Evlat... Geleceğini... biliyordum.\n" +
                "Anlatıcı: Ülgen Han'ın hali bitmişti; üzeri yaralıydı ve ışığı sönmek üzereydi.\n" +
                "Börühan: Seni eve götürüyorum, baba."));
            PrologueBuilder.Set(encounter, "nextScene", Ending);
            SetMusic(MapKit.GetTrack("Boss"));
        }

        // ================= Boss arenas, intros and the ending =================

        static void BossArena(string scene, string location, int floorNumber, string themeName, EnemyDefinition boss, string defeatedFlag,
            DialogueAsset intro, DialogueAsset victory, string riftScene, string nextScene)
        {
            var g = new MapGrid(26, 24);
            g.Rect(0, 0, 20, 18);
            g.Rect(-6, -3, 1, 1, MapGrid.Pillar); g.Rect(6, -3, 1, 1, MapGrid.Pillar);
            g.Rect(-6, 5, 1, 1, MapGrid.Pillar); g.Rect(6, 5, 1, 1, MapGrid.Pillar);

            var theme = MapKit.GetTheme(themeName);
            var f = StartFloor(scene, location, floorNumber, g, theme, new Vector2(0, -7.5f));
            var tiles = themeName switch
            {
                "Col" => new TileSet { floor = MapKit.GetTile("Col", "KristalKum"), floorAlt = MapKit.GetTile("Col", "KristalKum2"), decor = MapKit.GetTile("Col", "KirikKristal"), wallFill = MapKit.GetTile("Col", "KumTepesi"), wallEdge = MapKit.GetTile("Col", "KristalSutun"), pillar = MapKit.GetTile("Col", "KristalSutun") },
                "Demirhane" => new TileSet { floor = MapKit.GetTile("Demirhane", "DemirZemin"), floorAlt = MapKit.GetTile("Demirhane", "KomurZemin"), decor = MapKit.GetTile("Demirhane", "Kivilcim"), wallFill = MapKit.GetTile("Demirhane", "DemirDuvar"), wallEdge = MapKit.GetTile("Demirhane", "DemirDuvarZirve"), pillar = MapKit.GetTile("Demirhane", "Ors") },
                _ => new TileSet { floor = MapKit.GetTile("Bataklik", "BatakZemin"), floorAlt = MapKit.GetTile("Bataklik", "CamurZemin"), decor = MapKit.GetTile("Bataklik", "ZehirliSis"), wallFill = MapKit.GetTile("Bataklik", "CurukDuvar"), wallEdge = MapKit.GetTile("Bataklik", "CurukAgac"), pillar = MapKit.GetTile("Bataklik", "CurukAgac") },
            };
            PaintMap(g, tiles, scene.GetHashCode());
            SetMusic(MapKit.GetTrack("Boss"));

            Shrine(f, "giris", 6, -7);
            PrologueBuilder.StartTrigger(f.world, "BossGiris", intro, null, scene + "_giris_goruldu");
            var enemy = CombatKit.PlaceEnemy(boss, new Vector2(0, 4), f.enemyRoot).GetComponent<EnemyController>();

            var encounter = new GameObject("BossKarsilasmasi").AddComponent<BossEncounter>();
            PrologueBuilder.Set(encounter, "boss", enemy);
            PrologueBuilder.Set(encounter, "defeatedFlag", defeatedFlag);
            PrologueBuilder.Set(encounter, "victoryDialogue", victory);
            PrologueBuilder.Set(encounter, "nextScene", nextScene ?? "");

            if (riftScene != null)
            {
                var rift = Gate(f, "Yarik", 0, 4, riftScene, "");
                PrologueBuilder.Activator(f.world, "YarikGorunurlugu", defeatedFlag, true, rift);
            }
        }

        static void SetMusic(MusicTrack track)
        {
            var music = Object.FindObjectsByType<SceneMusic>(FindObjectsInactive.Include).First(m => m.gameObject.scene == SceneManager.GetActiveScene());
            PrologueBuilder.Set(music, "track", track);
        }

        static void FloorIntro(string title, DialogueAsset subtitles, string next, int floorNumber, string location, string music)
        {
            PrologueBuilder.CreateCamera(5f, Color.black);
            var canvas = PrologueBuilder.CreateCanvas("Canvas", 10);
            var titleText = PrologueBuilder.CenterTitle(canvas.transform);
            PrologueBuilder.SkipHint(canvas.transform);
            PrologueBuilder.Instantiate(PrologueBuilder.CreatePrefabs().dialogueBox);
            MapKit.AddSceneMusic(MapKit.GetTrack(music));

            var cutscene = new GameObject("Sinematik").AddComponent<CutscenePlayer>();
            PrologueBuilder.Set(cutscene, "subtitles", subtitles);
            PrologueBuilder.Set(cutscene, "titleText", titleText);
            PrologueBuilder.Set(cutscene, "openingTitle", title);
            PrologueBuilder.Set(cutscene, "nextScene", next);
            PrologueBuilder.Set(cutscene, "nextSpawn", "giris");
            PrologueBuilder.Set(cutscene, "locationName", location);
            PrologueBuilder.Set(cutscene, "floor", floorNumber);
        }

        static void BuildEnding()
        {
            PrologueBuilder.CreateCamera(5f, Color.black);
            var canvas = PrologueBuilder.CreateCanvas("Canvas", 10);
            var titleText = PrologueBuilder.CenterTitle(canvas.transform);
            PrologueBuilder.SkipHint(canvas.transform);
            PrologueBuilder.Instantiate(PrologueBuilder.CreatePrefabs().dialogueBox);
            MapKit.AddSceneMusic(MapKit.GetTrack("Son"));

            var cutscene = new GameObject("Sinematik").AddComponent<CutscenePlayer>();
            PrologueBuilder.Set(cutscene, "subtitles", Dialogue("son",
                "Anlatıcı: İkisi birlikte yeryüzüne döndü.\n" +
                "Anlatıcı: Aradan uzun zaman geçti.\n" +
                "Anlatıcı: Ülgen Han iyileşti; eskisinden de güçlü, eskisinden de parlak.\n" +
                $"Anlatıcı: İyilik kötülüğün üstesinden gelmiş, mutlak denge yeniden sağlanmıştı. [bayrak: {StoryFlags.GameFinished}]"));
            PrologueBuilder.Set(cutscene, "titleText", titleText);
            PrologueBuilder.Set(cutscene, "closingTitle", "SON\n\nBörühan: Yeraltının Katları");
            PrologueBuilder.Set(cutscene, "nextScene", SceneNames.MainMenu);
            PrologueBuilder.Set(cutscene, "locationName", "Yeryüzü — Son");
        }

        // ================= Scene plumbing =================

        static void BuildScene(string name, System.Action build)
        {
            var path = $"{PrologueBuilder.SceneDir}/{name}.unity";
            if (File.Exists(path))
                return;
            var scene = PrologueBuilder.BeginScene();
            build();
            EditorSceneManager.SaveScene(scene, path);
            PrologueBuilder.CloseScene(scene);
            MapKit.AddToBuild(path);
        }

        /// <summary>Hooks the prologue up to the underworld and gives the yurt Ülgen Han's sword.</summary>
        static void PatchPrologue()
        {
            EditScene(SceneNames.Floor1Intro, scene =>
            {
                var cutscene = scene.GetRootGameObjects().Select(r => r.GetComponent<CutscenePlayer>()).First(c => c != null);
                PrologueBuilder.Set(cutscene, "nextScene", Kat1);
                PrologueBuilder.Set(cutscene, "nextSpawn", "giris");
                PrologueBuilder.Set(cutscene, "closingTitle", "");
            });

            EditScene(SceneNames.Yurt, scene =>
            {
                var world = scene.GetRootGameObjects().First(r => r.name == "Dunya").transform;
                if (world.Find("KilicAskisi") != null)
                    return;
                var rack = PrologueBuilder.Prop(world, "KilicAskisi", PrologueBuilder.Art("KilicAskisi", 14, 22, new Color32(110, 80, 50, 255), Shape.Box), new Vector2(-1.5f, 2.2f), 0.4f);
                var sword = PrologueBuilder.SpriteObject(rack.transform, "Kilic", PrologueBuilder.Art("Kilic", 3, 12, new Color32(205, 210, 220, 255), Shape.Box), new Vector2(0f, 0.25f), 1);
                PrologueBuilder.Interact(rack, "Al", Dialogue("otag_kilic",
                    "Anlatıcı: Askıda, Ülgen Han'ın ona armağan ettiği kılıç asılıydı.\n" +
                    "Ülgen Han: Bu kılıç yıldızların demirinden dövüldü. Onu yalnızca doğru olan için çek.\n" +
                    $"Anlatıcı: Börühan kılıcı kuşandı. [bayrak: {StoryFlags.Sword}]\n" +
                    "Anlatıcı: Hafif saldırı: J / Sol Tık · Ağır saldırı: K / Sağ Tık · Kaçış: Shift · Savuşturma: Q"),
                    completedFlag: StoryFlags.Sword, repeat: Dialogue("otag_kilic_bos", "Anlatıcı: Askı artık boş. Kılıç Börühan'ın belinde."));
                PrologueBuilder.Activator(world, "AskiKilici", StoryFlags.Sword, false, sword);
            });

            EditScene(SceneNames.Overworld, scene =>
            {
                foreach (var door in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneDoor>(true)))
                    PrologueBuilder.Set(door, "lockedTargetScene", "");
            });
        }

        static void EditScene(string name, System.Action<Scene> edit)
        {
            var path = $"{PrologueBuilder.SceneDir}/{name}.unity";
            var open = SceneManager.GetSceneByPath(path);
            bool wasOpen = open.IsValid() && open.isLoaded;
            if (wasOpen && open.isDirty)
            {
                Debug.LogWarning($"[Börühan] {name} açık ve kaydedilmemiş; atlandı.");
                return;
            }
            var previous = SceneManager.GetActiveScene();
            var scene = wasOpen ? open : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            edit(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (previous.IsValid())
                SceneManager.SetActiveScene(previous);
            if (!wasOpen)
                EditorSceneManager.CloseScene(scene, true);
        }

        static void RemoveTestRoom()
        {
            const string path = "Assets/Scenes/SavasOdasi.unity";
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToArray();
            if (File.Exists(path))
                AssetDatabase.DeleteAsset(path);
            if (AssetDatabase.IsValidFolder("Assets/Dialogue/Sinama"))
                AssetDatabase.DeleteAsset("Assets/Dialogue/Sinama");
        }
    }
}
