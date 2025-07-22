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
            // 1. Define pages to keep and splitting points
            var pagesToKeep = paperStack.Papers
                .Where(p => p.State == PaperState.Keep)
                .Select(p => p.PageId + 1) // DocLib uses 1-based page numbers
                .OrderBy(p => p)
                .ToList();

            var originalSplittingPoints = paperStack.Papers
                .Where(p => p.State == PaperState.SplittingPoint)
                .Select(p => p.PageId + 1)
                .OrderBy(p => p)
                .ToList();

            if (!pagesToKeep.Any())
            {
                GD.PrintErr("No pages to keep. Aborting stitching operation.");
                return;
            }

            // 2. Create a cleaned PDF containing only the pages to keep
            string pageRange = string.Join(",", pagesToKeep);
            byte[] cleanedPdfBytes = DocLib.Instance.Split(paperStack.Path, pageRange);

            // 3. Calculate the new indices of the splitting points within the cleaned PDF
            var newSplittingPoints = new List<int>();
            foreach (var splitPoint in originalSplittingPoints)
            {
                // Find how many "keep" pages came before this split point.
                // This count becomes the new index for the split.
                int newIndex = pagesToKeep.Count(p => p < splitPoint);
                if (newIndex > 0)
                {
                    newSplittingPoints.Add(newIndex);
                }
            }

            // 4. Create output directory
            string outputFolder = ConfigManager.PilotConfig.AbsoluteOutputFolderPath;
            string originalFileName = Path.GetFileNameWithoutExtension(paperStack.Path);
            //string outputDirName = $"{paperStack.Name}_{originalFileName}";
            //string outputDirPath = Path.Combine(outputFolder, outputDirName);
            Directory.CreateDirectory(outputFolder);

            // 5. Split the cleaned PDF based on the new splitting points
            int startPage = 1;
            int fileCounter = 1;
            var pagesInCleanedPdf = pagesToKeep.Count;

            foreach (var endPage in newSplittingPoints)
            {
                if (startPage > endPage) continue;

                string splitRange = $"{startPage}-{endPage}";
                byte[] splitPdfBytes = DocLib.Instance.Split(cleanedPdfBytes, splitRange);

                string outputFileName = $"{paperStack.Name}_{fileCounter++:D3}.pdf";
                string outputPath = Path.Combine(outputFolder, outputFileName);
                File.WriteAllBytes(outputPath, splitPdfBytes);

                startPage = endPage + 1;
            }

            // 6. Save the last part of the PDF
            if (startPage <= pagesInCleanedPdf)
            {
                string splitRange = $"{startPage}-{pagesInCleanedPdf}";
                byte[] splitPdfBytes = DocLib.Instance.Split(cleanedPdfBytes, splitRange);

                string outputFileName = $"{paperStack.Name}_{fileCounter++:D3}.pdf";
                string outputPath = Path.Combine(outputFolder, outputFileName);
                File.WriteAllBytes(outputPath, splitPdfBytes);
            }

            GD.PrintRich($"[color=green]Successfully cleaned and split PDF.[/color] Saved to: [url={outputFolder}]{outputFolder}[/url]");
        }
    }
}

