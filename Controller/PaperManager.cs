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
        private int _paperPreviewWidth { get; set; } = 800;
        private int _paperPreviewHeight => (int)(_paperPreviewWidth * 1.414f);

        private PaperStack _paperStack = new();

        public override void _Ready()
        {
            base._Ready();

            ConfigManager.LoadAll();
            
            ProcessNextPDF();

            this.GetComponentsInChildren<PaperGrid>().First().Setup(this, _paperStack);
        }
        private void ProcessNextPDF()
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
