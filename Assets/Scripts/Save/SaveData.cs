using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Boruhan.Save
{
    /// <summary>Everything that gets written to a save slot. Serialized with JsonUtility.</summary>
    [Serializable]
    public class VariableEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 2;

        public string sceneName;
        public string locationName;
        public int floor = 1;

        public bool hasPosition;
        public Vector2 playerPosition;
        /// <summary>SpawnPoint id to place the player at when entering sceneName (used when hasPosition is false).</summary>
        public string spawnId;

        /// <summary>Börühan's current health. Below 0 = full.</summary>
        public float playerHealth = -1f;

        /// <summary>Story progress. See Core.StoryFlags for the names used.</summary>
        public List<string> flags = new List<string>();

        /// <summary>Whole-number variables set by events (Değişken komutları).</summary>
        public List<VariableEntry> variables = new List<VariableEntry>();

        public float playTimeSeconds;
        public string lastSavedUtc;

        public static SaveData CreateNew()
        {
            return new SaveData
            {
                sceneName = Core.SceneNames.NewGame,
                locationName = "Giriş",
                floor = 0,
            };
        }

        public string FormatPlayTime()
        {
            var t = TimeSpan.FromSeconds(playTimeSeconds);
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        public string FormatLastSaved()
        {
            if (DateTime.TryParse(lastSavedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc))
                return utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            return "-";
        }
    }
}
