using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniformAndEquipmentManagementSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string EmployeeId { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        public string NIC { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public DateTime EmploymentDate { get; set; }

        [Required]
        public string Position { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department Department { get; set; }

        public string? ImagePath { get; set; }

        [NotMapped]
        public string? Password { get; set; }
    }
}
