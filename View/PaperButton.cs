using Godot;
using System.Drawing.Imaging;
using System.IO;
using System;
using PaperPilot.Model;
using PaperPilot;
using PaperPilot.Controller;
using System.Linq;
using PaperPilot.Config;

namespace PaperPilot.View
{
    public partial class PaperButton : Node
    {
        private PaperManager _paperManager;
        private Paper _paper = null;
        private PaperStateColorConfig _colorConfig;

        private Button _button = null;
        private ColorRect _colorRect = null;

        public void Setup(PaperManager paperManager, Paper paper)
        {
            _paperManager = paperManager;
            _paper = paper;
            _colorConfig = ConfigManager.StateColorConfig;

            string absolutePath =
                ProjectSettings.GlobalizePath("res://Test-PDF.pdf");

            _button = this.GetComponentsInChildren<Button>().First();
            _button.Icon = _paper.PagePreview;
            _button.Text = _paper.PageId.ToString();

            _colorRect = this.GetComponentsInChildren<ColorRect>().First();
            _colorRect.Color = _colorConfig.StateColors[paper.State];
        }
    }
}