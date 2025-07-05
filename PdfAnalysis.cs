using Docnet.Core;
using Docnet.Core.Models;
using System;
using System.Collections.Generic;
using Godot;
using PaperPilot.Controller;

namespace PaperPilot
{
    public static class PdfAnalysis
    {
        /// <summary>
        /// Analyzes a Godot ImageTexture to determine if the page is blank (mostly white).
        /// </summary>
        /// <param name="texture">The ImageTexture of the PDF page (should be RGBA8).</param>
        /// <returns>True if the page is blank, otherwise false.</returns>
        public static bool AnalyzeForBlank(ImageTexture texture)
        {
            if (texture == null || texture.GetImage() == null)
                return true; // Consider missing image as blank

            Image image = texture.GetImage();

            int width = image.GetWidth();
            int height = image.GetHeight();
            int total = width * height;
            int nonWhite = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = image.GetPixel(x, y);
                    // "White" pixel: all channels above 0.94 (~240/255)
                    if (color.R < 0.94f || color.G < 0.94f || color.B < 0.94f)
                        nonWhite++;
                }
            }

            double fraction = (double)nonWhite / total;
            return fraction < ConfigManager.PilotConfig.BlankPageThreshold;
        }
    }


}
