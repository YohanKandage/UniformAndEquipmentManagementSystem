using ClosedXML.Excel;
using System.Data;

namespace UniformAndEquipmentManagementSystem.Services
{
    public interface IExcelService
    {
        byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1");
        byte[] ExportToExcel(DataTable dataTable, string sheetName = "Sheet1");
        byte[] ExportToExcel(DataTable dataTable, string sheetName, string title);
    }

    public class ExcelService : IExcelService
    {
        public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            // Get properties of the type
            var properties = typeof(T).GetProperties();

            // Add headers
            for (int i = 0; i < properties.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = properties[i].Name;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Add data
            int row = 2;
            foreach (var item in data)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    var value = properties[i].GetValue(item);
                    worksheet.Cell(row, i + 1).Value = value?.ToString() ?? "";
                }
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders to all cells
            var dataRange = worksheet.Range(1, 1, row - 1, properties.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToExcel(DataTable dataTable, string sheetName = "Sheet1")
        {
            return ExportToExcel(dataTable, sheetName, null);
        }

        public byte[] ExportToExcel(DataTable dataTable, string sheetName, string title)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            int startRow = 1;

            // Add title if provided
            if (!string.IsNullOrEmpty(title))
            {
                var titleCell = worksheet.Cell(startRow, 1);
                titleCell.Value = title;
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 14;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range(startRow, 1, startRow, dataTable.Columns.Count).Merge();
                startRow = 2;
            }

            // Add headers
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var cell = worksheet.Cell(startRow, i + 1);
                cell.Value = dataTable.Columns[i].ColumnName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Add data
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    var value = dataTable.Rows[row][col];
                    var cell = worksheet.Cell(startRow + row + 1, col + 1);
                    cell.Value = value?.ToString() ?? "";
                    
                    // Apply conditional formatting for status columns
                    if (dataTable.Columns[col].ColumnName.ToLower().Contains("status"))
                    {
                        var statusValue = value?.ToString().ToLower();
                        if (statusValue == "pending" || statusValue == "low stock")
                        {
                            cell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                        }
                        else if (statusValue == "approved" || statusValue == "available")
                        {
                            cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        }
                        else if (statusValue == "cancelled" || statusValue == "out of stock")
                        {
                            cell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                        }
                    }

                    // Apply conditional formatting for quantity columns
                    if (dataTable.Columns[col].ColumnName.ToLower().Contains("quantity"))
                    {
                        if (int.TryParse(value?.ToString(), out int quantity))
                        {
                            if (quantity == 0)
                            {
                                cell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                            }
                            else if (quantity < 10)
                            {
                                cell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                            }
                        }
                    }
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders to all cells
            var dataRange = worksheet.Range(startRow, 1, startRow + dataTable.Rows.Count, dataTable.Columns.Count);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Add alternating row colors for better readability
            for (int row = startRow + 1; row <= startRow + dataTable.Rows.Count; row++)
            {
                if (row % 2 == 0)
                {
                    var rowRange = worksheet.Range(row, 1, row, dataTable.Columns.Count);
                    rowRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
            }

            // Freeze the header row
            worksheet.SheetView.FreezeRows(startRow);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
} 