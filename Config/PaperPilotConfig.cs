using Godot;
using System.IO;
using System.Text.Json.Serialization;

namespace PaperPilot.Config
{
    public class PaperPilotConfig
    {
        public string InputFolderPath { get; set; }
        public string OutputFolderPath { get; set; }
        public double BlankPageThreshold { get; set; }

        [JsonIgnore]
        public string AbsoluteInputFolderPath => GetAbsolutePath(InputFolderPath);
        [JsonIgnore]
        public string AbsoluteOutputFolderPath => GetAbsolutePath(OutputFolderPath);

        private static readonly string UserDataDir = ProjectSettings.GlobalizePath("user://");

        public PaperPilotConfig()
        {
            InputFolderPath = "input";
            OutputFolderPath = "output";
            BlankPageThreshold = 0.01;
        }

        private string GetAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (path.StartsWith("res://") || path.StartsWith("user://"))
            {
                return ProjectSettings.GlobalizePath(path);
            }
            
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            return Path.Combine(UserDataDir, path);
        }
    }
}