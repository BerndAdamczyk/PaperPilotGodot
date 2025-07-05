using Godot;
using PaperPilot.Config;
using PaperPilot.Controller;
using PaperPilot.Model;
using System.Collections.Generic;

[Tool]
public partial class EditorConfigTool : Node
{
    [Export(PropertyHint.Dir)]
    public string InputFolder { get; set; } = "input";
    [Export(PropertyHint.Dir)]
    public string OutputFolder { get; set; } = "output";

    // Example: Color exports for 4 paper states
    [Export]
    public Color NotAnalyzedColor { get; set; } = Colors.Gray;
    [Export]
    public Color KeepColor { get; set; } = Colors.Green;
    [Export]
    public Color EmptyColor { get; set; } = Colors.Red;
    [Export]
    public Color SplittingPointColor { get; set; } = Colors.Purple;

    [ExportToolButton("Save Config")]
    public Callable SaveConfigButton => Callable.From(SaveConfig);

    public void SaveConfig()
    {
        if (Engine.IsEditorHint())
        {
            // Set config values
            ConfigManager.PilotConfig ??= new PaperPilotConfig();
            ConfigManager.PilotConfig.InputFolderPath = InputFolder;
            ConfigManager.PilotConfig.OutputFolderPath = OutputFolder;

            // Set colors
            ConfigManager.StateColorConfig ??= new PaperStateColorConfig();
            var stateColors = ConfigManager.StateColorConfig.StateColors;
            stateColors[PaperState.NotAnalyzed] = NotAnalyzedColor;
            stateColors[PaperState.Keep] = KeepColor;
            stateColors[PaperState.Empty] = EmptyColor;
            stateColors[PaperState.SplittingPoint] = SplittingPointColor;

            ConfigManager.SaveAll();

            GD.Print("[ConfigEditorTool] Default config saved to user://");
            // Optionally: open the config folder
            // ConfigManager.ShowConfigFolder();
        }
    }
}
