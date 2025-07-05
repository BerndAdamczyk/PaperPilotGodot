using Godot;

[Tool]
public partial class MinimalTestNode : Node
{
    [Export]
    public string Text { get; set; } = "test";
    [Export]
    public bool SaveConfig { get; set; } = false;
}
