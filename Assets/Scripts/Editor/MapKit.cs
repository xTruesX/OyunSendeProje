using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boruhan.Audio;
using Boruhan.Gameplay;
using Boruhan.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Shape = Boruhan.EditorTools.PrologueBuilder.Shape;

namespace Boruhan.EditorTools
{
    /// <summary>
    /// Tilesets, palettes, room themes, placeholder music, and the tilemap layout every room uses:
    ///   Harita/Zemin    – floor, no collision
    ///   Harita/Dekor    – paths, grass, details, no collision
    ///   Harita/Engeller – anything solid (mountains, rocks, walls); y-sorted with the player
    /// Everything is created only if missing, so replacing PNGs / WAVs / tiles is safe.
    /// </summary>
    public static class MapKit
    {
        const string TileArtDir = "Assets/Art/Tiles";
        const string TileDir = "Assets/Tiles";
        const string PaletteDir = "Assets/Tiles/Paletler";
        const string ThemeDir = "Assets/Tiles/Temalar";
        const string MusicFileDir = "Assets/Audio/Muzik";
        const string TrackDir = "Assets/Audio/Parcalar";

        public const string LayerFloor = "Zemin";
        public const string LayerDecor = "Dekor";
        public const string LayerSolid = "Engeller";

        // ================= Assets =================

        public static void EnsureAssets()
        {
            EnsureTilesets();
            EnsureMusic();
            EnsureThemes();
        }

        static void EnsureTilesets()
        {
            CreateSet("Bozkir",
                ("Kar", 16, 16, new Color32(170, 180, 200, 255), Shape.Tile, false, false),
                ("KarKoyu", 16, 16, new Color32(145, 155, 182, 255), Shape.Tile, false, false),
                ("Yol", 16, 16, new Color32(120, 100, 80, 255), Shape.Tile, false, false),
                ("Buz", 16, 16, new Color32(170, 210, 235, 255), Shape.Tile, false, false),
                ("Cimen", 16, 16, new Color32(90, 115, 80, 255), Shape.Circle, false, false),
                ("Kaya", 16, 14, new Color32(95, 95, 105, 255), Shape.Box, true, false),
                ("KuruAgac", 16, 40, new Color32(45, 35, 35, 255), Shape.Box, true, true),
                ("Dag", 16, 16, new Color32(62, 62, 74, 255), Shape.Tile, true, false),
                ("DagZirvesi", 32, 48, new Color32(92, 92, 108, 255), Shape.Mountain, true, true),
                ("DagZirvesiBuyuk", 32, 64, new Color32(82, 82, 98, 255), Shape.Mountain, true, true));

            CreateSet("Magara",
                ("MagaraZemin", 16, 16, new Color32(55, 45, 40, 255), Shape.Tile, false, false),
                ("MagaraZemin2", 16, 16, new Color32(68, 55, 48, 255), Shape.Tile, false, false),
                ("Lav", 16, 16, new Color32(230, 90, 30, 255), Shape.Tile, true, false),
                ("MagaraDuvari", 16, 16, new Color32(32, 27, 27, 255), Shape.Tile, true, false),
                ("Sarkit", 20, 40, new Color32(80, 70, 65, 255), Shape.Box, true, true));

            CreateSet("Gok",
                ("Bulut", 16, 16, new Color32(225, 225, 240, 255), Shape.Tile, false, false),
                ("Bulut2", 16, 16, new Color32(208, 208, 230, 255), Shape.Tile, false, false),
                ("Yildiz", 16, 16, new Color32(255, 230, 150, 255), Shape.Circle, false, false),
                ("BulutDuvari", 16, 16, new Color32(245, 245, 255, 255), Shape.Tile, true, false),
                ("BulutKulesi", 32, 40, new Color32(250, 250, 255, 255), Shape.Box, true, true));

            CreateSet("IcMekan",
                ("Kece", 16, 16, new Color32(110, 45, 40, 255), Shape.Tile, false, false),
                ("Tahta", 16, 16, new Color32(130, 95, 60, 255), Shape.Tile, false, false),
                ("Hali", 16, 16, new Color32(150, 50, 60, 255), Shape.Box, false, false),
                ("Duvar", 16, 16, new Color32(120, 90, 60, 255), Shape.Tile, true, false));
        }

