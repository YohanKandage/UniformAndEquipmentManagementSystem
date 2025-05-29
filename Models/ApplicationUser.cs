using Microsoft.AspNetCore.Identity;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }
        public string? Gender { get; set; }
        public int? EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }
    }
} 