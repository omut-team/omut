using System;
using System.IO;
using UnityEngine;

namespace Omut.Core
{
    // Запись и чтение сохранения в файл. Один слот.
    // Windows: C:\Users\<имя>\AppData\LocalLow\<компания>\<игра>\omut_save.json
    public static class SaveSystem
    {
        public const string FileName = "omut_save.json";

        // Путь можно подменить (для тестов).
        public static string FolderOverride;

        public static string FilePath => Path.Combine(FolderOverride ?? Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(FilePath);

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        // Прочитать из текста. Битый файл = null.
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                return data != null && data.version > 0 ? data : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static bool Save(SaveData data)
        {
            try
            {
                data.savedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // Сначала во временный файл, потом подмена: если игра упадёт посреди записи, старое сохранение цело.
                string temp = path + ".tmp";
                File.WriteAllText(temp, ToJson(data));
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Не удалось сохранить: " + e.Message);
                return false;
            }
        }

        public static SaveData Load()
        {
            try
            {
                return Exists() ? FromJson(File.ReadAllText(FilePath)) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Не удалось прочитать сохранение: " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            if (Exists())
                File.Delete(FilePath);
        }
    }
}
