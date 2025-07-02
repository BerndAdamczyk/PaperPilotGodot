using Godot;
using System.Diagnostics;
using System.IO;

public static class PdfTextureLoader
{
    // Passe diesen Pfad ggf. an oder mache ihn per Konfiguration/Export sichtbar!
    private static readonly string PdftoppmPath = @"C:\Programmierung\Godot_Projekte\paperpilot\bin\poppler-24.08.0\Library\bin\pdftoppm.exe";

    /// <summary>
    /// Rendert eine PDF-Seite als PNG mit Poppler und gibt eine Godot-ImageTexture zurück.
    /// </summary>
    /// <param name="path">Pfad zur PDF-Datei</param>
    /// <param name="pageIndex">0-basierter Seitenindex (erste Seite = 0)</param>
    /// <param name="width">Breite der Ausgabe (ignoriert, Poppler nimmt Originalgröße)</param>
    /// <param name="height">Höhe der Ausgabe (ignoriert, Poppler nimmt Originalgröße)</param>
    /// <returns>ImageTexture bei Erfolg, sonst null</returns>
    public static ImageTexture? RenderPdfPage(string path, int pageIndex = 0, int width = 800, int height = 1000)
    {
        // pdftoppm ist 1-basiert
        int page = pageIndex + 1;
        string userDir = OS.GetUserDataDir();
        string tempBase = Path.Combine(userDir, "pdf_temp_page");
        string outputPng = $"{tempBase}-{page}.png";

        // Falls alte Datei existiert, löschen
        if (File.Exists(outputPng))
            File.Delete(outputPng);

        // PDF-Pfad globalisieren (Godot → absoluten Pfad)
        string absPdf = ProjectSettings.GlobalizePath(path);

        // Poppler-Aufruf vorbereiten
        var psi = new ProcessStartInfo
        {
            FileName = PdftoppmPath,
            Arguments = $"-png -f {page} -l {page} \"{absPdf}\" \"{tempBase}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        try
        {
            using (var proc = Process.Start(psi))
            {
                proc.WaitForExit();
            }

            // Warten, bis Datei existiert (Poppler braucht manchmal kurz)
            int tries = 0;
            while (!File.Exists(outputPng) && tries < 10)
            {
                System.Threading.Thread.Sleep(50);
                tries++;
            }

            if (File.Exists(outputPng))
            {
                var image = new Image();
                var err = image.Load(outputPng);
                if (err == Error.Ok)
                {
                    var tex = ImageTexture.CreateFromImage(image);
                    // Optional: temp-PNG löschen
                    // File.Delete(outputPng);
                    return tex;
                }
            }
            else
            {
                GD.PrintErr($"[PDF] PNG wurde von pdftoppm nicht erzeugt: {outputPng}");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[PDF] Fehler beim Rendern: {ex.Message}");
        }

        return null;
    }
}