        /// <summary>Creates each tile (PNG + Tile asset) if missing, then a Tile Palette containing them.</summary>
        static void CreateSet(string set, params (string name, int w, int h, Color color, Shape shape, bool solid, bool tall)[] defs)
        {
            Directory.CreateDirectory($"{TileDir}/{set}");
            var tiles = new List<TileBase>();
            foreach (var d in defs)
            {
                var path = $"{TileDir}/{set}/{d.name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    // Tall tiles stand on their cell: the bottom of the sprite sits on the bottom of the cell.
                    var pivot = d.tall ? new Vector2(0.5f, 8f / d.h) : new Vector2(0.5f, 0.5f);
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = PrologueBuilder.Art($"{TileArtDir}/{set}", d.name, d.w, d.h, d.color, d.shape, pivot);
                    tile.colliderType = d.solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                    AssetDatabase.CreateAsset(tile, path);
                }
                tiles.Add(tile);
            }
            CreatePalette(set, tiles);
        }

        static void CreatePalette(string set, List<TileBase> tiles)
        {
            Directory.CreateDirectory(PaletteDir);
            var path = $"{PaletteDir}/{set}.prefab";
            if (File.Exists(path))
                return;

            var root = new GameObject(set, typeof(Grid));
            var layer = new GameObject("Layer1", typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(root.transform, false);
            var tilemap = layer.GetComponent<Tilemap>();
            const int perRow = 5;
            for (int i = 0; i < tiles.Count; i++)
                tilemap.SetTile(new Vector3Int((i % perRow) * 2, -(i / perRow) * 3, 0), tiles[i]);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            var palette = ScriptableObject.CreateInstance<GridPalette>();
            palette.name = "Palette Settings";
            palette.cellSizing = GridPalette.CellSizing.Automatic;
            AssetDatabase.AddObjectToAsset(palette, prefab);
            AssetDatabase.SaveAssets();
        }

        public static Tile GetTile(string set, string name) => AssetDatabase.LoadAssetAtPath<Tile>($"{TileDir}/{set}/{name}.asset");

        static void EnsureMusic()
        {
            Track("AnaMenu", 110, 165);
            Track("Sinematik", 98, 147);
            Track("Otag", 131, 196);
            Track("Bozkir", 147, 220);
            Track("Gok", 196, 294);
            Track("Magara", 82, 123);
            Track("Kat1", 117, 175);
        }

        public static MusicTrack GetTrack(string name) => AssetDatabase.LoadAssetAtPath<MusicTrack>($"{TrackDir}/{name}.asset");

        /// <summary>Placeholder: a quiet two-note drone that loops seamlessly. Replace the clip in the track asset.</summary>
        static void Track(string name, int lowHz, int highHz)
        {
            Directory.CreateDirectory(MusicFileDir);
            Directory.CreateDirectory(TrackDir);

            var wavPath = $"{MusicFileDir}/{name}.wav";
            if (!File.Exists(wavPath))
            {
                WriteDrone(wavPath, lowHz, highHz);
                AssetDatabase.ImportAsset(wavPath);
                var importer = (AudioImporter)AssetImporter.GetAtPath(wavPath);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            var trackPath = $"{TrackDir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<MusicTrack>(trackPath) != null)
                return;
            var track = ScriptableObject.CreateInstance<MusicTrack>();
            track.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);
            track.volume = 0.5f;
            AssetDatabase.CreateAsset(track, trackPath);
        }

        static void WriteDrone(string path, int lowHz, int highHz)
        {
            const int rate = 22050;
            const int seconds = 6;
            int samples = rate * seconds;
            using var writer = new BinaryWriter(File.Create(path));

            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + samples * 2);
            writer.Write("WAVEfmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);      // PCM
            writer.Write((short)1);      // mono
            writer.Write(rate);
            writer.Write(rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(samples * 2);

            // Whole-number frequencies over a whole number of seconds -> the loop point has no click.
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                float v = (Mathf.Sin(2f * Mathf.PI * lowHz * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * highHz * t)) * 0.06f * swell;
                writer.Write((short)(Mathf.Clamp(v, -1f, 1f) * short.MaxValue));
            }
        }

        static void EnsureThemes()
        {
            Theme("Bozkir", GetTile("Bozkir", "Kar"), GetTile("Bozkir", "Dag"),
                new TileBase[] { GetTile("Bozkir", "DagZirvesi"), GetTile("Bozkir", "DagZirvesiBuyuk") }, 6,
                new Color(0.02f, 0.03f, 0.07f), new Color(0.7f, 0.78f, 1f), 0.5f, 6f, GetTrack("Bozkir"));
            Theme("Magara", GetTile("Magara", "MagaraZemin"), GetTile("Magara", "MagaraDuvari"),
                new TileBase[] { GetTile("Magara", "Sarkit") }, 4,
                Color.black, new Color(0.6f, 0.6f, 0.8f), 0.08f, 5f, GetTrack("Magara"));
            Theme("Gok", GetTile("Gok", "Bulut"), GetTile("Gok", "BulutDuvari"),
                new TileBase[] { GetTile("Gok", "BulutKulesi") }, 3,
                new Color(0.16f, 0.18f, 0.32f), new Color(1f, 0.97f, 0.9f), 1f, 5f, GetTrack("Gok"));
            Theme("IcMekan", GetTile("IcMekan", "Kece"), GetTile("IcMekan", "Duvar"),
                new TileBase[0], 2,
                new Color(0.04f, 0.03f, 0.03f), new Color(1f, 0.85f, 0.7f), 0.5f, 5f, GetTrack("Otag"));
        }

        public static RoomTheme GetTheme(string name) => AssetDatabase.LoadAssetAtPath<RoomTheme>($"{ThemeDir}/{name}.asset");

        static void Theme(string name, TileBase floor, TileBase fill, TileBase[] edge, int thickness,
            Color background, Color light, float intensity, float cameraSize, MusicTrack music)
        {
            Directory.CreateDirectory(ThemeDir);
            var path = $"{ThemeDir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<RoomTheme>(path) != null)
                return;

            var theme = ScriptableObject.CreateInstance<RoomTheme>();
            theme.floor = floor;
            theme.borderFill = fill;
            theme.borderEdge = edge;
            theme.borderThickness = thickness;
            theme.background = background;
            theme.lightColor = light;
            theme.lightIntensity = intensity;
            theme.cameraSize = cameraSize;
            theme.music = music;
            AssetDatabase.CreateAsset(theme, path);
        }

        // ================= Building maps =================

        /// <summary>Adds the Harita grid to the active scene and paints the room floor plus its border.</summary>
        public static Grid CreateRoomMap(Rect area, RoomTheme theme, CameraFollow camera)
        {
            var grid = new GameObject("Harita", typeof(Grid)).GetComponent<Grid>();
            var floor = Layer(grid, LayerFloor, -100, false);
            Layer(grid, LayerDecor, -90, false);
            var solid = Layer(grid, LayerSolid, 0, true);

            var room = new RectInt(Mathf.FloorToInt(area.xMin), Mathf.FloorToInt(area.yMin), Mathf.RoundToInt(area.width), Mathf.RoundToInt(area.height));
            int t = theme.borderThickness;
            for (int x = room.xMin - t; x < room.xMax + t; x++)
            for (int y = room.yMin - t; y < room.yMax + t; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                floor.SetTile(cell, theme.floor);
                if (room.Contains(new Vector2Int(x, y)))
                    continue;

                bool innerEdge = x >= room.xMin - 1 && x <= room.xMax && y >= room.yMin - 1 && y <= room.yMax;
                bool hasEdge = theme.borderEdge != null && theme.borderEdge.Length > 0;
                var tile = innerEdge && hasEdge
                    ? theme.borderEdge[(((x * 73856093) ^ (y * 19349663)) & 0x7fffffff) % theme.borderEdge.Length]
                    : theme.borderFill;
                solid.SetTile(cell, tile);
            }
            RebuildCollision(solid);

            if (camera != null)
            {
                // Let the camera show part of the border so the room feels enclosed by it.
                float margin = Mathf.Max(1f, t - 1);
                PrologueBuilder.Set(camera, "bounds", new Rect(area.xMin - margin, area.yMin - margin, area.width + margin * 2f, area.height + margin * 2f + 1f));
            }
            return grid;
        }

        static Tilemap Layer(Grid grid, string name, int order, bool solid)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid.transform, false);
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            // Solid things are sorted one by one so the player can walk in front of / behind them.
            renderer.mode = solid ? TilemapRenderer.Mode.Individual : TilemapRenderer.Mode.Chunk;

