using Godot;
using System.Drawing.Imaging;
using System.IO;
using System;

public partial class PaperButton : Button
{
    public override void _Ready()
    {
        base._Ready();

        string absolutePath =
            ProjectSettings.GlobalizePath("res://Test-PDF.pdf");
        Icon = PdfTextureLoader.RenderPdfPage(absolutePath);

    }
}
