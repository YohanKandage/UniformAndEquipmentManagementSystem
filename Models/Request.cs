using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Request
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        [Required]
        [ForeignKey("Item")]
        public int ItemId { get; set; }
        public virtual Item Item { get; set; } = null!;

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public string? Reason { get; set; }

        public string? Remarks { get; set; }

        public string? AdminComment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cost { get; set; }

        public DateTime? ProcessedDate { get; set; }

        [ForeignKey("ProcessedBy")]
        public string? ProcessedById { get; set; }
        public virtual ApplicationUser? ProcessedBy { get; set; }

        public string? ProofImage1 { get; set; }
        public string? ProofImage2 { get; set; }
        public string? ProofImage3 { get; set; }
    }
} 