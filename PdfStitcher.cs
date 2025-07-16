using Docnet.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using PaperPilot.Controller;
using PaperPilot.Model;

namespace PaperPilot
{
    public static class PdfStitcher
    {
        public static void CleanAndSplitPdf(PaperStack paperStack)
        {
            // 1. Clean the PDF by removing empty pages
            var pagesToKeep = paperStack.Papers
                .Where(p => p.State != PaperState.Empty)
                .Select(p => p.PageId + 1) // DocLib uses 1-based page numbers
                .OrderBy(p => p)
                .ToList();

            if (!pagesToKeep.Any())
            {
                GD.PrintErr("No pages to keep. Aborting stitching operation.");
                return;
            }

            string pageRange = string.Join(",", pagesToKeep);
            byte[] cleanedPdfBytes = DocLib.Instance.Split(paperStack.Path, pageRange);

            // 2. Find splitting points in the cleaned PDF
            var splittingPoints = paperStack.Papers
                .Where(p => p.State == PaperState.SplittingPoint)
                .Select(p => p.PageId + 1)
                .OrderBy(p => p)
                .ToList();

            // 3. Create output directory
            string outputFolder = ConfigManager.PilotConfig.OutputFolderPath;
            string originalFileName = Path.GetFileNameWithoutExtension(paperStack.Path);
            string outputDirName = $"{originalFileName}_{paperStack.Name}";
            string outputDirPath = Path.Combine(outputFolder, outputDirName);
            Directory.CreateDirectory(outputDirPath);

            // 4. Split the cleaned PDF
            int startPage = 1;
            for (int i = 0; i < splittingPoints.Count; i++)
            {
                int endPage = splittingPoints[i];
                string splitRange = $"{startPage}-{endPage}";
                byte[] splitPdfBytes = DocLib.Instance.Split(cleanedPdfBytes, splitRange);

                string outputFileName = $"{originalFileName}_{paperStack.Name}_{i + 1:D3}.pdf";
                string outputPath = Path.Combine(outputDirPath, outputFileName);
                File.WriteAllBytes(outputPath, splitPdfBytes);

                startPage = endPage + 1;
            }

            // 5. Save the last part of the PDF
            if (startPage <= pagesToKeep.Count)
            {
                string splitRange = $"{startPage}-{pagesToKeep.Count}";
                byte[] splitPdfBytes = DocLib.Instance.Split(cleanedPdfBytes, splitRange);

                string outputFileName = $"{originalFileName}_{paperStack.Name}_{splittingPoints.Count + 1:D3}.pdf";
                string outputPath = Path.Combine(outputDirPath, outputFileName);
                File.WriteAllBytes(outputPath, splitPdfBytes);
            }

            GD.PrintRich($"[color=green]Successfully cleaned and split PDF.[/color] Saved to: [url={outputDirPath}]{outputDirPath}[/url]");
        }
    }
}

