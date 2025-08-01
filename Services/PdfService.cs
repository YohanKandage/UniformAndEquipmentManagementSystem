using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using System.IO;

namespace UniformAndEquipmentManagementSystem.Services
{
    public interface IPdfService
    {
        byte[] GenerateDocument(Action<Document> buildDocument);
        Style GetTitleStyle();
        Style GetSectionStyle();
        Style GetNormalStyle();
        Style GetTableHeaderStyle();
        Style GetTableContentStyle();
        Style GetHeaderTitleStyle();
        Style GetHeaderSubtitleStyle();
        Style GetCompanyInfoStyle();
        Style GetSignatureStyle();
        Style GetSignatureLabelStyle();
        Style GetDocumentNumberStyle();
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
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20)
                .SetBold()
                .SetFontColor(ColorConstants.DARK_GRAY);
        }

        public Style GetSectionStyle()
        {
            return new Style()
                .SetFontSize(14)
                .SetMarginTop(15)
                .SetMarginBottom(10)
                .SetBold()
                .SetFontColor(ColorConstants.BLUE);
        }

        public Style GetNormalStyle()
        {
            return new Style()
                .SetFontSize(10)
                .SetMarginBottom(5);
        }

        public Style GetTableHeaderStyle()
        {
            return new Style()
                .SetFontSize(10)
                .SetBold()
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
        }

        public Style GetTableContentStyle()
        {
            return new Style()
                .SetFontSize(10)
                .SetFontColor(ColorConstants.BLACK);
        }

        public Style GetHeaderTitleStyle()
        {
            return new Style()
                .SetFontSize(24)
                .SetBold()
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER);
        }

        public Style GetHeaderSubtitleStyle()
        {
            return new Style()
                .SetFontSize(18)
                .SetBold()
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5);
        }

        public Style GetCompanyInfoStyle()
        {
            return new Style()
                .SetFontSize(10)
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetMarginBottom(2);
        }

        public Style GetSignatureStyle()
        {
            return new Style()
                .SetFontSize(12)
                .SetBold()
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(40);
        }

        public Style GetSignatureLabelStyle()
        {
            return new Style()
                .SetFontSize(10)
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(5);
        }

        public Style GetDocumentNumberStyle()
        {
            return new Style()
                .SetFontSize(12)
                .SetBold()
                .SetFontColor(ColorConstants.BLUE)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginBottom(10);
        }
    }
} 