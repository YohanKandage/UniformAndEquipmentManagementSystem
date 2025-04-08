using Microsoft.AspNetCore.Identity;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? EmployeeId { get; set; }
        public Employee Employee { get; set; }
    }
}
