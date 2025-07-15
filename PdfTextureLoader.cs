#nullable enable

using Godot;
using System.Diagnostics;
using System.IO;
using Docnet.Core;
using Docnet.Core.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using Docnet.Core.Readers;


public static class PdfTextureLoader
{
    

    /// <summary>
    /// Rendert eine PDF-Seite als PNG mit Poppler und gibt eine Godot-ImageTexture zurück.
    /// </summary>
    /// <param name="path">Pfad zur PDF-Datei</param>
    /// <param name="pageIndex">0-basierter Seitenindex (erste Seite = 0)</param>
    /// <param name="width">Breite der Ausgabe (ignoriert, Poppler nimmt Originalgröße)</param>
    /// <param name="height">Höhe der Ausgabe (ignoriert, Poppler nimmt Originalgröße)</param>
    /// <returns>ImageTexture bei Erfolg, sonst null</returns>
    //public static ImageTexture? RenderPdfPage(string path, int pageIndex = 0, int width = 800, int height = 1000)
    //{
    //    // pdftoppm ist 1-basiert
    //    int page = pageIndex + 1;
    //    string userDir = OS.GetUserDataDir();
    //    string tempBase = Path.Combine(userDir, "pdf_temp_page");
    //    string outputPng = $"{tempBase}-{page}.png";

    //    // Falls alte Datei existiert, löschen
    //    if (File.Exists(outputPng))
    //        File.Delete(outputPng);

    //    // PDF-Pfad globalisieren (Godot → absoluten Pfad)
    //    string absPdf = ProjectSettings.GlobalizePath(path);

    //    // Poppler-Aufruf vorbereiten
    //    var psi = new ProcessStartInfo
    //    {
    //        FileName = PdftoppmPath,
    //        Arguments = $"-png -f {page} -l {page} \"{absPdf}\" \"{tempBase}\"",
    //        UseShellExecute = false,
    //        CreateNoWindow = true,
    //        RedirectStandardOutput = false,
    //        RedirectStandardError = false
    //    };

    //    try
    //    {
    //        using (var proc = Process.Start(psi))
    //        {
    //            proc.WaitForExit();
    //        }

    //        // Warten, bis Datei existiert (Poppler braucht manchmal kurz)
    //        int tries = 0;
    //        while (!File.Exists(outputPng) && tries < 10)
    //        {
    //            System.Threading.Thread.Sleep(50);
    //            tries++;
    //        }

    //        if (File.Exists(outputPng))
    //        {
    //            var image = new Image();
    //            var err = image.Load(outputPng);
    //            if (err == Error.Ok)
    //            {
    //                var tex = ImageTexture.CreateFromImage(image);
    //                // Optional: temp-PNG löschen
    //                // File.Delete(outputPng);
    //                return tex;
    //            }
    //        }
    //        else
    //        {
    //            GD.PrintErr($"[PDF] PNG wurde von pdftoppm nicht erzeugt: {outputPng}");
    //        }
    //    }
    //    catch (System.Exception ex)
    //    {
    //        GD.PrintErr($"[PDF] Fehler beim Rendern: {ex.Message}");
    //    }

    //    return null;
    //}

    public static ImageTexture? RenderPdfPage(string path, int pageIndex = 0, int width = 800, int height = 1000)
    {
        try
        {
            using (var docReader = DocLib.Instance.GetDocReader(path, new PageDimensions(width, height)))
            using (var pageReader = docReader.GetPageReader(pageIndex))
            {
                int w = pageReader.GetPageWidth();
                int h = pageReader.GetPageHeight();
                var rawBytes = pageReader.GetImage(); // BGRA32 format

                // Convert BGRA to RGBA
                byte[] rgba = new byte[w * h * 4];
                for (int i = 0; i < w * h; i++)
                {
                    rgba[i * 4 + 0] = rawBytes[i * 4 + 2]; // R
                    rgba[i * 4 + 1] = rawBytes[i * 4 + 1]; // G
                    rgba[i * 4 + 2] = rawBytes[i * 4 + 0]; // B
                    rgba[i * 4 + 3] = rawBytes[i * 4 + 3]; // A
                }

                // Nutze CreateFromData statt SetData
                var gdImage = Godot.Image.CreateFromData(
                    w, h, false, Godot.Image.Format.Rgba8, rgba);

                var texture = new ImageTexture();
                texture.SetImage(gdImage);
                return texture;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"RenderPdfPage failed: {ex.Message}");
            return null;
        }
    }
    public static ImageTexture? RenderPdfPage(IPageReader pageReader, int pageIndex = 0, int width = 800, int height = 1000)
    {
        try
        {
            int w = pageReader.GetPageWidth();
            int h = pageReader.GetPageHeight();
            var rawBytes = pageReader.GetImage(); // BGRA32 format

            // Convert BGRA to RGBA
            byte[] rgba = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                rgba[i * 4 + 0] = rawBytes[i * 4 + 2]; // R
                rgba[i * 4 + 1] = rawBytes[i * 4 + 1]; // G
                rgba[i * 4 + 2] = rawBytes[i * 4 + 0]; // B
                rgba[i * 4 + 3] = rawBytes[i * 4 + 3]; // A
            }

            // Nutze CreateFromData statt SetData
            var gdImage = Godot.Image.CreateFromData(
                w, h, false, Godot.Image.Format.Rgba8, rgba);

            var texture = new ImageTexture();
            texture.SetImage(gdImage);
            return texture;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"RenderPdfPage failed: {ex.Message}");
            return null;
        }
    }
}
