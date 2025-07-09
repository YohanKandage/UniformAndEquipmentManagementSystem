using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class ItemAssignment
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }
        public virtual Item Item { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public int? RequestId { get; set; }
        public virtual Request Request { get; set; }

        public DateTime AssignedDate { get; set; }
        public DateTime? ReturnedDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Assigned"; // Assigned, Returned, etc.

        public string? Remarks { get; set; }
        public decimal? Cost { get; set; }
    }
} 