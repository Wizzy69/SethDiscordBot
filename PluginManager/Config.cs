﻿using PluginManager.Others;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PluginManager
{
    internal class AppConfig
    {
        public Dictionary<string, object> ApplicationVariables { get; set; }
        public List<string>               ProtectedKeyWords    { get; set; }
    }

    public static class Config
    {
        private static AppConfig appConfig;

        public static bool AddValueToVariables<T>(string key, T value, bool isProtected)
        {
            if (appConfig.ApplicationVariables.ContainsKey(key)) return false;
            if (value == null) return false;
            appConfig.ApplicationVariables.Add(key, value);
            if (isProtected) appConfig.ProtectedKeyWords.Add(key);
            SaveConfig();
            return true;
        }

        public static T? GetValue<T>(string key)
        {
            if (!appConfig.ApplicationVariables.ContainsKey(key)) return default;
            try
            {
                JsonElement element = (JsonElement)appConfig.ApplicationVariables[key];
                return element.Deserialize<T>();
            }
            catch
            {
                return (T)appConfig.ApplicationVariables[key];
            }
        }

        public static bool SetValue<T>(string key, T value)
        {
            if (!appConfig.ApplicationVariables.ContainsKey(key)) return false;
            if (appConfig.ProtectedKeyWords.Contains(key)) return false;
            if (value == null) return false;

            appConfig.ApplicationVariables[key] = JsonSerializer.SerializeToElement(value);
            SaveConfig();
            return true;
        }

        public static bool RemoveKey(string key)
        {
            appConfig.ApplicationVariables.Remove(key);
            appConfig.ProtectedKeyWords.Remove(key);
            SaveConfig();
            return true;
        }

        public static async void SaveConfig()
        {
            string path = Functions.dataFolder + "config.json";
            await Functions.SaveToJsonFile<AppConfig>(path, appConfig);
        }

        public static async Task LoadConfig()
        {
            string path = Functions.dataFolder + "config.json";
            if (File.Exists(path))
            {
                appConfig = await Functions.ConvertFromJson<AppConfig>(path);
                Functions.WriteLogFile($"Loaded {appConfig.ApplicationVariables.Keys.Count} application variables.\nLoaded {appConfig.ProtectedKeyWords.Count} readonly variables.");
            }
            else
                appConfig = new() { ApplicationVariables = new Dictionary<string, object>(), ProtectedKeyWords = new List<string>() };
        }

        public static bool ContainsValue<T>(T value) => appConfig.ApplicationVariables.ContainsValue(value!);
        public static bool ContainsKey(string key)   => appConfig.ApplicationVariables.ContainsKey(key);

        public static Dictionary<string, object> GetAllVariables() => new(appConfig.ApplicationVariables);
    }
}
