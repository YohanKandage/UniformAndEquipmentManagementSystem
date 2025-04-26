using Microsoft.AspNetCore.Identity;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Department { get; set; }
        public int? EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }
    }
} 