using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WinClock
{
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _currentStrings = new Dictionary<string, string>();
        private static string _currentLanguage = "en";

        public static void LoadLanguage(string lang)
        {
            SetLanguage(lang);
        }

        public static void SetLanguage(string lang)
        {
            try
            {
                _currentLanguage = lang;
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Localization", $"{lang}.json");
                
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _currentStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }
                else
                {
                    // Fallback to English if file not found
                    if (lang != "en") SetLanguage("en");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading localization: {ex.Message}");
            }
        }

        public static string GetString(string key)
        {
            if (_currentStrings != null && _currentStrings.TryGetValue(key, out string value))
            {
                return value;
            }
            return key; // Return key as fallback
        }

        public static string CurrentLanguage => _currentLanguage;
    }
}
