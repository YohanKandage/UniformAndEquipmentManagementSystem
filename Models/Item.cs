using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Item Type")]
        public string ItemType { get; set; } // "Uniform" or "Equipment"

        [Required]
        [Display(Name = "Item ID")]
        public string ItemId { get; set; }

        [Required]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        [Required]
        [ForeignKey("Department")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        [Required]
        [Display(Name = "Job Role")]
        public string JobRole { get; set; }

        [Required]
        public string Material { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        public string? ImagePath { get; set; }
    }
}
