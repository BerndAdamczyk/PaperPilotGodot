using Docnet.Core;
using Docnet.Core.Models;
using Godot;
using PaperPilot.Config;
using PaperPilot.Model;
using PaperPilot.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaperPilot.Controller
{
    public partial class PaperManager : Node
    {
        public Action<Paper> PageProcessed { get; set; } //PDF Rendering
        public Action<int> GridContainerColumnsChanged { get; set; }
        private int _paperPreviewWidth { get; set; } = 800;
        private int _paperPreviewHeight => (int)(_paperPreviewWidth * 1.414f);

        private const int _startColumnCount = 8;
        private const string DefaultOutputFileName = "";
        private const string DefaultPageCountText = "0";

        private PaperStateColorConfig _colorConfig;
        private FileSystemWatcher _fileWatcher;
        private bool _isProcessing = false;
        private Godot.Timer _debounceTimer;

        //UI Elements
        private PaperStack _paperStack = new();
        private PaperGrid _paperGrid = null;
        private SpinBox _spinBox_Columns = null;
        private LineEdit _le_OutputFileName = null;
        private Button _btn_Skip = null;
        private Button _btn_Confirm = null;
        private Button _btn_GenerateQr = null;
        private Button _btn_Settings = null;
        private Label _lbl_StEmpty = null;
        private Label _lbl_StSplitt = null;
        private Label _lbl_StKeep = null;
        private Label _lbl_StTotal = null;
        private Label _lbl_Status = null;

        public override void _Ready()
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
            _btn_Settings = this.GetComponentsInChildren<Button>("Settings").First();
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
            _lbl_Status = this.GetComponentsInChildren<Label>("Status").First();

            _spinBox_Columns_ValueChanged(_startColumnCount);
            _spinBox_Columns.ValueChanged += _spinBox_Columns_ValueChanged;
            _btn_Skip.Pressed += _btn_Skip_Pressed;
            _btn_Confirm.Pressed += _btn_Confirm_Pressed;
            _btn_GenerateQr.Pressed += _btn_GenerateQr_Pressed;
            _btn_Settings.Pressed += _btn_Settings_Pressed;

            SetButtonActiveState(_btn_Confirm, false);
            SetButtonActiveState(_btn_Skip, false);

            ResetUI();
            SetupDebounceTimer();

            // Initial check for PDF
            ProcessNextPDF();
            
            // Setup FileSystemWatcher
            SetupFileWatcher();
        }


        private void _btn_Confirm_Pressed()
        {
            ExportPDF();
            _paperGrid.ClearGrid();
            try
            {
                File.Delete(_paperStack.Path);
                GD.Print($"Deleted processed file: {_paperStack.Path}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error deleting file: {ex.Message}");
            }

            ResetUI();
            ProcessNextPDF();
        }

        private void SetButtonActiveState(Button button, bool state)
        {
            button.Disabled = !state;
        }

        private void _btn_Skip_Pressed()
        {
            _paperGrid.ClearGrid();
            try
            {
                string skippedFolderPath = Path.Combine(Path.GetDirectoryName(_paperStack.Path), "skipped");
                Directory.CreateDirectory(skippedFolderPath);
                string destPath = Path.Combine(skippedFolderPath, Path.GetFileName(_paperStack.Path));
                File.Move(_paperStack.Path, destPath);
                GD.Print($"Skipped and moved file to: {destPath}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error skipping file: {ex.Message}");
            }
            ProcessNextPDF();
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
                var absolutePath = ProjectSettings.GlobalizePath(path).Replace("/", "\\");
                GD.Print($"QR Code PDF generated successfully at: {absolutePath}");
                Process.Start("explorer.exe", $"/select, \"{absolutePath}\"");
            };
            AddChild(dialog);
            dialog.PopupCentered();
        }
        private void _btn_Settings_Pressed()
        {
            ConfigManager.ShowConfigFolder();
        }

        private void _spinBox_Columns_ValueChanged(double value)
        {
            GridContainerColumnsChanged?.Invoke(Mathf.RoundToInt(value));
        }

        private async void ProcessNextPDF()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                SetButtonActiveState(_btn_Confirm, false);
                SetButtonActiveState(_btn_Skip, false);
                _lbl_Status.Text = "Searching for PDF...";

                string pdfPath = GetOldestPDF();

                if (string.IsNullOrEmpty(pdfPath))
                {
                    _lbl_Status.Text = "Waiting for new PDF in input folder...";
                    return;
                }

                if (!await IsFileReady(pdfPath))
                {
                    _lbl_Status.Text = "Error: Could not access file.";
                    return;
                }

                _lbl_Status.Text = $"Processing: {Path.GetFileName(pdfPath)}";
                _paperStack = new PaperStack { Path = pdfPath };
                _le_OutputFileName.Text = Path.GetFileNameWithoutExtension(pdfPath);

                using (var docReader = DocLib.Instance.GetDocReader(_paperStack.Path, new PageDimensions(_paperPreviewWidth, _paperPreviewHeight)))
                {
                    for (int i = 0; i < docReader.GetPageCount(); i++)
                    {
                        Paper paper = new Paper();
                        using (var pageReader = docReader.GetPageReader(i))
                        {
                            paper.PageId = i;
                            paper.PagePreview = PdfTextureLoader.RenderPdfPage(pageReader, i, _paperPreviewWidth, _paperPreviewHeight);
                            if (PdfAnalysis.AnalyzeForSplit(paper.PagePreview))
                                paper.State = PaperState.SplittingPoint;
                            else
                                paper.State = PdfAnalysis.AnalyzeForBlank(paper.PagePreview) ? PaperState.Empty : PaperState.Keep;
                        }
                        _paperStack.Papers.Add(paper);
                        UpdatePaperStackCounts();
                        PageProcessed?.Invoke(paper);
                        
                        await ToSignal(GetTree(), "process_frame");
                    }
                }
                _lbl_Status.Text = "Processing complete.";
                SetButtonActiveState(_btn_Confirm, true);
                SetButtonActiveState(_btn_Skip, true);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"ProcessNextPDF failed: {ex.Message}");
                _lbl_Status.Text = "Error processing PDF.";
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task<bool> IsFileReady(string filePath)
        {
            const int maxRetries = 50;
            const int delayMs = 500;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.None))
                    {
                        return true; // File is accessible
                    }
                }
                catch (IOException)
                {
                    GD.Print($"File is locked, retrying... Attempt {i + 1}/{maxRetries}");
                    await Task.Delay(delayMs);
                }
            }
            GD.PrintErr($"Could not access file after {maxRetries} attempts: {filePath}");
            return false;
        }

        private string GetOldestPDF()
        {
            try
            {
                string folder = ConfigManager.PilotConfig.AbsoluteInputFolderPath;
                if (!Directory.Exists(folder))
                {
                    GD.PrintErr($"Input folder does not exist: {folder}");
                    return null;
                }

                var pdfFiles = Directory.GetFiles(folder, "*.pdf", SearchOption.TopDirectoryOnly);
                if (pdfFiles.Length == 0)
                {
                    return null;
                }

                return pdfFiles.Select(f => new FileInfo(f)).OrderBy(f => f.CreationTime).First().FullName;
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

        private void ResetUI(string outputFileName = DefaultOutputFileName, 
            string empty = DefaultPageCountText, 
            string splitt = DefaultPageCountText, 
            string keep = DefaultPageCountText, 
            string total = DefaultPageCountText)
        {
            _le_OutputFileName.Text = outputFileName;
            _lbl_StEmpty.Text = empty;
            _lbl_StSplitt.Text = splitt;
            _lbl_StKeep.Text = keep;
            _lbl_StTotal.Text = total;
        }

        private void SetupDebounceTimer()
        {
            _debounceTimer = new Godot.Timer
            {
                WaitTime = 1.0, // Wait 1 second
                OneShot = true
            };
            AddChild(_debounceTimer);
            _debounceTimer.Timeout += OnDebounceTimerTimeout;
        }
        
        private void SetupFileWatcher()
        {
            string folder = ConfigManager.PilotConfig.AbsoluteInputFolderPath;
            if (!Directory.Exists(folder)) 
            {
                GD.PrintErr($"File watcher setup failed: Input folder not found at '{folder}'");
                return;
            }

            _fileWatcher = new FileSystemWatcher(folder)
            {
                Filter = "*.pdf",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
                InternalBufferSize = 8192 // 8 KB buffer
            };
            _fileWatcher.Created += OnPdfEvent;
            _fileWatcher.Error += OnPdfEvent; // Treat error as a trigger
        }

        private void OnPdfEvent(object sender, FileSystemEventArgs e)
        {
            GD.Print($"PDF event detected: {e.Name}. Starting debounce timer.");
            _debounceTimer.CallDeferred("start");
        }

        private void OnPdfEvent(object sender, ErrorEventArgs e)
        {
            GD.PrintErr($"FileSystemWatcher Error, but treating as a trigger: {e.GetException().Message}");
            _debounceTimer.CallDeferred("start");
        }

        private void OnDebounceTimerTimeout()
        {
            GD.Print("Debounce timer finished. Checking for new PDF.");
            if (_isProcessing || _btn_Confirm.Disabled == false)
            {
                GD.Print("Already processing a file or waiting for confirmation, skipping trigger.");
                return;
            }
            CallDeferred(nameof(ProcessNextPDF));
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            _fileWatcher?.Dispose();
            _debounceTimer?.Stop();
        }
    }
}
