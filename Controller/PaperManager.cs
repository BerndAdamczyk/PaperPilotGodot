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
        public Action<Paper> PageProcessed { get; set; } //PDF Rendering
        public Action<int> GridContainerColumnsChanged { get; set; }
        private int _paperPreviewWidth { get; set; } = 800;
        private int _paperPreviewHeight => (int)(_paperPreviewWidth * 1.414f);

        private PaperStateColorConfig _colorConfig;

        //UI Elements
        private PaperStack _paperStack = new();
        private PaperGrid _paperGrid = null;
        private GridContainer _GC_paperGrid = null;
        private SpinBox _spinBox_Columns = null;
        private LineEdit _le_OutputFileName = null;
        private Button _btn_Skip = null;
        private Button _btn_Confirm = null;
        private Button _btn_GenerateQr = null;
        private Label _lbl_StEmpty = null;
        private Label _lbl_StSplitt = null;
        private Label _lbl_StKeep = null;
        private Label _lbl_StTotal = null;

        public override async void _Ready()
        {
            base._Ready();
            ConfigManager.LoadAll();
            _colorConfig = ConfigManager.StateColorConfig;

            _paperGrid = this.GetComponentsInChildren<PaperGrid>().First();
            _paperGrid.Setup(this);

            _spinBox_Columns = this.GetComponentsInChildren<SpinBox>("SpinBox_Columns").First();
            _le_OutputFileName = this.GetComponentsInChildren<LineEdit>("OutputFileName").First();
            _btn_Skip = this.GetComponentsInChildren<Button>("Skip").First();
            _btn_Confirm = this.GetComponentsInChildren<Button>("Confirm").First();
            _btn_GenerateQr = this.GetComponentsInChildren<Button>("GenerateQr").First();
            _lbl_StEmpty = this.GetComponentsInChildren<Label>("Empty").First();
            _lbl_StEmpty.LabelSettings = new();
            _lbl_StEmpty.LabelSettings.FontColor = _colorConfig.StateColors[PaperState.Empty];
            _lbl_StSplitt = this.GetComponentsInChildren<Label>("Splitt").First();
            _lbl_StSplitt.LabelSettings = new();
            _lbl_StSplitt.LabelSettings.FontColor = _colorConfig.StateColors[PaperState.SplittingPoint];
            _lbl_StKeep = this.GetComponentsInChildren<Label>("Keep").First();
            _lbl_StKeep.LabelSettings = new();
            _lbl_StKeep.LabelSettings.FontColor = _colorConfig.StateColors[PaperState.Keep];
            _lbl_StTotal = this.GetComponentsInChildren<Label>("Total").First();

            _spinBox_Columns.ValueChanged += _spinBox_Columns_ValueChanged;
            _btn_Skip.Pressed += _btn_Skip_Pressed;
            _btn_Confirm.Pressed += _btn_Confirm_Pressed;
            _btn_GenerateQr.Pressed += _btn_GenerateQr_Pressed;

            await ProcessNextPDF();
        }

        private async void _btn_Confirm_Pressed()
        {
            ExportPDF();
            await ProcessNextPDF();
        }


        private async void _btn_Skip_Pressed()
        {
            await ProcessNextPDF();
        }

        private void _btn_GenerateQr_Pressed()
        {
            var dialog = new FileDialog
            {
                Access = FileDialog.AccessEnum.Filesystem,
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Title = "Save QR Code PDF",
                CurrentFile = "PaperPilot_Split_Marker.pdf"
            };
            dialog.FileSelected += (path) =>
            {
                Tools.QrCodeGenerator.GenerateSplitMarkerPdf(path);
                GD.Print($"QR Code PDF generated successfully at: {path}");
            };
            AddChild(dialog);
            dialog.PopupCentered();
        }

        private void _spinBox_Columns_ValueChanged(double value)
        {
            GridContainerColumnsChanged?.Invoke(Mathf.RoundToInt(value));
        }

        private async Task ProcessNextPDF()
        {
            try
            {
                _paperStack = new();
                _paperStack.Path = GetOldestPDF();
                using (var docReader = DocLib.Instance
                    .GetDocReader(_paperStack.Path, new PageDimensions(_paperPreviewWidth, _paperPreviewHeight)))
                {
                    for (int i = 0; i < docReader.GetPageCount(); i++)
                    {
                        Paper paper = new Paper();
                        using (var pageReader = docReader.GetPageReader(i))
                        {
                            paper.PageId = i;
                            paper.PagePreview = PdfTextureLoader.RenderPdfPage(pageReader, i, 
                                _paperPreviewWidth, _paperPreviewHeight);
                            if (PdfAnalysis.AnalyzeForSplit(paper.PagePreview))
                                paper.State = PaperState.SplittingPoint;
                            else
                                paper.State = PdfAnalysis.AnalyzeForBlank(paper.PagePreview)
                                    ? PaperState.Empty
                                    : PaperState.Keep;
                        }
                        _paperStack.Papers.Add(paper);
                        UpdatePaperStackCounts();
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
        private void ExportPDF()
        {
            _paperStack.Name = _le_OutputFileName.Text;
            PdfStitcher.CleanAndSplitPdf(_paperStack);
        }

        private void UpdatePaperStackCounts()
        {
            _paperStack.KeptPages = _paperStack.Papers.Count(p => p.State == PaperState.Keep);
            _paperStack.EmptyPages = _paperStack.Papers.Count(p => p.State == PaperState.Empty);
            _paperStack.SplittingPointPages = _paperStack.Papers.Count(p => p.State == PaperState.SplittingPoint);

            _lbl_StKeep.Text = _paperStack.KeptPages.ToString();
            _lbl_StEmpty.Text = _paperStack.EmptyPages.ToString();
            _lbl_StSplitt.Text = _paperStack.SplittingPointPages.ToString();
            _lbl_StTotal.Text = _paperStack.TotalPages.ToString();
        }
    }
}
