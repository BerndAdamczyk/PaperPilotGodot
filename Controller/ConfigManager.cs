using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using PaperPilot.Config;
using PaperPilot.Model;

namespace PaperPilot.Controller
{
    public static class ConfigManager
    {
        private static readonly string UserConfigDir = ProjectSettings.GlobalizePath("user://");
        private static readonly string PilotConfigFile = Path.Combine(UserConfigDir, "paperpilot_config.json");
        private static readonly string StateColorConfigFile = Path.Combine(UserConfigDir, "paperstatecolor_config.json");

        public static PaperPilotConfig PilotConfig { get; set; }
        public static PaperStateColorConfig StateColorConfig { get; set; }

        public static void LoadAll()
        {
            bool pilotConfigExisted = File.Exists(PilotConfigFile);
            bool stateColorConfigExisted = File.Exists(StateColorConfigFile);

            PilotConfig = Load<PaperPilotConfig>(PilotConfigFile) ?? new PaperPilotConfig();
            StateColorConfig = LoadPaperStateColorConfig(StateColorConfigFile) ?? new PaperStateColorConfig();

            if (!pilotConfigExisted || !stateColorConfigExisted)
            {
                SaveAll();
                GD.Print("Default config files created at: ", UserConfigDir);
            }
            
            // Create directories if they are not in res:// and path is not null
            string resPath = ProjectSettings.GlobalizePath("res://");

            string absInput = PilotConfig.AbsoluteInputFolderPath;
            if (!string.IsNullOrEmpty(absInput) && !absInput.StartsWith(resPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(absInput);
            }

            string absOutput = PilotConfig.AbsoluteOutputFolderPath;
            if (!string.IsNullOrEmpty(absOutput) && !absOutput.StartsWith(resPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(absOutput);
            }
        }

        public static void SaveAll()
        {
            Save(PilotConfigFile, PilotConfig);
            SavePaperStateColorConfig(StateColorConfigFile, StateColorConfig);
        }

        // ---- Generic load/save for simple configs ----
        private static T Load<T>(string file) where T : class
        {
            try
            {
                if (!File.Exists(file)) return null;
                var json = File.ReadAllText(file);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to load {typeof(T).Name} from {file}: {ex.Message}");
                return null;
            }
        }

        private static void Save<T>(string file, T obj)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(obj, options);
                File.WriteAllText(file, json);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to save {typeof(T).Name} to {file}: {ex.Message}");
            }
        }

        // ---- Special handling for Godot.Color in StateColorConfig ----
        private static PaperStateColorConfig LoadPaperStateColorConfig(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var json = File.ReadAllText(file);

                // Parse as DTO: Dictionary<string, float[]>
                var dto = JsonSerializer.Deserialize<Dictionary<string, float[]>>(json);
                var result = new PaperStateColorConfig();
                result.StateColors.Clear();
                foreach (var kv in dto)
                {
                    if (Enum.TryParse<PaperState>(kv.Key, out var state) && kv.Value.Length >= 3)
                    {
                        float r = kv.Value[0], g = kv.Value[1], b = kv.Value[2];
                        float a = kv.Value.Length > 3 ? kv.Value[3] : 1.0f;
                        result.StateColors[state] = new Color(r, g, b, a);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to load PaperStateColorConfig: {ex.Message}");
                return null;
            }
        }

        private static void SavePaperStateColorConfig(string file, PaperStateColorConfig cfg)
        {
            try
            {
                var dto = new Dictionary<string, float[]>();
                foreach (var kv in cfg.StateColors)
                    dto[kv.Key.ToString()] = new[] { kv.Value.R, kv.Value.G, kv.Value.B, kv.Value.A };
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(dto, options);
                File.WriteAllText(file, json);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to save PaperStateColorConfig: {ex.Message}");
            }
        }

        // ---- Utility ----
        public static void ShowConfigFolder() => OS.ShellOpen(UserConfigDir);
    }
}