            if (solid)
            {
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                var composite = go.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            }
            return go.GetComponent<Tilemap>();
        }

        /// <summary>
        /// The merged collision shape is saved with the scene. When tiles are painted from code it can be left
        /// empty, so toggle the merge to force Unity to rebuild it from the current tiles.
        /// </summary>
        public static void RebuildCollision(Tilemap solid)
        {
            var tilemapCollider = solid.GetComponent<TilemapCollider2D>();
            var composite = solid.GetComponent<CompositeCollider2D>();
            if (tilemapCollider == null || composite == null)
                return;

            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.None;
            tilemapCollider.ProcessTilemapChanges();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.GenerateGeometry();
        }

        public static void AddSceneMusic(MusicTrack track)
        {
            var music = new GameObject("SahneMuzigi").AddComponent<SceneMusic>();
            PrologueBuilder.Set(music, "track", track);
        }

        /// <summary>Creates a new, playable room scene from a theme and adds it to the build.</summary>
        public static string CreateRoomScene(string sceneName, string locationName, int floor, Vector2Int size, RoomTheme theme)
        {
            EnsureAssets();
            var path = $"{PrologueBuilder.SceneDir}/{sceneName}.unity";
            var prefabs = PrologueBuilder.CreatePrefabs();

            var scene = PrologueBuilder.BeginScene();
            var area = new Rect(-size.x / 2, -size.y / 2, size.x, size.y);
            var spawn = new Vector2(0f, area.yMin + 1.5f);
            var ctx = PrologueBuilder.GameplayScene(prefabs, locationName, area, spawn, theme.background, theme.lightIntensity, theme.lightColor, theme.cameraSize);
            var manager = scene.GetRootGameObjects().Select(r => r.GetComponent<RoomManager>()).First(m => m != null);
            PrologueBuilder.Set(manager, "floor", floor);
            CreateRoomMap(area, theme, ctx.camera);
            PrologueBuilder.Spawn(ctx.world, "giris", spawn);
            AddSceneMusic(theme.music);

            EditorSceneManager.SaveScene(scene, path);
            PrologueBuilder.CloseScene(scene);
            AddToBuild(path);
            return path;
        }

        public static void AddToBuild(string path)
        {
            if (EditorBuildSettings.scenes.Any(s => s.path == path))
                return;
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(path, true)).ToArray();
        }

        // ================= One-time upgrade of the prologue scenes =================

        /// <summary>Gives the existing prologue scenes tilemap floors, mountain/rock borders and music. Safe to run twice.</summary>
        public static void UpgradePrologueScenes()
        {
            EnsureAssets();
            UpgradeScene("Yeryuzu", GetTheme("Bozkir"), new Rect(-32f, -22f, 64f, 44f), GetTrack("Bozkir"));
            UpgradeScene("Magara", GetTheme("Magara"), new Rect(-5f, -15f, 10f, 30f), GetTrack("Magara"));
            UpgradeScene("Otag", null, default, GetTrack("Otag"));
            UpgradeScene("GokKati", null, default, GetTrack("Gok"));
            UpgradeScene("GirisSinematigi", null, default, GetTrack("Sinematik"));
            UpgradeScene("Kat1Giris", null, default, GetTrack("Kat1"));
            UpgradeScene("AnaMenu", null, default, GetTrack("AnaMenu"));
        }

        static void UpgradeScene(string name, RoomTheme theme, Rect area, MusicTrack music)
        {
            var path = $"{PrologueBuilder.SceneDir}/{name}.unity";
            if (!File.Exists(path))
                return;

            var openScene = SceneManager.GetSceneByPath(path);
            bool wasOpen = openScene.IsValid() && openScene.isLoaded;
            if (wasOpen && openScene.isDirty)
            {
                Debug.LogWarning($"[Börühan] {name} açık ve kaydedilmemiş değişiklikleri var; atlandı. Kaydedip tekrar çalıştırın.");
                return;
            }

            var previous = SceneManager.GetActiveScene();
            var scene = wasOpen ? openScene : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var roots = scene.GetRootGameObjects();

            if (theme != null && !roots.Any(r => r.name == "Harita"))
            {
                // The old single-sprite floor is replaced by the painted tilemap.
                var world = roots.FirstOrDefault(r => r.name == "Dunya");
                var oldFloor = world != null ? world.transform.Find("Zemin") : null;
                if (oldFloor != null)
                    Object.DestroyImmediate(oldFloor.gameObject);

                var camera = roots.Select(r => r.GetComponent<CameraFollow>()).FirstOrDefault(c => c != null);
                CreateRoomMap(area, theme, camera);
            }

            if (!roots.Any(r => r.GetComponentInChildren<SceneMusic>(true) != null))
                AddSceneMusic(music);

            EditorSceneManager.SaveScene(scene);
            if (previous.IsValid())
                SceneManager.SetActiveScene(previous);
            if (!wasOpen)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
