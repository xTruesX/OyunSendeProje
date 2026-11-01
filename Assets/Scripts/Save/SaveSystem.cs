using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Boruhan.Save
{
    /// <summary>Reads and writes save slots as JSON files under persistentDataPath/Kayitlar.</summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        static string Directory => Path.Combine(Application.persistentDataPath, "Kayitlar");

        static string PathFor(int slot) => Path.Combine(Directory, $"kayit_{slot + 1}.json");

        public static bool Exists(int slot) => File.Exists(PathFor(slot));

        /// <summary>Returns null if the slot is empty or the file can't be read.</summary>
        public static SaveData Load(int slot)
        {
            var path = PathFor(slot);
            if (!File.Exists(path))
                return null;

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || string.IsNullOrEmpty(data.sceneName))
                    throw new InvalidDataException("Kayıt dosyası boş ya da eksik.");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Kayıt] {path} okunamadı: {e.Message}");
                return null;
            }
        }

        public static bool Save(int slot, SaveData data)
        {
            var path = PathFor(slot);
            var tempPath = path + ".tmp";

            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                data.lastSavedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

                // Write to a temp file first so a crash mid-write can't corrupt the existing save.
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Kayıt] {path} yazılamadı: {e.Message}");
                return false;
            }
        }

        public static void Delete(int slot)
        {
            var path = PathFor(slot);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
