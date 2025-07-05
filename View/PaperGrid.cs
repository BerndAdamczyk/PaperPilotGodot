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
    public partial class PaperGrid : Node
    {
        private PaperManager _paperManager = null;

        private PaperStack _paperStack = null;
        private PaperStateColorConfig _colorConfig = null;

        private List<PaperButton> _buttons = new List<PaperButton>();

        public void Setup(PaperManager paperManager, PaperStack paperStack)
        {
            _paperManager = paperManager;
            _paperStack = paperStack;
            _colorConfig = ConfigManager.StateColorConfig;

            var paperButtonScene = GD.Load<PackedScene>("res://btn_PaperGrid.tscn");
            Node paperButtonInstance = null;

            for (int i = 0; i < _paperStack.Papers.Count; i++)
            {
                paperButtonInstance = paperButtonScene.Instantiate();
                PaperButton paperButton = paperButtonInstance
                    .GetComponentsInChildren<PaperButton>().First();
                paperButton.Setup(paperManager, paperStack.Papers[i]);

                AddChild(paperButtonInstance);

                _buttons.Add(paperButton);
            }
        }
    }
}