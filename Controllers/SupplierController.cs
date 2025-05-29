using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Services;
using System.Threading.Tasks;
using System.Linq;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using System;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,StockManager")]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;

        public SupplierController(ApplicationDbContext context, IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // GET: Supplier
        public async Task<IActionResult> Index(string companyName, string email, string contactNo, string category)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(companyName))
            {
                query = query.Where(s => s.CompanyName.Contains(companyName));
            }

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(s => s.Email.Contains(email));
            }

            if (!string.IsNullOrEmpty(contactNo))
            {
                query = query.Where(s => s.ContactNo.Contains(contactNo));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.SupplierCategory == category);
            }

            var suppliers = await query.ToListAsync();

            // Pass filter values to view
            ViewBag.CompanyName = companyName;
            ViewBag.Email = email;
            ViewBag.ContactNo = contactNo;
            ViewBag.Category = category;

            return View(suppliers);
        }

        // GET: Supplier/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Supplier/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CompanyName,CompanyCode,Email,ContactNo,ContactPerson,ContactPersonEmail,Address,Province,District,PostalCode,ApprovalState,IsActive,SupplierCategory,ContractStartDate,ContractEndDate,PaymentTerms,DeliveryTerms")] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Supplier/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Supplier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyName,CompanyCode,Email,ContactNo,ContactPerson,ContactPersonEmail,Address,Province,District,PostalCode,ApprovalState,IsActive,SupplierCategory,ContractStartDate,ContractEndDate,PaymentTerms,DeliveryTerms")] Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Supplier/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Supplier/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Supplier/DownloadPdf/5
        public async Task<IActionResult> DownloadPdf(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            var pdfBytes = _pdfService.GenerateDocument(document =>
            {
                // Title
                var title = new Paragraph($"Supplier Details - {supplier.CompanyName}")
                    .AddStyle(_pdfService.GetTitleStyle());
                document.Add(title);

                // Company Information Section
                var companySection = new Paragraph("Company Information")
                    .AddStyle(_pdfService.GetSectionStyle());
                document.Add(companySection);

                var companyInfoTable = new Table(2).UseAllAvailableWidth();
                companyInfoTable.AddCell("Company Code").AddCell(supplier.CompanyCode);
                companyInfoTable.AddCell("Company Name").AddCell(supplier.CompanyName);
                companyInfoTable.AddCell("Status").AddCell(supplier.IsActive ? "Active" : "Inactive");
                companyInfoTable.AddCell("Approval State").AddCell(supplier.ApprovalState);
                document.Add(companyInfoTable);
                document.Add(new Paragraph().SetMarginBottom(20));

                // Contact Information Section
                var contactSection = new Paragraph("Contact Information")
                    .AddStyle(_pdfService.GetSectionStyle());
                document.Add(contactSection);

                var contactInfoTable = new Table(2).UseAllAvailableWidth();
                contactInfoTable.AddCell("Contact Person").AddCell(supplier.ContactPerson);
                contactInfoTable.AddCell("Email").AddCell(supplier.Email);
                contactInfoTable.AddCell("Phone").AddCell(supplier.ContactNo);
                contactInfoTable.AddCell("Address").AddCell(supplier.Address);
                document.Add(contactInfoTable);
                document.Add(new Paragraph().SetMarginBottom(20));

                // Contract Information Section
                var contractSection = new Paragraph("Contract Information")
                    .AddStyle(_pdfService.GetSectionStyle());
                document.Add(contractSection);

                var contractInfoTable = new Table(2).UseAllAvailableWidth();
                contractInfoTable.AddCell("Contract Start Date").AddCell(supplier.ContractStartDate.ToString("d"));
                contractInfoTable.AddCell("Contract End Date").AddCell(supplier.ContractEndDate.ToString("d"));
                contractInfoTable.AddCell("Payment Terms").AddCell(supplier.PaymentTerms);
                contractInfoTable.AddCell("Delivery Terms").AddCell(supplier.DeliveryTerms);
                document.Add(contractInfoTable);
            });

            string fileName = $"Supplier_{supplier.CompanyCode}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}