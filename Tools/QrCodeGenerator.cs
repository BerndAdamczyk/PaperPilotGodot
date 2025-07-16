using System.IO;
using Docnet.Core;
using Docnet.Core.Editors;
using QRCoder;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace PaperPilot.Tools
{
    public static class QrCodeGenerator
    {
        public static void GenerateSplitMarkerPdf(string path)
        {
            // 1. Generate QR Code image
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(PdfAnalysis.SPLIT_MARKER, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImageBytes = qrCode.GetGraphic(20);

            // 2. Create a beautiful A4 page with the QR code
            byte[] finalImageBytes;
            const int a4Width = 2480; // A4 at 300 DPI
            const int a4Height = 3508;

            using (var qrImage = (Bitmap)Image.FromStream(new MemoryStream(qrCodeImageBytes)))
            using (var finalBitmap = new Bitmap(a4Width, a4Height))
            using (var g = Graphics.FromImage(finalBitmap))
            {
                // Fill background with white
                g.Clear(Color.White);

                // Position QR code in the center
                int x = (a4Width - qrImage.Width) / 2;
                int y = (a4Height - qrImage.Height) / 2;
                g.DrawImage(qrImage, x, y);

                // Add a title
                string title = "Paper Pilot Split Marker";
                using (var font = new Font("Arial", 50, FontStyle.Bold))
                {
                    var textSize = g.MeasureString(title, font);
                    float titleX = (a4Width - textSize.Width) / 2;
                    float titleY = y - textSize.Height - 40; // 40px padding
                    g.DrawString(title, font, Brushes.Black, titleX, titleY);
                }

                // Save final image to a byte array
                using (var ms = new MemoryStream())
                {
                    finalBitmap.Save(ms, ImageFormat.Jpeg);
                    finalImageBytes = ms.ToArray();
                }
            }

            // 3. Create JpegImage object for Docnet
            var jpegImage = new JpegImage
            {
                Bytes = finalImageBytes,
                Width = a4Width,
                Height = a4Height
            };

            // 4. Use Docnet to convert the image to a PDF
            byte[] pdfBytes = DocLib.Instance.JpegToPdf(new List<JpegImage> { jpegImage });

            // 5. Save PDF
            File.WriteAllBytes(path, pdfBytes);
        }
    }
}
