using System;
using Boruhan.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boruhan.Core
{
    /// <summary>Holds the save slot and data for the game currently being played. Survives scene loads.</summary>
    public static class GameSession
    {
        public static int Slot { get; private set; } = -1;
        public static SaveData Data { get; private set; }

        public static bool IsActive => Data != null;

        /// <summary>False for debug sessions (a scene played directly in the editor).</summary>
        public static bool CanSave => IsActive && Slot >= 0;

        public static event Action<string> FlagChanged;

        // Play Mode starts without a domain reload in this project, so clear the previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Slot = -1;
            Data = null;
            FlagChanged = null;
        }

        /// <summary>Continues the slot if it has a save, otherwise starts a new game in it.</summary>
        public static bool StartSlot(int slot)
        {
            SaveData data;
            if (SaveSystem.Exists(slot))
            {
                data = SaveSystem.Load(slot);
                if (data == null)
                    return false;
            }
            else
            {
                data = SaveData.CreateNew();
                SaveSystem.Save(slot, data);
            }

            if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
            {
                Debug.LogWarning($"[Kayıt] '{data.sceneName}' sahnesi bulunamadı, oyunun başına dönülüyor.");
                data.sceneName = SceneNames.NewGame;
                data.hasPosition = false;
            }

            data.flags ??= new System.Collections.Generic.List<string>();
            Slot = slot;
            Data = data;
            SceneManager.LoadScene(data.sceneName);
            return true;
        }

        public static void StartDebugSession(string sceneName)
        {
            Slot = -1;
            Data = SaveData.CreateNew();
            Data.sceneName = sceneName;
            Debug.Log("[Kayıt] Sahne doğrudan açıldı; test modunda kayıt yapılmaz. Ana menüden başlatın.");
        }

        public static bool Save() => CanSave && SaveSystem.Save(Slot, Data);

        public static void End()
        {
            Slot = -1;
            Data = null;
        }

        public static bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && Data != null && Data.flags.Contains(flag);
        }

        public static void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || Data == null || Data.flags.Contains(flag))
                return;

            Data.flags.Add(flag);
            FlagChanged?.Invoke(flag);
        }

        /// <summary>Moves to another scene, arriving at the given SpawnPoint. Saves on the way (checkpoint).</summary>
        public static void TravelTo(string sceneName, string spawnId)
        {
            if (Data != null)
            {
                Data.sceneName = sceneName;
                Data.spawnId = spawnId;
                Data.hasPosition = false;
                Save();
            }
            ScreenFader.FadeToScene(sceneName);
        }
    }
}
