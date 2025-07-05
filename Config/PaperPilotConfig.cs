using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Godot;

namespace PaperPilot.Config
{

    public class PaperPilotConfig
    {
        public string InputFolderPath { get; set; }

        //=> (Engine.IsEditorHint())
        //? ProjectSettings.GlobalizePath(InputFolderPath)
        //: OS.GetExecutablePath().GetBaseDir().PathJoin(InputFolderPath);

        public string OutputFolderPath { get; set; }
        public double BlankPageThreshold { get; set; }
    }
}
