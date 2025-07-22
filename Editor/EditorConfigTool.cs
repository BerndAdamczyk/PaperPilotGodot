using Godot;
using PaperPilot.Config;
using PaperPilot.Controller;
using PaperPilot.Model;
using System.Collections.Generic;

[Tool]
public partial class EditorConfigTool : Node
{
    [Export(PropertyHint.Dir)]
    public string InputFolder { get; set; }
    [Export(PropertyHint.Dir)]
    public string OutputFolder { get; set; }

    [Export]
    public double BlankPageThreshold { get; set; }

    [Export]
    public Color NotAnalyzedColor { get; set; }
    [Export]
    public Color KeepColor { get; set; }
    [Export]
    public Color EmptyColor { get; set; }
    [Export]
    public Color SplittingPointColor { get; set; }

    [ExportToolButton("Save Config")]
    public Callable SaveConfigButton => Callable.From(SaveConfig);
    
    [ExportToolButton("Load Config")]
    public Callable LoadConfigButton => Callable.From(LoadConfigValues);

    public override void _Ready()
    {
        base._Ready();
        if (Engine.IsEditorHint())
        {
            ConfigManager.LoadAll();
            LoadConfigValues();
        }
    }

    public void LoadConfigValues()
    {
        if (!Engine.IsEditorHint()) return;

        GD.Print("[EditorConfigTool] Loading config values into editor.");
        ConfigManager.LoadAll();

        InputFolder = ConfigManager.PilotConfig.InputFolderPath;
        OutputFolder = ConfigManager.PilotConfig.OutputFolderPath;
        BlankPageThreshold = ConfigManager.PilotConfig.BlankPageThreshold;

        var stateColors = ConfigManager.StateColorConfig.StateColors;
        NotAnalyzedColor = stateColors.GetValueOrDefault(PaperState.NotAnalyzed, new PaperStateColorConfig().StateColors[PaperState.NotAnalyzed]);
        KeepColor = stateColors.GetValueOrDefault(PaperState.Keep, new PaperStateColorConfig().StateColors[PaperState.Keep]);
        EmptyColor = stateColors.GetValueOrDefault(PaperState.Empty, new PaperStateColorConfig().StateColors[PaperState.Empty]);
        SplittingPointColor = stateColors.GetValueOrDefault(PaperState.SplittingPoint, new PaperStateColorConfig().StateColors[PaperState.SplittingPoint]);
    }

    public void SaveConfig()
    {
        if (!Engine.IsEditorHint()) return;

        ConfigManager.PilotConfig ??= new PaperPilotConfig();
        ConfigManager.PilotConfig.InputFolderPath = InputFolder;
        ConfigManager.PilotConfig.OutputFolderPath = OutputFolder;
        ConfigManager.PilotConfig.BlankPageThreshold = BlankPageThreshold;

        ConfigManager.StateColorConfig ??= new PaperStateColorConfig();
        var stateColors = ConfigManager.StateColorConfig.StateColors;
        stateColors[PaperState.NotAnalyzed] = NotAnalyzedColor;
        stateColors[PaperState.Keep] = KeepColor;
        stateColors[PaperState.Empty] = EmptyColor;
        stateColors[PaperState.SplittingPoint] = SplittingPointColor;

        ConfigManager.SaveAll();

        GD.Print("[EditorConfigTool] Config saved to user://");
        ConfigManager.ShowConfigFolder();
    }
}