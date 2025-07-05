using System.Collections.Generic;
using Godot;
using PaperPilot.Model;

namespace PaperPilot.Config
{
    public class PaperStateColorConfig
    {
        public Dictionary<PaperState, Color> StateColors = new()
        {
            { PaperState.NotAnalyzed, Colors.Gray },
            { PaperState.Keep, Colors.Green },
            { PaperState.Empty, Colors.Red },
            { PaperState.SplittingPoint, Colors.Purple }
        };
    }
}
