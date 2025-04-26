using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.IO;

namespace UniformAndEquipmentManagementSystem.Services
{
    public interface IPdfService
    {
        byte[] GenerateDocument(Action<Document> buildDocument);
        Style GetTitleStyle();
        Style GetSectionStyle();
        Style GetNormalStyle();
    }

    public class PdfService : IPdfService
    {
        public byte[] GenerateDocument(Action<Document> buildDocument)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                
                // Set margins (in points)
                document.SetMargins(50, 50, 50, 50);
                
                buildDocument(document);
                
                document.Close();
                return stream.ToArray();
            }
        }

        public Style GetTitleStyle()
        {
            return new Style()
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
        }

        public Style GetSectionStyle()
        {
            return new Style()
                .SetFontSize(14)
                .SetMarginTop(15)
                .SetMarginBottom(10);
        }

        public Style GetNormalStyle()
        {
            return new Style()
                .SetFontSize(12)
                .SetMarginBottom(5);
        }
    }
} 