using System.ComponentModel.DataAnnotations;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Company Code")]
        public string CompanyCode { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Contact Person Email")]
        public string ContactPersonEmail { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Province { get; set; } = string.Empty;

        [Required]
        public string District { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Approval State")]
        public string ApprovalState { get; set; } = "Pending";

        [Required]
        [Display(Name = "Supplier Category")]
        public string SupplierCategory { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Contract Start Date")]
        public DateTime ContractStartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Contract End Date")]
        public DateTime ContractEndDate { get; set; }

        [Required]
        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Delivery Terms")]
        public string DeliveryTerms { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Item>? Items { get; set; }
    }
} 