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

        public const string TestFlagsKey = "Boruhan.TestBayraklari";

        // Play Mode starts without a domain reload in this project, so clear the previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Slot = -1;
            Data = null;
            FlagChanged = null;
            VariableChanged = null;
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
                GameSettings.Instance.ApplyToNewGame(data);
                SaveSystem.Save(slot, data);
            }

            if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
            {
                Debug.LogWarning($"[Kayıt] '{data.sceneName}' sahnesi bulunamadı, oyunun başına dönülüyor.");
                data.sceneName = SceneNames.NewGame;
                data.hasPosition = false;
            }

            data.flags ??= new System.Collections.Generic.List<string>();
            data.variables ??= new System.Collections.Generic.List<Save.VariableEntry>();
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
#if UNITY_EDITOR
            // Test flags set in Börühan Stüdyo, so a scene can be played directly in the right story state.
            foreach (var flag in UnityEditor.EditorPrefs.GetString(TestFlagsKey, "").Split(','))
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    Data.flags.Add(flag.Trim());
            }
#endif
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

        /// <summary>How many flags start with the prefix (e.g. collected "can_parcasi_" items).</summary>
        public static int CountFlags(string prefix)
        {
            if (Data == null)
                return 0;
            int count = 0;
            foreach (var flag in Data.flags)
            {
                if (flag.StartsWith(prefix))
                    count++;
            }
            return count;
        }

        public static event Action<string> VariableChanged;

        public static void ClearFlag(string flag)
        {
            if (Data != null && Data.flags.Remove(flag))
                FlagChanged?.Invoke(flag);
        }

        public static int GetVariable(string key)
        {
            if (Data == null || string.IsNullOrEmpty(key))
                return 0;
            var entry = Data.variables.Find(v => v.key == key);
            return entry != null ? entry.value : 0;
        }

        public static void SetVariable(string key, int value)
        {
            if (Data == null || string.IsNullOrEmpty(key))
                return;
            var entry = Data.variables.Find(v => v.key == key);
            if (entry == null)
                Data.variables.Add(entry = new Save.VariableEntry { key = key });
            entry.value = value;
            VariableChanged?.Invoke(key);
        }

        public static void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || Data == null || Data.flags.Contains(flag))
                return;

            Data.flags.Add(flag);
            FlagChanged?.Invoke(flag);
        }

        /// <summary>
        /// After dying: reloads the slot's last save (save point, pause-menu save or room change) with full
        /// health, dropping everything since. In test mode (no save) the current room restarts instead.
        /// </summary>
        public static void RetryFromLastSave()
        {
            if (CanSave && SaveSystem.Exists(Slot))
            {
                var data = SaveSystem.Load(Slot);
                if (data != null)
                {
                    data.flags ??= new System.Collections.Generic.List<string>();
                    data.variables ??= new System.Collections.Generic.List<Save.VariableEntry>();
                    Data = data;
                }
            }
            if (Data == null)
                return;
            if (!CanSave)
            {
                Data.sceneName = SceneManager.GetActiveScene().name;
                Data.hasPosition = false;
            }
            Data.playerHealth = -1f;
            ScreenFader.FadeToScene(Data.sceneName);
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
