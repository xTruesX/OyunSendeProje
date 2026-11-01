using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Story;
using Boruhan.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Shape = Boruhan.EditorTools.PrologueBuilder.Shape;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Combat setup: gives the player prefab a sword, adds health bars to the HUD, defines the
    /// Sırlar Çölü enemies and builds the combat test room. Only creates what's missing.
    /// </summary>
    public static class CombatKit
    {
            public const string SwordFlag = "kilic_alindi";

        const string EnemyDir = "Assets/Combat/Dusmanlar";
        const string EnemyArtDir = "Assets/Art/Placeholder/Dusmanlar";
        const string PlayerPrefab = "Assets/Prefabs/Boruhan.prefab";
        const string HudPrefab = "Assets/Prefabs/OyunArayuzu.prefab";

        [MenuItem("Börühan/Savaş Sistemini Kur")]
        public static void Setup()
        {
            MapKit.EnsureAssets();
            EnsurePlayerCombat();
            EnsureHud();
            EnsureEnemies();
            EnsureSoundEffects();
            EnsureHealthOrbs();
            EnsureDeathScreen();
            AssetDatabase.SaveAssets();
        }

        // ================= Sound effects =================

        const string SfxDir = "Assets/Audio/Efektler";

        /// <summary>Creates placeholder attack and dash sounds (if missing) and hooks them up to the player.</summary>
        [MenuItem("Börühan/Bakım/Ses Efektlerini Oluştur")]
        public static void EnsureSoundEffects()
        {
            var light = Sfx("HafifSaldiri", 0.14f, (t, k) => Noise() * 0.5f * Env(k, 0.02f) + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 500f, k) * t) * 0.35f * Env(k, 0.01f));
            var heavy = Sfx("AgirSaldiri", 0.28f, (t, k) => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 55f, k) * t) * 0.8f * Env(k, 0.01f) + Noise() * 0.35f * Env(Mathf.Min(1f, k * 2.5f), 0.01f));
            var miss = Sfx("IskaSaldiri", 0.16f, (t, k) => Noise() * 0.3f * Mathf.Sin(Mathf.PI * k));
            var dodge = Sfx("Kacis", 0.2f, (t, k) => Noise() * 0.4f * Mathf.Sin(Mathf.PI * Mathf.Sqrt(k)) * (1f - k));
            var blink = Sfx("AynaSicramasi", 0.3f, (t, k) => (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(500f, 1600f, k) * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.15f) * Env(k, 0.05f));
            var unavailable = Sfx("KacisHazirDegil", 0.12f, (t, k) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 160f * t)) * 0.25f * Env(k, 0.02f));
            var ready = Sfx("AynaSicramasiHazir", 0.35f, (t, k) => Mathf.Sin(2f * Mathf.PI * (k < 0.4f ? 880f : 1320f) * t) * 0.35f * Env(k < 0.4f ? k / 0.4f : (k - 0.4f) / 0.6f, 0.05f));

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var combat = root.GetComponent<PlayerCombat>();
                var so = new SerializedObject(combat);
                void Assign(string field, AudioClip clip)
                {
                    var prop = so.FindProperty(field);
                    if (prop.objectReferenceValue == null)
                        prop.objectReferenceValue = clip;
                }
                Assign("lightAttackSound", light);
                Assign("heavyAttackSound", heavy);
                Assign("missSound", miss);
                Assign("dodgeSound", dodge);
                Assign("blinkSound", blink);
                Assign("dodgeUnavailableSound", unavailable);
                Assign("blinkReadySound", ready);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ================= Death screen =================

        /// <summary>Adds the death screen (Yenildin / Tekrar Dene / Ana Menü) and its sounds to the HUD prefab, if missing.</summary>
        [MenuItem("Börühan/Bakım/Ölüm Ekranını Kur")]
        public static void EnsureDeathScreen()
        {
            var fall = Sfx("Olum", 0.9f, (t, k) => (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 60f, k) * t) * 0.6f + Noise() * 0.25f * (1f - k)) * Env(k, 0.02f));
            // A low minor chord that swells and fades.
            var sting = Sfx("OlumEkrani", 2.4f, (t, k) =>
                (Mathf.Sin(2f * Mathf.PI * 110f * t) + Mathf.Sin(2f * Mathf.PI * 130.8f * t) * 0.8f + Mathf.Sin(2f * Mathf.PI * 164.8f * t) * 0.6f)
                * 0.25f * Mathf.Sin(Mathf.PI * Mathf.Sqrt(k)));
            var retry = Sfx("TekrarDene", 0.5f, (t, k) => Mathf.Sin(2f * Mathf.PI * (k < 0.3f ? 440f : k < 0.6f ? 554f : 659f) * t) * 0.35f * (1f - k));

            var root = PrefabUtility.LoadPrefabContents(HudPrefab);
            try
            {
                if (root.GetComponent<DeathScreen>() != null)
                    return;
                PrologueBuilder.EnsureFonts();

                var panel = PrologueBuilder.UIObject("OlumEkrani", root.transform);
                PrologueBuilder.Stretch(panel);
                panel.transform.SetAsLastSibling();
                panel.AddComponent<Image>().color = new Color(0.06f, 0.01f, 0.02f, 0.88f);
                var group = panel.AddComponent<CanvasGroup>();
                var layout = panel.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 24;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;

                PrologueBuilder.Sized(PrologueBuilder.Label(panel.transform, "Baslik", "YENİLDİN", 110, new Color32(170, 30, 35, 255), PrologueBuilder.titleFont), 1400, 150);
                var message = PrologueBuilder.Sized(PrologueBuilder.Label(panel.transform, "Mesaj", "", 34, PrologueBuilder.TextLight), 1400, 60);
                message.fontStyle = FontStyle.Italic;
                PrologueBuilder.Sized(PrologueBuilder.Label(panel.transform, "Bosluk", "", 10, Color.clear), 10, 40);
                var retryButton = PrologueBuilder.MakeButton(panel.transform, "TekrarDeneButonu", "Tekrar Dene");
                var menuButton = PrologueBuilder.MakeButton(panel.transform, "AnaMenuButonu", "Ana Menüye Dön");
                PrologueBuilder.Sized(PrologueBuilder.Label(panel.transform, "Ipucu", "Son kayıttan devam edersin.", 24, new Color32(200, 190, 170, 160)), 1400, 40);

                var screen = root.AddComponent<DeathScreen>();
                PrologueBuilder.Set(screen, "root", panel);
                PrologueBuilder.Set(screen, "group", group);
                PrologueBuilder.Set(screen, "messageText", message);
                PrologueBuilder.Set(screen, "retryButton", retryButton);
                PrologueBuilder.Set(screen, "mainMenuButton", menuButton);
                PrologueBuilder.Set(screen, "deathSound", fall);
                PrologueBuilder.Set(screen, "screenSound", sting);
                PrologueBuilder.Set(screen, "retrySound", retry);
                PrefabUtility.SaveAsPrefabAsset(root, HudPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ================= Health orbs =================

        const string HealthOrbPrefab = "Assets/Prefabs/SifaKuresi.prefab";

        // Summons that may drop a health orb: (enemy, ability, chance).
        static readonly (string enemy, string ability, float chance)[] OrbDrops =
        {
            ("AynaKopya", "Kopyalar", 0.3f),
            ("TemirHan", "Körük Çağrısı", 0.4f),
        };

        /// <summary>
        /// Creates the Şifa Küresi prefab (if missing) and lets the bosses' summons drop it. Only fills
        /// abilities that have no orb yet, so chances changed in the Inspector are kept.
        /// </summary>
        [MenuItem("Börühan/Bakım/Şifa Kürelerini Kur")]
        public static void EnsureHealthOrbs()
        {
            var orb = AssetDatabase.LoadAssetAtPath<HealthOrb>(HealthOrbPrefab);
            if (orb == null)
            {
                var sprite = PrologueBuilder.Art(PrologueBuilder.ArtDir, "SifaKuresi", 8, 8, new Color32(90, 230, 110, 255), Shape.Circle, new Vector2(0.5f, 0.5f));
                var sound = Sfx("SifaKuresi", 0.3f, (t, k) => (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(600f, 1200f, k) * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 1800f, k) * t) * 0.15f) * Env(k, 0.03f));
                var go = new GameObject("SifaKuresi");
                try
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = 5;
                    go.AddComponent<CircleCollider2D>().isTrigger = true;
                    ((CircleCollider2D)go.GetComponent<Collider2D>()).radius = 0.35f;
                    var component = go.AddComponent<HealthOrb>();
                    PrologueBuilder.Set(component, "pickupSound", sound);
                    orb = PrefabUtility.SaveAsPrefabAsset(go, HealthOrbPrefab).GetComponent<HealthOrb>();
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            foreach (var (enemy, abilityName, chance) in OrbDrops)
            {
                var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyDir}/{enemy}.asset");
                if (def == null)
                    continue;
                foreach (var ability in def.abilities)
                {
                    if (ability == null || ability.displayName != abilityName)
                        continue;
                    if (ability is SummonIllusionsAbility illusions && illusions.healthOrb == null)
                    {
                        illusions.healthOrb = orb;
                        illusions.healthOrbChance = chance;
                    }
                    else if (ability is SummonMinionsAbility minions && minions.healthOrb == null)
                    {
                        minions.healthOrb = orb;
                        minions.healthOrbChance = chance;
                    }
                    EditorUtility.SetDirty(ability);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static readonly System.Random sfxRandom = new System.Random(7);
        static float Noise() => (float)(sfxRandom.NextDouble() * 2.0 - 1.0);
        // Quick attack, then decay over the rest of the sound (k = 0..1 through the sound).
        static float Env(float k, float attack) => k < attack ? k / attack : Mathf.Pow(1f - (k - attack) / (1f - attack), 2f);

        /// <summary>Writes a short placeholder WAV if the file doesn't exist yet. Replace the file with your own sound.</summary>
        static AudioClip Sfx(string name, float seconds, System.Func<float, float, float> sample)
        {
            Directory.CreateDirectory(SfxDir);
            var path = $"{SfxDir}/{name}.wav";
            if (!File.Exists(path))
            {
                const int rate = 22050;
                int count = Mathf.RoundToInt(rate * seconds);
                using (var w = new BinaryWriter(File.Create(path)))
                {
                    w.Write("RIFF".ToCharArray()); w.Write(36 + count * 2); w.Write("WAVEfmt ".ToCharArray());
                    w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                    w.Write("data".ToCharArray()); w.Write(count * 2);
                    for (int i = 0; i < count; i++)
                        w.Write((short)(Mathf.Clamp(sample((float)i / rate, (float)i / count), -1f, 1f) * short.MaxValue * 0.8f));
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ================= Player =================

        static void EnsurePlayerCombat()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                if (root.GetComponent<PlayerCombat>() != null)
                    return;

                var health = root.GetComponent<Health>();
                if (health == null)
                    health = root.AddComponent<Health>();
                PrologueBuilder.Set(health, "team", (int)Team.Player);
                PrologueBuilder.Set(health, "maxHealth", 100f);

                // No sword sprite on him: his sword animations (Boruhan_Kilic) already draw it in his hand.
                var slash = PrologueBuilder.SpriteObject(root.transform, "Savurma",
                    PrologueBuilder.Art(PrologueBuilder.ArtDir, "Kilic_Savurma", 32, 32, Color.white, Shape.Crescent, new Vector2(0.5f, 0.5f)), Vector2.zero, 10);

                var combat = root.AddComponent<PlayerCombat>();
                PrologueBuilder.Set(combat, "slashRenderer", slash.GetComponent<SpriteRenderer>());
                PrologueBuilder.Set(combat, "bodyRenderer", root.transform.Find("Govde").GetComponent<SpriteRenderer>());
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ================= HUD =================

        static void EnsureHud()
        {
            var root = PrefabUtility.LoadPrefabContents(HudPrefab);
            try
            {
                if (root.GetComponent<PlayerHealthBar>() != null)
                    return;
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/CinzelDecorative-Bold.ttf") ?? font;

                // Player health, top-left; the talisman moves below it.
                ((RectTransform)root.transform.Find("Tilsim")).anchoredPosition = new Vector2(40f, -86f);
                var bar = Rect(root.transform, "CanBari", new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(380f, 30f));
                bar.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
                var fill = Fill(bar, new Color(0.8f, 0.15f, 0.15f));
                var label = Label(bar, "Yazi", "Can", font, 20, Color.white, TextAnchor.MiddleLeft);
                label.rectTransform.offsetMin = new Vector2(12f, 0f);
                var playerBar = root.AddComponent<PlayerHealthBar>();
                PrologueBuilder.Set(playerBar, "fill", fill);
                PrologueBuilder.Set(playerBar, "label", label);

                // Boss health, top-centre.
                var boss = Rect(root.transform, "BossBari", new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 64f));
                var bossName = Label(boss, "Isim", "", titleFont, 30, new Color32(212, 168, 75, 255), TextAnchor.UpperCenter);
                var bossBack = Rect(boss, "Bar", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 22f));
                bossBack.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
                var bossFill = Fill(bossBack, new Color(0.65f, 0.1f, 0.15f));
                var bossBar = root.AddComponent<BossHealthBar>();
                PrologueBuilder.Set(bossBar, "root", boss.gameObject);
                PrologueBuilder.Set(bossBar, "fill", bossFill);
                PrologueBuilder.Set(bossBar, "nameLabel", bossName);

                var hint = root.transform.Find("KontrolIpucu").GetComponent<Text>();
                hint.text = "Hareket: WASD  ·  Etkileşim: E  ·  Saldırı: J / Sol Tık  ·  Ağır: K / Sağ Tık  ·  Kaçış: Shift  ·  Savuşturma: Q  ·  Duraklat: Esc";
                hint.rectTransform.sizeDelta = new Vector2(1500f, 40f);

                PrefabUtility.SaveAsPrefabAsset(root, HudPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static RectTransform Fill(RectTransform parent, Color color)
        {
            var rect = (RectTransform)new GameObject("Dolgu", typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        static Text Label(Transform parent, string name, string text, Font font, int size, Color color, TextAnchor anchor)
        {
            var label = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.raycastTarget = false;
            return label;
        }

        // ================= Enemies =================

        public static EnemyAbility AddAbility(EnemyDefinition definition, Type type, string name, bool inMoveset = true)
        {
            var ability = (EnemyAbility)ScriptableObject.CreateInstance(type);
            ability.name = name;
            ability.displayName = name;
            AssetDatabase.AddObjectToAsset(ability, definition);
            if (inMoveset)
                definition.abilities.Add(ability);
            EditorUtility.SetDirty(definition);
            return ability;
        }

        internal static T Add<T>(EnemyDefinition definition, string name, Action<T> setup, bool inMoveset = true) where T : EnemyAbility
        {
            var ability = (T)AddAbility(definition, typeof(T), name, inMoveset);
            setup(ability);
            return ability;
        }

        internal static Sprite EnemyArt(string name, int w, int h, Color color, Shape shape) =>
            PrologueBuilder.Art(EnemyArtDir, name, w, h, color, shape, new Vector2(0.5f, 0f));

        internal static EnemyDefinition Enemy(string file, Action<EnemyDefinition> stats, Action<EnemyDefinition> moveset)
        {
            Directory.CreateDirectory(EnemyDir);
            var path = $"{EnemyDir}/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            stats(def);
            AssetDatabase.CreateAsset(def, path);
            moveset(def);
            EditorUtility.SetDirty(def);
            return def;
        }

        internal static Dictionary<string, EnemyDefinition> EnsureEnemies()
        {
            var all = new Dictionary<string, EnemyDefinition>();

            all["Fisilti"] = Enemy("Fisilti", d =>
            {
                d.displayName = "Fısıltı";
                d.difficulty = Difficulty.Kolay;
                d.description = "Çölün rüzgârında dolaşan, en güvendiğin kişilerin sesiyle fısıldayan ruh. Üzerine atılır.";
                d.sprite = EnemyArt("Fisilti", 14, 14, new Color32(170, 130, 220, 255), Shape.Circle);
                d.maxHealth = 30f; d.moveSpeed = 2.4f; d.contactDamage = 5f;
                d.aggroRange = 6f; d.preferredDistance = 2.5f;
            }, d =>
            {
                Add<LungeAbility>(d, "Fısıltı Atılışı", a =>
                {
                    a.damage = 10f; a.distance = 3.5f; a.speed = 11f;
                    a.windup = 0.55f; a.recovery = 0.6f; a.cooldown = 2.2f; a.minRange = 1.2f; a.maxRange = 4.5f;
                });
            });

            all["KirikYalan"] = Enemy("KirikYalan", d =>
            {
                d.displayName = "Kırık Yalan";
                d.difficulty = Difficulty.Kolay;
                d.description = "Çölü oluşturan milyonlarca kırık yalandan biri. Uzak durur, keskin kristaller fırlatır.";
                d.sprite = EnemyArt("KirikYalan", 12, 18, new Color32(150, 220, 240, 255), Shape.Box);
                d.maxHealth = 35f; d.moveSpeed = 2f;
                d.aggroRange = 7f; d.preferredDistance = 5f;
            }, d =>
            {
                Add<ProjectileAbility>(d, "Kristal Kıymık", a =>
                {
                    a.damage = 8f; a.speed = 7.5f;
                    a.windup = 0.5f; a.cooldown = 1.8f; a.minRange = 2f; a.maxRange = 9f;
                });
                Add<TeleportAbility>(d, "Kaçış", a =>
                {
                    a.destination = TeleportAbility.Destination.AwayFromPlayer; a.distance = 3.5f;
                    a.windup = 0.25f; a.recovery = 0.2f; a.cooldown = 4f; a.maxRange = 2.5f;
                });
            });

            all["HainGolge"] = Enemy("HainGolge", d =>
            {
                d.displayName = "Hain Gölge";
                d.difficulty = Difficulty.Orta;
                d.description = "Dostunu sırtından vuranların gölgesi. Arkana ışınlanıp hançerler.";
                d.sprite = EnemyArt("HainGolge", 12, 22, new Color32(70, 55, 90, 255), Shape.Figure);
                d.maxHealth = 70f; d.moveSpeed = 3f; d.poise = 18f; d.knockbackResistance = 0.3f;
                d.aggroRange = 6.5f; d.preferredDistance = 1.6f;
            }, d =>
            {
                Add<MeleeStrikeAbility>(d, "Hançer", a =>
                {
                    a.damage = 9f; a.hits = 2; a.reach = 0.8f; a.radius = 0.75f; a.timeBetweenHits = 0.3f;
                    a.windup = 0.45f; a.cooldown = 1.8f; a.maxRange = 1.8f;
                });
                var backstab = Add<MeleeStrikeAbility>(d, "Sırt Hançeri", a =>
                {
                    a.damage = 14f; a.windup = 0.35f; a.recovery = 0.5f;
                }, inMoveset: false);
                Add<TeleportAbility>(d, "Sırttan Vuruş", a =>
                {
                    a.destination = TeleportAbility.Destination.BehindPlayer; a.distance = 1.2f; a.followUp = backstab;
                    a.windup = 0.35f; a.recovery = 0.3f; a.cooldown = 6f; a.minRange = 2.5f; a.maxRange = 9f;
                });
            });

            all["Serap"] = Enemy("Serap", d =>
            {
                d.displayName = "Serap";
                d.difficulty = Difficulty.Orta;
                d.description = "Yanılsamaların efendisi. Kendi kopyalarını yaratır; gerçeğini bulmalısın.";
                d.sprite = EnemyArt("Serap", 12, 22, new Color32(225, 195, 130, 255), Shape.Figure);
                d.maxHealth = 55f; d.moveSpeed = 2.4f; d.poise = 10f;
                d.aggroRange = 7f; d.preferredDistance = 4.5f;
            }, d =>
            {
                Add<SummonIllusionsAbility>(d, "Serap", a =>
                {
                    a.count = 2; a.radius = 2.5f; a.lifetime = 9f;
                    a.windup = 0.6f; a.cooldown = 9f; a.maxRange = 9f;
                });
                Add<ProjectileAbility>(d, "Yelpaze", a =>
                {
                    a.damage = 7f; a.count = 3; a.spreadAngle = 40f; a.speed = 6.5f; a.color = new Color(1f, 0.85f, 0.5f);
                    a.windup = 0.6f; a.cooldown = 2.6f; a.minRange = 2f; a.maxRange = 8f;
                });
                Add<TeleportAbility>(d, "Kaybolma", a =>
                {
                    a.destination = TeleportAbility.Destination.NearPlayer; a.distance = 4f;
                    a.windup = 0.3f; a.cooldown = 5f; a.maxRange = 2f;
                });
            });

            all["SirBekcisi"] = Enemy("SirBekcisi", d =>
            {
                d.displayName = "Sır Bekçisi";
                d.difficulty = Difficulty.Zor;
                d.description = "Saklanan sırların taş bekçisi. Kalkanını ağır saldırıyla kır, yere vuruşlarından kaç.";
                d.sprite = EnemyArt("SirBekcisi", 24, 30, new Color32(120, 100, 80, 255), Shape.Box);
                d.scale = 1.3f; d.footSize = new Vector2(1.2f, 0.5f);
                d.maxHealth = 170f; d.moveSpeed = 1.5f; d.contactDamage = 8f; d.poise = 45f; d.knockbackResistance = 0.8f;
                d.aggroRange = 5.5f; d.preferredDistance = 1.8f; d.strafe = false;
            }, d =>
            {
                Add<GuardAbility>(d, "Taş Kalkan", a =>
                {
                    a.duration = 3f; a.damageTaken = 0.15f;
                    a.windup = 0.3f; a.cooldown = 7f; a.maxRange = 6f;
                });
                Add<AreaSlamAbility>(d, "Yer Sarsma", a =>
                {
                    a.damage = 18f; a.radius = 2f; a.waves = 2; a.waveGrowth = 1.6f;
                    a.windup = 0.85f; a.recovery = 0.8f; a.cooldown = 4.5f; a.maxRange = 2.5f;
                });
                Add<LungeAbility>(d, "Hücum", a =>
                {
                    a.damage = 20f; a.distance = 5f; a.speed = 10f; a.hitRadius = 0.9f;
                    a.windup = 0.9f; a.recovery = 0.9f; a.cooldown = 6f; a.minRange = 3f; a.maxRange = 7.5f;
                });
            });

            all["AynaKopya"] = Enemy("AynaKopya", d =>
            {
                d.displayName = "Ayna Kopya";
                d.difficulty = Difficulty.Zor;
                d.isBoss = true;
                d.description = "Çölün sonunda bekleyen sahte Börühan. Kendini kopyalar, korkularınla seni yavaşlatır ve vuruşlarını sana geri yansıtır.";
                d.sprite = EnemyArt("AynaKopya", 12, 22, new Color32(200, 225, 245, 255), Shape.Figure);
                d.scale = 1.1f;
                d.maxHealth = 320f; d.moveSpeed = 3.1f; d.poise = 60f; d.knockbackResistance = 0.85f;
                d.aggroRange = 6f; d.preferredDistance = 2f;
            }, d =>
            {
                Add<MeleeStrikeAbility>(d, "Ayna Kombosu", a =>
                {
                    a.damage = 10f; a.hits = 3; a.reach = 0.9f; a.radius = 0.85f; a.timeBetweenHits = 0.3f;
                    a.windup = 0.45f; a.recovery = 0.6f; a.cooldown = 2.4f; a.maxRange = 2f; a.weight = 1.5f;
                });
                Add<LungeAbility>(d, "Kristal Atılış", a =>
                {
                    a.damage = 15f; a.distance = 4.5f; a.speed = 13f;
                    a.windup = 0.5f; a.cooldown = 4f; a.minRange = 2.5f; a.maxRange = 6f;
                });
                Add<SummonIllusionsAbility>(d, "Kopyalar", a =>
                {
                    a.count = 3; a.radius = 3f; a.lifetime = 10f;
                    a.windup = 0.7f; a.cooldown = 14f; a.maxRange = 10f;
                });
                Add<CurseAbility>(d, "Korkuların", a =>
                {
                    a.range = 7f; a.slowMultiplier = 0.5f; a.slowDuration = 3f;
                    a.windup = 0.8f; a.cooldown = 11f; a.maxRange = 7f;
                    a.telegraphColor = new Color(0.5f, 0.2f, 0.7f, 0.25f);
                });
                Add<ReflectAbility>(d, "Ayna", a =>
                {
                    a.duration = 1.6f; a.reflectMultiplier = 1.5f;
                    a.windup = 0.3f; a.cooldown = 9f; a.maxRange = 3f; a.weight = 0.8f;
                });
                var cut = Add<MeleeStrikeAbility>(d, "Arkadan Kesik", a =>
                {
                    a.damage = 14f; a.windup = 0.35f; a.recovery = 0.5f;
                }, inMoveset: false);
                Add<TeleportAbility>(d, "Yansıma Adımı", a =>
                {
                    a.destination = TeleportAbility.Destination.BehindPlayer; a.distance = 1.3f; a.followUp = cut;
                    a.windup = 0.3f; a.cooldown = 7f; a.minRange = 3f; a.maxRange = 10f;
                });
            });

            return all;
        }

        /// <summary>Puts an enemy of the given kind into the active scene.</summary>
        public static GameObject PlaceEnemy(EnemyDefinition definition, Vector2 position, Transform parent)
        {
            var go = new GameObject(definition.displayName);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Health>();
            var enemy = go.AddComponent<EnemyController>();
            PrologueBuilder.Set(enemy, "definition", definition);
            PrologueBuilder.Set(enemy, "bodyRenderer", sr);
            go.AddComponent<WorldHealthBar>();
            enemy.ApplyDefinition();
            return go;
        }
    }
}
