#if UNITY_EDITOR


using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace GabSith.WFT
{
    public class ProjectSettingsManager
    {
        private const string SettingsFileName = "WFTProjectSettings.json";

        private static Dictionary<string, string> settingsCache;
        private static string resourcesFolderPath;

        public static string defaultPath = "Assets/WF Tools - GabSith/Generated";
        public static bool useHeader = true;

        static ProjectSettingsManager()
        {
            InitializeResourcesFolder();
            LoadSettings();
        }

        private static void InitializeResourcesFolder()
        {
            string scriptPath = GetScriptPath();
            if (string.IsNullOrEmpty(scriptPath))
            {
                Debug.LogError("Could not find ProjectSettingsManager script");
                return;
            }

            string scriptDirectory = Path.GetDirectoryName(scriptPath);
            resourcesFolderPath = Path.Combine(scriptDirectory, "Resources");

            if (!Directory.Exists(resourcesFolderPath))
            {
                Directory.CreateDirectory(resourcesFolderPath);
                AssetDatabase.Refresh();
            }
        }

        private static void LoadSettings()
        {
            settingsCache = new Dictionary<string, string>();
            string fullPath = Path.Combine(resourcesFolderPath, SettingsFileName);

            if (File.Exists(fullPath))
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    var serializableDictionary = JsonUtility.FromJson<SerializableDictionary>(json);

                    if (serializableDictionary != null &&
                        serializableDictionary.keys != null &&
                        serializableDictionary.values != null &&
                        serializableDictionary.keys.Count == serializableDictionary.values.Count)
                    {
                        settingsCache = serializableDictionary.ToDictionary();
                    }
                    else
                    {
                        Debug.LogWarning("ProjectSettingsManager: Settings file was in an invalid format. Creating a new one.");
                        SaveSettings();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"ProjectSettingsManager: Error loading settings: {e.Message}");
                    SaveSettings();
                }
            }
            else
            {
                SaveSettings();
            }
        }

        private static void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(new SerializableDictionary(settingsCache), true);
                string fullPath = Path.Combine(resourcesFolderPath, SettingsFileName);
                File.WriteAllText(fullPath, json);
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ProjectSettingsManager: Error saving settings: {e.Message}");
            }
        }

        public static void SetString(string key, string value)
        {
            settingsCache[key] = value;
            SaveSettings();
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return settingsCache.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public static void SetBool(string key, bool value)
        {
            settingsCache[key] = value.ToString();
            SaveSettings();
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (settingsCache.TryGetValue(key, out string value))
            {
                return bool.TryParse(value, out bool result) ? result : defaultValue;
            }
            return defaultValue;
        }

        public static void SetColor(string key, Color value)
        {
            string colorString = $"{value.r}-{value.g}-{value.b}-{value.a}";
            SetString(key, colorString);
        }

        public static Color GetColor(string key, Color defaultValue)
        {
            string colorString = GetString(key, $"{defaultValue.r}-{defaultValue.g}-{defaultValue.b}-{defaultValue.a}");

            string[] components = colorString.Split('-');

            if (components.Length == 4 &&
                float.TryParse(components[0], out float r) &&
                float.TryParse(components[1], out float g) &&
                float.TryParse(components[2], out float b) &&
                float.TryParse(components[3], out float a))
            {
                return new Color(r, g, b, a);
            }
            return defaultValue;
        }

        public static bool EditorGUIBool(string key, string label, bool defaultValue = false)
        {
            bool value = GetBool(key, defaultValue);
            bool newValue = EditorGUILayout.ToggleLeft(label, value);
            if (newValue != value)
            {
                SetBool(key, newValue);
            }
            return newValue;
        }

        public static Color EditorGUIColor(string key, string label, Color defaultValue)
        {
            Color value = GetColor(key, defaultValue);
            Color newValue = EditorGUILayout.ColorField(new GUIContent(label), value, true, true, true);
            if (newValue != value)
            {
                SetColor(key, newValue);
            }
            return newValue;
        }



        public static bool DeleteKey(string key)
        {
            bool removed = settingsCache.Remove(key);
            if (removed)
            {
                SaveSettings();
            }
            return removed;
        }

        public static void DeleteAllData()
        {
            settingsCache.Clear();

            string fullPath = Path.Combine(resourcesFolderPath, SettingsFileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log("ProjectSettingsManager: All saved data has been deleted.");
            }
            else
            {
                Debug.Log("ProjectSettingsManager: No saved data file found to delete.");
            }
            AssetDatabase.Refresh();
        }

        public static bool HasKey(string key)
        {
            return settingsCache.ContainsKey(key);
        }

        private static string GetScriptPath()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script ProjectSettingsManager");
            if (guids.Length == 0)
            {
                return null;
            }
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        [System.Serializable]
        private class SerializableDictionary
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();

            public SerializableDictionary() { }

            public SerializableDictionary(Dictionary<string, string> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }

            public Dictionary<string, string> ToDictionary()
            {
                var dictionary = new Dictionary<string, string>();
                for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
                {
                    dictionary[keys[i]] = values[i];
                }
                return dictionary;
            }
        }
    }
}

#endif