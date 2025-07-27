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

        private TextureRect _textureRect = null;
        private Label _label = null;
        private Panel _panel = null;

        private Button _btn_Paper = null;

        public void Setup(PaperManager paperManager, Paper paper)
        {
            _paperManager = paperManager;
            _paper = paper;
            _colorConfig = ConfigManager.StateColorConfig;

            _btn_Paper = this.GetComponentsInChildren<Button>().First();
            _btn_Paper.Pressed += _btn_Paper_Pressed;

            string absolutePath =
                ProjectSettings.GlobalizePath("res://Test-PDF.pdf");

            _textureRect = this.GetComponentsInChildren<TextureRect>().First();
            _textureRect.Texture = _paper.PagePreview;


            _label = this.GetComponentsInChildren<Label>().First();
            _label.Text = _paper.PageId.ToString();

            _panel = this.GetComponentsInChildren<Panel>().First();
            SetPanelColor(_panel, _colorConfig.StateColors[paper.State]);
        }

        public void SetPanelColor(Panel panel, Color color)
        {
            var style = new StyleBoxFlat();
            style.BgColor = color;
            panel.AddThemeStyleboxOverride("panel", style);
        }

        private void _btn_Paper_Pressed()
        {
            GD.Print($"Button pressed: {_paper.PageId}");
            if (_paper.State == PaperState.SplittingPoint) return;

            _paper.State = (PaperState) ((((int)_paper.State) + 1) % 2);
            SetPanelColor(_panel, _colorConfig.StateColors[_paper.State]);
        }
    }
}