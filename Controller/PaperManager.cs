using Docnet.Core.Models;
using Docnet.Core;
using Godot;
using PaperPilot.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaperPilot.Config;
using PaperPilot.View;

namespace PaperPilot.Controller
{
    public partial class PaperManager : Node
    {
        public Action<Paper> PageProcessed;
        public Action<int> GridContainerColumnsChanged;
        private int _paperPreviewWidth { get; set; } = 800;
        private int _paperPreviewHeight => (int)(_paperPreviewWidth * 1.414f);

        private PaperStack _paperStack = new();
        private PaperGrid _paperGrid = null;
        private GridContainer _GC_paperGrid = null;
        private SpinBox _spinBox_Columns = null;
        private Button _btn_Skip = null;
        private Button _btn_Confirm = null;

        public override void _Ready()
        {
            base._Ready();
            ConfigManager.LoadAll();

            _paperGrid = this.GetComponentsInChildren<PaperGrid>().First();
            _paperGrid.Setup(this);

            _spinBox_Columns = this.GetComponentsInChildren<SpinBox>("SpinBox_Columns").First();
            _btn_Skip = this.GetComponentsInChildren<Button>("Skip").First();
            _btn_Confirm = this.GetComponentsInChildren<Button>("Confirm").First();

            _spinBox_Columns.ValueChanged += _spinBox_Columns_ValueChanged;
            _btn_Skip.Pressed += _btn_Skip_Pressed;
            _btn_Confirm.Pressed += _btn_Confirm_Pressed;

            ProcessNextPDF();
        }

        private void _btn_Confirm_Pressed()
        {
            throw new NotImplementedException();
        }

        private void _btn_Skip_Pressed()
        {
            throw new NotImplementedException();
        }

        private void _spinBox_Columns_ValueChanged(double value)
        {
            GridContainerColumnsChanged?.Invoke(Mathf.RoundToInt(value));
        }

        private async void ProcessNextPDF()
        {
            try
            {
                _paperStack.Papers = new List<Paper>();
                using (var docReader = DocLib.Instance
                    .GetDocReader(GetOldestPDF(), new PageDimensions(_paperPreviewWidth, _paperPreviewHeight)))
                {
                    for (int i = 0; i < docReader.GetPageCount(); i++)
                    {
                        Paper paper = new Paper();
                        using (var pageReader = docReader.GetPageReader(i))
                        {
                            paper.PageId = i;
                            paper.PagePreview = PdfTextureLoader.RenderPdfPage(pageReader, i, 
                                _paperPreviewWidth, _paperPreviewHeight);
                            paper.State = PdfAnalysis.AnalyzeForBlank(paper.PagePreview)
                                ? PaperState.Empty
                                : PaperState.Keep;
                        }
                        _paperStack.Papers.Add(paper);
                        PageProcessed?.Invoke(paper);

                        // Yield to Godot so UI can update/events can process
                        await ToSignal(GetTree(), "process_frame");
                    }
                }

            }
            catch (Exception ex)
            {
                GD.PrintErr($"RenderPdfPage failed: {ex.Message}");
            }
        }
        private string GetOldestPDF()
        {
            try
            {
                string folder = ConfigManager.PilotConfig.InputFolderPath;
                if (!Directory.Exists(folder))
                {
                    GD.PrintErr($"Input folder does not exist: {folder}");
                    return null;
                }

                var pdfFiles = Directory.GetFiles(folder, "*.pdf", SearchOption.TopDirectoryOnly);
                if (pdfFiles.Length == 0)
                    return null;

                string oldest = pdfFiles
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.CreationTime)
                    .First().FullName;

                return oldest;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"GetOldestPDF failed: {ex.Message}");
                return null;
            }
        }

    }
}
