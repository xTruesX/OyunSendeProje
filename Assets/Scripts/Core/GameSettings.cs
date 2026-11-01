using System.Collections.Generic;
using Boruhan.Save;
using UnityEngine;

namespace Boruhan.Core
{
    /// <summary>Game-wide settings, edited in Börühan Stüdyo (Assets/Resources/OyunAyarlari).</summary>
    [CreateAssetMenu(menuName = "Börühan/Oyun Ayarları", fileName = "OyunAyarlari")]
    public class GameSettings : ScriptableObject
    {
        [Tooltip("The scene a new game starts in.")]
        public string newGameScene = SceneNames.NewGame;
        [Tooltip("SpawnPoint id in that scene. Empty = the scene's default start.")]
        public string newGameSpawn = "";
        public string newGameLocationName = "Giriş";
        [Tooltip("Story flags already set when a new game starts.")]
        public List<string> startingFlags = new List<string>();

        static GameSettings instance;

        public static GameSettings Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<GameSettings>("OyunAyarlari");
                if (instance == null)
                    instance = CreateInstance<GameSettings>();
                return instance;
            }
        }

        public void ApplyToNewGame(SaveData data)
        {
            if (!string.IsNullOrEmpty(newGameScene))
                data.sceneName = newGameScene;
            data.spawnId = newGameSpawn;
            data.locationName = newGameLocationName;
            foreach (var flag in startingFlags)
            {
                if (!string.IsNullOrEmpty(flag) && !data.flags.Contains(flag))
                    data.flags.Add(flag);
            }
        }
    }
}
