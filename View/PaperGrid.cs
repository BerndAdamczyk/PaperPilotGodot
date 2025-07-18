using Godot;
using PaperPilot.Config;
using PaperPilot.Controller;
using PaperPilot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

namespace PaperPilot.View
{
    public partial class PaperGrid : GridContainer
    {
        private PaperManager _paperManager = null;

        private PaperStateColorConfig _colorConfig = null;

        private PackedScene _paperButtonScene = null;

        private List<PaperButton> _buttons = new List<PaperButton>();

        public void Setup(PaperManager paperManager)
        {
            _paperManager = paperManager;
            _colorConfig = ConfigManager.StateColorConfig;
            _paperButtonScene = GD.Load<PackedScene>("res://btn_PaperGrid.tscn");

            _paperManager.PageProcessed += SetupNextPage;
            _paperManager.GridContainerColumnsChanged += ChangeColumns;
        }

        public void SetupNextPage(Paper paper)
        {
            Node paperButtonInstance = null;

            paperButtonInstance = _paperButtonScene.Instantiate();
            PaperButton paperButton = (PaperButton) paperButtonInstance;
            paperButton.Setup(_paperManager, paper);

            AddChild(paperButtonInstance);

            _buttons.Add(paperButton);
        }
        public void ClearGrid()
        {
            foreach (var button in _buttons)
            {
                button.QueueFree();
            }
            _buttons.Clear();
        }
        private void ChangeColumns(int columns)
        {
            this.Columns = columns;
        }


        public override void _ExitTree()
        {
            base._ExitTree();

            _paperManager.PageProcessed -= SetupNextPage;
        }
    }
}