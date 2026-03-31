using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace OptikFormApp.Services
{
    public class KeyboardShortcutService
    {
        private readonly string _shortcutsPath;
        private Dictionary<string, ShortcutConfig> _shortcuts = new();

        public class ShortcutConfig
        {
            public string Action { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Key { get; set; } = "";
            public string Modifiers { get; set; } = "";
            public string Category { get; set; } = "";
        }

        public KeyboardShortcutService()
        {
            _shortcutsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shortcuts.json");
            LoadDefaultShortcuts();
            LoadCustomShortcuts();
        }

        private void LoadDefaultShortcuts()
        {
            _shortcuts = new Dictionary<string, ShortcutConfig>
            {
                ["LoadTxt"] = new ShortcutConfig { 
                    Action = "LoadTxt", 
                    DisplayName = "Optik Verisi Yükle", 
                    Key = "O", 
                    Modifiers = "Ctrl",
                    Category = "Dosya"
                },
                ["SaveExam"] = new ShortcutConfig { 
                    Action = "SaveExam", 
                    DisplayName = "Sınavı Kaydet", 
                    Key = "S", 
                    Modifiers = "Ctrl",
                    Category = "Dosya"
                },
                ["ExportExcel"] = new ShortcutConfig { 
                    Action = "ExportExcel", 
                    DisplayName = "Excel'e Aktar", 
                    Key = "E", 
                    Modifiers = "Ctrl",
                    Category = "Dosya"
                },
                ["ExportCsv"] = new ShortcutConfig { 
                    Action = "ExportCsv", 
                    DisplayName = "CSV'ye Aktar", 
                    Key = "C", 
                    Modifiers = "Ctrl+Shift",
                    Category = "Dosya"
                },
                ["ExportPdf"] = new ShortcutConfig { 
                    Action = "ExportPdf", 
                    DisplayName = "PDF Karne Al", 
                    Key = "P", 
                    Modifiers = "Ctrl",
                    Category = "Dosya"
                },
                ["Evaluate"] = new ShortcutConfig { 
                    Action = "Evaluate", 
                    DisplayName = "Puanları Hesapla", 
                    Key = "F5", 
                    Modifiers = "",
                    Category = "İşlemler"
                },
                ["ShowShortcuts"] = new ShortcutConfig { 
                    Action = "ShowShortcuts", 
                    DisplayName = "Kısayolları Göster", 
                    Key = "F1", 
                    Modifiers = "",
                    Category = "Yardım"
                },
                ["Undo"] = new ShortcutConfig { 
                    Action = "Undo", 
                    DisplayName = "Geri Al", 
                    Key = "Z", 
                    Modifiers = "Ctrl",
                    Category = "Düzenleme"
                },
                ["Redo"] = new ShortcutConfig { 
                    Action = "Redo", 
                    DisplayName = "Yenile", 
                    Key = "Y", 
                    Modifiers = "Ctrl",
                    Category = "Düzenleme"
                }
            };
        }

        private void LoadCustomShortcuts()
        {
            if (!File.Exists(_shortcutsPath)) return;

            try
            {
                var json = File.ReadAllText(_shortcutsPath);
                var custom = JsonSerializer.Deserialize<Dictionary<string, ShortcutConfig>>(json);
                if (custom != null)
                {
                    foreach (var kvp in custom)
                    {
                        if (_shortcuts.ContainsKey(kvp.Key))
                            _shortcuts[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch { /* Ignore errors, use defaults */ }
        }

        public void SaveShortcuts()
        {
            try
            {
                var json = JsonSerializer.Serialize(_shortcuts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_shortcutsPath, json);
            }
            catch { /* Ignore save errors */ }
        }

        public void UpdateShortcut(string action, string key, string modifiers)
        {
            if (_shortcuts.ContainsKey(action))
            {
                _shortcuts[action].Key = key;
                _shortcuts[action].Modifiers = modifiers;
                SaveShortcuts();
            }
        }

        public ShortcutConfig? GetShortcut(string action)
        {
            return _shortcuts.TryGetValue(action, out var config) ? config : null;
        }

        public List<ShortcutConfig> GetAllShortcuts()
        {
            return _shortcuts.Values.ToList();
        }

        public List<string> GetCategories()
        {
            return _shortcuts.Values.Select(s => s.Category).Distinct().ToList();
        }

        public string GetShortcutDisplay(string action)
        {
            var config = GetShortcut(action);
            if (config == null) return "";
            return string.IsNullOrEmpty(config.Modifiers) 
                ? config.Key 
                : $"{config.Modifiers}+{config.Key}";
        }

        public void ResetToDefaults()
        {
            LoadDefaultShortcuts();
            SaveShortcuts();
        }
    }
}
