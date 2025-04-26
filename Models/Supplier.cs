using System.ComponentModel.DataAnnotations;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "Company Code")]
        public string CompanyCode { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Contact No")]
        public string ContactNo { get; set; }

        [Required]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Contact Person Email")]
        public string ContactPersonEmail { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Province { get; set; }

        [Required]
        public string District { get; set; }

        [Required]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Required]
        [Display(Name = "Approval State")]
        public string ApprovalState { get; set; }

        [Required]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Supplier Category")]
        public string SupplierCategory { get; set; }

        [Required]
        [Display(Name = "Contract Start Date")]
        [DataType(DataType.Date)]
        public DateTime ContractStartDate { get; set; }

        [Required]
        [Display(Name = "Contract End Date")]
        [DataType(DataType.Date)]
        public DateTime ContractEndDate { get; set; }

        [Required]
        public string PaymentTerms { get; set; }

        [Required]
        public string DeliveryTerms { get; set; }
    }
} 