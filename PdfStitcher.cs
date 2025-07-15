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
        /// <summary>
        /// Creates a new PDF file containing only the pages that are not marked as 'Empty' in the paperStack.
        /// The new file will be saved in the configured output folder.
        /// </summary>
        /// <param name="paperStack">The PaperStack object containing the list of pages and their states.</param>
        public static void StitchDaStack(PaperStack paperStack)
        {
            // 1. Get pages to keep (everything that is not 'Empty')
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

            // 2. Generate page range string for DocLib (e.g., "1,3-5,7")
            var ranges = new List<string>();
            int rangeStart = pagesToKeep[0];
            int rangeEnd = pagesToKeep[0];

            for (int i = 1; i < pagesToKeep.Count; i++)
            {
                if (pagesToKeep[i] == rangeEnd + 1)
                {
                    rangeEnd = pagesToKeep[i];
                }
                else
                {
                    if (rangeStart == rangeEnd)
                        ranges.Add(rangeStart.ToString());
                    else
                        ranges.Add($"{rangeStart}-{rangeEnd}");
                    
                    rangeStart = pagesToKeep[i];
                    rangeEnd = pagesToKeep[i];
                }
            }
            if (rangeStart == rangeEnd)
                ranges.Add(rangeStart.ToString());
            else
                ranges.Add($"{rangeStart}-{rangeEnd}");

            string pageRange = string.Join(",", ranges);
            GD.Print($"Keeping pages: {pageRange}");

            // 3. Define output path
            string outputFolder = ConfigManager.PilotConfig.OutputFolderPath;
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            
            string originalFileName = Path.GetFileNameWithoutExtension(paperStack.Path);
            string outputFileName = $"{originalFileName}_stitched.pdf";
            string outputPath = Path.Combine(outputFolder, outputFileName);

            // 4. Split the PDF using DocLib
            try
            {
                byte[] newPdfBytes = DocLib.Instance.Split(paperStack.Path, pageRange);

                // 5. Save the new PDF
                File.WriteAllBytes(outputPath, newPdfBytes);

                GD.PrintRich($"[color=green]Successfully stitched PDF.[/color] Saved to: [url={outputPath}]{outputPath}[/url]");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error while stitching PDF '{paperStack.Path}': {ex.Message}");
            }
        }
    }
}
