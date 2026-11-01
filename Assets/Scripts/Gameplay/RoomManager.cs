using Boruhan.Core;
using Boruhan.Story;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boruhan.Gameplay
{
    /// <summary>Places the player from the save (or a spawn point), tracks play time and writes the scene state to the save.</summary>
    public class RoomManager : MonoBehaviour
    {
        [SerializeField] string locationName = "Bilinmeyen Yer";
        [Tooltip("Underworld floor number. 0 = above ground (not shown in the save slot).")]
        [SerializeField] int floor;
        [SerializeField] PlayerController player;
        [Tooltip("Used when the save has no position and no matching SpawnPoint id.")]
        [SerializeField] Transform defaultSpawn;

        public static RoomManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;

            var sceneName = SceneManager.GetActiveScene().name;
            if (!GameSession.IsActive)
                GameSession.StartDebugSession(sceneName);

            var data = GameSession.Data;
            if (data.sceneName != sceneName)
                data.hasPosition = false;

            data.sceneName = sceneName;
            data.locationName = locationName;
            data.floor = floor;

            player.Teleport(data.hasPosition ? data.playerPosition : FindSpawn(data.spawnId));
        }

        Vector2 FindSpawn(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (var spawn in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude))
                {
                    if (spawn.id == id)
                        return spawn.transform.position;
                }
                Debug.LogWarning($"[Oda] '{id}' adlı SpawnPoint bulunamadı, varsayılan başlangıç noktası kullanılıyor.");
            }
            return defaultSpawn.position;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (Time.timeScale > 0f)
                GameSession.Data.playTimeSeconds += Time.unscaledDeltaTime;
        }

        public bool SaveGame()
        {
            var data = GameSession.Data;
            data.hasPosition = true;
            data.playerPosition = player.transform.position;
            return GameSession.Save();
        }

        void OnApplicationQuit() => SaveGame();
    }
}
