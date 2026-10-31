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
            AssetDatabase.SaveAssets();
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

                var sword = PrologueBuilder.SpriteObject(root.transform, "Kilic",
                    PrologueBuilder.Art("Kilic", 3, 12, new Color32(205, 210, 220, 255), Shape.Box), new Vector2(0.45f, 0.2f), 1);
                var swordActivator = root.AddComponent<FlagActivator>();
                PrologueBuilder.Set(swordActivator, "flag", SwordFlag);
                PrologueBuilder.SetArray(swordActivator, "targets", sword);

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
