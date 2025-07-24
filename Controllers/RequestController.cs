using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.Extensions.Logging;
using UniformAndEquipmentManagementSystem.Services;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Layout.Borders;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Employee,PropertyManager,StockManager")]
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RequestController> _logger;
        private readonly IPdfService _pdfService;

        public RequestController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<RequestController> logger,
            IPdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Index(string employeeName, string department, string status, string searchString)
        {
            var user = await _userManager.GetUserAsync(User);
            var userEmail = user?.Email;

            if (User.IsInRole("PropertyManager"))
            {
                var query = _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.Item)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(employeeName))
                {
                    query = query.Where(r => 
                        (r.Employee.FirstName + " " + r.Employee.LastName).Contains(employeeName));
                }

                if (!string.IsNullOrEmpty(department))
                {
                    query = query.Where(r => r.Employee.Department.Name == department);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                    {
                        query = query.Where(r => r.Status == statusEnum);
                    }
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(r =>
                        r.Employee.FirstName.Contains(searchString) ||
                        r.Employee.LastName.Contains(searchString) ||
                        r.Employee.Department.Name.Contains(searchString) ||
                        r.Item.ItemName.Contains(searchString) ||
                        r.Reason.Contains(searchString));
                }

                var requests = await query.ToListAsync();

                // Get all departments
                var departments = await _context.Departments
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                // Pass filter values and departments to view
                ViewBag.EmployeeName = employeeName;
                ViewBag.Department = department;
                ViewBag.Status = status;
                ViewBag.SearchString = searchString;
                ViewBag.Departments = departments;

                return View("AllRequests", requests);
            }
            else
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (employee == null)
                {
                    return NotFound();
                }

                var requests = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.Item)
                    .Where(r => r.EmployeeId == employee.Id)
                    .ToListAsync();

                return View(requests);
            }
        }

        public async Task<IActionResult> Create()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            // Get department-specific items count for initial display
            var uniformCount = await _context.Items
                .CountAsync(i => i.DepartmentId == employee.DepartmentId && 
                               i.ItemType == "Uniform" && 
                               i.Status == "Available");

            var equipmentCount = await _context.Items
                .CountAsync(i => i.DepartmentId == employee.DepartmentId && 
                               i.ItemType == "Equipment" && 
                               i.Status == "Available");

            ViewBag.UniformCount = uniformCount;
            ViewBag.EquipmentCount = equipmentCount;
            ViewBag.DepartmentName = employee.Department?.Name;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsByType(string type)
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            var items = await _context.Items
                .Where(i => i.DepartmentId == employee.DepartmentId && 
                           i.ItemType == type && 
                           i.Status == "Available")
                .Select(i => new {
                    id = i.Id,
                    itemName = i.ItemName,
                    imagePath = i.ImagePath ?? "/images/default-item.png",
                    price = i.Price,
                    details = $"Department: {employee.Department.Name} | Material: {i.Material} | Price: Rs. {i.Price:F2} | Available: {i.Quantity}" + 
                             (i.ItemType == "Equipment" && i.Price > 1000 ? "\n\nNote: Please handover the previous fault items within 7 days to proceed the request" : "")
                })
                .ToListAsync();

            return Json(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemId,Reason")] Request request, string Size, List<IFormFile> ProofImages)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (employee == null)
                {
                    return NotFound();
                }

                // Log the incoming request data
                Console.WriteLine($"Debug - Request Data:");
                Console.WriteLine($"ItemId: {request.ItemId}");
                Console.WriteLine($"Reason: {request.Reason}");
                Console.WriteLine($"Size: {Size}");
                Console.WriteLine($"EmployeeId: {employee.Id}");

                // Validate that the requested item belongs to the employee's department
                var selectedItem = await _context.Items
                    .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.DepartmentId == employee.DepartmentId);

                if (selectedItem == null)
                {
                    ModelState.AddModelError("ItemId", "The selected item is not available for your department");
                    return View(request);
                }

                // Log the selected item
                Console.WriteLine($"Debug - Selected Item:");
                Console.WriteLine($"ItemType: {selectedItem.ItemType}");
                Console.WriteLine($"ItemName: {selectedItem.ItemName}");
                Console.WriteLine($"DepartmentId: {selectedItem.DepartmentId}");

                // Validate size for uniform requests
                if (selectedItem.ItemType == "Uniform" && string.IsNullOrEmpty(Size))
                {
                    ModelState.AddModelError("Size", "Size is required for uniform requests");
                    return View(request);
                }

                // Validate quantity
                if (selectedItem.Quantity <= 0)
                {
                    ModelState.AddModelError("ItemId", "This item is currently out of stock");
                    return View(request);
                }

                // Set required fields
                request.EmployeeId = employee.Id;
                request.RequestDate = DateTime.Now;
                request.Status = RequestStatus.Pending;

                // Add size and department to the reason if it's a uniform request
                if (selectedItem.ItemType == "Uniform")
                {
                    request.Reason = $"Department: {employee.Department.Name}\nSize: {Size}\n\nReason: {request.Reason}";
                }
                else
                {
                    request.Reason = $"Department: {employee.Department.Name}\n\nReason: {request.Reason}";
                }

                // Handle proof images
                if (ProofImages != null && ProofImages.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "proofs");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    for (int i = 0; i < Math.Min(ProofImages.Count, 3); i++)
                    {
                        var file = ProofImages[i];
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            var relativePath = "/images/proofs/" + uniqueFileName;
                            if (i == 0) request.ProofImage1 = relativePath;
                            if (i == 1) request.ProofImage2 = relativePath;
                            if (i == 2) request.ProofImage3 = relativePath;
                        }
                    }
                }

                // Log the final request object
                Console.WriteLine($"Debug - Final Request Object:");
                Console.WriteLine($"EmployeeId: {request.EmployeeId}");
                Console.WriteLine($"ItemId: {request.ItemId}");
                Console.WriteLine($"RequestDate: {request.RequestDate}");
                Console.WriteLine($"Status: {request.Status}");
                Console.WriteLine($"Reason: {request.Reason}");

                _context.Add(request);
                var result = await _context.SaveChangesAsync();
                Console.WriteLine($"Debug - SaveChanges Result: {result}");

                if (result > 0)
                {
                    TempData["Success"] = "Request submitted successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to save the request. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating request: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An error occurred while submitting your request. Please try again.");
            }
            return View(request);
        }

        public async Task<IActionResult> AssignedItems()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            var assignedItems = await _context.ItemAssignments
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Department)
                .Where(ia => ia.EmployeeId == employee.Id && ia.Status == "Assigned")
                .ToListAsync();

            return View(assignedItems);
        }

        public async Task<IActionResult> MyInventory()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            // Get assigned items for this employee from ItemAssignment table
            var assignedItems = await _context.ItemAssignments
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Department)
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Supplier)
                .Where(ia => ia.EmployeeId == employee.Id && ia.Status == "Assigned")
                .OrderByDescending(ia => ia.AssignedDate)
                .ToListAsync();

            return View(assignedItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int itemId, int assignmentId)
        {
            try
            {
                var assignment = await _context.ItemAssignments
                    .Include(ia => ia.Item)
                        .ThenInclude(i => i.Department)
                    .Include(ia => ia.Item)
                        .ThenInclude(i => i.Supplier)
                    .Include(ia => ia.Employee)
                    .Include(ia => ia.Request)
                    .FirstOrDefaultAsync(ia => ia.Id == assignmentId && ia.ItemId == itemId);

                if (assignment == null)
                {
                    return PartialView("_ItemDetailsError", "Item assignment not found.");
                }

                return PartialView("_ItemDetails", assignment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item details for item {ItemId} and assignment {AssignmentId}", itemId, assignmentId);
                return PartialView("_ItemDetailsError", "Error loading item details.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAssignmentPdf(int assignmentId)
        {
            var assignment = await _context.ItemAssignments
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Department)
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Supplier)
                .Include(ia => ia.Employee)
                .Include(ia => ia.Request)
                .FirstOrDefaultAsync(ia => ia.Id == assignmentId);

            if (assignment == null)
            {
                return NotFound();
            }

            var pdfBytes = _pdfService.GenerateDocument(doc =>
            {
                // Professional Header with Logo and Company Information
                var headerTable = new Table(3).UseAllAvailableWidth();
                headerTable.SetMarginBottom(30);
                
                // FMI Logo Section
                var logoCell = new Cell().Add(
                    new Paragraph("FMI").AddStyle(_pdfService.GetHeaderTitleStyle())
                );
                logoCell.SetWidth(120);
                logoCell.SetHeight(80);
                logoCell.SetBackgroundColor(ColorConstants.BLUE);
                logoCell.SetBorder(null);
                logoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Company Information Section
                var companyInfoCell = new Cell().Add(
                    new Paragraph("FMI COMPANY LIMITED").AddStyle(_pdfService.GetHeaderSubtitleStyle())
                ).Add(
                    new Paragraph("23, Galle Road, Colombo 04").AddStyle(_pdfService.GetCompanyInfoStyle())
                ).Add(
                    new Paragraph("Sri Lanka").AddStyle(_pdfService.GetCompanyInfoStyle())
                ).Add(
                    new Paragraph("Phone: +94 11 234 5678").AddStyle(_pdfService.GetCompanyInfoStyle())
                ).Add(
                    new Paragraph("Email: info@fmi.com").AddStyle(_pdfService.GetCompanyInfoStyle())
                ).Add(
                    new Paragraph("Website: www.fmi.com").AddStyle(_pdfService.GetCompanyInfoStyle())
                );
                companyInfoCell.SetBorder(null);
                companyInfoCell.SetTextAlignment(TextAlignment.LEFT);
                companyInfoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Document Number Section
                var documentNumberCell = new Cell().Add(
                    new Paragraph($"Document #: {assignment.Id:D6}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Date: {DateTime.Now:MMM dd, yyyy}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Time: {DateTime.Now:HH:mm}").AddStyle(_pdfService.GetDocumentNumberStyle())
                );
                documentNumberCell.SetBorder(null);
                documentNumberCell.SetTextAlignment(TextAlignment.RIGHT);
                documentNumberCell.SetVerticalAlignment(VerticalAlignment.TOP);
                
                headerTable.AddCell(logoCell);
                headerTable.AddCell(companyInfoCell);
                headerTable.AddCell(documentNumberCell);
                doc.Add(headerTable);

                // Document Title with decorative line
                doc.Add(new Paragraph("ITEM ASSIGNMENT RECEIPT").AddStyle(_pdfService.GetTitleStyle()));
                
                // Decorative line
                var line = new Paragraph("").SetBorderBottom(new SolidBorder(ColorConstants.BLUE, 2)).SetMarginBottom(20);
                doc.Add(line);
                
                // Assignment Details Section
                doc.Add(new Paragraph("Assignment Information").AddStyle(_pdfService.GetSectionStyle()));
                
                var assignmentTable = new Table(2).UseAllAvailableWidth();
                assignmentTable.SetMarginBottom(25);
                assignmentTable.SetBorder(null);
                
                AddTableRow(assignmentTable, "Assignment ID", $"#{assignment.Id:D6}");
                AddTableRow(assignmentTable, "Assignment Date", assignment.AssignedDate.ToString("MMMM dd, yyyy"));
                AddTableRow(assignmentTable, "Assignment Time", assignment.AssignedDate.ToString("hh:mm tt"));
                AddTableRow(assignmentTable, "Status", assignment.Status);
                if (!string.IsNullOrEmpty(assignment.Remarks))
                    AddTableRow(assignmentTable, "Remarks", assignment.Remarks);
                if (assignment.Cost.HasValue)
                    AddTableRow(assignmentTable, "Cost", $"Rs. {assignment.Cost.Value:N2}");
                
                doc.Add(assignmentTable);

                // Item Details Section
                doc.Add(new Paragraph("Item Details").AddStyle(_pdfService.GetSectionStyle()));
                
                var itemTable = new Table(2).UseAllAvailableWidth();
                itemTable.SetMarginBottom(25);
                itemTable.SetBorder(null);
                
                AddTableRow(itemTable, "Item Name", assignment.Item.ItemName);
                AddTableRow(itemTable, "Item ID", assignment.Item.ItemId);
                AddTableRow(itemTable, "Item Type", assignment.Item.ItemType);
                AddTableRow(itemTable, "Material", assignment.Item.Material);
                AddTableRow(itemTable, "Price", $"Rs. {assignment.Item.Price:N2}");
                AddTableRow(itemTable, "Department", assignment.Item.Department?.Name ?? "N/A");
                AddTableRow(itemTable, "Supplier", assignment.Item.Supplier?.CompanyName ?? "N/A");
                
                doc.Add(itemTable);

                // Employee Details Section
                doc.Add(new Paragraph("Employee Information").AddStyle(_pdfService.GetSectionStyle()));
                
                var employeeTable = new Table(2).UseAllAvailableWidth();
                employeeTable.SetMarginBottom(25);
                employeeTable.SetBorder(null);
                
                AddTableRow(employeeTable, "Employee Name", $"{assignment.Employee?.FirstName} {assignment.Employee?.LastName}");
                AddTableRow(employeeTable, "Employee ID", assignment.Employee?.EmployeeId ?? "N/A");
                AddTableRow(employeeTable, "Department", assignment.Employee?.Department?.Name ?? "N/A");
                AddTableRow(employeeTable, "Email", assignment.Employee?.Email ?? "N/A");
                
                doc.Add(employeeTable);

                // Request Information (if available)
                if (assignment.Request != null)
                {
                    doc.Add(new Paragraph("Request Information").AddStyle(_pdfService.GetSectionStyle()));
                    
                    var requestTable = new Table(2).UseAllAvailableWidth();
                    requestTable.SetMarginBottom(25);
                    requestTable.SetBorder(null);
                    
                    AddTableRow(requestTable, "Request Date", assignment.Request.RequestDate.ToString("MMMM dd, yyyy"));
                    AddTableRow(requestTable, "Request Status", assignment.Request.Status.ToString().Replace("By", " by "));
                    if (!string.IsNullOrEmpty(assignment.Request.Reason))
                        AddTableRow(requestTable, "Reason", assignment.Request.Reason);
                    if (!string.IsNullOrEmpty(assignment.Request.AdminComment))
                        AddTableRow(requestTable, "Admin Comment", assignment.Request.AdminComment);
                    
                    doc.Add(requestTable);
                }

                // Terms and Conditions
                doc.Add(new Paragraph("Terms and Conditions").AddStyle(_pdfService.GetSectionStyle()));
                doc.Add(new Paragraph("1. This item is assigned to the employee for official use only.").AddStyle(_pdfService.GetNormalStyle()));
                doc.Add(new Paragraph("2. The employee is responsible for the care and maintenance of the assigned item.").AddStyle(_pdfService.GetNormalStyle()));
                doc.Add(new Paragraph("3. Items must be returned in good condition upon termination or transfer.").AddStyle(_pdfService.GetNormalStyle()));
                doc.Add(new Paragraph("4. Any damage or loss must be reported immediately to the department supervisor.").AddStyle(_pdfService.GetNormalStyle()));
                doc.Add(new Paragraph("5. The assigned item remains the property of FMI Company Limited.").AddStyle(_pdfService.GetNormalStyle()));

                // Signature Section
                doc.Add(new Paragraph("").SetMarginTop(40));
                
                var signatureTable = new Table(2).UseAllAvailableWidth();
                signatureTable.SetMarginBottom(30);
                signatureTable.SetBorder(null);
                
                // Administrator Signature
                var adminSignatureCell = new Cell();
                adminSignatureCell.SetBorder(null);
                adminSignatureCell.SetTextAlignment(TextAlignment.CENTER);
                adminSignatureCell.Add(new Paragraph("_________________________").AddStyle(_pdfService.GetSignatureStyle()));
                adminSignatureCell.Add(new Paragraph("Administrator").AddStyle(_pdfService.GetSignatureLabelStyle()));
                adminSignatureCell.Add(new Paragraph("FMI Company Limited").AddStyle(_pdfService.GetSignatureLabelStyle()));
                
                // Stock Manager Signature
                var stockManagerSignatureCell = new Cell();
                stockManagerSignatureCell.SetBorder(null);
                stockManagerSignatureCell.SetTextAlignment(TextAlignment.CENTER);
                stockManagerSignatureCell.Add(new Paragraph("_________________________").AddStyle(_pdfService.GetSignatureStyle()));
                stockManagerSignatureCell.Add(new Paragraph("Stock Manager").AddStyle(_pdfService.GetSignatureLabelStyle()));
                stockManagerSignatureCell.Add(new Paragraph("FMI Company Limited").AddStyle(_pdfService.GetSignatureLabelStyle()));
                
                signatureTable.AddCell(adminSignatureCell);
                signatureTable.AddCell(stockManagerSignatureCell);
                doc.Add(signatureTable);

                // Employee Acknowledgment
                doc.Add(new Paragraph("").SetMarginTop(20));
                var acknowledgmentTable = new Table(1).UseAllAvailableWidth();
                acknowledgmentTable.SetBorder(null);
                
                var acknowledgmentCell = new Cell();
                acknowledgmentCell.SetBorder(null);
                acknowledgmentCell.SetTextAlignment(TextAlignment.CENTER);
                acknowledgmentCell.Add(new Paragraph("_________________________").AddStyle(_pdfService.GetSignatureStyle()));
                acknowledgmentCell.Add(new Paragraph("Employee Signature").AddStyle(_pdfService.GetSignatureLabelStyle()));
                acknowledgmentCell.Add(new Paragraph($"{assignment.Employee?.FirstName} {assignment.Employee?.LastName}").AddStyle(_pdfService.GetSignatureLabelStyle()));
                acknowledgmentCell.Add(new Paragraph("I acknowledge receipt of the above assigned item(s)").AddStyle(_pdfService.GetSignatureLabelStyle()));
                
                acknowledgmentTable.AddCell(acknowledgmentCell);
                doc.Add(acknowledgmentTable);

                // Footer
                doc.Add(new Paragraph("").SetMarginTop(40));
                var footerTable = new Table(1).UseAllAvailableWidth();
                footerTable.SetMarginTop(20);
                
                var footerCell = new Cell().Add(
                    new Paragraph("Generated on: " + DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt")).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER)
                ).Add(
                    new Paragraph("This is an official FMI document - Keep for your records").SetFontSize(8).SetTextAlignment(TextAlignment.CENTER)
                ).Add(
                    new Paragraph("FMI Company Limited - Uniform and Equipment Management System").SetFontSize(8).SetTextAlignment(TextAlignment.CENTER)
                );
                footerCell.SetBorder(null);
                footerCell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                footerTable.AddCell(footerCell);
                
                doc.Add(footerTable);
            });

            return File(pdfBytes, "application/pdf", $"FMI_Assignment_Receipt_{assignment.Id:D6}.pdf");
        }

        private void AddTableRow(Table table, string label, string value)
        {
            var labelCell = new Cell().Add(new Paragraph(label).AddStyle(_pdfService.GetTableHeaderStyle()));
            labelCell.SetBorder(null);
            labelCell.SetPadding(10);
            labelCell.SetWidth(150);
            labelCell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            
            var valueCell = new Cell().Add(new Paragraph(value).AddStyle(_pdfService.GetTableContentStyle()));
            valueCell.SetBorder(null);
            valueCell.SetPadding(10);
            valueCell.SetBackgroundColor(ColorConstants.WHITE);
            
            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadRequestPdf(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.Item)
                .Include(r => r.ProcessedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            var pdfBytes = _pdfService.GenerateDocument(doc =>
            {
                // Header with FMI Logo and Company Information
                var headerTable = new Table(3).UseAllAvailableWidth();
                headerTable.SetMarginBottom(30);
                
                // FMI Logo Section (Blue background with FMI text)
                var logoCell = new Cell().Add(
                    new Paragraph("FMI").AddStyle(_pdfService.GetHeaderTitleStyle())
                );
                logoCell.SetWidth(120);
                logoCell.SetHeight(80);
                logoCell.SetBackgroundColor(ColorConstants.BLUE);
                logoCell.SetBorder(null);
                logoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Company Information Section
                var companyInfoCell = new Cell().Add(
                    new Paragraph("Facilities Management Integrated (Pvt) Ltd.").AddStyle(_pdfService.GetHeaderSubtitleStyle())
                ).Add(
                    new Paragraph("Telephone: (+94) 11 59 40740").AddStyle(_pdfService.GetCompanyInfoStyle())
                ).Add(
                    new Paragraph("Address: No. 490, Oceanica Towers, Colombo 03").AddStyle(_pdfService.GetCompanyInfoStyle())
                );
                companyInfoCell.SetBorder(null);
                companyInfoCell.SetTextAlignment(TextAlignment.LEFT);
                companyInfoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Document Number Section
                var documentNumberCell = new Cell().Add(
                    new Paragraph($"Document #: {request.Id:D6}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Date: {DateTime.Now:MMM dd, yyyy}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Time: {DateTime.Now:HH:mm}").AddStyle(_pdfService.GetDocumentNumberStyle())
                );
                documentNumberCell.SetBorder(null);
                documentNumberCell.SetTextAlignment(TextAlignment.RIGHT);
                documentNumberCell.SetVerticalAlignment(VerticalAlignment.TOP);
                
                headerTable.AddCell(logoCell);
                headerTable.AddCell(companyInfoCell);
                headerTable.AddCell(documentNumberCell);
                doc.Add(headerTable);

                // Filter Information Section
                var filterTable = new Table(2).UseAllAvailableWidth();
                filterTable.SetMarginBottom(20);
                filterTable.SetBorder(null);
                
                AddTableRow(filterTable, "Start Date:", "ALL");
                AddTableRow(filterTable, "Status:", "ALL");
                AddTableRow(filterTable, "End Date:", "ALL");
                
                doc.Add(filterTable);

                // Document Title
                doc.Add(new Paragraph("Request Report").AddStyle(_pdfService.GetTitleStyle()));
                
                // Decorative line
                var line = new Paragraph("").SetBorderBottom(new SolidBorder(ColorConstants.BLUE, 2)).SetMarginBottom(20);
                doc.Add(line);
                
                // Request Details Section
                doc.Add(new Paragraph("Request Information").AddStyle(_pdfService.GetSectionStyle()));
                
                var requestTable = new Table(2).UseAllAvailableWidth();
                requestTable.SetMarginBottom(25);
                requestTable.SetBorder(null);
                
                AddTableRow(requestTable, "Request ID", $"#{request.Id:D6}");
                AddTableRow(requestTable, "Request Date", request.RequestDate.ToString("MMMM dd, yyyy"));
                AddTableRow(requestTable, "Request Time", request.RequestDate.ToString("hh:mm tt"));
                AddTableRow(requestTable, "Status", request.Status.ToString().Replace("By", " by "));
                if (request.ProcessedDate.HasValue)
                {
                    AddTableRow(requestTable, "Processed Date", request.ProcessedDate.Value.ToString("MMMM dd, yyyy"));
                    AddTableRow(requestTable, "Processed Time", request.ProcessedDate.Value.ToString("hh:mm tt"));
                }
                if (!string.IsNullOrEmpty(request.Reason))
                    AddTableRow(requestTable, "Reason", request.Reason);
                if (!string.IsNullOrEmpty(request.Remarks))
                    AddTableRow(requestTable, "Remarks", request.Remarks);
                if (request.Cost.HasValue)
                    AddTableRow(requestTable, "Cost", $"Rs. {request.Cost.Value:N2}");
                
                doc.Add(requestTable);

                // Employee Details Section
                doc.Add(new Paragraph("Employee Information").AddStyle(_pdfService.GetSectionStyle()));
                
                var employeeTable = new Table(2).UseAllAvailableWidth();
                employeeTable.SetMarginBottom(25);
                employeeTable.SetBorder(null);
                
                AddTableRow(employeeTable, "Employee Name", $"{request.Employee?.FirstName} {request.Employee?.LastName}");
                AddTableRow(employeeTable, "Employee ID", request.Employee?.EmployeeId ?? "N/A");
                AddTableRow(employeeTable, "Department", request.Employee?.Department?.Name ?? "N/A");
                AddTableRow(employeeTable, "Email", request.Employee?.Email ?? "N/A");
                
                doc.Add(employeeTable);

                // Item Details Section
                doc.Add(new Paragraph("Item Information").AddStyle(_pdfService.GetSectionStyle()));
                
                var itemTable = new Table(2).UseAllAvailableWidth();
                itemTable.SetMarginBottom(25);
                itemTable.SetBorder(null);
                
                AddTableRow(itemTable, "Item Name", request.Item?.ItemName ?? "N/A");
                AddTableRow(itemTable, "Item ID", request.Item?.ItemId ?? "N/A");
                AddTableRow(itemTable, "Item Type", request.Item?.ItemType ?? "N/A");
                AddTableRow(itemTable, "Material", request.Item?.Material ?? "N/A");
                AddTableRow(itemTable, "Price", request.Item?.Price != null ? $"Rs. {request.Item.Price:N2}" : "N/A");
                
                doc.Add(itemTable);

                // Signature Footer with Dotted Lines
                doc.Add(new Paragraph("").SetMarginTop(40));
                
                var signatureTable = new Table(3).UseAllAvailableWidth();
                signatureTable.SetMarginTop(20);
                
                // Prepared By
                var preparedByCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Prepared By").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                preparedByCell.SetBorder(null);
                preparedByCell.SetTextAlignment(TextAlignment.CENTER);
                
                // Authorized By
                var authorizedByCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Authorized By").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                authorizedByCell.SetBorder(null);
                authorizedByCell.SetTextAlignment(TextAlignment.CENTER);
                
                // Issued Date
                var issuedDateCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Issued Date").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                issuedDateCell.SetBorder(null);
                issuedDateCell.SetTextAlignment(TextAlignment.CENTER);
                
                signatureTable.AddCell(preparedByCell);
                signatureTable.AddCell(authorizedByCell);
                signatureTable.AddCell(issuedDateCell);
                
                doc.Add(signatureTable);
            });

            return File(pdfBytes, "application/pdf", $"FMI_Request_Report_{request.Id:D6}.pdf");
        }

        // GET: Request/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.Item)
                .Include(r => r.ProcessedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // GET: Request/Process/5
        [Authorize(Roles = "PropertyManager,StockManager")]
        public async Task<IActionResult> Process(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.Pending)
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            return View(request);
        }

        // POST: Request/Process/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager,StockManager")]
        public async Task<IActionResult> Process(int id, string status, string remarks)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.Pending)
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            var userEmail = User.Identity?.Name;
            var processedBy = await _userManager.FindByEmailAsync(userEmail);

            if (processedBy == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
            {
                request.Status = statusEnum;
            }
            else
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction(nameof(Index));
            }

            request.Remarks = remarks;
            request.ProcessedById = processedBy.Id;
            request.ProcessedDate = DateTime.Now;

            if (statusEnum == RequestStatus.ApprovedByPropertyManager && request.Item != null)
            {
                // Property Manager approval should NOT reduce inventory
                // Inventory will only be reduced when Admin approves
                // Just update the status, no quantity changes
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Request has been {statusEnum.ToString().ToLower()} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestId}", id);
                TempData["Error"] = "An error occurred while processing the request.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 